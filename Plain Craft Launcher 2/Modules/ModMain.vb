Option Strict On
Imports System.Collections.ObjectModel
Imports PCLLF.Presentation.Controls
Imports PCLLF.Presentation.Shell

'LF 原类型、公开字段及真实平台/模型投影；全部队列处理、帮助、主时钟和系统业务在 C#。
Public Module ModMain
    Public Enum HintType
        Blue
        Green
        Red
    End Enum
    Public Enum MyMsgBoxType
        Text
        [Select]
        Input
        Login
    End Enum
    Public Class MyMsgBoxConverter
        Inherits MainMessageData(Of MyMsgBoxType, Validate)
    End Class

    Private HintWaiting As New ConcurrentQueue(Of MainHintData(Of HintType))
    Public WaitingMyMsgBox As New ConcurrentQueue(Of MyMsgBoxConverter)
    Public IsAprilGiveup As Boolean = False
    Public DragControl As Object = Nothing
    '窗体声明
    Public FrmMain As FormMain
    Public FrmStart As SplashScreen

    '页面声明（出于单元测试考虑，初始化页面已转入 FormMain 中）
    Public FrmLaunchLeft As PageLaunchLeft
    Public FrmLaunchRight As PageLaunchRight
    Public FrmSelectLeft As PageSelectLeft
    Public FrmSelectRight As PageSelectRight
    Public FrmSpeedLeft As PageSpeedLeft
    Public FrmSpeedRight As PageSpeedRight

    '联机页面声明
#If LEGACY_LINK Then
    Public FrmLinkMain As PageLinkMain
#End If

    '下载页面声明
    Public FrmDownloadLeft As PageDownloadLeft
    Public FrmDownloadInstall As PageDownloadInstall
    Public FrmDownloadMod As PageDownloadMod
    Public FrmDownloadPack As PageDownloadPack
    Public FrmDownloadDataPack As PageDownloadDataPack
    Public FrmDownloadShader As PageDownloadShader
    Public FrmDownloadResourcePack As PageDownloadResourcePack

    '设置页面声明
    Public FrmSetupLeft As PageSetupLeft
    Public FrmSetupLaunch As PageSetupLaunch
    Public FrmSetupUI As PageSetupUI
    Public FrmSetupSystem As PageSetupSystem
#If LEGACY_LINK Then
    Public FrmSetupLink As PageSetupLink
#End If

    '其他页面声明
    Public FrmOtherLeft As PageOtherLeft
    Public FrmOtherHelp As PageOtherHelp
    Public FrmOtherAbout As PageOtherAbout
    Public FrmOtherTest As PageOtherTest

    '登录页面声明
    Public FrmLoginLegacy As PageLoginLegacy
    Public FrmLoginNide As PageLoginNide
    Public FrmLoginNideSkin As PageLoginNideSkin
    Public FrmLoginAuth As PageLoginAuth
    Public FrmLoginAuthSkin As PageLoginAuthSkin
    Public FrmLoginMs As PageLoginMs
    Public FrmLoginMsSkin As PageLoginMsSkin

    '版本设置页面声明
    Public FrmInstanceLeft As PageInstanceLeft
    Public FrmInstanceOverall As PageInstanceOverall
    Public FrmInstanceMod As PageInstanceMod
    Public FrmInstanceModDisabled As PageInstanceModDisabled
    Public FrmInstanceSetup As PageInstanceSetup
    Public FrmInstanceExport As PageInstanceExport
    Public FrmDownloadModify As PageDownloadModify

    '资源信息分页声明
    Public FrmDownloadResourceDetail As PageDownloadResourceDetail

    Private ReadOnly NativeHints As New MainHintRuntime(Of HintType)(New MainHintHost(Of HintType) With {
        .Queue = Function() HintWaiting, .Panel = Function() FrmMain.PanHint, .NewUuid = AddressOf GetUuid,
        .Animations = AddressOf ReadAnimations, .IsAnimationRunning = AddressOf AniIsRun,
        .Blue = HintType.Blue, .Green = HintType.Green})
    Private ReadOnly NativeMessages As New MainMessageRuntime(Of MyMsgBoxConverter, MyMsgBoxType, Validate)(New MainMessageHost(Of MyMsgBoxConverter, MyMsgBoxType) With {
        .Window = Function() FrmMain, .Panel = Function() FrmMain?.PanMsg,
        .DragStop = Sub() FrmMain.DragStop(), .IsUiThread = Function() RunInUi(),
        .Queue = Function() WaitingMyMsgBox, .Create = Function() New MyMsgBoxConverter,
        .Text = MyMsgBoxType.Text, .Select = MyMsgBoxType.Select, .Input = MyMsgBoxType.Input, .Login = MyMsgBoxType.Login,
        .CreateInput = Function(Value) New MyMsgInput(Value), .CreateSelect = Function(Value) New MyMsgSelect(Value),
        .CreateText = Function(Value) New MyMsgText(Value), .CreateLogin = Function(Value) New MyMsgLogin(Value)})
    Private ReadOnly NativeArguments As New MainArgumentRuntime(New MainArgumentHost With {
        .VersionName = Function() VersionBaseName, .VersionCode = Function() VersionCode,
        .BuildTypeDisplay = Function() BuildTypeDisplay, .BuildType = Function() BuildType.ToString(),
        .VersionBranch = Function() VersionBranchMain, .Identify = Function() Identify,
        .BasePath = Function() Paths.Base, .ExePath = Function() PathExe, .TempPath = Function() PathTemp,
        .RegistryFolder = Function() RegFolder, .JavaFolder = Function() McLaunchJavaSelected?.Folder,
        .MinecraftFolder = Function() McFolderSelected, .InstanceLoaded = Function() If(McInstanceSelected?.IsLoaded, False),
        .InstancePath = Function() McInstanceSelected.PathVersion, .InstanceIndie = Function() McInstanceSelected.PathIndie,
        .InstanceName = Function() McInstanceSelected.Name, .VanillaName = Function() McInstanceSelected.Version.VanillaName,
        .InstanceLoaders = Function() McInstanceSelected.Version.ModLoaders,
        .LoaderVersion = Function(Loader) McInstanceSelected.Version.ModLoaderVersions(Loader),
        .LoginFinished = Function() McLoginLoader.State = LoadState.Finished, .LoginName = Function() McLoginLoader.Output.Name,
        .LoginUuid = Function() McLoginLoader.Output.Uuid, .LoginType = Function() CInt(McLoginLoader.Input.Type),
        .LegacyLogin = CInt(McLoginType.Legacy), .MsLogin = CInt(McLoginType.Ms), .NideLogin = CInt(McLoginType.Nide), .AuthLogin = CInt(McLoginType.Auth),
        .RandomHint = AddressOf PageOtherTest.GetRandomHint, .RandomCave = AddressOf PageOtherTest.GetRandomCave,
        .Setting = Function(Name) Settings.GetSafe(Name, McInstanceSelected)})
    Private ReadOnly NativeHelpHost As MainHelpHost(Of CustomEvent.EventType) = CreateHelpHost()
    Private ReadOnly NativeHelp As New MainHelpRuntime(Of HelpEntry, CustomEvent.EventType)(NativeHelpHost, Function(Path) New HelpEntry(Path))
    Public HelpLoader As New LoaderTask(Of Integer, List(Of HelpEntry))("Help Page", AddressOf HelpLoad,, ThreadPriority.BelowNormal)
    Private ReadOnly NativeTimers As New MainTimerRuntime(New MainTimerHost With {
        .HintTick = AddressOf HintTick, .MessageTick = AddressOf MyMsgBoxTick, .DragTick = Sub() FrmMain.DragTick(),
        .TaskbarRefresh = AddressOf LoaderTaskbarProgressRefresh, .Ui = Sub(Work) RunInUi(Work), .UiWait = Sub(Work) RunInUiWait(Work),
        .StartThread = Sub(Work, Name) RunInNewThread(Work, Name), .Window = Function() FrmMain, .Hidden = Function() FrmMain.Hidden,
        .ShowAprilButton = Function() FrmMain.BtnExtraApril_ShowCheck, .AprilRibble = Sub() FrmMain.BtnExtraApril.Ribble(),
        .AprilYear = Function() Settings.Get(Of Integer)("AprilYear"), .AprilGiveUp = Function() IsAprilGiveup,
        .HasLaunchPage = Function() FrmLaunchLeft IsNot Nothing, .IsLaunchPage = Function() FrmMain.PageCurrent = FormMain.PageType.Launch,
        .AnimationControl = Function() AniControlEnabled, .LastMouse = Function() FrmMain.lastMouseArg,
        .LaunchButton = Function() FrmLaunchLeft.BtnLaunch, .MainPanel = Function() FrmMain.PanMain, .FormPanel = Function() FrmMain.PanForm,
        .AprilPosition = Function() FrmLaunchLeft.AprilPosTrans, .AprilScale = Function() FrmLaunchLeft.AprilScaleTrans,
        .RandomInteger = AddressOf RandomInteger, .RedHint = Sub(Text) Hint(Text, HintType.Red)})
    Private ReadOnly NativeTaskTemp As New MainTaskTempRuntime(New MainTaskTempHost With {
        .OsDrive = Function() OsDrive, .TempPath = Function() PathTemp, .NewUuid = AddressOf GetUuid,
        .RandomInteger = AddressOf RandomInteger, .CheckPermissionWithException = AddressOf CheckPermissionWithException, .CheckPermission = AddressOf CheckPermission})
    Private ReadOnly NativeFeedback As New MainFeedbackRuntime(New MainFeedbackHost With {
        .OsFullName = Function() My.Computer.Info.OSFullName, .AvailablePhysicalMemory = Function() My.Computer.Info.AvailablePhysicalMemory,
        .TotalPhysicalMemory = Function() My.Computer.Info.TotalPhysicalMemory, .Dpi = Function() DPI,
        .MinecraftFolder = Function() McFolderSelected, .BasePath = Function() Paths.Base, .IssuesUrl = Function() LauncherIssuesUrl,
        .Message = Function(Text, Title, First, Second) MyMsgBox(Text, Title, First, Second),
        .Explorer = Sub(Address) OpenExplorer(Address), .Website = Sub(Address) OpenWebsite(Address)})

    Private Function ReadAnimations() As ControlAnimations
        PresentationRuntime.EnsureConfigured()
        Return ControlServices.Current.Animations
    End Function
    Private Function CreateHelpHost() As MainHelpHost(Of CustomEvent.EventType)
        Return New MainHelpHost(Of CustomEvent.EventType) With {
            .ArgumentReplace = Function(Text, Escape) ArgumentReplace(Text, Escape),
            .BasePath = Function() Paths.Base, .TempPath = Function() PathTemp, .ImagePath = Function() PathImage,
            .VersionCode = Function() VersionCode, .HelpVersion = Function() Settings.Get(Of Integer)("SystemHelpVersion"),
            .SetHelpVersion = Sub(Value) Settings.Set("SystemHelpVersion", Value), .ExtractResources = Sub(Path, Name) ExtractResources(Path, Name),
            .PopupEvent = CustomEvent.EventType.弹出窗口, .CreateRow = Function() New MyListItem,
            .ClearCustomEvent = Sub(Row)
                                    CustomEventService.SetEventType(Row, CustomEvent.EventType.None)
                                    CustomEventService.SetEventData(Row, Nothing)
                                End Sub,
            .ConnectClick = Sub(Row, Handler)
                                AddHandler DirectCast(Row, MyListItem).Click, Sub(Sender, Args) Handler(Sender)
                            End Sub,
            .OpenEntry = Sub(Entry) PageOtherHelp.OnItemClick(DirectCast(Entry, HelpEntry))}
    End Function
    Public Class HelpEntry
        Inherits MainHelpEntryData(Of CustomEvent.EventType)
        Public Sub New(FilePath As String)
            MyBase.New(FilePath, NativeHelpHost)
        End Sub
        Public Shadows Function ToListItem() As MyListItem
            Return DirectCast(MyBase.ToListItem(), MyListItem)
        End Function
        Public Overloads Function SetToListItem(Item As MyListItem) As MyListItem
            Return DirectCast(MyBase.SetToListItem(Item), MyListItem)
        End Function
    End Class
    Private Sub HelpLoad(Loader As LoaderTask(Of Integer, List(Of HelpEntry)))
        NativeHelp.Load(Function() Loader.IsCanceled, Sub(Entries) Loader.Output = Entries)
    End Sub
    Public Sub HelpTryExtract()
        NativeHelp.TryExtract()
    End Sub
    Public Sub Hint(Text As String, Optional Type As HintType = HintType.Blue, Optional Log As Boolean = True)
        NativeHints.Enqueue(Text, Type, Log)
    End Sub
    Private Sub HintTick()
        NativeHints.Tick()
    End Sub
    Private Sub HideAllHint()
        NativeHints.HideAll()
    End Sub
    Public Function MyMsgBox(Caption As String, Optional Title As String = "提示", Optional Button1 As String = "确定", Optional Button2 As String = "", Optional Button3 As String = "",
        Optional IsWarn As Boolean = False, Optional HighLight As Boolean = True, Optional ForceWait As Boolean = False,
        Optional Button1Action As Action = Nothing, Optional Button2Action As Action = Nothing, Optional Button3Action As Action = Nothing) As Integer
        Return NativeMessages.Show(Caption, Title, Button1, Button2, Button3, IsWarn, HighLight, ForceWait, Button1Action, Button2Action, Button3Action)
    End Function
    Public Function MyMsgBoxInput(Title As String, Optional Text As String = Nothing, Optional DefaultInput As String = Nothing, Optional ValidateRules As Collection(Of Validate) = Nothing,
        Optional HintText As String = "", Optional Button1 As String = "确定", Optional Button2 As String = "取消", Optional IsWarn As Boolean = False) As String
        Return NativeMessages.Input(Title, Text, DefaultInput, ValidateRules, HintText, Button1, Button2, IsWarn)
    End Function
    Public Function MyMsgBoxSelect(Selections As IEnumerable(Of IMyRadio), Optional Title As String = "提示", Optional Button1 As String = "确定", Optional Button2 As String = "", Optional IsWarn As Boolean = False) As Integer?
        Return NativeMessages.Select(Selections, Title, Button1, Button2, IsWarn)
    End Function
    Public Sub MyMsgBoxTick()
        NativeMessages.Tick()
    End Sub
    Friend Sub MyMsgBoxContent(Caption As String, Title As String, Factory As Func(Of FrameworkElement))
        NativeMessages.Content(Caption, Title, Factory)
    End Sub
    Friend Function MyMsgBoxConfirmContent(Caption As String, Title As String, ConfirmText As String, Factory As Func(Of FrameworkElement),
        Optional CancelText As String = "取消", Optional ThirdText As String = "", Optional WarningButton As Integer = 1, Optional IsWarn As Boolean = True) As Integer
        Return NativeMessages.ConfirmContent(Caption, Title, ConfirmText, Factory, CancelText, ThirdText, WarningButton, IsWarn)
    End Function
    Public ReadOnly Property IsAprilEnabled As Boolean
        Get
            Return NativeTimers.IsAprilEnabled
        End Get
    End Property
    Private Sub TimerFool()
        NativeTimers.FoolTick()
    End Sub
    Private Sub TimerMain()
        NativeTimers.Tick()
    End Sub
    Public Sub TimerMainStart()
        NativeTimers.Start()
    End Sub
    Public Sub ShowWindowToTop(Handle As IntPtr)
        MainWindowInterop.ShowToTop(Handle)
    End Sub
    Public Function FindWindow(<System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.VBByRefStr)> ByRef ClassName As String,
        <System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.VBByRefStr)> ByRef WindowName As String) As IntPtr
        Return MainWindowInterop.FindWindow(ClassName, WindowName)
    End Function
    Public Function SetForegroundWindow(hWnd As IntPtr) As Integer
        Return MainWindowInterop.SetForegroundWindow(hWnd)
    End Function
    Public Sub SetGPUPreference(Executeable As String)
        MainWindowInterop.SetGpuPreference(Executeable)
    End Sub
    Public Function ArgumentReplace(Text As String, Optional EscapeHandler As Func(Of String, String) = Nothing, Optional ReplaceTime As Boolean = True) As String
        Return NativeArguments.Replace(Text, EscapeHandler, ReplaceTime)
    End Function
    Public Sub TryClearTaskTemp()
        NativeTaskTemp.TryClear()
    End Sub
    Public Function RequestTaskTempFolder(Optional RequireNonSpace As Boolean = False) As String
        Return NativeTaskTemp.Request(RequireNonSpace)
    End Function
    Public Sub Feedback(Optional ShowMsgbox As Boolean = True, Optional ForceOpenLog As Boolean = False)
        NativeFeedback.Feedback(ShowMsgbox, ForceOpenLog)
    End Sub
    Public Sub FeedbackInfo()
        NativeFeedback.Info()
    End Sub
End Module
