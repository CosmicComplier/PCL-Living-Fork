Imports System.IO
Imports System.Threading

Friend NotInheritable Class ResourceRecycleStore
    Private Shared ReadOnly Files As New PCLCS.ResourceRecycleFiles(Of InstanceResourceEntry)(
        AddressOf PathUtils.RemoveExtendedPrefix, Function(Left, Right) PathUtils.IsSame(Left, Right),
        Function() New InstanceResourceEntry(), Sub(Item, FileName, Width) Item.Icon = InstanceResourceStore.LoadImage(FileName, Width))
    Friend Class Record
        Public Id As String
        Public Box As String
        Public Destination As String
        Public Entry As InstanceResourceEntry
    End Class

    Private Shared Function ReadRecord(Root As String, Recycle As String, Id As String) As Record
        Dim Value = Files.ReadRecord(Root, Recycle, Id)
        Return New Record With {.Id = Value.Id, .Box = Value.Box, .Destination = Value.Destination, .Entry = Value.Entry}
    End Function

    Friend Shared Function List(Root As String, Recycle As String, Kind As InstanceResourceKind) As List(Of InstanceResourceEntry)
        Return Files.List(Root, Recycle, CInt(Kind))
    End Function

    Friend Shared Sub Restore(Root As String, Recycle As String, Ids As IEnumerable(Of String), Token As CancellationToken)
        Files.Restore(Root, Recycle, Ids, Token)
    End Sub


    Friend Shared Sub Purge(Root As String, Recycle As String, Ids As IEnumerable(Of String), Token As CancellationToken)
        Files.Purge(Root, Recycle, Ids, Token)
    End Sub
End Class
