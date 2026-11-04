Imports System.Windows.Markup
Imports PCLLF.Presentation.Controls

<ContentProperty("Inlines")>
Public Class MyRadioBox
    Inherits MyRadioBoxView
    Implements IMyRadio, ISettingControl

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly CheckedProperty As DependencyProperty = MyRadioBoxView.CheckedProperty
    Public Shared Shadows ReadOnly TextProperty As DependencyProperty = MyRadioBoxView.TextProperty
    Public Shadows Event PreviewCheck(sender As Object, e As RouteEventArgs)
    Public Shadows Event PreviewChange(sender As Object, e As RouteEventArgs)
    Public Shadows Event Check(sender As Object, e As RouteEventArgs) Implements IMyRadio.Check
    Public Shadows Event Changed(sender As Object, e As RouteEventArgs) Implements IMyRadio.Changed
    Protected Overrides Function OnPreviewCheck(User As Boolean) As Boolean
        Dim e As New RouteEventArgs(User)
        RaiseEvent PreviewCheck(Me, e)
        Return e.Handled
    End Function
    Protected Overrides Sub OnPreviewChange(User As Boolean)
        RaiseEvent PreviewChange(Me, New RouteEventArgs(User))
    End Sub
    Protected Overrides Sub OnCheck(User As Boolean)
        RaiseEvent Check(Me, New RouteEventArgs(User))
    End Sub
    Protected Overrides Sub OnChanged(User As Boolean)
        RaiseEvent Changed(Me, New RouteEventArgs(User))
    End Sub
    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Checked = NewValue = SettingService.GetValue(Me)
    End Sub
    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return If(Checked, SettingService.GetValue(Me), Nothing)
    End Function
    Private Sub SaveSetting() Handles Me.Check
        SettingService.SaveSetting(Me)
    End Sub
End Class
