Option Strict On
Imports PCLLF.Presentation.Controls

Public Class MyPageLeft
    Inherits MyPageLeftView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly AnimatedControlProperty As DependencyProperty = MyPageLeftView.AnimatedControlProperty
End Class
