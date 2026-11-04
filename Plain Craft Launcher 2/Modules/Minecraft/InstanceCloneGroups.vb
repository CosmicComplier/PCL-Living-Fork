Friend NotInheritable Class InstanceCloneGroupNode
    Inherits PCLCS.InstanceCloneGroupNodeData(Of InstanceCloneGroupNode)
End Class

Friend NotInheritable Class InstanceCloneGroupSet
    Inherits PCLCS.InstanceCloneGroupSetData(Of InstanceCloneGroupNode)
    Friend Function Find(VersionFolder As String) As InstanceCloneGroupNode
        Return InstanceCloneGroups.Service.Find(Me, VersionFolder)
    End Function
End Class

'只为显示解析来源关系；迁移仍必须通过 InstanceCloner 自身的完整校验。
Friend NotInheritable Class InstanceCloneGroups
    Friend Shared ReadOnly Service As New PCLCS.InstanceCloneGroupsEngine(Of InstanceCloneGroupNode, InstanceCloneGroupSet)(
        AddressOf PathUtils.RemoveExtendedPrefix, AddressOf InstanceCloner.EnsureNoLinks,
        Function() New InstanceCloneGroupNode, Function() New InstanceCloneGroupSet)
    Friend Shared Function Key(VersionFolder As String) As String
        Return Service.Key(VersionFolder)
    End Function
    Friend Shared Function Build(VisibleFolders As IEnumerable(Of String), Token As CancellationToken) As InstanceCloneGroupSet
        Return Service.Build(VisibleFolders, Token)
    End Function
    Friend Shared Function Flatten(Node As InstanceCloneGroupNode) As List(Of InstanceCloneGroupNode)
        Return Service.Flatten(Node)
    End Function
    Friend Shared Function Contains(Node As InstanceCloneGroupNode, VersionFolder As String) As Boolean
        Return Service.Contains(Node, VersionFolder)
    End Function
End Class
