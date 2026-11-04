Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading
Imports Newtonsoft.Json.Linq
Imports InspectionCore = PCLCS.ModDependencyInspection(Of PCL.ModDependencyInspector.InspectionContext, PCL.ModDependencyInspector.ModInfo, PCL.ModDependencyInspector.Dependency, PCL.ModDependencyInspector.DependencyEdge, PCL.ModDependencyInspector.InspectionIssue, PCL.ModDependencyInspector.RemovalImpact, PCL.ModDependencyInspector.InspectionReport, PCL.ModDependencyInspector.OnlineProjectInfo, PCL.ModDependencyInspector.OnlineDependency, PCL.ModDependencyInspector.OnlineFileInfo)

'原 LF 本地依赖类型与调用入口；声明解析、图、可靠度及版本规则由 C# 维护。
Friend NotInheritable Class ModDependencyInspector
    Private Sub New()
    End Sub

    Friend Enum MatchState
        Unknown
        Satisfied
        Unsatisfied
    End Enum

    Friend Enum FileProviderKind
        None
        Required
        Possible
    End Enum

    Friend Enum DependencyKind
        Required
        [Optional]
        Recommended
        Suggested
        Incompatible
        Conflict
    End Enum

    Friend Enum IssueKind
        MissingRequired
        DuplicateId
        VersionMismatch
        Incompatible
        Recommendation
        LoaderMismatch
    End Enum

    Friend Class InspectionContext
        Inherits InspectionCore.ContextData
    End Class

    Friend Class ModInfo
        Inherits InspectionCore.ModData
    End Class

    Friend Class Dependency
        Inherits InspectionCore.DependencyData

        Public Kind As DependencyKind
        <Newtonsoft.Json.JsonIgnore>
        Public Overrides Property KindValue As PCLCS.ModDependencyKind
            Get
                Return CType(CInt(Me.Kind), PCLCS.ModDependencyKind)
            End Get
            Set(Value As PCLCS.ModDependencyKind)
                Me.Kind = CType(CInt(Value), DependencyKind)
            End Set
        End Property
    End Class

    Friend Class DependencyEdge
        Inherits InspectionCore.EdgeData

        Public State As MatchState
        <Newtonsoft.Json.JsonIgnore>
        Public Overrides Property StateValue As PCLCS.ModDependencyMatchState
            Get
                Return CType(CInt(Me.State), PCLCS.ModDependencyMatchState)
            End Get
            Set(Value As PCLCS.ModDependencyMatchState)
                Me.State = CType(CInt(Value), MatchState)
            End Set
        End Property
    End Class

    Friend Class InspectionIssue
        Inherits InspectionCore.IssueData

        Public Kind As IssueKind
        <Newtonsoft.Json.JsonIgnore>
        Public Overrides Property KindValue As PCLCS.ModDependencyIssueKind
            Get
                Return CType(CInt(Me.Kind), PCLCS.ModDependencyIssueKind)
            End Get
            Set(Value As PCLCS.ModDependencyIssueKind)
                Me.Kind = CType(CInt(Value), IssueKind)
            End Set
        End Property
    End Class

    Friend Class RemovalImpact
        Inherits InspectionCore.ImpactData
    End Class

    Friend Class InspectionReport
        Inherits InspectionCore.ReportData
    End Class

    Friend Class OnlineProjectInfo
        Inherits InspectionCore.OnlineProjectData
    End Class

    Friend Class OnlineDependency
        Inherits InspectionCore.OnlineDependencyData
    End Class

    Friend Class OnlineFileInfo
        Inherits InspectionCore.OnlineFileData
    End Class

    Private Shared ReadOnly InstanceHost As New PCLCS.ModDependencyInstanceHost(Of McInstance) With {
        .Reliable = Function(Instance) Instance.Version.Reliable,
        .VanillaName = Function(Instance) Instance.Version.VanillaName,
        .ModLoaderVersions = Function(Instance) Instance.Version.ModLoaderVersions.Select(Function(Pair) New KeyValuePair(Of String, String)(Pair.Key.ToString(), Pair.Value)),
        .JsonObject = Function(Instance) Instance.JsonObject,
        .HasOptiFine = Function(Instance) Instance.Version.HasOptiFine,
        .OptiFineAsMod = Function(Instance) Instance.Version.OptiFineAsMod,
        .OptiFine = Function(Instance) Instance.Version.OptiFine}

    Friend Shared Function ContextForInstance(Instance As McInstance, Optional JavaVersion As String = Nothing) As InspectionContext
        Return InspectionCore.ContextForInstance(Instance, InstanceHost, JavaVersion)
    End Function
    Friend Shared Function FilePathKey(FullPath As String) As String
        Return InspectionCore.FilePathKey(FullPath)
    End Function
    Friend Shared Function SameFilePath(First As String, Second As String) As Boolean
        Return InspectionCore.SameFilePath(First, Second)
    End Function
    Friend Shared Function Inspect(ModsFolder As String, Optional Context As InspectionContext = Nothing,
                                   Optional Token As CancellationToken = Nothing) As InspectionReport
        Return InspectionCore.Inspect(ModsFolder, Context, Token)
    End Function
    Friend Shared Function Identities(Item As ModInfo) As Dictionary(Of String, String)
        Return InspectionCore.Identities(Item)
    End Function
    Friend Shared Function FileSatisfiesDependency(Report As InspectionReport, FullPath As String, Requirement As Dependency) As Boolean
        Return InspectionCore.FileSatisfiesDependency(Report, FullPath, Requirement)
    End Function
    Friend Shared Function WithEnabledFiles(Report As InspectionReport, Paths As IEnumerable(Of String), Token As CancellationToken) As InspectionReport
        Return InspectionCore.WithEnabledFiles(Report, Paths, Token)
    End Function
    Friend Shared Function IsRequiredFileProvider(Report As InspectionReport, Edge As DependencyEdge, FullPath As String) As Boolean
        Return InspectionCore.IsRequiredFileProvider(Report, Edge, FullPath)
    End Function
    Friend Shared Function FileProviderRelation(Report As InspectionReport, Edge As DependencyEdge, FullPath As String) As FileProviderKind
        Return CType(CInt(InspectionCore.FileProviderRelation(Report, Edge, FullPath)), FileProviderKind)
    End Function
    Friend Shared Sub BuildReport(Report As InspectionReport, Token As CancellationToken)
        InspectionCore.BuildReport(Report, Token)
    End Sub
    Friend Shared Function GetImpacts(Report As InspectionReport, RemovedPaths As IEnumerable(Of String)) As List(Of RemovalImpact)
        Return InspectionCore.GetImpacts(Report, RemovedPaths)
    End Function
    Friend Shared Function DescribeImpacts(Report As InspectionReport, RemovedPaths As IEnumerable(Of String)) As String
        Return InspectionCore.DescribeImpacts(Report, RemovedPaths)
    End Function
    Friend Shared Function DependencyMentions(Requirement As Dependency, Ids As IEnumerable(Of String)) As Boolean
        Return InspectionCore.DependencyMentions(Requirement, Ids)
    End Function
    Friend Shared Function MatchVersion(Actual As String, Requirement As String, Optional Syntax As String = "fabric") As MatchState
        Return CType(CInt(InspectionCore.MatchVersion(Actual, Requirement, Syntax)), MatchState)
    End Function
    Friend Shared Function RebuildIndex(ModsFolder As String, Optional Context As InspectionContext = Nothing, Optional Token As CancellationToken = Nothing) As InspectionReport
        Return InspectionCore.RebuildIndex(ModsFolder, Context, Token)
    End Function
    Friend Shared Function ScanRaw(ModsFolder As String, Context As InspectionContext, Token As CancellationToken, Optional ForceFresh As Boolean = False) As InspectionReport
        Return InspectionCore.ScanRaw(ModsFolder, Context, Token, ForceFresh)
    End Function
    Friend Shared Sub Reevaluate(Report As InspectionReport, Context As InspectionContext, Token As CancellationToken)
        InspectionCore.Reevaluate(Report, Context, Token)
    End Sub
    Friend Shared Function LoaderWarningFor(Folder As String, FullPath As String, Context As InspectionContext) As String
        Return InspectionCore.LoaderWarningFor(Folder, FullPath, Context)
    End Function
    Friend Shared Function IsModFile(FullPath As String) As Boolean
        Return InspectionCore.IsModFile(FullPath)
    End Function
    Friend Shared Function EmbeddedModsForDisplay(Report As InspectionReport, FullPath As String) As List(Of ModInfo)
        Return InspectionCore.EmbeddedModsForDisplay(Report, FullPath)
    End Function
    Friend Shared Function IsLegacyForge(Context As InspectionContext) As Boolean
        Return InspectionCore.IsLegacyForge(Context)
    End Function
    Friend Shared Function RuntimeOwnerId(Id As String, Context As InspectionContext) As String
        Return InspectionCore.RuntimeOwnerId(Id, Context)
    End Function
    Friend Shared Function IsInternalLoaderId(Id As String) As Boolean
        Return InspectionCore.IsInternalLoaderId(Id)
    End Function
    Friend Shared Function RuntimeDisplayName(Id As String) As String
        Return InspectionCore.RuntimeDisplayName(Id)
    End Function
    Friend Shared Function RequirementDisplay(Requirement As Dependency, Context As InspectionContext) As String
        Return InspectionCore.RequirementDisplay(Requirement, Context)
    End Function
    Friend Shared Function RuntimeProvidersForPlatformDependency(Report As InspectionReport, OwnerPath As String, Requirement As OnlineDependency) As List(Of ModInfo)
        Return InspectionCore.RuntimeProvidersForPlatformDependency(Report, OwnerPath, Requirement)
    End Function
End Class
