Imports PCLLF.Presentation.Controls

Public Class MyIconButton
    Inherits MyIconButtonView

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shadows Enum Themes
        Color
        White
        Black
        Red
        Custom
    End Enum
    Public Shadows Property Theme As Themes
        Get
            Return CType(CInt(MyBase.Theme), Themes)
        End Get
        Set(value As Themes)
            MyBase.Theme = CType(CInt(value), MyIconButtonView.Themes)
        End Set
    End Property
    Protected Overrides Sub WithAnimationSuppressed(Work As Action)
        AniControlEnabled += 1
        Work()
        AniControlEnabled -= 1
    End Sub
    Public Shadows Event Click(sender As Object, e As EventArgs)
    Protected Overrides Sub OnClick(sender As Object, e As EventArgs)
        RaiseEvent Click(sender, e)
    End Sub
End Class

Partial Public Module ModAnimation
    Public Sub AniDispose(Control As MyIconButton, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        Control.DisposeAnimated(RemoveFromChildren, If(CallBack Is Nothing, Nothing, New Action(Of Object)(Sub(Value) CallBack(Value))))
    End Sub
End Module
