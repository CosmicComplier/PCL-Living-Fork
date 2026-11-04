Imports System.Collections.ObjectModel
Imports System.IO.Compression
Imports System.Net.Http
Imports System.Threading
Imports InstallCore = PCLCS.ModpackInstallEngine(Of PCL.ModLoader.LoaderBase, PCL.ModNet.NetFile, PCL.ResourceVersion, PCL.ResourceProject, PCL.ExportedVersionIsolation.Choice, PCL.ModDownload.DlOptiFineListEntry)

'LF 原 API、Loader/Dialogs/模型与平台原语接线；格式识别、计划与实际负载由 C# 执行。
Public Module ModModpack
    Private ReadOnly Service As New InstallCore(CreateHost())

    Private Function CreateHost() As PCLCS.ModpackInstallHost(Of LoaderBase, NetFile, ResourceVersion, ResourceProject, ExportedVersionIsolation.Choice, DlOptiFineListEntry)
        Return New PCLCS.ModpackInstallHost(Of LoaderBase, NetFile, ResourceVersion, ResourceProject, ExportedVersionIsolation.Choice, DlOptiFineListEntry) With {
            .PackMode = Function() ModModpackMode.Enabled,
            .BaseFolder = Function() Paths.Base,
            .MinecraftFolder = Function() McFolderSelected,
            .RequestTempFolder = Function() RequestTaskTempFolder(),
            .SelectPackFile = Function() Dialogs.SelectFile("选择整合包压缩文件", False, filter:={({"rar", "zip", "mrpack"}, "整合包文件")}).FirstOrDefault(),
            .SelectInstallFolder = Function() Dialogs.SelectFolder("选择安装目标（必须是一个空文件夹）", False).FirstOrDefault(),
            .RunInThread = Sub(Work) RunInThread(Work),
            .RunInUi = Sub(Work) RunInUi(Work),
            .Alert = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.Alert),
            .Hint = Sub(Message, Red) Hint(Message, If(Red, HintType.Red, HintType.Blue)),
            .Message = Function(Message) MyMsgBox(Message.Text, Message.Title, Message.Button1, Message.Button2, IsWarn:=Message.IsWarn, ForceWait:=Message.ForceWait),
            .NameValidator = AddressOf CreateNameValidator,
            .WriteIni = Sub(Name, Key, Value) WriteIni(Name, Key, CStr(Value)),
            .ClearIniCache = AddressOf IniClearCache,
            .ReadIni = Function(Name, Key, DefaultValue) ReadIni(Name, Key, DefaultValue),
            .ReadIniBoolean = Function(Name, Key, DefaultValue) CBool(ReadIni(Name, Key, CStr(DefaultValue))),
            .SettingString = Function(Key) Settings.Get(Of String)(Key),
            .ReadIsolation = AddressOf ExportedVersionIsolation.ReadArchiveChoice,
            .GameFolder = AddressOf ExportedVersionIsolation.GameFolder,
            .ConfigureIsolation = AddressOf ExportedVersionIsolation.ConfigureTarget,
            .CopyIsolatedOverrides = AddressOf ExportedVersionIsolation.CopyOverrides,
            .OverrideTarget = AddressOf ExportedVersionIsolation.OverrideTarget,
            .ModRequest = Function(Url, Method, Content, ContentType) DlModRequest(Url, Method, Content, ContentType),
            .CurseForgeProjects = Function(Ids) ResourceProject.FromProjectIds(Ids, ResourcePlatforms.CurseForge),
            .ParseVersion = Function(Raw, Type) ResourceVersion.FromPlatformJson(DirectCast(Raw, JObject), Type),
            .VersionToNetFile = Function(Version, Target, GameVersion, Loaders) Version.ToNetFile(Target, ResourceVersion.DownloadReason.ModPack, GameVersion, Loaders),
            .NetFile = Function(Urls, Target, Hash) New NetFile(Urls, Target, New FileChecker With {.Hash = Hash}, True),
            .CurseForgeUrls = AddressOf ResourceVersion.ParseCurseForgeDownloadUrls,
            .TrackUrls = Function(Urls, GameVersion, Loaders) ResourceVersion.ParseModrinthTrackArguments(Urls, ResourceVersion.DownloadReason.ModPack, GameVersion, Loaders),
            .ModMirror = AddressOf DlSourceModGet,
            .TextIntTask = Function(Name, Work, Options) NewTask(Of String, Integer)(Name, Work, Options),
            .IntJsonTask = Function(Name, Work, Options) NewTask(Of Integer, JArray)(Name, Work, Options),
            .JsonFilesTask = Function(Name, Work, Options) NewTask(Of JArray, List(Of NetFile))(Name, Work, Options),
            .TextTextTask = Function(Name, Work, Options) NewTask(Of String, String)(Name, Work, Options),
            .TextCombo = AddressOf NewTextCombo,
            .IntCombo = AddressOf NewIntCombo,
            .Download = AddressOf NewDownload,
            .Weight = Function(Loader) Loader.ProgressWeight,
            .InstallGame = AddressOf InstallGame,
            .TaskbarNames = Function() LoaderTaskbar.Select(Function(Loader) Loader.Name),
            .AttachInstallState = Sub(Loader) Loader.OnStateChanged = AddressOf McInstallState,
            .AddTaskbar = Sub(Loader) LoaderTaskbarAdd(DirectCast(Loader, LoaderCombo(Of String))),
            .Start = Sub(Loader, Folder) Loader.Start(Folder),
            .RefreshDownload = Sub() FrmMain.BtnExtraDownload.ShowRefresh(),
            .RibbleDownload = Sub() FrmMain.BtnExtraDownload.Ribble(),
            .RecordInstalled = AddressOf ModpackUpdate.RecordInstalled,
            .OptiFineFinished = Function() DlOptiFineListBmclapiLoader.State = LoadState.Finished,
            .WaitOptiFine = Sub() DlOptiFineListBmclapiLoader.WaitForExit(),
            .OptiFineEntries = Function() DlOptiFineListBmclapiLoader.Output.Value,
            .OpenExplorer = Sub(Folder) OpenExplorer(Folder),
            .StartProcess = Sub(Name, Arguments) StartProcess(Name, Arguments),
            .EndProgram = Sub(Value) FrmMain.EndProgram(Value),
            .AddFolder = Sub(Folder, Name, ShowHint) PageSelectLeft.AddFolder(Folder, Name, ShowHint),
            .SelectInstancePage = Sub() FrmMain.PageChange(FormMain.PageType.InstanceSelect),
            .Sleep = AddressOf Thread.Sleep}
    End Function
    Private Function CreateNameValidator(Folder As String) As PCLCS.ModpackNameValidator
        Dim Rule As New ValidateFolderName(Folder)
        Return New PCLCS.ModpackNameValidator With {
            .Validate = Function(Value) Rule.Validate(Value),
            .Prompt = Function() MyMsgBoxInput("输入版本名称", "", "", New Collection(Of Validate) From {Rule})}
    End Function
    Private Function InstallGame(Name As String, Folder As String, Components As McComponents) As List(Of LoaderBase)
        Return McInstallLoadersGet(New McInstallRequest With {.NewInstanceName = Name, .InstanceFolder = Folder, .Components = Components})
    End Function
    Private Function ApplyOptions(Loader As LoaderBase, Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
        If Options.ProgressWeight.HasValue Then Loader.ProgressWeight = Options.ProgressWeight.Value
        If Options.Block.HasValue Then Loader.Block = Options.Block.Value
        If Options.Show.HasValue Then Loader.Show = Options.Show.Value
        Return Loader
    End Function
    Private Function NewTask(Of TInput, TOutput)(Name As String, Work As Action(Of PCLCS.ModpackInstallTask(Of TInput, TOutput)), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
        Return ApplyOptions(New LoaderTask(Of TInput, TOutput)(Name, Sub(Task)
            Work(New PCLCS.ModpackInstallTask(Of TInput, TOutput) With {
                .Input = Function() Task.Input, .Output = Sub(Value) Task.Output = Value,
                .Cancellation = Function() Task.CreateCancellationToken(),
                .ProgressProvider = Function(ToPercentage) Task.CreateSyncProgressProvider(ToPercentage:=ToPercentage),
                .Progress = Function() Task.Progress, .SetProgress = Sub(Value) Task.Progress = Value,
                .Cancel = AddressOf Task.Cancel})
        End Sub), Options)
    End Function
    Private Function NewTextCombo(Name As String, Loaders As List(Of LoaderBase), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
        Return ApplyOptions(New LoaderCombo(Of String)(Name, Loaders), Options)
    End Function
    Private Function NewIntCombo(Name As String, Loaders As List(Of LoaderBase), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
        Return ApplyOptions(New LoaderCombo(Of Integer)(Name, Loaders), Options)
    End Function
    Private Function NewDownload(Name As String, Files As List(Of NetFile), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
        Return ApplyOptions(New LoaderDownload(Name, Files), Options)
    End Function

    Public Sub ModpackInstall()
        Service.ModpackInstall()
    End Sub
    Public Function ModpackInstall(File As String, Optional InstanceName As String = Nothing, Optional Logo As String = Nothing, Optional Source As JObject = Nothing) As LoaderCombo(Of String)
        Return DirectCast(Service.ModpackInstall(File, InstanceName, Logo, Source), LoaderCombo(Of String))
    End Function
    Private Sub ExtractModpackFiles(InstallTemp As String, FileAddress As String, Optional c As CancellationToken = Nothing, Optional p As ProgressProvider = Nothing)
        Service.ExtractModpackFiles(InstallTemp, FileAddress, c, p)
    End Sub
    Private Sub CopyOverrideDirectory(OverridesFolder As String, VersionFolder As String, Loader As LoaderBase,
        Optional Isolation As ExportedVersionIsolation.Choice = Nothing, Optional GameFolder As String = Nothing)
        Service.CopyOverrideDirectory(OverridesFolder, VersionFolder, Loader, Isolation, GameFolder)
    End Sub
    Private Sub NotifyIncompatibleLoader(LoaderName As String)
        Service.NotifyIncompatibleLoader(LoaderName)
    End Sub
    Private Function InstallPackCurseForge(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String,
        Optional InstanceName As String = Nothing, Optional Logo As String = Nothing, Optional Source As JObject = Nothing) As LoaderCombo(Of String)
        Return DirectCast(Service.InstallPackCurseForge(FileAddress, Archive, ArchiveBaseFolder, InstanceName, Logo, Source), LoaderCombo(Of String))
    End Function
    Private Function InstallPackModrinth(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String,
        Optional InstanceName As String = Nothing, Optional Logo As String = Nothing, Optional Source As JObject = Nothing) As LoaderCombo(Of String)
        Return DirectCast(Service.InstallPackModrinth(FileAddress, Archive, ArchiveBaseFolder, InstanceName, Logo, Source), LoaderCombo(Of String))
    End Function
    Private Function InstallPackHMCL(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String) As LoaderCombo(Of String)
        Return DirectCast(Service.InstallPackHMCL(FileAddress, Archive, ArchiveBaseFolder), LoaderCombo(Of String))
    End Function
    Private Function InstallPackMMC(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String) As LoaderCombo(Of String)
        Return DirectCast(Service.InstallPackMMC(FileAddress, Archive, ArchiveBaseFolder), LoaderCombo(Of String))
    End Function
    Private Function InstallPackMCBBS(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String, Optional InstanceName As String = Nothing) As LoaderCombo(Of String)
        Return DirectCast(Service.InstallPackMCBBS(FileAddress, Archive, ArchiveBaseFolder, InstanceName), LoaderCombo(Of String))
    End Function
    Private Function InstallPackLauncherPack(FileAddress As String, Archive As ZipArchive, ArchiveBaseFolder As String) As LoaderCombo(Of String)
        Return DirectCast(Service.InstallPackLauncherPack(FileAddress, Archive, ArchiveBaseFolder), LoaderCombo(Of String))
    End Function
    Private Function InstallPackCompress(FileAddress As String, Archive As ZipArchive) As LoaderCombo(Of String)
        Return DirectCast(Service.InstallPackCompress(FileAddress, Archive), LoaderCombo(Of String))
    End Function
End Module
