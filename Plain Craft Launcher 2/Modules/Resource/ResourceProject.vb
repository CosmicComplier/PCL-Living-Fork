Imports System.Net.Http

''' <summary>原 LF 项目类型；完整模型、平台解析、缓存与呈现决策在 C#。</summary>
Public Class ResourceProject
    Inherits PCLCS.ResourceProjectModelData

    Protected Friend Shared Cache As New Dictionary(Of String, ResourceProject)

    Public Sub New(Data As JObject)
        MyBase.New(Data, ResourceCatalogRuntime.ProjectServices)
    End Sub
    Public Shared Function FromProjectId(ProjectId As String, Platform As ResourcePlatforms) As ResourceProject
        Return ResourceCatalogRuntime.Pipeline.FromProjectId(ProjectId, Platform)
    End Function
    Public Shared Function FromProjectIds(ProjectIds As IEnumerable(Of String), Platform As ResourcePlatforms) As IEnumerable(Of ResourceProject)
        Return ResourceCatalogRuntime.Pipeline.FromProjectIds(ProjectIds, Platform)
    End Function
    Public Function ToResourceItem(ShowMcVersionDesc As Boolean, ShowLoaderDesc As Boolean) As MyVirtualizingElement(Of MyResourceItem)
        Dim Context = PCLCS.ResourcePresentation.ProjectContext(Me, ShowMcVersionDesc, ShowLoaderDesc)
        Return New MyVirtualizingElement(Of MyResourceItem)(Function() PCLCS.ResourcePresentation.ProjectRow(Me, Context, CreateItemHost())) With {.Height = 64}
    End Function
    Public Sub ApplyLogoToMyImage(Img As MyImage)
        PCLCS.ResourcePresentation.ApplyLogo(Me, Sub(Value) Img.Source = Value, Sub(Value) Img.FallbackSource = Value)
    End Sub
    Public Overloads Function IsLike(Project As ResourceProject) As Boolean
        Return MyBase.IsLike(Project)
    End Function
    Public Overrides Function Equals(Obj As Object) As Boolean
        Return MyBase.Equals(TryCast(Obj, ResourceProject))
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return MyBase.GetHashCode()
    End Function
    Public Shared Operator =(Left As ResourceProject, Right As ResourceProject) As Boolean
        Return EqualityComparer(Of ResourceProject).Default.Equals(Left, Right)
    End Operator
    Public Shared Operator <>(Left As ResourceProject, Right As ResourceProject) As Boolean
        Return Not Left = Right
    End Operator

    '仅映射原控件成员，分段、标题、图片源、筛选描述及计数格式均由 C# 决定。
    Private Shared Function CreateItemHost() As PCLCS.ResourceProjectListHost(Of MyResourceItem)
        Return New PCLCS.ResourceProjectListHost(Of MyResourceItem) With {
            .Create = Function() New MyResourceItem,
            .SetTag = Sub(Row, Value) Row.Tag = Value,
            .SetLogoSource = Sub(Row, Value) Row.PathLogo.Source = Value,
            .SetLogoFallback = Sub(Row, Value) Row.PathLogo.FallbackSource = Value,
            .SetTitle = Sub(Row, Value) Row.Title = Value,
            .SetSubtitle = Sub(Row, Value) Row.SubTitle = Value,
            .RemoveSubtitle = Sub(Row) DirectCast(Row.LabTitleRaw.Parent, StackPanel).Children.Remove(Row.LabTitleRaw),
            .SetContent = Sub(Row, Tags, Description)
                              Row.Tags = Tags
                              Row.Description = Description
                          End Sub,
            .HideVersion = AddressOf HideVersion,
            .SetVersion = Sub(Row, Value) Row.LabVersion.Text = Value,
            .SetFooter = Sub(Row, Source, Time, Downloads)
                             Row.LabSource.Text = Source
                             Row.LabTime.Text = Time
                             Row.LabDownload.Text = Downloads
                         End Sub}
    End Function
    Private Shared Sub HideVersion(Row As MyResourceItem)
        DirectCast(Row.PathVersion.Parent, Grid).Children.Remove(Row.PathVersion)
        DirectCast(Row.LabVersion.Parent, Grid).Children.Remove(Row.LabVersion)
        Row.ColumnVersion1.Width = New GridLength(0)
        Row.ColumnVersion2.MaxWidth = 0
        Row.ColumnVersion3.Width = New GridLength(0)
    End Sub
End Class

'原可替换缓存字典与真实网络／镜像／日志服务；不建立第二份生产缓存。
Friend NotInheritable Class ResourceCatalogRuntime
    Friend Shared ReadOnly VersionServices As New PCLCS.ResourceVersionServices With {
        .MirrorUrl = AddressOf DlSourceModGet,
        .DebugMode = Function() ModeDebug,
        .ImageRoot = Function() PathImage}
    Friend Shared ReadOnly ProjectServices As New PCLCS.ResourceProjectServices With {
        .ReadVersion = Function(Data, Types) ResourceVersion.FromPlatformJson(Data, Types),
        .StoreCache = Sub(Project) ResourceProject.Cache(Project.Id) = DirectCast(Project, ResourceProject),
        .MirrorUrl = AddressOf DlSourceModGet,
        .AllDrops = Function() AllDrops,
        .ImageRoot = Function() PathImage,
        .Info = Sub(Message) Logger.Info(Message)}
    Friend Shared ReadOnly Pipeline As New PCLCS.ResourceCatalogPipeline(Of ResourceProject, ResourceVersion)(
        New PCLCS.ResourceCatalogHost(Of ResourceProject, ResourceVersion) With {
            .ProjectCache = Function() ResourceProject.Cache,
            .VersionCache = Function() ResourceVersion.ProjectFilesCache,
            .CreateProject = Function(Data) New ResourceProject(Data),
            .CreateVersion = Function() New ResourceVersion,
            .Request = Function(Url, Method, Content, ContentType) DlModRequest(Url, Method, Content, ContentType),
            .Info = Sub(Message) Logger.Info(Message), .Warn = Sub(Message) Logger.Warn(Message)})
End Class
