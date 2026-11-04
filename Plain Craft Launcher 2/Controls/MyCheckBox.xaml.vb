Imports System.Windows.Markup
Imports PCLLF.Presentation.Controls

<ContentProperty("Inlines")>
Public Class MyCheckBox
    Inherits MyCheckBoxView
    Implements ISettingControl

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub

    Public Shared Shadows ReadOnly TextProperty As DependencyProperty = MyCheckBoxView.TextProperty
    Public Shared Shadows ReadOnly CheckedProperty As DependencyProperty = MyCheckBoxView.CheckedProperty

    Public Shadows Event Change(sender As Object, user As Boolean)
    Public Shadows Event PreviewChange(sender As Object, e As RouteEventArgs)
    Protected Overrides Sub OnChange(user As Boolean)
        RaiseEvent Change(Me, user)
    End Sub
    Protected Overrides Function RaisePreviewChange(user As Boolean) As Boolean
        Dim e As New RouteEventArgs(user)
        RaiseEvent PreviewChange(Me, e)
        Return e.Handled
    End Function

    Private Shadows Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        MyBase.RefreshSetting(NewValue)
    End Sub
    Private Shadows Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return MyBase.GetCurrentSetting()
    End Function
    Private Sub SaveSetting() Handles Me.Change
        SettingService.SaveSetting(Me)
    End Sub
End Class
