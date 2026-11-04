Imports System.Net.Http
Imports FileEngine = PCLCS.LocalResourceLoaderEngine(Of PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion)

Public Module LocalResourceLoaders
    Public LocalResourceLoader As New LoaderTask(Of String, List(Of LocalResourceFile))("LocalResourceLoader", AddressOf LocalResourceFileLoad)
    Public LocalResourceOnlineLoader As New LoaderTask(Of (ModList As List(Of LocalResourceFile), Cache As JObject, Target As McVersion), Integer)("LocalResourceOnlineLoader", AddressOf LocalResourceOnlineLoad)
    Private ReadOnly Engine As New FileEngine(New PCLCS.LocalResourceLoaderHost(Of LocalResourceFile, ResourceProject, ResourceVersion) With {
        .ReadCompatibility = AddressOf ReadCompatibility,
        .CreateLocal = Function(Path) New LocalResourceFile(Path),
        .CreateProject = Function(Data) New ResourceProject(Data),
        .ReadVersion = AddressOf ResourceVersion.FromPlatformJson,
        .ProjectCache = Function() ResourceProject.Cache,
        .Request = Function(Url, Method, Body, ContentType) DlModRequest(Url, Method, Body, ContentType),
        .StartThread = Function(Work, Name) RunInNewThread(Sub() Work(), Name),
        .TempPath = Function() PathTemp,
        .DebugMode = Function() ModeDebug,
        .CacheVersion = Function() Versions.LocalModCacheVersion,
        .CurrentVersion = Function() PageInstanceLeft.Instance?.Version,
        .IsUpdatingFolder = Function(Path) PageInstanceMod.UpdatingInstanceModFolders.Contains(Path),
        .SetShowProgress = AddressOf SetShowProgress,
        .SetLoadText = AddressOf SetLoadText,
        .ReloadFolder = Sub() FrmInstanceMod.LoaderRun(LoaderFolderRunType.UpdateOnly),
        .StartOnline = Sub(Mods, Cache, Target) LocalResourceOnlineLoader.Start((Mods, Cache, Target), IsForceRestart:=True),
        .IsUpdateFilter = Function() If(FrmInstanceMod?.Filter = PageInstanceMod.FilterType.CanUpdate, False),
        .RefreshUi = Sub() RunInUi(Sub() FrmInstanceMod?.RefreshUI()),
        .RefreshBars = Sub() RunInUi(Sub() FrmInstanceMod?.RefreshBars()),
        .Info = Sub(Message) Logger.Info(Message),
        .Warning = Sub(Message) Logger.Warn(Message),
        .WarnQuiet = Sub(Message) Logger.Warn(Message, LogBehavior.None),
        .Trace = Sub(Message) Logger.Trace(Message),
        .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message)})
    Private Function ReadCompatibility() As PCLCS.LocalResourceCompatibilityContext
        Dim Target = PageInstanceLeft.Instance
        If Target Is Nothing Then Return Nothing
        Return New PCLCS.LocalResourceCompatibilityContext With {
            .Folder = IO.Path.Combine(Target.PathIndie, "mods"),
            .Inspect = Sub(Folder, Token) ModDependencyInspector.Inspect(Folder, ModDependencyInspector.ContextForInstance(Target), Token)}
    End Function
    Private Sub SetShowProgress(Value As Boolean)
        RunInUiWait(Sub() If FrmInstanceMod IsNot Nothing Then FrmInstanceMod.Load.ShowProgress = Value)
    End Sub
    Private Sub SetLoadText(Value As String)
        RunInUiWait(Sub() If FrmInstanceMod IsNot Nothing Then FrmInstanceMod.Load.Text = Value)
    End Sub
    Private Sub LocalResourceFileLoad(Loader As LoaderTask(Of String, List(Of LocalResourceFile)))
        Engine.LoadFiles(New PCLCS.LocalResourceFileTask(Of LocalResourceFile) With {
            .CreateCancellationToken = AddressOf Loader.CreateCancellationToken,
            .Input = Function() Loader.Input,
            .IsCanceled = Function() Loader.IsCanceled,
            .SetOutput = Sub(Value) Loader.Output = Value})
    End Sub
    Private Sub LocalResourceOnlineLoad(Loader As LoaderTask(Of (ModList As List(Of LocalResourceFile), Cache As JObject, Target As McVersion), Integer))
        Engine.LoadOnline(New PCLCS.LocalResourceOnlineTask(Of LocalResourceFile) With {
            .Input = Function() Loader.Input,
            .IsCanceled = Function() Loader.IsCanceled,
            .IsCanceledWithThread = Function(Thread) Loader.IsCanceledWithThread(Thread)})
    End Sub
End Module
