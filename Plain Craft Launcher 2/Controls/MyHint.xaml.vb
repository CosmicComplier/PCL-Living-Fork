Imports System.Windows.Markup
Imports PCLLF.Presentation.Controls

<ContentProperty("Inlines")>
Public Class MyHint
    Inherits MyHintView

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly IsWarnProperty As DependencyProperty = MyHintView.IsWarnProperty
    Public Shared Shadows ReadOnly TextProperty As DependencyProperty = MyHintView.TextProperty
    Public Shadows Enum Themes
        Blue = 0
        Red = 1
        Yellow = 2
    End Enum
    Public Shadows Property Theme As Themes
        Get
            Return CType(CInt(MyBase.Theme), Themes)
        End Get
        Set(value As Themes)
            MyBase.Theme = CType(CInt(value), MyHintView.Themes)
        End Set
    End Property
    Protected Overrides Function WpfSize(Pixels As Double) As Double
        Return GetWPFSize(Pixels)
    End Function
    Protected Overrides Function ReadDismissed(Key As String) As Boolean
        Return Settings.Get(Of Boolean)(Key)
    End Function
    Protected Overrides Sub WriteDismissed(Key As String)
        Settings.SetSafe(Key, True)
    End Sub
    Public Shadows Event Click(sender As Object, e As MouseButtonEventArgs)
    Protected Overrides Sub OnClick(sender As Object, Args As MouseButtonEventArgs)
        RaiseEvent Click(sender, Args)
    End Sub
End Class

Partial Public Module ModAnimation
    Public Sub AniDispose(Control As MyHint, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        Control.DisposeAnimated(RemoveFromChildren, If(CallBack Is Nothing, Nothing, New Action(Of Object)(Sub(Value) CallBack(Value))))
    End Sub
End Module
