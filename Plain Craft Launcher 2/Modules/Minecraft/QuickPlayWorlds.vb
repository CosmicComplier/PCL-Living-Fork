Imports System.IO

Friend Class QuickPlaySession
    Inherits PCLCS.QuickPlaySessionData
End Class

Friend Class RecentWorld
    Inherits PCLCS.RecentWorldData
End Class

Friend Module QuickPlayWorlds
    Private ReadOnly HistoryService As New PCLCS.QuickPlayHistory(
        LocalGameData.Service, LocalGameData.Gate, Function() Settings.Get(Of Boolean)("LaunchRecentWorlds"),
        Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName),
        Function(First, Second) PathUtils.IsSame(First, Second), Function(VersionPath) ModModpackMode.AllowsInstance(VersionPath),
        Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message), Function() New RecentWorld)
    Private ReadOnly FlowService As New PCLCS.QuickPlayFlow(
        HistoryService, LocalGameData.Service, LocalGameData.Gate, Function() Settings.Get(Of Boolean)("LaunchRecentWorlds"),
        Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName),
        Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message), Function() New QuickPlaySession, Function() New RecentWorld)
    Friend Function Supported(Instance As McInstance) As Boolean
        Return FlowService.Supported(Adapt(Instance))
    End Function
    Friend Function WorldPath(GameFolder As String, Identifier As String) As String
        Return PCLCS.QuickPlayWorldRules.WorldPath(GameFolder, Identifier, Sub(Root, World) InstanceResourceStore.EnsureWithin(Root, World))
    End Function
    Friend Function Start(Instance As McInstance, Identifier As String) As Boolean
        Try
            If Instance Is Nothing OrElse Not ModModpackMode.AllowsInstance(Instance.PathVersion) Then Throw New IOException("请使用当前整合包目录中的游戏版本。")
            Instance.Load()
            If Not Supported(Instance) Then Throw New IOException("直接进入存档需要支持 Quick Play 的 Minecraft Java 1.20 或更新版本。")
            WorldPath(Instance.PathIndie, Identifier)
            If HasRunningMinecraft Then Throw New IOException("请先关闭正在运行的游戏，再直接进入存档。")
            FrmMain.PageChange(FormMain.PageType.Launch)
            Return McLaunchStart(New McLaunchOptions With {.Instance = Instance, .ServerIp = "", .QuickPlayWorld = Identifier})
        Catch ex As Exception
            Hint(ex.Message, HintType.Red)
            Return False
        End Try
    End Function
    Friend Function Prepare(Instance As McInstance, Identifier As String, Arguments As List(Of String), SaveBatch As Boolean) As QuickPlaySession
        Return DirectCast(FlowService.Prepare(Adapt(Instance), Identifier, Arguments, SaveBatch), QuickPlaySession)
    End Function
    Private Sub RemoveArguments(Arguments As List(Of String), Names As IEnumerable(Of String))
        PCLCS.QuickPlayWorldRules.RemoveArguments(Arguments, Names)
    End Sub
    Friend Sub Track(Session As QuickPlaySession, Game As Process)
        FlowService.Track(Session, Game)
    End Sub
    Friend Sub Finish(Session As QuickPlaySession)
        FlowService.Finish(Session)
    End Sub
    Private Sub ReadSessionLog(Session As QuickPlaySession)
        HistoryService.ReadSessionLog(Session)
    End Sub
    Friend Sub Remember(World As RecentWorld)
        HistoryService.Remember(World)
    End Sub
    Friend Sub DiscoverExistingWorlds(Instance As McInstance)
        FlowService.DiscoverExistingWorlds(Adapt(Instance))
    End Sub
    Private Function ReadEntries() As List(Of RecentWorld)
        Return HistoryService.ReadEntries().Cast(Of RecentWorld)().ToList()
    End Function
    Friend Function Recent() As List(Of RecentWorld)
        Return HistoryService.Recent().Cast(Of RecentWorld)().ToList()
    End Function
    Friend Sub RecoverExitedSessions()
        FlowService.RecoverExitedSessions()
    End Sub
    Friend Sub OnEnabledChanged(Value As Object)
        RunInUi(Sub() FrmLaunchRight?.RefreshRecentWorlds())
    End Sub
    Friend Function HasRunningSessions() As Boolean
        Return FlowService.HasRunningSessions()
    End Function
    Private Function Adapt(Instance As McInstance) As PCLCS.IQuickPlayInstance
        Return If(Instance Is Nothing, Nothing, New QuickPlayInstance(Instance))
    End Function
    Private NotInheritable Class QuickPlayInstance
        Implements PCLCS.IQuickPlayInstance
        Private ReadOnly Owner As McInstance
        Friend Sub New(Instance As McInstance)
            Owner = Instance
        End Sub
        Public ReadOnly Property VersionPath As String Implements PCLCS.IQuickPlayInstance.VersionPath
            Get
                Return Owner.PathVersion
            End Get
        End Property
        Public ReadOnly Property GameFolder As String Implements PCLCS.IQuickPlayInstance.GameFolder
            Get
                Return Owner.PathIndie
            End Get
        End Property
        Public ReadOnly Property VersionValid As Boolean Implements PCLCS.IQuickPlayInstance.VersionValid
            Get
                Return Owner.Version.Valid
            End Get
        End Property
        Public ReadOnly Property VanillaMajor As Integer Implements PCLCS.IQuickPlayInstance.VanillaMajor
            Get
                Return Owner.Version.Vanilla.Major
            End Get
        End Property
        Public ReadOnly Property IsSnapshot As Boolean Implements PCLCS.IQuickPlayInstance.IsSnapshot
            Get
                Return Owner.Version.IsSnapshot
            End Get
        End Property
        Public ReadOnly Property ReleaseTime As Date Implements PCLCS.IQuickPlayInstance.ReleaseTime
            Get
                Return Owner.ReleaseTime
            End Get
        End Property
        Public ReadOnly Property HasSavedIsolation As Boolean Implements PCLCS.IQuickPlayInstance.HasSavedIsolation
            Get
                Return Settings.HasSaved("VersionArgumentIndieV2", Instance:=Owner)
            End Get
        End Property
        Public ReadOnly Property IsolationEnabled As Boolean Implements PCLCS.IQuickPlayInstance.IsolationEnabled
            Get
                Return Settings.Get(Of Boolean)("VersionArgumentIndieV2", Instance:=Owner)
            End Get
        End Property
    End Class
End Module
