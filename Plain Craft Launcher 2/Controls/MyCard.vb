Imports PCLLF.Presentation.Controls

Public Class MyCard
    Inherits MyCardView

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly TitleProperty As DependencyProperty = MyCardView.TitleProperty
    Public Shared Shadows ReadOnly IsSwapedProperty As DependencyProperty = MyCardView.IsSwapedProperty
    Public Shadows ReadOnly Property MainChrome As MyDropShadow
        Get
            Return DirectCast(MyBase.MainChrome, MyDropShadow)
        End Get
    End Property
    Protected Overrides Function CreateChrome() As MyDropShadowView
        Return New MyDropShadow
    End Function
    Protected Overrides Function WpfSize(Pixels As Double) As Double
        Return GetWPFSize(Pixels)
    End Function
    Protected Overrides ReadOnly Property HasMainWindow As Boolean
        Get
            Return FrmMain IsNot Nothing
        End Get
    End Property
    Protected Overrides Sub PostLoaded(Work As Action)
        RunInUi(Work, True)
    End Sub
    Public Shadows Event PreviewSwap(sender As Object, e As RouteEventArgs)
    Public Shadows Event Swap(sender As Object, e As RouteEventArgs)
    Protected Overrides Function CreateSwapArgs() As Object
        Return New RouteEventArgs(True)
    End Function
    Protected Overrides Function OnPreviewSwap(Args As Object) As Boolean
        Dim e = DirectCast(Args, RouteEventArgs)
        RaiseEvent PreviewSwap(Me, e)
        Return e.Handled
    End Function
    Protected Overrides Sub OnSwap(Args As Object)
        RaiseEvent Swap(Me, DirectCast(Args, RouteEventArgs))
    End Sub
End Class

Partial Public Module ModAnimation
    Public Sub AniDispose(Control As MyCard, RemoveFromChildren As Boolean, Optional CallBack As ParameterizedThreadStart = Nothing)
        Control.DisposeAnimated(RemoveFromChildren, If(CallBack Is Nothing, Nothing, New Action(Of Object)(Sub(Value) CallBack(Value))))
    End Sub
End Module
