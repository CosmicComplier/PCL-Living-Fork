Imports PCLLF.Presentation.Mods
Imports NativePage = PCLLF.Presentation.Mods.LegacyModPage(Of PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion, PCL.MyLocalModItem, PCL.ModMinecraft.McInstance, PCL.ModNet.NetFile, PCL.MyIconButton, PCL.ModDependencyImpactView.Item)
Imports NativeHost = PCLLF.Presentation.Mods.LegacyModPageHost(Of PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion, PCL.MyLocalModItem, PCL.ModMinecraft.McInstance, PCL.ModNet.NetFile, PCL.MyIconButton, PCL.ModDependencyImpactView.Item)

'原模型、Loader、依赖后端与主窗口的真实 typed 接线；不建立平行资源模型或调度器。
Friend NotInheritable Class LocalModPageRuntime
    Friend Shared Function CreateHost(Owner As PageInstanceMod) As NativeHost
        Return New NativeHost With {
            .Items = Function() Owner.ModItems, .Selected = Function() Owner.SelectedMods, .SetSelected = Sub(Value) Owner.SelectedMods = Value,
            .Output = Function() LocalResourceLoader.Output, .UpdatingFolders = Function() PageInstanceMod.UpdatingInstanceModFolders,
            .Instance = Function() PageInstanceLeft.Instance, .GlobalInstance = Function() McInstanceSelected,
            .AnyLocked = Function() McInstanceLock.IsAnyLocked, .ResetIndexLookups = AddressOf ModDependencyAutoFill.ResetIndexLookups,
            .RebuildIndex = AddressOf RebuildIndex, .RefreshLoaderWarning = Sub(Item, Target) Item.RefreshLoaderWarning(IO.Path.Combine(Target.PathIndie, "mods"), ModDependencyInspector.ContextForInstance(Target)),
            .GameFolder = Function(Value) Value.PathIndie, .InstanceName = Function(Value) Value.Name, .Modable = Function(Value) Value.Modable, .Locked = AddressOf McInstanceLock.IsLocked,
            .LastWasDetails = Function() FrmMain.PageLast.Page = FormMain.PageType.ResourceDetail, .IsCurrentPage = Function() FrmMain.PageRight Is Owner,
            .CtrlDown = Function() My.Computer.Keyboard.CtrlKeyDown, .ShiftDown = Function() My.Computer.Keyboard.ShiftKeyDown,
            .IsInstanceSetup = Function() FrmMain.PageCurrent = FormMain.PageType.InstanceSetup, .IsInstanceSelect = Function() FrmMain.PageCurrent = FormMain.PageType.InstanceSelect,
            .IsModPage = Function() FrmMain.PageCurrent = FormMain.PageType.InstanceSetup AndAlso FrmMain.PageCurrentSub = FormMain.PageSubType.InstanceMod,
            .ModpackMode = Function() ModModpackMode.Enabled, .HistoryBusy = Function() EnvironmentHistoryUi.IsBusy,
            .AnimationControl = Function() AniControlEnabled, .SetAnimationControl = Sub(Value) AniControlEnabled = Value, .ChangeAnimationControl = Sub(Delta) AniControlEnabled += Delta,
            .LoaderRun = Function(Type) LoaderFolderRun(LocalResourceLoader, PageInstanceLeft.Instance.PathIndie & "mods\", CType(Type, LoaderFolderRunType)),
            .ForceFolderLoader = Sub(Folder) LoaderFolderRun(LocalResourceLoader, Folder, LoaderFolderRunType.ForceRun), .LoaderState = Function() CInt(LocalResourceLoader.State),
            .ApplyMode = AddressOf ModModpackMode.ApplyInstanceVisibility, .BindMainKey = Sub() Owner.BindMainKey(), .AdjustFilterMargins = Sub() Owner.AdjustFilterMargins(), .InitializeLoader = Sub() Owner.InitializeNativeLoader(),
            .ClearProjectCache = Sub() ResourceProject.Cache.Clear(), .ClearVersionCache = Sub() ResourceVersion.ProjectFilesCache.Clear(), .SelectModPage = Sub() FrmInstanceLeft.ItemMod.Checked = True,
            .ReloadCurrent = Sub(Force) ReloadCurrent(Force), .TempPath = Function() PathTemp,
            .Hint = AddressOf ShowHint, .ConfirmMode = AddressOf ModModpackMode.ConfirmModAction, .ReadBoolean = Function(Key) Settings.Get(Of Boolean)(Key), .WriteSetting = Sub(Key, Value) Settings.Set(Key, Value),
            .SelectInstallFiles = Function() Dialogs.SelectFile("选择要安装的 Mod", True, filter:={({"jar", "litemod", "disabled", "old"}, "Mod 文件")}),
            .Explorer = AddressOf OpenExplorer, .Website = AddressOf OpenWebsite,
            .Message = Function(Text, Caption, Button1, Button2, Warning) MyMsgBox(Text, Caption, Button1, Button2, IsWarn:=Warning),
            .ContentMessage = Sub(Text, Caption, Content) MyMsgBoxContent(Text, Caption, Content),
            .ImpactConfirm = Function(Text, ActionName, Items) MyMsgBoxConfirmContent(Text, "模组依赖影响", "继续" & ActionName, Function() New ModDependencyImpactView(Items, ActionName)),
            .InspectImpacts = AddressOf InspectImpacts, .InspectDetails = AddressOf InspectDetails,
            .Dispatch = Sub(Work) RunInUi(Work), .Background = Sub(Work, Name, Priority) RunInNewThread(Work, Name, Priority),
            .NewItem = Function(Entry, Selected) Owner.CreateItem(Entry, Selected), .BindUpdate = AddressOf BindUpdate, .Entry = Function(Item) Item.Entry,
            .Checked = Function(Item) Item.Checked, .SetChecked = Sub(Item, Value) Item.Checked = Value, .BindRowEvents = Sub(Item) Owner.BindRowEvents(Item),
            .CreateButton = AddressOf CreateButton, .SetButtons = Sub(Item, Buttons) Item.Buttons = Buttons,
            .OpenLogo = Function() Logo.IconButtonOpen, .InfoLogo = Function() Logo.IconButtonInfo, .DeleteLogo = Function() Logo.IconButtonDelete,
            .StopLogo = Function() Logo.IconButtonStop, .CheckLogo = Function() Logo.IconButtonCheck, .ListLogo = Function() Logo.IconButtonList,
            .ResolveItem = Function(Sender) CType(If(TypeOf Sender Is MyIconButton, CType(Sender, MyIconButton).Tag, Sender), MyLocalModItem),
            .NewMod = Function(Address) New LocalResourceFile(Address), .NavigateDownload = AddressOf NavigateDownload, .NavigateDetails = AddressOf NavigateDetails,
            .ToNetFile = Function(Value, Address) Value.ToNetFile(Address, ResourceVersion.DownloadReason.Update, PageInstanceLeft.Instance.Version.VanillaName, PageInstanceLeft.Instance.Version.ModLoaders),
            .CreateUpdateJob = AddressOf CreateUpdateJob, .CaptureHistory = Sub(Instance, Caption, Token) EnvironmentHistory.CaptureInstance(Instance, Caption, Token),
            .RefreshDownloadButton = Sub() FrmMain.BtnExtraDownload.ShowRefresh(), .RibbleDownloadButton = Sub() FrmMain.BtnExtraDownload.Ribble(), .FormatSize = AddressOf StringUtils.FormatByteSize}
    End Function
    Private Shared Function RebuildIndex(Target As McInstance, Progress As Action(Of Integer, Integer)) As LegacyModDependencyIndexResult
        Dim Report = ModDependencyOnline.RebuildWithOnline(IO.Path.Combine(Target.PathIndie, "mods"), ModDependencyInspector.ContextForInstance(Target),
            Options:=New ModDependencyOnline.LookupOptions With {.Progress = Progress})
        Return New LegacyModDependencyIndexResult With {.IndexReadFailed = Function() Report.IndexReadFailed, .IndexPersisted = Function() Report.IndexPersisted, .OnlineIncomplete = Function() Report.OnlineIncomplete}
    End Function
    Private Shared Sub ShowHint(Text As String, Kind As Integer, WriteLog As Boolean)
        Dim Color = If(Kind = 1, HintType.Red, If(Kind = 2, HintType.Green, If(Kind = 3, HintType.Blue, HintType.Blue)))
        Hint(Text, Color, Log:=WriteLog)
    End Sub
    Private Shared Sub ReloadCurrent(Force As Boolean)
        If FrmInstanceMod IsNot Nothing Then FrmInstanceMod.ReloadModList(Force)
    End Sub
    Private Shared Sub BindUpdate(Entry As LocalResourceFile, Item As MyLocalModItem)
        AddHandler Entry.OnUpdate, Sub() RunInUi(AddressOf Item.Refresh)
    End Sub
    Private Shared Function CreateButton(Scale As Double, Icon As String, Description As String, Item As MyLocalModItem, Work As Action(Of Object)) As MyIconButton
        Dim Button As New MyIconButton With {.LogoScale = Scale, .Logo = Icon, .Tag = Item, .ToolTip = Description}
        AddHandler Button.Click, Sub(Sender, Args) Work(Sender)
        Return Button
    End Function
    Private Shared Function InspectImpacts(Target As McInstance, Candidates As String()) As List(Of ModDependencyImpactView.Item)
        Dim Report = ModDependencyOnline.InspectWithOnline(IO.Path.Combine(Target.PathIndie, "mods"), ModDependencyInspector.ContextForInstance(Target),
            Options:=New ModDependencyOnline.LookupOptions With {.ComparePlatformDependencies = True, .MaxSearchFiles = 0})
        Return ModDependencyImpactView.Collect(Report, Candidates)
    End Function
    Private Shared Function InspectDetails(Target As McInstance, Entry As LocalResourceFile) As LegacyModDependencyDetails
        Dim Report = ModDependencyOnline.InspectWithOnline(IO.Path.Combine(Target.PathIndie, "mods"), ModDependencyInspector.ContextForInstance(Target),
            Options:=New ModDependencyOnline.LookupOptions With {.TargetFile = Entry.File.FullName, .ComparePlatformDependencies = True, .MaxSearchFiles = 0})
        Return New LegacyModDependencyDetails With {
            .Caption = Report.DescribeFile(Entry.File.FullName),
            .HasMod = Function() Report.Mods.Any(Function(Item) ModDependencyInspector.SameFilePath(Item.RootPath, Entry.File.FullName)),
            .CreateView = Function() New ModDependencyGraphView(Report, Entry.File.FullName)}
    End Function
    Private Shared Sub NavigateDownload(Instance As McInstance, Search As String)
        PageResource.TargetInstance = Instance
        PageResource.TargetName = Search
        FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod)
    End Sub
    Private Shared Sub NavigateDetails(Project As ResourceProject)
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.ResourceDetail,
            .Additional = {Project, New List(Of String), PageInstanceLeft.Instance.Version.VanillaName, PageInstanceLeft.Instance.Version.ModLoaders, ResourceTypes.Mod}})
    End Sub
    Private Shared Function CreateUpdateJob(Name As Func(Of String), Files As List(Of NetFile), Weight As Double, Replace As Action) As LegacyModUpdateJob
        Dim Loaders As New List(Of LoaderBase) From {
            New LoaderDownload("下载新版 Mod 文件", Files) With {.ProgressWeight = Weight},
            New LoaderTask(Of Integer, Integer)("替换旧版 Mod 文件", Sub() Replace())}
        Dim Loader As New LoaderCombo(Of IEnumerable(Of LocalResourceFile))(Name(), Loaders)
        Dim Port As New LegacyModUpdateJob With {
            .State = Function() CInt(Loader.State), .Error = Function() Loader.Error,
            .Start = Sub() Loader.Start(), .AddTaskbar = Sub() LoaderTaskbarAdd(Loader)}
        Port.SetOnStateChanged = Sub(Work) Loader.OnStateChanged = Sub() Work(Port)
        Return Port
    End Function
End Class
