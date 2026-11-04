Imports System.Windows.Markup
Imports PCLLF.Presentation.Controls

<ContentProperty("Inlines")>
Public Class MyButton
    Inherits MyButtonView

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub

    Public Shared Shadows ReadOnly TextProperty As DependencyProperty = MyButtonView.TextProperty
    Public Shared Shadows ReadOnly PaddingProperty As DependencyProperty = MyButtonView.PaddingProperty

    '保留原嵌套类型名；实际颜色状态只存放在 C# 基类。
    Public Shadows Enum ColorState
        Normal = 0
        Highlight = 1
        Red = 2
    End Enum
    Public Shadows Property ColorType As ColorState
        Get
            Return CType(CInt(MyBase.ColorType), ColorState)
        End Get
        Set(value As ColorState)
            MyBase.ColorType = CType(CInt(value), MyButtonView.ColorState)
        End Set
    End Property

    Public Shadows Event Click(sender As Object, e As MouseButtonEventArgs)
    Protected Overrides Sub OnClick(sender As Object, args As MouseButtonEventArgs)
        RaiseEvent Click(sender, args)
    End Sub
End Class
