Imports PCL.MyLoading
Imports PCLLF.Presentation.Controls
Imports Microsoft.VisualBasic.CompilerServices

Public Class MyLoading
    Inherits MyLoadingView

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly ForegroundProperty As DependencyProperty = MyLoadingView.ForegroundProperty
    Public Shadows Enum MyLoadingState
        Unloaded = -1
        Run = 0
        [Stop] = 1
        [Error] = 2
    End Enum
    Public Shadows Event IsErrorChanged(sender As Object, isError As Boolean)
    Public Shadows Event StateChanged(sender As Object, newState As MyLoadingState, oldState As MyLoadingState)
    Public Shadows Event Click(sender As Object, e As MouseButtonEventArgs)
    Public Shadows Property State As ILoadingTrigger
        Get
            Return DirectCast(MyBase.State, LegacyTriggerAdapter).Original
        End Get
        Set(value As ILoadingTrigger)
            MyBase.State = If(value Is Nothing, Nothing, New LegacyTriggerAdapter(value))
        End Set
    End Property
    Protected Overrides Function CreateDefaultState() As ILoadingViewTrigger
        Return New LegacyTriggerAdapter(New MyLoadingStateSimulator)
    End Function
    Protected Overrides Sub UiPost(Work As Action)
        RunInUi(Work)
    End Sub
    Protected Overrides Function DisplayError(ErrorValue As Exception) As String
        Return ErrorValue.GetDisplay(False)
    End Function
    Protected Overrides Sub OnStateChanged(NextState As MyLoadingView.MyLoadingState, OldState As MyLoadingView.MyLoadingState)
        RaiseEvent StateChanged(Me, CType(CInt(NextState), MyLoadingState), CType(CInt(OldState), MyLoadingState))
    End Sub
    Protected Overrides Sub OnIsErrorChanged(IsError As Boolean)
        RaiseEvent IsErrorChanged(Me, IsError)
    End Sub
    Protected Overrides Sub OnClick(sender As Object, Args As MouseButtonEventArgs)
        RaiseEvent Click(sender, Args)
    End Sub

    Private NotInheritable Class LegacyTriggerAdapter
        Implements ILoadingViewTrigger
        Public ReadOnly Original As ILoadingTrigger
        Public Sub New(Value As ILoadingTrigger)
            Original = Value
        End Sub
        Public ReadOnly Property IsLoader As Boolean Implements ILoadingViewTrigger.IsLoader
            Get
                Return Original.IsLoader
            End Get
        End Property
        Public Property LoadingState As MyLoadingView.MyLoadingState Implements ILoadingViewTrigger.LoadingState
            Get
                Return CType(CInt(Original.LoadingState), MyLoadingView.MyLoadingState)
            End Get
            Set(value As MyLoadingView.MyLoadingState)
                Original.LoadingState = CType(CInt(value), MyLoadingState)
            End Set
        End Property
        Public ReadOnly Property [Error] As Exception Implements ILoadingViewTrigger.Error
            Get
                Return DirectCast(NewLateBinding.LateGet(Original, Nothing, "Error", {}, Nothing, Nothing, Nothing), Exception)
            End Get
        End Property
        Public ReadOnly Property Progress As Double Implements ILoadingViewTrigger.Progress
            Get
                Return Conversions.ToDouble(NewLateBinding.LateGet(Original, Nothing, "Progress", {}, Nothing, Nothing, Nothing))
            End Get
        End Property
        Public Function Subscribe(StateCallback As Action(Of MyLoadingView.MyLoadingState, MyLoadingView.MyLoadingState), ProgressCallback As Action(Of Double, Double)) As IDisposable Implements ILoadingViewTrigger.Subscribe
            Dim StateHandler As ILoadingTrigger.LoadingStateChangedEventHandler = Sub(NextState, OldState) StateCallback(CType(CInt(NextState), MyLoadingView.MyLoadingState), CType(CInt(OldState), MyLoadingView.MyLoadingState))
            Dim ProgressHandler As ILoadingTrigger.ProgressChangedEventHandler = Sub(NextValue, OldValue) ProgressCallback(NextValue, OldValue)
            AddHandler Original.LoadingStateChanged, StateHandler
            AddHandler Original.ProgressChanged, ProgressHandler
            Return New LoadingTriggerSubscription(Sub()
                                                      RemoveHandler Original.LoadingStateChanged, StateHandler
                                                      RemoveHandler Original.ProgressChanged, ProgressHandler
                                                  End Sub)
        End Function
    End Class
End Class

Public Interface ILoadingTrigger
    ReadOnly Property IsLoader As Boolean
    Property LoadingState As MyLoadingState
    Event LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState)
    Event ProgressChanged(NewProgress As Double, OldProgress As Double)
End Interface

Public Class MyLoadingStateSimulator
    Inherits LoadingStateSimulatorView
    Implements ILoadingTrigger
    Public Shadows Property LoadingState As MyLoadingState Implements ILoadingTrigger.LoadingState
        Get
            Return CType(CInt(MyBase.LoadingState), MyLoadingState)
        End Get
        Set(value As MyLoadingState)
            MyBase.LoadingState = CType(CInt(value), MyLoadingView.MyLoadingState)
        End Set
    End Property
    Public Shadows ReadOnly Property IsLoader As Boolean Implements ILoadingTrigger.IsLoader
        Get
            Return MyBase.IsLoader
        End Get
    End Property
    Public Event LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState) Implements ILoadingTrigger.LoadingStateChanged
    Public Event ProgressChanged(NewProgress As Double, OldProgress As Double) Implements ILoadingTrigger.ProgressChanged
    Protected Overrides Sub OnStateChanged(NextState As MyLoadingView.MyLoadingState, OldState As MyLoadingView.MyLoadingState)
        RaiseEvent LoadingStateChanged(CType(CInt(NextState), MyLoadingState), CType(CInt(OldState), MyLoadingState))
    End Sub
End Class
