Friend NotInheritable Class RecentEnvironmentChange
    Inherits PCLCS.RecentEnvironmentChangeData
End Class

Friend NotInheritable Class RecentEnvironmentChangesReport
    Inherits PCLCS.RecentEnvironmentChangesReportData(Of RecentEnvironmentChange)
End Class

'只对比最近正常运行的环境清单。存档、截图、账号、地图标记不属于崩溃变更对照范围。
Friend NotInheritable Class RecentEnvironmentChanges
    Private Shared ReadOnly Service As New PCLCS.RecentEnvironmentChangesEngine(Of McInstance, RecentEnvironmentChange, RecentEnvironmentChangesReport)(
        EnvironmentHistory.Service, CreateHost(), Function() New RecentEnvironmentChange, Function() New RecentEnvironmentChangesReport)
    Private Shared Function CreateHost() As PCLCS.RecentEnvironmentHost(Of McInstance)
        Return New PCLCS.RecentEnvironmentHost(Of McInstance) With {
            .NormalizeRoot = AddressOf InstanceCloner.NormalizeRoot,
            .RelativePath = AddressOf ModpackUpdate.RelativePath,
            .EnsureNoLinks = AddressOf InstanceCloner.EnsureNoLinks,
            .EnsureWithin = Sub(Root, Address) InstanceResourceStore.EnsureWithin(Root, Address),
            .Target = AddressOf ModpackUpdate.Target, .Digest = AddressOf InstanceCloner.Digest,
            .SamePath = Function(Left, Right) PathUtils.IsSame(Left, Right),
            .VersionPath = Function(Instance) Instance.PathVersion,
            .GameRootForInstance = AddressOf InstanceCloner.GameRootForInstance}
    End Function
    Friend Shared Function BuildInstance(Instance As McInstance, Token As CancellationToken) As RecentEnvironmentChangesReport
        Return Service.BuildInstance(Instance, Token)
    End Function
    Friend Shared Function Build(VersionFolder As String, GameRoot As String, Token As CancellationToken) As RecentEnvironmentChangesReport
        Return Service.Build(VersionFolder, GameRoot, Token)
    End Function
    Friend Shared Function AppendAnalysis(Analysis As String, Report As RecentEnvironmentChangesReport) As String
        Return Service.AppendAnalysis(Analysis, Report)
    End Function
End Class
