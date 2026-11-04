Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Net.Http
Imports System.Text
Imports System.Threading
Imports Newtonsoft.Json.Linq
Imports LaunchCore = PCLCS.LegacyLaunchEngine(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.ModLaunch.McLaunchOptions, PCL.ModLaunch.McLoginData, PCL.ModLaunch.McLoginServer, PCL.ModLaunch.McLoginMs, PCL.ModLaunch.McLoginLegacy, PCL.ModWatcher.Watcher)
Imports LaunchHost = PCLCS.LegacyLaunchHost(Of PCL.ModMinecraft.McInstance, PCL.ModLaunch.McLaunchOptions, PCL.ModLaunch.McLoginData, PCL.ModLaunch.McLoginServer, PCL.ModLaunch.McLoginMs, PCL.ModLaunch.McLoginLegacy, PCL.ModWatcher.Watcher)

'LF 原字段、嵌套类型及平台调用身份；完整启动／登录／参数／进程业务由 C# 实现维护。
Public Module ModLaunch
    Public CurrentLaunchOptions As McLaunchOptions = Nothing
    Public McLaunchLoader As New LoaderTask(Of McLaunchOptions, Object)("Loader Launch", AddressOf McLaunchStart) With {.OnStateChanged = AddressOf McLaunchState}
    Public McLaunchLoaderReal As LoaderCombo(Of Object)
    Public McLaunchProcess As Process
    Public McLaunchWatcher As Watcher
    Public McLaunchJavaSelected As Java = Nothing
    Private CanceledHint As String = Nothing
    Private McLaunchArgument As String
    Private McLaunchEncoding As Encoding
    Public McLoginLoader As New LoaderTask(Of McLoginData, McLoginResult)("登录", AddressOf McLoginStart, AddressOf McLoginInput, ThreadPriority.BelowNormal) With {.ReloadTimeout = 1, .ProgressWeight = 15, .Block = False}
    Public McLoginMsLoader As New LoaderTask(Of McLoginMs, McLoginResult)("Loader Login Ms", AddressOf McLoginMsStart) With {.ReloadTimeout = 1}
    Public McLoginLegacyLoader As New LoaderTask(Of McLoginLegacy, McLoginResult)("Loader Login Legacy", AddressOf McLoginLegacyStart)
    Public McLoginNideLoader As New LoaderTask(Of McLoginServer, McLoginResult)("Loader Login Nide", AddressOf McLoginServerStart) With {.ReloadTimeout = 1000 * 60 * 10}
    Public McLoginAuthLoader As New LoaderTask(Of McLoginServer, McLoginResult)("Loader Login Auth", AddressOf McLoginServerStart) With {.ReloadTimeout = 1000 * 60 * 10}
    Private ReadOnly Service As New LaunchCore(CreateLaunchHost())

    Public Enum McLoginType
        Legacy = 0
        Nide = 2
        Auth = 3
        Ms = 5
    End Enum
    Public Structure McLoginResult
        Public Name As String
        Public Uuid As String
        Public AccessToken As String
        Public Type As String
        Public ClientToken As String
        Public ProfileJson As String
    End Structure
    Public Class McLaunchOptions
        Implements PCLCS.ILegacyLaunchOptions(Of McInstance)
        ''' <summary>
        ''' 强制指定在启动后进入的服务器 IP。
        ''' 默认值：Nothing。使用版本设置的值。
        ''' </summary>
        Public ServerIp As String = Nothing
        ''' <summary>
        ''' 将启动脚本保存到该地址，然后取消启动。这同时会改变启动时的提示等。
        ''' 默认值：Nothing。不保存。
        ''' </summary>
        Public SaveBatch As String = Nothing
        ''' <summary>
        ''' 强行指定启动的 MC 版本。
        ''' 默认值：Nothing。使用 McInstanceCurrent。
        ''' </summary>
        Public Instance As McInstance = Nothing
        ''' <summary>
        ''' 额外的启动参数。
        ''' </summary>
        Public ExtraGameArgs As New List(Of String)
        Public QuickPlayWorld As String = Nothing
        Friend PersonalOptions As PersonalOptionsSession
        Friend QuickPlay As QuickPlaySession
        Friend EnvironmentPoint As String

        Private Property BridgeServerIp As String Implements PCLCS.ILegacyLaunchOptions(Of McInstance).ServerIp
            Get
                Return ServerIp
            End Get
            Set(Value As String)
                ServerIp = Value
            End Set
        End Property
        Private Property BridgeSaveBatch As String Implements PCLCS.ILegacyLaunchOptions(Of McInstance).SaveBatch
            Get
                Return SaveBatch
            End Get
            Set(Value As String)
                SaveBatch = Value
            End Set
        End Property
        Private Property BridgeInstance As McInstance Implements PCLCS.ILegacyLaunchOptions(Of McInstance).Instance
            Get
                Return Instance
            End Get
            Set(Value As McInstance)
                Instance = Value
            End Set
        End Property
        Private ReadOnly Property BridgeExtraGameArgs As List(Of String) Implements PCLCS.ILegacyLaunchOptions(Of McInstance).ExtraGameArgs
            Get
                Return ExtraGameArgs
            End Get
        End Property
        Private Property BridgeQuickPlayWorld As String Implements PCLCS.ILegacyLaunchOptions(Of McInstance).QuickPlayWorld
            Get
                Return QuickPlayWorld
            End Get
            Set(Value As String)
                QuickPlayWorld = Value
            End Set
        End Property
        Private Property BridgeEnvironmentPoint As String Implements PCLCS.ILegacyLaunchOptions(Of McInstance).EnvironmentPoint
            Get
                Return EnvironmentPoint
            End Get
            Set(Value As String)
                EnvironmentPoint = Value
            End Set
        End Property
        Private Property BridgePersonalOptions As PCLCS.PersonalOptionsSessionData Implements PCLCS.ILegacyLaunchOptions(Of McInstance).PersonalOptions
            Get
                Return PersonalOptions
            End Get
            Set(Value As PCLCS.PersonalOptionsSessionData)
                PersonalOptions = DirectCast(Value, PersonalOptionsSession)
            End Set
        End Property
        Private Property BridgeQuickPlay As PCLCS.QuickPlaySessionData Implements PCLCS.ILegacyLaunchOptions(Of McInstance).QuickPlay
            Get
                Return QuickPlay
            End Get
            Set(Value As PCLCS.QuickPlaySessionData)
                QuickPlay = DirectCast(Value, QuickPlaySession)
            End Set
        End Property
    End Class

    Public MustInherit Class McLoginData
        Implements PCLCS.ILegacyLaunchLoginData
        ''' <summary>
        ''' 登录方式。
        ''' </summary>
        Public Type As McLoginType
        Public Overrides Function Equals(obj As Object) As Boolean
            Return PCLCS.LegacyLaunchIdentity.LoginEquals(Me, obj)
        End Function
        Private ReadOnly Property BridgeTypeCode As Integer Implements PCLCS.ILegacyLaunchLoginData.TypeCode
            Get
                Return CInt(Type)
            End Get
        End Property
    End Class

    Public Class McLoginServer
        Inherits McLoginData
        Implements PCLCS.ILegacyLaunchServerData

        ''' <summary>
        ''' 登录用户名。
        ''' </summary>
        Public UserName As String
        ''' <summary>
        ''' 登录密码。
        ''' </summary>
        Public Password As String
        ''' <summary>
        ''' 登录服务器基础地址。
        ''' </summary>
        Public BaseUrl As String
        ''' <summary>
        ''' 登录所使用的标识符，目前只可能为 “Auth” 或 “Nide”，用于存储缓存等。
        ''' </summary>
        Public Token As String
        ''' <summary>
        ''' 登录方式的描述字符串，如 “正版”、“统一通行证”。
        ''' </summary>
        Public Description As String
        ''' <summary>
        ''' 是否在本次登录中强制要求玩家重新选择角色，目前仅对 Authlib-Injector 生效。
        ''' </summary>
        Public ForceReselectProfile As Boolean = False

        Public Sub New(Type As McLoginType)
            Me.Type = Type
        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return PCLCS.LegacyLaunchIdentity.ServerHash(UserName, Password, BaseUrl, Token, CInt(Type))
        End Function


        Private ReadOnly Property BridgeUserName As String Implements PCLCS.ILegacyLaunchServerData.UserName
            Get
                Return UserName
            End Get
        End Property
        Private ReadOnly Property BridgePassword As String Implements PCLCS.ILegacyLaunchServerData.Password
            Get
                Return Password
            End Get
        End Property
        Private ReadOnly Property BridgeBaseUrl As String Implements PCLCS.ILegacyLaunchServerData.BaseUrl
            Get
                Return BaseUrl
            End Get
        End Property
        Private ReadOnly Property BridgeToken As String Implements PCLCS.ILegacyLaunchServerData.Token
            Get
                Return Token
            End Get
        End Property
        Private ReadOnly Property BridgeDescription As String Implements PCLCS.ILegacyLaunchServerData.Description
            Get
                Return Description
            End Get
        End Property
        Private ReadOnly Property BridgeForceReselectProfile As Boolean Implements PCLCS.ILegacyLaunchServerData.ForceReselectProfile
            Get
                Return ForceReselectProfile
            End Get
        End Property
    End Class

    Public Class McLoginMs
        Inherits McLoginData
        Implements PCLCS.ILegacyLaunchMicrosoftData

        ''' <summary>
        ''' 缓存的 OAuth Refresh Token。若没有则为空字符串。
        ''' </summary>
        Public OAuthRefreshToken As String = ""
        Public AccessToken As String = ""
        Public Uuid As String = ""
        Public UserName As String = ""
        Public ProfileJson As String = ""

        Public Sub New()
            Type = McLoginType.Ms
        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return PCLCS.LegacyLaunchIdentity.MicrosoftHash(OAuthRefreshToken, UserName)
        End Function

        Private ReadOnly Property BridgeOAuthRefreshToken As String Implements PCLCS.ILegacyLaunchMicrosoftData.OAuthRefreshToken
            Get
                Return OAuthRefreshToken
            End Get
        End Property
        Private ReadOnly Property BridgeAccessToken As String Implements PCLCS.ILegacyLaunchMicrosoftData.AccessToken
            Get
                Return AccessToken
            End Get
        End Property
        Private ReadOnly Property BridgeUuid As String Implements PCLCS.ILegacyLaunchMicrosoftData.Uuid
            Get
                Return Uuid
            End Get
        End Property
        Private ReadOnly Property BridgeUserName As String Implements PCLCS.ILegacyLaunchMicrosoftData.UserName
            Get
                Return UserName
            End Get
        End Property
        Private ReadOnly Property BridgeProfileJson As String Implements PCLCS.ILegacyLaunchMicrosoftData.ProfileJson
            Get
                Return ProfileJson
            End Get
        End Property
    End Class

    Public Class McLoginLegacy
        Inherits McLoginData
        Implements PCLCS.ILegacyLaunchOfflineData
        ''' <summary>
        ''' 登录用户名。
        ''' </summary>
        Public UserName As String
        ''' <summary>
        ''' 皮肤种类。
        ''' </summary>
        Public SkinType As Integer
        ''' <summary>
        ''' 若采用正版皮肤，则为该皮肤名。
        ''' </summary>
        Public SkinName As String

        Public Sub New()
            Type = McLoginType.Legacy
        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return PCLCS.LegacyLaunchIdentity.OfflineHash(UserName, SkinType, SkinName, CInt(Type))
        End Function

        Private ReadOnly Property BridgeUserName As String Implements PCLCS.ILegacyLaunchOfflineData.UserName
            Get
                Return UserName
            End Get
        End Property
        Private ReadOnly Property BridgeSkinType As Integer Implements PCLCS.ILegacyLaunchOfflineData.SkinType
            Get
                Return SkinType
            End Get
        End Property
        Private ReadOnly Property BridgeSkinName As String Implements PCLCS.ILegacyLaunchOfflineData.SkinName
            Get
                Return SkinName
            End Get
        End Property
    End Class

    Private Function CreateLaunchHost() As LaunchHost
        Return New LaunchHost With {
            .Options = Function() CurrentLaunchOptions, .StoreOptions = Sub(Value) CurrentLaunchOptions = Value,
            .SelectedInstance = Function() McInstanceSelected, .StoreSelectedInstance = Sub(Value) McInstanceSelected = Value,
            .CreateInstance = Function(Name) New McInstance(Name), .SelectedFolder = Function() McFolderSelected,
            .SelectedJava = Function() McLaunchJavaSelected, .StoreSelectedJava = Sub(Value) McLaunchJavaSelected = Value,
            .Arguments = Function() McLaunchArgument, .StoreArguments = Sub(Value) McLaunchArgument = Value,
            .CanceledHint = Function() CanceledHint, .StoreCanceledHint = Sub(Value) CanceledHint = Value,
            .Encoding = Function() McLaunchEncoding, .StoreEncoding = Sub(Value) McLaunchEncoding = Value,
            .StoreProcess = Sub(Value) McLaunchProcess = Value, .StoreWatcher = Sub(Value) McLaunchWatcher = Value,
            .ReadString = Function(Key, Instance) Settings.Get(Of String)(Key, Instance:=Instance),
            .ReadInteger = Function(Key, Instance) If(Key = "LoginType", CInt(Settings.Get(Of McLoginType)(Key)), Settings.Get(Of Integer)(Key, Instance:=Instance)),
            .ReadBoolean = Function(Key, Instance) Settings.Get(Of Boolean)(Key, Instance:=Instance),
            .WriteSetting = Sub(Key, Value) Settings.Set(Key, Value),
            .ReadIni = Function(File, Key, DefaultValue) ReadIni(File, Key, DefaultValue),
            .WriteIni = Sub(File, Key, Value) WriteIni(File, Key, Value), .ClearIniCache = AddressOf IniClearCache,
            .AccountSnapshot = Function(Prefix) Settings.AccountSnapshot(Prefix),
            .SaveMicrosoftSession = Sub(Values, PreviousName, Canceled) Settings.SaveMicrosoftSession(Values, PreviousName, Canceled),
            .SaveAccountGroup = Sub(Values, Canceled) Settings.SaveAccountGroup(Values, Canceled),
            .UpdateServerAccounts = Sub(Token, Name, Password, Remove) Settings.UpdateServerAccounts(Token, Name, Password, Remove),
            .UpdateLegacyNames = Sub(Change) Settings.UpdateLegacyNames(Change),
            .OnUiThread = Function() RunInUi(), .Debug = Function() ModeDebug,
            .ReleaseBuild = Function() BuildType = BuildTypes.Release, .IsGbkEncoding = Function() IsGBKEncoding,
            .PackEnabled = Function() ModModpackMode.Enabled, .CleaningFiles = Function() PageOtherTest.IsCleaningGameFiles,
            .HasRunningMinecraft = Function() ModWatcher.HasRunningMinecraft, .PackAllowsInstance = Function(Folder) ModModpackMode.AllowsInstance(Folder),
            .WorldBackupsBusy = Function() WorldBackups.IsBusy, .WorldBackupsUiBusy = Function() WorldBackupsUi.IsBusy,
            .ClonerUiBusy = Function() InstanceClonerUi.IsBusy, .HistoryUiBusy = Function() EnvironmentHistoryUi.IsBusy,
            .ModpackUpdating = Function() ModpackUpdateFlow.IsUpdating, .InstanceLocked = Function(Instance) McInstanceLock.IsLocked(Instance),
            .PendingChanges = Function(Folder) ModpackUpdate.Pending(Folder).Count, .AcquireInstance = Function(Worker, Folder) McInstanceLock.TryAcquire(Worker, Folder),
            .Dispatch = Sub(Work) RunInUi(Work), .DispatchWait = Sub(Work) RunInUiWait(Work), .NewThread = Sub(Work, Name) RunInNewThread(Work, Name),
            .RefreshButtons = Sub() FrmLaunchLeft.RefreshButtonsUI(), .RefreshPageAfterSwitch = Sub() FrmLaunchLeft.RefreshPage(False, False),
            .AprilGiveup = Sub() FrmMain.AprilGiveup(), .RemoveInstanceSelectionPage = AddressOf RemoveInstanceSelectionPage,
            .LoginPage = Sub() FrmLaunchLeft.PageChangeToLogin(), .ClearLaunchLog = Sub() FrmLaunchRight.LabLog.Text = "",
            .LaunchingPage = Sub() FrmLaunchLeft.PageChangeToLaunching(), .RefreshLaunching = Sub() FrmLaunchLeft.Dispatcher.Invoke(AddressOf FrmLaunchLeft.LaunchingRefresh),
            .RefreshRecentVisibility = Sub() FrmLaunchRight?.UpdateRecentWorldVisibility(), .RefreshLoginPage = Sub() FrmLaunchLeft.RefreshPage(True, False),
            .RefreshRecentWorlds = Sub() FrmLaunchRight?.RefreshRecentWorlds(), .RefreshKeybindings = AddressOf RefreshVisibleKeybindings,
            .AppendLog = Sub(Text) FrmLaunchRight.LabLog.AppendText(Text), .Hint = AddressOf PublishLaunchHint, .Message = AddressOf ShowLaunchMessage,
            .OpenWebsite = AddressOf OpenWebsite, .OpenExplorer = Sub(Address) OpenExplorer(Address),
            .CurrentLoginValidation = AddressOf CurrentLoginValidation, .LoginValidation = AddressOf ValidateLoginData,
            .LoginInput = AddressOf ReadLoginInput, .ExitServerLogin = AddressOf ExitServerLogin, .SelectProfile = AddressOf SelectLoginProfile,
            .DeviceLoginPrompt = AddressOf CreateDevicePrompt, .Request = AddressOf SendLaunchRequest,
            .OAuthClientId = Function() OAuthClientId, .TempFolder = Function() PathTemp, .PureFolder = Function() PathPure.Value,
            .ImagePath = Function() PathImage, .ExecutablePath = Function() PathExe, .OsDrive = Function() OsDrive,
            .VersionDisplay = Function() VersionDisplay, .VersionCode = Function() CStr(VersionCode),
            .Dpi = Function() DPI, .LauncherSize = AddressOf LauncherPixelSize, .AvailablePhysicalMemory = Function() My.Computer.Info.AvailablePhysicalMemory,
            .RamGb = Function(Instance) PageInstanceSetup.GetRam(Instance), .IsAdmin = Function() WindowsUtils.HasAdminRole(),
            .SetGpuPreference = AddressOf SetGPUPreference, .RunAdminTaskDone = Function(Arguments) RunAsAdmin(Arguments) = ProcessReturnValues.TaskDone,
            .StartProcess = Function(Info) StartProcess(Info), .LoadImage = AddressOf LoadLaunchImage,
            .ArgumentReplace = Function(Text, ReplaceTime) ArgumentReplace(Text, ReplaceTime:=ReplaceTime),
            .FilterAccessToken = AddressOf FilterAccessToken, .FilterUserName = AddressOf FilterUserName,
            .Libraries = Function(Instance, MainJar) McLibListGet(Instance, MainJar), .JsonRuleCheck = AddressOf McJsonRuleCheck,
            .AssetsIndexName = AddressOf McAssetsGetIndexName, .CreateLauncherProfiles = AddressOf McFolderLauncherProfilesJsonCreate,
            .SelectJava = Function(Instance, Fix, Token, Progress) SelectOrDownloadJava(Instance, Fix, Token, Progress),
            .JavaRequirement = Function(Instance) GetJavaRequirement(Instance).Range,
            .LaunchLoader = Function() TaskPort(McLaunchLoader), .LoginLoader = Function() LoginPort(McLoginLoader),
            .MicrosoftLoader = Function() LoginPort(McLoginMsLoader), .OfflineLoader = Function() LoginPort(McLoginLegacyLoader),
            .ServerLoader = Function(Kind) LoginPort(If(Kind = CInt(McLoginType.Nide), McLoginNideLoader, McLoginAuthLoader)),
            .ClientFix = AddressOf ClientFixStep, .CreateCombo = AddressOf CreateLaunchCombo,
            .StoreLaunchOperation = Sub(Operation) McLaunchLoaderReal = DirectCast(Operation.Source, LoaderCombo(Of Object)),
            .AddTaskbar = Sub(Operation) LoaderTaskbarAdd(DirectCast(Operation.Source, LoaderCombo(Of Object))),
            .DependencyContext = AddressOf DependencyContext, .ConfirmDependencies = AddressOf ConfirmDependencies, .OpenMods = AddressOf OpenDependencyMods,
            .MemoryOptimize = Sub() PageOtherTest.MemoryOptimize(False), .RandomInteger = AddressOf RandomInteger,
            .RecoverWorlds = Sub(Folder, Token) WorldBackups.RecoverBeforeLaunch(Folder, Token),
            .PreparePersonal = Function(Instance) PersonalGameSettings.Prepare(Instance),
            .TrackPersonal = Sub(Session, Game) PersonalGameSettings.Track(DirectCast(Session, PersonalOptionsSession), Game),
            .FinishPersonal = Sub(Session) PersonalGameSettings.Finish(DirectCast(Session, PersonalOptionsSession)),
            .PrepareQuickPlay = Function(Instance, Identifier, Arguments, SaveBatch) QuickPlayWorlds.Prepare(Instance, Identifier, Arguments, SaveBatch),
            .TrackQuickPlay = Sub(Session, Game) QuickPlayWorlds.Track(DirectCast(Session, QuickPlaySession), Game),
            .FinishQuickPlay = Sub(Session) QuickPlayWorlds.Finish(DirectCast(Session, QuickPlaySession)),
            .TrackWorlds = Sub(Instance, Game) WorldBackups.Track(Instance, Game),
            .CaptureEnvironment = Function(Instance, Reason, Token) EnvironmentHistory.CaptureInstance(Instance, Reason, Token),
            .CreateWatcher = Function(Loader, Instance, Title, BeforeMonitor, Stopped, Point) New Watcher(DirectCast(Loader.Source, LoaderTask(Of Process, Integer)), Instance, Title, BeforeMonitor, Stopped, Point),
            .WatcherState = Function(Value) CInt(Value.State), .MusicPause = Function() MusicPause(), .MusicResume = Function() MusicResume(),
            .EndProgram = Sub() FrmMain.EndProgram(False), .HideLauncher = Sub() FrmMain.Hidden = True,
            .MinimizeLauncher = Sub() FrmMain.WindowState = WindowState.Minimized
        }
    End Function

    Private Sub FillPort(Port As PCLCS.LegacyLaunchTaskPort, Raw As LoaderBase, Optional ForceRestarting As Func(Of Boolean) = Nothing, Optional Canceled As Func(Of Boolean) = Nothing, Optional CanceledWithThread As Func(Of Thread, Boolean) = Nothing)
        Port.Source = Raw
        Port.ReadState = Function() CType(CInt(Raw.State), PCLCS.LegacyLaunchState)
        Port.WriteState = Sub(Value) Raw.State = CType(CInt(Value), LoadState)
        Port.ReadError = Function() Raw.Error
        Port.ReadCanceled = If(Canceled, Function() Raw.State = LoadState.Canceled)
        Port.ReadForceRestarting = If(ForceRestarting, Function() False)
        Port.CanceledWithThread = If(CanceledWithThread, Function(Worker) Raw.State = LoadState.Canceled)
        Port.ReadProgress = Function() Raw.Progress
        Port.WriteProgress = Sub(Value) Raw.Progress = Value
        Port.CreateCancellationToken = Function() Raw.CreateCancellationToken()
        Port.CreateSyncProgressProvider = Function() Raw.CreateSyncProgressProvider()
        Port.Cancel = Sub() Raw.Cancel()
        Port.CancelParent = Sub() Raw.Parent.Cancel()
    End Sub
    Private Function TaskPort(Of TInput, TOutput)(Raw As LoaderTask(Of TInput, TOutput)) As PCLCS.LegacyLaunchTask(Of TInput, TOutput)
        Dim Port As New PCLCS.LegacyLaunchTask(Of TInput, TOutput) With {
            .ReadInput = Function() Raw.Input, .ReadOutput = Function() Raw.Output, .WriteOutput = Sub(Value) Raw.Output = Value,
            .StartWithInput = Sub(Input, Force) Raw.Start(Input, Force),
            .Wait = Sub(Input, ProgressSource, Force, Token, Progress) Raw.WaitForExit(Input, If(ProgressSource Is Nothing, Nothing, DirectCast(ProgressSource.Source, LoaderBase)), Force, Token, Progress)
        }
        FillPort(Port, Raw, Function() Raw.IsForceRestarting, Function() Raw.IsCanceled, Function(Worker) Raw.IsCanceledWithThread(Worker))
        Return Port
    End Function
    Private Function LoginPort(Of TInput)(Raw As LoaderTask(Of TInput, McLoginResult)) As PCLCS.LegacyLaunchTask(Of TInput, PCLCS.ILegacyLaunchLoginResult)
        Dim Result As New PCLCS.LegacyLaunchLoginResultPort With {
            .ReadName = Function() Raw.Output.Name, .WriteName = Sub(Value) Raw.Output.Name = Value,
            .ReadUuid = Function() Raw.Output.Uuid, .WriteUuid = Sub(Value) Raw.Output.Uuid = Value,
            .ReadAccessToken = Function() Raw.Output.AccessToken, .WriteAccessToken = Sub(Value) Raw.Output.AccessToken = Value,
            .ReadType = Function() Raw.Output.Type, .WriteType = Sub(Value) Raw.Output.Type = Value,
            .ReadClientToken = Function() Raw.Output.ClientToken, .WriteClientToken = Sub(Value) Raw.Output.ClientToken = Value,
            .ReadProfileJson = Function() Raw.Output.ProfileJson, .WriteProfileJson = Sub(Value) Raw.Output.ProfileJson = Value
        }
        Dim Port As New PCLCS.LegacyLaunchTask(Of TInput, PCLCS.ILegacyLaunchLoginResult) With {
            .ReadInput = Function() Raw.Input, .ReadOutput = Function() Result,
            .WriteOutput = Sub(Value) Raw.Output = New McLoginResult With {
                .Name = Value.Name, .Uuid = Value.Uuid, .AccessToken = Value.AccessToken,
                .Type = Value.Type, .ClientToken = Value.ClientToken, .ProfileJson = Value.ProfileJson},
            .StartWithInput = Sub(Input, Force) Raw.Start(Input, Force),
            .Wait = Sub(Input, ProgressSource, Force, Token, Progress) Raw.WaitForExit(Input, If(ProgressSource Is Nothing, Nothing, DirectCast(ProgressSource.Source, LoaderBase)), Force, Token, Progress)
        }
        FillPort(Port, Raw, Function() Raw.IsForceRestarting, Function() Raw.IsCanceled, Function(Worker) Raw.IsCanceledWithThread(Worker))
        Return Port
    End Function
    Private Function OperationPort(Of TInput)(Raw As LoaderCombo(Of TInput)) As PCLCS.LegacyLaunchOperation
        Dim Port As New PCLCS.LegacyLaunchOperation With {.Start = Sub() Raw.Start()}
        FillPort(Port, Raw)
        Return Port
    End Function
    Private Function ClientFixStep(Instance As McInstance) As PCLCS.LegacyLaunchStep
        Return New PCLCS.LegacyLaunchExistingStep With {
            .Name = "补全文件", .Task = OperationPort(New LoaderCombo(Of String)("补全文件", DlClientFix(Instance, False, True))), .PreserveMetadata = False
        }
    End Function
    Private Function CreateLaunchCombo(Name As String, Steps As IList(Of PCLCS.LegacyLaunchStep)) As PCLCS.LegacyLaunchOperation
        Dim Children As New List(Of LoaderBase)
        For Each [Step] In Steps
            Dim Raw As LoaderBase
            If TypeOf [Step] Is PCLCS.LegacyLaunchExistingStep Then
                Raw = DirectCast(DirectCast([Step], PCLCS.LegacyLaunchExistingStep).Task.Source, LoaderBase)
                If DirectCast([Step], PCLCS.LegacyLaunchExistingStep).PreserveMetadata Then
                    Children.Add(Raw)
                    Continue For
                End If
            ElseIf TypeOf [Step] Is PCLCS.LegacyLaunchIntStep Then
                Dim Work = DirectCast([Step], PCLCS.LegacyLaunchIntStep).Work
                Raw = New LoaderTask(Of Integer, Integer)([Step].Name, Sub(Task) Work(TaskPort(Task))) With {.InterruptOnCancel = [Step].InterruptOnCancel}
            ElseIf TypeOf [Step] Is PCLCS.LegacyLaunchStringStep Then
                Dim Work = DirectCast([Step], PCLCS.LegacyLaunchStringStep).Work
                Raw = New LoaderTask(Of String, Integer)([Step].Name, Sub(Task) Work(TaskPort(Task))) With {.InterruptOnCancel = [Step].InterruptOnCancel}
            ElseIf TypeOf [Step] Is PCLCS.LegacyLaunchProcessStep Then
                Dim Work = DirectCast([Step], PCLCS.LegacyLaunchProcessStep).Work
                Raw = New LoaderTask(Of Integer, Process)([Step].Name, Sub(Task) Work(TaskPort(Task))) With {.InterruptOnCancel = [Step].InterruptOnCancel}
            ElseIf TypeOf [Step] Is PCLCS.LegacyLaunchWaitStep Then
                Dim Work = DirectCast([Step], PCLCS.LegacyLaunchWaitStep).Work
                Raw = New LoaderTask(Of Process, Integer)([Step].Name, Sub(Task) Work(TaskPort(Task))) With {.InterruptOnCancel = [Step].InterruptOnCancel}
            Else
                Throw New InvalidOperationException("未知启动步骤类型：" & [Step].GetType().FullName)
            End If
            Raw.ProgressWeight = [Step].ProgressWeight
            Raw.Block = [Step].Block
            Raw.Show = [Step].Show
            Children.Add(Raw)
        Next
        Return OperationPort(New LoaderCombo(Of Object)(Name, Children) With {.Show = False})
    End Function

    Private Sub RemoveInstanceSelectionPage()
        FrmMain.PageStack = FrmMain.PageStack.Where(Function(Page) Page.Page <> FormMain.PageType.InstanceSelect).ToList()
    End Sub
    Private Sub RefreshVisibleKeybindings()
        If TypeOf FrmMain?.PageRight Is PageInstanceKeybindings Then DirectCast(FrmMain.PageRight, PageInstanceKeybindings).RefreshBindings()
    End Sub
    Private Sub PublishLaunchHint(Text As String, Kind As PCLCS.LegacyLaunchHint)
        Select Case Kind
            Case PCLCS.LegacyLaunchHint.Red : Hint(Text, HintType.Red)
            Case PCLCS.LegacyLaunchHint.Green : Hint(Text, HintType.Green)
            Case PCLCS.LegacyLaunchHint.Blue : Hint(Text, HintType.Blue)
            Case Else : Hint(Text)
        End Select
    End Sub
    Private Function ShowLaunchMessage(Message As PCLCS.LegacyLaunchMessage) As Integer
        Return MyMsgBox(Message.Text, Message.Title, Message.Button1, Message.Button2, Message.Button3,
            IsWarn:=Message.IsWarn, HighLight:=Message.HighLight, ForceWait:=Message.ForceWait,
            Button1Action:=Message.Button1Action, Button2Action:=Message.Button2Action, Button3Action:=Message.Button3Action)
    End Function
    Private Function CurrentLoginValidation(Kind As Integer) As String
        Select Case CType(Kind, McLoginType)
            Case McLoginType.Ms : Return FrmLoginMs.IsVaild()
            Case McLoginType.Legacy : Return FrmLoginLegacy.IsVaild()
            Case McLoginType.Nide : Return FrmLoginNide.IsVaild()
            Case McLoginType.Auth : Return FrmLoginAuth.IsVaild()
            Case Else : Return "未知的登录方式"
        End Select
    End Function
    Private Function ValidateLoginData(Kind As Integer, Data As McLoginData) As String
        Select Case CType(Kind, McLoginType)
            Case McLoginType.Ms : Return PageLoginMs.IsVaild(Data)
            Case McLoginType.Legacy : Return PageLoginLegacy.IsVaild(Data)
            Case McLoginType.Nide : Return PageLoginNide.IsVaild(Data)
            Case McLoginType.Auth : Return PageLoginAuth.IsVaild(Data)
            Case Else : Return "未知的登录方式"
        End Select
    End Function
    Private Function ReadLoginInput(Kind As Integer, Skin As Boolean) As McLoginData
        Select Case CType(Kind, McLoginType)
            Case McLoginType.Legacy : Return PageLoginLegacy.GetLoginData()
            Case McLoginType.Ms : Return If(Skin, PageLoginMsSkin.GetLoginData(), PageLoginMs.GetLoginData())
            Case McLoginType.Nide : Return If(Skin, PageLoginNideSkin.GetLoginData(), PageLoginNide.GetLoginData())
            Case McLoginType.Auth : Return If(Skin, PageLoginAuthSkin.GetLoginData(), PageLoginAuth.GetLoginData())
            Case Else : Return Nothing
        End Select
    End Function
    Private Sub ExitServerLogin(Kind As Integer)
        Select Case CType(Kind, McLoginType)
            Case McLoginType.Auth : PageLoginAuthSkin.ExitLogin()
            Case McLoginType.Nide : PageLoginNideSkin.ExitLogin()
        End Select
    End Sub
    Private Function SelectLoginProfile(Profiles As JArray) As Integer
        Dim Controls As New List(Of IMyRadio)
        For Each Profile In Profiles
            Controls.Add(New MyRadioBox With {.Text = Profile("name").ToString()})
        Next
        Dim Selected As Integer = MyMsgBoxSelect(Controls, "选择使用的角色")
        Return Selected
    End Function
    Private Function CreateDevicePrompt(Prepared As JObject) As Func(Of PCLCS.LegacyLaunchDeviceResult)
        Dim Converter As New MyMsgBoxConverter With {.Content = Prepared, .ForceWait = True, .Type = MyMsgBoxType.Login}
        WaitingMyMsgBox.Enqueue(Converter)
        Return Function()
                   Dim Result = Converter.Result
                   If Result Is Nothing Then Return Nothing
                   If TypeOf Result Is RestartException Then Return New PCLCS.LegacyLaunchDeviceResult With {.Kind = PCLCS.LegacyLaunchDeviceResultKind.Restart}
                   If TypeOf Result Is Exception Then Return New PCLCS.LegacyLaunchDeviceResult With {.Kind = PCLCS.LegacyLaunchDeviceResultKind.Error, .Error = DirectCast(Result, Exception)}
                   Dim Tokens = DirectCast(Result, String())
                   Return New PCLCS.LegacyLaunchDeviceResult With {.Kind = PCLCS.LegacyLaunchDeviceResultKind.Tokens, .AccessToken = Tokens(0), .RefreshToken = Tokens(1)}
               End Function
    End Function
    Private Function SendLaunchRequest(Request As PCLCS.LegacyLaunchRequest) As String
        If Request.Kind = PCLCS.LegacyLaunchRequestKind.Multiple Then
            Return NetRequestByClientMultiple(Request.Url, Request.Method, ThreadCount:=Request.ThreadCount.GetValueOrDefault(3),
                Content:=Request.Content, ContentType:=Request.ContentType, Headers:=Request.Headers,
                RequireJson:=Request.RequireJson, Encoding:=Request.Encoding)
        End If
        Return NetRequestByClientRetry(Request.Url, Request.Method, Content:=Request.Content,
            ContentType:=Request.ContentType, Headers:=Request.Headers, RequireJson:=Request.RequireJson, Encoding:=Request.Encoding)
    End Function
    Private Function LauncherPixelSize() As PCLCS.LegacyLaunchSize
        Return New PCLCS.LegacyLaunchSize(GetPixelSize(FrmMain.PanForm.ActualWidth), GetPixelSize(FrmMain.PanForm.ActualHeight))
    End Function
    Private Function LoadLaunchImage(Address As String) As PCLCS.LegacyLaunchImage
        Return ImagePort(New MyBitmap(Address))
    End Function
    Private Function ImagePort(Image As MyBitmap) As PCLCS.LegacyLaunchImage
        Return New PCLCS.LegacyLaunchImage With {
            .ReadHeight = Function() Image.Pic.Height,
            .Save = Sub(Address) Image.Save(Address),
            .Clip = Function(X, Y, Width, Height) ImagePort(Image.Clip(X, Y, Width, Height))
        }
    End Function
    Private Function DependencyContext(Instance As McInstance, JavaVersion As String, ModsFolder As String) As PCLCS.LegacyLaunchDependencyContext
        Dim Context = ModDependencyInspector.ContextForInstance(Instance, JavaVersion)
        Return New PCLCS.LegacyLaunchDependencyContext With {
            .Inspect = Function(Token) DependencyReport(ModsFolder, ModDependencyInspector.Inspect(ModsFolder, Context, Token))
        }
    End Function
    Private Function DependencyReport(Folder As String, Report As ModDependencyInspector.InspectionReport) As PCLCS.LegacyLaunchDependencyReport
        RunInUi(Sub() FrmInstanceMod?.ApplyLoaderWarnings())
        Return New PCLCS.LegacyLaunchDependencyReport With {
            .Source = Report, .ReadHasErrors = Function() Report.HasErrors,
            .WasAccepted = Function() LaunchDependencyCheck.WasAccepted(Folder, Report),
            .Accept = Sub() LaunchDependencyCheck.Accept(Folder, Report),
            .BuildAutoFill = Function(Token) DependencyPlan(ModDependencyAutoFill.BuildPlan(Folder, Report, Token)),
            .BuildActivation = Function(Token) DependencyPlan(ModDependencyActivation.Prepare(Folder, Report, Token))
        }
    End Function
    Private Function DependencyPlan(Plan As ModDependencyAutoFill.Plan) As PCLCS.LegacyLaunchDependencyPlan
        Return New PCLCS.LegacyLaunchDependencyPlan With {
            .Source = Plan, .ReadRoot = Function() Plan.Root, .ReadActionText = Function() Plan.ActionText,
            .WriteCaption = Sub(Value) Plan.Caption = Value,
            .ReadItemsCount = Function() Plan.Items.Count, .ReadEnablesCount = Function() Plan.Enables.Count,
            .Install = Function(Token) ModDependencyAutoFill.Install(Plan, Token)
        }
    End Function
    Private Function ConfirmDependencies(Report As PCLCS.LegacyLaunchDependencyReport, Plan As PCLCS.LegacyLaunchDependencyPlan, AfterRepair As Boolean) As Integer
        Dim Original = DirectCast(Report.Source, ModDependencyInspector.InspectionReport)
        If AfterRepair Then Return MyMsgBoxConfirmContent("前置已处理，仍有以下问题。", "启动前模组检查", "返回 Mod 管理",
            Function() New ModDependencyCheckView(Original), "取消启动", "仍然启动", WarningButton:=3)
        Dim OriginalPlan = DirectCast(Plan.Source, ModDependencyAutoFill.Plan)
        Return MyMsgBoxConfirmContent("请检查以下模组问题。", "启动前模组检查", OriginalPlan.ActionText,
            Function() New ModDependencyCheckView(Original, OriginalPlan), "取消启动", "仍然启动", WarningButton:=3)
    End Function
    Private Sub OpenDependencyMods(Instance As McInstance)
        PageInstanceLeft.Instance = Instance
        FrmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.InstanceMod)
    End Sub

    Public Function McLaunchStart(Optional Options As McLaunchOptions = Nothing) As Boolean
        Return Service.McLaunchStart(Options)
    End Function
    Public Sub McLaunchLog(Text As String, Optional Level As LogLevel = LogLevel.Info)
        Service.McLaunchLog(Text, Level)
    End Sub
    Private Sub McLaunchState(Loader As LoaderTask(Of McLaunchOptions, Object))
        Service.McLaunchState(TaskPort(Loader))
    End Sub
    Private Sub McLaunchStart(Loader As LoaderTask(Of McLaunchOptions, Object))
        Service.McLaunchStart(TaskPort(Loader))
    End Sub
    Private Sub McLaunchDependencyCheck(Loader As LoaderTask(Of Integer, Integer))
        Service.McLaunchDependencyCheck(TaskPort(Loader))
    End Sub
    Private Sub InstallLaunchDependencies(Instance As McInstance, Plan As ModDependencyAutoFill.Plan, Token As CancellationToken)
        Service.InstallLaunchDependencies(Instance, DependencyPlan(Plan), Token)
    End Sub
    Private Sub McLaunchMemoryOptimize(Loader As LoaderTask(Of Integer, Integer))
        Service.McLaunchMemoryOptimize(TaskPort(Loader))
    End Sub
    Private Sub McLaunchPrecheck()
        Service.McLaunchPrecheck()
    End Sub
    Public Function McLoginName() As String
        Return Service.McLoginName()
    End Function
    Public Function McLoginAble() As String
        Return Service.McLoginAble()
    End Function
    Public Function McLoginAble(LoginData As McLoginData) As String
        Return Service.McLoginAble(LoginData)
    End Function
    Public Function McLoginInput() As McLoginData
        Return Service.McLoginInput()
    End Function
    Private Sub McLoginStart(Data As LoaderTask(Of McLoginData, McLoginResult))
        Service.McLoginStart(LoginPort(Data))
    End Sub
    Private Sub McLoginMsStart(Data As LoaderTask(Of McLoginMs, McLoginResult))
        Service.McLoginMsStart(LoginPort(Data))
    End Sub
    Private Sub McLoginServerStart(Data As LoaderTask(Of McLoginServer, McLoginResult))
        Service.McLoginServerStart(LoginPort(Data))
    End Sub
    Private Sub McLoginLegacyStart(Data As LoaderTask(Of McLoginLegacy, McLoginResult))
        Service.McLoginLegacyStart(LoginPort(Data))
    End Sub
    Private Sub McLoginRequestValidate(ByRef Data As LoaderTask(Of McLoginServer, McLoginResult))
        Service.McLoginRequestValidate(LoginPort(Data))
    End Sub
    Private Sub McLoginRequestRefresh(ByRef Data As LoaderTask(Of McLoginServer, McLoginResult), RequestUser As Boolean)
        Service.McLoginRequestRefresh(LoginPort(Data), RequestUser)
    End Sub
    Private Function McLoginRequestLogin(ByRef Data As LoaderTask(Of McLoginServer, McLoginResult)) As Boolean
        Return Service.McLoginRequestLogin(LoginPort(Data))
    End Function
    Private Function MsLoginStep1New(Data As LoaderTask(Of McLoginMs, McLoginResult)) As (OAuthAccessToken As String, OAuthRefreshToken As String)
        Return Service.MsLoginStep1New(LoginPort(Data))
    End Function
    Private Function MsLoginStep1Refresh(Code As String) As (OAuthAccessToken As String, OAuthRefreshToken As String)
        Return Service.MsLoginStep1Refresh(Code)
    End Function
    Private Function MsLoginStep2(AccessToken As String) As String
        Return Service.MsLoginStep2(AccessToken)
    End Function
    Private Function MsLoginStep3(XBLToken As String) As (XSTSToken As String, UHS As String)
        Return Service.MsLoginStep3(XBLToken)
    End Function
    Private Function MsLoginStep4(Tokens As (XSTSToken As String, UHS As String)) As (AccessToken As String, ExpiresAt As Integer)
        Return Service.MsLoginStep4(Tokens)
    End Function
    Private Sub MsLoginStep5(AccessToken As String)
        Service.MsLoginStep5(AccessToken)
    End Sub
    Private Function MsLoginStep6(AccessToken As String) As (UUID As String, UserName As String, ProfileJson As String)
        Return Service.MsLoginStep6(AccessToken)
    End Function
    Private Function McLoginLegacyUuidWithCustomSkin(UserName As String, SkinType As Integer, SkinName As String) As String
        Return Service.McLoginLegacyUuidWithCustomSkin(UserName, SkinType, SkinName)
    End Function
    Public Function McLoginMojangUuid(Name As String, ThrowOnNotFound As Boolean) As Object
        Return Service.McLoginMojangUuid(Name, ThrowOnNotFound)
    End Function
    Public Function McLoginLegacyUuid(Name As String) As Object
        Return Service.McLoginLegacyUuid(Name)
    End Function
    Private Sub McLaunchJava(Task As LoaderTask(Of Integer, Integer))
        Service.McLaunchJava(TaskPort(Task))
    End Sub
    Private Sub McLaunchArgumentMain(Loader As LoaderTask(Of String, Integer))
        Service.McLaunchArgumentMain(TaskPort(Loader))
    End Sub
    Private Function McLaunchArgumentsJVM() As List(Of String)
        Return Service.McLaunchArgumentsJVM()
    End Function
    Private Function McLaunchArgumentsGame() As List(Of String)
        Return Service.McLaunchArgumentsGame()
    End Function
    Private Function McLaunchArgumentsReplace(Arguments As List(Of String)) As IEnumerable(Of (Key As String, Value As String))
        Return Service.McLaunchArgumentsReplace(Arguments)
    End Function
    Private Function SplitJavaArguments(Arg As String) As IEnumerable(Of String)
        Return Service.SplitJavaArguments(Arg)
    End Function
    Private Function DeduplicateJavaArguments(Args As List(Of String), IsJVMArgs As Boolean) As List(Of String)
        Return Service.DeduplicateJavaArguments(Args, IsJVMArgs)
    End Function
    Private Sub McLaunchNatives(Loader As LoaderTask(Of Integer, Integer))
        Service.McLaunchNatives(TaskPort(Loader))
    End Sub
    Public Function GetNativesFolder(Optional Instance As McInstance = Nothing) As String
        Return Service.GetNativesFolder(Instance)
    End Function
    Private Sub McLaunchPrerun()
        Service.McLaunchPrerun()
    End Sub
    Private Sub McLaunchCustom(Loader As LoaderTask(Of Integer, Integer))
        Service.McLaunchCustom(TaskPort(Loader))
    End Sub
    Private Sub McLaunchRun(Loader As LoaderTask(Of Integer, Process))
        Service.McLaunchRun(TaskPort(Loader))
    End Sub
    Private Sub McLaunchWait(Loader As LoaderTask(Of Process, Integer))
        Service.McLaunchWait(TaskPort(Loader))
    End Sub
    Private Sub McLaunchEnd()
        Service.McLaunchEnd()
    End Sub

End Module
