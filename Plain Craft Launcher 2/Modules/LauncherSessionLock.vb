'原内部类型身份保留；实际BCL单例锁/持有/反向释放全部由C#实现。
Friend NotInheritable Class LauncherSessionLock
    Implements IDisposable
    Private ReadOnly Native As PCLCS.Startup.LauncherSessionLease
    Private Sub New(Lease As PCLCS.Startup.LauncherSessionLease)
        Native = Lease
    End Sub
    Friend Shared Function TryAcquire(Names As IEnumerable(Of String), Timeout As Integer) As LauncherSessionLock
        Dim Lease = PCLCS.Startup.LauncherSessionLease.TryAcquire(Names, Timeout)
        Return If(Lease Is Nothing, Nothing, New LauncherSessionLock(Lease))
    End Function
    Public Sub Dispose() Implements IDisposable.Dispose
        Native.Dispose()
    End Sub
End Class
