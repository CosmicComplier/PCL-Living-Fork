Imports System.Net.Http
Imports System.Threading
Imports Newtonsoft.Json.Linq
Imports OnlineCore = PCLCS.ModDependencyOnlinePipeline(Of PCL.ModDependencyInspector.InspectionContext, PCL.ModDependencyInspector.ModInfo, PCL.ModDependencyInspector.Dependency, PCL.ModDependencyInspector.DependencyEdge, PCL.ModDependencyInspector.InspectionIssue, PCL.ModDependencyInspector.RemovalImpact, PCL.ModDependencyInspector.InspectionReport, PCL.ModDependencyInspector.OnlineProjectInfo, PCL.ModDependencyInspector.OnlineDependency, PCL.ModDependencyInspector.OnlineFileInfo, PCL.ModDependencyOnline.LookupOptions, PCL.ModDependencyOnline.LookupCache)

'保留原 LF 注入入口与实际旧类型；网络匹配、缓存、预算、指纹及 HTTP 传输由 C# 实现。
Friend NotInheritable Class ModDependencyOnline
    Private Sub New()
    End Sub

    Friend Class LookupOptions
        Inherits PCLCS.ModDependencyLookupOptions(Of LookupCache)
    End Class
    Friend Class LookupCache
        Inherits PCLCS.ModDependencyLookupCache
    End Class
    Private Shared ReadOnly SharedCache As New LookupCache
    Private Shared ReadOnly Pipeline As New OnlineCore(SharedCache, AddressOf DefaultRequest)

    '保留原反射注入的异常类型和构造签名，C# Session 捕获同一基类及实际字段。
    Private Class HttpFailure
        Inherits PCLCS.ModDependencyHttpFailure
        Public Sub New(Code As Integer, RetrySeconds As Integer)
            MyBase.New(Code, RetrySeconds)
        End Sub
    End Class

    Friend Shared Function InspectWithOnline(ModsFolder As String, Optional Context As ModDependencyInspector.InspectionContext = Nothing,
                                            Optional Token As CancellationToken = Nothing, Optional Options As LookupOptions = Nothing) As ModDependencyInspector.InspectionReport
        Return Pipeline.InspectWithOnline(ModsFolder, Context, Token, Options)
    End Function
    Friend Shared Function Enrich(Report As ModDependencyInspector.InspectionReport, ModsFolder As String,
                                 Optional Token As CancellationToken = Nothing, Optional Options As LookupOptions = Nothing) As ModDependencyInspector.InspectionReport
        Return Pipeline.Enrich(Report, ModsFolder, Token, Options)
    End Function
    Friend Shared Function SearchQuery(FullPath As String) As String
        Return OnlineCore.SearchQuery(FullPath)
    End Function
    Friend Shared Function IsAllowedApi(Url As String) As Boolean
        Return PCLCS.ModDependencyTransport.IsAllowedApi(Url)
    End Function
    Friend Shared Function DefaultRequest(Url As String, Method As HttpMethod, Body As String, Token As CancellationToken) As JToken
        Return PCLCS.ModDependencyTransport.DefaultRequest(Url, Method, Body, Token, AddressOf SignRequest,
            Function(Code, RetrySeconds) New HttpFailure(Code, RetrySeconds))
    End Function
    Private Shared Sub SignRequest(Url As String, Request As HttpRequestMessage)
        Dim SignedRequest = Request
        SecretHeadersSign(Url, SignedRequest)
    End Sub
End Class
