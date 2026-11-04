Imports PCLCS
Imports PCLLF.Presentation.Controls

' Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
Public Class PageOtherHelp
    Implements IRefreshable
    Private _controller As LegacyAuxiliaryHelpController(Of HelpEntry,CustomEvent.EventType)
    Friend ReadOnly Property Controller As LegacyAuxiliaryHelpController(Of HelpEntry,CustomEvent.EventType)
        Get
            If _controller Is Nothing Then _controller=New LegacyAuxiliaryHelpController(Of HelpEntry,CustomEvent.EventType)(AuxiliaryPageRuntime.Help,New LegacyAuxiliaryHelpView With {
.Page=Function() Me,.Scroll=Function() PanBack,.Loading=Function() Load,.LoadingPanel=Function() PanLoad,.List=Function() PanList,.SearchList=Function() PanSearchList,.SearchCard=Function() PanSearch,.Query=Function() SearchBox.Text})
            Return _controller
        End Get
    End Property

    Private Sub PageOther_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub

    Private Sub PageOther_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        Controller.Initialized()
    End Sub

    Private Sub HelpListLoad(Loader As LoaderTask(Of Integer, List(Of HelpEntry)))
        Controller.HelpListLoad(Loader.Output)
    End Sub

    Public Shared Sub OnItemClick(Entry As HelpEntry)
        AuxiliaryPageRuntime.Help.OnItemClick(Entry)
    End Sub

    Public Shared Sub EnterHelpPage(Location As String)
        AuxiliaryPageRuntime.Help.EnterHelpPage(Location)
    End Sub

    Public Shared Sub EnterHelpPage(Entry As HelpEntry)
        AuxiliaryPageRuntime.Help.EnterHelpPage(Entry)
    End Sub

    Public Sub SearchRun() Handles SearchBox.TextChanged
        Controller.SearchRun()
    End Sub

    Public Sub Refresh() Implements IRefreshable.Refresh
        Controller.Refresh()
    End Sub

End Class
