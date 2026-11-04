Imports System.Windows.Markup
Imports PCLLF.Presentation.Controls

<ContentProperty("Inlines")>
Public Class MyRadioButton
    Inherits MyRadioButtonView

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub

    Public Shared Shadows ReadOnly TextProperty As DependencyProperty = MyRadioButtonView.TextProperty

    Public Shadows Enum ColorState
        White = 0
        Highlight = 1
    End Enum
    Public Shadows Property ColorType As ColorState
        Get
            Return CType(CInt(MyBase.ColorType), ColorState)
        End Get
        Set(value As ColorState)
            MyBase.ColorType = CType(CInt(value), MyRadioButtonView.ColorState)
        End Set
    End Property

    Public Shadows Event Check(sender As Object, raiseByMouse As Boolean)
    Public Shadows Event PreviewClick(sender As Object, e As RouteEventArgs)
    Protected Overrides Sub OnCheck(raiseByMouse As Boolean)
        RaiseEvent Check(Me, raiseByMouse)
    End Sub
    Protected Overrides Function RaisePreviewClick(user As Boolean) As Boolean
        Dim e As New RouteEventArgs(user)
        RaiseEvent PreviewClick(Me, e)
        Return e.Handled
    End Function
End Class
