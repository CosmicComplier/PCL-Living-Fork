Imports System.Threading
Imports Inspector = PCL.ModDependencyInspector

'原静态入口保持；实际会话去重、指纹与已禁用前置启用规划使用同一 C# 服务。
Friend NotInheritable Class LaunchDependencyCheck
    Friend Shared Sub SettingsChanged()
        ModDependencyAutoFill.Service.SettingsChanged()
    End Sub
    Friend Shared Function Fingerprint(Report As Inspector.InspectionReport) As String
        Return ModDependencyAutoFill.Service.Fingerprint(Report)
    End Function
    Friend Shared Function WasAccepted(Folder As String, Report As Inspector.InspectionReport) As Boolean
        Return ModDependencyAutoFill.Service.WasAccepted(Folder, Report)
    End Function
    Friend Shared Sub Accept(Folder As String, Report As Inspector.InspectionReport)
        ModDependencyAutoFill.Service.Accept(Folder, Report)
    End Sub
End Class

Friend NotInheritable Class ModDependencyActivation
    Friend Class Item
        Inherits PCLCS.DependencyActivationItemData(Of Inspector.ModInfo)
    End Class
    Friend Shared Function IssueKey(Issue As Inspector.InspectionIssue) As String
        Return ModDependencyAutoFill.Service.IssueKey(Issue)
    End Function
    Friend Shared Function Prepare(Root As String, Report As Inspector.InspectionReport, Token As Threading.CancellationToken) As ModDependencyAutoFill.Plan
        Return ModDependencyAutoFill.Service.Prepare(Root, Report, Token)
    End Function
End Class
