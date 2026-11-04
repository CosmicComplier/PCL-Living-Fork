Imports System.Windows.Media
Imports Inspector = PCL.ModDependencyInspector
Imports GraphController = PCLCS.ModDependencyGraphController(Of PCL.ModDependencyInspector.InspectionContext, PCL.ModDependencyInspector.ModInfo, PCL.ModDependencyInspector.Dependency, PCL.ModDependencyInspector.DependencyEdge, PCL.ModDependencyInspector.InspectionIssue, PCL.ModDependencyInspector.RemovalImpact, PCL.ModDependencyInspector.InspectionReport, PCL.ModDependencyInspector.OnlineProjectInfo, PCL.ModDependencyInspector.OnlineDependency, PCL.ModDependencyInspector.OnlineFileInfo, PCL.ModDependencyGraphView.GraphNode)

'原 LF 图节点身份与弹窗入口；完整关系、布局、渲染及异步图标生命周期由 C# 维护。
Friend NotInheritable Class ModDependencyGraphView
    Inherits Grid

    Private ReadOnly Controller As GraphController
    Friend ReadOnly GraphCanvas As Canvas
    Friend ReadOnly Property PreferredWidth As Double
        Get
            Return Controller.PreferredWidth
        End Get
    End Property

    Friend Class GraphNode
        Inherits PCLCS.ModDependencyGraphNodeData
    End Class

    Friend Sub New(Report As Inspector.InspectionReport, FullPath As String)
        Controller = New GraphController(Me, Report, FullPath, New PCLCS.ModDependencyGraphHost With {
            .DefaultIcon = Function(Name) CType(New MyBitmap(PathImage & Name), ImageSource),
            .ReadIcon = AddressOf ModDependencyIcon.Read})
        GraphCanvas = Controller.GraphCanvas
    End Sub

    Friend Sub LoadIconsForPreview()
        Controller.LoadIconsForPreview()
    End Sub
End Class
