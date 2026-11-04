Imports SetupController = PCLCS.InstanceSettingsController(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.MyListItem, PCL.MyIconButton)
Imports SetupHost = PCLCS.InstanceSettingsHost(Of PCL.ModMinecraft.McInstance)

Public Class PageInstanceSetup
    Private LoadedFirst As Boolean = False
    Private ComboServerLoginLast As Integer
    Private RamTextLeft As Integer = 2, RamTextRight As Integer = 1
    Private ReadOnly SettingsView As New PCLCS.InstanceSettingsView(Of MyListItem, MyIconButton)
    Private ReadOnly RamView As New PCLCS.InstanceRamDisplayView
    Private RamController As PCLCS.InstanceRamDisplayController
    Private PageController As SetupController
    Private ReadOnly Property Controller As SetupController
        Get
            UpdateViews()
            If PageController Is Nothing Then
                RamController = New PCLCS.InstanceRamDisplayController(RamView, New PCLCS.InstanceRamDisplayHost With {
                    .Active = Function() FrmMain.PageCurrent = FormMain.PageType.InstanceSetup AndAlso FrmInstanceLeft.PageID = FormMain.PageSubType.InstanceSetup,
                    .Ram = Function() GetRam(PageInstanceLeft.Instance), .TotalBytes = Function() CDbl(My.Computer.Info.TotalPhysicalMemory),
                    .AvailableBytes = Function() CDbl(My.Computer.Info.AvailablePhysicalMemory),
                    .Animation = Function() AniControlEnabled, .IsAnimating = AddressOf AniIsRun,
                    .AnimateGrid = Sub(Used, Game, Empty) AniStart({AaGridLengthWidth(ColumnRamUsed, Used, 800,, New AniEaseOutFluent(AniEasePower.Strong)), AaGridLengthWidth(ColumnRamGame, Game, 800,, New AniEaseOutFluent(AniEasePower.Strong)), AaGridLengthWidth(ColumnRamEmpty, Empty, 800,, New AniEaseOutFluent(AniEasePower.Strong))}, "VersionSetup Ram Grid"),
                    .AnimateLeft = Sub(Used, Total, Title) AniStart({AaOpacity(LabRamUsed, Used, 100), AaOpacity(LabRamTotal, Total, 100), AaOpacity(LabRamUsedTitle, Title, 100)}, "VersionSetup Ram TextLeft"),
                    .AnimateRight = Sub(Game, Title) AniStart({AaX(LabRamGame, Game, 100,, New AniEaseOutFluent(AniEasePower.Weak)), AaX(LabRamGameTitle, Title, 100,, New AniEaseOutFluent(AniEasePower.Weak))}, "VersionSetup Ram TextRight")})
                PageController = New SetupController(SettingsView, CreateHost(), RamController)
            End If
            Return PageController
        End Get
    End Property
    Private Sub UpdateViews()
        SettingsView.Isolation = ComboArgumentIndieV2
        SettingsView.Login = ComboServerLogin
        SettingsView.JavaKind = ComboArgumentJava
        SettingsView.JavaSelect = ComboArgumentJavaSelect
        SettingsView.ServerEnter = TextServerEnter
        SettingsView.AuthServer = TextServerAuthServer
        SettingsView.AuthRegister = TextServerAuthRegister
        SettingsView.AuthName = TextServerAuthName
        SettingsView.JavaRange = TextArgumentJavaRange
        SettingsView.AdvanceRun = TextAdvanceRun
        SettingsView.Slider = SliderRamCustom
        SettingsView.JavaHint = HintArgumentJava
        SettingsView.JavaSelectPanel = PanArgumentJavaSelect
        SettingsView.JavaSearch = BtnArgumentJavaSearch
        SettingsView.AdvanceRunWait = CheckAdvanceRunWait
        SettingsView.JavaLabel = LabArgumentJava
        SettingsView.NideControls = {LabServerNide, TextServerNide, PanServerNide}
        SettingsView.AuthControls = {LabServerAuthName, TextServerAuthName, LabServerAuthRegister, TextServerAuthRegister, LabServerAuthServer, TextServerAuthServer, BtnServerAuthLittle}
        SettingsView.ScrollHome = Sub() PanBack.ScrollToHome()
        SettingsView.ResizeServer = Sub() CardServer.TriggerForceResize()
        SettingsView.LoadedFirst = Function() LoadedFirst
        SettingsView.SetLoadedFirst = Sub(Value) LoadedFirst = Value
        SettingsView.LoginLast = Function() ComboServerLoginLast
        SettingsView.SetLoginLast = Sub(Value) ComboServerLoginLast = Value
        SettingsView.NideValidated = Function() TextServerNide.IsValidated
        SettingsView.AuthValidated = Function() TextServerAuthServer.IsValidated
        SettingsView.JavaRangeError = Function() TextArgumentJavaRange.ValidateResult
        SettingsView.SetJavaHint = Sub(Text, Theme)
                                       HintArgumentJava.Text = Text
                                       HintArgumentJava.Theme = CType(Theme, MyHint.Themes)
                                   End Sub
        SettingsView.SetJavaRangeValidator = Sub(ValidateText) TextArgumentJavaRange.ValidateRules = New ObjectModel.Collection(Of Validate) From {New ValidateFunc(Function(Text) ValidateText(Text))}
        SettingsView.CreateJavaItem = Function(Entry, Title) New MyListItem With {.FontSize = 13, .Height = 24, .IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable, .Tag = Entry, .Title = Title}
        SettingsView.OnJavaClick = Sub(Item, Work) AddHandler Item.Click, Sub() Work()
        SettingsView.CreateJavaButton = Function(Open) If(Open, New MyIconButton With {.Logo = Logo.IconButtonOpen, .LogoScale = 1.1, .Height = 24, .Width = 24}, New MyIconButton With {.Logo = Logo.IconButtonStop, .Height = 24, .Width = 24})
        SettingsView.OnButtonClick = Sub(Button, Work) AddHandler Button.Click, Sub() Work()
        SettingsView.SetButtons = Sub(Item, Buttons) Item.Buttons = Buttons
        RamView.Game = LabRamGame
        RamView.Used = LabRamUsed
        RamView.Total = LabRamTotal
        RamView.GameTitle = LabRamGameTitle
        RamView.UsedTitle = LabRamUsedTitle
        RamView.RectUsed = RectRamUsed
        RamView.Display = PanRamDisplay
        RamView.UsedColumn = ColumnRamUsed
        RamView.GameColumn = ColumnRamGame
        RamView.EmptyColumn = ColumnRamEmpty
        RamView.LeftState = Function() RamTextLeft
        RamView.SetLeftState = Sub(Value) RamTextLeft = Value
        RamView.RightState = Function() RamTextRight
        RamView.SetRightState = Sub(Value) RamTextRight = Value
        RamView.SetSliderMax = Sub(Value) SliderRamCustom.MaxValue = Value
    End Sub
    Private Function CreateHost() As SetupHost
        Return New SetupHost With {
            .Instance = Function() PageInstanceLeft.Instance, .SetInstance = Sub(Value) PageInstanceLeft.Instance = Value,
            .Integer = Function(Key, Target) Settings.Get(Of Integer)(Key, Instance:=Target),
            .Boolean = Function(Key, Target) Settings.Get(Of Boolean)(Key, Instance:=Target),
            .HasSaved = Function(Key, Target) Settings.HasSaved(Key, Target),
            .Set = Sub(Key, Value, Target) Settings.Set(Key, Value, Target), .Reset = Sub(Key, Target) Settings.Reset(Key, Target),
            .ApplyVisibility = AddressOf ModModpackMode.ApplyInstanceVisibility,
            .RefreshSettings = Sub() SettingService.RefreshSettings(Me), .ResetSettings = Sub() SettingService.ResetSettings(Me),
            .Animation = Function() AniControlEnabled, .AddAnimation = Sub(Value) AniControlEnabled += Value,
            .RunInUi = Sub(Work) RunInUi(Work), .JavaWorker = Function() PageInstanceLeft.CurrentJavaWorker, .JavaRefresh = Function() JavaListRefreshWorker,
            .JavaRequirement = Function(Target) GetJavaRequirement(Target).Range,
            .ReloadCurrentJava = AddressOf PageInstanceLeft.ReloadCurrentJava,
            .ImportJava = Sub() ManuallyImportJava(), .SearchJava = AddressOf ManuallySearchJava, .RemoveJava = AddressOf ManuallyRemoveJava,
            .OpenInstanceFolder = AddressOf PageInstanceOverall.OpenInstanceFolder, .Explorer = Sub(Path) OpenExplorer(Path),
            .Info = Sub(Text) Logger.Info(Text), .HintGreen = Sub(Text, Log) Hint(Text, HintType.Green, Log),
            .Error = Sub(Err, Text) Logger.Error(Err, Text), .ErrorAlert = Sub(Err, Text) Logger.Error(Err, Text, LogBehavior.Alert),
            .Confirm = Function(Text, Title, First, Second, Warn) MyMsgBox(Text, Title, First, Second, IsWarn:=Warn),
            .ClearInstanceCache = Sub() WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", ""),
            .RefreshInstanceList = Sub() LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\"),
            .LoadInstance = Function(Name) New McInstance(Name).Load(),
            .SwitchGlobal = Sub() FrmMain.PageChange(FormMain.PageType.Setup, FormMain.PageSubType.SetupLaunch),
            .LeftQuote = vbLQ, .RightQuote = vbRQ}
    End Function
    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub
    Public Sub Reload()
        Controller.Reload()
    End Sub
    Public Sub Reset()
        Controller.Reset()
    End Sub
    Public Shared Sub OnVersionRamTypeChanged(Type As Integer)
        FrmInstanceSetup?.RamType(Type)
    End Sub
    Public Sub RamType(Type As Integer)
        Controller.RamType(Type)
    End Sub
    Public Sub RefreshRam(ShowAnim As Boolean)
        Dim UnusedController = Controller
        RamController.Refresh(ShowAnim)
    End Sub
    Private Sub RefreshRam() Handles SliderRamCustom.Change, RadioRamType0.Check, RadioRamType1.Check, RadioRamType2.Check
        RefreshRam(True)
    End Sub
    Private Sub RefreshRamText() Handles RectRamGame.SizeChanged, RectRamEmpty.SizeChanged, LabRamGame.SizeChanged
        Dim UnusedController = Controller
        RamController.RefreshText()
    End Sub
    Public Shared Function GetRam(Instance As McInstance) As Double
        Return PCLCS.InstanceRamPolicy.Get(Of McInstance, McInstance.McInstanceState, McInstanceCardType)(Instance, Function(Key, Target) Settings.Get(Of Integer)(Key, Instance:=Target), Function() CDbl(My.Computer.Info.AvailablePhysicalMemory), Function(Target) PageSetupLaunch.GetRam(Target, True))
    End Function
    Public Shared Sub OnVersionServerLoginChanged(Type As Integer)
        PCLCS.InstanceSettingsRules.ServerLoginChanged(Function() FrmInstanceSetup IsNot Nothing, Function() PageInstanceLeft.Instance, Sub(Target) PageInstanceLeft.Instance = Target, Function(Name) New McInstance(Name).Load(), Sub() WriteIni(McFolderSelected & "PCL.ini", "InstanceCache", ""), Sub() LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\"), Function(Target) Target.Name)
    End Sub
    Private Sub TextServerEnter_Change() Handles TextServerEnter.TextChanged
        Controller.NormalizeServerPunctuation()
    End Sub
    Private Sub ComboServerLogin_Changed() Handles ComboServerLogin.SelectionChanged, TextServerNide.ValidatedTextChanged, TextServerAuthServer.ValidatedTextChanged, TextServerAuthRegister.ValidatedTextChanged
        Controller.ServerLoginChanged()
    End Sub
    Private Sub UpdateServerLoginUI()
        Controller.UpdateServerLoginUi()
    End Sub
    Private Sub BtnServerAuthLittle_Click(sender As Object, e As EventArgs) Handles BtnServerAuthLittle.Click
        Controller.SetLittleSkin()
    End Sub
    Private Sub ReloadCurrentJava() Handles ComboArgumentJava.SelectionChanged, TextArgumentJavaRange.ValidatedTextChanged
        Controller.ReloadCurrentJava()
    End Sub
    Private Sub UpdateJavaRelatedUi() Handles ComboArgumentJava.SelectionChanged, TextArgumentJavaRange.TextChanged
        Controller.UpdateJavaRelatedUi()
    End Sub
    Private Sub _UpdateJavaRelatedUi()
        Controller.UpdateJavaUi()
    End Sub
    Private Sub HintArgumentJava_Click(sender As Object, e As MouseButtonEventArgs) Handles HintArgumentJava.Click
        Controller.JavaHintClick()
    End Sub
    Public Sub UpdateJavaList()
        Controller.UpdateJavaList()
    End Sub
    Private Sub BtnAdvanceJavaSearch_Click() Handles BtnArgumentJavaSearch.Click
        Controller.SearchJava()
    End Sub
    Private Sub ComboArgumentIndieV2_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentIndieV2.SelectionChanged
        Controller.IsolationChanged(CType(sender, ComboBox), e)
    End Sub
    Private Sub TextAdvanceRun_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextAdvanceRun.TextChanged
        Controller.AdvanceRunChanged()
    End Sub
    Private Sub BtnSwitch_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSwitch.Click
        Controller.SwitchGlobal()
    End Sub
    Private Sub ReplaceEnter(sender As MyTextBox, e As TextChangedEventArgs) Handles TextAdvanceJvm.TextChanged, TextAdvanceGame.TextChanged
        SetupController.ReplaceEnter(sender)
    End Sub
End Class
