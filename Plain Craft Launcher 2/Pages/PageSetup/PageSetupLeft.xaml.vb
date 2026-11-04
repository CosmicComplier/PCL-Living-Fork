Imports PCLCS
Imports PCLLF.Presentation.Controls

' Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
Public Class PageSetupLeft
    Public PageID As FormMain.PageSubType
    Private _controller As LegacyAuxiliaryNavigationController
    Friend ReadOnly Property Controller As LegacyAuxiliaryNavigationController
        Get
            If _controller Is Nothing Then
                _controller = New LegacyAuxiliaryNavigationController(AuxiliaryPageRuntime.NavigationHost(Function() CInt(PageID),Sub(Value) PageID=CType(Value,FormMain.PageSubType)),New LegacyAuxiliaryNavigationView With {
                    .Launch=Function() ItemLaunch,
                    .UI=Function() ItemUI,
                    .Link=Function() ItemLink,
                    .System=Function() ItemSystem
                },True)
            End If
            Return _controller
        End Get
    End Property

    Private Sub PageSetupLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub

    Private Sub PageOtherLeft_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        Controller.Unloaded()
    End Sub

    Public Sub New()
        InitializeComponent()
        Controller.InitializePageId()
    End Sub

    Private Sub PageCheck(sender As MyListItem, e As EventArgs) Handles ItemLaunch.Check, ItemSystem.Check, ItemUI.Check, ItemLink.Check
        Controller.PageCheck(sender)
    End Sub

    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        Return DirectCast(Controller.PageGet(CInt(ID)), MyPageRight)
    End Function

    Public Sub PageChange(ID As FormMain.PageSubType, Optional Force As Boolean = False)
        Controller.PageChange(CInt(ID), Force)
    End Sub

    Private Shared Sub PageChangeRun(Target As MyPageRight)
        LegacyAuxiliaryNavigationController.PageChangeRun(AuxiliaryPageRuntime.NavigationHost(Nothing, Nothing),Target)
    End Sub

    Public Sub Reset(sender As Object, e As EventArgs)
        Controller.Reset(sender)
    End Sub

End Class
