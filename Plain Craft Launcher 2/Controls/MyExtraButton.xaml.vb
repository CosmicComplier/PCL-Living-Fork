Option Strict On
Imports PCLLF.Presentation.Controls

Public Class MyExtraButton
    Inherits MyExtraButtonView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shadows Event Click(sender As Object, e As MouseButtonEventArgs)
    Public Shadows Event RightClick(sender As Object, e As MouseButtonEventArgs)
    Protected Overrides Sub OnClick(sender As Object, Args As MouseButtonEventArgs)
        RaiseEvent Click(sender, Args)
    End Sub
    Protected Overrides Sub OnRightClick(sender As Object, Args As MouseButtonEventArgs)
        RaiseEvent RightClick(sender, Args)
    End Sub
    Public Shadows Delegate Function ShowCheckDelegate() As Boolean
    Public Shadows ShowCheck As ShowCheckDelegate
    Protected Overrides ReadOnly Property HasShowCheck As Boolean
        Get
            Return ShowCheck IsNot Nothing
        End Get
    End Property
    Protected Overrides Function CheckShow() As Boolean
        Return ShowCheck()
    End Function
    Protected Overrides Sub UiPost(Work As Action)
        RunInUi(Work)
    End Sub
End Class
