Imports System.Reflection
Imports System.Security.Cryptography

Public Module ModLauncherUpdate
    Private Const Repository As String = LauncherRepository
    Private Const CompatibilityRepository As String = "DDZmumo/LivingPCL2-Re"
    Private Const ReleasePage As String = LauncherReleasesUrl & "/latest"
    Friend Const CnbManifestUrl As String = LauncherCnbUrl & "/-/git/raw/updates/latest.json"
    Friend Const CnbPreviewManifestUrl As String = LauncherCnbUrl & "/-/git/raw/updates/latest-preview.json"
    Friend Const GitHubLatestUrl As String = "https://api.github.com/repos/" & Repository & "/releases/latest"
    Friend Const GitHubReleaseListUrl As String = "https://api.github.com/repos/" & Repository & "/releases?per_page=30"
    Private CheckRunning As Integer

    Friend Enum UpdateSource
        Cnb = 0
        GitHub = 1
    End Enum

    Friend Enum UpdateChannel
        Stable = 0
        StableAndPrerelease = 1
    End Enum
    Friend Function PreferredUpdateChannel() As UpdateChannel
        Return If(Settings.Get(Of Integer)("ToolLauncherUpdateChannel") = 1, UpdateChannel.StableAndPrerelease, UpdateChannel.Stable)
    End Function
    Friend Function ChangeUpdateChannel(Value As Integer, Confirm As Func(Of Boolean)) As Boolean
        If Value <> 0 AndAlso Value <> 1 Then Return False
        If Value = CInt(PreferredUpdateChannel()) Then Return True
        If Value = 1 AndAlso (Confirm Is Nothing OrElse Not Confirm()) Then Return False
        Settings.Set("ToolLauncherUpdateChannel", Value)
        Return True
    End Function

    Friend Function PreferredUpdateSource() As UpdateSource
        Return If(Settings.Get(Of Integer)("ToolLauncherUpdateSource") = CInt(UpdateSource.GitHub), UpdateSource.GitHub, UpdateSource.Cnb)
    End Function

    Private Function UpdateSourceName(Source As UpdateSource) As String
        Return If(Source = UpdateSource.GitHub, "GitHub", "CNB")
    End Function

    Friend NotInheritable Class UpdatePromptView
        Inherits StackPanel
        Friend ReadOnly Property PreferredWidth As Double = 570
        Friend ReadOnly SourcePicker As New MyComboBox With {.Width = 240, .Height = 30, .HorizontalAlignment = HorizontalAlignment.Left, .Margin = New Thickness(18, 0, 0, 0)}
        Friend ReadOnly Property SelectedSource As UpdateSource
            Get
                Return If(SourcePicker.SelectedIndex = 1, UpdateSource.GitHub, UpdateSource.Cnb)
            End Get
        End Property
        Friend Sub New(Tag As String, Source As UpdateSource)
            Children.Add(New TextBlock With {.Text = "当前版本：" & VersionBaseName & vbCrLf & If(Tag.Contains("-"), "最新预发布版：", "最新正式版：") & Tag, .Margin = New Thickness(2, 0, 2, 18), .LineHeight = 24})
            SourcePicker.Items.Add(New MyComboBoxItem With {.Content = "CNB（国内）"})
            SourcePicker.Items.Add(New MyComboBoxItem With {.Content = "GitHub Releases"})
            SourcePicker.SelectedIndex = CInt(Source)
            Dim Row As New DockPanel With {.Margin = New Thickness(2, 0, 2, 16)}
            Row.Children.Add(New TextBlock With {.Text = "本次下载源", .VerticalAlignment = VerticalAlignment.Center})
            Row.Children.Add(SourcePicker) : Children.Add(Row)
            Dim Note As New TextBlock With {.Text = "下载源选择仅本次生效。下载并校验完成后，启动器将自动替换并重新启动。", .TextWrapping = TextWrapping.Wrap, .Margin = New Thickness(2, 0, 2, 0)}
            Note.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray3") : Children.Add(Note)
        End Sub
    End Class

    Friend NotInheritable Class UpdateTransport
        Friend ReadJson As Func(Of String, JObject) = Function(Url) JObject.Parse(NetRequestByClient(Url, Accept:="application/json", Timeout:=5000, RequireJson:=True))
        Friend ReadReleaseList As Func(Of String, JArray) = Function(Url) JArray.Parse(NetRequestByClient(Url, Accept:="application/json", Timeout:=5000, RequireJson:=True))
        Friend ReadText As Func(Of String, String) = Function(Url) NetRequestByClient(Url, Timeout:=8000)
        Friend Download As Action(Of String, String, FileChecker) = AddressOf DownloadWithTask
    End Class

    Friend Function NewerRelease(Tag As String, Current As String) As Boolean
        Return NewerReleaseForChannel(Tag, Current, UpdateChannel.Stable)
    End Function

    Private Function ParseReleaseVersion(Value As String, ByRef Core As System.Version, ByRef Preview As String) As Boolean
        Dim Match = System.Text.RegularExpressions.Regex.Match(If(Value, ""), "\A[vV]?(\d+\.\d+\.\d+(?:\.\d+)?)(?:-([0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*))?\z")
        If Not Match.Success OrElse Not System.Version.TryParse(Match.Groups(1).Value, Core) Then Return False
        Core = New System.Version(Core.Major, Core.Minor, Math.Max(0, Core.Build), Math.Max(0, Core.Revision))
        Preview = Match.Groups(2).Value
        Return True
    End Function
    Friend Function CompareReleaseVersions(First As String, Second As String) As Integer
        Dim A As System.Version = Nothing, B As System.Version = Nothing, AP As String = Nothing, BP As String = Nothing
        If Not ParseReleaseVersion(First, A, AP) OrElse Not ParseReleaseVersion(Second, B, BP) Then Throw New ArgumentException("发布版本号无效。")
        Dim Result = A.CompareTo(B)
        If Result <> 0 Then Return Result
        If AP = "" OrElse BP = "" Then Return If(AP = BP, 0, If(AP = "", 1, -1))
        Dim AT = System.Text.RegularExpressions.Regex.Matches(AP.ToLowerInvariant(), "[0-9]+|[a-z]+").Cast(Of System.Text.RegularExpressions.Match)().Select(Function(Item) Item.Value).ToArray()
        Dim BT = System.Text.RegularExpressions.Regex.Matches(BP.ToLowerInvariant(), "[0-9]+|[a-z]+").Cast(Of System.Text.RegularExpressions.Match)().Select(Function(Item) Item.Value).ToArray()
        For Index = 0 To Math.Min(AT.Length, BT.Length) - 1
            Dim AN = Char.IsDigit(AT(Index)(0)), BN = Char.IsDigit(BT(Index)(0))
            If AN AndAlso BN Then
                Dim AV = AT(Index).TrimStart("0"c), BV = BT(Index).TrimStart("0"c)
                Result = AV.Length.CompareTo(BV.Length)
                If Result = 0 Then Result = String.CompareOrdinal(AV, BV)
            ElseIf AN <> BN Then
                Result = If(AN, -1, 1)
            Else
                Result = String.CompareOrdinal(AT(Index), BT(Index))
            End If
            If Result <> 0 Then Return Result
        Next
        Return AT.Length.CompareTo(BT.Length)
    End Function
    Friend Function NewerReleaseForChannel(Tag As String, Current As String, Channel As UpdateChannel) As Boolean
        Dim Core As System.Version = Nothing, Preview As String = Nothing
        If Not ParseReleaseVersion(Tag, Core, Preview) OrElse (Preview <> "" AndAlso Channel = UpdateChannel.Stable) Then Return False
        If Not ParseReleaseVersion(Current, Core, Preview) Then Return False
        Return CompareReleaseVersions(Tag, Current) > 0
    End Function

    Friend Function ValidAssetUrl(Value As String) As Boolean
        Dim Url As Uri = Nothing
        If Not Uri.TryCreate(Value, UriKind.Absolute, Url) OrElse Url.Scheme <> "https" OrElse Url.UserInfo <> "" OrElse Not Url.IsDefaultPort OrElse Url.Fragment <> "" Then Return False
        If Url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) Then
            Return Url.AbsolutePath.StartsWith("/" & Repository & "/releases/download/", StringComparison.OrdinalIgnoreCase) OrElse
                Url.AbsolutePath.StartsWith("/" & CompatibilityRepository & "/releases/download/", StringComparison.OrdinalIgnoreCase)
        End If
        Return Url.Host.Equals("cnb.cool", StringComparison.OrdinalIgnoreCase) AndAlso Url.AbsolutePath.StartsWith("/" & LauncherCnbRepository & "/-/releases/download/", StringComparison.OrdinalIgnoreCase)
    End Function

    Friend Function ShouldPromptForUpdate(Tag As String, Current As String, IgnoredTag As String) As Boolean
        Return NewerReleaseForChannel(Tag, Current, PreferredUpdateChannel()) AndAlso Not String.Equals(Tag, IgnoredTag, StringComparison.Ordinal)
    End Function

    Friend Function IgnoreTagAfterChoice(IgnoredTag As String, Tag As String, Choice As Integer) As String
        If Choice = 2 Then Return Tag
        If Choice = 1 AndAlso String.Equals(IgnoredTag, Tag, StringComparison.Ordinal) Then Return ""
        Return IgnoredTag
    End Function

    Friend Function ReleaseExecutable(Info As JObject) As JObject
        Dim Assets = TryCast(Info("assets"), JArray)
        If Assets Is Nothing Then Throw New Exception("发布中没有下载附件。")
        Dim Executables = Assets.OfType(Of JObject)().Where(Function(Asset)
            Dim Name = Asset("name")?.ToString()
            Return Name IsNot Nothing AndAlso Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
        End Function).ToList()
        Dim Candidates = Executables.Where(Function(Asset) Asset("name").ToString().StartsWith("PCL-Living-Fork-", StringComparison.OrdinalIgnoreCase)).ToList()
        If Candidates.Count = 0 Then Candidates = Executables.Where(Function(Asset) Asset("name").ToString().StartsWith("PCL Living Fork - ", StringComparison.OrdinalIgnoreCase)).ToList()
        If Candidates.Count = 0 Then Candidates = Executables.Where(Function(Asset) Asset("name").ToString().StartsWith("LivingPCL2-Re-", StringComparison.OrdinalIgnoreCase)).ToList()
        If Candidates.Count <> 1 Then Throw New Exception("无法确定本版本的启动器 EXE。")
        If Not ValidAssetUrl(Candidates(0)("browser_download_url")?.ToString()) Then Throw New Exception("下载地址不属于本项目。")
        Return Candidates(0)
    End Function

    Friend Function ManifestHash(Manifest As String, Name As String) As String
        For Each Line In Manifest.Replace(vbCr, "").Split(vbLf)
            Dim Match = System.Text.RegularExpressions.Regex.Match(Line, "\A([0-9a-fA-F]{64})\s+\*?(.+?)\s*\z")
            If Match.Success AndAlso Match.Groups(2).Value = Name Then Return Match.Groups(1).Value.ToLowerInvariant()
        Next
        Throw New Exception("发布附件缺少对应的 SHA256 校验值。")
    End Function

    Private Function HashAsset(Info As JObject) As JObject
        Dim Asset = DirectCast(Info("assets"), JArray).OfType(Of JObject)().SingleOrDefault(Function(Item) Item("name")?.ToString() = "SHA256SUMS.txt")
        If Asset Is Nothing OrElse Not ValidAssetUrl(Asset("browser_download_url")?.ToString()) Then Throw New Exception("发布中没有可信的校验文件。")
        Return Asset
    End Function

    Private Sub ValidateReleaseInfo(Info As JObject, Cnb As Boolean, Channel As UpdateChannel)
        Dim Parsed As System.Version = Nothing, Preview As String = Nothing
        If Info Is Nothing OrElse Not ParseReleaseVersion(Info("tag_name")?.ToString(), Parsed, Preview) Then Throw New Exception("发布标签不是可识别的版本号。")
        For Each Field In {"draft", "prerelease"}
            If Info(Field) Is Nothing OrElse Info(Field).Type <> JTokenType.Boolean Then Throw New Exception("更新源的发布状态无效。")
        Next
        If CBool(Info("draft")) OrElse (Channel = UpdateChannel.Stable AndAlso CBool(Info("prerelease"))) Then Throw New Exception("更新源尚无符合当前通道的发布。")
        If CBool(Info("prerelease")) <> (Preview <> "") Then Throw New Exception("发布标记与版本号不一致。")
        Dim Executable = ReleaseExecutable(Info), Manifest = HashAsset(Info)
        If Executable("size")?.Type <> JTokenType.Integer OrElse CLng(Executable("size")) <= 0 OrElse CLng(Executable("size")) > 1024L * 1024 * 1024 Then Throw New Exception("更新附件大小无效。")
        For Each Asset In {Executable, Manifest}
            Dim Url As New Uri(Asset("browser_download_url").ToString()), Name = Asset("name").ToString()
            If System.IO.Path.GetFileName(Name) <> Name OrElse Url.Host <> If(Cnb, "cnb.cool", "github.com") Then Throw New Exception("更新附件与所选更新源不一致。")
            Dim Prefix = If(Cnb, "/" & LauncherCnbRepository & "/-/releases/download/", "/" & Repository & "/releases/download/")
            Dim Expected = Prefix & Uri.EscapeDataString(Info("tag_name").ToString()) & "/" & Uri.EscapeDataString(Name)
            If Not String.Equals(Url.AbsolutePath, Expected, StringComparison.Ordinal) Then Throw New Exception("更新附件不属于所选发布版本。")
        Next
    End Sub

    Friend Function FindLatestRelease(Current As String, Optional Transport As UpdateTransport = Nothing) As JObject
        Return FindLatestReleaseForSource(Current, PreferredUpdateSource(), Transport)
    End Function

    Friend Function FindLatestReleaseForSource(Current As String, Source As UpdateSource, Optional Transport As UpdateTransport = Nothing) As JObject
        Return FindLatestReleaseForChannel(Current, Source, PreferredUpdateChannel(), Transport)
    End Function
    Friend Function FindLatestReleaseForChannel(Current As String, Source As UpdateSource, Channel As UpdateChannel, Optional Transport As UpdateTransport = Nothing) As JObject
        If Transport Is Nothing Then Transport = New UpdateTransport
        Dim Best As JObject = Nothing, Failure As Exception = Nothing
        Dim Endpoints = If(Channel = UpdateChannel.Stable,
            If(Source = UpdateSource.GitHub, {GitHubLatestUrl, CnbManifestUrl}, {CnbManifestUrl, GitHubLatestUrl}),
            If(Source = UpdateSource.GitHub, {GitHubReleaseListUrl, CnbPreviewManifestUrl, GitHubLatestUrl, CnbManifestUrl}, {CnbPreviewManifestUrl, GitHubReleaseListUrl, CnbManifestUrl, GitHubLatestUrl}))
        For Each Endpoint In Endpoints
            Try
                Dim Cnb = Endpoint = CnbManifestUrl OrElse Endpoint = CnbPreviewManifestUrl
                Dim Info As JObject = Nothing
                If Endpoint = GitHubReleaseListUrl Then
                    For Each Candidate In Transport.ReadReleaseList(Endpoint).OfType(Of JObject)().Take(30)
                        Try
                            ValidateReleaseInfo(Candidate, False, Channel)
                            If Info Is Nothing OrElse CompareReleaseVersions(Candidate("tag_name").ToString(), Info("tag_name").ToString()) > 0 Then Info = Candidate
                        Catch ex As Exception
                            ex.ThrowIfCanceled()
                        End Try
                    Next
                    If Info Is Nothing Then Throw New Exception("GitHub 尚无符合当前通道的完整发布。")
                Else
                    Info = Transport.ReadJson(Endpoint)
                    ValidateReleaseInfo(Info, Cnb, Channel)
                End If
                If Best Is Nothing OrElse CompareReleaseVersions(Info("tag_name").ToString(), Best("tag_name").ToString()) > 0 Then Best = Info
                If Source = UpdateSource.GitHub AndAlso Not Cnb Then Return Info
                If NewerReleaseForChannel(Info("tag_name").ToString(), Current, Channel) Then Return Info
            Catch ex As Exception
                ex.ThrowIfCanceled()
                Failure = ex
                Logger.Info("更新源暂不可用：" & If(Endpoint = CnbManifestUrl OrElse Endpoint = CnbPreviewManifestUrl, "CNB", "GitHub"))
            End Try
        Next
        If Best IsNot Nothing Then Return Best
        Throw New Exception("CNB 与 GitHub 更新源均未返回可用发布。", Failure)
    End Function

    Friend Function DownloadUpdate(Info As JObject, Staged As String, Optional Transport As UpdateTransport = Nothing) As String
        If Transport Is Nothing Then Transport = New UpdateTransport
        Dim Cnb = (New Uri(ReleaseExecutable(Info)("browser_download_url").ToString())).Host = "cnb.cool"
        ValidateReleaseInfo(Info, Cnb, UpdateChannel.StableAndPrerelease)
        Dim ExpectedHash As String = Nothing
        Try
            ExpectedHash = ManifestHash(Transport.ReadText(HashAsset(Info)("browser_download_url").ToString()), ReleaseExecutable(Info)("name").ToString())
            DownloadCandidate(Info, Staged, ExpectedHash, Transport)
            Return ExpectedHash
        Catch ex As Exception
            ex.ThrowIfCanceled()
            Logger.Info(If(Cnb, "CNB", "GitHub") & " 下载未完成，尝试备用源的同版本附件。")
        End Try
        Dim Fallback = ReleaseForDownloadSource(Info, If(Cnb, UpdateSource.GitHub, UpdateSource.Cnb), Transport)
        Dim FallbackHash = ManifestHash(Transport.ReadText(HashAsset(Fallback)("browser_download_url").ToString()), ReleaseExecutable(Fallback)("name").ToString())
        If ExpectedHash IsNot Nothing AndAlso Not String.Equals(ExpectedHash, FallbackHash, StringComparison.OrdinalIgnoreCase) Then Throw New Exception("CNB 与 GitHub 的更新校验值不一致，已停止更新。")
        DownloadCandidate(Fallback, Staged, FallbackHash, Transport)
        Return FallbackHash
    End Function

    Friend Function ReleaseForDownloadSource(Info As JObject, Source As UpdateSource, Optional Transport As UpdateTransport = Nothing) As JObject
        If Transport Is Nothing Then Transport = New UpdateTransport
        Dim Cnb = (New Uri(ReleaseExecutable(Info)("browser_download_url").ToString())).Host = "cnb.cool"
        ValidateReleaseInfo(Info, Cnb, UpdateChannel.StableAndPrerelease)
        If Cnb = (Source = UpdateSource.Cnb) Then Return DirectCast(Info.DeepClone(), JObject)
        Dim Selected As JObject
        If Source = UpdateSource.GitHub Then
            Selected = Transport.ReadJson("https://api.github.com/repos/" & Repository & "/releases/tags/" & Uri.EscapeDataString(Info("tag_name").ToString()))
        Else
            Selected = DirectCast(Info.DeepClone(), JObject)
            For Each Asset As JObject In DirectCast(Selected("assets"), JArray).OfType(Of JObject)()
                Dim Name = Asset("name")?.ToString()
                If Name Is Nothing OrElse System.IO.Path.GetFileName(Name) <> Name Then Throw New Exception("备用附件名称无效。")
                Asset("browser_download_url") = LauncherCnbUrl & "/-/releases/download/" & Uri.EscapeDataString(Selected("tag_name").ToString()) & "/" & Uri.EscapeDataString(Name)
            Next
        End If
        ValidateReleaseInfo(Selected, Source = UpdateSource.Cnb, UpdateChannel.StableAndPrerelease)
        If Selected("tag_name").ToString() <> Info("tag_name").ToString() Then Throw New Exception("所选源的发布版本与已确认版本不一致。")
        Return Selected
    End Function

    Friend Function CreateUpdateDownloadTask(Url As String, Target As String, Check As FileChecker) As LoaderCombo(Of Integer)
        Dim Source = If((New Uri(Url)).Host = "cnb.cool", "CNB", "GitHub")
        Dim Version = System.Text.RegularExpressions.Regex.Match(Url, "/(v[0-9.]+(?:-[0-9A-Za-z.-]+)?)/", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups(1).Value
        Dim Download As New LoaderDownload("下载启动器更新", {New NetFile({Url}, Target, Check)}) With {.ProgressWeight = 1}
        Return New LoaderCombo(Of Integer)("下载启动器更新：" & Version & "（" & Source & "）", New List(Of LoaderBase) From {Download}) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
    End Function

    Private Sub DownloadWithTask(Url As String, Target As String, Check As FileChecker)
        Dim Work = CreateUpdateDownloadTask(Url, Target, Check)
        If FrmMain Is Nothing Then
            Work.WaitForExit()
        Else
            RunInUiWait(Sub()
                Work.Start(1)
                LoaderTaskbarAdd(Work)
                FrmMain.BtnExtraDownload.ShowRefresh()
                FrmMain.BtnExtraDownload.Ribble()
            End Sub)
            Work.WaitForExit(StartIfNeeded:=False)
        End If
    End Sub

    Private Sub DownloadCandidate(Info As JObject, Staged As String, Hash As String, Transport As UpdateTransport)
        Dim Asset = ReleaseExecutable(Info)
        Transport.Download(Asset("browser_download_url").ToString(), Staged, New FileChecker With {.Hash = Hash, .ActualSize = CLng(Asset("size"))})
        ValidateExecutable(Staged, Hash)
    End Sub

    Friend Sub CheckForUpdates(Startup As Boolean)
        If Interlocked.CompareExchange(CheckRunning, 1, 0) <> 0 Then
            If Not Startup Then Hint("正在检查更新，请稍候……")
            Return
        End If
        Dim Source = PreferredUpdateSource()
        If Not Startup Then Hint("正在通过 " & UpdateSourceName(Source) & " 检查 PCL Living Fork 更新……")
        RunInNewThread(Sub()
            Try
                Dim Info As JObject = FindLatestReleaseForSource(VersionBaseName, Source)
                Dim Tag = Info("tag_name")?.ToString()
                If Not NewerReleaseForChannel(Tag, VersionBaseName, PreferredUpdateChannel()) Then
                    If Not Startup Then MyMsgBox($"当前版本：{VersionBaseName}{vbCrLf}当前通道最新版本：{Tag}{vbCrLf}{vbCrLf}当前没有更新的版本。", "PCL Living Fork 更新")
                    Return
                End If
                Dim IgnoredTag = Settings.Get(Of String)("ToolLauncherUpdateIgnoredTag")
                If Startup AndAlso Not ShouldPromptForUpdate(Tag, VersionBaseName, IgnoredTag) Then Return
                Dim View As UpdatePromptView = Nothing, DownloadSource = Source
                Dim Choice = MyMsgBoxConfirmContent("请选择本次更新的下载源。", "发现 PCL Living Fork 新版本", "下载并更新", Function()
                    View = New UpdatePromptView(Tag, Source) : Return View
                End Function, "暂不更新", IsWarn:=False)
                If Choice = 1 Then
                    RunInUiWait(Sub() DownloadSource = View.SelectedSource)
                    Settings.Set("ToolLauncherUpdateIgnoredTag", IgnoreTagAfterChoice(IgnoredTag, Tag, Choice))
                    DownloadAndUpdate(Info, DownloadSource)
                ElseIf Choice = 2 Then
                    Settings.Set("ToolLauncherUpdateIgnoredTag", IgnoreTagAfterChoice(IgnoredTag, Tag, Choice))
                End If
            Catch ex As Exception
                If Startup Then
                    Logger.Info("启动更新检查未完成，将在下次启动时重试。")
                Else
                    UpdateFailure(ex.Message)
                End If
            Finally
                Interlocked.Exchange(CheckRunning, 0)
            End Try
        End Sub, "PCL Living Fork 更新检查", ThreadPriority.BelowNormal)
    End Sub

    Private Sub DownloadAndUpdate(Info As JObject, Source As UpdateSource)
        Try
            Dim Folder = System.IO.Path.Combine(Paths.Base, "PCL", "Updates")
            Directory.CreateDirectory(Folder)
            Dim Staged = System.IO.Path.Combine(Folder, LauncherExecutableName)
            Hint("正在下载 PCL Living Fork 更新……")
            Dim ExpectedHash = DownloadUpdate(ReleaseForDownloadSource(Info, Source), Staged)
            If HasDownloadingTask() Then Throw New Exception("仍有下载任务正在进行。请等待任务结束后重试更新。")
            Dim Target = Process.GetCurrentProcess().MainModule.FileName
            Dim Arguments = $"--livingpcl-update {Process.GetCurrentProcess().Id} ""{System.IO.Path.GetFileName(Target)}"" {ExpectedHash}"
            Dim Helper = Process.Start(New ProcessStartInfo(Staged, Arguments) With {.UseShellExecute = False, .CreateNoWindow = True, .WindowStyle = ProcessWindowStyle.Hidden, .WorkingDirectory = Folder})
            If Helper Is Nothing Then Throw New Exception("无法启动更新替换程序。")
            If Helper.WaitForExit(500) Then Throw New Exception("更新替换程序未能正常启动。")
            RunInUi(Sub() FrmMain.EndProgram(SendWarning:=False))
        Catch ex As OperationCanceledException
            Hint("已取消启动器更新。")
        Catch ex As ThreadInterruptedException
            Hint("已取消启动器更新。")
        Catch ex As Exception
            UpdateFailure(ex.Message)
        End Try
    End Sub

    Private Sub UpdateFailure(Reason As String)
        Dim GitHubFirst = PreferredUpdateSource() = UpdateSource.GitHub
        Select Case MyMsgBox("更新未完成，现有启动器已保留。" & vbCrLf & vbCrLf & Reason, "PCL Living Fork 更新失败", If(GitHubFirst, "GitHub 下载页", "国内下载页"), If(GitHubFirst, "CNB 备用", "GitHub 备用"), "取消", IsWarn:=True)
            Case 1 : OpenWebsite(If(GitHubFirst, ReleasePage, LauncherCnbUrl & "/-/releases"))
            Case 2 : OpenWebsite(If(GitHubFirst, LauncherCnbUrl & "/-/releases", ReleasePage))
        End Select
    End Sub

    Friend Sub ValidateExecutable(FileName As String, ExpectedHash As String)
        If Not System.Text.RegularExpressions.Regex.IsMatch(If(ExpectedHash, ""), "\A[0-9a-fA-F]{64}\z") Then Throw New Exception("更新校验值无效。")
        Using Stream = System.IO.File.OpenRead(FileName), Hash = SHA256.Create()
            Dim Actual = BitConverter.ToString(Hash.ComputeHash(Stream)).Replace("-", "")
            If Not Actual.Equals(ExpectedHash, StringComparison.OrdinalIgnoreCase) Then Throw New Exception("更新文件 SHA256 校验失败。")
        End Using
        If Not IsLivingExecutable(FileName) Then Throw New Exception("更新文件不是 PCL Living Fork 启动器。")
    End Sub

    '兼容新旧构建的程序集名称，且产品信息须属于 LivingPCL，避免覆盖官方程序。
    Friend Function IsLivingExecutable(FileName As String) As Boolean
        Dim Identity = AssemblyName.GetAssemblyName(FileName).Name
        Return (Identity = "LivingPCL" OrElse Identity = "Plain Craft Launcher 2") AndAlso
            {"LivingPCL", LauncherDisplayName}.Contains(FileVersionInfo.GetVersionInfo(FileName).ProductName, StringComparer.Ordinal)
    End Function

    '沿用官方 PCL 的流程：运行已下载的新 EXE，由新进程替换退出后的旧 EXE。
    Friend Sub ReplaceExecutable(Staged As String, Target As String, ExpectedHash As String, Restart As Boolean)
        ValidateExecutable(Staged, ExpectedHash)
        Dim Temporary = Target & ".updating-" & Guid.NewGuid().ToString("N")
        Dim Backup = Target & ".update-backup-" & Guid.NewGuid().ToString("N")
        Dim Replaced As Boolean = False
        Dim HadTarget = System.IO.File.Exists(Target)
        If HadTarget AndAlso Not IsLivingExecutable(Target) Then Throw New Exception("更新目标不是 PCL Living Fork 启动器。")
        Try
            System.IO.File.Copy(Staged, Temporary)
            ValidateExecutable(Temporary, ExpectedHash)
            If HadTarget Then
                System.IO.File.Replace(Temporary, Target, Backup, True)
            Else
                System.IO.File.Move(Temporary, Target)
            End If
            Replaced = True
            If Restart Then
                Dim Started = Process.Start(New ProcessStartInfo(Target) With {.UseShellExecute = True, .WorkingDirectory = System.IO.Path.GetDirectoryName(Target)})
                If Started Is Nothing OrElse Started.WaitForExit(3000) Then Throw New Exception("新版本启动失败。")
            End If
            If System.IO.File.Exists(Backup) Then System.IO.File.Delete(Backup)
        Catch
            If Replaced Then
                If HadTarget AndAlso System.IO.File.Exists(Backup) Then
                    System.IO.File.Copy(Backup, Target, True)
                    System.IO.File.Delete(Backup)
                ElseIf Not HadTarget AndAlso System.IO.File.Exists(Target) Then
                    System.IO.File.Delete(Target)
                End If
            End If
            Throw
        Finally
            If System.IO.File.Exists(Temporary) Then System.IO.File.Delete(Temporary)
        End Try
    End Sub

    Friend Function HandleUpdateCommand(Arguments As String()) As Boolean
        If Arguments.Length = 0 OrElse Arguments(0) <> "--livingpcl-update" Then Return False
        Try
            If Arguments.Length <> 4 Then Throw New Exception("更新参数不完整。")
            Dim ParentId As Integer
            If Not Integer.TryParse(Arguments(1), ParentId) OrElse ParentId <= 0 Then Throw New Exception("更新进程号无效。")
            Dim Staged = Assembly.GetExecutingAssembly().Location
            Dim UpdateFolder = System.IO.Path.GetDirectoryName(Staged)
            Dim PclFolder = System.IO.Path.GetDirectoryName(UpdateFolder)
            If Not System.IO.Path.GetFileName(UpdateFolder).Equals("Updates", StringComparison.OrdinalIgnoreCase) OrElse
                Not System.IO.Path.GetFileName(PclFolder).Equals("PCL", StringComparison.OrdinalIgnoreCase) Then Throw New Exception("更新程序位置无效。")
            Dim Name = Arguments(2)
            If System.IO.Path.GetFileName(Name) <> Name OrElse Not Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) Then Throw New Exception("更新目标无效。")
            Dim Target = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PclFolder), Name)
            ValidateExecutable(Staged, Arguments(3))
            Dim Parent As Process = Nothing
            Try
                Parent = Process.GetProcessById(ParentId)
            Catch ex As ArgumentException
            End Try
            If Parent IsNot Nothing Then
                If Not String.Equals(Parent.MainModule.FileName, Target, StringComparison.OrdinalIgnoreCase) Then Throw New Exception("更新进程与目标不一致。")
                If Not Parent.WaitForExit(20000) Then Throw New Exception("原启动器尚未退出。")
            End If
            ReplaceExecutable(Staged, Target, Arguments(3), True)
        Catch ex As Exception
            If MessageBox.Show("自动更新未完成，请从发布页手动更新。" & vbCrLf & ex.Message, "PCL Living Fork 更新失败", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
                Process.Start(New ProcessStartInfo(ReleasePage) With {.UseShellExecute = True})
            End If
        End Try
        Environment.Exit(0)
        Return True
    End Function
End Module
