Imports System.Threading
Imports IndexCore = PCLCS.ModDependencyInspection(Of PCL.ModDependencyInspector.InspectionContext, PCL.ModDependencyInspector.ModInfo, PCL.ModDependencyInspector.Dependency, PCL.ModDependencyInspector.DependencyEdge, PCL.ModDependencyInspector.InspectionIssue, PCL.ModDependencyInspector.RemovalImpact, PCL.ModDependencyInspector.InspectionReport, PCL.ModDependencyInspector.OnlineProjectInfo, PCL.ModDependencyInspector.OnlineDependency, PCL.ModDependencyInspector.OnlineFileInfo).Index

'Original LF names; the persistent graph, leases and file transactions live in C#.
Friend NotInheritable Class ModDependencyIndex
    Private Sub New()
    End Sub
    Private Const Schema As Integer = IndexCore.Schema
    Private Const MaxBytes As Long = IndexCore.MaxBytes
    Friend Shared Property CacheDirectory As String
        Get
            Return IndexCore.CacheDirectory
        End Get
        Set(Value As String)
            IndexCore.CacheDirectory = Value
        End Set
    End Property
    Friend Shared Function IndexPath(Root As String, Context As ModDependencyInspector.InspectionContext) As String
        Return IndexCore.IndexPath(Root, Context)
    End Function
    Friend Shared Sub ClearMemory()
        IndexCore.ClearMemory()
    End Sub
    Friend Shared Function Inspect(Root As String, Context As ModDependencyInspector.InspectionContext, Token As CancellationToken, Force As Boolean) As ModDependencyInspector.InspectionReport
        Return IndexCore.Inspect(Root, Context, Token, Force)
    End Function
    Friend Shared Sub RememberOnline(Root As String, Tree As ModDependencyInspector.InspectionReport, Token As CancellationToken)
        IndexCore.RememberOnline(Root, Tree, Token)
    End Sub
End Class
