Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports Inspector = PCL.ModDependencyInspector

Friend NotInheritable Class LaunchDependencyCheck
    Private Shared ReadOnly Accepted As New ConcurrentDictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

    Friend Shared Sub SettingsChanged()
        Accepted.Clear()
        RunInUi(Sub() FrmLaunchRight?.RefreshDependencyOptions())
    End Sub

    Friend Shared Function Fingerprint(Report As Inspector.InspectionReport) As String
        Dim Values As New List(Of String) From {JObject.FromObject(Report.Context).ToString(Newtonsoft.Json.Formatting.None)}
        Values.Add(Report.MetadataIncomplete.ToString())
        Values.AddRange(Report.Notices.OrderBy(Function(Value) Value, StringComparer.Ordinal))
        For Each Item In Report.Mods.OrderBy(Function(ModInfo) ModInfo.Key, StringComparer.Ordinal)
            Values.Add(Item.Key & "|" & Item.Version & "|" & Item.Enabled & "|" & Item.Format)
            Values.AddRange(Item.Dependencies.Select(Function(Requirement) Requirement.Kind.ToString() & ":" & Requirement.Display).OrderBy(Function(Value) Value, StringComparer.Ordinal))
        Next
        For Each FileName In Report.Mods.Select(Function(Item) Item.RootPath).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Function(Value) Value, StringComparer.OrdinalIgnoreCase)
            Try
                Dim Info As New FileInfo(FileName)
                Values.Add(FileName & "|" & If(Info.Exists, Info.Length.ToString() & "|" & Info.LastWriteTimeUtc.Ticks, "missing"))
            Catch
                Values.Add(FileName & "|unavailable")
            End Try
        Next
        Values.AddRange(Report.Issues.Select(Function(Issue) Issue.Kind.ToString() & "|" & Issue.IsError & "|" & Issue.Message).OrderBy(Function(Value) Value, StringComparer.Ordinal))
        Using Hash = SHA256.Create()
            Return Convert.ToBase64String(Hash.ComputeHash(Encoding.UTF8.GetBytes(String.Join(vbLf, Values))))
        End Using
    End Function

    Friend Shared Function WasAccepted(Folder As String, Report As Inspector.InspectionReport) As Boolean
        Dim Previous As String = Nothing
        Return Accepted.TryGetValue(Inspector.FilePathKey(Folder), Previous) AndAlso Previous = Fingerprint(Report)
    End Function

    Friend Shared Sub Accept(Folder As String, Report As Inspector.InspectionReport)
        Accepted(Inspector.FilePathKey(Folder)) = Fingerprint(Report)
    End Sub
End Class

Friend NotInheritable Class ModDependencyActivation
    Friend Class Item
        Public Name As String
        Public Source As String
        Public Destination As String
        Public Sha1 As String
        Public Owner As Inspector.ModInfo
        Public RequiredBy As New List(Of String)
        Public Requirements As New List(Of String)
        Public ResolvedIssues As New List(Of String)
    End Class

    Friend Shared Function IssueKey(Issue As Inspector.InspectionIssue) As String
        Return If(Issue.Owner?.Key, "") & "|" & Issue.Kind.ToString() & "|" & If(Issue.Requirement?.Display, Issue.Message)
    End Function

    Friend Shared Function Prepare(Root As String, Report As Inspector.InspectionReport, Token As Threading.CancellationToken) As ModDependencyAutoFill.Plan
        Dim Plan As New ModDependencyAutoFill.Plan With {.Root = Path.GetFullPath(Root), .Report = Report, .Stamp = LaunchDependencyCheck.Fingerprint(Report)}
        Dim Selected As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim Roots = Report.Mods.Where(Function(Item) Not Item.Enabled).Select(Function(Item) Item.RootPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        Dim Current = Report
        For Round = 0 To 63
            Token.ThrowIfCancellationRequested()
            Dim Changed = False
            For Each Issue In Current.Issues.Where(Function(Item) Item.Kind = Inspector.IssueKind.MissingRequired AndAlso Item.Requirement?.Kind = Inspector.DependencyKind.Required)
                Dim Matches = Roots.Where(Function(FileName) Not Selected.Contains(FileName) AndAlso Inspector.FileSatisfiesDependency(Current, FileName, Issue.Requirement)).ToList()
                If Matches.Count <> 1 Then Continue For
                Selected.Add(Matches(0)) : Changed = True
            Next
            If Not Changed Then Exit For
            Current = Inspector.WithEnabledFiles(Report, Selected, Token)
        Next
        If Selected.Count = 0 Then Return Plan
        Dim OriginalErrors As New HashSet(Of String)(Report.Issues.Where(Function(Issue) Issue.IsError).Select(AddressOf IssueKey), StringComparer.Ordinal)
        If Current.Issues.Any(Function(Issue) Issue.IsError AndAlso Not OriginalErrors.Contains(IssueKey(Issue))) OrElse
            Current.Edges.Any(Function(Edge) Selected.Contains(Edge.Owner.RootPath) AndAlso Edge.Evaluated AndAlso Edge.Requirement.Kind = Inspector.DependencyKind.Required AndAlso Edge.State <> Inspector.MatchState.Satisfied) Then
            Plan.Notes.Add("已禁用的前置还存在版本冲突或未满足的依赖，请在模组管理中处理后启用。")
            Return Plan
        End If
        For Each Source In Selected
            Token.ThrowIfCancellationRequested()
            Try
                InstanceResourceStore.EnsureWithin(Plan.Root, Source)
                Dim Destination = System.Text.RegularExpressions.Regex.Replace(Source, "(?:\.disabled|\.old)+$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                InstanceResourceStore.EnsureWithin(Plan.Root, Destination)
                If Destination = Source OrElse Not {".jar", ".zip", ".litemod"}.Contains(Path.GetExtension(Destination).ToLowerInvariant()) OrElse File.Exists(Destination) OrElse Directory.Exists(Destination) Then
                    Plan.Notes.Add("启用后的文件名已有占用，未自动处理：" & Path.GetFileName(Destination))
                    Plan.Enables.Clear() : Return Plan
                End If
                Dim Owner = Report.Mods.FirstOrDefault(Function(Item) Inspector.SameFilePath(Item.RootPath, Source) AndAlso Not Item.IsNested)
                Dim Entry As New Item With {.Name = If(Owner?.Name, Path.GetFileName(Destination)), .Owner = Owner, .Source = Source, .Destination = Destination, .Sha1 = InstanceResourceUpdates.FileHash(Source)}
                For Each Issue In Report.Issues.Where(Function(Value) Value.Kind = Inspector.IssueKind.MissingRequired AndAlso Value.Requirement IsNot Nothing AndAlso Inspector.FileSatisfiesDependency(Report, Source, Value.Requirement))
                    If Not Entry.RequiredBy.Contains(Issue.Owner.Name) Then Entry.RequiredBy.Add(Issue.Owner.Name)
                    If Not Entry.Requirements.Contains(Issue.Requirement.Display) Then Entry.Requirements.Add(Issue.Requirement.Display)
                    Entry.ResolvedIssues.Add(IssueKey(Issue))
                Next
                Plan.Enables.Add(Entry)
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                Plan.Notes.Add("无法确认已禁用文件，未自动启用：" & Path.GetFileName(Source))
                Plan.Enables.Clear() : Return Plan
            End Try
        Next
        Return Plan
    End Function
End Class
