Public Module ModWatcher
    Public McWatcherList As New List(Of Watcher)
    Public HasRunningMinecraft As Boolean = False
    Private ReadOnly Host As New PCLCS.LegacyWatcherHost(Of Watcher, McInstance, LoaderTask(Of Process, Integer), Watcher.MinecraftState) With {
        .Loading = Watcher.MinecraftState.Loading, .Running = Watcher.MinecraftState.Running,
        .Crashed = Watcher.MinecraftState.Crashed, .Ended = Watcher.MinecraftState.Ended, .Canceled = Watcher.MinecraftState.Canceled,
        .ReadWatchers = Function() McWatcherList, .WriteWatchers = Sub(Value) McWatcherList = Value,
        .WriteHasRunningMinecraft = Sub(Value) HasRunningMinecraft = Value,
        .LaunchLog = Sub(Text, Level) McLaunchLog(Text, Level),
        .RunInUi = Sub(Work) RunInUi(Work), .RunInNewThread = Sub(Work, Name) RunInNewThread(Work, Name),
        .UpdateRecentWorldVisibility = Sub() FrmLaunchRight?.UpdateRecentWorldVisibility(),
        .RefreshRecentWorlds = Sub() FrmLaunchRight?.RefreshRecentWorlds(), .RefreshShutdownButton = Sub() FrmMain.BtnExtraShutdown.ShowRefresh(),
        .EndProgram = Sub() FrmMain.EndProgram(False), .RestoreLauncher = Sub() FrmMain.Hidden = False,
        .MusicStop = Function() Settings.Get(Of Boolean)("UiMusicStop"), .MusicStart = Function() Settings.Get(Of Boolean)("UiMusicStart"),
        .MusicResume = Function() MusicResume(), .MusicPause = Function() MusicPause(),
        .LauncherVisibility = Function() Settings.Get(Of Integer)("LaunchArgumentVisible"), .WindowType = Function() Settings.Get(Of Integer)("LaunchArgumentWindowType"),
        .LoaderInput = Function(Loader) Loader.Input, .LoaderCanceled = Function(Loader) Loader.State = LoadState.Canceled,
        .LoaderProgress = Sub(Loader, Value) Loader.Progress = Value,
        .ProcessStopped = Sub(Instance, GameProcess, NormalExit) WorldBackups.OnProcessStopped(Instance, GameProcess, NormalExit),
        .MarkHealthy = Sub(VersionPath, Point, NormalExit) EnvironmentHistory.MarkHealthy(VersionPath, Point, NormalExit),
        .ReleaseTime = Function(Instance) Instance.ReleaseTime, .InstancePath = Function(Instance) Instance.PathVersion,
        .IndiePath = Function(Instance) Instance.PathIndie, .InstanceName = Function(Instance) Instance.Name, .BasePath = Function() Paths.Base,
        .CrashHint = Sub() Hint("检测到 Minecraft 出现错误，错误分析已开始……", HintType.Red), .FeedbackInfo = Sub() FeedbackInfo(),
        .CreateCrashAnalyzer = Function(Instance) New CrashAnalyzer(Instance),
        .EnumWindows = AddressOf NativeEnumWindows,
        .GetClassName = Function(Handle, Buffer, Count) GetClassName(Handle, Buffer, Count),
        .GetWindowText = Function(Handle, Buffer, Count) GetWindowText(Handle, Buffer, Count),
        .SetWindowText = Sub(Handle, Text) SetWindowText(Handle, Text), .ShowWindow = Sub(Handle, Command) ShowWindow(Handle, Command),
        .IsWindowVisible = Function(Handle) IsWindowVisible(Handle), .GetWindowProcessId = AddressOf NativeWindowProcessId}
    Private ReadOnly Group As New PCLCS.LegacyWatcherGroup(Of Watcher, McInstance, LoaderTask(Of Process, Integer), Watcher.MinecraftState)(Host)
    Private Sub NativeEnumWindows(Callback As Action(Of IntPtr, Integer), Parameter As Integer)
        EnumWindows(Sub(Handle, Value) Callback(Handle, Value), Parameter)
    End Sub
    Private Function NativeWindowProcessId(Handle As IntPtr) As Integer
        Dim ProcessId As Integer
        GetWindowThreadProcessId(Handle, ProcessId)
        Return ProcessId
    End Function
    Private Delegate Sub EnumWindowsSub(hwnd As IntPtr, lParam As Integer)
    Private Declare Function EnumWindows Lib "user32" (hWnd As EnumWindowsSub, lParam As Integer) As Boolean
    Private Declare Function GetClassName Lib "user32" Alias "GetClassNameA" (hWnd As Integer, str As StringBuilder, maxCount As Integer) As Integer
    Private Declare Function GetWindowText Lib "user32" Alias "GetWindowTextA" (hWnd As Integer, str As StringBuilder, maxCount As Integer) As Integer
    Private Declare Function SetWindowText Lib "user32" Alias "SetWindowTextA" (hWnd As Integer, str As String) As Boolean
    Private Declare Function ShowWindow Lib "user32" (hWnd As IntPtr, cmdWindow As UInteger) As Boolean
    Private Declare Function IsWindowVisible Lib "user32" (hWnd As IntPtr) As Boolean
    Private Declare Function GetWindowThreadProcessId Lib "user32" (hWnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer

    Public Class Watcher
        Implements PCLCS.ILegacyWatcherData(Of McInstance, LoaderTask(Of Process, Integer), MinecraftState)
        Public GameProcess As Process
        Public Instance As McInstance
        Public Loader As LoaderTask(Of Process, Integer)
        Public WaitingLog As New List(Of String)(1000)
        Public LatestLog As New ConcurrentQueue(Of String)
        Private ReadOnly Native As PCLCS.LegacyWatcher(Of Watcher, McInstance, LoaderTask(Of Process, Integer), MinecraftState)
        Public Sub New(Loader As LoaderTask(Of Process, Integer), Instance As McInstance, WindowTitle As String, Optional BeforeMonitoring As Action(Of Process) = Nothing, Optional OnStopped As Action = Nothing, Optional EnvironmentPoint As String = Nothing)
            Native = New PCLCS.LegacyWatcher(Of Watcher, McInstance, LoaderTask(Of Process, Integer), MinecraftState)(Me, Host, Group)
            Native.Initialize(Loader, Instance, WindowTitle, BeforeMonitoring, OnStopped, EnvironmentPoint)
        End Sub
        Public Property State As MinecraftState
            Get
                Return Native.State
            End Get
            Set(value As MinecraftState)
                Native.State = value
            End Set
        End Property
        Public Enum MinecraftState
            Loading
            Running
            Crashed
            Ended
            Canceled
        End Enum
        Public Sub Kill()
            Native.Kill()
        End Sub
        Private Property CoreGameProcess As Process Implements PCLCS.ILegacyWatcherData(Of McInstance, LoaderTask(Of Process, Integer), MinecraftState).CoreGameProcess
            Get
                Return GameProcess
            End Get
            Set(value As Process)
                GameProcess = value
            End Set
        End Property
        Private Property CoreInstance As McInstance Implements PCLCS.ILegacyWatcherData(Of McInstance, LoaderTask(Of Process, Integer), MinecraftState).CoreInstance
            Get
                Return Instance
            End Get
            Set(value As McInstance)
                Instance = value
            End Set
        End Property
        Private Property CoreLoader As LoaderTask(Of Process, Integer) Implements PCLCS.ILegacyWatcherData(Of McInstance, LoaderTask(Of Process, Integer), MinecraftState).CoreLoader
            Get
                Return Loader
            End Get
            Set(value As LoaderTask(Of Process, Integer))
                Loader = value
            End Set
        End Property
        Private Property CoreState As MinecraftState Implements PCLCS.ILegacyWatcherData(Of McInstance, LoaderTask(Of Process, Integer), MinecraftState).CoreState
            Get
                Return State
            End Get
            Set(value As MinecraftState)
                State = value
            End Set
        End Property
        Private Property CoreWaitingLog As List(Of String) Implements PCLCS.ILegacyWatcherData(Of McInstance, LoaderTask(Of Process, Integer), MinecraftState).CoreWaitingLog
            Get
                Return WaitingLog
            End Get
            Set(value As List(Of String))
                WaitingLog = value
            End Set
        End Property
        Private Property CoreLatestLog As ConcurrentQueue(Of String) Implements PCLCS.ILegacyWatcherData(Of McInstance, LoaderTask(Of Process, Integer), MinecraftState).CoreLatestLog
            Get
                Return LatestLog
            End Get
            Set(value As ConcurrentQueue(Of String))
                LatestLog = value
            End Set
        End Property
    End Class
End Module
