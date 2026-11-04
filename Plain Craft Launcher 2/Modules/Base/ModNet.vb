Imports PCLCS.Network
Imports System.Net.Sockets
Imports NetworkCore = PCLCS.Network.LegacyNetworkRuntime(Of PCL.ModNet.NetSource, PCL.ModNet.NetThread, PCL.ModNet.NetFile, PCL.ModNet.LoaderDownload, PCL.ModNet.NetManagerClass, PCL.ModLoader.LoaderBase, PCL.ModNet.NetState, PCL.ModBase.LoadState, PCL.MyLoading.MyLoadingState)
Imports NetFileCore = PCLCS.Network.LegacyNetFile(Of PCL.ModNet.NetSource, PCL.ModNet.NetThread, PCL.ModNet.NetFile, PCL.ModNet.LoaderDownload, PCL.ModNet.NetManagerClass, PCL.ModLoader.LoaderBase, PCL.ModNet.NetState, PCL.ModBase.LoadState, PCL.MyLoading.MyLoadingState)
Imports NetManagerCore = PCLCS.Network.LegacyNetManager(Of PCL.ModNet.NetSource, PCL.ModNet.NetThread, PCL.ModNet.NetFile, PCL.ModNet.LoaderDownload, PCL.ModNet.NetManagerClass, PCL.ModLoader.LoaderBase, PCL.ModNet.NetState, PCL.ModBase.LoadState, PCL.MyLoading.MyLoadingState)

'原模块、旧嵌套类型/枚举/字段身份和真实宿主边界；完整网络/下载业务在 C#。
Public Module ModNet
    Public NetTaskSpeedLimitLow As Long = 256 * 1024L
    Public NetTaskSpeedLimitLeft As Long = -1
    Public NetTaskThreadCount As Integer = 0

    Private ReadOnly NativeHost As New LegacyNetworkHost(Of NetFile, LoaderDownload, NetManagerClass, LoaderBase) With {
        .Clock = Function() GetTimeMs(), .NewUuid = Function() GetUuid(),
        .RandomInteger = Function(Min, Max) RandomInteger(Min, Max),
        .InUi = Function() RunInUi(), .IsProgramEnding = Function() IsProgramEnding,
        .TempFolder = Function() PathTemp, .RequestTempFolder = Function() RequestTaskTempFolder(), .AppData = Function() Paths.AppData,
        .IntegerSetting = Function(Key) Settings.Get(Of Integer)(Key), .BooleanSetting = Function(Key) Settings.Get(Of Boolean)(Key),
        .SignCdn = AddressOf SecretCdnSign, .SignHeaders = AddressOf SecretHeadersSign,
        .WrapRedirects = Function(Transport) New SafeRedirectHandler(Transport),
        .CreateCachedClient = AddressOf CreateNativeCachedClient, .Waiter = New NativeTaskWaiter(),
        .RunNewThread = Function(Work, Name, Priority) RunInNewThread(Work, Name, Priority),
        .MinecraftFolders = Function() McFolderList.Select(Function(Folder) Folder.Location),
        .Taskbar = Function() LoaderTaskbar, .Manager = Function() NetManager,
        .CreateFile = Function(Urls, Path, Check, Browser) New NetFile(Urls, Path, Check, Browser),
        .CreateDownload = Function(Name, Files) New LoaderDownload(Name, Files),
        .SpeedLimitLow = Function() NetTaskSpeedLimitLow, .SetSpeedLimitLow = Sub(Value) NetTaskSpeedLimitLow = Value,
        .SpeedLimitLeft = Function() NetTaskSpeedLimitLeft,
        .AddSpeedLimitLeft = Function(Delta) Interlocked.Add(NetTaskSpeedLimitLeft, Delta),
        .ThreadCount = Function() NetTaskThreadCount,
        .IncrementThreadCount = Function() Interlocked.Increment(NetTaskThreadCount), .DecrementThreadCount = Function() Interlocked.Decrement(NetTaskThreadCount),
        .AllowCredentialHeaders = Function(File) File.AllowCredentialHeaders,
        .CanceledMergeFinished = Function(File) File.CanceledMergeFinished,
        .AcceptLoginCertificate = AddressOf LoginCertificatePolicy.Accept
    }
    Private ReadOnly NativeRuntime As New NetworkCore(NativeHost)
    Public NetManager As New NetManagerClass()

    <System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>
    Private Function CreateNativeCachedClient(Path As String, Transport As HttpMessageHandler) As HttpClient
        Return CacheCow.Client.ClientExtensions.CreateClient(New CacheCow.Client.FileCacheStore.FileStore(Path), Transport)
    End Function
    Private NotInheritable Class NativeTaskWaiter
        Implements ILegacyNetworkTaskWaiter
        Public Function Result(Of T)(Work As Task(Of T), Source As CancellationTokenSource, Timeout As Integer) As T Implements ILegacyNetworkTaskWaiter.Result
            Return GetResultWithTimeout(Work, Source, Timeout)
        End Function
        Public Sub Result(Work As Task, Source As CancellationTokenSource, Timeout As Integer) Implements ILegacyNetworkTaskWaiter.Result
            GetResultWithTimeout(Work, Source, Timeout)
        End Sub
    End Class

    Public ReadOnly Property NetTaskThreadLimit As Integer
        Get
            Return NativeRuntime.NetTaskThreadLimit
        End Get
    End Property
    Public ReadOnly Property NetTaskSpeedLimitHigh As Long
        Get
            Return NativeRuntime.NetTaskSpeedLimitHigh
        End Get
    End Property
    Public Sub UpdateNetTaskSpeedLimitHigh(Value As Integer)
        NativeRuntime.UpdateNetTaskSpeedLimitHigh(Value)
    End Sub
    Public Function NetRequestByClient(Url As String, Optional Method As HttpMethod = Nothing,
            Optional Content As Object = Nothing, Optional ContentType As String = Nothing, Optional Accept As String = "*/*",
            Optional Timeout As Integer = 25000, Optional Headers As String(,) = Nothing, Optional RequireJson As Boolean = False,
            Optional Encoding As Encoding = Nothing, Optional SimulateBrowserHeaders As Boolean = False, Optional MakeLog As Boolean = True) As String
        Return NativeRuntime.NetRequestByClient(Url, Method, Content, ContentType, Accept, Timeout, Headers, RequireJson, Encoding, SimulateBrowserHeaders, MakeLog)
    End Function
    Public Function NetRequestByClientRetry(Url As String, Optional Method As HttpMethod = Nothing, Optional BackupUrl As String = Nothing,
            Optional Content As Object = Nothing, Optional ContentType As String = Nothing, Optional Accept As String = "*/*",
            Optional Headers As String(,) = Nothing, Optional RequireJson As Boolean = False,
            Optional Encoding As Encoding = Nothing, Optional SimulateBrowserHeaders As Boolean = False) As String
        Return NativeRuntime.NetRequestByClientRetry(Url, Method, BackupUrl, Content, ContentType, Accept, Headers, RequireJson, Encoding, SimulateBrowserHeaders)
    End Function
    Public Function NetRequestByClientMultiple(Url As String, Optional Method As HttpMethod = Nothing, Optional ThreadCount As Integer = 3,
            Optional Content As Object = Nothing, Optional ContentType As String = Nothing, Optional Accept As String = "*/*",
            Optional Headers As String(,) = Nothing, Optional RequireJson As Boolean = False, Optional Timeout As Integer = 25000,
            Optional Encoding As Encoding = Nothing, Optional SimulateBrowserHeaders As Boolean = False) As String
        Return NativeRuntime.NetRequestByClientMultiple(Url, Method, ThreadCount, Content, ContentType, Accept, Headers, RequireJson, Timeout, Encoding, SimulateBrowserHeaders)
    End Function
    Public Function NetRequestByLoader(Url As String, Optional Timeout As Integer = 45000, Optional IsJson As Boolean = False, Optional SimulateBrowserHeaders As Boolean = False) As String
        Return NativeRuntime.NetRequestByLoader(Url, Timeout, IsJson, SimulateBrowserHeaders)
    End Function
    Public Function NetRequestByLoader(Urls As IEnumerable(Of String), Optional Timeout As Integer = 45000, Optional IsJson As Boolean = False, Optional SimulateBrowserHeaders As Boolean = False) As String
        Return NativeRuntime.NetRequestByLoader(Urls, Timeout, IsJson, SimulateBrowserHeaders)
    End Function
    Public Sub NetDownloadByClient(Url As String, LocalPath As String, Optional SimulateBrowserHeaders As Boolean = False)
        NativeRuntime.NetDownloadByClient(Url, LocalPath, SimulateBrowserHeaders)
    End Sub
    Public Sub NetDownloadByLoader(Url As String, LocalPath As String, Optional LoaderToSyncProgress As LoaderBase = Nothing, Optional Check As FileChecker = Nothing, Optional SimulateBrowserHeaders As Boolean = False)
        NativeRuntime.NetDownloadByLoader(Url, LocalPath, LoaderToSyncProgress, Check, SimulateBrowserHeaders)
    End Sub
    Public Sub NetDownloadByLoader(Urls As IEnumerable(Of String), LocalPath As String, Optional LoaderToSyncProgress As LoaderBase = Nothing, Optional Check As FileChecker = Nothing, Optional SimulateBrowserHeaders As Boolean = False)
        NativeRuntime.NetDownloadByLoader(Urls, LocalPath, LoaderToSyncProgress, Check, SimulateBrowserHeaders)
    End Sub
    Private Function SendRequest(Url As String, Method As HttpMethod,
            Optional Content As Object = Nothing, Optional Headers As Dictionary(Of String, String) = Nothing,
            Optional SimulateBrowserHeaders As Boolean = False, Optional Timeout As Integer = 25000, Optional Encoding As Encoding = Nothing) As Byte()
        Return NativeRuntime.SendRequest(Url, Method, Content, Headers, SimulateBrowserHeaders, Timeout, Encoding)
    End Function
    Public Function HasDownloadingTask(Optional IgnoreCustomDownload As Boolean = False) As Boolean
        Return NativeRuntime.HasDownloadingTask(IgnoreCustomDownload)
    End Function
    Public Function FindFreePort() As Integer
        Return NativeRuntime.FindFreePort()
    End Function
    Public Function GetUsedPorts() As List(Of Integer)
        Return NativeRuntime.GetUsedPorts()
    End Function
    Public Function FindFreePorts(ConsecutiveCount As Integer, ParamArray ExtraBlackLists As Integer()) As List(Of Integer)
        Return NativeRuntime.FindFreePorts(ConsecutiveCount, ExtraBlackLists)
    End Function
    Public Function Ping(Ip As String, Optional Timeout As Integer = 10000, Optional MakeLog As Boolean = True) As Integer
        Return NativeRuntime.Ping(Ip, Timeout, MakeLog)
    End Function
    Public Sub ShouldValidateSslCertificateOnLogin(Enabled As Boolean)
        NativeRuntime.ShouldValidateSslCertificateOnLogin(Enabled)
    End Sub

    Public Class NetSource
        Inherits LegacyNetSourceData(Of NetThread)
    End Class
    Public Enum NetState
        WaitingToCheck = -1
        WaitingToDownload = 0
        Connecting = 1
        Reading = 2
        Downloading = 3
        Merging = 4
        Finished = 5
        Canceled = 6
    End Enum
    Public Enum NetPreDownloadBehaviour
        HintWhileExists
        ExitWhileExistsOrDownloading
        IgnoreCheck
    End Enum
    Public Class NetThread
        Inherits LegacyNetThreadData(Of NetFile, NetSource, NetThread, NetState)
        Public Sub New()
            MyBase.New(Function() GetTimeMs(), NetState.WaitingToDownload)
        End Sub
        Public Shared Operator =(Left As NetThread, Right As NetThread) As Boolean
            Return EqualityComparer(Of NetThread).Default.Equals(Left, Right)
        End Operator
        Public Shared Operator <>(Left As NetThread, Right As NetThread) As Boolean
            Return Not Left = Right
        End Operator
    End Class
    Public Class NetFile
        Inherits NetFileCore
        Friend CanceledMergeFinished As Action
        Friend AllowCredentialHeaders As Boolean = True
        Public Sub New(Urls As IEnumerable(Of String), LocalPath As String, Optional Checker As FileChecker = Nothing, Optional SimulateBrowserHeaders As Boolean = False)
            MyBase.New(NativeRuntime, Urls, LocalPath, Checker, SimulateBrowserHeaders)
        End Sub
    End Class
    Public Class LoaderDownload
        Inherits LoaderBase
        Implements ILegacyNetworkDownload(Of NetFile)
        Public Files As ConcurrentQueue(Of NetFile)
        Private ReadOnly NativeData As New LegacyNetworkDownloadRecord()
        Private Property CoreFiles As ConcurrentQueue(Of NetFile) Implements ILegacyNetworkDownload(Of NetFile).CoreFiles
            Get
                Return Files
            End Get
            Set(Value As ConcurrentQueue(Of NetFile))
                Files = Value
            End Set
        End Property
        Private ReadOnly Property CoreRecord As LegacyNetworkDownloadRecord Implements ILegacyNetworkDownload(Of NetFile).CoreRecord
            Get
                Return NativeData
            End Get
        End Property
        Public Sub New(Name As String, FileTasks As IEnumerable(Of NetFile))
            NativeRuntime.InitializeDownload(Me, Name, FileTasks)
        End Sub
        Public Overrides Property Progress As Double
            Get
                Return NativeRuntime.DownloadProgress(Me)
            End Get
            Set(Value As Double)
                NetworkCore.RejectDownloadProgress(Value)
            End Set
        End Property
        Public Property FailCount As Integer Implements ILegacyNetworkDownload(Of NetFile).FailCount
            Get
                Return NativeRuntime.DownloadFailCount(Me)
            End Get
            Set(Value As Integer)
                NativeRuntime.SetDownloadFailCount(Me, Value)
            End Set
        End Property
        Private Sub CorePreviewFinish() Implements ILegacyNetworkDownload(Of NetFile).CorePreviewFinish
            RaisePreviewFinish()
        End Sub
        Public Sub RefreshStat() Implements ILegacyNetworkDownload(Of NetFile).RefreshStat
            NativeRuntime.RefreshDownloadStat(Me)
        End Sub
        Public Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
            NativeRuntime.StartDownload(Me, Input, IsForceRestart)
        End Sub
        Private Sub CheckExistingFiles(Files As List(Of NetFile), FolderList As List(Of String))
            NativeRuntime.CheckExistingFiles(Me, Files, FolderList)
        End Sub
        Private Function CheckExistingFile(FolderList As List(Of String), VersionFolders As List(Of String), File As NetFile) As String
            Return NativeRuntime.CheckExistingFile(Me, FolderList, VersionFolders, File)
        End Function
        Public Sub OnFileFinish(File As NetFile) Implements ILegacyNetworkDownload(Of NetFile).OnFileFinish
            NativeRuntime.OnDownloadFileFinish(Me, File)
        End Sub
        Public Sub OnFinish()
            NativeRuntime.OnDownloadFinish(Me)
        End Sub
        Public Sub OnFileFail(File As NetFile) Implements ILegacyNetworkDownload(Of NetFile).OnFileFail
            NativeRuntime.OnDownloadFileFail(Me, File)
        End Sub
        Public Overrides Sub Failed(Ex As Exception)
            NativeRuntime.FailDownload(Me, Ex)
        End Sub
        Public Sub OnFail(ExList As List(Of Exception)) Implements ILegacyNetworkDownload(Of NetFile).OnFail
            NativeRuntime.OnDownloadFail(Me, ExList)
        End Sub
        Public Overrides Sub Cancel()
            NativeRuntime.CancelDownload(Me)
        End Sub
    End Class
    Public Class NetManagerClass
        Inherits NetManagerCore
        Public Sub New()
            MyBase.New(NativeRuntime)
        End Sub
    End Class
End Module
