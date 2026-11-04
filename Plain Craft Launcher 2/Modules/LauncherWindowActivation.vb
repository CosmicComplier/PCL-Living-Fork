'原Win32测试入口与内部类型保留；真实Unicode lookup/协议/重试业务在C#。
Friend NotInheritable Class LauncherWindowActivation
    Private Sub New()
    End Sub
    Private Shared Function FindWindowW(ClassName As String, WindowName As String) As IntPtr
        Return PCLCS.Startup.LauncherWindowActivation.FindWindow(ClassName, WindowName)
    End Function
    Friend Shared Function TryActivateExistingWindow() As Boolean
        Return PCLCS.Startup.LauncherWindowActivation.TryActivateExistingWindow()
    End Function
    Friend Shared Function RequestActivation(Lookup As Func(Of String, IntPtr),
                                             Queue As Func(Of IntPtr, UInteger, Boolean),
                                             Foreground As Action(Of IntPtr),
                                             Pause As Action(Of Integer)) As Boolean
        Return PCLCS.Startup.LauncherWindowActivation.RequestActivation(Lookup, Queue, Foreground, Pause)
    End Function
End Class
