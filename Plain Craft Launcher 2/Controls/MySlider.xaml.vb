Imports PCLLF.Presentation.Controls

Public Class MySlider
    Inherits MySliderView
    Implements ISettingControl

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shadows Event Change(sender As Object, user As Boolean)
    Public Shadows Event PreviewChange(sender As Object, e As RouteEventArgs)
    Protected Overrides Sub OnChange(User As Boolean)
        RaiseEvent Change(Me, User)
    End Sub
    Protected Overrides Function OnPreviewChange() As Boolean
        Dim e As New RouteEventArgs(False)
        RaiseEvent PreviewChange(Me, e)
        Return e.Handled
    End Function
    Protected Overrides ReadOnly Property CurrentDragControl As Object
        Get
            Return DragControl
        End Get
    End Property
    Protected Overrides Sub SetDragControl(Value As Object)
        DragControl = Value
    End Sub
    Protected Overrides Sub PerformMainDrag()
        FrmMain.DragDoing()
    End Sub
    Protected Overrides ReadOnly Property PopupDpi As Double
        Get
            Return DPI
        End Get
    End Property
    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Value = CInt(Val(NewValue))
    End Sub
    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return CStr(Value)
    End Function
    Private Sub SaveSetting() Handles Me.Change
        SettingService.SaveSetting(Me)
    End Sub
End Class
