Imports OverallController = PCLCS.InstanceOverallPageController(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.MyListItem, PCL.ModLoader.LoaderCombo(Of String))
Imports OverallHost = PCLCS.InstanceOverallPageHost(Of PCL.ModMinecraft.McInstance, PCL.MyListItem, PCL.ModLoader.LoaderCombo(Of String))

Public Class PageInstanceOverall
    Private IsLoad As Boolean = False
    Public InstanceHeaderItem As MyListItem
    Private OriginalHeaderInfo As String = ""
    Private ReadOnly OverallView As New PCLCS.InstanceOverallPageView(Of MyListItem)
    Private PageController As OverallController
    Private ReadOnly Property Controller As OverallController
        Get
            OverallView.DisplayType = ComboDisplayType
        OverallView.DisplayLogo = ComboDisplayLogo
        OverallView.CustomLogo = ItemDisplayLogoCustom
            OverallView.Delete = BtnManageDelete
        OverallView.Star = BtnDisplayStar
        OverallView.FolderMods = BtnFolderMods
        OverallView.Installed = PanInstalledComponents
        OverallView.HeaderPanel = PanDisplayItem
            OverallView.ScrollHome = Sub() PanBack.ScrollToHome()
        OverallView.ResizeDisplay = Sub() PanDisplay.TriggerForceResize()
            OverallView.SetStarText = Sub(Text) BtnDisplayStar.Text = Text
            OverallView.Header = Function() InstanceHeaderItem
        OverallView.SetHeader = Sub(Value) InstanceHeaderItem = Value
            OverallView.HeaderInfo = Function(Item) Item.Info
        OverallView.SetHeaderInfo = Sub(Item, Text) Item.Info = Text
            OverallView.IsLoaded = Function() IsLoad
        OverallView.SetLoaded = Sub(Value) IsLoad = Value
            OverallView.OriginalHeaderInfo = Function() OriginalHeaderInfo
        OverallView.SetOriginalHeaderInfo = Sub(Value) OriginalHeaderInfo = Value
            If PageController Is Nothing Then PageController = New OverallController(OverallView, CreateHost(), Sub(Value) AniControlEnabled += Value, Function() AniControlEnabled)
            Return PageController
        End Get
    End Property
    Private Function CreateHost() As OverallHost
        Return New OverallHost With {
            .Instance = Function() PageInstanceLeft.Instance, .SetInstance = Sub(Value) PageInstanceLeft.Instance = Value,
            .SelectedInstance = Function() McInstanceSelected, .MinecraftFolder = Function() McFolderSelected,
            .ModpackEnabled = Function() ModModpackMode.Enabled, .LaunchLoading = Function() McLaunchLoader.State = LoadState.Loading,
            .Shift = Function() My.Computer.Keyboard.ShiftKeyDown, .LegacyLogin = Function() Settings.Get(Of McLoginType)("LoginType") = McLoginType.Legacy,
            .HideHintSeen = Function() Settings.Get(Of Boolean)("HintHide"), .MarkHideHint = Sub() Settings.Set("HintHide", True),
            .ForceListRefresh = Sub() McInstanceListForceRefresh = True,
            .Locked = AddressOf McInstanceLock.IsLocked, .IgnoreFileCheck = AddressOf ShouldIgnoreFileCheck,
            .CreateHeader = Function(Target) Target.ToListItem().Init(), .RefreshInstalled = AddressOf RefreshInstalledComponents,
            .RefreshModDisabled = Sub() FrmInstanceLeft.RefreshModDisabled(), .RefreshPageName = Sub() FrmMain.PageNameRefresh(),
            .RefreshInstanceList = Sub() LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\"),
            .LoadInstance = Function(Name) New McInstance(Name).Load(), .Rename = Function(Target, Name) Target.Rename(Name),
            .ReadIni = Function(Path, Key, DefaultValue) ReadIni(Path, Key, DefaultValue), .WriteIni = Sub(Path, Key, Value) WriteIni(Path, Key, Value),
            .ReadLogo = AddressOf MinecraftLogoCompatibility.ReadVersionLogo, .StorageLogo = AddressOf MinecraftLogoCompatibility.ToStorageUri,
            .WriteLogo = AddressOf MinecraftLogoCompatibility.WriteVersionLogo,
            .SelectImage = Function() Dialogs.SelectFile("选择图片", False, filter:={({"png", "jpeg", "jpg", "gif", "webp"}, "常用图片文件")}).FirstOrDefault(),
            .InputDescription = Function(Title, OldValue) MyMsgBoxInput(Title, "修改版本的描述文本，留空则使用 PCL 的默认描述。", OldValue, New ObjectModel.Collection(Of Validate), "默认描述"),
            .InputName = Function(Name, Folder) MyMsgBoxInput("重命名版本", "", Name, New ObjectModel.Collection(Of Validate) From {New ValidateFolderName(Folder, IgnoreList:=New List(Of String) From {Name})}),
            .SaveScript = Function(Title, FileName) Dialogs.SaveFile(Title, FileName, filter:={("bat", "批处理文件")}),
            .Confirm = Function(Text, Title, Warn) MyMsgBox(Text, Title,, "取消",, Warn),
            .LaunchScript = Function(Target, Path) McLaunchStart(New McLaunchOptions With {.SaveBatch = Path, .Instance = Target}),
            .TaskNames = Function() LoaderTaskbar.Select(Function(Loader) Loader.Name),
            .CreateFixLoader = Function(Target, Name) New LoaderCombo(Of String)(Name, DlClientFix(Target, True, False)),
            .HookState = Sub(Loader, Work) Loader.OnStateChanged = Sub() Work(),
            .LoaderState = Function(Loader) CInt(Loader.State), .Finished = CInt(LoadState.Finished), .Failed = CInt(LoadState.Failed), .Canceled = CInt(LoadState.Canceled),
            .LoaderName = Function(Loader) Loader.Name, .LoaderError = Function(Loader) Loader.Error.GetDisplay(False),
            .StartLoader = Sub(Loader, Input) Loader.Start(Input), .TaskbarAdd = AddressOf LoaderTaskbarAdd,
            .RefreshDownload = Sub() FrmMain.BtnExtraDownload.ShowRefresh(), .RibbleDownload = Sub() FrmMain.BtnExtraDownload.Ribble(), .PageBack = Sub() FrmMain.PageBack(),
            .Hint = Sub(Text) Hint(Text), .HintRed = Sub(Text) Hint(Text, HintType.Red), .HintGreen = Sub(Text) Hint(Text, HintType.Green), .HintBlue = Sub(Text) Hint(Text, HintType.Blue),
            .Explorer = Sub(Path) OpenExplorer(Path), .Error = Sub(Err, Text) Logger.Error(Err, Text), .ErrorAlert = Sub(Err, Text) Logger.Error(Err, Text, LogBehavior.Alert), .Warn = Sub(Err, Text) Logger.Warn(Err, Text)}
    End Function
    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub
    Friend Sub RefreshModpackVisibility()
        Controller.RefreshModpackVisibility()
    End Sub
    Private Sub Reload()
        Controller.Reload()
    End Sub
    Private Sub ComboDisplayType_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboDisplayType.SelectionChanged
        Controller.DisplayTypeChanged()
    End Sub
    Private Sub BtnDisplayDesc_Click(sender As Object, e As EventArgs) Handles BtnDisplayDesc.Click
        Controller.Description()
    End Sub
    Private Sub BtnDisplayRename_Click(sender As Object, e As EventArgs) Handles BtnDisplayRename.Click
        Controller.Rename()
    End Sub
    Private Sub ComboDisplayLogo_SelectionChanged() Handles ItemDisplayLogoCustom.PreviewMouseLeftButtonUp, ComboDisplayLogo.SelectionChanged
        Controller.LogoChanged()
    End Sub
    Private Sub BtnDisplayStar_Click(sender As Object, e As EventArgs) Handles BtnDisplayStar.Click
        Controller.Star()
    End Sub
    Private Sub BtnFolderVersion_Click() Handles BtnFolderVersion.Click
        OpenInstanceFolder(PageInstanceLeft.Instance)
    End Sub
    Public Shared Sub OpenInstanceFolder(Instance As McInstance)
        OpenExplorer(Instance.PathVersion)
    End Sub
    Private Sub BtnFolderSaves_Click() Handles BtnFolderSaves.Click
        Controller.OpenGameFolder("saves")
    End Sub
    Private Sub BtnFolderMods_Click() Handles BtnFolderMods.Click
        Controller.OpenGameFolder("mods")
    End Sub
    Private Sub BtnFolderScreen_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnFolderScreen.Click
        Controller.OpenGameFolder("screenshots")
    End Sub
    Private Sub BtnManageScript_Click() Handles BtnManageScript.Click
        Controller.Script()
    End Sub
    Private Sub BtnManageCheck_Click(sender As Object, e As EventArgs) Handles BtnManageCheck.Click
        Controller.Repair()
    End Sub
    Private Sub BtnManageDelete_Click(sender As Object, e As EventArgs) Handles BtnManageDelete.Click
        Controller.Delete()
    End Sub
End Class
