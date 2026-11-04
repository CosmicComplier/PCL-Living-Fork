Public Class ResourceSearcher
    Public Class SearchRequest
        Inherits PCLCS.ResourceSearchRequestData(Of ResourceProject, SearchResult)
        Public Sub New(Type As ResourceTypes, Storage As SearchResult, TargetResultCount As Integer)
            MyBase.New(Type, Storage, TargetResultCount)
        End Sub
        Public Overrides Function Equals(Obj As Object) As Boolean
            Return MyBase.Equals(TryCast(Obj, SearchRequest))
        End Function
        Public Overrides Function GetHashCode() As Integer
            Return MyBase.GetHashCode()
        End Function
        Public Shared Operator =(Left As SearchRequest, Right As SearchRequest) As Boolean
            Return EqualityComparer(Of SearchRequest).Default.Equals(Left, Right)
        End Operator
        Public Shared Operator <>(Left As SearchRequest, Right As SearchRequest) As Boolean
            Return Not Left = Right
        End Operator
    End Class
    Public Class SearchResult
        Inherits PCLCS.ResourceSearchResultData(Of ResourceProject)
    End Class
    Private Shared ReadOnly Engine As New PCLCS.ResourceSearchEngine(Of ResourceProject, SearchResult)(
        New PCLCS.ResourceSearchHost(Of ResourceProject) With {
            .CreateProject = Function(Data) New ResourceProject(Data),
            .Request = Function(Url) DlModRequest(Url),
            .StartThread = Function(Work, Name) RunInNewThread(Sub() Work(), Name),
            .SimplifyChinese = Function(Value) StrConv(Value, VbStrConv.SimplifiedChinese),
            .WikiEntries = Function() WikiEntry.All.Value,
            .Info = Sub(Message) Logger.Info(Message),
            .Warning = Sub(Message) Logger.Warn(Message),
            .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message)})
    Public Shared Function GetLoader(InputDelegate As Func(Of SearchRequest)) As LoaderTask(Of SearchRequest, Integer)
        Return New LoaderTask(Of SearchRequest, Integer)("社区资源获取", AddressOf Run, InputDelegate) With {.ReloadTimeout = 60 * 1000}
    End Function
    Private Shared Sub Run(Task As LoaderTask(Of SearchRequest, Integer))
        Engine.Run(Task.Input, New PCLCS.ResourceSearchTask With {
            .IsCanceled = Function() Task.IsCanceled,
            .Progress = Function() Task.Progress,
            .SetProgress = Sub(Value) Task.Progress = Value})
    End Sub
End Class
