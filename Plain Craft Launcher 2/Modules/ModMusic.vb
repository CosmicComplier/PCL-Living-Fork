Option Strict On
Imports PCLLF.Presentation.Shell

'原公开字段保持唯一真实列表与 NAudio 对象；C# 通过 live host 直接读写它们。
Public Module ModMusic
    Public MusicWaitingList As List(Of String) = Nothing
    Public MusicAllList As List(Of String) = Nothing
    Public MusicNAudio As Object = Nothing
    Public Enum MusicStates
        [Stop]
        Play
        Pause
    End Enum
    Private ReadOnly Native As New MusicRuntime(New MusicHost With {
        .All = Function() MusicAllList, .Waiting = Function() MusicWaitingList,
        .SetAll = Sub(Value) MusicAllList = Value, .SetWaiting = Sub(Value) MusicWaitingList = Value,
        .Playback = Function() MusicNAudio, .SetPlayback = Sub(Value) MusicNAudio = Value,
        .BasePath = Function() Paths.Base, .BooleanSetting = Function(Name) Settings.Get(Of Boolean)(Name),
        .IntegerSetting = Function(Name) Settings.Get(Of Integer)(Name), .RandomInteger = AddressOf RandomInteger,
        .Ui = Sub(Work) RunInUi(Work), .Thread = AddressOf RunInThread,
        .StartThread = Sub(Work, Name, Priority) RunInNewThread(Work, Name, Priority),
        .Button = Function() FrmMain.BtnExtraMusic, .RefreshSettings = Sub() FrmSetupUI?.MusicRefreshUI(),
        .Hint = Sub(Text, Type, Log) Hint(Text, CType(Type, HintType), Log)})
    Public ReadOnly Property MusicState As MusicStates
        Get
            Return CType(CInt(Native.State), MusicStates)
        End Get
    End Property
    Public Sub MusicControlPause()
        Native.ControlPause()
    End Sub
    Public Sub MusicControlNext()
        Native.ControlNext()
    End Sub
    Public Sub MusicRefreshPlay(ShowHint As Boolean, Optional IsFirstLoad As Boolean = False)
        Native.RefreshPlay(ShowHint, IsFirstLoad)
    End Sub
    Public Function MusicPause() As Boolean
        Return Native.Pause()
    End Function
    Public Function MusicResume() As Boolean
        Return Native.Resume()
    End Function
End Module
