Imports System.IO
Imports System.Threading
Imports ResourceController = PCLCS.ResourcePageController(Of PCL.ModMinecraft.McInstance, PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo)
Imports ResourceView = PCLCS.ResourcePageView(Of PCL.ModMinecraft.McInstance, PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo)
Imports ResourceHost = PCLCS.ResourcePageHost(Of PCL.ModMinecraft.McInstance, PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo)
Imports ResourceRequest = PCLCS.ResourcePageController(Of PCL.ModMinecraft.McInstance, PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo).Request
Imports ResourceResult = PCLCS.ResourcePageController(Of PCL.ModMinecraft.McInstance, PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo).Result

Public Class PageInstanceResources
    Implements IRefreshable

    Private ReadOnly Kind As InstanceResourceKind
    Private ReadOnly Controller As ResourceController
    Private ReadOnly ResourceLoader As LoaderTask(Of ResourceRequest, ResourceResult)
    Private KeyboardOwner As FormMain
    Private KeyboardHandler As KeyEventHandler
    Private BackupRefreshHandler As WorldBackups.ChangedEventHandler

    Public Sub New()
        Me.New(InstanceResourceKind.Saves)
    End Sub
    Friend Sub New(Kind As InstanceResourceKind)
        Me.Kind = Kind
        InitializeComponent()
        LocalResourceUi.ApplySelectionIcons(BtnUpdate, BtnDelete, BtnDeselect)
        BtnExport.Logo = Logo.IconButtonSave
        BtnBackupRestore.Logo = Logo.IconButtonRefresh
        For Each Button As MyRadioButton In PanFilter.Children
            Button.LabText.Margin = New Thickness(-2, 0, 8, 0)
        Next
        Dim View = CreateView()
        Dim Host = CreateHost()
        Controller = New ResourceController(CInt(Kind), View, Host,
            New PCLCS.InstanceResourceTransactions(AddressOf PathUtils.RemoveExtendedPrefix))
        ResourceLoader = New LoaderTask(Of ResourceRequest, ResourceResult)("读取游戏资源", AddressOf ReadResources)
        PageLoaderInit(Load, PanLoad, PanAllBack, Nothing, ResourceLoader, AddressOf ResourcesLoaded, AutoRun:=False)
    End Sub

    Private Function CreateView() As ResourceView
        Dim View As New ResourceView With {
            .Root = Me, .Viewport = PanBack, .PanEmpty = PanEmpty, .PanOperations = PanOperations,
            .PanResourceList = PanResourceList, .PanFilter = PanFilter, .BackupSettings = BackupSettings,
            .PanList = PanList, .PanWorlds = PanWorlds, .PanScreenshots = PanScreenshots,
            .PanManageButtons = PanManageButtons, .CardList = New CardAdapter(CardList),
            .CardSelect = CardSelect, .TransSelect = TransSelect,
            .LabOperation = LabOperation, .LabEmpty = LabEmpty, .LabEmptyTitle = LabEmptyTitle,
            .LabEmptyHint = LabEmptyHint, .LabBackupStatus = LabBackupStatus, .LabSelect = LabSelect,
            .GetSearch = Function() SearchBox.Text, .SetSearch = Sub(Value) SearchBox.Text = Value,
            .SetSearchHint = Sub(Value) SearchBox.HintText = Value,
            .SearchHasFocus = Function() SearchBox.IsKeyboardFocusWithin,
            .FocusViewport = Sub() PanBack.Focus(), .ScrollHome = Sub() PanBack.ScrollToHome(),
            .BindBackupOwner = Sub(Value) BackupSettings.BindOwner(Value),
            .CommitBackupSettings = Function() BackupSettings.CommitPending(),
            .SaveLogo = Logo.IconButtonSave, .RestoreLogo = Logo.IconButtonRefresh}
        View.BtnOpenFolder = NativeButton(BtnOpenFolder)
        View.BtnImportFile = NativeButton(BtnImportFile)
        View.BtnDownload = NativeButton(BtnDownload)
        View.BtnSelectAll = NativeButton(BtnSelectAll)
        View.BtnBackupAll = NativeButton(BtnBackupAll)
        View.BtnRestore = NativeButton(BtnRestore)
        View.BtnUp = NativeButton(BtnUp)
        View.BtnCancel = NativeButton(BtnCancel)
        View.BtnHintDownload = NativeButton(BtnHintDownload)
        View.BtnHintRestore = NativeButton(BtnHintRestore)
        View.BtnRecycleBack = NativeButton(BtnRecycleBack)
        View.BtnUpdate = NativeButton(BtnUpdate)
        View.BtnBackupRestore = NativeButton(BtnBackupRestore)
        View.BtnExport = NativeButton(BtnExport)
        View.BtnDelete = NativeButton(BtnDelete)
        View.BtnDeselect = NativeButton(BtnDeselect)
        View.FilterAll = NativeFilter(BtnFilterAll)
        View.FilterUpdates = NativeFilter(BtnFilterUpdates)
        View.FilterFiles = NativeFilter(BtnFilterFiles)
        View.FilterFolders = NativeFilter(BtnFilterFolders)
        View.Filters = {View.FilterAll, View.FilterUpdates, View.FilterFiles, View.FilterFolders}
        View.CreateRow = Function(Actions) New RowAdapter(Me, Actions)
        View.CreateThumbnail = Function(Changed, Clicked) New ThumbnailAdapter(Changed, Clicked, PanBack)
        View.CreateBackupGroup = Function(Changed, BeforeCreate) New BackupGroupAdapter(CardSelect, Changed, BeforeCreate)
        View.CreateBackupCard = AddressOf CreateBackupCard
        View.RefreshSelectionBar =
            Function(Count, Previous, Animate)
                LocalResourceUi.RefreshSelectionBar(CardSelect, TransSelect, Count, Previous, "Resource Sidebar " & PageUuid, Animate)
                Return Previous
            End Function
        View.StopSelectionBar = Sub() AniStop("Resource Sidebar " & PageUuid)
        Return View
    End Function

    Private Shared Function NativeButton(Button As MyButton) As PCLCS.ResourcePageButton
        Return New PCLCS.ResourcePageButton(Button, Function() Button.Text, Sub(Value) Button.Text = Value)
    End Function
    Private Shared Function NativeButton(Button As MyTextButton) As PCLCS.ResourcePageButton
        Return New PCLCS.ResourcePageButton(Button, Function() Button.Text, Sub(Value) Button.Text = Value)
    End Function
    Private Shared Function NativeButton(Button As MyIconTextButton) As PCLCS.ResourcePageButton
        Return New PCLCS.ResourcePageButton(Button, Function() Button.Text, Sub(Value) Button.Text = Value, Sub(Value) Button.Logo = Value)
    End Function
    Private Shared Function NativeFilter(Button As MyRadioButton) As PCLCS.ResourcePageFilter
        Return New PCLCS.ResourcePageFilter(Button, CInt(Button.Tag), Function() Button.Checked,
            Sub(Value) Button.Checked = Value, Sub(Value) Button.Text = Value)
    End Function

    Private Function CreateHost() As ResourceHost
        Return New ResourceHost With {
            .CurrentInstance = Function() PageInstanceLeft.Instance,
            .GameFolder = Function(Value) Value.PathIndie,
            .VersionPath = Function(Value) Value.PathVersion,
            .GameVersion = Function(Value) Value.Version.VanillaName,
            .AllowsInstance = Function(Value) ModModpackMode.AllowsInstance(Value.PathVersion),
            .IsLocked = Function(Value) McInstanceLock.IsLocked(Value),
            .NeedsSchematicLoader = Function(Value) Value.Version.ModLoaders = ModLoaders.None,
            .QuickPlaySupported = Function(Value) QuickPlayWorlds.Supported(Value),
            .HasRunningGame = Function() HasRunningMinecraft,
            .ModpackEnabled = Function() ModModpackMode.Enabled,
            .IsCurrentPage = Function() FrmMain?.PageRight Is Me,
            .AnimationEnabled = Function() AniControlEnabled = 0,
            .BackupBusy = Function() WorldBackups.IsBusy,
            .SamePath = Function(Left, Right) PathUtils.IsSame(Left, Right),
            .RemoveExtendedPrefix = AddressOf PathUtils.RemoveExtendedPrefix,
            .Dispatch = Sub(Work) RunInUi(Work),
            .Background = Sub(Work, Name) RunInNewThread(Work, Name),
            .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            .Error = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.Toast),
            .Hint = Sub(Message, Success) Hint(Message, If(Success, HintType.Green, HintType.Red)),
            .Message = Function(Text, Title, First, Second, Warning) MyMsgBox(Text, Title, First, Second, IsWarn:=Warning),
            .Input = AddressOf InputResourceName,
            .SelectFiles = Function(Title, Multiple, Extensions) Dialogs.SelectFile(Title, Multiple, filter:={(Extensions, "资源文件")}).ToArray(),
            .SaveFile = Function(Title, Name, Extension) Dialogs.SaveFile(Title, Name, filter:={(Extension, "资源文件")}),
            .SelectFolder = Function(Title, Multiple) Dialogs.SelectFolder(Title, Multiple).ToArray(),
            .OpenExplorer = Sub(Value) OpenExplorer(Value),
            .OpenWebsite = Sub(Value) OpenWebsite(Value),
            .CreateDirectory = Sub(Value) DirectoryUtils.Create(Value),
            .OpenProcess = Sub(Value) Process.Start(Value),
            .Navigate = AddressOf NavigateResource,
            .QuickPlay = Sub(Value, Name) QuickPlayWorlds.Start(Value, Name),
            .DescribeWorld = Function(Value) WorldMetadata.Describe(Value),
            .Scan = Function(Root, Folder, Value, Token) InstanceResourceStore.Scan(Root, Folder, CType(Value, InstanceResourceKind), Token),
            .RecycleList = Function(Root, Recycle, Value) ResourceRecycleStore.List(Root, Recycle, CType(Value, InstanceResourceKind)),
            .RestoreRecycle = Sub(Root, Recycle, Ids, Token) ResourceRecycleStore.Restore(Root, Recycle, Ids, Token),
            .PurgeRecycle = Sub(Root, Recycle, Ids, Token) ResourceRecycleStore.Purge(Root, Recycle, Ids, Token),
            .UpdatesSupported = Function(Value) InstanceResourceUpdates.Supported(CType(Value, InstanceResourceKind)),
            .CheckOnline = Function(Root, Items, Value, Version, Token) InstanceResourceUpdates.Check(Root, Items, CType(Value, InstanceResourceKind), Version, Token),
            .InstallOnline = Sub(Root, Items, Recycle, Value, Version, Token) InstanceResourceUpdates.Install(Root, Items, Recycle, CType(Value, InstanceResourceKind), Version, Token),
            .OnlineEntry = Function(Value) Value.Entry,
            .HasUpdate = Function(Value) Value.HasUpdate,
            .ProjectName = Function(Value) Value.ProjectName,
            .Website = Function(Value) Value.Website,
            .Status = Function(Value) Value.Status,
            .Tags = Function(Value) Value.Tags,
            .BackupPoints = Function(Value) WorldBackups.Points(Value),
            .StartBackup = Sub(Value, Ids, RestoreId) WorldBackupsUi.Start(Value, RestoreId:=RestoreId, DeleteIds:=Ids),
            .StartLoader = Sub(Request) ResourceLoader.Start(Request, IsForceRestart:=True),
            .CancelLoader = Sub() ResourceLoader.Cancel(),
            .LoaderFailed = Function() ResourceLoader.State = LoadState.Failed,
            .BindExternalEvents = AddressOf BindExternalEvents}
    End Function

    Private Shared Function InputResourceName(Title As String, Text As String, Initial As String, FolderRoot As String, StrictName As Boolean) As String
        If FolderRoot IsNot Nothing Then
            Return MyMsgBoxInput(Title, Text, Initial, New ObjectModel.Collection(Of Validate) From {New ValidateFolderName(FolderRoot)})
        End If
        If StrictName Then
            Return MyMsgBoxInput(Title, Text, Initial, New ObjectModel.Collection(Of Validate) From {New ValidateNullOrWhiteSpace, New ValidateLength(1, 120)})
        End If
        Return MyMsgBoxInput(Title, Text, Initial)
    End Function
    Private Shared Sub NavigateResource(Target As Integer, Instance As McInstance, Query As String)
        If Target = 0 Then
            FrmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.InstanceModify)
            Return
        End If
        PageResource.TargetInstance = Instance
        PageResource.TargetName = Query
        Dim Page = If(Target = 1, FormMain.PageSubType.DownloadMod,
            If(Target = 2, FormMain.PageSubType.DownloadResourcePack, FormMain.PageSubType.DownloadShader))
        FrmMain.PageChange(FormMain.PageType.Download, Page)
    End Sub
    Private Sub BindExternalEvents(Attach As Boolean, Refresh As Action, Handler As KeyEventHandler)
        If Attach Then
            If Kind = InstanceResourceKind.Saves Then
                BackupRefreshHandler = Sub() Refresh()
                AddHandler WorldBackups.Changed, BackupRefreshHandler
            End If
            If KeyboardOwner Is Nothing AndAlso FrmMain IsNot Nothing Then
                KeyboardOwner = FrmMain
                KeyboardHandler = Handler
                AddHandler KeyboardOwner.KeyDown, KeyboardHandler
            End If
        Else
            If Kind = InstanceResourceKind.Saves AndAlso BackupRefreshHandler IsNot Nothing Then
                RemoveHandler WorldBackups.Changed, BackupRefreshHandler
                BackupRefreshHandler = Nothing
            End If
            If KeyboardOwner IsNot Nothing Then
                RemoveHandler KeyboardOwner.KeyDown, KeyboardHandler
                KeyboardOwner = Nothing
                KeyboardHandler = Nothing
            End If
        End If
    End Sub

    Private Sub ReadResources(Loader As LoaderTask(Of ResourceRequest, ResourceResult))
        Dim Request = Loader.Input
        Dim Result = Controller.Read(Request, If(Request.RecycleMode, CancellationToken.None, Loader.CreateCancellationToken()), Function() Loader.IsCanceled)
        If Result IsNot Nothing Then Loader.Output = Result
    End Sub
    Private Sub ResourcesLoaded(Loader As LoaderTask(Of ResourceRequest, ResourceResult))
        Controller.Apply(Loader.Output)
    End Sub
    Public Sub RefreshResources() Implements IRefreshable.Refresh
        Controller.Refresh()
    End Sub
    Friend Sub ShowResources(Root As String, Folder As String, Items As IEnumerable(Of InstanceResourceEntry))
        Controller.ShowResources(Root, Folder, Items)
    End Sub
    Friend Sub ApplyOnlineInfo(Infos As IEnumerable(Of InstanceResourceOnlineInfo))
        Controller.ApplyOnlineInfo(Infos)
    End Sub
    Friend Sub ScreenshotClick(Entry As InstanceResourceEntry, Count As Integer, Optional Open As Action(Of ProcessStartInfo) = Nothing)
        Controller.ScreenshotClick(Entry, Count, Open)
    End Sub
    Friend Sub ChangeAllSelected(Value As Boolean)
        Controller.ChangeAllSelected(Value)
    End Sub

    Private Sub PageLoaded() Handles Me.Loaded
        Controller.PageLoaded()
    End Sub
    Private Sub PageUnloaded() Handles Me.Unloaded
        Controller.PageUnloaded()
    End Sub
    Private Sub ClearSelection() Handles Me.PageExit
        Controller.ClearSelection()
    End Sub
    Private Sub SearchChanged() Handles SearchBox.TextChanged
        Controller?.SearchChanged()
    End Sub
    Private Sub FilterChanged(Sender As MyRadioButton, User As Boolean) Handles BtnFilterAll.Check, BtnFilterUpdates.Check, BtnFilterFiles.Check, BtnFilterFolders.Check
        Controller?.FilterChanged(CInt(Sender.Tag))
    End Sub
    Private Sub SelectAllClick() Handles BtnSelectAll.Click
        Controller.SelectAllClick()
    End Sub
    Private Sub DeselectClick() Handles BtnDeselect.Click
        Controller.DeselectClick()
    End Sub
    Private Sub RefreshScreenshotViewport() Handles PanBack.ScrollChanged, PanBack.SizeChanged
        Controller?.RefreshScreenshotViewport()
    End Sub
    Private Sub SearchKeyDown(Sender As Object, e As KeyEventArgs) Handles SearchBox.PreviewKeyDown
        Controller.SearchKeyDown(Sender, e)
    End Sub
    Private Sub ResourceKeyDown(Sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Controller.ResourceKeyDown(Sender, e)
    End Sub
    Private Sub OpenFolder() Handles BtnOpenFolder.Click, BtnHintOpen.Click
        Controller.OpenFolder()
    End Sub
    Private Sub UpClick() Handles BtnUp.Click
        Controller.UpClick()
    End Sub
    Private Sub ImportFiles() Handles BtnImportFile.Click, BtnHintInstall.Click
        Controller.ImportFiles()
    End Sub
    Private Sub ResourceDragOver(Sender As Object, e As DragEventArgs) Handles Me.PreviewDragOver
        Controller.ResourceDragOver(Sender, e)
    End Sub
    Private Sub ResourceDrop(Sender As Object, e As DragEventArgs) Handles Me.PreviewDrop
        Controller.ResourceDrop(Sender, e)
    End Sub
    Private Sub ExportSelected() Handles BtnExport.Click
        Controller.ExportSelected()
    End Sub
    Private Sub DeleteSelected() Handles BtnDelete.Click
        Controller.DeleteSelected()
    End Sub
    Private Sub DownloadNew() Handles BtnDownload.Click, BtnHintDownload.Click
        Controller.DownloadNew()
    End Sub
    Private Sub UpdateSelected() Handles BtnUpdate.Click
        Controller.UpdateSelected()
    End Sub
    Private Sub CancelClick() Handles BtnCancel.Click
        Controller.CancelClick()
    End Sub
    Private Sub RetryLoad() Handles Load.Click
        Controller.RetryLoad()
    End Sub
    Private Sub CheckUpdates()
        Controller.CheckUpdates()
    End Sub
    Private Property Owner As McInstance
        Get
            Return Controller.Owner
        End Get
        Set(Value As McInstance)
            Controller.Owner = Value
        End Set
    End Property
    Private Property CurrentRoot As String
        Get
            Return Controller.CurrentRoot
        End Get
        Set(Value As String)
            Controller.CurrentRoot = Value
        End Set
    End Property
    Private Property CurrentFolder As String
        Get
            Return Controller.CurrentFolder
        End Get
        Set(Value As String)
            Controller.CurrentFolder = Value
        End Set
    End Property
    Private Property RecycleRoot As String
        Get
            Return Controller.RecycleRoot
        End Get
        Set(Value As String)
            Controller.RecycleRoot = Value
        End Set
    End Property
    Private Property Entries As List(Of InstanceResourceEntry)
        Get
            Return Controller.Entries
        End Get
        Set(Value As List(Of InstanceResourceEntry))
            Controller.Entries = Value
        End Set
    End Property
    Private Property Busy As Boolean
        Get
            Return Controller.Busy
        End Get
        Set(Value As Boolean)
            Controller.Busy = Value
        End Set
    End Property
    Private Property Checking As Boolean
        Get
            Return Controller.Checking
        End Get
        Set(Value As Boolean)
            Controller.Checking = Value
        End Set
    End Property
    Private Property OnlineCancellation As CancellationTokenSource
        Get
            Return Controller.OnlineCancellation
        End Get
        Set(Value As CancellationTokenSource)
            Controller.OnlineCancellation = Value
        End Set
    End Property
End Class
