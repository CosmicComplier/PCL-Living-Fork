Imports PCLLF.Presentation.Controls

Public Class MyComboBox
    Inherits MyComboBoxView
    Implements ISettingControl

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shadows Event TextChanged(sender As Object, e As TextChangedEventArgs)
    Protected Overrides Sub OnTextChanged(sender As Object, Args As TextChangedEventArgs)
        RaiseEvent TextChanged(sender, Args)
    End Sub
    Protected Overrides ReadOnly Property MainWindowOpacity As Double
        Get
            Return FrmMain.Opacity
        End Get
    End Property
    Protected Overrides Sub ReportError(ErrorValue As Exception, Message As String)
        Logger.Error(ErrorValue, Message)
    End Sub
    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        RefreshSettingValue(NewValue, Function(Item) SettingService.GetValue(Item), Function() SettingService.GetKey(Me), Sub(Message) Logger.Warn(Message))
    End Sub
    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return CurrentSettingValue(Function(Item) SettingService.GetValue(Item))
    End Function
    Private Sub SaveSetting() Handles Me.SelectionChanged
        If SelectedItem Is Nothing Then Return
        SettingService.SaveSetting(Me)
    End Sub
End Class
