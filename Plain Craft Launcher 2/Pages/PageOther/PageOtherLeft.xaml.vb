Imports PCLCS
Imports PCLLF.Presentation.Controls

' Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
Public Class PageOtherLeft
    Public PageID As FormMain.PageSubType
    Private _controller As LegacyAuxiliaryNavigationController
    Friend ReadOnly Property Controller As LegacyAuxiliaryNavigationController
        Get
            If _controller Is Nothing Then
                _controller = New LegacyAuxiliaryNavigationController(AuxiliaryPageRuntime.NavigationHost(Function() CInt(PageID),Sub(Value) PageID=CType(Value,FormMain.PageSubType)),New LegacyAuxiliaryNavigationView With {
                    .Help=Function() ItemHelp,
                    .About=Function() ItemAbout,
                    .Test=Function() ItemTest
                },False)
            End If
            Return _controller
        End Get
    End Property

    Private Sub PageOtherLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub

    Private Sub PageOtherLeft_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        Controller.Unloaded()
    End Sub

    Public Sub New()
        InitializeComponent()
        Controller.InitializePageId()
    End Sub

    Private Sub PageCheck(sender As FrameworkElement, e As RouteEventArgs) Handles ItemAbout.Check, ItemHelp.Check, ItemTest.Check
        Controller.PageCheck(sender)
    End Sub

    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        Return DirectCast(Controller.PageGet(CInt(ID)), MyPageRight)
    End Function

    Public Sub PageChange(ID As FormMain.PageSubType)
        Controller.PageChange(CInt(ID))
    End Sub

    Private Shared Sub PageChangeRun(Target As MyPageRight)
        LegacyAuxiliaryNavigationController.PageChangeRun(AuxiliaryPageRuntime.NavigationHost(Nothing, Nothing),Target)
    End Sub

    Public Sub Refresh(sender As Object, e As EventArgs) '由边栏按钮匿名调用
        Controller.Refresh(sender)
    End Sub

    Public Shared Sub RefreshHelp()
        AuxiliaryPageRuntime.RefreshHelp()
    End Sub

End Class
