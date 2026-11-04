Imports PCLLF.Presentation.Mods
Imports NativePage = PCLLF.Presentation.Mods.LegacyModPage(Of PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion, PCL.MyLocalModItem, PCL.ModMinecraft.McInstance, PCL.ModNet.NetFile, PCL.MyIconButton, PCL.ModDependencyImpactView.Item)

Public Class PageInstanceMod
    Implements IRefreshable
    Public ModItems As New ConcurrentDictionary(Of String, MyLocalModItem)
    Public SelectedMods As New List(Of String)
    Public Shared UpdatingInstanceModFolders As New List(Of String)
    Private ReadOnly Native As NativePage
    Public Sub New()
        Native = New NativePage(CreateView(), LocalModPageRuntime.CreateHost(Me))
        InitializeComponent()
    End Sub
    Private Function CreateView() As LegacyModPageView
        Return New LegacyModPageView With {
            .Root = Function() Me, .Back = Function() PanBack, .Empty = Function() PanEmpty, .ListBack = Function() PanListBack, .List = Function() PanList,
            .Search = Function() SearchBox.Text, .SetSearch = Sub(Value) SearchBox.Text = Value, .SetSelectionText = Sub(Value) LabSelect.Text = Value,
            .ScrollHome = Sub() PanBack.ScrollToHome(), .FocusBack = Sub() PanBack.Focus(), .Filters = AddressOf ReadFilterViews,
            .EnableDisable = Sub(Value) BtnSelectDisable.IsEnabled = Value, .EnableEnable = Sub(Value) BtnSelectEnable.IsEnabled = Value, .EnableUpdate = Sub(Value) BtnSelectUpdate.IsEnabled = Value,
            .SelectionBar = AddressOf RefreshNativeSelectionBar}
    End Function
    Private Function ReadFilterViews() As PCLCS.ResourcePageFilter()
        Return {New PCLCS.ResourcePageFilter(BtnFilterAll, 0, Function() BtnFilterAll.Checked, Sub(Value) BtnFilterAll.Checked = Value, Sub(Value) BtnFilterAll.Text = Value),
            New PCLCS.ResourcePageFilter(BtnFilterEnabled, 1, Function() BtnFilterEnabled.Checked, Sub(Value) BtnFilterEnabled.Checked = Value, Sub(Value) BtnFilterEnabled.Text = Value),
            New PCLCS.ResourcePageFilter(BtnFilterDisabled, 2, Function() BtnFilterDisabled.Checked, Sub(Value) BtnFilterDisabled.Checked = Value, Sub(Value) BtnFilterDisabled.Text = Value),
            New PCLCS.ResourcePageFilter(BtnFilterCanUpdate, 3, Function() BtnFilterCanUpdate.Checked, Sub(Value) BtnFilterCanUpdate.Checked = Value, Sub(Value) BtnFilterCanUpdate.Text = Value)}
    End Function
    Private Sub RefreshNativeSelectionBar(Count As Integer, ByRef Previous As Integer, Animate As Boolean)
        LocalResourceUi.RefreshSelectionBar(CardSelect, TransSelect, Count, Previous, "Mod Sidebar", Animate)
    End Sub
    Friend Sub InitializeNativeLoader()
        LocalResourceUi.ApplySelectionIcons(BtnSelectUpdate, BtnSelectDelete, BtnSelectCancel)
        PageLoaderInit(Load, PanLoad, PanAllBack, Nothing, LocalResourceLoader, AddressOf LoadUIFromLoaderOutput, AutoRun:=False)
    End Sub
    Friend Sub BindMainKey()
        AddHandler FrmMain.KeyDown, AddressOf FrmMain_KeyDown
    End Sub
    Friend Sub AdjustFilterMargins()
        For Each Button As MyRadioButton In PanFilter.Children
            Button.LabText.Margin = New Thickness(-2, 0, 8, 0)
        Next
    End Sub
    Friend Function CreateItem(Entry As LocalResourceFile, Selected As Boolean) As MyLocalModItem
        Return New MyLocalModItem With {.SnapsToDevicePixels = True, .Entry = Entry, .ButtonHandler = AddressOf McModContent, .Checked = Selected}
    End Function
    Friend Sub BindRowEvents(Item As MyLocalModItem)
        AddHandler Item.Changed, AddressOf CheckChanged
        AddHandler Item.Click, Sub(Sender As MyLocalModItem, Args As EventArgs) Sender.Checked = Not Sender.Checked
        AddHandler Item.MouseRightButtonUp, AddressOf Info_Click
    End Sub
    Public Sub PageOther_Loaded() Handles Me.Loaded
        Native.Loaded()
    End Sub
    Public Sub ReloadModList(Optional ForceReload As Boolean = False)
        Native.ReloadModList(ForceReload)
    End Sub
    Private Sub RefreshSelf() Implements IRefreshable.Refresh
        Refresh()
    End Sub
    Public Shared Sub Refresh()
        NativePage.Refresh(LocalModPageRuntime.CreateHost(Nothing))
    End Sub
    Private Sub LoaderInit() Handles Me.Initialized
        Native.Initialize()
    End Sub
    Private Sub Load_Click(sender As Object, e As MouseButtonEventArgs) Handles Load.Click
        Native.LoadClick()
    End Sub
    Public Function LoaderRun(Type As LoaderFolderRunType) As Boolean
        Return Native.RunLoader(CInt(Type))
    End Function
    Private Sub LoadUIFromLoaderOutput()
        Native.LoadUI()
    End Sub
    Private Sub McModContent(sender As MyLocalModItem, e As EventArgs)
        Native.ItemContent(sender)
    End Sub
    Public Sub RefreshUI()
        Native.RefreshUI()
    End Sub
    Public Sub RefreshBars()
        Native.RefreshBars()
    End Sub
    Private Sub BtnManageOpen_Click(sender As Object, e As EventArgs) Handles BtnManageOpen.Click, BtnHintOpen.Click
        Native.OpenFolder()
    End Sub
    Private Sub BtnManageSelectAll_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageSelectAll.Click
        Native.SelectAll()
    End Sub
    Private Sub BtnManageInstall_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageInstall.Click, BtnHintInstall.Click
        Native.InstallClick()
    End Sub
    Public Shared Function InstallMods(FilePathList As IEnumerable(Of String)) As Boolean
        Return NativePage.Install(LocalModPageRuntime.CreateHost(Nothing), FilePathList, False)
    End Function
    Private Sub BtnManageDownload_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnManageDownload.Click, BtnHintDownload.Click
        Native.Download()
    End Sub
    Public Sub CheckChanged(sender As MyLocalModItem, e As RouteEventArgs)
        Native.CheckChanged(sender)
    End Sub
    Private Sub UnselectedAllWithAnimation() Handles Load.StateChanged, Me.PageExit
        Native.UnselectAnimated()
    End Sub
    Private Sub FrmMain_KeyDown(sender As Object, e As KeyEventArgs)
        Native.MainKey(e)
    End Sub
    Private Sub SearchBox_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles SearchBox.PreviewKeyDown
        Native.SearchKey(e)
    End Sub
    Public Property Filter As FilterType
        Get
            Return CType(CInt(Native.Filter), FilterType)
        End Get
        Set(value As FilterType)
            Native.Filter = CType(CInt(value), LegacyModFilter)
        End Set
    End Property
    Public Enum FilterType As Integer
        All = 0
        Enabled = 1
        Disabled = 2
        CanUpdate = 3
    End Enum
    Private Sub ChangeFilter(sender As MyRadioButton, raiseByMouse As Boolean) Handles BtnFilterAll.Check, BtnFilterCanUpdate.Check, BtnFilterDisabled.Check, BtnFilterEnabled.Check
        Native.FilterChanged(CInt(sender.Tag))
    End Sub
    Private Sub BtnSelectED_Click(sender As MyIconTextButton, e As RouteEventArgs) Handles BtnSelectEnable.Click, BtnSelectDisable.Click
        Native.SelectED(Not sender.Equals(BtnSelectDisable))
    End Sub
    Private Sub BtnSelectUpdate_Click() Handles BtnSelectUpdate.Click
        Native.UpdateSelected()
    End Sub
    Public Sub UpdateMods(ModList As IEnumerable(Of LocalResourceFile))
        Native.UpdateMods(ModList, False)
    End Sub
    Private Sub BtnSelectDelete_Click() Handles BtnSelectDelete.Click
        Native.DeleteSelected()
    End Sub
    Private Sub BtnSelectCancel_Click() Handles BtnSelectCancel.Click
        Native.CancelSelection()
    End Sub
    Public Sub Info_Click(sender As Object, e As EventArgs)
        Native.Info(sender)
    End Sub
    Public Sub Open_Click(sender As MyIconButton, e As EventArgs)
        Native.OpenSender(sender)
    End Sub
    Public Sub Delete_Click(sender As MyIconButton, e As EventArgs)
        Native.DeleteItem(CType(sender.Tag, MyLocalModItem))
    End Sub
    Public Sub ED_Click(sender As MyIconButton, e As EventArgs)
        Native.ToggleItem(CType(sender.Tag, MyLocalModItem))
    End Sub
    Public ReadOnly Property IsSearching As Boolean
        Get
            Return Native.IsSearching
        End Get
    End Property
    Public Sub SearchRun() Handles SearchBox.TextChanged
        Native.SearchRun()
    End Sub
End Class
