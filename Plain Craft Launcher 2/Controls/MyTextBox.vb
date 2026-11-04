Imports PCLLF.Presentation.Controls
Imports PCLCS.Validation

Public Class MyTextBox
    Inherits MyTextBoxView
    Implements ISettingControl

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly CornerRadiusProperty As DependencyProperty = MyTextBoxView.CornerRadiusProperty
    Public Shared Shadows ReadOnly ValidateResultProperty As DependencyProperty = MyTextBoxView.ValidateResultProperty
    Public Shared Shadows ReadOnly IsValidatedProperty As DependencyProperty = MyTextBoxView.IsValidatedProperty
    Public Shared Shadows ReadOnly HintTextProperty As DependencyProperty = MyTextBoxView.HintTextProperty
    Public Shared Shadows Event ValidateChanged(sender As Object, e As EventArgs)
    Protected Overrides Sub OnValidateChanged()
        RaiseEvent ValidateChanged(Me, New EventArgs)
    End Sub
    Private _ValidateRules As New ObjectModel.Collection(Of Validate)
    Public Shadows Property ValidateRules As ObjectModel.Collection(Of Validate)
        Get
            Return _ValidateRules
        End Get
        Set(Value As ObjectModel.Collection(Of Validate))
            _ValidateRules = Value
            Validate()
        End Set
    End Property
    Protected Overrides Function ReadValidateRules() As IEnumerable(Of LegacyValidationRule)
        Return _ValidateRules
    End Function
    Public Shadows Custom Event ValidatedTextChanged As RoutedEventHandler
        AddHandler(value As RoutedEventHandler)
            ChangedEventList.Add(value)
        End AddHandler
        RemoveHandler(value As RoutedEventHandler)
            ChangedEventList.Remove(value)
        End RemoveHandler
        RaiseEvent(sender As Object, e As RoutedEventArgs)
            For Each Handler As RoutedEventHandler In ChangedEventList
                If Handler IsNot Nothing Then Handler(sender, e)
            Next
        End RaiseEvent
    End Event
    Protected Overrides Sub UiPost(Work As Action)
        RunInUi(Work)
    End Sub
    Protected Overrides Sub UiWait(Work As Action)
        RunInUiWait(Work)
    End Sub
    Protected Overrides Sub StartWorker(Work As Action, Name As String)
        RunInNewThread(Work, Name)
    End Sub
    Protected Overrides Sub ReportError(ErrorValue As Exception, Message As String)
        Logger.Error(ErrorValue, Message)
    End Sub
    Protected Overrides Function ReadTextColor(Enabled As Boolean) As ControlColor
        Dim Value As MyColor = If(Enabled, ColorGray1, ColorGray4)
        Return New ControlColor(Value.A, Value.R, Value.G, Value.B)
    End Function
    Private Sub RefreshSetting(NewValue As String) Implements ISettingControl.RefreshSetting
        Text = NewValue
    End Sub
    Private Function GetCurrentSetting() As String Implements ISettingControl.GetCurrentSetting
        Return Text
    End Function
    Private Sub SaveSetting() Handles Me.ValidatedTextChanged
        SettingService.SaveSetting(Me)
    End Sub
End Class
