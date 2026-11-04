Option Strict On
Imports PCLLF.Presentation.Controls

Public Class MyTextButton
    Inherits MyTextButtonView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly TextProperty As DependencyProperty = MyTextButtonView.TextProperty
    Public Shadows Event Click(sender As Object, e As EventArgs)
    Protected Overrides Sub OnClick()
        RaiseEvent Click(Me, Nothing)
    End Sub
End Class
