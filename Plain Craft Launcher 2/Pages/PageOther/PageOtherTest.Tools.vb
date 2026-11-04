Imports PCLCS
Imports PCLLF.Presentation.Controls

' Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
Partial Public Class PageOtherTest
    Private PageLoaded As Boolean
    Private Shared MemoryRunning As Integer
    Private Shared CleanupRunning As Integer
    Private _controller As LegacyToolboxPageController(Of NetFile,LoaderDownload,LoaderTask(Of Integer,Integer),LoaderCombo(Of Integer))
    Friend ReadOnly Property Controller As LegacyToolboxPageController(Of NetFile,LoaderDownload,LoaderTask(Of Integer,Integer),LoaderCombo(Of Integer))
        Get
            If _controller Is Nothing Then _controller=New LegacyToolboxPageController(Of NetFile,LoaderDownload,LoaderTask(Of Integer,Integer),LoaderCombo(Of Integer))(AuxiliaryPageRuntime.Tools,New LegacyToolboxPageView With {
.Url=Function() TextDownloadUrl,.Folder=Function() TextDownloadFolder,.Name=Function() TextDownloadName,.Start=Function() BtnDownloadStart,.Open=Function() BtnDownloadOpen,.Clear=Function() BtnClear,.Loaded=Function() PageLoaded,.SetLoaded=Sub(Value) PageLoaded=Value})
            Return _controller
        End Get
    End Property
    Friend Shared Function AcquireCleanup() As Integer
        Return Threading.Interlocked.CompareExchange(CleanupRunning,1,0)
    End Function
    Friend Shared Function AcquireMemory() As Integer
        Return Threading.Interlocked.CompareExchange(MemoryRunning,1,0)
    End Function
    Friend Shared Sub ReleaseCleanup()
        Threading.Interlocked.Exchange(CleanupRunning,0)
    End Sub
    Friend Shared Sub ReleaseMemory()
        Threading.Interlocked.Exchange(MemoryRunning,0)
    End Sub
    Friend Shared Function ReadCleanup() As Integer
        Return Threading.Volatile.Read(CleanupRunning)
    End Function
    Friend Shared ReadOnly Property IsCleaningGameFiles As Boolean
        Get
            Return AuxiliaryPageRuntime.Tools.IsCleaningGameFiles
        End Get
    End Property

    Public Sub New()
        InitializeComponent()
        Controller.InitializeValidation()
    End Sub

    Private Sub MeLoaded() Handles Me.Loaded
        Controller.Loaded()
    End Sub

    Private Sub StartButtonRefresh() Handles TextDownloadUrl.TextChanged, TextDownloadFolder.TextChanged, TextDownloadName.TextChanged
        Controller.StartButtonRefresh()
    End Sub

    Private Sub TextDownloadUrl_TextChanged() Handles TextDownloadUrl.TextChanged
        Controller.UrlChanged()
    End Sub

    Private Sub SaveCacheDownloadFolder() Handles TextDownloadFolder.ValidatedTextChanged
        Controller.SaveFolder()
    End Sub

    Private Sub TextDownloadUrl_KeyDown(sender As Object, e As KeyEventArgs) Handles TextDownloadUrl.KeyDown, TextDownloadName.KeyDown
        Controller.UrlKeyDown(e)
    End Sub

    Private Sub BtnDownloadStart_Click() Handles BtnDownloadStart.Click
        Controller.StartDownload()
    End Sub

    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        AuxiliaryPageRuntime.Tools.StartCustomDownload(Url, FileName, Folder)
    End Sub

    Private Shared Function TryStartCustomDownload(Url As String, FileName As String, Folder As String) As Boolean
        Return AuxiliaryPageRuntime.Tools.TryStartCustomDownload(Url, FileName, Folder)
    End Function

    Private Shared Sub DownloadState(Loader As LoaderCombo(Of Integer), Session As ToolboxDownloadSession)
        AuxiliaryPageRuntime.Tools.DownloadState(Loader, Session)
    End Sub

    Private Sub BtnDownloadSelect_Click() Handles BtnDownloadSelect.Click
        Controller.SelectFolder()
    End Sub

    Private Sub BtnDownloadOpen_Click() Handles BtnDownloadOpen.Click
        Controller.OpenFolder()
    End Sub

    Private Sub BtnClear_Click() Handles BtnClear.Click
        Controller.Clear()
    End Sub

    Public Shared Sub RubbishClear()
        AuxiliaryPageRuntime.Tools.RubbishClear()
    End Sub

    Private Shared Function GameFilesBusy() As Boolean
        Return AuxiliaryPageRuntime.Tools.GameFilesBusy()
    End Function

    Private Sub BtnMemory_Click() Handles BtnMemory.Click
        Controller.Memory()
    End Sub

    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        AuxiliaryPageRuntime.Tools.MemoryOptimize(ShowHint)
    End Sub

    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        AuxiliaryPageRuntime.Tools.MemoryOptimizeInternal(ShowHint)
    End Sub

End Class
