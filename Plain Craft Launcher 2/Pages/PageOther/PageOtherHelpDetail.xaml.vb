Imports PCLCS
Imports PCLLF.Presentation.Controls

' Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
Public Class PageOtherHelpDetail
    Implements IRefreshable
    Public Entry As HelpEntry
    Private _controller As LegacyAuxiliaryHelpDetailController(Of HelpEntry,CustomEvent.EventType)
    Friend ReadOnly Property Controller As LegacyAuxiliaryHelpDetailController(Of HelpEntry,CustomEvent.EventType)
        Get
            If _controller Is Nothing Then _controller=New LegacyAuxiliaryHelpDetailController(Of HelpEntry,CustomEvent.EventType)(AuxiliaryPageRuntime.Help.Host,New LegacyAuxiliaryHelpDetailView(Of HelpEntry) With {
.Page=Function() Me,.Entry=Function() Entry,.SetEntry=Sub(Value) Entry=Value,.Scroll=Function() PanBack,.Content=Function() PanCustom})
            Return _controller
        End Get
    End Property

    Public Sub Refresh() Implements IRefreshable.Refresh
        Controller.Refresh()
    End Sub

    Private Sub PageOtherHelpDetail_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub

    Public Function Init(Entry As HelpEntry) As Boolean
        Return Controller.Init(Entry)
    End Function

End Class
