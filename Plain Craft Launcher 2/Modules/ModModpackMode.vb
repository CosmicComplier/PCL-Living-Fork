Imports System.Windows
Imports System.Windows.Controls
Imports ModeCore = PCLCS.ModpackModeController(Of PCL.MyPageRight, PCL.ModMinecraft.McFolder, PCL.ModMinecraft.McInstance)

'LF 原路由/控件/设置/Loader 适配。模式判断、界面决策与异步作用域流程在 C# 中。
Friend Module ModModpackMode
    Friend Const SettingKey As String = "LivingModpackMode"
    Friend Const ExportMarkerKey As String = "LivingModpackExported"
    Private ReadOnly Service As New ModeCore(CreateHost())

    Private Function CreateHost() As PCLCS.ModpackModeHost(Of MyPageRight, McFolder, McInstance)
        Return New PCLCS.ModpackModeHost(Of MyPageRight, McFolder, McInstance) With {
            .Routes = New PCLCS.ModpackModeRoutes With {
                .SetupLaunch = CInt(FormMain.PageSubType.SetupLaunch), .SetupSystem = CInt(FormMain.PageSubType.SetupSystem),
                .Download = CInt(FormMain.PageType.Download), .Other = CInt(FormMain.PageType.Other), .Link = CInt(FormMain.PageType.Link),
                .SetupPage = CInt(FormMain.PageType.Setup), .InstanceBackups = CInt(FormMain.PageSubType.InstanceBackups),
                .InstanceSaves = CInt(FormMain.PageSubType.InstanceSaves), .InstanceExport = CInt(FormMain.PageSubType.InstanceExport),
                .InstanceOverall = CInt(FormMain.PageSubType.InstanceOverall)},
            .Setting = Function(Key) Settings.Get(Of Boolean)(Key),
            .SetSetting = Sub(Key, Value) Settings.Set(Key, Value),
            .ForceShow = Function() PageSetupUI.HiddenForceShow,
            .CurrentPageIsSetup = Function() FrmMain IsNot Nothing AndAlso FrmMain.PageCurrent = FormMain.PageType.Setup,
            .BaseFolder = Function() Paths.Base,
            .MinecraftFolder = Function() McFolderSelected,
            .LauncherDisplayName = Function() LauncherDisplayName,
            .SystemPage = Function() FrmSetupSystem,
            .SetSystemPage = Sub(Page) FrmSetupSystem = DirectCast(Page, PageSetupSystem),
            .CreateSystemPage = Function() New PageSetupSystem,
            .CreateCompactPage = Function() New PageSetupModpack,
            .MainWindow = Function() FrmMain,
            .Header = AddressOf HeaderView,
            .Setup = AddressOf SetupView,
            .Launch = AddressOf LaunchView,
            .Instance = AddressOf InstanceView,
            .Message = Function(Message) MyMsgBox(Message.Text, Message.Title, Message.Button1, Message.Button2, IsWarn:=Message.IsWarn, ForceWait:=Message.ForceWait),
            .RunInUi = Sub(Work) RunInUi(Work),
            .RunInNewThread = Sub(Work, Name) RunInNewThread(Work, Name),
            .RefreshHidden = Sub() PageSetupUI.HiddenRefresh(),
            .UpdateTitle = Sub() FrmMain?.UpdateTitleBar(),
            .RefreshLaunchButtons = Sub() FrmLaunchLeft?.RefreshButtonsUI(),
            .ChangeToSystemPage = Sub() FrmSetupLeft?.PageChange(FormMain.PageSubType.SetupSystem, True),
            .PackFolder = AddressOf MinecraftFolderPolicy.PackFolder,
            .IsPackVersion = AddressOf MinecraftFolderPolicy.IsPackVersion,
            .CreateFolder = Function(Name, Folder) New McFolder With {.Name = Name, .Location = Folder, .Type = McFolder.Types.Vanilla},
            .ReadSelectedName = Function(Folder) ReadIni(Folder & "PCL.ini", "Version"),
            .ForceInstanceRefresh = Sub() McInstanceListForceRefresh = True,
            .StartFolderList = Sub() McFolderListLoader.Start(IsForceRestart:=True),
            .RefreshSelect = Sub() FrmSelectLeft?.RefreshForModpackMode(),
            .WaitFolderList = Sub() McFolderListLoader.WaitForExit(),
            .RunInstanceList = Sub(Folder) LoaderFolderRun(McInstanceListLoader, Folder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\", WaitForExit:=True),
            .Instances = Function() McInstanceList.Values.SelectMany(Function(List) List),
            .InstanceError = Function(Instance) Instance.State = McInstance.McInstanceState.Error,
            .InstanceName = Function(Instance) Instance.Name,
            .SelectInstance = Sub(Instance) McInstanceSelected = Instance,
            .RefreshLaunchPage = Sub(KeepInput, Anim) FrmLaunchLeft?.RefreshPage(KeepInput, Anim),
            .WriteIni = Sub(Name, Key, Value) WriteIni(Name, Key, CStr(Value))}
    End Function
    Private Function HeaderView(Main As Window) As PCLCS.ModpackModeHeaderView
        Dim View = DirectCast(Main, FormMain)
        Return New PCLCS.ModpackModeHeaderView With {.Panel = View.PanTitleSelect, .Download = View.BtnTitleSelect1,
            .Link = View.BtnTitleSelect2, .Setup = View.BtnTitleSelect3, .Other = View.BtnTitleSelect4,
            .SetupText = Sub(Value) View.BtnTitleSelect3.Text = Value}
    End Function
    Private Function SetupView() As PCLCS.ModpackModeSetupView
        Dim View = FrmSetupLeft
        If View Is Nothing Then Return Nothing
        Return New PCLCS.ModpackModeSetupView With {.Launch = View.ItemLaunch, .System = View.ItemSystem,
            .UI = View.ItemUI, .Link = View.ItemLink, .ItemsPanel = View.PanItem,
            .SystemTitle = Sub(Value) View.ItemSystem.Title = Value, .SystemButtons = Function() View.ItemSystem.Buttons}
    End Function
    Private Function LaunchView() As PCLCS.ModpackModeLaunchView
        Dim View = FrmLaunchLeft
        If View Is Nothing Then Return Nothing
        Return New PCLCS.ModpackModeLaunchView With {.Version = View.BtnVersion, .More = View.BtnMore}
    End Function
    Private Function InstanceView() As PCLCS.ModpackModeInstanceView
        Return New PCLCS.ModpackModeInstanceView With {
            .Export = FrmInstanceLeft?.ItemExport,
            .ModDownload = FrmInstanceMod?.BtnManageDownload,
            .ModHintDownload = FrmInstanceMod?.BtnHintDownload,
            .DisabledVersion = FrmInstanceModDisabled?.BtnVersion,
            .RefreshOverview = Sub() FrmInstanceOverall?.RefreshModpackVisibility(),
            .RefreshModify = Sub() FrmDownloadModify?.RefreshModpackRestrictions(),
            .ModEmptyHint = Sub(Text) FrmInstanceMod.LabEmptyHint.Text = Text,
            .DisabledHint = Sub(Text) FrmInstanceModDisabled.LabModHint.Text = Text,
            .RefreshDisabledDownload = Sub() FrmInstanceModDisabled.BtnDownload_Loaded()}
    End Function

    Private Property CompactSettings As PageSetupModpack
        Get
            Return DirectCast(Service.CompactSettings, PageSetupModpack)
        End Get
        Set(value As PageSetupModpack)
            Service.CompactSettings = value
        End Set
    End Property
    Private Property FolderScopeRevision As Integer
        Get
            Return Service.FolderScopeRevision
        End Get
        Set(value As Integer)
            Service.FolderScopeRevision = value
        End Set
    End Property
    Friend ReadOnly Property CanEnable As Boolean
        Get
            Return Service.CanEnable
        End Get
    End Property
    Friend ReadOnly Property Enabled As Boolean
        Get
            Return Service.Enabled
        End Get
    End Property
    Friend Function LauncherSettingsPage() As MyPageRight
        Return Service.LauncherSettingsPage()
    End Function
    Friend Function NormalizeSetupPage(Page As FormMain.PageSubType) As FormMain.PageSubType
        Return CType(Service.NormalizeSetupPage(CInt(Page)), FormMain.PageSubType)
    End Function
    Friend Function BlocksPage(Page As FormMain.PageType) As Boolean
        Return Service.BlocksPage(CInt(Page))
    End Function
    Friend Function ConfirmChange(Current As Boolean, Desired As Boolean, Confirm As Func(Of Boolean, Boolean)) As Boolean
        Return ModeCore.ConfirmChange(Current, Desired, Confirm)
    End Function
    Friend Function ShowWarning(Enable As Boolean) As Boolean
        Return Service.ShowWarning(Enable)
    End Function
    Friend Sub Toggle(Check As MyCheckBox, User As Boolean)
        Service.Toggle(Function() Check.Checked, Sub(Value) Check.Checked = Value, User)
    End Sub
    Friend Sub OnChanged(Value As Object)
        Service.OnChanged(Value)
    End Sub
    Friend Function DisplayTitle(Title As String) As String
        Return Service.DisplayTitle(Title)
    End Function
    Friend Sub ApplyVisibility(Optional Main As FormMain = Nothing)
        Service.ApplyVisibility(Main)
    End Sub
    Friend Function NormalizeInstancePage(Page As FormMain.PageSubType) As FormMain.PageSubType
        Return CType(Service.NormalizeInstancePage(CInt(Page)), FormMain.PageSubType)
    End Function
    Friend Function CanChangeMinecraft(Current As String, Requested As String) As Boolean
        Return Service.CanChangeMinecraft(Current, Requested)
    End Function
    Friend Function ConfirmModAction(ActionName As String, Optional Confirm As Func(Of String, Boolean) = Nothing) As Boolean
        Return Service.ConfirmModAction(ActionName, Confirm)
    End Function
    Friend Sub ApplyInstanceVisibility()
        Service.ApplyInstanceVisibility()
    End Sub
    Private Sub HideButtonColumn(Button As FrameworkElement)
        Service.HideButtonColumn(Button)
    End Sub
    Friend Sub ApplyHeader(Panel As Panel, Download As UIElement, Link As UIElement, Setup As MyRadioButton, Other As UIElement)
        Service.ApplyHeader(New PCLCS.ModpackModeHeaderView With {.Panel = Panel, .Download = Download, .Link = Link,
            .Setup = Setup, .Other = Other, .SetupText = Sub(Value) Setup.Text = Value})
    End Sub
    Friend Sub ApplyLaunchLayout(Target As PageLaunchLeft)
        ModeCore.ApplyLaunchLayout(Target.BtnMore)
    End Sub
    Friend Function CreatePackFolderList() As List(Of McFolder)
        Return Service.CreatePackFolderList()
    End Function
    Friend Function AllowsInstance(Folder As String) As Boolean
        Return Service.AllowsInstance(Folder)
    End Function
    Private Sub RefreshFolderScope()
        Service.RefreshFolderScope()
    End Sub
    Friend Sub ConfigureExport(ExportRoot As String, IncludeLauncher As Boolean, PackMode As Boolean)
        Service.ConfigureExport(ExportRoot, IncludeLauncher, PackMode)
    End Sub
End Module
