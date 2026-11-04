Option Strict On
Imports PCLLF.Presentation.Controls
Imports Microsoft.VisualBasic.CompilerServices

Public Class MyPageRight
    Inherits MyPageRightView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Private Shared Shadows ReadOnly PanScrollProperty As DependencyProperty = MyPageRightView.PanScrollProperty
    Public Shadows Enum PageStates
        Empty
        LoaderWait
        LoaderEnter
        LoaderStayForce
        LoaderStay
        LoaderExit
        ContentEnter
        ContentStay
        ContentExit
        PageExit
    End Enum
    Public Shadows Property PageState As PageStates
        Get
            Return CType(CInt(MyBase.PageState), PageStates)
        End Get
        Set(value As PageStates)
            MyBase.PageState = CType(CInt(value), MyPageRightView.PageStates)
        End Set
    End Property
    Public Shadows Event PageEnter()
    Public Shadows Event PageExit()
    Protected Overrides Sub OnPageEnter()
        RaiseEvent PageEnter()
    End Sub
    Protected Overrides Sub OnPageExit()
        RaiseEvent PageExit()
    End Sub
    Protected Overrides Sub UiPost(Work As Action)
        RunInUi(Work)
    End Sub
    Protected Overrides Sub UiWait(Work As Action)
        RunInUiWait(Work)
    End Sub
    Public Sub PageLoaderInit(Of TIn, TOut)(LoaderUi As MyLoading, PanLoader As FrameworkElement, PanContent As FrameworkElement, PanAlways As FrameworkElement,
                                          RealLoader As LoaderTask(Of TIn, TOut), Optional FinishedInvoke As Action(Of LoaderTask(Of TIn, TOut)) = Nothing,
                                          Optional InputInvoke As Func(Of TIn) = Nothing, Optional AutoRun As Boolean = True)
        Dim Binding As New PageLoaderBinding With {
            .Original = RealLoader, .ReadState = Function() CType(CInt(RealLoader.State), PageLoaderState),
            .Start = Sub(Input, Force) RealLoader.Start(Input, IsForceRestart:=Force),
            .UsesStartGetInput = True, .InitialInput = Function() RealLoader.StartGetInput(Nothing, InputInvoke),
            .StartGetInput = Function(Input) NewLateBinding.LateGet(RealLoader, Nothing, "StartGetInput", {Input, InputInvoke}, Nothing, Nothing, Nothing),
            .InputInvoke = If(InputInvoke Is Nothing, Nothing, New Func(Of Object)(Function() InputInvoke())),
            .Finished = If(FinishedInvoke Is Nothing, Nothing, New Action(Sub() FinishedInvoke(RealLoader))),
            .ConnectPreviewFinish = Sub(Callback) AddHandler RealLoader.PreviewFinish, Sub() Callback(),
            .ConnectState = Sub(Callback) AddHandler RealLoader.OnStateChangedUi, Sub(Loader, NextState, OldState) Callback(CType(CInt(NextState), PageLoaderState), CType(CInt(OldState), PageLoaderState)),
            .SetLoadingState = Sub() LoaderUi.State = RealLoader,
            .ConnectClick = Sub(Callback) AddHandler LoaderUi.Click, Sub() Callback()
        }
        InitializeLoader(Binding, PanLoader, PanContent, PanAlways, AutoRun)
    End Sub
    Public Sub PageLoaderInit(Of TIn)(LoaderUi As MyLoading, PanLoader As FrameworkElement, PanContent As FrameworkElement, PanAlways As FrameworkElement,
                                    RealLoader As LoaderCombo(Of TIn), Optional FinishedInvoke As Action(Of LoaderCombo(Of TIn)) = Nothing,
                                    Optional InputInvoke As Func(Of TIn) = Nothing, Optional AutoRun As Boolean = True)
        Dim Binding As New PageLoaderBinding With {
            .Original = RealLoader, .ReadState = Function() CType(CInt(RealLoader.State), PageLoaderState),
            .Start = Sub(Input, Force) RealLoader.Start(Input, IsForceRestart:=Force),
            .UsesStartGetInput = False, .InputInvoke = If(InputInvoke Is Nothing, Nothing, New Func(Of Object)(Function() InputInvoke())),
            .Finished = If(FinishedInvoke Is Nothing, Nothing, New Action(Sub() FinishedInvoke(RealLoader))),
            .ConnectPreviewFinish = Sub(Callback) AddHandler RealLoader.PreviewFinish, Sub() Callback(),
            .ConnectState = Sub(Callback) AddHandler RealLoader.OnStateChangedUi, Sub(Loader, NextState, OldState) Callback(CType(CInt(NextState), PageLoaderState), CType(CInt(OldState), PageLoaderState)),
            .SetLoadingState = Sub() LoaderUi.State = RealLoader,
            .ConnectClick = Sub(Callback) AddHandler LoaderUi.Click, Sub() Callback()
        }
        InitializeLoader(Binding, PanLoader, PanContent, PanAlways, AutoRun)
    End Sub
    Public Shadows Sub PageLoaderRestart(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = True)
        MyBase.PageLoaderRestart(Input, IsForceRestart)
    End Sub
    Public Shadows Sub TriggerEnterAnimation(ParamArray Elements As FrameworkElement())
        MyBase.TriggerEnterAnimation(Elements)
    End Sub
    Public Shadows Sub TriggerExitAnimation(ParamArray Elements As FrameworkElement())
        MyBase.TriggerExitAnimation(Elements)
    End Sub
    Friend Shadows Function GetAllAnimControls(Element As FrameworkElement, Optional IgnoreInvisibility As Boolean = False) As IEnumerable(Of FrameworkElement)
        Return MyBase.GetAllAnimControls(Element, IgnoreInvisibility)
    End Function
End Class
