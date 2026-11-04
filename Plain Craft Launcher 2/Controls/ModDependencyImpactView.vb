Imports System.Windows.Media
Imports Inspector = PCL.ModDependencyInspector
Imports ImpactController = PCLCS.ModDependencyImpactViewController(Of PCL.ModDependencyInspector.InspectionContext, PCL.ModDependencyInspector.ModInfo, PCL.ModDependencyInspector.Dependency, PCL.ModDependencyInspector.DependencyEdge, PCL.ModDependencyInspector.InspectionIssue, PCL.ModDependencyInspector.RemovalImpact, PCL.ModDependencyInspector.InspectionReport, PCL.ModDependencyInspector.OnlineProjectInfo, PCL.ModDependencyInspector.OnlineDependency, PCL.ModDependencyInspector.OnlineFileInfo, PCL.ModDependencyImpactView.Item)

Friend NotInheritable Class ModDependencyImpactView
    Inherits Grid

    Friend Class Item
        Inherits PCLCS.ModDependencyImpactItemData
    End Class
    Friend ReadOnly Cards As New Grid
    Friend ReadOnly Property PreferredWidth As Double = ImpactController.PreferredWidth

    Friend Shared Function Collect(Report As Inspector.InspectionReport, Paths As IEnumerable(Of String)) As List(Of Item)
        Return ImpactController.Collect(Report, Paths)
    End Function
    Friend Sub New(Items As List(Of Item), ActionName As String)
        ImpactController.Build(Me, Cards, Items, ActionName, Function() CType(New MyBitmap(PathImage & "Icons/NoIcon.png"), ImageSource))
    End Sub
End Class
