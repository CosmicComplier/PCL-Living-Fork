Imports PCLCS
Imports PCLLF.Presentation.Controls

' LF settings pages keep their XAML/DP owners and real platform/model identities.
' All page policy, workflows and mutable page state are implemented in C#.
Friend NotInheritable Class SetupPageRuntime
    Friend Shared ReadOnly Common As New LegacySetupPageHost With {
        .Read = Function(Key) Settings.Get(Of Object)(Key),
        .Write = Sub(Key, Value) Settings.Set(Key, Value),
        .Reset = Sub(Key) Settings.Reset(Key),
        .Default = AddressOf Settings.GetDefault,
        .RefreshSettings = Sub(Page) SettingService.RefreshSettings(Page),
        .ResetSettings = Sub(Page) SettingService.ResetSettings(Page),
        .AnimationSuppression = Function() AniControlEnabled,
        .AddAnimationSuppression = Sub(Delta) AniControlEnabled += Delta,
        .Message = Function(Text, Title, Button1, Button2, Button3, Warning) MyMsgBox(Text, Title, Button1, Button2, Button3, IsWarn:=Warning),
        .Hint = Sub(Text, Kind, Log) ModMain.Hint(Text, CType(Kind, HintType), Log),
        .BaseFolder = Function() Paths.Base,
        .ImageRoot = Function() PathImage,
        .AppData = Function() Paths.AppData,
        .UserDataFolder = Function() Paths.AppDataThenName,
        .OpenExplorer = AddressOf ModBase.OpenExplorer,
        .OpenWebsite = AddressOf ModBase.OpenWebsite,
        .DeleteDirectory = Sub(Path) DirectoryUtils.Delete(Path),
        .InThread = AddressOf RunInThread,
        .InUi = Sub(Work) RunInUi(Work),
        .Sleep = Sub(Delay) Thread.Sleep(Delay),
        .ExtractResource = AddressOf ExtractResources,
        .LoadBitmap = Function(Path) New MyBitmap(Path),
        .SelectImage = Function(Title, Extensions, Label) Dialogs.SelectFile(Title, False, filter:={(Extensions, Label)}),
        .StartAnimations = AddressOf StartAnimations,
        .StopAnimation = Sub(Name) AniStop(Name),
        .AnimationRunning = AddressOf AniIsRun
    }
    Friend Shared ReadOnly UI As New LegacyPageSetupUIRuntime(New LegacySetupUiHost With {
        .Common = Common,
        .CurrentUI = Function() If(FrmSetupUI Is Nothing, Nothing, FrmSetupUI.Controller),
        .ReadHiddenForceShow = AddressOf PageSetupUI.ReadHiddenForceShow,
        .WriteHiddenForceShow = AddressOf PageSetupUI.WriteHiddenForceShow,
        .HasMain = Function() FrmMain IsNot Nothing,
        .MainPanel = Function() FrmMain.PanForm,
        .MainBackground = Function() FrmMain.ImgBack,
        .TitleLogo = Function() FrmMain.ImageTitleLogo,
        .ApplyBackground = Sub(Panel, Image) ApplyMainBackground(Panel, Image),
        .UseDefaultBackground = AddressOf ModTheme.UseDefaultBackground,
        .MarkCustomBackground = AddressOf ModTheme.MarkCustomBackground,
        .UpdateBackgroundAndTitle = Sub() FrmMain.UpdateBackgroundAndTitleBar(),
        .RandomPicture = Function(Pictures) RandomOne(Pictures),
        .MusicAll = Function() MusicAllList,
        .ClearMusicPlayer = Sub() MusicNAudio = Nothing,
        .AssignWaitingMusic = Sub(Values) MusicWaitingList = Values,
        .AssignAllMusic = Sub(Values) MusicAllList = Values,
        .RefreshMusic = Sub(ShowHint) MusicRefreshPlay(ShowHint),
        .RefreshHome = Sub() FrmLaunchRight.ForceRefresh(),
        .SetHomeHintEvent = Sub(Control, OpenWeb) CustomEventService.SetEventType(Control, If(OpenWeb, CustomEvent.EventType.打开网页, CustomEvent.EventType.None)),
        .SetHomeHintEventData = AddressOf CustomEventService.SetEventData,
        .Shell = AddressOf Shell,
        .HasShell = AddressOf HasShell,
        .IsPage = AddressOf IsPage,
        .RefreshLaunchButtons = Sub() FrmLaunchLeft.RefreshButtonsUI(),
        .RefreshEmptyDownload = Sub() FrmSelectRight.BtnEmptyDownload_Loaded(),
        .RefreshDisabledModsDownload = Sub() FrmInstanceModDisabled.BtnDownload_Loaded(),
        .ApplyModpackVisibility = AddressOf ModModpackMode.ApplyVisibility,
        .HasDisabledModsPage = Function() FrmInstanceModDisabled IsNot Nothing
    })
    Friend Shared ReadOnly Launch As New LegacyPageSetupLaunchRuntime(Of McInstance, McSkinInfo)(New LegacySetupLaunchHost(Of McInstance, McSkinInfo) With {
        .Common = Common,
        .CurrentLaunch = Function() If(FrmSetupLaunch Is Nothing, Nothing, FrmSetupLaunch.Controller),
        .SelectedInstance = Function() McInstanceSelected,
        .InstanceName = Function(Instance) Instance.Name,
        .InstanceIndiePath = Function(Instance) Instance.PathIndie,
        .InstanceLoaded = Function(Instance) Instance.IsLoaded,
        .InstanceModable = Function(Instance) Instance.Modable,
        .InstanceOptiFine = Function(Instance) Instance.Version.HasOptiFine,
        .LoadInstance = Sub(Instance) Instance.Load(),
        .AssignSetupInstance = Sub(Instance) PageInstanceLeft.Instance = Instance,
        .ImportPersonalOptions = AddressOf PersonalGameSettings.ImportFrom,
        .ShowInstanceSetup = Sub() FrmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.InstanceSetup),
        .PersonalProfileExists = AddressOf PersonalGameSettings.HasProfile,
        .IsCurrentRamPage = Function() FrmMain.PageCurrent = FormMain.PageType.Setup AndAlso FrmSetupLeft.PageID = FormMain.PageSubType.SetupLaunch,
        .ApplyInstanceVisibility = AddressOf ModModpackMode.ApplyInstanceVisibility,
        .AvailableMemory = Function() My.Computer.Info.AvailablePhysicalMemory,
        .TotalMemory = Function() My.Computer.Info.TotalPhysicalMemory,
        .SelectSkin = AddressOf McSkinSelect,
        .SkinValid = Function(Skin) Skin.IsVaild,
        .SkinSlim = Function(Skin) Skin.IsSlim,
        .SkinPath = Function(Skin) Skin.LocalFile,
        .StartLegacySkin = Sub(Force) PageLaunchLeft.SkinLegacy.Start(IsForceRestart:=Force),
        .SaveSkin = Sub() MySkin.Save(PageLaunchLeft.SkinLegacy),
        .RefreshSkinCache = Sub() MySkin.RefreshCache(Nothing),
        .StartJavaRefresh = Sub() JavaListRefreshWorker.Start(),
        .SearchJava = AddressOf ManuallySearchJava,
        .ImportJava = AddressOf ManuallyImportJava,
        .UpdateJavaLists = AddressOf ModJava.UpdateJavaLists,
        .RemoveJava = AddressOf ManuallyRemoveJava,
        .JavaRefreshRunning = Function() JavaListRefreshWorker.Running,
        .NewJavaItem = Function() New MyListItem(),
        .NewJavaButton = Function() New MyIconButton(),
        .ConnectItemClick = AddressOf ConnectItemClick,
        .ConnectIconClick = AddressOf ConnectIconClick,
        .SetJavaButtons = Sub(Item, Buttons) DirectCast(Item, MyListItem).Buttons = Buttons.Cast(Of MyIconButton)().ToList(),
        .JavaUpLogo = Logo.IconButtonArrowUp,
        .JavaDownLogo = Logo.IconButtonArrowDown,
        .JavaDeleteLogo = Logo.IconButtonStop,
        .JavaOpenLogo = Logo.IconButtonOpen,
        .HasAdminRole = AddressOf WindowsUtils.HasAdminRole
    })
    Friend Shared ReadOnly SystemHost As New LegacySetupSystemHost With {
        .Common = Common,
        .ModpackEnabled = Function() ModModpackMode.Enabled,
        .CanEnableModpack = Function() ModModpackMode.CanEnable,
        .ToggleModpack = Sub(Control, User) ModModpackMode.Toggle(DirectCast(Control, MyCheckBox), User),
        .LauncherDisplayName = Function() ModBase.LauncherDisplayName,
        .LauncherVersion = Function() VersionBaseName,
        .CheckLauncherUpdate = AddressOf UpdateCheckByButton
    }
    Private Shared Sub ConnectItemClick(Item As MyListItemView, Work As Action)
        AddHandler DirectCast(Item, MyListItem).Click, Sub() Work()
    End Sub
    Private Shared Sub ConnectIconClick(Button As MyIconButtonView, Work As Action)
        AddHandler DirectCast(Button, MyIconButton).Click, Sub() Work()
    End Sub
    Private Shared Sub StartAnimations(Plans As IEnumerable(Of LegacySetupAnimation), Name As String)
        AniStart(Plans.Select(AddressOf CreateAnimation).ToArray(), Name)
    End Sub
    Private Shared Function CreateAnimation(Plan As LegacySetupAnimation) As AniData
        Dim Ease As AniEase = Nothing
        Select Case Plan.Ease
            Case LegacySetupEase.OutFluentWeak : Ease = New AniEaseOutFluent(AniEasePower.Weak)
            Case LegacySetupEase.OutFluentStrong : Ease = New AniEaseOutFluent(AniEasePower.Strong)
        End Select
        Select Case Plan.Kind
            Case LegacySetupAnimationKind.GridWidth
                Return AaGridLengthWidth(DirectCast(Plan.Target, ColumnDefinition), Plan.Delta, Plan.Duration,, Ease)
            Case LegacySetupAnimationKind.Opacity
                Return AaOpacity(Plan.Target, Plan.Delta, Plan.Duration,, Ease)
            Case LegacySetupAnimationKind.X
                Return AaX(Plan.Target, Plan.Delta, Plan.Duration,, Ease)
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(Plan))
        End Select
    End Function
    Private Shared Function HasShell(Part As LegacySetupShellPart) As Boolean
        Select Case Part
            Case LegacySetupShellPart.InstanceLeft : Return FrmInstanceLeft IsNot Nothing
            Case LegacySetupShellPart.SetupLeft : Return FrmSetupLeft IsNot Nothing
            Case LegacySetupShellPart.OtherLeft : Return FrmOtherLeft IsNot Nothing
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Part))
        End Select
    End Function
    Private Shared Function IsPage(Page As LegacySetupShellPage) As Boolean
        Select Case Page
            Case LegacySetupShellPage.Launch : Return FrmMain.PageCurrent = FormMain.PageType.Launch
            Case LegacySetupShellPage.InstanceSelect : Return FrmMain.PageCurrent = FormMain.PageType.InstanceSelect
            Case LegacySetupShellPage.InstanceSetup : Return FrmMain.PageCurrent = FormMain.PageType.InstanceSetup
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Page))
        End Select
    End Function
    Private Shared Function Shell(Element As LegacySetupShellElement) As FrameworkElement
        Select Case Element
            Case LegacySetupShellElement.TitlePanel : Return FrmMain.PanTitleSelect
            Case LegacySetupShellElement.TitleDownload : Return FrmMain.BtnTitleSelect1
            Case LegacySetupShellElement.TitleLink : Return FrmMain.BtnTitleSelect2
            Case LegacySetupShellElement.TitleSetup : Return FrmMain.BtnTitleSelect3
            Case LegacySetupShellElement.TitleOther : Return FrmMain.BtnTitleSelect4
            Case LegacySetupShellElement.ModifyInstance : Return FrmInstanceLeft.ItemModify
            Case LegacySetupShellElement.SetupLaunch : Return FrmSetupLeft.ItemLaunch
            Case LegacySetupShellElement.SetupUI : Return FrmSetupLeft.ItemUI
            Case LegacySetupShellElement.SetupLink : Return FrmSetupLeft.ItemLink
            Case LegacySetupShellElement.SetupSystem : Return FrmSetupLeft.ItemSystem
            Case LegacySetupShellElement.SetupNavigation : Return FrmSetupLeft.PanItem
            Case LegacySetupShellElement.OtherHelp : Return FrmOtherLeft.ItemHelp
            Case LegacySetupShellElement.OtherAbout : Return FrmOtherLeft.ItemAbout
            Case LegacySetupShellElement.OtherTest : Return FrmOtherLeft.ItemTest
            Case LegacySetupShellElement.OtherNavigation : Return FrmOtherLeft.PanItem
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Element))
        End Select
    End Function
End Class
