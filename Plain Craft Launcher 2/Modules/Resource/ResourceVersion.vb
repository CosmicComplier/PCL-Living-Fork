''' <summary>原 LF 资源版本类型，使用原枚举与列表的单份 C# 模型存储。</summary>
Public Class ResourceVersion
    Inherits PCLCS.ResourceVersionData(Of ResourceVersion.ReleaseTypes)

    Public Sub New()
        MyBase.New(ResourceCatalogRuntime.VersionServices)
    End Sub
    Public Enum ReleaseTypes
        Release = 1
        Beta = 2
        Alpha = 3
    End Enum
    Public Enum DownloadReason
        Standalone
        Dependency
        ModPack
        Update
    End Enum
    Protected Friend Shared ProjectFilesCache As New Dictionary(Of String, List(Of ResourceVersion))

    Public Shared Function FromPlatformJson(Data As JObject, ExpectedResourceType As ResourceTypes) As ResourceVersion
        Return PCLCS.ResourceVersionRules.FromPlatformJson(Data, ExpectedResourceType, Function() New ResourceVersion)
    End Function
    Public Shared Function FromProjectId(ProjectId As String, Platform As ResourcePlatforms, Optional LoadDependencies As Boolean = True) As List(Of ResourceVersion)
        Return ResourceCatalogRuntime.Pipeline.VersionsFromProjectId(ProjectId, Platform, LoadDependencies)
    End Function
    Public Shared Function FromCacheJson(Data As JObject) As ResourceVersion
        Return PCLCS.ResourceVersionRules.FromCacheJson(Of ResourceVersion, ReleaseTypes)(Data, Function() New ResourceVersion)
    End Function
    Public Function ToListItem(ClickHandler As MyListItem.ClickEventHandler, Optional SaveAsButtonHandler As MyIconButton.ClickEventHandler = Nothing,
                               Optional IsBadDisplay As Boolean = False) As MyVirtualizingElement(Of MyListItem)
        Return New MyVirtualizingElement(Of MyListItem)(
            Function() PCLCS.ResourcePresentation.VersionRow(Me, ClickHandler, SaveAsButtonHandler, IsBadDisplay, CreateItemHost())) With {.Height = 42}
    End Function
    Public Function ToNetFile(LocalFileOrFolder As String,
            Optional TrackDownloadReason As DownloadReason? = Nothing, Optional TrackGameVersion As String = Nothing, Optional TrackLoader As ModLoaders = ModLoaders.None) As NetFile
        Dim File = ToDownloadDescriptor(LocalFileOrFolder, If(TrackDownloadReason.HasValue, TrackDownloadReason.Value.ToString(), Nothing), TrackGameVersion, TrackLoader)
        Return New NetFile(File.Urls, File.Destination, New FileChecker With {.Hash = File.Hash, .ActualSize = File.Size}, SimulateBrowserHeaders:=True)
    End Function
    Public Shared Function ParseModrinthTrackArguments(Urls As List(Of String),
            Optional TrackDownloadReason As DownloadReason? = Nothing, Optional TrackGameVersion As String = Nothing, Optional TrackLoader As ModLoaders = ModLoaders.None) As IEnumerable(Of String)
        Return PCLCS.ResourceVersionRules.ParseModrinthTrackArguments(Urls, If(TrackDownloadReason.HasValue, TrackDownloadReason.Value.ToString(), Nothing), TrackGameVersion, TrackLoader)
    End Function
    Public Shared Function ParseCurseForgeDownloadUrls(Url As String) As List(Of String)
        Return PCLCS.ResourceVersionRules.ParseCurseForgeDownloadUrls(Url)
    End Function

    '仅原懒列表项、实际 Click 委托与保存按钮的类型适配。
    Private Shared Function CreateItemHost() As PCLCS.ResourceVersionListHost(Of MyListItem, MyListItem.ClickEventHandler, MyIconButton.ClickEventHandler)
        Return New PCLCS.ResourceVersionListHost(Of MyListItem, MyListItem.ClickEventHandler, MyIconButton.ClickEventHandler) With {
            .Create = Function() New MyListItem,
            .Configure = Sub(Row, Owner, Title, Image)
                             Row.SnapsToDevicePixels = True
                             Row.Height = 42
                             Row.Type = MyListItem.CheckType.Clickable
                             Row.Tag = Owner
                             Row.Title = Title
                             Row.Logo = Image
                         End Sub,
            .SetInfo = Sub(Row, Value) Row.Info = Value,
            .Title = Function(Row) Row.Title,
            .AttachClick = Sub(Row, Handler) AddHandler Row.Click, Handler,
            .AttachSave = AddressOf AttachSave}
    End Function
    Private Shared Sub AttachSave(Row As MyListItem, Handler As MyIconButton.ClickEventHandler)
        Dim Button As New MyIconButton With {.Logo = Logo.IconButtonSave, .ToolTip = "另存为"}
        AddHandler Button.Click, Handler
        Row.Buttons = {Button}
    End Sub
End Class
