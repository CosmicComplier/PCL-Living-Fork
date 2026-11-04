Option Strict On
Imports PCLLF.Presentation.Controls

Public Class MyComboBoxItem
    Inherits MyComboBoxItemView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Overrides Function Tostring() As String
        Return MyBase.ToString()
    End Function
    Public Shared Overloads Widening Operator CType(Value As MyComboBoxItem) As String
        Return Value.Content.ToString()
    End Operator
End Class
