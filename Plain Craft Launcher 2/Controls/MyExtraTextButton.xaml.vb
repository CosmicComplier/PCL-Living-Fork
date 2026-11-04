Option Strict On
Imports System.Windows.Markup
Imports PCLLF.Presentation.Controls

<ContentProperty("Inlines")>
Public Class MyExtraTextButton
    Inherits MyExtraTextButtonView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly TextProperty As DependencyProperty = MyExtraTextButtonView.TextProperty
    Public Shadows Event Click(sender As Object, e As MouseButtonEventArgs)
    Protected Overrides Sub OnClick(sender As Object, Args As MouseButtonEventArgs)
        RaiseEvent Click(sender, Args)
    End Sub
    Protected Overrides Sub UiPost(Work As Action)
        RunInUi(Work)
    End Sub
End Class
