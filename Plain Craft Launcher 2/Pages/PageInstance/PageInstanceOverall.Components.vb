Partial Public Class PageInstanceOverall
    Private ComponentRefreshId As Integer
    Private ReadOnly ComponentsLoader As New LoaderTask(Of ComponentRequest, ComponentResult)("读取已安装组件", AddressOf ReadInstalledComponents)
    Private Class ComponentRequest
        Inherits PCLCS.InstanceComponentRequestData(Of McInstance)
    End Class
    Private Class ComponentResult
        Inherits PCLCS.InstanceComponentResultData(Of ComponentRequest, InstalledComponentSummary.Item)
    End Class
    Private ComponentController As PCLCS.InstanceComponentsController(Of McInstance, ComponentRequest, ComponentResult, InstalledComponentSummary.Item, MyCard, MyImage)
    Private ReadOnly Property ComponentView As PCLCS.InstanceComponentsController(Of McInstance, ComponentRequest, ComponentResult, InstalledComponentSummary.Item, MyCard, MyImage)
        Get
            If ComponentController Is Nothing Then ComponentController = New PCLCS.InstanceComponentsController(Of McInstance, ComponentRequest, ComponentResult, InstalledComponentSummary.Item, MyCard, MyImage)(
                New PCLCS.InstanceComponentsHost(Of McInstance, ComponentRequest, ComponentResult, InstalledComponentSummary.Item, MyCard, MyImage) With {
                    .Instance = Function() PageInstanceLeft.Instance, .Modpack = Function() ModModpackMode.Enabled,
                    .NextRevision = Function() PCLCS.InstanceComponentRevision.Next(ComponentRefreshId), .Revision = Function() ComponentRefreshId,
                    .CreateRequest = Function() New ComponentRequest, .CreateResult = Function() New ComponentResult,
                    .HasSavedIsolation = Function(Target) Settings.HasSaved("VersionArgumentIndieV2", Target),
                    .Isolation = Function(Target) Settings.Get(Of Boolean)("VersionArgumentIndieV2", Target),
                    .VersionFolder = Function(Target) Target.PathVersion, .MinecraftFolder = Function() McFolderSelected, .ImageRoot = Function() PathImage,
                    .Version = Function(Target) Target.Version, .Components = Function(Target) Configs.McInstallManifest.Get(Target.Config)?.Components,
                    .Start = Sub(Request, Force) ComponentsLoader.Start(Request, IsForceRestart:=Force), .Cancel = Sub() ComponentsLoader.Cancel(),
                    .Warn = Sub(Err, Message) Logger.Warn(Err, Message), .Cards = PanInstalledComponents,
                    .CreateImage = Function(Source) New MyImage With {.Source = Source}, .SetStretch = Sub(Image, Stretch) Image.Stretch = Stretch,
                    .CreateCard = Function(Title) New MyCard With {.Title = Title}, .TouchTitle = Sub(Card)
                                                                                                        Dim Title = Card.MainTextBlock
                                                                                                    End Sub}, InstalledComponentSummary.Engine)
            Return ComponentController
        End Get
    End Property
    Private Shared Function ComponentTask(Loader As LoaderTask(Of ComponentRequest, ComponentResult)) As PCLCS.InstanceComponentsTask(Of ComponentRequest, ComponentResult)
        Return New PCLCS.InstanceComponentsTask(Of ComponentRequest, ComponentResult) With {
            .Input = Function() Loader.Input, .Output = Function() Loader.Output, .SetOutput = Sub(Value) Loader.Output = Value, .IsCanceled = Function() Loader.IsCanceled}
    End Function
    Private Sub ComponentsLoaderInit() Handles Me.Initialized
        PageLoaderInit(LoadComponents, PanComponentsLoad, PanBack, Nothing, ComponentsLoader, AddressOf InstalledComponentsLoaded, AutoRun:=False)
    End Sub
    Private Sub RefreshInstalledComponents()
        ComponentView.Refresh()
    End Sub
    Private Sub ReadInstalledComponents(Loader As LoaderTask(Of ComponentRequest, ComponentResult))
        ComponentView.Read(ComponentTask(Loader))
    End Sub
    Private Sub InstalledComponentsLoaded(Loader As LoaderTask(Of ComponentRequest, ComponentResult))
        ComponentView.Loaded(ComponentTask(Loader))
    End Sub
    Friend Sub ShowInstalledComponents(Items As IEnumerable(Of InstalledComponentSummary.Item))
        ComponentView.Show(Items)
    End Sub
End Class
