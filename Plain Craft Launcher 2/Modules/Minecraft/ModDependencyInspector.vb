Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

'只读本地元数据。这里的“确定错误”不包含无法解析的版本、未知 Java、候选嵌套 Jar 或未读完整的声明。
'不使用联网资源缓存：ResourceVersion.Dependencies 是平台工程 ID，不能用作加载器的 Mod ID。
Friend NotInheritable Class ModDependencyInspector
    Private Sub New()
    End Sub

    Friend Enum MatchState
        Unknown
        Satisfied
        Unsatisfied
    End Enum

    Friend Enum FileProviderKind
        None
        Required
        Possible
    End Enum

    Friend Enum DependencyKind
        Required
        [Optional]
        Recommended
        Suggested
        Incompatible
        Conflict
    End Enum

    Friend Enum IssueKind
        MissingRequired
        DuplicateId
        VersionMismatch
        Incompatible
        Recommendation
        LoaderMismatch
    End Enum

    Friend Class InspectionContext
        Public MinecraftVersion As String
        Public JavaVersion As String
        Public Side As String = "client"
        'False means that installed loaders are unknown, rather than that no loader is installed.
        Public LoaderKnown As Boolean
        Public LoaderVersions As New Dictionary(Of String, String)(StringComparer.Ordinal)
        Public AdditionalBuiltins As New Dictionary(Of String, String)(StringComparer.Ordinal)
        'Only legacy Forge loads the mods/<Minecraft version> directory.
        Public LegacyForgeVersionFolder As String
    End Class

    Friend Class ModInfo
        Public Id As String
        Public Group As String
        Public Name As String
        Public Version As String
        Public RootPath As String
        Public MetadataPath As String
        Public IconPath As String
        Public Format As String
        Public Side As String = "*"
        Public Enabled As Boolean
        Public IsNested As Boolean
        Public IsBuiltin As Boolean
        Public IsCandidate As Boolean
        'Legacy mcmod.info is descriptive; only a discovered @Mod (or runtime container) establishes a loaded ID.
        Public IsMetadataOnly As Boolean
        Public IsRuntimeCandidate As Boolean
        Public IncompatibleLoader As Boolean
        Public LoaderMismatchDetail As String
        Public Provides As New Dictionary(Of String, String)(StringComparer.Ordinal)
        Public Dependencies As New List(Of Dependency)
        Public ReadOnly Property Key As String
            Get
                Return If(RootPath, "") & "!" & If(MetadataPath, "") & ":" & Id
            End Get
        End Property
        Public ReadOnly Property Display As String
            Get
                Return If(String.IsNullOrWhiteSpace(Name), Id, Name) & " [" & Id & "]"
            End Get
        End Property
        Friend Function CopyWithEnabled(Value As Boolean) As ModInfo
            Dim Copy = DirectCast(MemberwiseClone(), ModInfo)
            Copy.Enabled = Value
            Return Copy
        End Function
    End Class

    Friend Class Dependency
        Public Id As String
        Public Versions As JToken = New JValue("*")
        Public Syntax As String = "fabric"
        Public Kind As DependencyKind
        Public Side As String = "*"
        Public Reason As String
        Public Children As New List(Of Dependency)
        Public Join As String = "any"
        Public Unless As Dependency
        Public IsUnknown As Boolean
        'Fabric suggests/recommends are informational. Forge/Quilt optional dependencies validate versions if present.
        Public EnforceWhenPresent As Boolean
        Public ReadOnly Property Display As String
            Get
                If Children.Count > 0 Then Return "(" & String.Join(If(Join = "all", " 且 ", " 或 "), Children.Select(Function(Child) Child.Display)) & ")"
                Dim Range = If(Versions Is Nothing, "*", If(Versions.Type = JTokenType.String, Versions.ToString(), Versions.ToString(Formatting.None)))
                Return If(Id, "未知依赖") & If(Range = "*" OrElse Range = "", "", " " & Range)
            End Get
        End Property
    End Class

    Friend Class DependencyEdge
        Public Owner As ModInfo
        Public Requirement As Dependency
        Public Providers As New List(Of ModInfo)
        Public State As MatchState
        Public Evaluated As Boolean
        Public ConditionBypassed As Boolean
    End Class

    Friend Class InspectionIssue
        Public Kind As IssueKind
        Public Owner As ModInfo
        Public Requirement As Dependency
        Public RelatedMods As New List(Of ModInfo)
        Public IsError As Boolean
        Public Message As String
    End Class

    Friend Class RemovalImpact
        Public [Mod] As ModInfo
        Public Requirements As New List(Of Dependency)
        Public IsDirect As Boolean
        Public IsDefinite As Boolean
        Public Reason As String
    End Class

    'Platform project IDs stay separate from loader Mod IDs, including after an exact content match.
    Friend Class OnlineProjectInfo
        Public Platform As String
        Public ProjectId As String
        Public VersionId As String
        Public ProjectName As String
        Public VersionName As String
        Public Website As String
        Public MatchBasis As String
        Public IsExact As Boolean
        Public Dependencies As New List(Of OnlineDependency)
    End Class
    Friend Class OnlineDependency
        Public Platform As String
        Public ProjectId As String
        Public VersionId As String
        Public VersionName As String
        Public ProjectName As String
        Public Website As String
        Public FileName As String
        Public Kind As String
        'Exact project identity regardless of a platform-pinned version; never used to satisfy that version.
        Public ProjectPaths As New List(Of String)
        Public MatchedPaths As New List(Of String)
        Public EnabledPaths As New List(Of String)
        Public Status As String = "尚未映射到本地文件，需核对"
        <JsonIgnore>
        Public ReadOnly Property DisplayName As String
            Get
                Dim Caption = {ProjectName, ProjectId, FileName, VersionName, VersionId}.FirstOrDefault(Function(Value) Not String.IsNullOrWhiteSpace(Value))
                Return If(Caption, "未命名依赖")
            End Get
        End Property
    End Class
    Friend Class OnlineFileInfo
        Public FullPath As String
        Public Sha1 As String
        Public Enabled As Boolean
        Public IsSupplementTarget As Boolean
        Public Matches As New List(Of OnlineProjectInfo)
        Public Candidates As New List(Of OnlineProjectInfo)
        Public Status As String
    End Class

    Friend Class InspectionReport
        Friend IndexHit As Boolean
        Friend IndexReevaluated As Boolean
        Friend IndexPersisted As Boolean
        Friend IndexReadFailed As Boolean
        Friend IndexFile As String
        Friend IndexFingerprint As String
        Friend IndexGeneration As String
        Public Context As InspectionContext
        Public Mods As New List(Of ModInfo)
        Public Builtins As New List(Of ModInfo)
        Public Edges As New List(Of DependencyEdge)
        Public Issues As New List(Of InspectionIssue)
        Public Notices As New List(Of String)
        Public MetadataIncomplete As Boolean
        Public OnlineAttempted As Boolean
        Public OnlineCancelled As Boolean
        Public OnlineTimedOut As Boolean
        Public OnlineIncomplete As Boolean
        Public OnlineRequests As Integer
        Public OnlineCheckedAtUtc As DateTime
        Public OnlineFiles As New List(Of OnlineFileInfo)
        Public OnlineNotes As New List(Of String)
        Public OnlineIndexScope As String
        Public ReverseDependencies As New Dictionary(Of String, List(Of DependencyEdge))(StringComparer.Ordinal)
        Public ReadOnly Property HasErrors As Boolean
            Get
                Return Issues.Any(Function(Issue) Issue.IsError)
            End Get
        End Property
        Public Function ToSummary(Optional MaxIssues As Integer = 30) As String
            Dim Lines As New List(Of String) From {
                "已检查 " & Mods.Where(Function(Item) Item.Enabled AndAlso Not Item.IsNested).Select(Function(Item) Item.RootPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() &
                " 个启用文件，识别 " & Mods.Count & " 个模组（包含禁用文件与内嵌模组）。",
                "确定问题：" & Issues.Where(Function(Issue) Issue.IsError).Count() & "；待确认或可选提示：" & Issues.Where(Function(Issue) Not Issue.IsError).Count() & "。"}
            For Each Issue In Issues.OrderByDescending(Function(Item) Item.IsError).Take(Math.Max(1, MaxIssues))
                Lines.Add(If(Issue.IsError, "• ", "· [提示] ") & Issue.Message)
            Next
            If Issues.Count > Math.Max(1, MaxIssues) Then Lines.Add("另有 " & (Issues.Count - Math.Max(1, MaxIssues)) & " 项，请在模组依赖详情中查看。")
            If MetadataIncomplete Then Lines.Add("部分文件的元数据未读完整；缺失依赖仅供确认，未据此判定启动错误。")
            If Notices.Count > 0 Then
                Lines.Add("")
                Lines.AddRange(Notices.Take(5))
                If Notices.Count > 5 Then Lines.Add("另有 " & (Notices.Count - 5) & " 条读取说明。")
            End If
            If OnlineAttempted Then
                Lines.Add("")
                Lines.Add("下载源补充：精确匹配 " & OnlineFiles.Where(Function(Item) Item.IsSupplementTarget AndAlso Item.Matches.Any()).Count() & " 个文件；使用 " & OnlineRequests & " 次请求。")
                Lines.AddRange(OnlineNotes.Take(4))
                Lines.Add("平台项目依赖仅作补充；候选和未映射项目未用于判定缺失或版本错误。")
            End If
            Lines.Add("")
            Lines.Add(If(OnlineAttempted, "以本地可靠声明为准；平台补充不能代替加载器对运行环境和内嵌版本的判断。",
                         "仅检查本地声明；运行时条件、加载器选择的内嵌版本与未声明依赖仍由游戏加载器判断。"))
            Return String.Join(vbCrLf, Lines)
        End Function
        Public Function DescribeFile(FullPath As String) As String
            Return ModDependencyInspector.DescribeFile(Me, FullPath)
        End Function
        Public Function DescribeOnlineFile(FullPath As String) As String
            If Not OnlineAttempted Then Return ""
            Dim Target = FilePathKey(FullPath), Lines As New List(Of String)
            Dim Info = OnlineFiles.FirstOrDefault(Function(Item) SameFilePath(Item.FullPath, Target))
            If Info IsNot Nothing Then
                Lines.Add("下载源补充（本地可靠声明优先；" & OnlineCheckedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") & "）：")
                For Each Match In Info.Matches
                    Lines.Add("• " & Match.Platform & " · " & Match.ProjectName & " · " & Match.VersionName & "（" & Match.MatchBasis & "）")
                    If Not String.IsNullOrEmpty(Match.Website) Then Lines.Add("  " & Match.Website)
                    If Match.Dependencies.Count = 0 Then Lines.Add("  该平台版本没有声明项目依赖。")
                    For Each Requirement In Match.Dependencies.Take(40)
                        Dim Caption = Requirement.DisplayName
                        Lines.Add("  • [" & OnlineKind(Requirement.Kind) & "] " & Caption & "：" & Requirement.Status)
                        If Not String.IsNullOrEmpty(Requirement.Website) Then Lines.Add("    " & Requirement.Website)
                    Next
                Next
                If Info.Matches.Count = 0 Then Lines.Add(If(Info.Status, "没有找到精确文件匹配。"))
                If Info.Candidates.Count > 0 Then
                    Lines.Add("按文件名搜索的候选（未选定、不能据此确认依赖）：")
                    For Each Candidate In Info.Candidates.Take(6)
                        Lines.Add("  • " & Candidate.Platform & " · " & Candidate.ProjectName & vbCrLf & "    " & Candidate.Website)
                    Next
                End If
            End If
            Dim Reverse As New HashSet(Of String)(StringComparer.Ordinal)
            For Each Owner In OnlineFiles.Where(Function(Item) Not SameFilePath(Item.FullPath, Target))
                For Each Match In Owner.Matches
                    For Each Requirement In Match.Dependencies.Where(Function(Item) Item.MatchedPaths.Any(Function(Value) SameFilePath(Value, Target)))
                        Reverse.Add("  • " & Path.GetFileName(Owner.FullPath) & " · " & Match.Platform & " [" & OnlineKind(Requirement.Kind) & "]")
                    Next
                Next
            Next
            If Reverse.Count > 0 Then
                Lines.Add("按平台精确匹配，被这些文件依赖（仍需核对运行环境）：")
                Lines.AddRange(Reverse.Take(40))
            End If
            If OnlineCancelled Then Lines.Add("联网补充已取消，本地检查结果已保留。")
            If OnlineTimedOut Then Lines.Add("联网补充达到时间预算，本地检查结果已保留。")
            Return String.Join(vbCrLf, Lines)
        End Function
        Public Function DescribeOnlineImpacts(RemovedPaths As IEnumerable(Of String)) As String
            If Not OnlineAttempted Then Return ""
            Dim Removed As New HashSet(Of String)(RemovedPaths.Select(Function(Value) FilePathKey(Value)), StringComparer.OrdinalIgnoreCase)
            Dim Lines As New HashSet(Of String)(StringComparer.Ordinal)
            For Each Owner In OnlineFiles.Where(Function(Item) Item.Enabled AndAlso Item.IsSupplementTarget AndAlso Not Removed.Contains(FilePathKey(Item.FullPath)))
                For Each Match In Owner.Matches.Where(Function(Item) Item.IsExact)
                    For Each Requirement In Match.Dependencies.Where(Function(Item) Item.Kind = "required" AndAlso Item.EnabledPaths.Count > 0)
                        If Not Requirement.EnabledPaths.All(Function(Value) Removed.Contains(FilePathKey(Value))) Then Continue For
                        Lines.Add("• " & Path.GetFileName(Owner.FullPath) & "：按 " & Match.Platform & " 版本声明，可能失去项目依赖 " &
                                  If(Requirement.ProjectName, Requirement.ProjectId) & "（平台补充，需核对）。")
                    Next
                Next
            Next
            If Lines.Count = 0 Then Return ""
            Return "下载源声明的补充影响：" & vbCrLf & String.Join(vbCrLf, Lines.Take(20))
        End Function
        Private Shared Function OnlineKind(Kind As String) As String
            Select Case Kind
                Case "required" : Return "必需"
                Case "optional" : Return "可选"
                Case "embedded" : Return "内嵌"
                Case "incompatible" : Return "不兼容"
                Case Else : Return "其他声明"
            End Select
        End Function
    End Class

    Private Const MaxMetadataBytes As Integer = 1024 * 1024
    Private Const MaxNestedBytes As Long = 64L * 1024 * 1024
    Private Const MaxNestedDepth As Integer = 4
    Private Const MaxNestedCount As Integer = 256

    'Does not write settings, resolve online projects, or read account files.
    Friend Shared Function ContextForInstance(Instance As McInstance, Optional JavaVersion As String = Nothing) As InspectionContext
        If Instance Is Nothing Then Throw New ArgumentNullException(NameOf(Instance))
        Dim Result As New InspectionContext With {.MinecraftVersion = If(Instance.Version.Reliable, Instance.Version.VanillaName, Nothing), .JavaVersion = JavaVersion, .LoaderKnown = True}
        For Each Pair In Instance.Version.ModLoaderVersions
            Result.LoaderVersions(CanonicalLoaderId(Pair.Key.ToString())) = Pair.Value
        Next
        'McVersion represents Quilt under the Fabric flag; its Fabric compatibility version is NOT the Quilt version.
        Dim Libraries = TryCast(Instance.JsonObject?("libraries"), JArray)
        If Libraries IsNot Nothing Then
            For Each Library As JObject In Libraries.OfType(Of JObject)()
                Dim Coordinate = Text(Library("name"))
                If Coordinate.StartsWith("org.quiltmc:quilt-loader:", StringComparison.Ordinal) Then
                    Result.LoaderVersions.Remove("fabricloader")
                    Result.LoaderVersions("quilt_loader") = Coordinate.Substring("org.quiltmc:quilt-loader:".Length).Split(":"c)(0)
                    Result.LoaderVersions("fabricloader") = Nothing
                ElseIf Result.LoaderVersions.ContainsKey("neoforge") AndAlso Coordinate.StartsWith("net.neoforged.fancymodloader:loader:", StringComparison.Ordinal) Then
                    'BuiltInLanguageLoader uses the FML loader JAR version, independently of the NeoForge platform version.
                    Result.AdditionalBuiltins("javafml") = Coordinate.Split(":"c)(2)
                End If
            Next
        End If
        Dim ForgeVersion As String = Nothing
        If Result.LoaderVersions.TryGetValue("forge", ForgeVersion) AndAlso Regex.IsMatch(If(Result.MinecraftVersion, ""), "^1\.(?:[0-9]|1[0-2])(?:\.|$)") Then
            Result.LegacyForgeVersionFolder = Result.MinecraftVersion
        End If
        If Result.LoaderVersions.ContainsKey("neoforge") AndAlso Result.MinecraftVersion = "1.20.1" Then
            Result.LoaderVersions("forge") = Result.LoaderVersions("neoforge")
        End If
        If Instance.Version.HasOptiFine AndAlso Not Instance.Version.OptiFineAsMod Then Result.AdditionalBuiltins("optifine") = Instance.Version.OptiFine
        Return Result
    End Function

    '仅统一比较键；实际读取仍使用原始路径，保留 Windows 长路径支持。
    Friend Shared Function FilePathKey(FullPath As String) As String
        Dim Value = Path.GetFullPath(FullPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))
        If Value.StartsWith("\\?\UNC\", StringComparison.OrdinalIgnoreCase) Then
            Value = "\\" & Value.Substring(8)
        ElseIf Value.StartsWith("\\?\", StringComparison.OrdinalIgnoreCase) AndAlso Value.Length >= 7 AndAlso
            Char.IsLetter(Value(4)) AndAlso Value(5) = ":"c AndAlso Value(6) = "\"c Then
            Value = Value.Substring(4)
        End If
        Return Value
    End Function

    Friend Shared Function SameFilePath(First As String, Second As String) As Boolean
        Return String.Equals(FilePathKey(First), FilePathKey(Second), StringComparison.OrdinalIgnoreCase)
    End Function

    Friend Shared Function Inspect(ModsFolder As String, Optional Context As InspectionContext = Nothing,
                                   Optional Token As CancellationToken = Nothing) As InspectionReport
        Dim Result = ModDependencyIndex.Inspect(ModsFolder, If(Context, New InspectionContext()), Token, False)
        RememberLoaderWarnings(ModsFolder, Result)
        Return Result
    End Function
    Friend Shared Function RebuildIndex(ModsFolder As String, Optional Context As InspectionContext = Nothing,
                                      Optional Token As CancellationToken = Nothing) As InspectionReport
        Dim Result = ModDependencyIndex.Inspect(ModsFolder, If(Context, New InspectionContext()), Token, True)
        RememberLoaderWarnings(ModsFolder, Result)
        Return Result
    End Function
    Friend Shared Function ScanRaw(ModsFolder As String, Context As InspectionContext, Token As CancellationToken, Optional ForceFresh As Boolean = False) As InspectionReport
        Dim Result As New InspectionReport With {.Context = If(Context, New InspectionContext())}
        Result.Builtins = CreateBuiltins(Result.Context)
        If Not Directory.Exists(ModsFolder) Then Return Result
        Dim Roots As New List(Of String) From {Path.GetFullPath(ModsFolder)}
        If Not String.IsNullOrEmpty(Result.Context.LegacyForgeVersionFolder) AndAlso
            Regex.IsMatch(Result.Context.LegacyForgeVersionFolder, "^[0-9.]+$") Then
            Dim Legacy = Path.Combine(ModsFolder, Result.Context.LegacyForgeVersionFolder)
            If Directory.Exists(Legacy) Then Roots.Add(Path.GetFullPath(Legacy))
        End If
        For Each Folder In Roots
            Token.ThrowIfCancellationRequested()
            Dim Files As String()
            Try
                If (File.GetAttributes(Folder) And FileAttributes.ReparsePoint) <> 0 Then
                    Incomplete(Result, "跳过链接目录：" & Path.GetFileName(Folder))
                    Continue For
                End If
                Files = Directory.EnumerateFiles(Folder, "*", SearchOption.TopDirectoryOnly).Where(Function(Item) IsModFile(Item)).OrderBy(Function(Item) Item, StringComparer.OrdinalIgnoreCase).ToArray()
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                Result.IndexReadFailed = True
                Incomplete(Result, "无法枚举模组目录：" & ex.Message)
                Continue For
            End Try
            For Each FullPath In Files
                Token.ThrowIfCancellationRequested()
                Dim Enabled = Not FullPath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) AndAlso Not FullPath.EndsWith(".old", StringComparison.OrdinalIgnoreCase)
                Try
                    If (File.GetAttributes(FullPath) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("跳过链接文件")
                    Using Stream As New FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete),
                          Archive As New ZipArchive(Stream, ZipArchiveMode.Read, False)
                        Dim Budget As New ReadBudget With {.ForceFresh = ForceFresh}
                        ReadArchive(Archive, Path.GetFullPath(FullPath), "", Enabled, False, 0, Result, Budget, Token)
                    End Using
                Catch ex As OperationCanceledException
                    Throw
                Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is InvalidDataException OrElse
                    TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is JsonException OrElse TypeOf ex Is FormatException OrElse TypeOf ex Is DecoderFallbackException
                    If (TypeOf ex Is IOException AndAlso Not TypeOf ex Is InvalidDataException) OrElse TypeOf ex Is UnauthorizedAccessException Then Result.IndexReadFailed = True
                    If Enabled Then
                        Incomplete(Result, Path.GetFileName(FullPath) & "：无法完整读取元数据（" & ex.Message & "）。")
                    Else
                        Result.Notices.Add(Path.GetFileName(FullPath) & "：已禁用，无法读取元数据。")
                    End If
                End Try
            Next
        Next
        BuildReport(Result, Token)
        Return Result
    End Function
    Friend Shared Sub Reevaluate(Report As InspectionReport, Context As InspectionContext, Token As CancellationToken)
        Report.Context = Context : Report.Builtins = CreateBuiltins(Context)
        Report.Edges.Clear() : Report.Issues.Clear() : Report.ReverseDependencies.Clear()
        BuildReport(Report, Token)
        Report.Notices = Report.Notices.Distinct().ToList()
    End Sub

    Private Class LoaderWarningMark
        Public Length As Long
        Public LastWrite As Long
        Public Message As String
    End Class
    Private Class LoaderWarningSnapshot
        Public ContextKey As String
        Public Files As New Dictionary(Of String, LoaderWarningMark)(StringComparer.OrdinalIgnoreCase)
    End Class
    Private Shared ReadOnly LoaderWarnings As New System.Collections.Concurrent.ConcurrentDictionary(Of String, LoaderWarningSnapshot)(StringComparer.OrdinalIgnoreCase)
    Private Shared Function LoaderContextKey(Context As InspectionContext) As String
        Return Context.LoaderKnown & "|" & Context.MinecraftVersion & "|" & String.Join(";", Context.LoaderVersions.OrderBy(Function(Pair) Pair.Key).
            Select(Function(Pair) Pair.Key & "=" & Pair.Value))
    End Function
    Private Shared Sub RememberLoaderWarnings(Folder As String, Report As InspectionReport)
        Dim Snapshot As New LoaderWarningSnapshot With {.ContextKey = LoaderContextKey(Report.Context)}
        For Each Issue In Report.Issues.Where(Function(Value) Value.Kind = IssueKind.LoaderMismatch)
            Try
                Dim Info As New FileInfo(Issue.Owner.RootPath)
                If Info.Exists Then Snapshot.Files(FilePathKey(Info.FullName)) = New LoaderWarningMark With {.Length = Info.Length,
                    .LastWrite = Info.LastWriteTimeUtc.Ticks, .Message = "加载器不匹配" & vbCrLf & Issue.Message}
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                'A changed or removed file will be checked again on refresh.
            End Try
        Next
        If LoaderWarnings.Count > 32 Then LoaderWarnings.Clear()
        LoaderWarnings(FilePathKey(Folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) = Snapshot
    End Sub
    Friend Shared Function LoaderWarningFor(Folder As String, FullPath As String, Context As InspectionContext) As String
        If Context Is Nothing Then Return ""
        Dim Snapshot As LoaderWarningSnapshot = Nothing, Mark As LoaderWarningMark = Nothing
        If Not LoaderWarnings.TryGetValue(FilePathKey(Folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Snapshot) OrElse Snapshot.ContextKey <> LoaderContextKey(Context) OrElse
            Not Snapshot.Files.TryGetValue(FilePathKey(FullPath), Mark) Then Return ""
        Try
            Dim Info As New FileInfo(FullPath)
            If Info.Exists AndAlso (Info.Attributes And FileAttributes.ReparsePoint) = 0 AndAlso Info.Length = Mark.Length AndAlso
                Info.LastWriteTimeUtc.Ticks = Mark.LastWrite Then Return Mark.Message
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
            'Never keep a red marker for a file that no longer matches the inspected content.
        End Try
        Return ""
    End Function

    Private Class ReadBudget
        Public ForceFresh As Boolean
        Public Bytes As Long
        Public Count As Integer
    End Class

    Friend Shared Function IsModFile(FullPath As String) As Boolean
        Dim Name = FullPath
        If Name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) OrElse Name.EndsWith(".old", StringComparison.OrdinalIgnoreCase) Then Name = Path.GetFileNameWithoutExtension(Name)
        Return {".jar", ".zip", ".litemod"}.Contains(Path.GetExtension(Name).ToLowerInvariant())
    End Function

    Private Shared Sub Incomplete(Report As InspectionReport, Message As String)
        Report.MetadataIncomplete = True
        If Not Report.Notices.Contains(Message) Then Report.Notices.Add(Message)
    End Sub

    Private Shared Function ReadText(Entry As ZipArchiveEntry) As String
        If Entry.Length > MaxMetadataBytes Then Throw New InvalidDataException("元数据超过 1 MiB")
        Using Stream = Entry.Open(), Reader As New StreamReader(Stream, New UTF8Encoding(False, True), True)
            Dim Buffer(4095) As Char, Content As New StringBuilder(), Count As Integer
            Do
                Count = Reader.Read(Buffer, 0, Buffer.Length)
                Content.Append(Buffer, 0, Count)
                If Content.Length > MaxMetadataBytes Then Throw New InvalidDataException("元数据超过 1 MiB")
            Loop While Count > 0
            Return Content.ToString()
        End Using
    End Function

    Private Shared Sub ReadArchive(Archive As ZipArchive, RootPath As String, NestedPath As String, Enabled As Boolean,
                                  Nested As Boolean, Depth As Integer, Report As InspectionReport, Budget As ReadBudget, Token As CancellationToken)
        Token.ThrowIfCancellationRequested()
        If IsLegacyForge(Report.Context) Then
            Dim Before = Report.Mods.Count
            ReadLegacyArchive(Archive, RootPath, NestedPath, Enabled, Nested, Depth, Report, Budget, Token)
            If Report.Mods.Skip(Before).Any(Function(Value) Not Value.IsMetadataOnly) OrElse
                Not {"fabric.mod.json", "quilt.mod.json", "META-INF/mods.toml", "META-INF/neoforge.mods.toml"}.Any(Function(Name) Archive.GetEntry(Name) IsNot Nothing) Then Return
            'A modern-only mod in a legacy instance still needs a loader mismatch warning.
            Report.Mods.RemoveRange(Before, Report.Mods.Count - Before)
        End If
        Dim Choices As New List(Of String)
        Dim Loaders = Report.Context.LoaderVersions
        If Loaders.ContainsKey("quilt_loader") Then Choices.Add("quilt.mod.json")
        If Loaders.ContainsKey("fabricloader") OrElse Loaders.ContainsKey("quilt_loader") Then Choices.Add("fabric.mod.json")
        If Loaders.ContainsKey("neoforge") AndAlso NeoModernMetadata(Report.Context).GetValueOrDefault(True) Then Choices.Add("META-INF/neoforge.mods.toml")
        If Loaders.ContainsKey("forge") OrElse Loaders.ContainsKey("neoforge") Then Choices.Add("META-INF/mods.toml")
        Choices.AddRange({"quilt.mod.json", "fabric.mod.json", "META-INF/neoforge.mods.toml", "META-INF/mods.toml", "mcmod.info", "litemod.json"})
        Dim Metadata = Choices.Distinct(StringComparer.Ordinal).Select(Function(Name) Archive.GetEntry(Name)).FirstOrDefault(Function(Entry) Entry IsNot Nothing)
        If Metadata Is Nothing Then
            Dim Before = Report.Mods.Count
            ReadLegacyArchive(Archive, RootPath, NestedPath, Enabled, Nested, Depth, Report, Budget, Token)
            For Each Item In Report.Mods.Skip(Before)
                Item.IncompatibleLoader = Not LoaderAccepts("legacy", Report.Context)
            Next
            Return
        End If
        Dim Items As New List(Of ModInfo), NestedJars As New List(Of String), Content = ReadText(Metadata), DeclaredPlatform As String = ""
        Select Case Metadata.FullName
            Case "fabric.mod.json"
                Dim Json = JObject.Parse(Content)
                Dim Schema = If(Json("schemaVersion") Is Nothing, "0", Text(Json("schemaVersion"))), Supported = Schema = "0" OrElse Schema = "1"
                If Not Supported AndAlso Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：Fabric schemaVersion 未受支持，未推断其中的依赖声明。")
                For Each Name In If(Schema = "0", {"requires", "recommends", "conflicts"}, {"depends", "recommends", "suggests", "breaks", "conflicts"})
                    If Json(Name) IsNot Nothing AndAlso Not TypeOf Json(Name) Is JObject AndAlso Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：无法完整读取 " & Name & " 声明。")
                Next
                Dim Item = NewMod(Json("id"), Json("name"), Json("version"), RootPath, NestedPath & Metadata.FullName, "Fabric", Enabled, Nested)
                Item.Side = ReadSide(Json(If(Schema = "0", "side", "environment")))
                If Schema = "0" AndAlso Item.Side = "universal" Then Item.Side = "*"
                Item.IconPath = ReadIconPath(Json("icon"))
                For Each AliasId In If(Schema = "1", ArrayValues(Json("provides")), Enumerable.Empty(Of JToken)())
                    If AliasId.Type = JTokenType.String Then Item.Provides(Text(AliasId)) = Item.Version
                Next
                If Supported Then ReadFabricDependencies(Item, Json, Schema = "0")
                Items.Add(Item)
                For Each Jar In If(Schema = "1", ArrayValues(Json("jars")), Enumerable.Empty(Of JToken)())
                    Dim EntryName = If(Jar.Type = JTokenType.Object, Text(Jar("file")), "")
                    If EntryName <> "" Then NestedJars.Add(EntryName)
                Next
            Case "quilt.mod.json"
                Dim Json = JObject.Parse(Content), Loader = TryCast(Json("quilt_loader"), JObject)
                If Loader Is Nothing Then Throw New FormatException("缺少 quilt_loader")
                If Text(Json("schema_version")) <> "1" AndAlso Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：Quilt 元数据并非受支持的 schema_version 1。")
                For Each Name In {"depends", "breaks", "provides", "jars"}
                    If Loader(Name) IsNot Nothing AndAlso Not TypeOf Loader(Name) Is JArray AndAlso Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：无法完整读取 " & Name & " 声明。")
                Next
                Dim Item = NewMod(Loader("id"), Loader("metadata")?("name"), Loader("version"), RootPath, NestedPath & Metadata.FullName, "Quilt", Enabled, Nested)
                Item.Group = Text(Loader("group"))
                Item.IconPath = ReadIconPath(Loader("metadata")?("icon"))
                Item.Side = ReadSide(Json("minecraft")?("environment"))
                For Each AliasValue In ArrayValues(Loader("provides"))
                    Dim AliasId = If(AliasValue.Type = JTokenType.String, Text(AliasValue), Text(AliasValue("id")))
                    If AliasId = "" Then Continue For
                    Dim AliasVersion = If(AliasValue.Type = JTokenType.Object AndAlso AliasValue("version") IsNot Nothing, Text(AliasValue("version")), Item.Version)
                    Item.Provides(AliasId) = AliasVersion
                    If Not AliasId.Contains(":") AndAlso Item.Group <> "" Then Item.Provides(Item.Group & ":" & AliasId) = AliasVersion
                    If AliasId.Contains(":") Then Item.Provides(AliasId.Substring(AliasId.LastIndexOf(":"c) + 1)) = AliasVersion
                Next
                For Each Value In ArrayValues(Loader("depends"))
                    Item.Dependencies.Add(ReadQuiltDependency(Value, DependencyKind.Required, True))
                Next
                For Each Value In ArrayValues(Loader("breaks"))
                    Item.Dependencies.Add(ReadQuiltDependency(Value, DependencyKind.Incompatible, False))
                Next
                Items.Add(Item)
                For Each Jar In ArrayValues(Loader("jars"))
                    If Jar.Type = JTokenType.String Then NestedJars.Add(Text(Jar))
                Next
            Case "META-INF/mods.toml", "META-INF/neoforge.mods.toml"
                'NeoForge 1.20.4 also uses mods.toml; distinguish it using actual platform dependencies.
                Dim Platforms = ReadToml(Content).Where(Function(Section) Section.Name.StartsWith("dependencies.", StringComparison.Ordinal) AndAlso
                    Scalar(Section, "mandatory") <> "false" AndAlso {"", "required"}.Contains(Scalar(Section, "type"))).Select(Function(Section) Scalar(Section, "modId")).ToList()
                If Metadata.FullName.Contains("neoforge") OrElse Platforms.Contains("neoforge") Then
                    DeclaredPlatform = "neoforge"
                ElseIf Platforms.Contains("forge") Then
                    DeclaredPlatform = "forge"
                End If
                Dim IsNeo = DeclaredPlatform = "neoforge" OrElse (DeclaredPlatform = "" AndAlso Loaders.ContainsKey("neoforge"))
                ReadForge(Content, Archive, RootPath, NestedPath & Metadata.FullName, Enabled, Nested, IsNeo, Items, Report)
                Dim JarJar = Archive.GetEntry("META-INF/jarjar/metadata.json")
                If JarJar IsNot Nothing Then
                    For Each Jar In ArrayValues(JObject.Parse(ReadText(JarJar))("jars"))
                        Dim EntryName = Text(Jar("path"))
                        If EntryName <> "" Then NestedJars.Add(EntryName)
                    Next
                End If
            Case "mcmod.info"
                Dim Before = Report.Mods.Count
                ReadLegacyArchive(Archive, RootPath, NestedPath, Enabled, Nested, Depth, Report, Budget, Token)
                For Each Item In Report.Mods.Skip(Before)
                    Item.IncompatibleLoader = Not LoaderAccepts("legacy", Report.Context)
                Next
                Return
            Case "litemod.json"
                Dim Json = JObject.Parse(Content)
                Dim Item = NewMod(Json("name"), Json("displayName"), Json("version"), RootPath, NestedPath & Metadata.FullName, "LiteLoader", Enabled, Nested)
                Item.IconPath = ReadIconPath(Json("icon"))
                Items.Add(Item)
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：LiteLoader 依赖可能在运行时代码中声明。")
        End Select
        If Items.Count = 0 Then Throw New FormatException("声明中未找到模组 ID")
        For Each Item In Items
            Item.IncompatibleLoader = Not LoaderAccepts(Metadata.FullName, Report.Context, DeclaredPlatform)
            Dim NeoGeneration = NeoModernMetadata(Report.Context)
            If Item.IncompatibleLoader AndAlso Loaders.ContainsKey("neoforge") AndAlso NeoGeneration.HasValue AndAlso
                {"META-INF/mods.toml", "META-INF/neoforge.mods.toml"}.Contains(Metadata.FullName) Then
                Item.LoaderMismatchDetail = "此 NeoForge 版本读取 " & If(NeoGeneration.Value, "META-INF/neoforge.mods.toml", "META-INF/mods.toml") & "，当前文件的声明格式不适用。"
            End If
        Next
        Report.Mods.AddRange(Items)
        ReadNestedArchives(Archive, NestedJars, RootPath, NestedPath, Enabled, Depth, Report, Budget, Token)
    End Sub

    Private Shared Sub ReadNestedArchives(Archive As ZipArchive, NestedJars As IEnumerable(Of String), RootPath As String, NestedPath As String,
                                          Enabled As Boolean, Depth As Integer, Report As InspectionReport, Budget As ReadBudget, Token As CancellationToken)
        For Each EntryName In NestedJars.Distinct(StringComparer.Ordinal)
            Token.ThrowIfCancellationRequested()
            Dim Parts = EntryName.Replace("\"c, "/"c).Split("/"c)
            If EntryName.StartsWith("/", StringComparison.Ordinal) OrElse Parts.Any(Function(Part) Part = ".." OrElse Part = "") Then
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：跳过不安全的内嵌路径。")
                Continue For
            End If
            Dim Jar = Archive.GetEntry(EntryName)
            If Jar Is Nothing Then
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：声明的内嵌文件不存在：" & EntryName)
                Continue For
            End If
            If Depth >= MaxNestedDepth OrElse Budget.Count >= MaxNestedCount OrElse Jar.Length > MaxNestedBytes - Budget.Bytes Then
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：内嵌模组达到读取上限。")
                Continue For
            End If
            Budget.Bytes += Jar.Length
            Budget.Count += 1
            Try
                Using Buffer As New MemoryStream()
                    Using Input = Jar.Open()
                        Dim Bytes(8191) As Byte, Count As Integer, Remaining = Jar.Length
                        Do
                            Count = Input.Read(Bytes, 0, CInt(Math.Min(Bytes.Length, Remaining + 1)))
                            If Count > Remaining Then Throw New InvalidDataException("内嵌文件长度与声明不符")
                            Buffer.Write(Bytes, 0, Count)
                            Remaining -= Count
                        Loop While Count > 0
                    End Using
                    Buffer.Position = 0
                    Using Inner As New ZipArchive(Buffer, ZipArchiveMode.Read, True)
                        ReadArchive(Inner, RootPath, NestedPath & EntryName & "!", Enabled, True, Depth + 1, Report, Budget, Token)
                    End Using
                End Using
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is InvalidDataException OrElse TypeOf ex Is JsonException OrElse TypeOf ex Is FormatException OrElse TypeOf ex Is DecoderFallbackException
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "!" & EntryName & "：无法完整读取内嵌模组。")
            End Try
        Next
    End Sub

    Private Shared Function NewMod(Id As JToken, Name As JToken, Version As JToken, RootPath As String, MetadataPath As String,
                                  Format As String, Enabled As Boolean, Nested As Boolean) As ModInfo
        Dim ModId = Text(Id)
        If Not Regex.IsMatch(ModId, "^[A-Za-z0-9_.-]+$") Then Throw New FormatException("模组 ID 无效")
        Return New ModInfo With {.Id = ModId, .Name = If(Text(Name) = "", ModId, Text(Name)), .Version = Text(Version),
            .RootPath = RootPath, .MetadataPath = MetadataPath, .Format = Format, .Enabled = Enabled, .IsNested = Nested}
    End Function

    Private Shared Function Text(Value As JToken) As String
        Return If(Value Is Nothing OrElse Value.Type = JTokenType.Null, "", Value.ToString())
    End Function
    Private Shared Function ArrayValues(Value As JToken) As IEnumerable(Of JToken)
        Return If(TypeOf Value Is JArray, CType(Value, JArray).AsEnumerable(), Enumerable.Empty(Of JToken)())
    End Function
    Private Shared Function ReadIconPath(Value As JToken) As String
        If TypeOf Value Is JObject Then
            Value = CType(Value, JObject).Properties().OrderBy(Function(Item)
                Dim Size As Integer
                Return If(Integer.TryParse(Item.Name, Size) AndAlso Size >= 32, Size, Integer.MaxValue)
            End Function).Select(Function(Item) Item.Value).FirstOrDefault()
        End If
        If Value Is Nothing OrElse Value.Type <> JTokenType.String Then Return ""
        Dim Name = Text(Value).Replace("\"c, "/"c)
        If Name.StartsWith("/", StringComparison.Ordinal) OrElse Name.Contains(":") OrElse
            Name.Split("/"c).Any(Function(Part) Part = ".." OrElse Part = "" OrElse Part = ".") Then Return ""
        Return Name
    End Function

    Private Shared Function ReadSide(Value As JToken) As String
        If TypeOf Value Is JArray Then
            Dim Values = CType(Value, JArray).Select(Function(Item) Text(Item)).ToList()
            If Values.Contains("*") OrElse (Values.Contains("client") AndAlso Values.Contains("server")) Then Return "*"
            Return String.Join(",", Values)
        End If
        Return If(Text(Value) = "", "*", Text(Value)).ToLowerInvariant().Replace("dedicated_server", "server")
    End Function
    Private Shared Function SideApplies(Side As String, Context As InspectionContext) As Boolean
        Return Side = "*" OrElse Side = "" OrElse Side = "both" OrElse Side.Split(","c).Contains(If(Context.Side, "client").ToLowerInvariant())
    End Function
    Private Shared Sub ReadFabricDependencies(Item As ModInfo, Json As JObject, Optional LegacySchema As Boolean = False)
        Dim Kinds = If(LegacySchema,
            New Dictionary(Of String, DependencyKind) From {{"requires", DependencyKind.Required}, {"recommends", DependencyKind.Suggested}, {"conflicts", DependencyKind.Incompatible}},
            New Dictionary(Of String, DependencyKind) From {{"depends", DependencyKind.Required}, {"recommends", DependencyKind.Recommended},
                {"suggests", DependencyKind.Suggested}, {"breaks", DependencyKind.Incompatible}, {"conflicts", DependencyKind.Conflict}})
        For Each Pair In Kinds
            Dim Values = TryCast(Json(Pair.Key), JObject)
            If Values Is Nothing Then Continue For
            For Each Entry In Values.Properties()
                Item.Dependencies.Add(New Dependency With {.Id = CanonicalId(Entry.Name), .Versions = Entry.Value.DeepClone(), .Kind = Pair.Value})
            Next
        Next
    End Sub
    Private Shared Function ReadQuiltDependency(Value As JToken, Kind As DependencyKind, Any As Boolean) As Dependency
        Dim Result As New Dependency With {.Kind = Kind, .Join = If(Any, "any", "all")}
        If TypeOf Value Is JArray Then
            For Each Child In CType(Value, JArray)
                Result.Children.Add(ReadQuiltDependency(Child, Kind, Any))
            Next
            Result.IsUnknown = Result.Children.Count = 0
        ElseIf Value.Type = JTokenType.String Then
            Result.Id = CanonicalId(Text(Value))
        ElseIf TypeOf Value Is JObject Then
            Result.Id = CanonicalId(Text(Value("id")))
            Result.Versions = If(Value("versions")?.DeepClone(), New JValue("*"))
            Result.Reason = Text(Value("reason"))
            If Text(Value("optional")).Equals("true", StringComparison.OrdinalIgnoreCase) Then
                Result.Kind = DependencyKind.Optional
                Result.EnforceWhenPresent = True
            End If
            If Value("unless") IsNot Nothing Then Result.Unless = ReadQuiltDependency(Value("unless"), DependencyKind.Required, True)
        Else
            Result.IsUnknown = True
        End If
        If Result.Children.Count = 0 AndAlso String.IsNullOrEmpty(Result.Id) Then Result.IsUnknown = True
        Return Result
    End Function

    Private Class TomlSection
        Public Name As String
        Public Values As New Dictionary(Of String, String)(StringComparer.Ordinal)
    End Class

    'A bounded TOML subset reader, with quoted/comment/multiline state. Descriptions must never become dependency declarations.
    'Only complete scalar assignments in recognised sections are used; unsupported values remain unknown.
    Private Shared Function ReadToml(Content As String) As List(Of TomlSection)
        Dim Sections As New List(Of TomlSection), Current As New TomlSection With {.Name = ""}, Statement As New StringBuilder()
        Sections.Add(Current)
        Dim Quote As Char = ChrW(0), Triple As Boolean, Escape As Boolean, Square As Integer, Curly As Integer, Comment As Boolean, Index As Integer
        While Index <= Content.Length
            Dim Ch = If(Index = Content.Length, ChrW(10), Content(Index))
            If Comment Then
                If Ch <> ChrW(10) Then
                    Index += 1
                    Continue While
                End If
                Comment = False
            End If
            If Quote <> ChrW(0) Then
                Statement.Append(Ch)
                If Escape Then
                    Escape = False
                ElseIf Ch = "\"c AndAlso Quote = """"c Then
                    Escape = True
                ElseIf Ch = Quote Then
                    If Triple Then
                        If Index + 2 < Content.Length AndAlso Content(Index + 1) = Quote AndAlso Content(Index + 2) = Quote Then
                            Statement.Append(Quote, 2)
                            Index += 2
                            Quote = ChrW(0)
                            Triple = False
                        End If
                    Else
                        Quote = ChrW(0)
                    End If
                End If
            ElseIf Ch = "#"c Then
                Comment = True
            ElseIf Ch = """"c OrElse Ch = "'"c Then
                Quote = Ch
                Triple = Index + 2 < Content.Length AndAlso Content(Index + 1) = Ch AndAlso Content(Index + 2) = Ch
                Statement.Append(Ch)
                If Triple Then
                    Statement.Append(Ch, 2)
                    Index += 2
                End If
            Else
                If Ch = "["c Then Square += 1
                If Ch = "]"c Then Square -= 1
                If Ch = "{"c Then Curly += 1
                If Ch = "}"c Then Curly -= 1
                If Ch = ChrW(10) AndAlso Square <= 0 AndAlso Curly <= 0 Then
                    Dim Line = Statement.ToString().Trim()
                    Statement.Clear()
                    If Line.StartsWith("[", StringComparison.Ordinal) Then
                        Dim Header = Regex.Match(Line, "^\[{1,2}\s*([A-Za-z0-9_.-]+)\s*\]{1,2}$")
                        Current = New TomlSection With {.Name = If(Header.Success, Header.Groups(1).Value, "?")}
                        Sections.Add(Current)
                    ElseIf Line <> "" Then
                        Dim Assignment = Regex.Match(Line, "^([A-Za-z0-9_.-]+)\s*=\s*(.+)$", RegexOptions.Singleline)
                        If Assignment.Success Then Current.Values(Assignment.Groups(1).Value) = Assignment.Groups(2).Value.Trim()
                    End If
                    Square = 0
                    Curly = 0
                Else
                    Statement.Append(Ch)
                End If
            End If
            Index += 1
        End While
        If Quote <> ChrW(0) OrElse Square <> 0 OrElse Curly <> 0 Then Throw New FormatException("TOML 字符串或容器未结束")
        Return Sections
    End Function
    Private Shared Function Scalar(Section As TomlSection, Key As String, Optional DefaultValue As String = "") As String
        Dim Raw As String = Nothing
        If Not Section.Values.TryGetValue(Key, Raw) Then Return DefaultValue
        If Raw.StartsWith("""" & """" & """", StringComparison.Ordinal) OrElse Raw.StartsWith("'''", StringComparison.Ordinal) Then Return DefaultValue
        If Raw.Length >= 2 AndAlso Raw(0) = "'"c AndAlso Raw.Last() = "'"c Then Return Raw.Substring(1, Raw.Length - 2)
        If Raw.Length >= 2 AndAlso Raw(0) = """"c AndAlso Raw.Last() = """"c Then
            Try
                Return JsonConvert.DeserializeObject(Of String)(Raw)
            Catch ex As JsonException
                Return DefaultValue
            End Try
        End If
        If Raw = "true" OrElse Raw = "false" Then Return Raw
        Return DefaultValue
    End Function
    Private Shared Sub ReadForge(Content As String, Archive As ZipArchive, RootPath As String, MetadataPath As String, Enabled As Boolean,
                                 Nested As Boolean, Neo As Boolean, Items As List(Of ModInfo), Report As InspectionReport)
        Dim Sections = ReadToml(Content), GlobalSection = Sections(0), FileProperties = Sections.FirstOrDefault(Function(Entry) Entry.Name = "properties")
        If Enabled AndAlso Sections.Any(Function(Section) Section.Name = "?") Then Incomplete(Report, Path.GetFileName(RootPath) & "：存在未识别的 TOML 表名，部分声明未解析。")
        Dim ManifestVersion As String = ""
        Dim Manifest = Archive.GetEntry("META-INF/MANIFEST.MF")
        If Manifest IsNot Nothing Then
            Dim ManifestText = Regex.Replace(ReadText(Manifest), "\r?\n ", "")
            Dim Match = Regex.Match(ManifestText, "(?m)^Implementation-Version:\s*([^\r\n]+)")
            If Match.Success Then ManifestVersion = Match.Groups(1).Value.Trim()
        End If
        For Each Section In Sections.Where(Function(Entry) Entry.Name = "mods")
            Dim Version = Regex.Replace(Scalar(Section, "version"), "\$\{file\.([A-Za-z0-9_.-]+)\}", Function(Match)
                Dim Value = If(Match.Groups(1).Value = "jarVersion", ManifestVersion, If(FileProperties Is Nothing, "", Scalar(FileProperties, Match.Groups(1).Value)))
                Return If(Value = "", Match.Value, Value)
            End Function)
            Dim Item = NewMod(New JValue(Scalar(Section, "modId")), New JValue(Scalar(Section, "displayName")), New JValue(Version), RootPath, MetadataPath, If(Neo, "NeoForge", "Forge"), Enabled, Nested)
            Item.IconPath = ReadIconPath(New JValue(Scalar(Section, "logoFile", Scalar(GlobalSection, "logoFile"))))
            If Scalar(GlobalSection, "clientSideOnly") = "true" Then Item.Side = "client"
            Items.Add(Item)
        Next
        For Each Section In Sections.Where(Function(Entry) Entry.Name.StartsWith("dependencies.", StringComparison.Ordinal))
            Dim OwnerId = Section.Name.Substring("dependencies.".Length), Item = Items.FirstOrDefault(Function(Entry) Entry.Id = OwnerId)
            If Item Is Nothing Then
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：依赖声明所属 ID 不存在：" & OwnerId)
                Continue For
            End If
            Dim Mandatory = Scalar(Section, "mandatory")
            Dim KindText = Scalar(Section, "type", If(Neo AndAlso Mandatory = "", "required", ""))
            Dim Kind As DependencyKind, Unknown = False
            Select Case KindText
                Case "required" : Kind = DependencyKind.Required
                Case "optional" : Kind = DependencyKind.Optional
                Case "incompatible" : Kind = DependencyKind.Incompatible
                Case "discouraged" : Kind = DependencyKind.Conflict
                Case Else
                    If Mandatory = "true" Then
                        Kind = DependencyKind.Required
                    ElseIf Mandatory = "false" Then
                        Kind = DependencyKind.Optional
                    Else
                        Kind = DependencyKind.Required
                        Unknown = True
                    End If
            End Select
            Dim Id = Scalar(Section, "modId")
            Item.Dependencies.Add(New Dependency With {.Id = CanonicalId(Id), .Versions = New JValue(Scalar(Section, "versionRange", "*")),
                .Syntax = "maven", .Kind = Kind, .Side = ReadSide(New JValue(Scalar(Section, "side", "*"))), .Reason = Scalar(Section, "reason"),
                .EnforceWhenPresent = Kind = DependencyKind.Optional, .IsUnknown = Unknown OrElse Id = ""})
        Next
        For Each Item In Items
            Dim Features = Sections.FirstOrDefault(Function(Entry) Entry.Name = "features." & Item.Id)
            If Features IsNot Nothing AndAlso Features.Values.ContainsKey("java_version") Then
                Item.Dependencies.Add(New Dependency With {.Id = "java", .Versions = New JValue(Scalar(Features, "java_version")), .Syntax = "maven", .Kind = DependencyKind.Required})
            End If
            'Forge FML uses the Forge major version; NeoForge's language loader version is a separate numbering scheme.
            Dim Language = Scalar(GlobalSection, "modLoader", If(Neo, "javafml", "")), Range = Scalar(GlobalSection, "loaderVersion")
            If {"javafml", "lowcodefml"}.Contains(Language) AndAlso Range <> "" Then
                Item.Dependencies.Add(New Dependency With {.Id = Language, .Versions = New JValue(Range), .Syntax = "maven", .Kind = DependencyKind.Required, .Reason = "FML 语言加载器版本"})
            End If
        Next
    End Sub
    Friend Shared Function IsLegacyForge(Context As InspectionContext) As Boolean
        If Context Is Nothing OrElse Not Context.LoaderVersions.ContainsKey("forge") Then Return False
        Return Regex.IsMatch(If(Context.MinecraftVersion, ""), "^1\.(?:[0-9]|1[0-2])(?:\.|$)") OrElse
            Regex.IsMatch(If(Context.LoaderVersions("forge"), ""), "^(?:[1-9]|1[0-4])\.")
    End Function

    Private Shared Function NeoModernMetadata(Context As InspectionContext) As Boolean?
        Dim Version As System.Version = Nothing
        If Not Context.LoaderVersions.ContainsKey("neoforge") OrElse Not System.Version.TryParse(Context.MinecraftVersion, Version) Then Return Nothing
        Return Version >= New System.Version(1, 20, 5)
    End Function
    Private Shared Function LoaderAccepts(Metadata As String, Context As InspectionContext, Optional DeclaredPlatform As String = "") As Boolean
        If Not Context.LoaderKnown OrElse Context.LoaderVersions.Count = 0 Then Return True
        Dim Loaders = Context.LoaderVersions
        Select Case Metadata
            Case "fabric.mod.json" : Return Loaders.ContainsKey("fabricloader") OrElse Loaders.ContainsKey("quilt_loader")
            Case "quilt.mod.json" : Return Loaders.ContainsKey("quilt_loader")
            Case "META-INF/neoforge.mods.toml" : Return Loaders.ContainsKey("neoforge") AndAlso NeoModernMetadata(Context).GetValueOrDefault(True)
            Case "META-INF/mods.toml"
                If DeclaredPlatform = "neoforge" Then Return Loaders.ContainsKey("neoforge") AndAlso Not NeoModernMetadata(Context).GetValueOrDefault(False)
                If DeclaredPlatform = "forge" Then Return (Loaders.ContainsKey("forge") AndAlso Not IsLegacyForge(Context)) OrElse
                    (Loaders.ContainsKey("neoforge") AndAlso Context.MinecraftVersion = "1.20.1")
                Return (Loaders.ContainsKey("forge") AndAlso Not IsLegacyForge(Context)) OrElse (Loaders.ContainsKey("neoforge") AndAlso Not NeoModernMetadata(Context).GetValueOrDefault(False))
            Case "legacy" : Return IsLegacyForge(Context)
            Case "litemod.json" : Return Loaders.ContainsKey("liteloader")
            Case Else : Return True
        End Select
    End Function

    Private Shared Function LoaderDisplay(Context As InspectionContext) As String
        If Context.LoaderVersions.ContainsKey("quilt_loader") Then Return "Quilt"
        If Context.LoaderVersions.ContainsKey("fabricloader") Then Return "Fabric"
        If Context.LoaderVersions.ContainsKey("neoforge") Then Return "NeoForge"
        If Context.LoaderVersions.ContainsKey("forge") Then Return If(IsLegacyForge(Context), "Forge（旧版）", "Forge")
        If Context.LoaderVersions.ContainsKey("liteloader") Then Return "LiteLoader"
        Return "当前加载器"
    End Function
    Friend Shared Function RuntimeOwnerId(Id As String, Context As InspectionContext) As String
        Select Case If(Id, "")
            Case "javafml", "lowcodefml", "fml", "FML"
                Return If(Context.LoaderVersions.ContainsKey("neoforge"), "neoforge", "forge")
            Case "forge", "neoforge", "fabricloader", "quilt_loader", "liteloader", "minecraft", "java" : Return Id
            Case Else : Return ""
        End Select
    End Function
    Friend Shared Function IsInternalLoaderId(Id As String) As Boolean
        Return {"javafml", "lowcodefml", "fml", "FML"}.Contains(If(Id, ""))
    End Function
    Friend Shared Function RuntimeDisplayName(Id As String) As String
        Select Case Id
            Case "forge" : Return "Forge"
            Case "neoforge" : Return "NeoForge"
            Case "fabricloader" : Return "Fabric Loader"
            Case "quilt_loader" : Return "Quilt Loader"
            Case "liteloader" : Return "LiteLoader"
            Case "minecraft" : Return "Minecraft"
            Case "java" : Return "Java"
            Case Else : Return Id
        End Select
    End Function
    Friend Shared Function RequirementDisplay(Requirement As Dependency, Context As InspectionContext) As String
        If Requirement.Children.Count > 0 Then Return "(" & String.Join(If(Requirement.Join = "all", " 且 ", " 或 "), Requirement.Children.Select(Function(Child) RequirementDisplay(Child, Context))) & ")"
        Dim Owner = RuntimeOwnerId(Requirement.Id, Context)
        If Owner = "" Then Return Requirement.Display
        If IsInternalLoaderId(Requirement.Id) Then Return RuntimeDisplayName(Owner) & " 内置支持"
        Return RuntimeDisplayName(Owner) & Requirement.Display.Substring(If(Requirement.Id, "").Length)
    End Function

    Private Class LegacyCacheEntry
        Public Mods As List(Of ModInfo)
        Public Notices As List(Of String)
    End Class
    Private Shared ReadOnly LegacyCache As New System.Collections.Concurrent.ConcurrentDictionary(Of String, LegacyCacheEntry)(StringComparer.OrdinalIgnoreCase)

    Private Shared Sub ReadLegacyArchive(Archive As ZipArchive, RootPath As String, NestedPath As String, Enabled As Boolean,
                                         Nested As Boolean, Depth As Integer, Report As InspectionReport, Budget As ReadBudget, Token As CancellationToken)
        'Forge 1.12 discovers class-level @Mod annotations, not modern TOML or every mcmod.info entry.
        'Rules: https://github.com/MinecraftForge/MinecraftForge/blob/1.12.x/src/main/java/net/minecraftforge/fml/common/FMLModContainer.java
        'Embedded dependencies: https://docs.minecraftforge.net/en/1.12.x/gettingstarted/dependencymanagement/
        Dim CacheKey As String = Nothing, BeforeMods = Report.Mods.Count, BeforeNotices = Report.Notices.Count
        If Not Nested Then
            Dim SourceInfo = New FileInfo(RootPath)
            CacheKey = FilePathKey(RootPath) & "|" & SourceInfo.Length & "|" & SourceInfo.LastWriteTimeUtc.Ticks & "|" & SourceInfo.CreationTimeUtc.Ticks & "|" & Enabled & "|" & Report.Context.MinecraftVersion & "|" &
                String.Join(";", Report.Context.LoaderVersions.OrderBy(Function(Pair) Pair.Key).Select(Function(Pair) Pair.Key & "=" & Pair.Value))
            Dim Saved As LegacyCacheEntry = Nothing
            If Not Budget.ForceFresh AndAlso LegacyCache.TryGetValue(CacheKey, Saved) Then
                Report.Mods.AddRange(Saved.Mods.Select(Function(Item) Item.CopyWithEnabled(Enabled)))
                For Each Notice In Saved.Notices
                    Incomplete(Report, Notice)
                Next
                Return
            End If
        End If
        Dim Descriptions As New List(Of JObject), Info = Archive.GetEntry("mcmod.info")
        If Info IsNot Nothing Then
            Try
                Dim Json = JToken.Parse(ReadText(Info))
                Descriptions.AddRange(ArrayValues(If(TypeOf Json Is JArray, Json, Json("modList"))).OfType(Of JObject)())
            Catch ex As Exception When TypeOf ex Is JsonException OrElse TypeOf ex Is InvalidDataException
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：无法完整读取 mcmod.info，继续检查 @Mod 声明。")
            End Try
        End If
        Dim Items As New List(Of ModInfo), Total As Long, Count As Integer
        Dim Manifest = Archive.GetEntry("META-INF/MANIFEST.MF"), Headers As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        If Manifest IsNot Nothing Then
            For Each Line In Regex.Replace(ReadText(Manifest), "\r*\n ", "").Split(ChrW(10))
                If String.IsNullOrWhiteSpace(Line) Then Exit For
                Dim Colon = Line.IndexOf(":"c)
                If Colon > 0 Then Headers(Line.Substring(0, Colon)) = Line.Substring(Colon + 1).Trim()
            Next
        End If
        Dim PluginName As String = Nothing
        If Headers.TryGetValue("FMLCorePlugin", PluginName) Then
            Try
                Dim Plugin = Archive.GetEntry(PluginName.Replace("."c, "/"c) & ".class"), Core As JObject = Nothing
                If Plugin IsNot Nothing AndAlso Plugin.Length <= MaxMetadataBytes Then
                    Using Stream = Plugin.Open(), ClassReader As New LegacyClassReader(Stream, Plugin.Length)
                        Core = ClassReader.ReadMod(1)
                    End Using
                End If
                Dim PluginCore = Core, ContainerName = Text(Core?("container"))
                If ContainerName <> "" Then
                    Dim Container = Archive.GetEntry(ContainerName.Replace("."c, "/"c) & ".class")
                    If Container IsNot Nothing AndAlso Container.Length <= MaxMetadataBytes Then
                        Using Stream = Container.Open(), ClassReader As New LegacyClassReader(Stream, Container.Length)
                            Core = ClassReader.ReadMod(2)
                        End Using
                        If Text(Core?("modId")) <> "" Then
                            Dim Item = NewMod(Core("modId"), Core("name"), Core("version"), RootPath, NestedPath & Container.FullName, "Forge (旧版核心插件)", Enabled, Nested)
                            Item.IsRuntimeCandidate = Text(PluginCore?("uncertain")) = "True"
                            Items.Add(Item)
                            If Item.IsRuntimeCandidate AndAlso Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：核心插件按运行环境决定是否注册 " & Item.Id & "，未将其作为可自动安装的缺失依赖。")
                        End If
                    End If
                End If
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is FormatException
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：核心插件的运行时模组信息无法静态确认。")
            End Try
        End If
        'Exact Forge ITypeDiscoverer.classFile rule: do not discover Scala Foo$.class, whitespace copies or __MACOSX files.
        For Each Entry In Archive.Entries.Where(Function(Value) Not Value.FullName.StartsWith("__MACOSX", StringComparison.Ordinal) AndAlso
            Regex.IsMatch(Value.FullName, "\A[^\s\$]+(\$[^\s]+)?\.class\z"))
            Token.ThrowIfCancellationRequested()
            Count += 1
            If Count > 50000 OrElse Entry.Length > 4L * 1024 * 1024 OrElse Entry.Length > 128L * 1024 * 1024 - Total Then
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：旧版类声明扫描达到读取上限。")
                Exit For
            End If
            Total += Entry.Length
            Try
                Dim Annotation As JObject
                Using Stream = Entry.Open(), ClassReader As New LegacyClassReader(Stream, Entry.Length)
                    Annotation = ClassReader.ReadMod()
                End Using
                If Annotation Is Nothing Then Continue For
                Dim Id = Text(Annotation("modid"))
                Dim Description = Descriptions.FirstOrDefault(Function(Value) Text(Value("modid")) = Id)
                Dim MetadataOverrides = Annotation("useMetadata") Is Nothing OrElse Text(Annotation("useMetadata")).Equals("true", StringComparison.OrdinalIgnoreCase)
                Dim Name = Text(Annotation("name")), Version = Text(Annotation("version"))
                If Description IsNot Nothing AndAlso (MetadataOverrides OrElse Name = "") Then Name = Text(Description("name"))
                If Version = "" Then
                    Dim Properties = Archive.GetEntry("version.properties")
                    If Properties IsNot Nothing Then
                        Dim Match = Regex.Match(ReadText(Properties), "(?m)^\s*" & Regex.Escape(Id) & "\.version\s*[=:]\s*([^\r\n\\]+)\s*$")
                        If Match.Success Then Version = Match.Groups(1).Value.Trim()
                    End If
                    If Version = "" AndAlso Description IsNot Nothing Then Version = Text(Description("version"))
                End If
                If Version.Contains("${") Then Version = ""
                Dim Item = NewMod(New JValue(Id), New JValue(Name), New JValue(Version), RootPath, NestedPath & Entry.FullName, "Forge (旧版)", Enabled, Nested)
                If Description IsNot Nothing Then Item.IconPath = ReadIconPath(Description("logoFile"))
                If Text(Annotation("clientSideOnly")).Equals("true", StringComparison.OrdinalIgnoreCase) Then Item.Side = "client"
                If Text(Annotation("serverSideOnly")).Equals("true", StringComparison.OrdinalIgnoreCase) Then Item.Side = "server"
                If Description IsNot Nothing AndAlso MetadataOverrides AndAlso Text(Description("useDependencyInformation")).Equals("true", StringComparison.OrdinalIgnoreCase) Then
                    For Each Required In ArrayValues(Description("requiredMods"))
                        AddLegacyDependency(Item, Text(Required), DependencyKind.Required)
                    Next
                    For Each OptionalValue In ArrayValues(Description("dependencies")).Concat(ArrayValues(Description("dependants")))
                        AddLegacyDependency(Item, Text(OptionalValue), DependencyKind.Optional)
                    Next
                Else
                    For Each Raw In Text(Annotation("dependencies")).Split(";"c).Where(Function(Value) Not String.IsNullOrWhiteSpace(Value))
                        Dim Parts = Raw.Split(":"c), Instructions = Parts(0).Trim().Split("-"c)
                        If Parts.Length <> 2 OrElse Instructions.Any(Function(Value) Not {"required", "before", "after", "client", "server"}.Contains(Value)) Then
                            If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：无法完整解析旧版依赖声明。")
                            Continue For
                        End If
                        Dim Side = If(Instructions.Contains("client"), "client", If(Instructions.Contains("server"), "server", "*"))
                        AddLegacyDependency(Item, Parts(1), If(Instructions.Contains("required"), DependencyKind.Required, DependencyKind.Optional), Side)
                    Next
                End If
                Dim MinecraftRange = Text(Annotation("acceptedMinecraftVersions"))
                If {"[1.12]", "[1.12.1]", "[1.12,1.12.1]"}.Contains(MinecraftRange) Then MinecraftRange = "[1.12,1.12.2]"
                If MinecraftRange <> "" Then AddLegacyDependency(Item, "minecraft@" & MinecraftRange, DependencyKind.Required)
                Items.Add(Item)
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is FormatException
                If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：部分旧版类声明无法读取。")
            End Try
        Next
        If Items.Count = 0 Then
            'Core plugins may construct containers at runtime. Keep descriptions for detail UI only, never as duplicate IDs.
            For Each Description In Descriptions
                Try
                    Dim Item = NewMod(Description("modid"), Description("name"), Description("version"), RootPath, NestedPath & "mcmod.info", "Forge (旧版)", Enabled, Nested)
                    Item.IsMetadataOnly = True : Item.IconPath = ReadIconPath(Description("logoFile")) : Items.Add(Item)
                Catch ex As FormatException
                    'Unexpanded templates do not establish a loader ID.
                End Try
            Next
            If Enabled Then Incomplete(Report, Path.GetFileName(RootPath) & "：未找到静态 @Mod 声明，可能通过核心插件在运行时注册。")
        End If
        Report.Mods.AddRange(Items)
        Dim Contained As String = Nothing
        If Headers.TryGetValue("ContainedDeps", Contained) Then
            Dim Paths = Contained.Split({" "c}, StringSplitOptions.RemoveEmptyEntries).Select(Function(Name)
                Return If(Archive.GetEntry("META-INF/libraries/" & Name) IsNot Nothing, "META-INF/libraries/" & Name, Name)
            End Function)
            ReadNestedArchives(Archive, Paths, RootPath, NestedPath, Enabled, Depth, Report, Budget, Token)
        End If
        If CacheKey IsNot Nothing Then
            If LegacyCache.Count >= 1024 Then LegacyCache.Clear()
            LegacyCache(CacheKey) = New LegacyCacheEntry With {.Mods = Report.Mods.Skip(BeforeMods).Select(Function(Item) Item.CopyWithEnabled(Enabled)).ToList(),
                .Notices = Report.Notices.Skip(BeforeNotices).ToList()}
        End If
    End Sub

    Private Shared Sub AddLegacyDependency(Item As ModInfo, Raw As String, Kind As DependencyKind, Optional Side As String = "*")
        Raw = Raw.Trim()
        Dim At = Raw.IndexOf("@"c), Id = If(At < 0, Raw, Raw.Substring(0, At)).Trim()
        If Id = "*" Then Return 'A load-order rule, not a dependency on a mod called *.
        Item.Dependencies.Add(New Dependency With {.Id = Id, .Versions = New JValue(If(At < 0, "*", Raw.Substring(At + 1).Trim())),
            .Kind = Kind, .Syntax = "maven", .Side = Side, .EnforceWhenPresent = Kind = DependencyKind.Optional,
            .IsUnknown = Not Regex.IsMatch(Id, "^[A-Za-z0-9_.-]+$")})
    End Sub

    'Reads only class metadata; never loads, invokes or rewrites mod code. Limits also apply to malformed class attributes.
    Private NotInheritable Class LegacyClassReader
        Implements IDisposable
        Private ReadOnly Reader As BinaryReader
        Private Remaining As Long
        Private Pool As Object()
        Private Shared ReadOnly StrictUtf8 As New UTF8Encoding(False, True)
        Private Class PoolReference
            Public Kind As Integer, First As Integer, Second As Integer
            Public Sub New(Kind As Integer, First As Integer, Optional Second As Integer = 0)
                Me.Kind = Kind : Me.First = First : Me.Second = Second
            End Sub
        End Class
        Public Sub New(Stream As Stream, Length As Long)
            Reader = New BinaryReader(New BufferedStream(Stream, 8192), Encoding.UTF8, False) : Remaining = Length
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            Reader.Dispose()
        End Sub
        Private Function Bytes(Count As Integer) As Byte()
            If Count < 0 OrElse Count > Remaining Then Throw New InvalidDataException("无效的类声明长度")
            Dim Value = Reader.ReadBytes(Count)
            If Value.Length <> Count Then Throw New EndOfStreamException()
            Remaining -= Count : Return Value
        End Function
        Private Function U1() As Integer
            If Remaining < 1 Then Throw New EndOfStreamException()
            Remaining -= 1 : Return Reader.ReadByte()
        End Function
        Private Function U2() As Integer
            Return U1() * 256 + U1()
        End Function
        Private Function U4() As Long
            Return CLng(U2()) * 65536 + U2()
        End Function
        Private Sub Skip(Count As Long)
            If Count < 0 OrElse Count > Remaining Then Throw New InvalidDataException("无效的类声明长度")
            While Count > 0
                Dim Part = CInt(Math.Min(8192, Count)) : Bytes(Part) : Count -= Part
            End While
        End Sub
        Private Function Constant(Index As Integer) As Object
            If Index <= 0 OrElse Index >= Pool.Length Then Throw New InvalidDataException("无效的常量索引")
            Return Pool(Index)
        End Function
        Private Function Utf(Index As Integer) As String
            Dim Value = TryCast(Constant(Index), String)
            If Value Is Nothing Then Throw New InvalidDataException("无效的文本常量")
            Return Value
        End Function
        Private Shared Function ModifiedUtf8(Value As Byte()) As String
            Try
                Return StrictUtf8.GetString(Value)
            Catch ex As DecoderFallbackException
                'Java encodes NUL and UTF-16 surrogates differently from standard UTF-8.
            End Try
            Dim Result As New StringBuilder(), Index As Integer
            While Index < Value.Length
                Dim First = CInt(Value(Index)), Code As Integer, Count As Integer
                If First < 128 Then
                    Code = First : Count = 1
                ElseIf (First And &HE0) = &HC0 Then
                    Code = First And &H1F : Count = 2
                ElseIf (First And &HF0) = &HE0 Then
                    Code = First And &HF : Count = 3
                Else
                    Throw New InvalidDataException("无效的 Java UTF-8 常量")
                End If
                If Index + Count > Value.Length Then Throw New InvalidDataException("截断的 Java UTF-8 常量")
                For Offset = 1 To Count - 1
                    Dim NextByte = Value(Index + Offset)
                    If (NextByte And &HC0) <> &H80 Then Throw New InvalidDataException("无效的 Java UTF-8 常量")
                    Code = Code * 64 + (NextByte And &H3F)
                Next
                Result.Append(ChrW(Code)) : Index += Count
            End While
            Return Result.ToString()
        End Function
        Private Function ReferencedName(Index As Integer) As String
            Dim Value = TryCast(Constant(Index), PoolReference)
            If Value Is Nothing OrElse Value.Kind <> 7 Then Return ""
            Return Utf(Value.First)
        End Function
        Public Function ReadMod(Optional CoreMode As Integer = 0) As JObject
            If U4() <> &HCAFEBABEL Then Throw New InvalidDataException("无效的 Java 类文件")
            Skip(4) : ReDim Pool(U2() - 1)
            Dim Index = 1
            While Index < Pool.Length
                Dim Kind = U1()
                Select Case Kind
                    Case 1 : Pool(Index) = ModifiedUtf8(Bytes(U2()))
                    Case 3 : Pool(Index) = U4()
                    Case 4 : Skip(4)
                    Case 5, 6 : Skip(8) : Index += 1
                    Case 7, 8, 16, 19, 20 : Pool(Index) = New PoolReference(Kind, U2())
                    Case 9, 10, 11, 12, 17, 18 : Pool(Index) = New PoolReference(Kind, U2(), U2())
                    Case 15 : Skip(3)
                    Case Else : Throw New InvalidDataException("未知的 Java 常量类型")
                End Select
                Index += 1
            End While
            If CoreMode = 0 AndAlso Not Pool.Any(Function(Value) Equals(Value, "Lnet/minecraftforge/fml/common/Mod;") OrElse Equals(Value, "Lcpw/mods/fml/common/Mod;")) Then Return Nothing
            Skip(4)
            Dim Superclass = ReferencedName(U2()), Core As New JObject()
            If CoreMode = 2 AndAlso Not {"net/minecraftforge/fml/common/DummyModContainer", "cpw/mods/fml/common/DummyModContainer"}.Contains(Superclass) Then Return Nothing
            Skip(CLng(U2()) * 2)
            For MemberKind = 1 To 2
                Dim Members = U2()
                For Member = 1 To Members
                    Skip(2)
                    Dim MemberName = Utf(U2()), Descriptor = Utf(U2())
                    Dim Attributes = U2()
                    For Attribute = 1 To Attributes
                        Dim Name = Utf(U2()), Length = U4(), Before = Remaining
                        If MemberKind = 2 AndAlso Name = "Code" AndAlso ((CoreMode = 1 AndAlso MemberName = "getModContainerClass" AndAlso Descriptor = "()Ljava/lang/String;") OrElse
                            (CoreMode = 2 AndAlso MemberName = "<init>" AndAlso Descriptor = "()V")) Then
                            Skip(4)
                            Dim CodeLength = U4()
                            If CodeLength > 65535 Then Throw New InvalidDataException("类方法超过读取上限")
                            Core = ReadCoreCode(Bytes(CInt(CodeLength)), CoreMode)
                        End If
                        Skip(Length - (Before - Remaining))
                    Next
                Next
            Next
            If CoreMode <> 0 Then Return Core
            Dim ClassAttributes = U2(), Result As JObject = Nothing
            For Attribute = 1 To ClassAttributes
                Dim Name = Utf(U2()), Length = U4(), Before = Remaining
                If Name = "RuntimeVisibleAnnotations" Then
                    Dim Count = U2()
                    For Annotation = 1 To Count
                        Dim Kind As String = Nothing, Value = ReadAnnotation(0, Kind)
                        If Kind = "Lnet/minecraftforge/fml/common/Mod;" OrElse Kind = "Lcpw/mods/fml/common/Mod;" Then Result = Value
                    Next
                End If
                Skip(Length - (Before - Remaining))
            Next
            Return Result
        End Function
        Private Function ReadCoreCode(Code As Byte(), Mode As Integer) As JObject
            Dim Result As New JObject(), Index As Integer, PreviousString As String = Nothing, PreviousClass As String = Nothing, Conditional = False
            While Index < Code.Length
                Dim Op = CInt(Code(Index)), Operands As Integer
                Select Case Op
                    Case 0 To 15, 26 To 53, 59 To 131, 133 To 152, 172 To 177, 190, 191, 194, 195 : Operands = 0
                    Case 16, 18, 21 To 25, 54 To 58, 188 : Operands = 1
                    Case 17, 19, 20, 132, 178 To 184, 187, 189, 192, 193 : Operands = 2
                    Case 185, 186 : Operands = 4
                    Case 197 : Operands = 3
                    Case 153 To 168, 198, 199
                        If Mode <> 1 Then Return New JObject()
                        Conditional = True : Operands = 2
                    Case 200, 201
                        If Mode <> 1 Then Return New JObject()
                        Conditional = True : Operands = 4
                    Case Else : Return New JObject() 'Branches, switches and dynamic/wide code are deliberately not inferred.
                End Select
                If Index + Operands >= Code.Length Then Throw New InvalidDataException("截断的类方法")
                Dim Argument = If(Operands = 0, 0, If(Operands = 1, CInt(Code(Index + 1)), CInt(Code(Index + 1)) * 256 + Code(Index + 2)))
                Dim Literal As String = Nothing, ClassLiteral As String = Nothing
                If Op = 18 OrElse Op = 19 Then
                    Dim Reference = TryCast(Constant(Argument), PoolReference)
                    If Reference IsNot Nothing AndAlso Reference.Kind = 8 Then Literal = Utf(Reference.First)
                    If Reference IsNot Nothing AndAlso Reference.Kind = 7 Then ClassLiteral = Utf(Reference.First).Replace("/"c, "."c)
                End If
                If Mode = 1 AndAlso Op = 182 AndAlso PreviousClass IsNot Nothing Then
                    Dim Method = TryCast(Constant(Argument), PoolReference)
                    If Method IsNot Nothing AndAlso Method.Kind = 10 AndAlso ReferencedName(Method.First) = "java/lang/Class" Then
                        Dim NameType = TryCast(Constant(Method.Second), PoolReference)
                        If NameType IsNot Nothing AndAlso NameType.Kind = 12 AndAlso Utf(NameType.First) = "getName" AndAlso Utf(NameType.Second) = "()Ljava/lang/String;" Then Literal = PreviousClass
                    End If
                End If
                If Mode = 1 AndAlso Op = 176 AndAlso PreviousString IsNot Nothing Then
                    Result("container") = PreviousString : Result("uncertain") = Conditional : Return Result
                End If
                If Mode = 2 AndAlso Op = 181 Then
                    Dim Field = TryCast(Constant(Argument), PoolReference)
                    If Field IsNot Nothing AndAlso Field.Kind = 9 AndAlso {"net/minecraftforge/fml/common/ModMetadata", "cpw/mods/fml/common/ModMetadata"}.Contains(ReferencedName(Field.First)) Then
                        Dim NameType = TryCast(Constant(Field.Second), PoolReference)
                        If NameType IsNot Nothing AndAlso NameType.Kind = 12 AndAlso Utf(NameType.Second) = "Ljava/lang/String;" Then
                            Dim Name = Utf(NameType.First)
                            If {"modId", "name", "version"}.Contains(Name) Then Result(Name) = If(PreviousString Is Nothing, JValue.CreateNull(), New JValue(PreviousString))
                        End If
                    End If
                End If
                PreviousString = Literal : PreviousClass = ClassLiteral : Index += 1 + Operands
            End While
            Return If(Mode = 2, Result, New JObject())
        End Function
        Private Function ReadAnnotation(Depth As Integer, ByRef Kind As String) As JObject
            If Depth > 16 Then Throw New InvalidDataException("Java 注解嵌套过深")
            Kind = Utf(U2())
            Dim Result As New JObject(), Count = U2()
            For Pair = 1 To Count
                Dim Name = Utf(U2()) : Result(Name) = ReadElement(Depth + 1)
            Next
            Return Result
        End Function
        Private Function ReadElement(Depth As Integer) As JToken
            If Depth > 16 Then Throw New InvalidDataException("Java 注解嵌套过深")
            Select Case ChrW(U1())
                Case "s"c : Return New JValue(Utf(U2()))
                Case "Z"c : Return New JValue(Convert.ToInt64(Constant(U2()), CultureInfo.InvariantCulture) <> 0)
                Case "B"c, "C"c, "D"c, "F"c, "I"c, "J"c, "S"c : Skip(2) : Return JValue.CreateNull()
                Case "e"c : Skip(4) : Return JValue.CreateNull()
                Case "c"c : Skip(2) : Return JValue.CreateNull()
                Case "@"c
                    Dim Kind As String = Nothing : Return ReadAnnotation(Depth, Kind)
                Case "["c
                    Dim Result As New JArray(), Count = U2()
                    For Index = 1 To Count
                        Result.Add(ReadElement(Depth + 1))
                    Next
                    Return Result
                Case Else : Throw New InvalidDataException("未知的 Java 注解值")
            End Select
        End Function
    End Class

    Private Shared Function CanonicalId(Id As String) As String
        'Mod IDs are exact identifiers. In particular, "fabric" is the old Fabric API ID, not Fabric Loader.
        Return If(Id, "")
    End Function
    Private Shared Function CanonicalLoaderId(Id As String) As String
        Select Case If(Id, "").ToLowerInvariant()
            Case "fabric", "fabric-loader", "fabric_loader", "fabricloader" : Return "fabricloader"
            Case "quilt", "quilt-loader", "quiltloader", "quilt_loader" : Return "quilt_loader"
            Case "liteloader" : Return "liteloader"
            Case "neoforge", "forge", "minecraft", "java" : Return Id.ToLowerInvariant()
            Case Else : Return If(Id, "")
        End Select
    End Function
    Private Shared Function CreateBuiltins(Context As InspectionContext) As List(Of ModInfo)
        Dim Values As New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"minecraft", Context.MinecraftVersion}, {"java", NormalizeJava(Context.JavaVersion)}}
        If Not Context.LoaderKnown AndAlso Context.LoaderVersions.Count = 0 Then
            For Each Id In {"forge", "neoforge", "fabricloader", "quilt_loader", "liteloader"}
                Values(Id) = Nothing
            Next
        End If
        For Each Pair In Context.LoaderVersions
            Values(CanonicalLoaderId(Pair.Key)) = Pair.Value
        Next
        If Values.ContainsKey("forge") OrElse Values.ContainsKey("neoforge") Then
            Values("fml") = Nothing
            Values("javafml") = Nothing
            Values("lowcodefml") = Nothing
            Dim Forge As String = Nothing
            If Values.TryGetValue("forge", Forge) AndAlso Regex.IsMatch(If(Forge, ""), "^\d+(?:\.\d+)*$") Then
                Values("javafml") = Forge.Split("."c)(0)
                Values("lowcodefml") = Forge.Split("."c)(0)
            End If
        End If
        If IsLegacyForge(Context) Then Values("FML") = Nothing
        For Each Pair In Context.AdditionalBuiltins
            Values(CanonicalId(Pair.Key)) = Pair.Value
        Next
        Return Values.Select(Function(Pair) New ModInfo With {.Id = Pair.Key, .Name = Pair.Key, .Version = Pair.Value, .IsBuiltin = True, .Enabled = True}).ToList()
    End Function
    Private Shared Function NormalizeJava(Value As String) As String
        If String.IsNullOrEmpty(Value) Then Return Value
        If Value.StartsWith("1.", StringComparison.Ordinal) Then Value = Value.Substring(2)
        'Java 8 update syntax is numeric; preserve unknown suffixes instead of guessing EA/release ordering.
        Return Regex.Replace(Value, "^(\d+(?:\.\d+)*)_(\d+)$", "$1.$2")
    End Function
    Friend Shared Function Identities(Item As ModInfo) As Dictionary(Of String, String)
        Dim Result As New Dictionary(Of String, String)(Item.Provides, StringComparer.Ordinal)
        If Item.IsMetadataOnly OrElse Item.IncompatibleLoader Then Return Result
        Result(Item.Id) = Item.Version
        If Not String.IsNullOrEmpty(Item.Group) Then Result(Item.Group & ":" & Item.Id) = Item.Version
        Return Result
    End Function
    Private Shared Function ActiveMods(Report As InspectionReport) As List(Of ModInfo)
        Return Report.Mods.Where(Function(Item) Item.Enabled AndAlso Not Item.IsMetadataOnly AndAlso Not Item.IncompatibleLoader AndAlso SideApplies(Item.Side, Report.Context)).ToList()
    End Function
    Friend Shared Function FileSatisfiesDependency(Report As InspectionReport, FullPath As String, Requirement As Dependency) As Boolean
        Dim Modules = Report.Mods.Where(Function(Item) SameFilePath(Item.RootPath, FullPath) AndAlso SideApplies(Item.Side, Report.Context))
        Return Resolve(Requirement, ProviderMap(Modules), Report.Context).State = MatchState.Satisfied
    End Function

    Friend Shared Function WithEnabledFiles(Report As InspectionReport, Paths As IEnumerable(Of String), Token As CancellationToken) As InspectionReport
        Dim Enabled As New HashSet(Of String)(Paths.Select(AddressOf FilePathKey), StringComparer.OrdinalIgnoreCase)
        Dim Result As New InspectionReport With {.Context = Report.Context, .Builtins = Report.Builtins, .MetadataIncomplete = Report.MetadataIncomplete}
        Result.Notices.AddRange(Report.Notices)
        Result.Mods.AddRange(Report.Mods.Select(Function(Item) Item.CopyWithEnabled(Item.Enabled OrElse Enabled.Contains(FilePathKey(Item.RootPath)))))
        BuildReport(Result, Token)
        Return Result
    End Function

    Friend Shared Function IsRequiredFileProvider(Report As InspectionReport, Edge As DependencyEdge, FullPath As String) As Boolean
        Return FileProviderRelation(Report, Edge, FullPath) = FileProviderKind.Required
    End Function

    Friend Shared Function FileProviderRelation(Report As InspectionReport, Edge As DependencyEdge, FullPath As String) As FileProviderKind
        If Not Edge.Evaluated OrElse Edge.ConditionBypassed OrElse SameFilePath(Edge.Owner.RootPath, FullPath) Then Return FileProviderKind.None
        Dim Providers = Edge.Providers.Where(Function(Item) Not Item.IsBuiltin).ToList()
        If Not Providers.Any(Function(Item) SameFilePath(Item.RootPath, FullPath)) OrElse FileSatisfiesDependency(Report, Edge.Owner.RootPath, Edge.Requirement) Then Return FileProviderKind.None
        If Providers.All(Function(Item) SameFilePath(Item.RootPath, FullPath)) Then Return FileProviderKind.Required
        Dim WithoutFile = ActiveMods(Report).Where(Function(Item) Not SameFilePath(Item.RootPath, FullPath)).Concat(Report.Builtins)
        If Edge.State = MatchState.Satisfied AndAlso Resolve(Edge.Requirement, ProviderMap(WithoutFile), Report.Context).State = MatchState.Unsatisfied Then Return FileProviderKind.Required
        If Edge.Requirement.Children.Count = 0 AndAlso Edge.State = MatchState.Satisfied AndAlso
            Resolve(Edge.Requirement, ProviderMap(Providers.Where(Function(Item) SameFilePath(Item.RootPath, FullPath))), Report.Context).State = MatchState.Unsatisfied Then Return FileProviderKind.None
        Return FileProviderKind.Possible
    End Function

    Private Shared Function ProviderMap(Mods As IEnumerable(Of ModInfo)) As Dictionary(Of String, List(Of ModInfo))
        Dim Result As New Dictionary(Of String, List(Of ModInfo))(StringComparer.Ordinal)
        For Each Item In Mods
            For Each Id In Identities(Item).Keys
                If Not Result.ContainsKey(Id) Then Result(Id) = New List(Of ModInfo)
                Result(Id).Add(Item)
            Next
        Next
        'A top-level Fabric/Quilt mod fixes that ID's version; an embedded copy is only a selectable fallback.
        For Each Id In Result.Keys.ToArray()
            Dim Direct = Result(Id).Where(Function(Item) Not Item.IsNested).ToList()
            If Direct.Count > 0 Then Result(Id) = Direct
        Next
        Return Result
    End Function

    Private Class Resolution
        Public State As MatchState
        Public Missing As Boolean
        Public Providers As New List(Of ModInfo)
    End Class
    Private Shared Function Resolve(Requirement As Dependency, Providers As Dictionary(Of String, List(Of ModInfo)), Context As InspectionContext,
                                    Optional Depth As Integer = 0) As Resolution
        If Depth > 32 OrElse Requirement.IsUnknown Then Return New Resolution With {.State = MatchState.Unknown}
        If Not SideApplies(Requirement.Side, Context) Then Return New Resolution With {.State = MatchState.Satisfied}
        Dim Result As New Resolution
        If Requirement.Children.Count > 0 Then
            Dim Children = Requirement.Children.Select(Function(Child) Resolve(Child, Providers, Context, Depth + 1)).ToList()
            Result.State = Combine(Children.Select(Function(Child) Child.State), Requirement.Join = "all")
            Result.Missing = Children.All(Function(Child) Child.Missing)
            Result.Providers = Children.SelectMany(Function(Child) Child.Providers).Distinct().ToList()
        Else
            Dim Candidates As List(Of ModInfo) = Nothing
            If Not Providers.TryGetValue(If(Requirement.Id, ""), Candidates) OrElse Candidates.Count = 0 Then
                Result.State = MatchState.Unsatisfied
                Result.Missing = True
            Else
                Result.Providers = Candidates.ToList()
                Result.State = Combine(Candidates.Select(Function(Item) If(Item.IsRuntimeCandidate, MatchState.Unknown, MatchVersions(Identities(Item)(Requirement.Id), Requirement.Versions, Requirement.Syntax))), False)
            End If
        End If
        If Requirement.Unless IsNot Nothing Then
            Dim UnlessResult = Resolve(Requirement.Unless, Providers, Context, Depth + 1)
            Dim Negative = Requirement.Kind = DependencyKind.Incompatible OrElse Requirement.Kind = DependencyKind.Conflict
            If UnlessResult.State = MatchState.Satisfied Then
                Result.State = If(Negative, MatchState.Unsatisfied, MatchState.Satisfied)
                Result.Missing = False
                Result.Providers = Result.Providers.Concat(UnlessResult.Providers).Distinct().ToList()
            ElseIf UnlessResult.State = MatchState.Unknown AndAlso ((Negative AndAlso Result.State = MatchState.Satisfied) OrElse (Not Negative AndAlso Result.State = MatchState.Unsatisfied)) Then
                Result.State = MatchState.Unknown
            End If
        End If
        If Requirement.Kind = DependencyKind.Optional AndAlso Result.Missing Then Result.State = MatchState.Satisfied
        Return Result
    End Function
    Private Shared Function Combine(States As IEnumerable(Of MatchState), All As Boolean) As MatchState
        Dim Values = States.ToArray()
        If Values.Length = 0 Then Return MatchState.Unknown
        If All Then
            If Values.Contains(MatchState.Unsatisfied) Then Return MatchState.Unsatisfied
            Return If(Values.Contains(MatchState.Unknown), MatchState.Unknown, MatchState.Satisfied)
        End If
        If Values.Contains(MatchState.Satisfied) Then Return MatchState.Satisfied
        Return If(Values.Contains(MatchState.Unknown), MatchState.Unknown, MatchState.Unsatisfied)
    End Function
    Friend Shared Sub BuildReport(Report As InspectionReport, Token As CancellationToken)
        For Each Group In Report.Mods.Where(Function(Item) Item.Enabled AndAlso Not Item.IsNested AndAlso Not Item.IsMetadataOnly AndAlso Item.IncompatibleLoader).
            GroupBy(Function(Item) FilePathKey(Item.RootPath), StringComparer.OrdinalIgnoreCase)
            Token.ThrowIfCancellationRequested()
            Dim Item = Group.First(), Expected = LoaderDisplay(Report.Context)
            Report.Issues.Add(New InspectionIssue With {.Kind = IssueKind.LoaderMismatch, .Owner = Item, .RelatedMods = Group.ToList(), .IsError = True,
                .Message = "文件：" & Path.GetFileName(Item.RootPath) & vbCrLf & "适用：" & Item.Format & "；当前：" & Expected & vbCrLf &
                If(String.IsNullOrEmpty(Item.LoaderMismatchDetail), "", Item.LoaderMismatchDetail & vbCrLf) &
                "处理：在 Mod 管理中禁用或移出此文件，换用支持 Minecraft " & Report.Context.MinecraftVersion & " 与 " & Expected &
                " 的版本；若要使用此模组，请另选匹配加载器的游戏实例。"})
        Next
        Dim Active = ActiveMods(Report), Map = ProviderMap(Active.Concat(Report.Builtins))
        For Each Group In Active.Where(Function(Item) Not Item.IsNested AndAlso Not Item.IsRuntimeCandidate).GroupBy(Function(Item) Item.Id, StringComparer.Ordinal).Where(Function(Items) Items.Count() > 1)
            Report.Issues.Add(New InspectionIssue With {.Kind = IssueKind.DuplicateId, .Owner = Group.First(), .RelatedMods = Group.ToList(), .IsError = True,
                .Message = "重复模组 ID " & Group.Key & "：" & String.Join("、", Group.Select(Function(Item) Path.GetFileName(Item.RootPath)))})
        Next
        Dim RequiredKeys As New HashSet(Of String)(Active.Where(Function(Item) Not Item.IsNested AndAlso Not Item.IsRuntimeCandidate).Select(Function(Item) Item.Key), StringComparer.Ordinal)
        'An embedded candidate is checked as required only when a required chain uniquely selects it.
        Dim Changed = True
        While Changed
            Changed = False
            For Each Item In Active.Where(Function(Entry) RequiredKeys.Contains(Entry.Key))
                Token.ThrowIfCancellationRequested()
                For Each Requirement In Item.Dependencies.Where(Function(Entry) Entry.Kind = DependencyKind.Required AndAlso SideApplies(Entry.Side, Report.Context))
                    Dim Value = Resolve(Requirement, Map, Report.Context)
                    Dim Candidates = Value.Providers.Where(Function(Entry) Entry.IsNested).ToList()
                    If Value.Providers.Count = 1 AndAlso Candidates.Count = 1 AndAlso RequiredKeys.Add(Candidates(0).Key) Then Changed = True
                Next
            Next
        End While
        For Each Item In Active
            Item.IsCandidate = Item.IsRuntimeCandidate OrElse (Item.IsNested AndAlso Not RequiredKeys.Contains(Item.Key))
        Next
        For Each Item In Active
            Token.ThrowIfCancellationRequested()
            For Each Requirement In Item.Dependencies
                Dim Value = Resolve(Requirement, Map, Report.Context)
                Dim Edge As New DependencyEdge With {.Owner = Item, .Requirement = Requirement, .State = Value.State, .Providers = Value.Providers,
                    .Evaluated = RequiredKeys.Contains(Item.Key) AndAlso SideApplies(Requirement.Side, Report.Context),
                    .ConditionBypassed = Requirement.Unless IsNot Nothing AndAlso Resolve(Requirement.Unless, Map, Report.Context).State = MatchState.Satisfied}
                Report.Edges.Add(Edge)
                For Each Provider In Value.Providers
                    If Not Report.ReverseDependencies.ContainsKey(Provider.Key) Then Report.ReverseDependencies(Provider.Key) = New List(Of DependencyEdge)
                    Report.ReverseDependencies(Provider.Key).Add(Edge)
                Next
                If Edge.Evaluated Then AddIssue(Report, Edge, Value)
            Next
        Next
        If Active.Any(Function(Item) Item.IsCandidate AndAlso Item.IsNested) Then Report.Notices.Add("内嵌模组的候选版本由加载器选择；未把未选中的候选自身依赖判为启动错误。")
    End Sub
    Private Shared Sub AddIssue(Report As InspectionReport, Edge As DependencyEdge, Value As Resolution)
        Dim Requirement = Edge.Requirement, Kind As IssueKind, IsError = False, Detail As String = Nothing
        Select Case Requirement.Kind
            Case DependencyKind.Required, DependencyKind.Optional
                If Value.State <> MatchState.Unsatisfied Then Return
                If Requirement.Kind = DependencyKind.Optional AndAlso Not Requirement.EnforceWhenPresent Then Return
                Kind = If(Value.Missing, IssueKind.MissingRequired, IssueKind.VersionMismatch)
                IsError = Not Report.MetadataIncomplete OrElse (Not Value.Missing AndAlso Value.Providers.All(Function(Item) Not Item.IsNested))
                Detail = If(Value.Missing, "缺少必需依赖：", "版本不匹配：") & Edge.Owner.Display & " 需要 " & RequirementDisplay(Requirement, Report.Context)
            Case DependencyKind.Incompatible, DependencyKind.Conflict
                If Value.State <> MatchState.Satisfied OrElse Value.Missing OrElse Not SideApplies(Requirement.Side, Report.Context) Then Return
                Kind = IssueKind.Incompatible
                IsError = Requirement.Kind = DependencyKind.Incompatible AndAlso Value.Providers.All(Function(Item) Not Item.IsCandidate)
                Detail = If(Value.Providers.Any(Function(Item) Item.IsCandidate), "内嵌候选可能不兼容：", "已声明不兼容：") & Edge.Owner.Display & " 与 " & Requirement.Display
            Case DependencyKind.Recommended
                If Value.State <> MatchState.Unsatisfied Then Return
                Kind = IssueKind.Recommendation
                Detail = "建议依赖未满足：" & Edge.Owner.Display & " 建议安装 " & Requirement.Display
            Case Else
                Return
        End Select
        If Value.Providers.Count > 0 AndAlso Not IsInternalLoaderId(Requirement.Id) Then Detail &= "；本地：" & String.Join("、", Value.Providers.Select(Function(Item) Item.Display & " " & If(String.IsNullOrEmpty(Item.Version), "未知版本", Item.Version)))
        Dim Disabled = Report.Mods.Where(Function(Item) Not Item.Enabled AndAlso DependencyMentions(Requirement, Identities(Item).Keys)).ToList()
        If Value.Missing AndAlso Disabled.Count > 0 Then Detail &= "；已禁用：" & String.Join("、", Disabled.Select(Function(Item) Path.GetFileName(Item.RootPath)).Distinct())
        If Not String.IsNullOrEmpty(Requirement.Reason) Then Detail &= "（" & Requirement.Reason & "）"
        Report.Issues.Add(New InspectionIssue With {.Kind = Kind, .Owner = Edge.Owner, .Requirement = Requirement, .RelatedMods = Value.Providers, .IsError = IsError, .Message = Detail})
    End Sub

    Friend Shared Function GetImpacts(Report As InspectionReport, RemovedPaths As IEnumerable(Of String)) As List(Of RemovalImpact)
        If Report Is Nothing Then Throw New ArgumentNullException(NameOf(Report))
        Dim Removed As New HashSet(Of String)(RemovedPaths.Select(Function(Name) FilePathKey(Name)), StringComparer.OrdinalIgnoreCase)
        Dim Active = ActiveMods(Report), Original = ProviderMap(Active.Concat(Report.Builtins))
        Dim Unavailable As New HashSet(Of String)(Active.Where(Function(Item) Removed.Contains(FilePathKey(Item.RootPath))).Select(Function(Item) Item.Key), StringComparer.Ordinal)
        Dim Result As New List(Of RemovalImpact), Changed = True, Round As Integer
        While Changed
            Changed = False
            Dim Map = ProviderMap(Active.Where(Function(Item) Not Unavailable.Contains(Item.Key)).Concat(Report.Builtins))
            Dim Additions As New List(Of RemovalImpact)
            For Each Item In Active.Where(Function(Entry) Not Removed.Contains(FilePathKey(Entry.RootPath)) AndAlso Not Unavailable.Contains(Entry.Key) AndAlso Not Entry.IsCandidate)
                Dim Failed As New List(Of Dependency)
                For Each Requirement In Item.Dependencies.Where(Function(Entry) Entry.Kind = DependencyKind.Required AndAlso SideApplies(Entry.Side, Report.Context))
                    If Resolve(Requirement, Original, Report.Context).State = MatchState.Unsatisfied Then Continue For
                    If Resolve(Requirement, Map, Report.Context).State = MatchState.Unsatisfied Then Failed.Add(Requirement)
                Next
                If Failed.Count = 0 Then Continue For
                Additions.Add(New RemovalImpact With {.Mod = Item, .Requirements = Failed, .IsDirect = Round = 0,
                    .IsDefinite = Not Report.MetadataIncomplete AndAlso Failed.All(Function(Requirement) Resolve(Requirement, Original, Report.Context).State = MatchState.Satisfied),
                    .Reason = If(Round = 0, "将失去必需依赖：", "间接受影响，必需依赖将无法加载：") & String.Join("；", Failed.Select(Function(Requirement) Requirement.Display))})
            Next
            For Each Impact In Additions
                Result.Add(Impact)
                Unavailable.Add(Impact.Mod.Key)
                Changed = True
            Next
            Round += 1
        End While
        Return Result
    End Function
    Friend Shared Function DescribeImpacts(Report As InspectionReport, RemovedPaths As IEnumerable(Of String)) As String
        Dim Paths = RemovedPaths.ToArray(), Impacts = GetImpacts(Report, Paths), Online = Report.DescribeOnlineImpacts(Paths)
        If Impacts.Count = 0 Then Return Online
        Dim Lines As New List(Of String) From {"以下模组依赖本次操作涉及的文件："}
        For Each Impact In Impacts.Take(30)
            Lines.Add("• " & Impact.Mod.Display & "（" & Path.GetFileName(Impact.Mod.RootPath) & "）" & If(Impact.IsDefinite, "", " [待确认]") & vbCrLf & "  " & Impact.Reason)
        Next
        If Impacts.Count > 30 Then Lines.Add("另有 " & (Impacts.Count - 30) & " 个模组受影响。")
        If Online <> "" Then Lines.Add(vbCrLf & Online)
        Lines.Add("仍可继续操作；如需一起禁用或删除，请先返回模组管理选择这些文件。")
        Return String.Join(vbCrLf, Lines)
    End Function
    Friend Shared Function DependencyMentions(Requirement As Dependency, Ids As IEnumerable(Of String)) As Boolean
        Dim Names = New HashSet(Of String)(Ids, StringComparer.Ordinal)
        If Names.Contains(If(Requirement.Id, "")) Then Return True
        If Requirement.Unless IsNot Nothing AndAlso DependencyMentions(Requirement.Unless, Names) Then Return True
        Return Requirement.Children.Any(Function(Child) DependencyMentions(Child, Names))
    End Function
    Private Shared Function DescribeFile(Report As InspectionReport, FullPath As String) As String
        Dim Target = FilePathKey(FullPath), Items = Report.Mods.Where(Function(Item) SameFilePath(Item.RootPath, Target)).ToList()
        Dim Lines As New List(Of String) From {Path.GetFileName(Target)}
        If Items.Count = 0 Then
            Lines.Add("未识别到支持的本地模组元数据，无法确定依赖关系。")
            Lines.AddRange(Report.Notices.Where(Function(Notice) Notice.Contains(Path.GetFileName(Target))).Take(4))
            Dim Online = Report.DescribeOnlineFile(Target)
            If Online <> "" Then Lines.Add(vbCrLf & Online)
            Return String.Join(vbCrLf, Lines)
        End If
        For Each Item In Items
            Lines.Add("")
            Lines.Add(Item.Display & " · " & If(String.IsNullOrEmpty(Item.Version), "未知版本", Item.Version) & " · " & Item.Format & If(Item.Enabled, "", " · 已禁用") & If(Item.IsNested, " · 内嵌", ""))
            If Item.Side <> "*" Then Lines.Add("运行环境：" & Item.Side)
            Lines.Add("需要的模组：")
            If Item.Dependencies.Count = 0 Then Lines.Add("  元数据未声明依赖。")
            For Each Requirement In Item.Dependencies.Take(60)
                Dim Edge = Report.Edges.FirstOrDefault(Function(Entry) Entry.Owner Is Item AndAlso Entry.Requirement Is Requirement)
                Dim State = If(Edge Is Nothing, "未参与本次启动检查", If(Edge.State = MatchState.Satisfied, "已满足", If(Edge.State = MatchState.Unsatisfied, "未满足", "版本待确认")))
                Dim KindText = New Dictionary(Of DependencyKind, String) From {{DependencyKind.Required, "必需"}, {DependencyKind.Optional, "可选"}, {DependencyKind.Recommended, "建议"},
                    {DependencyKind.Suggested, "可选提示"}, {DependencyKind.Incompatible, "不兼容"}, {DependencyKind.Conflict, "冲突提示"}}
                If Requirement.Kind = DependencyKind.Incompatible OrElse Requirement.Kind = DependencyKind.Conflict Then State = If(Edge IsNot Nothing AndAlso Edge.State = MatchState.Satisfied, "检测到匹配项", "未检测到确定匹配项")
                Lines.Add("  • [" & KindText(Requirement.Kind) & "] " & RequirementDisplay(Requirement, Report.Context) & "（" & State & If(Requirement.Side = "*", "", "；" & Requirement.Side) & "）")
            Next
            Dim Referrers As List(Of DependencyEdge) = Nothing
            Report.ReverseDependencies.TryGetValue(Item.Key, Referrers)
            Dim Reverse = If(Referrers, New List(Of DependencyEdge)).Where(Function(Edge) IsRequiredFileProvider(Report, Edge, Item.RootPath)).ToList()
            Lines.Add("被这些模组依赖：")
            If Reverse.Count = 0 Then Lines.Add("  未发现其他已启用模组声明依赖此 ID。")
            For Each Edge In Reverse.Take(60)
                Lines.Add("  • " & Edge.Owner.Display & "（" & Path.GetFileName(Edge.Owner.RootPath) & "）" & If(Edge.Requirement.Kind = DependencyKind.Required, " · 必需", " · 可选或兼容声明"))
            Next
        Next
        Lines.Add("")
        Lines.Add("本地依赖来自模组元数据；未声明的运行时依赖无法在此确认。")
        Dim Supplement = Report.DescribeOnlineFile(Target)
        If Supplement <> "" Then Lines.Add(vbCrLf & Supplement)
        Return String.Join(vbCrLf, Lines)
    End Function

    'Three-state version comparison. Unknown is deliberate and must never become a startup error.
    Friend Shared Function MatchVersion(Actual As String, Requirement As String, Optional Syntax As String = "fabric") As MatchState
        Return MatchVersions(Actual, New JValue(Requirement), Syntax)
    End Function
    Private Shared Function MatchVersions(Actual As String, Requirement As JToken, Syntax As String, Optional Depth As Integer = 0) As MatchState
        If Depth > 32 OrElse Requirement Is Nothing Then Return MatchState.Unknown
        If TypeOf Requirement Is JArray Then Return Combine(CType(Requirement, JArray).Select(Function(Value) MatchVersions(Actual, Value, Syntax, Depth + 1)), False)
        If TypeOf Requirement Is JObject Then
            Dim Any = Requirement("any"), All = Requirement("all")
            If (Any Is Nothing) = (All Is Nothing) Then Return MatchState.Unknown
            Dim Values = If(Any, All)
            If Not TypeOf Values Is JArray Then Return MatchState.Unknown
            Return Combine(CType(Values, JArray).Select(Function(Value) MatchVersions(Actual, Value, Syntax, Depth + 1)), All IsNot Nothing)
        End If
        If Requirement.Type <> JTokenType.String Then Return MatchState.Unknown
        Dim Range = Requirement.ToString().Trim()
        If Range = "" OrElse Range = "*" Then Return MatchState.Satisfied
        If String.IsNullOrWhiteSpace(Actual) OrElse Actual = "未知版本" OrElse Actual.Equals("unknown", StringComparison.OrdinalIgnoreCase) OrElse Actual.Contains("$" & "{") OrElse Range.Contains("$" & "{") Then Return MatchState.Unknown
        If Syntax = "maven" Then Return MatchMaven(Actual, Range)
        If Range.Contains("||") Then Return Combine(Range.Split(New String() {"||"}, StringSplitOptions.None).Select(Function(Value) MatchVersion(Actual, Value, Syntax)), False)
        Range = Regex.Replace(Range, "([><=~^]+)\s+", "$1")
        Dim Tokens = Regex.Split(Range, "\s+").Where(Function(Value) Value <> "").ToArray()
        If Tokens.Any(Function(Value) Not Regex.IsMatch(Value, "^(?:>=|<=|>|<|=|~|\^)?(?:[A-Za-z0-9][A-Za-z0-9._:+*-]*|\*)$")) Then Return MatchState.Unknown
        Return Combine(Tokens.Select(Function(Value) MatchPredicate(Actual, Value, Syntax)), True)
    End Function
    Private Class NumericVersion
        Public Parts As Long()
        Public Prerelease As String
    End Class
    Private Shared Function ParseVersion(Value As String) As NumericVersion
        Dim Match = Regex.Match(If(Value, ""), "^(\d+(?:\.\d+)*)(?:-((?:[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?))?(?:\+[0-9A-Za-z.-]+)?$")
        If Not Match.Success Then Return Nothing
        Dim Parts As New List(Of Long)
        For Each Part In Match.Groups(1).Value.Split("."c)
            Dim Number As Long
            If Not Long.TryParse(Part, NumberStyles.None, CultureInfo.InvariantCulture, Number) Then Return Nothing
            Parts.Add(Number)
        Next
        'Fabric 的空预发布键（例如 1.21-）与正式版 1.21 不同，用作该版本所有预发布的下界。
        Return New NumericVersion With {.Parts = Parts.ToArray(), .Prerelease = If(Match.Groups(2).Success, Match.Groups(2).Value, Nothing)}
    End Function
    Private Shared Function CompareVersions(Left As NumericVersion, Right As NumericVersion) As Integer
        For Index = 0 To Math.Max(Left.Parts.Length, Right.Parts.Length) - 1
            Dim Comparison = If(Index < Left.Parts.Length, Left.Parts(Index), 0L).CompareTo(If(Index < Right.Parts.Length, Right.Parts(Index), 0L))
            If Comparison <> 0 Then Return Comparison
        Next
        If String.Equals(Left.Prerelease, Right.Prerelease, StringComparison.Ordinal) Then Return 0
        If Left.Prerelease Is Nothing Then Return 1
        If Right.Prerelease Is Nothing Then Return -1
        If Left.Prerelease = "" Then Return -1
        If Right.Prerelease = "" Then Return 1
        Dim LeftParts = Left.Prerelease.Split("."c), RightParts = Right.Prerelease.Split("."c)
        For Index = 0 To Math.Min(LeftParts.Length, RightParts.Length) - 1
            Dim LeftNumber As Long, RightNumber As Long, LeftNumeric = Long.TryParse(LeftParts(Index), LeftNumber), RightNumeric = Long.TryParse(RightParts(Index), RightNumber)
            Dim Comparison As Integer
            If LeftNumeric AndAlso RightNumeric Then
                Comparison = LeftNumber.CompareTo(RightNumber)
            ElseIf LeftNumeric <> RightNumeric Then
                Comparison = If(LeftNumeric, -1, 1)
            Else
                Comparison = StringComparer.Ordinal.Compare(LeftParts(Index), RightParts(Index))
            End If
            If Comparison <> 0 Then Return Comparison
        Next
        Return LeftParts.Length.CompareTo(RightParts.Length)
    End Function
    Private Shared Function MatchPredicate(Actual As String, Predicate As String, Syntax As String) As MatchState
        If Predicate = "*" OrElse Predicate = "x" OrElse Predicate = "X" Then Return MatchState.Satisfied
        Dim Match = Regex.Match(Predicate, "^(>=|<=|>|<|=|~|\^)?(.+)$")
        If Not Match.Success Then Return MatchState.Unknown
        Dim OperatorText = Match.Groups(1).Value, Expected = Match.Groups(2).Value, Current = ParseVersion(Actual)
        If Regex.IsMatch(Expected, "^(?:\d+\.)*(?:\*|[xX])$") Then
            If OperatorText <> "" AndAlso OperatorText <> "=" Then Return MatchState.Unknown
            If Current Is Nothing Then Return MatchState.Unknown
            Dim Prefix = Expected.Split("."c).TakeWhile(Function(Part) Part <> "*" AndAlso Part <> "x" AndAlso Part <> "X").ToArray()
            For Index = 0 To Prefix.Length - 1
                Dim Number As Long
                If Not Long.TryParse(Prefix(Index), Number) Then Return MatchState.Unknown
                If If(Index < Current.Parts.Length, Current.Parts(Index), 0L) <> Number Then Return MatchState.Unsatisfied
            Next
            Return MatchState.Satisfied
        End If
        Dim Target = ParseVersion(Expected)
        If Current Is Nothing OrElse Target Is Nothing Then
            If OperatorText <> "" AndAlso OperatorText <> "=" Then Return MatchState.Unknown
            If Not Regex.IsMatch(Expected, "^[A-Za-z0-9][A-Za-z0-9._:+-]*$") OrElse Expected.EndsWith("+", StringComparison.Ordinal) OrElse
                {"latest", "latest.release", "latest.integration"}.Contains(Expected.ToLowerInvariant()) Then Return MatchState.Unknown
            Return If(String.Equals(Actual, Expected, StringComparison.Ordinal), MatchState.Satisfied, MatchState.Unsatisfied)
        End If
        Dim Comparison = CompareVersions(Current, Target)
        Select Case OperatorText
            Case "", "=" : Return If(Comparison = 0, MatchState.Satisfied, MatchState.Unsatisfied)
            Case ">" : Return If(Comparison > 0, MatchState.Satisfied, MatchState.Unsatisfied)
            Case ">=" : Return If(Comparison >= 0, MatchState.Satisfied, MatchState.Unsatisfied)
            Case "<" : Return If(Comparison < 0, MatchState.Satisfied, MatchState.Unsatisfied)
            Case "<=" : Return If(Comparison <= 0, MatchState.Satisfied, MatchState.Unsatisfied)
            Case "~", "^"
                If Syntax = "fabric" Then
                    'Fabric compares equal major (~ also minor), including major zero and omitted minor components.
                    For Index = 0 To If(OperatorText = "~", 1, 0)
                        If If(Index < Current.Parts.Length, Current.Parts(Index), 0L) <> If(Index < Target.Parts.Length, Target.Parts(Index), 0L) Then Return MatchState.Unsatisfied
                    Next
                    Return If(Comparison >= 0, MatchState.Satisfied, MatchState.Unsatisfied)
                End If
                Dim Upper = CType(Target.Parts.Clone(), Long()), Bump As Integer
                If OperatorText = "~" Then
                    Bump = If(Upper.Length > 1, 1, 0)
                Else
                    Bump = 0
                    While Bump < Upper.Length - 1 AndAlso Upper(Bump) = 0
                        Bump += 1
                    End While
                End If
                If Upper(Bump) = Long.MaxValue Then Return MatchState.Unknown
                Upper(Bump) += 1
                For Index = Bump + 1 To Upper.Length - 1
                    Upper(Index) = 0
                Next
                Dim UpperVersion As New NumericVersion With {.Parts = Upper, .Prerelease = Nothing}
                Return If(Comparison >= 0 AndAlso CompareVersions(Current, UpperVersion) < 0, MatchState.Satisfied, MatchState.Unsatisfied)
            Case Else : Return MatchState.Unknown
        End Select
    End Function
    Private Shared Function MatchMaven(Actual As String, Range As String) As MatchState
        'Maven's bare version is a soft preference, not an exact constraint.
        If Not Range.StartsWith("[", StringComparison.Ordinal) AndAlso Not Range.StartsWith("(", StringComparison.Ordinal) Then Return MatchState.Unknown
        Dim Intervals = Regex.Matches(Range, "[\[\(][^\[\]\(\)]*[\]\)]")
        If Intervals.Count = 0 OrElse Regex.Replace(Range, "[\[\(][^\[\]\(\)]*[\]\)]", "").Trim(" "c, ","c) <> "" Then Return MatchState.Unknown
        Dim Values As New List(Of MatchState), Current = ParseVersion(Actual)
        For Each Interval As Match In Intervals
            Dim Raw = Interval.Value, Body = Raw.Substring(1, Raw.Length - 2), Parts = Body.Split(","c)
            If Parts.Length = 1 Then
                If Raw(0) <> "["c OrElse Raw.Last() <> "]"c Then
                    Values.Add(MatchState.Unknown)
                Else
                    Dim Target = ParseVersion(Parts(0).Trim())
                    If Current Is Nothing OrElse Target Is Nothing OrElse Not Regex.IsMatch(Actual, "^\d+(?:\.\d+)*$") OrElse Not Regex.IsMatch(Parts(0).Trim(), "^\d+(?:\.\d+)*$") Then
                        Values.Add(If(String.Equals(Actual, Parts(0).Trim(), StringComparison.Ordinal), MatchState.Satisfied, MatchState.Unknown))
                    Else
                        Values.Add(If(CompareVersions(Current, Target) = 0, MatchState.Satisfied, MatchState.Unsatisfied))
                    End If
                End If
                Continue For
            End If
            'Maven qualifier ordering differs from SemVer; ordered ranges use purely numeric versions only.
            If Parts.Length <> 2 OrElse Current Is Nothing OrElse Not Regex.IsMatch(Actual, "^\d+(?:\.\d+)*$") Then
                Values.Add(MatchState.Unknown)
                Continue For
            End If
            Dim Bounds As New List(Of MatchState)
            For Index = 0 To 1
                Dim Bound = Parts(Index).Trim()
                If Bound = "" Then
                    Bounds.Add(MatchState.Satisfied)
                    Continue For
                End If
                Dim Target = ParseVersion(Bound)
                If Target Is Nothing OrElse Not Regex.IsMatch(Bound, "^\d+(?:\.\d+)*$") Then
                    Bounds.Add(MatchState.Unknown)
                    Continue For
                End If
                Dim Compare = CompareVersions(Current, Target)
                Dim Fits = If(Index = 0, Compare > 0 OrElse (Compare = 0 AndAlso Raw(0) = "["c), Compare < 0 OrElse (Compare = 0 AndAlso Raw.Last() = "]"c))
                Bounds.Add(If(Fits, MatchState.Satisfied, MatchState.Unsatisfied))
            Next
            Values.Add(Combine(Bounds, True))
        Next
        Return Combine(Values, False)
    End Function
End Class
