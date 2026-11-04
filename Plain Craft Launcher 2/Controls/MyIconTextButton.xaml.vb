Option Strict On
Imports System.Windows.Markup
Imports PCLLF.Presentation.Controls

<ContentProperty("Inlines")>
Public Class MyIconTextButton
    Inherits MyIconTextButtonView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly TextProperty As DependencyProperty = MyIconTextButtonView.TextProperty
    Public Shadows Enum ColorState
        Black
        Highlight
    End Enum
    Public Shared Shadows ReadOnly ColorTypeProperty As DependencyProperty = DependencyProperty.Register("ColorType", GetType(ColorState), GetType(MyIconTextButton), New PropertyMetadata(ColorState.Black))
    Public Shadows Property ColorType As ColorState
        Get
            Return CType(CInt(MyBase.ColorType), ColorState)
        End Get
        Set(value As ColorState)
            MyBase.ColorType = CType(CInt(value), MyIconTextButtonView.ColorState)
        End Set
    End Property
    Protected Overrides Function ReadColorType() As MyIconTextButtonView.ColorState
        Return CType(CInt(GetValue(ColorTypeProperty)), MyIconTextButtonView.ColorState)
    End Function
    Protected Overrides Sub WriteColorType(Value As MyIconTextButtonView.ColorState)
        SetValue(ColorTypeProperty, CType(CInt(Value), ColorState))
    End Sub
    Public Shadows Event Check(sender As Object, raiseByMouse As Boolean)
    Public Shadows Event Change(sender As Object, raiseByMouse As Boolean)
    Public Shadows Event Click(sender As Object, e As RouteEventArgs)
    Protected Overrides Sub OnClick()
        RaiseEvent Click(Me, New RouteEventArgs(True))
    End Sub
End Class
