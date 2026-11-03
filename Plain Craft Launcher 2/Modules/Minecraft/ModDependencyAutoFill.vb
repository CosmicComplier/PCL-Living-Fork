Imports System.IO
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Inspector = PCL.ModDependencyInspector

'只处理精确匹配的已安装文件所声明的必需项目；下载后仍用加载器元数据复核，不把平台项目 ID 当成 Mod ID。
Friend NotInheritable Class ModDependencyAutoFill
    Friend Class Item
        Public Platform As String
        Public ProjectId As String
        Public Name As String
        Public Version As ResourceVersion
        Public Raw As JObject
        Public RequiredBy As New List(Of String)
        Public Alternatives As New List(Of Item)
    End Class
    Friend Class Plan
        Public Root As String
        Public Report As Inspector.InspectionReport
        Public Stamp As String
        Public Caption As String
        Public Items As New List(Of Item)
        Public Enables As New List(Of ModDependencyActivation.Item)
        Public Notes As New List(Of String)
        Friend ReadOnly Property ActionText As String
            Get
                If Enables.Count > 0 Then Return If(Items.Count > 0, "启用并补全后启动", "启用并启动")
                Return If(Items.Count > 0, "补全并启动", "返回 Mod 管理")
            End Get
        End Property
    End Class
    Private Class Requirement
        Public Platform As String
        Public Project As String
        Public Version As String
        Public Owner As String
    End Class
    Private Shared ReadOnly Cached As New ConcurrentDictionary(Of String, Tuple(Of DateTime, JToken))
    Private Class CandidateMismatchException
        Inherits IOException
        Friend ReadOnly Index As Integer
        Friend Sub New(Index As Integer, Message As String)
            MyBase.New(Message)
            Me.Index = Index
        End Sub
    End Class

    Private Shared Function S(Value As JToken) As String
        Return If(Value Is Nothing OrElse Value.Type = JTokenType.Null, "", Value.ToString())
    End Function
    Private Shared Function A(Value As JToken) As IEnumerable(Of JToken)
        Return If(TryCast(Value, JArray), New JArray)
    End Function
    Private Shared Function Key(Platform As String, Project As String) As String
        Return Platform & "|" & Project
    End Function
    Private Shared Function ValidId(Platform As String, Id As String) As Boolean
        Return Regex.IsMatch(If(Id, ""), If(Platform = "CurseForge", "\A[1-9][0-9]{0,12}\z", "\A[A-Za-z0-9_-]{1,64}\z"))
    End Function
    Private Shared Function Loader(Context As Inspector.InspectionContext) As ModLoaders
        If Context.LoaderVersions.ContainsKey("quilt_loader") Then Return ModLoaders.Fabric
        If Context.LoaderVersions.ContainsKey("neoforge") Then Return ModLoaders.NeoForge
        If Context.LoaderVersions.ContainsKey("forge") Then Return ModLoaders.Forge
        If Context.LoaderVersions.ContainsKey("fabricloader") Then Return ModLoaders.Fabric
        If Context.LoaderVersions.ContainsKey("liteloader") Then Return ModLoaders.LiteLoader
        Return ModLoaders.None
    End Function
    Private Shared Function Compatible(Version As ResourceVersion, Context As Inspector.InspectionContext) As Boolean
        If Version Is Nothing OrElse Version.ResourceType <> ResourceTypes.Mod OrElse Not Version.DownloadAvailable OrElse Version.Size <= 0 OrElse
            Version.GameVersions Is Nothing OrElse Not Version.GameVersions.Contains(Context.MinecraftVersion, StringComparer.OrdinalIgnoreCase) OrElse
            Loader(Context) = ModLoaders.None OrElse Not Version.ModLoaders.HasFlag(Loader(Context)) Then Return False
        If Not Regex.IsMatch(If(Version.Hash, ""), "\A(?:[0-9a-f]{40}|[0-9a-f]{32})\z") Then Return False
        Try
            InstanceResourceStore.SafeName(Version.FileName)
            If Not Version.FileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) Then Return False
            Version.DownloadUrls = Version.DownloadUrls.Where(Function(Value)
                Dim Address As Uri = Nothing
                Return Uri.TryCreate(Value, UriKind.Absolute, Address) AndAlso Address.Scheme = "https" AndAlso Address.UserInfo = "" AndAlso Address.IsDefaultPort AndAlso
                    (Address.Host = "cdn.modrinth.com" OrElse Address.Host.EndsWith(".forgecdn.net", StringComparison.OrdinalIgnoreCase))
            End Function).Distinct().ToList()
            Return Version.DownloadUrls.Count > 0
        Catch
            Return False
        End Try
    End Function

    Private Shared Function ParseVersion(Raw As JObject, Platform As String) As ResourceVersion
        If Platform = "CurseForge" Then
            If S(Raw("gameId")) <> "432" OrElse S(Raw("downloadUrl")) = "" OrElse S(Raw("isAvailable")) = "False" Then Return Nothing
        Else
            Dim Files = A(Raw("files")).OfType(Of JObject)().ToList()
            Dim Primary = Files.FirstOrDefault(Function(Value) Value("primary")?.ToObject(Of Boolean)() = True)
            If Primary Is Nothing AndAlso Files.Count = 1 Then Primary = Files(0)
            If Primary Is Nothing Then Return Nothing
            Raw = DirectCast(Raw.DeepClone(), JObject)
            Raw("files") = New JArray(Primary.DeepClone())
        End If
        Dim Version = ResourceVersion.FromPlatformJson(Raw, ResourceTypes.Mod)
        If A(Raw(If(Platform = "Modrinth", "loaders", "gameVersions"))).Any(Function(Value) S(Value).Equals("quilt", StringComparison.OrdinalIgnoreCase)) Then Version.ModLoaders = Version.ModLoaders Or ModLoaders.Fabric
        Return Version
    End Function

    Private Shared Function Required(Raw As JObject, Platform As String, Owner As String) As IEnumerable(Of Requirement)
        Return A(Raw("dependencies")).Where(Function(Value) If(Platform = "Modrinth", S(Value("dependency_type")) = "required", S(Value("relationType")) = "3")).
            Select(Function(Value) New Requirement With {.Platform = Platform, .Project = S(Value(If(Platform = "Modrinth", "project_id", "modId"))),
                .Version = If(Platform = "Modrinth", S(Value("version_id")), ""), .Owner = Owner}).ToList()
    End Function

    Private Shared Function IsRange(Value As JToken) As Boolean
        If TypeOf Value Is JArray Then Return DirectCast(Value, JArray).Count > 0 AndAlso DirectCast(Value, JArray).All(AddressOf IsRange)
        If Value Is Nothing OrElse Value.Type <> JTokenType.String Then Return False
        Dim Text = Value.ToString()
        Return Regex.IsMatch(Text, "[<>~^*]|(?:^|\.)[xX](?:$|\.)") OrElse (Text.Contains(",") AndAlso (Text.StartsWith("[") OrElse Text.StartsWith("(")))
    End Function

    Friend Shared Function BuildPlan(Root As String, Report As Inspector.InspectionReport, Token As CancellationToken,
        Optional Options As ModDependencyOnline.LookupOptions = Nothing) As Plan
        Dim Result = ModDependencyActivation.Prepare(Root, Report, Token)
        If Not Report.Mods.Any(Function(Value) Value.Enabled) Then Return Result
        If Loader(Report.Context) = ModLoaders.None OrElse String.IsNullOrEmpty(Report.Context.MinecraftVersion) Then
            Result.Notes.Add("尚未确认当前 Minecraft 版本或加载器，未选择自动补全文件。") : Return Result
        End If
        Dim Configuration = If(Options, New ModDependencyOnline.LookupOptions With {.ComparePlatformDependencies = True, .MaxFiles = 512, .MaxSearchFiles = 0,
            .MaxRequests = 40, .TimeBudgetMs = 12000, .MaxTotalBytes = 2L * 1024 * 1024 * 1024})
        Configuration.ComparePlatformDependencies = True : Configuration.MaxSearchFiles = 0
        Using Budget = CancellationTokenSource.CreateLinkedTokenSource(Token)
            Budget.CancelAfter(20000)
            ModDependencyOnline.Enrich(Report, Root, Budget.Token, Configuration)
            Dim Existing As New HashSet(Of String)(Report.OnlineFiles.Where(Function(Value) Value.Enabled).SelectMany(Function(Value) Value.Matches).
                Where(Function(Value) Value.IsExact).Select(Function(Value) Key(Value.Platform, Value.ProjectId)), StringComparer.Ordinal)
            Dim Disabled As New HashSet(Of String)(Report.OnlineFiles.Where(Function(Value) Not Value.Enabled).SelectMany(Function(Value) Value.Matches).
                Where(Function(Value) Value.IsExact).Select(Function(Value) Key(Value.Platform, Value.ProjectId)), StringComparer.Ordinal)
            Dim Pending As New Queue(Of Requirement)
            Dim FlexibleProjects As New HashSet(Of String)(StringComparer.Ordinal)
            For Each File In Report.OnlineFiles.Where(Function(Value) Value.Enabled)
                Dim Matches = File.Matches.Where(Function(Value) Value.IsExact).OrderBy(Function(Value) If(Value.Platform = "Modrinth", 0, 1)).ToList()
                If Matches.Count = 0 Then Continue For
                Dim Match = Matches(0)
                Dim Missing = Report.Issues.Where(Function(Issue) Issue.Kind = Inspector.IssueKind.MissingRequired AndAlso Inspector.SameFilePath(Issue.Owner.RootPath, File.FullPath)).ToList()
                Dim LocalRanges = Missing.Count > 0 AndAlso Missing.All(Function(Issue) Issue.Requirement IsNot Nothing AndAlso Not Issue.Requirement.IsUnknown AndAlso Issue.Requirement.Children.Count = 0 AndAlso IsRange(Issue.Requirement.Versions))
                For Each Dependency In Match.Dependencies.Where(Function(Value) Value.Kind = "required")
                    Pending.Enqueue(New Requirement With {.Platform = Dependency.Platform, .Project = Dependency.ProjectId, .Version = Dependency.VersionId, .Owner = Match.ProjectName})
                    If LocalRanges OrElse String.IsNullOrEmpty(Dependency.VersionId) Then FlexibleProjects.Add(Key(Dependency.Platform, Dependency.ProjectId))
                Next
            Next
            '平台记录可能只指向发布时的旧前置文件。本地声明为范围，或存在未锁定文件的声明时，
            '先选择适配当前环境的候选；安装前再由各模组 JAR 的实际约束验证交集。
            Dim Selected As New Dictionary(Of String, Item)(StringComparer.Ordinal), Visited As New HashSet(Of String)(StringComparer.Ordinal)
            Dim RequestCount As Integer
            Dim Request As Func(Of String, JToken) = Function(Url)
                Budget.Token.ThrowIfCancellationRequested()
                Dim Saved As Tuple(Of DateTime, JToken) = Nothing
                If Options Is Nothing AndAlso Cached.TryGetValue(Url, Saved) AndAlso Saved.Item1 > DateTime.UtcNow Then Return Saved.Item2.DeepClone()
                RequestCount += 1
                If RequestCount > 64 Then Throw New IOException("依赖项目过多，已停止继续查询。")
                Dim Data = If(Configuration.Request, AddressOf ModDependencyOnline.DefaultRequest).Invoke(Url, HttpMethod.Get, Nothing, Budget.Token)
                If Options Is Nothing AndAlso Data IsNot Nothing Then
                    If Cached.Count > 256 Then Cached.Clear()
                    Cached(Url) = Tuple.Create(DateTime.UtcNow.AddMinutes(10), Data.DeepClone())
                End If
                Return Data
            End Function
            Try
                While Pending.Count > 0 AndAlso Visited.Count < 64
                    Budget.Token.ThrowIfCancellationRequested()
                    Dim Need = Pending.Dequeue()
                    If Need.Platform = "Modrinth" AndAlso Need.Project = "" AndAlso ValidId(Need.Platform, Need.Version) Then
                        Need.Project = S(Request("https://api.modrinth.com/v2/version/" & Uri.EscapeDataString(Need.Version))?("project_id"))
                    End If
                    If Not ValidId(Need.Platform, Need.Project) Then Continue While
                    Dim Identity = Key(Need.Platform, Need.Project)
                    Dim FixedVersion = If(FlexibleProjects.Contains(Identity), "", Need.Version)
                    If Existing.Contains(Identity) Then Continue While
                    If Disabled.Contains(Identity) Then
                        Result.Notes.Add(Need.Project & " 已安装但被禁用，请在模组管理中启用。") : Continue While
                    End If
                    Dim Entry As Item = Nothing
                    If Selected.TryGetValue(Identity, Entry) Then
                        If FixedVersion <> "" AndAlso Entry.Version.Id <> FixedVersion Then Throw New IOException("不同模组要求不同的固定依赖版本，需手动处理：" & Entry.Name)
                        If FixedVersion = "" AndAlso Need.Version <> "" AndAlso Entry.Version.Id <> Need.Version Then Result.Notes.Add(Entry.Name & " 的平台记录指向较早文件；本次已合并未锁定的声明，安装前将核对各模组的实际版本要求。")
                        If Not Entry.RequiredBy.Contains(Need.Owner) Then Entry.RequiredBy.Add(Need.Owner)
                        Continue While
                    End If
                    If Not Visited.Add(Identity) Then Continue While
                    Dim ProjectUrl = If(Need.Platform = "Modrinth", "https://api.modrinth.com/v2/project/", "https://api.curseforge.com/v1/mods/") & Uri.EscapeDataString(Need.Project)
                    Dim Project = Request(ProjectUrl)
                    If Need.Platform = "CurseForge" Then Project = Project?("data")
                    If Project Is Nothing OrElse If(Need.Platform = "Modrinth", S(Project("project_type")) <> "mod", S(Project("classId")) <> "6" OrElse S(Project("gameId")) <> "432") Then
                        Result.Notes.Add("无法确认依赖项目的模组类型：" & Need.Project) : Continue While
                    End If
                    If Need.Platform = "Modrinth" AndAlso S(Project("client_side")) = "unsupported" Then
                        Result.Notes.Add("此依赖仅支持服务端，未选择客户端安装：" & S(Project("title"))) : Continue While
                    End If
                    Dim RawFiles As IEnumerable(Of JToken)
                    If Need.Platform = "Modrinth" Then
                        If FixedVersion <> "" Then
                            RawFiles = {Request("https://api.modrinth.com/v2/version/" & Uri.EscapeDataString(FixedVersion))}
                        Else
                            RawFiles = A(Request(ProjectUrl & "/version?game_versions=" & Uri.EscapeDataString(New JArray(Report.Context.MinecraftVersion).ToString(Newtonsoft.Json.Formatting.None))))
                        End If
                    Else
                        RawFiles = A(Request(ProjectUrl & "/files?gameVersion=" & Uri.EscapeDataString(Report.Context.MinecraftVersion) & "&pageSize=50")?("data"))
                    End If
                    Dim Candidates As New List(Of Item)
                    For Each Raw In RawFiles.OfType(Of JObject)()
                        Try
                            If S(Raw(If(Need.Platform = "Modrinth", "project_id", "modId"))) <> Need.Project Then Continue For
                            Dim Version = ParseVersion(Raw, Need.Platform)
                            If Not Compatible(Version, Report.Context) OrElse (FixedVersion <> "" AndAlso Version.Id <> FixedVersion) Then Continue For
                            Candidates.Add(New Item With {.Platform = Need.Platform, .ProjectId = Need.Project, .Name = S(Project(If(Need.Platform = "Modrinth", "title", "name"))), .Version = Version, .Raw = Raw})
                        Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                            '单个错误文件不参与自动安装。
                        End Try
                    Next
                    Entry = Candidates.OrderBy(Function(Value) CInt(Value.Version.ReleaseType)).ThenByDescending(Function(Value) Value.Version.ReleaseDate).FirstOrDefault()
                    If Entry Is Nothing Then
                        Result.Notes.Add("没有找到匹配当前游戏版本和加载器的可下载前置：" & S(Project(If(Need.Platform = "Modrinth", "title", "name")))) : Continue While
                    End If
                    If FixedVersion = "" Then Entry.Alternatives.AddRange(Candidates.Where(Function(Value) Not ReferenceEquals(Value, Entry)).OrderBy(Function(Value) CInt(Value.Version.ReleaseType)).ThenByDescending(Function(Value) Value.Version.ReleaseDate).Take(7))
                    Entry.RequiredBy.Add(Need.Owner) : Selected.Add(Identity, Entry) : Result.Items.Add(Entry)
                    For Each Dependency In Required(Entry.Raw, Entry.Platform, Entry.Name)
                        If String.IsNullOrEmpty(Dependency.Version) Then FlexibleProjects.Add(Key(Dependency.Platform, Dependency.Project))
                        Pending.Enqueue(Dependency)
                    Next
                End While
                If Pending.Count > 0 Then Throw New IOException("必需前置数量超过本次自动补全上限，请分批管理模组。")
            Catch ex As OperationCanceledException
                If Token.IsCancellationRequested Then Throw
                Result.Notes.Add("依赖查询超时，本次未准备自动安装。") : Result.Items.Clear()
            Catch ex As Exception
                Result.Notes.Add("依赖查询未完成：" & ex.Message) : Result.Items.Clear()
            End Try
        End Using
        Return Result
    End Function

    Friend Shared Function CreateDownloadTask(Plan As Plan, Files As List(Of NetFile)) As LoaderCombo(Of Integer)
        Dim Steps As New List(Of LoaderBase)
        For Index = 0 To Files.Count - 1
            Steps.Add(New LoaderDownload("下载 " & Plan.Items(Index).Name & " " & Plan.Items(Index).Version.Display, {Files(Index)}) With {.Block = False,
                .ProgressWeight = Math.Max(1, Plan.Items(Index).Version.Size / 1024.0 / 1024.0)})
        Next
        Return New LoaderCombo(Of Integer)("补全模组依赖：" & If(Plan.Caption, New DirectoryInfo(Plan.Root).Parent.Name), Steps) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
    End Function

    Private Shared Function StagedFile(Plan As Plan, Stage As String, Index As Integer) As String
        Return Path.Combine(Stage, Index & "-" & Plan.Items(Index).Version.FileName)
    End Function

    Friend Shared Function Install(Plan As Plan, Token As CancellationToken, Optional Download As Action(Of List(Of NetFile), CancellationToken) = Nothing) As Integer
        For Attempt = 0 To 15
            Token.ThrowIfCancellationRequested()
            Try
                Return InstallAttempt(Plan, Token, Download)
            Catch ex As CandidateMismatchException
                If ex.Index < 0 OrElse ex.Index >= Plan.Items.Count Then Throw
                Dim Entry = Plan.Items(ex.Index), NextVersion As Item = Nothing
                Dim Requirements = String.Join("|", Required(Entry.Raw, Entry.Platform, "").Select(Function(Value) Key(Value.Platform, Value.Project) & ":" & Value.Version).OrderBy(Function(Value) Value, StringComparer.Ordinal))
                While Entry.Alternatives.Count > 0
                    Dim Candidate = Entry.Alternatives(0) : Entry.Alternatives.RemoveAt(0)
                    Dim OtherRequirements = String.Join("|", Required(Candidate.Raw, Candidate.Platform, "").Select(Function(Value) Key(Value.Platform, Value.Project) & ":" & Value.Version).OrderBy(Function(Value) Value, StringComparer.Ordinal))
                    If OtherRequirements <> Requirements Then Continue While '不在确认后擅自引入新的依赖项目。
                    NextVersion = Candidate : Exit While
                End While
                If NextVersion Is Nothing Then Throw New IOException(ex.Message & vbCrLf & "没有找到其他可验证的兼容候选。", ex)
                Logger.Info("前置候选不兼容，尝试较早版本：" & Entry.Name & " " & NextVersion.Version.Display)
                Entry.Version = NextVersion.Version : Entry.Raw = NextVersion.Raw
            End Try
        Next
        Throw New IOException("兼容版本尝试次数已达上限，未自动安装。")
    End Function

    Private Shared Function CandidateIndex(Plan As Plan, Stage As String, Paths As IEnumerable(Of String)) As Integer
        For Index = 0 To Plan.Items.Count - 1
            Dim CandidatePath = StagedFile(Plan, Stage, Index)
            If Paths.Any(Function(Value) Inspector.SameFilePath(Value, CandidatePath)) Then Return Index
        Next
        Return -1
    End Function

    Private Shared Function InstallAttempt(Plan As Plan, Token As CancellationToken, Download As Action(Of List(Of NetFile), CancellationToken)) As Integer
        Token.ThrowIfCancellationRequested()
        If Plan.Items.Count = 0 AndAlso Plan.Enables.Count = 0 Then Return 0
        Dim Before = Inspector.Inspect(Plan.Root, Plan.Report.Context, Token)
        If LaunchDependencyCheck.Fingerprint(Before) <> Plan.Stamp Then Throw New IOException("模组文件已变化，请重新启动以检查依赖。")
        Dim Stage = Path.Combine(Plan.Root, ".pcl-import-" & Guid.NewGuid().ToString("N")), Created As New List(Of Tuple(Of String, String))
        Dim Enabled As New List(Of ModDependencyActivation.Item)
        InstanceResourceStore.EnsureWithin(Plan.Root, Stage) : Directory.CreateDirectory(Stage)
        Try
            Dim Files As New List(Of NetFile)
            For Index = 0 To Plan.Items.Count - 1
                Dim Entry = Plan.Items(Index)
                If Not Compatible(Entry.Version, Before.Context) Then Throw New IOException("前置文件的版本或下载信息已经变化。")
                Files.Add(Entry.Version.ToNetFile(StagedFile(Plan, Stage, Index), ResourceVersion.DownloadReason.Dependency, Before.Context.MinecraftVersion, Loader(Before.Context)))
            Next
            If Download Is Nothing Then Download = Sub(Downloads, Cancellation)
                If FrmMain Is Nothing Then
                    Dim Work As New LoaderDownload("下载必需前置", Downloads)
                    Using Registration = Cancellation.Register(AddressOf Work.Cancel)
                        Work.WaitForExit(c:=Cancellation)
                    End Using
                Else
                    Dim Work = CreateDownloadTask(Plan, Downloads)
                    RunInUiWait(Sub()
                        Work.Start(1)
                        LoaderTaskbarAdd(Work)
                        FrmMain.BtnExtraDownload.ShowRefresh()
                        FrmMain.BtnExtraDownload.Ribble()
                    End Sub)
                    Using Registration = Cancellation.Register(AddressOf Work.Cancel)
                        Work.WaitForExit(c:=Cancellation, StartIfNeeded:=False)
                    End Using
                End If
            End Sub
            If Files.Count > 0 Then Download(Files, Token)
            For Index = 0 To Plan.Items.Count - 1
                Token.ThrowIfCancellationRequested()
                Dim Entry = Plan.Items(Index), FullPath = StagedFile(Plan, Stage, Index)
                If Not File.Exists(FullPath) OrElse New FileInfo(FullPath).Length <> Entry.Version.Size OrElse InstanceResourceUpdates.FileHash(FullPath, Entry.Version.Hash.Length = 32) <> Entry.Version.Hash Then Throw New IOException("下载的前置文件校验失败，未加入模组目录。")
            Next
            Dim Added = Inspector.Inspect(Stage, Before.Context, Token)
            If Added.MetadataIncomplete Then Throw New IOException("前置文件的元数据未能完整识别，未自动安装。")
            Dim Ready = Inspector.WithEnabledFiles(Before, Plan.Enables.Select(Function(Item) Item.Source), Token)
            Dim ExistingIds As New HashSet(Of String)(Ready.Mods.Where(Function(Value) Value.Enabled AndAlso Not Value.IsNested).Select(Function(Value) Value.Id), StringComparer.Ordinal)
            Dim AddedIds As New Dictionary(Of String, String)(StringComparer.Ordinal)
            Dim Chosen As New List(Of Integer)
            For Index = 0 To Plan.Items.Count - 1
                Dim FullPath = StagedFile(Plan, Stage, Index), Mods = Added.Mods.Where(Function(Value) Inspector.SameFilePath(Value.RootPath, FullPath) AndAlso Not Value.IsNested).ToList()
                If Mods.Count = 0 Then Throw New IOException("下载文件没有可识别的模组声明。")
                If Not Mods.Any(Function(Value) Value.Side <> "server") Then Throw New IOException("下载的前置仅支持服务端，未加入客户端模组目录。")
                If Mods.Any(Function(Value) ExistingIds.Contains(Value.Id)) Then
                    Dim Hash = InstanceResourceUpdates.FileHash(FullPath)
                    If Mods.Any(Function(Value) AddedIds.ContainsKey(Value.Id) AndAlso AddedIds(Value.Id) <> Hash) Then Throw New IOException("多个前置文件声明同一个模组 ID，未自动安装。")
                    Continue For
                End If
                Chosen.Add(Index)
                For Each Value In Mods
                    ExistingIds.Add(Value.Id)
                    AddedIds(Value.Id) = InstanceResourceUpdates.FileHash(FullPath)
                Next
            Next
            Dim Combined As New Inspector.InspectionReport With {.Context = Before.Context, .Builtins = Before.Builtins, .MetadataIncomplete = Before.MetadataIncomplete}
            Combined.Mods.AddRange(Ready.Mods)
            Combined.Mods.AddRange(Added.Mods.Where(Function(Value) Chosen.Any(Function(Index) Inspector.SameFilePath(Value.RootPath, StagedFile(Plan, Stage, Index)))))
            Inspector.BuildReport(Combined, Token)
            Dim Unmet = Combined.Edges.Where(Function(Value) Value.Owner.RootPath.StartsWith(Stage & "\", StringComparison.OrdinalIgnoreCase) AndAlso Value.Evaluated AndAlso
                Value.Requirement.Kind = Inspector.DependencyKind.Required AndAlso Value.State <> Inspector.MatchState.Satisfied).ToList()
            Unmet.AddRange(Combined.Edges.Where(Function(Value) Plan.Enables.Any(Function(Item) Inspector.SameFilePath(Item.Source, Value.Owner.RootPath)) AndAlso Value.Evaluated AndAlso Value.Requirement.Kind = Inspector.DependencyKind.Required AndAlso Value.State <> Inspector.MatchState.Satisfied))
            If Unmet.Count > 0 Then Throw New CandidateMismatchException(CandidateIndex(Plan, Stage, Unmet.Select(Function(Value) Value.Owner.RootPath)), "前置文件仍有未满足或无法确认的必需依赖，未自动安装：" & vbCrLf &
                String.Join(vbCrLf, Unmet.Take(5).Select(Function(Value) Value.Owner.Display & " 需要 " & Value.Requirement.Display)))
            Dim PreviousIssues As New HashSet(Of String)(Before.Issues.Select(Function(Value) Value.Kind.ToString() & "|" & Value.Message), StringComparer.Ordinal)
            Dim Introduced = Combined.Issues.Where(Function(Value) (Value.IsError OrElse (Value.Kind = Inspector.IssueKind.MissingRequired AndAlso Value.Owner.RootPath.StartsWith(Stage & "\", StringComparison.OrdinalIgnoreCase))) AndAlso
                Not PreviousIssues.Contains(Value.Kind.ToString() & "|" & Value.Message)).ToList()
            If Introduced.Count > 0 Then Throw New CandidateMismatchException(CandidateIndex(Plan, Stage, Introduced.Select(Function(Value) Value.Owner.RootPath).Concat(Introduced.SelectMany(Function(Value) Value.RelatedMods).Select(Function(Value) Value.RootPath))), "前置文件会引入依赖缺失、版本不匹配或冲突，未自动安装：" & vbCrLf & String.Join(vbCrLf, Introduced.Take(5).Select(Function(Value) Value.Message)))
            Dim Current = Inspector.Inspect(Plan.Root, Before.Context, Token)
            If LaunchDependencyCheck.Fingerprint(Current) <> Plan.Stamp Then Throw New IOException("下载期间模组文件发生变化，已停止自动安装。")
            For Each Entry In Plan.Enables
                InstanceResourceStore.EnsureWithin(Plan.Root, Entry.Source) : InstanceResourceStore.EnsureWithin(Plan.Root, Entry.Destination)
                If File.Exists(Entry.Destination) OrElse Directory.Exists(Entry.Destination) OrElse InstanceResourceUpdates.FileHash(Entry.Source) <> Entry.Sha1 Then Throw New IOException("已禁用的前置或目标文件已变化，未自动启用。")
            Next
            For Each Entry In Plan.Enables
                Token.ThrowIfCancellationRequested()
                File.Move(Entry.Source, Entry.Destination) : Enabled.Add(Entry)
            Next
            For Each Index In Chosen
                Token.ThrowIfCancellationRequested()
                Dim Entry = Plan.Items(Index), Destination = Path.Combine(Plan.Root, Entry.Version.FileName)
                InstanceResourceStore.EnsureWithin(Plan.Root, Destination)
                If File.Exists(Destination) OrElse Directory.Exists(Destination) Then Throw New IOException("已有同名文件，未覆盖：" & Entry.Version.FileName)
                File.Move(StagedFile(Plan, Stage, Index), Destination)
                Created.Add(Tuple.Create(Destination, Entry.Version.Hash))
            Next
            Token.ThrowIfCancellationRequested()
            Return Created.Count + Enabled.Count
        Catch
            For Each Entry In Created
                If File.Exists(Entry.Item1) AndAlso InstanceResourceUpdates.FileHash(Entry.Item1, Entry.Item2.Length = 32) = Entry.Item2 Then File.Delete(Entry.Item1)
            Next
            For Each Entry In Enabled.AsEnumerable().Reverse()
                If Not File.Exists(Entry.Source) AndAlso File.Exists(Entry.Destination) AndAlso InstanceResourceUpdates.FileHash(Entry.Destination) = Entry.Sha1 Then File.Move(Entry.Destination, Entry.Source)
            Next
            Throw
        Finally
            Try
                InstanceResourceStore.DeleteStage(Plan.Root, Stage)
            Catch ex As IOException
                Logger.Warn(ex, "前置下载临时文件暂时被占用，已保留在 " & Stage)
            End Try
        End Try
    End Function
End Class
