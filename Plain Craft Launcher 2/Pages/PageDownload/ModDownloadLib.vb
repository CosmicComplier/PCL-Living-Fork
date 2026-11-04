Imports System.Collections.Generic
Imports System.Linq
Imports System.Net.Http
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports InstallCore = PCLCS.MinecraftInstallEngine(Of PCL.ModLoader.LoaderBase, PCL.ModNet.NetFile, PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.ModDownload.DlOptiFineListEntry, PCL.ModDownload.DlForgelikeEntry, PCL.ModDownload.DlForgeVersionEntry, PCL.ModDownload.DlNeoForgeListEntry, PCL.ModDownload.DlLiteLoaderListEntry, PCL.ModMinecraft.McLibEntry, PCL.ResourceVersion, PCL.ModDownloadLib.McInstallRequest)
Imports MenuCore = PCLCS.MinecraftInstallMenuController(Of PCL.MyListItem, PCL.MyVirtualizingElement(Of PCL.MyListItem), PCL.MyListItem.ClickEventHandler, PCL.ModDownload.DlOptiFineListEntry, PCL.ModDownload.DlForgelikeEntry, PCL.ModDownload.DlForgeVersionEntry, PCL.ModDownload.DlNeoForgeListEntry, PCL.ModDownload.DlLiteLoaderListEntry, PCL.ResourceVersion)

'LF 原公开类型与 WPF/Loader/平台适配；完整安装、进程、合并/修改、清理与菜单规则在 C# 中。
Public Module ModDownloadLib
    Public Class McInstallRequest
        Inherits PCLCS.MinecraftInstallRequestData(Of DlForgeVersionEntry, DlNeoForgeListEntry, ResourceVersion, DlLiteLoaderListEntry)
    End Class
    Private ReadOnly NativeLoaders As New InstallLoaderFactory
    Private ReadOnly Service As New InstallCore(CreateHost())
    Private ReadOnly Menus As New MenuCore(CreateMenuHost())

    Private Function CreateHost() As PCLCS.MinecraftInstallHost(Of LoaderBase, NetFile, McInstance, DlForgeVersionEntry, DlNeoForgeListEntry, ResourceVersion, McLibEntry)
        Return New PCLCS.MinecraftInstallHost(Of LoaderBase, NetFile, McInstance, DlForgeVersionEntry, DlNeoForgeListEntry, ResourceVersion, McLibEntry) With {
            .Loaders = NativeLoaders,
            .States = New PCLCS.MinecraftInstallStates With {
                .Waiting = CInt(LoadState.Waiting), .Loading = CInt(LoadState.Loading), .Finished = CInt(LoadState.Finished),
                .Failed = CInt(LoadState.Failed), .Canceled = CInt(LoadState.Canceled)},
            .MinecraftFolder = Function() McFolderSelected,
            .TempFolder = Function() PathTemp,
            .PureFolder = Function() PathPure.Value,
            .RequestTempFolder = Function(Require) RequestTaskTempFolder(Require),
            .NewInstance = Function(Folder) New McInstance(Folder),
            .LibraryFiles = AddressOf McLibNetFilesFromInstance,
            .AssetFiles = Function(Instance, Hash, Loader) McAssetsFixList(Instance, Hash, Loader),
            .AssetIndex = AddressOf DlClientAssetIndexGet,
            .ClientJar = AddressOf DlClientJarGet,
            .ClientJsonUrl = Function(Id) CStr(DlClientListGet(Id)),
            .LauncherUrls = AddressOf DlSourceLauncherOrMetaGet,
            .NetFile = AddressOf NewInstallFile,
            .LocalPath = Function(File) File.LocalPath,
            .ResourceNetFile = Function(Version, Target, GameVersion, Loaders) Version.ToNetFile(Target, ResourceVersion.DownloadReason.Dependency, GameVersion, Loaders),
            .RetryRequest = Function(Request) NetRequestByClientRetry(Request.Url, Request.Method, Content:=Request.Content, ContentType:=Request.ContentType,
                Accept:=Request.Accept, RequireJson:=Request.RequireJson, Encoding:=Request.Encoding, SimulateBrowserHeaders:=Request.SimulateBrowserHeaders),
            .LoaderRequest = Function(Urls, Json) NetRequestByLoader(Urls, IsJson:=Json),
            .NeoForge = Function(Name) New DlNeoForgeListEntry(Name),
            .ForgeList = AddressOf ReadForgeList,
            .ReadLibraryList = Function(Json, Keep, Folder) McLibListGetWithJson(Json, Keep, Folder),
            .LibraryNetFiles = AddressOf McLibNetFilesFromTokens,
            .LibraryPath = Function(Original, Folder) McLibGet(Original, CustomMcFolder:=Folder),
            .CreateLauncherProfiles = AddressOf McFolderLauncherProfilesJsonCreate,
            .SelectJava = Function(Range, Token, Progress) SelectOrDownloadJava(Range, True, Token, Progress),
            .BooleanSetting = Function(Key) Settings.Get(Of Boolean)(Key),
            .ExtractResources = Sub(Name, ResourceName) ExtractResources(Name, ResourceName),
            .RunInThread = Sub(Work) RunInThread(Work),
            .RunInUi = Sub(Work) RunInUi(Work),
            .RunInUiWait = Sub(Work) RunInUiWait(Work),
            .RunInNewThread = Sub(Work, Name) RunInNewThread(Work, Name),
            .RefreshDownload = Sub() FrmMain.BtnExtraDownload.ShowRefresh(),
            .RibbleDownload = Sub() FrmMain.BtnExtraDownload.Ribble(),
            .RefreshInstanceList = Sub() LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\"),
            .Hint = AddressOf InstallHint,
            .Message = Function(Message) MyMsgBox(Message.Text, Message.Title, Message.Button1, Message.Button2, IsWarn:=Message.IsWarn, ForceWait:=Message.ForceWait),
            .SaveFile = Function(Name, Extension, Description) Dialogs.SaveFile("选择保存位置", Name, filter:={(Extension, Description)}),
            .SelectFolder = Function() Dialogs.SelectFolder("选择目标文件夹", False).FirstOrDefault(),
            .OpenWebsite = Sub(Url) OpenWebsite(Url),
            .WriteIni = Sub(Name, Key, Value) WriteIni(Name, Key, CStr(Value)),
            .NativesFolder = Function(Instance) GetNativesFolder(Instance),
            .FileWorkload = Function(Loader, Input) LoaderFileOperation.CreateWorkload(DirectCast(Loader, LoaderCombo(Of String)), Input),
            .TryAcquire = AddressOf McInstanceLock.TryAcquire,
            .InstancePage = Function() PageInstanceLeft.Instance,
            .SelectedInstance = Function() McInstanceSelected,
            .SetInstancePage = Sub(Instance) PageInstanceLeft.Instance = Instance,
            .SetSelectedInstance = Sub(Instance) McInstanceSelected = Instance,
            .BeforeMinecraftChange = Sub(Instance, Before, After, Token) WorldBackups.BeforeMinecraftChange(Instance, Before, After, Token),
            .CaptureEnvironment = Sub(Instance, Reason, Token) EnvironmentHistory.CaptureInstance(Instance, Reason, Token),
            .CleanupState = Function(Loader) CInt(Loader.State),
            .CleanupInput = Function(Loader) CStr(Loader.Input)}
    End Function
    Private Sub InstallHint(Text As String, Kind As Integer)
        Hint(Text, If(Kind = 1, HintType.Red, If(Kind = 2, HintType.Green, HintType.Blue)))
    End Sub
    Private Function NewInstallFile(Urls As IEnumerable(Of String), Name As String, Checker As PCLCS.MinecraftDownloadCheck) As NetFile
        Return New NetFile(Urls, Name, If(Checker Is Nothing, Nothing, New FileChecker With {
            .MinSize = Checker.MinSize, .ActualSize = Checker.ActualSize, .Hash = Checker.Hash, .IsJson = Checker.IsJson}))
    End Function
    Private Function ReadForgeList(Inherit As String) As List(Of DlForgeVersionEntry)
        Dim Loader = New LoaderTask(Of String, List(Of DlForgeVersionEntry))("McDownloadForgeLoader " & Inherit, AddressOf DlForgeVersionMain)
        Loader.WaitForExit(Inherit)
        Return Loader.Output
    End Function

    Private Class InstallLoaderFactory
        Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile)
        Private Shared Function ApplyOptions(Loader As LoaderBase, Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
            If Options.ProgressWeight.HasValue Then Loader.ProgressWeight = Options.ProgressWeight.Value
            If Options.Show.HasValue Then Loader.Show = Options.Show.Value
            If Options.Block.HasValue Then Loader.Block = Options.Block.Value
            Return Loader
        End Function
        Public Function NewTask(Of TInput, TOutput)(Name As String, Work As Action(Of PCLCS.MinecraftInstallTask(Of TInput, TOutput, LoaderBase)),
            Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).NewTask
            Return ApplyOptions(New LoaderTask(Of TInput, TOutput)(Name, Sub(Task) Work(WrapTask(Of TInput, TOutput)(Task))), Options)
        End Function
        Public Function WrapTask(Of TInput, TOutput)(Loader As LoaderBase) As PCLCS.MinecraftInstallTask(Of TInput, TOutput, LoaderBase) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).WrapTask
            Dim Task = DirectCast(Loader, LoaderTask(Of TInput, TOutput))
            Return New PCLCS.MinecraftInstallTask(Of TInput, TOutput, LoaderBase) With {
                .Native = Task, .ReadInput = Function() Task.Input,
                .ReadOutput = Function() Task.Output, .WriteOutput = Sub(Value) Task.Output = Value,
                .ReadProgress = Function() Task.Progress, .WriteProgress = Sub(Value) Task.Progress = Value,
                .ReadCanceled = Function() Task.IsCanceled, .ReadCanceledState = Function() Task.State = LoadState.Canceled,
                .Cancellation = Function() Task.CreateCancellationToken(),
                .ProgressProvider = Function(FromPercentage, ToPercentage) Task.CreateSyncProgressProvider(FromPercentage, ToPercentage),
                .Cancel = AddressOf Task.Cancel}
        End Function
        Public Function Combo(Of TInput)(Name As String, Steps As List(Of LoaderBase), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Combo
            Return ApplyOptions(New LoaderCombo(Of TInput)(Name, Steps), Options)
        End Function
        Public Function Download(Name As String, Files As List(Of NetFile), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Download
            Return ApplyOptions(New LoaderDownload(Name, Files), Options)
        End Function
        Public Sub Start(Of TInput)(Loader As LoaderBase, Input As TInput) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Start
            DirectCast(Loader, LoaderCombo(Of TInput)).Start(Input)
        End Sub
        Public Sub AddTaskbar(Of TInput)(Loader As LoaderBase) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).AddTaskbar
            LoaderTaskbarAdd(DirectCast(Loader, LoaderCombo(Of TInput)))
        End Sub
        Public Sub OnStateChanged(Loader As LoaderBase, Changed As Action(Of LoaderBase)) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).OnStateChanged
            Loader.OnStateChanged = Changed
        End Sub
        Public Function Name(Loader As LoaderBase) As String Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Name
            Return Loader.Name
        End Function
        Public Function State(Loader As LoaderBase) As Integer Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).State
            Return CInt(Loader.State)
        End Function
        Public Function ErrorValue(Loader As LoaderBase) As Exception Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Error
            Return Loader.Error
        End Function
        Public Function Children(Loader As LoaderBase) As IEnumerable(Of LoaderBase) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Children
            Return DirectCast(Loader, LoaderCombo(Of String)).GetLoaderList()
        End Function
        Public Function TaskbarNames() As IEnumerable(Of String) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).TaskbarNames
            Return LoaderTaskbar.Select(Function(Loader) Loader.Name)
        End Function
    End Class

    Private Function CreateMenuHost() As PCLCS.MinecraftInstallMenuHost(Of MyListItem, MyVirtualizingElement(Of MyListItem), MyListItem.ClickEventHandler, DlOptiFineListEntry, DlForgelikeEntry, DlLiteLoaderListEntry)
        Return New PCLCS.MinecraftInstallMenuHost(Of MyListItem, MyVirtualizingElement(Of MyListItem), MyListItem.ClickEventHandler, DlOptiFineListEntry, DlForgelikeEntry, DlLiteLoaderListEntry) With {
            .Virtualize = Function(Work, Height) New MyVirtualizingElement(Of MyListItem)(Function() Work()) With {.Height = Height},
            .CreateItem = AddressOf CreateMenuItem,
            .Initialize = Function(Item) Item.Init(),
            .Title = Function(Item) Item.Title, .Info = Function(Item) Item.Info,
            .SetInfo = Sub(Item, Value) Item.Info = Value,
            .SetButtons = AddressOf SetMenuButtons,
            .ImageRoot = Function() PathImage,
            .DebugMode = Function() ModeDebug,
            .OpenWebsite = Sub(Url) OpenWebsite(Url),
            .SaveOptiFine = AddressOf Service.McDownloadOptiFineSave,
            .SaveForgelike = AddressOf Service.McDownloadForgelikeSave,
            .SaveLiteLoader = AddressOf Service.McDownloadLiteLoaderSave,
            .SaveMinecraft = AddressOf Service.SaveMinecraft}
    End Function
    Private Function CreateMenuItem(Model As PCLCS.MinecraftInstallListItem, OnClick As MyListItem.ClickEventHandler, Content As Action(Of Object, EventArgs)) As MyListItem
        Dim Item As New MyListItem With {.Logo = Model.Logo, .SnapsToDevicePixels = True, .Title = Model.Title, .Height = 42,
            .Type = MyListItem.CheckType.Clickable, .Tag = Model.Tag, .Info = Model.Info}
        AddHandler Item.Click, OnClick
        If Content IsNot Nothing Then Item.ContentHandler = Sub(Sender, Args) Content(Sender, Args)
        Return Item
    End Function
    Private Sub SetMenuButtons(Sender As Object, Entries As List(Of PCLCS.MinecraftInstallMenuButton))
        Dim Buttons As New List(Of MyIconButton)
        For Each Entry In Entries
            Dim Current = Entry
            Dim Icon As String
            Select Case Entry.Icon
                Case PCLCS.MinecraftInstallMenuIcon.Save : Icon = Logo.IconButtonSave
                Case PCLCS.MinecraftInstallMenuIcon.Info : Icon = Logo.IconButtonInfo
                Case PCLCS.MinecraftInstallMenuIcon.Server : Icon = Logo.IconButtonServer
                Case Else : Icon = Logo.IconButtonList
            End Select
            Dim Button As New MyIconButton With {.Logo = Icon, .ToolTip = Entry.ToolTip, .Tag = Entry.Tag}
            If Entry.LogoScale.HasValue Then Button.LogoScale = Entry.LogoScale.Value
            AddHandler Button.Click, Sub(ButtonSender, Args) Current.Click(ButtonSender, Args)
            Buttons.Add(Button)
        Next
        DirectCast(Sender, MyListItem).Buttons = Buttons
    End Sub

    Private Const McDownloadClientLibName As String = "下载原版支持库文件"
    Private Const McDownloadClientJsonName As String = "下载原版 json 文件"
    Private Property InstallSyncLock As Object
        Get
            Return Service.InstallSyncLock
        End Get
        Set(value As Object)
            Service.InstallSyncLock = value
        End Set
    End Property
    Private Property VanillaSyncLock As Object
        Get
            Return Service.VanillaSyncLock
        End Get
        Set(value As Object)
            Service.VanillaSyncLock = value
        End Set
    End Property
    Private Property IsForgeRecommendedRefreshed As Boolean
        Get
            Return Service.IsForgeRecommendedRefreshed
        End Get
        Set(value As Boolean)
            Service.IsForgeRecommendedRefreshed = value
        End Set
    End Property

    Private Function McDownloadClientLoader(Id As String, JsonUrl As String, VersionFolder As String) As List(Of LoaderBase)
        Return Service.McDownloadClientLoader(Id, JsonUrl, VersionFolder)
    End Function
    Public Function McDownloadListItem(Entry As JObject, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return Menus.McDownloadListItem(Entry, OnClick, IsSaveOnly)
    End Function
    Private Sub McDownloadSaveMenuBuild(sender As Object, e As EventArgs)
        Menus.McDownloadSaveMenuBuild(sender, e)
    End Sub
    Private Sub McDownloadMenuBuild(sender As Object, e As EventArgs)
        Menus.McDownloadMenuBuild(sender, e)
    End Sub
    Private Sub McDownloadMenuLog(sender As Object, e As RoutedEventArgs)
        Menus.McDownloadMenuLog(sender, e)
    End Sub
    Private Sub McDownloadMenuSaveServer(sender As Object, e As RoutedEventArgs)
        Menus.McDownloadMenuSaveServer(sender, e)
    End Sub
    Public Sub McDownloadMenuSave(sender As Object, e As RoutedEventArgs)
        Menus.McDownloadMenuSave(sender, e)
    End Sub
    Public Sub McUpdateLogShow(InstanceJson As JToken)
        Menus.McUpdateLogShow(InstanceJson)
    End Sub
    Private Sub McDownloadOptiFineSave(DownloadInfo As DlOptiFineListEntry)
        Service.McDownloadOptiFineSave(DownloadInfo)
    End Sub
    Private Sub McDownloadOptiFineInstall(BaseMcFolderHome As String, Target As String, Task As LoaderTask(Of List(Of NetFile), Boolean), UseJavaWrapper As Boolean)
        Service.McDownloadOptiFineInstall(BaseMcFolderHome, Target, NativeLoaders.WrapTask(Of List(Of NetFile), Boolean)(Task), UseJavaWrapper)
    End Sub
    Private Function McDownloadOptiFineLoader(DownloadInfo As DlOptiFineListEntry, McFolder As String, ClientDownloadLoader As LoaderCombo(Of String), ClientFolder As String) As List(Of LoaderBase)
        Return Service.McDownloadOptiFineLoader(DownloadInfo, McFolder, ClientDownloadLoader, ClientFolder)
    End Function
    Private Function McDownloadOptiFineSaveLoader(DownloadInfo As DlOptiFineListEntry, TargetFolder As String) As List(Of LoaderBase)
        Return Service.McDownloadOptiFineSaveLoader(DownloadInfo, TargetFolder)
    End Function
    Public Function OptiFineDownloadListItem(Entry As DlOptiFineListEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return Menus.OptiFineDownloadListItem(Entry, OnClick, IsSaveOnly)
    End Function
    Private Sub OptiFineSaveContMenuBuild(sender As Object, e As EventArgs)
        Menus.OptiFineSaveContMenuBuild(sender, e)
    End Sub
    Private Sub OptiFineContMenuBuild(sender As Object, e As EventArgs)
        Menus.OptiFineContMenuBuild(sender, e)
    End Sub
    Private Sub OptiFineLog_Click(sender As Object, e As RoutedEventArgs)
        Menus.OptiFineLog_Click(sender, e)
    End Sub
    Public Sub OptiFineSave_Click(sender As Object, e As RoutedEventArgs)
        Menus.OptiFineSave_Click(sender, e)
    End Sub
    Private Sub McDownloadLiteLoaderSave(DownloadInfo As DlLiteLoaderListEntry)
        Service.McDownloadLiteLoaderSave(DownloadInfo)
    End Sub
    Private Function McDownloadLiteLoaderLoader(DownloadInfo As DlLiteLoaderListEntry, McFolder As String) As List(Of LoaderBase)
        Return Service.McDownloadLiteLoaderLoader(DownloadInfo, McFolder)
    End Function
    Public Function LiteLoaderDownloadListItem(Entry As DlLiteLoaderListEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return Menus.LiteLoaderDownloadListItem(Entry, OnClick, IsSaveOnly)
    End Function
    Private Sub LiteLoaderSaveContMenuBuild(sender As MyListItem, e As EventArgs)
        Menus.LiteLoaderSaveContMenuBuild(sender, e)
    End Sub
    Private Sub LiteLoaderContMenuBuild(sender As MyListItem, e As EventArgs)
        Menus.LiteLoaderContMenuBuild(sender, e)
    End Sub
    Private Sub LiteLoaderAll_Click(sender As Object, e As RoutedEventArgs)
        Menus.LiteLoaderAll_Click(sender, e)
    End Sub
    Public Sub LiteLoaderSave_Click(sender As Object, e As RoutedEventArgs)
        Menus.LiteLoaderSave_Click(sender, e)
    End Sub
    Public Sub McDownloadForgelikeSave(Info As DlForgelikeEntry)
        Service.McDownloadForgelikeSave(Info)
    End Sub
    Private Sub ForgelikeInjector(Target As String, Task As LoaderTask(Of Boolean, Boolean), McFolder As String, UseJavaWrapper As Boolean, IsNeoForge As Boolean)
        Service.ForgelikeInjector(Target, NativeLoaders.WrapTask(Of Boolean, Boolean)(Task), McFolder, UseJavaWrapper, IsNeoForge)
    End Sub
    Private Sub ForgelikeInjectorLine(Content As String, Task As LoaderTask(Of Boolean, Boolean))
        Service.ForgelikeInjectorLine(Content, NativeLoaders.WrapTask(Of Boolean, Boolean)(Task))
    End Sub
    Private Function McDownloadForgelikeLoader(IsNeoForge As Boolean, LoaderVersion As String, NewInstanceName As String, Inherit As String, Info As DlForgelikeEntry, McFolder As String, ClientDownloadLoader As LoaderCombo(Of String), ClientFolder As String) As List(Of LoaderBase)
        Return Service.McDownloadForgelikeLoader(IsNeoForge, LoaderVersion, NewInstanceName, Inherit, Info, McFolder, ClientDownloadLoader, ClientFolder)
    End Function
    Public Sub ForgeDownloadListItemPreload(Stack As StackPanel, Entries As List(Of DlForgeVersionEntry), OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean)
        Menus.ForgeDownloadListItemPreload(Stack, Entries, OnClick, IsSaveOnly)
    End Sub
    Public Function ForgeDownloadListItem(Entry As DlForgeVersionEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return Menus.ForgeDownloadListItem(Entry, OnClick, IsSaveOnly)
    End Function
    Private Sub ForgeContMenuBuild(sender As MyListItem, e As EventArgs)
        Menus.ForgeContMenuBuild(sender, e)
    End Sub
    Private Sub ForgeSaveContMenuBuild(sender As MyListItem, e As EventArgs)
        Menus.ForgeSaveContMenuBuild(sender, e)
    End Sub
    Private Sub ForgeLog_Click(sender As Object, e As RoutedEventArgs)
        Menus.ForgeLog_Click(sender, e)
    End Sub
    Public Sub ForgeSave_Click(sender As Object, e As RoutedEventArgs)
        Menus.ForgeSave_Click(sender, e)
    End Sub
    Public Sub McDownloadForgeRecommendedRefresh()
        Service.McDownloadForgeRecommendedRefresh()
    End Sub
    Public Function McDownloadForgeRecommendedGet(McVersionName As String) As String
        Return Service.McDownloadForgeRecommendedGet(McVersionName)
    End Function
    Public Sub NeoForgeDownloadListItemPreload(Stack As StackPanel, Entries As List(Of DlNeoForgeListEntry), OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean)
        Menus.NeoForgeDownloadListItemPreload(Stack, Entries, OnClick, IsSaveOnly)
    End Sub
    Public Function NeoForgeDownloadListItem(Info As DlNeoForgeListEntry, OnClick As MyListItem.ClickEventHandler, IsSaveOnly As Boolean) As MyVirtualizingElement(Of MyListItem)
        Return Menus.NeoForgeDownloadListItem(Info, OnClick, IsSaveOnly)
    End Function
    Private Sub NeoForgeContMenuBuild(sender As MyListItem, e As EventArgs)
        Menus.NeoForgeContMenuBuild(sender, e)
    End Sub
    Private Sub NeoForgeSaveContMenuBuild(sender As MyListItem, e As EventArgs)
        Menus.NeoForgeSaveContMenuBuild(sender, e)
    End Sub
    Private Sub NeoForgeLog_Click(sender As Object, e As RoutedEventArgs)
        Menus.NeoForgeLog_Click(sender, e)
    End Sub
    Public Sub NeoForgeSave_Click(sender As Object, e As RoutedEventArgs)
        Menus.NeoForgeSave_Click(sender, e)
    End Sub
    Public Sub McDownloadFabricLoaderSave(DownloadInfo As JObject)
        Service.McDownloadFabricLoaderSave(DownloadInfo)
    End Sub
    Private Function McDownloadFabricLoader(FabricVersion As String, MinecraftName As String, McFolder As String) As List(Of LoaderBase)
        Return Service.McDownloadFabricLoader(FabricVersion, MinecraftName, McFolder)
    End Function
    Public Function FabricDownloadListItem(Entry As JObject, OnClick As MyListItem.ClickEventHandler) As MyVirtualizingElement(Of MyListItem)
        Return Menus.FabricDownloadListItem(Entry, OnClick)
    End Function
    Public Function FabricApiDownloadListItem(Entry As ResourceVersion, OnClick As MyListItem.ClickEventHandler) As MyVirtualizingElement(Of MyListItem)
        Return Menus.FabricApiDownloadListItem(Entry, OnClick)
    End Function
    Public Function OptiFabricDownloadListItem(Entry As ResourceVersion, OnClick As MyListItem.ClickEventHandler) As MyVirtualizingElement(Of MyListItem)
        Return Menus.OptiFabricDownloadListItem(Entry, OnClick)
    End Function
    Public Function McInstallLoadersGet(Request As McInstallRequest) As List(Of LoaderBase)
        Return Service.McInstallLoadersGet(Request)
    End Function
    Private Sub MergeJson(VersionFolder As String, OptiFineFolder As String, OptiFineAsMod As Boolean,
                          ForgeFolder As String, NeoForgeFolder As String, FabricFolder As String, LiteLoaderFolder As String)
        Service.MergeJson(VersionFolder, OptiFineFolder, OptiFineAsMod, ForgeFolder, NeoForgeFolder, FabricFolder, LiteLoaderFolder)
    End Sub
    Public Function McInstallStart(Request As McInstallRequest) As Boolean
        Return Service.McInstallStart(Request)
    End Function
    Public Function McModificationStart(Request As McInstallRequest, Instance As McInstance) As Boolean
        Return Service.McModificationStart(Request, Instance)
    End Function
    Public Sub McInstallCommit(Instance As McInstance, NewManifest As McInstallManifest, BuildFolder As String, TargetFolder As String)
        Service.McInstallCommit(Instance, NewManifest, BuildFolder, TargetFolder)
    End Sub
    Public Sub LoaderStateChangedHintOnly(Loader As LoaderBase)
        Service.LoaderStateChangedHintOnly(Loader)
    End Sub
    Public Sub McInstallState(Loader As LoaderBase)
        Service.McInstallState(Loader)
    End Sub
    Public Sub McInstallFailedClearFolder(Loader)
        Service.McInstallFailedClearFolder(Loader)
    End Sub
End Module
