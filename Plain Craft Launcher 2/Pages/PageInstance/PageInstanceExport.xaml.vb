Imports System.Windows.Documents
Imports System.Windows.Media
Imports ExportController = PCLCS.ModpackExportPageController(Of PCL.ModMinecraft.McInstance, PCL.ExportOption, PCL.MyCheckBox, PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion, PCL.ModLoader.LoaderCombo(Of String))
Imports ExportPipeline = PCLCS.ModpackExportPipeline(Of PCL.ModMinecraft.McInstance, PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion)
Imports ExportRequest = PCLCS.ModpackExportRequest(Of PCL.ModMinecraft.McInstance)

Public Class ExportOption
    Inherits PCLCS.ModpackExportOptionData
End Class

Public Class PageInstanceExport
    Implements IRefreshable
    Private CurrentInstance As String = ""
    Private SubOptionBlackList As String() = {"Quark Programmer Art.zip", "+ EuphoriaPatches_", "PCL2 Skin.zip"}
    Private _RulesOverrides As List(Of String) = Nothing
    Private ExtraFiles As List(Of String) = Nothing
    Private ConfigPackPath As String = Nothing
    Private Const Sperator As String = "=============================================================="
    Private PageController As ExportController
    Private ReadOnly Pipeline As New ExportPipeline(CreatePipelineHost())
    Private ReadOnly Property Controller As ExportController
        Get
            If PageController Is Nothing Then PageController = New ExportController(CreateView(), CreateState(), CreateHost())
            Return PageController
        End Get
    End Property
    Private Function CreateView() As PCLCS.ModpackExportPageView(Of MyCheckBox)
        Return New PCLCS.ModpackExportPageView(Of MyCheckBox) With {
            .Name = TextExportName, .Version = TextExportVersion,
            .NameHint = Function() TextExportName.HintText,
            .SetNameHint = Sub(Value) TextExportName.HintText = Value,
            .SetVersionHint = Sub(Value) TextExportVersion.HintText = Value,
            .Basic = CheckOptionsBasic, .Java = CheckOptionsJava, .Launcher = CheckOptionsPcl, .Custom = CheckOptionsPclCustom,
            .ModpackMode = CheckOptionsModpackMode, .Isolation = CheckAdvancedVersionIsolation,
            .IncludeAssets = CheckAdvancedInclude, .Modrinth = CheckAdvancedModrinth, .ShaderSettings = CheckOptionsShaderPackSettings,
            .Options = PanOptions, .ResourcePacks = PanOptionsResourcePacks, .Saves = PanOptionsSaves, .Shaders = PanOptionsShaderPacks,
            .OverrideCancel = BtnOverrideCancel, .CardOptions = CardOptions,
            .HintOptiFine = HintOptiFine, .HintJava = HintJava, .HintModrinth = HintModrinthFormat, .Export = BtnExport,
            .CardInlines = Function() CardOptions.Inlines,
            .ScrollHome = Sub() PanBack.ScrollToHome(),
            .Checked = Function(Check) Check.Checked,
            .SetChecked = Sub(Check, Value) Check.Checked = Value,
            .Inlines = Function(Check) Check.Inlines,
            .CreateCheck = Function(Data) New MyCheckBox With {.Tag = Data},
            .Gray = Function() CType(ColorGray5, Brush),
            .SetJavaHint = Sub(Text, Red)
                               HintJava.Theme = If(Red, MyHint.Themes.Red, MyHint.Themes.Yellow)
                               HintJava.Text = Text
                           End Sub}
    End Function
    Private Function CreateState() As PCLCS.ModpackExportPageState
        Return New PCLCS.ModpackExportPageState With {
            .CurrentInstance = Function() CurrentInstance,
            .SetCurrentInstance = Sub(Value) CurrentInstance = Value,
            .BlackList = Function() SubOptionBlackList,
            .Rules = Function() _RulesOverrides, .SetRules = Sub(Value) _RulesOverrides = Value,
            .Extra = Function() ExtraFiles, .SetExtra = Sub(Value) ExtraFiles = Value,
            .PackPath = Function() ConfigPackPath, .SetPackPath = Sub(Value) ConfigPackPath = Value}
    End Function
    Private Function CreateHost() As PCLCS.ModpackExportPageHost(Of McInstance, ExportOption, LoaderCombo(Of String))
        Return New PCLCS.ModpackExportPageHost(Of McInstance, ExportOption, LoaderCombo(Of String)) With {
            .Instance = Function() PageInstanceLeft.Instance,
            .Name = Function(Instance) Instance.Name,
            .VersionFolder = Function(Instance) Instance.PathVersion,
            .GameRoot = Function(Instance) Instance.PathIndie,
            .VersionDescription = Function(Instance) Instance.VersionDisplayName(),
            .Version = Function(Instance) Instance.Version,
            .Modable = Function(Instance) Instance.Modable,
            .JavaType = Function(Instance) Settings.Get(Of Integer)("VersionArgumentJavaV2", Instance),
            .JavaWorker = Function() PageInstanceLeft.CurrentJavaWorker,
            .Ui = Sub(Work) RunInUi(Sub() Work()),
            .AdjustAnimation = Sub(Value) AniControlEnabled += Value,
            .Animation = Function() AniControlEnabled,
            .ReleaseBuild = Function() BuildType = BuildTypes.Release,
            .ModpackEnabled = Function() ModModpackMode.Enabled,
            .SetHelpEvent = Sub(Value) CustomEventService.SetEventData(BtnAdvancedHelp, Value),
            .CreateOption = Function() New ExportOption,
            .TaskNames = Function() LoaderTaskbar.Select(Function(Loader) Loader.Name),
            .ShowTasks = Sub() FrmMain.PageChange(FormMain.PageType.TaskManager),
            .TempFolder = Function() RequestTaskTempFolder(),
            .ReadConfigPath = Function() Settings.Get(Of String)("CacheExportConfig"),
            .WriteConfigPath = Sub(Value) Settings.Set("CacheExportConfig", Value),
            .SaveFile = Function(Title, Name, Initial, Filters) Dialogs.SaveFile(Title, Name, Initial, Filters),
            .SelectConfig = Function(Title, Initial) Dialogs.SelectFile(Title, False, Initial, {({"txt"}, "整合包导出配置")}).FirstOrDefault(),
            .Confirm = Function(Caption, Title, Button1, Button2) MyMsgBox(Caption, Title, Button1, Button2),
            .ErrorDisplay = Function(ErrorValue) ErrorValue.GetDisplay(True),
            .Info = Sub(Message) Logger.Info(Message),
            .HintGreen = Sub(Message) Hint(Message, HintType.Green),
            .Explorer = Sub(FileName) OpenExplorer(FileName),
            .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            .ErrorAlert = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.Alert),
            .ErrorToast = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.Toast),
            .CreateCombo = AddressOf CreateExportLoader,
            .StartCombo = Sub(Loader) Loader.Start(),
            .AddTaskbar = Sub(Loader) LoaderTaskbarAdd(Loader),
            .RefreshDownload = Sub() FrmMain.BtnExtraDownload.ShowRefresh(),
            .RibbleDownload = Sub() FrmMain.BtnExtraDownload.Ribble(),
            .QuoteLeft = Function() vbLQ, .QuoteRight = Function() vbRQ}
    End Function
    Private Shared Function CreatePipelineHost() As PCLCS.ModpackExportHost(Of McInstance, LocalResourceFile, ResourceProject, ResourceVersion)
        Return New PCLCS.ModpackExportHost(Of McInstance, LocalResourceFile, ResourceProject, ResourceVersion) With {
            .VersionFolder = Function(Instance) Instance.PathVersion,
            .Info = Function(Instance) Instance.Info,
            .Version = Function(Instance) Instance.Version,
            .SelectedMinecraftFolder = Function() McFolderSelected,
            .Executable = Function() PathExe, .ExecutableName = Function() LauncherExecutableName, .BaseFolder = Function() Paths.Base,
            .CreateLocal = Function(FileName) New LocalResourceFile(FileName),
            .SelectJava = Function(Instance, Prompt, Token, Progress) SelectOrDownloadJava(Instance, Prompt, Token, Progress),
            .WriteBoolIni = Sub(FileName, Key, Value) WriteIni(FileName, Key, Value),
            .ConfigureIsolation = Sub(Folder, Enabled, Dirs) ExportedVersionIsolation.ConfigureExport(Folder, Enabled, Dirs),
            .ConfigureMode = Sub(Folder, Include, Enabled) ModModpackMode.ConfigureExport(Folder, Include, Enabled),
            .Request = Function(Url, Method, Body, ContentType) DlModRequest(Url, Method, Body, ContentType),
            .StartThread = Function(Work, Name) RunInNewThread(Sub() Work(), Name),
            .Confirm = Function(Caption, Title, Button1, Button2) MyMsgBox(Caption, Title, Button1, Button2),
            .InfoLog = Sub(Message) Logger.Info(Message),
            .HintRed = Sub(Message) Hint(Message, HintType.Red),
            .Explorer = Sub(FileName) OpenExplorer(FileName),
            .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message)}
    End Function
    '三个原类型 Loader，只把实际 Input/Output/取消/进度面适配到 C#。
    Private Function CreateExportLoader(Request As ExportRequest) As LoaderCombo(Of String)
        Dim Copy As New LoaderTask(Of Integer, List(Of LocalResourceFile))(Request.CopyStepName,
            Sub(Loader) Pipeline.Copy(Request, New PCLCS.ModpackExportCopyTask(Of LocalResourceFile) With {
                .Output = Function() Loader.Output, .SetOutput = Sub(Value) Loader.Output = Value,
                .Progress = Function() Loader.Progress, .SetProgress = Sub(Value) Loader.Progress = Value,
                .Cancellation = Function() Loader.CreateCancellationToken(),
                .ProgressProvider = Function(Start, Finish) Loader.CreateSyncProgressProvider(Start, Finish)})) With {.ProgressWeight = Request.CopyWeight}
        Dim Online As New LoaderTask(Of List(Of LocalResourceFile), Dictionary(Of LocalResourceFile, List(Of String)))(Request.OnlineStepName,
            Sub(Loader) Pipeline.Online(Request, New PCLCS.ModpackExportOnlineTask(Of LocalResourceFile) With {
                .Input = Function() Loader.Input, .Output = Function() Loader.Output, .SetOutput = Sub(Value) Loader.Output = Value,
                .IsCanceled = Function() Loader.IsCanceled,
                .Progress = Function() Loader.Progress, .SetProgress = Sub(Value) Loader.Progress = Value})) With {.Show = Request.ShowOnline, .ProgressWeight = Request.OnlineWeight}
        Dim Archive As New LoaderTask(Of Dictionary(Of LocalResourceFile, List(Of String)), Integer)(Request.ArchiveStepName,
            Sub(Loader) Pipeline.Archive(Request, New PCLCS.ModpackExportArchiveTask(Of LocalResourceFile) With {
                .Input = Function() Loader.Input, .SetProgress = Sub(Value) Loader.Progress = Value})) With {.ProgressWeight = Request.ArchiveWeight}
        Return New LoaderCombo(Of String)(Request.LoaderName, {Copy, Online, Archive}) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
    End Function
    Private Sub PageInstanceExport_Initialized(sender As Object, e As EventArgs) Handles Me.Initialized
        Controller.Initialized()
    End Sub
    Private Sub PageInstanceExport_Loaded() Handles Me.Loaded
        Controller.Loaded()
    End Sub
    Public Sub RefreshAll() Implements IRefreshable.Refresh
        Controller.RefreshAll()
    End Sub
    Private Sub ReloadAllSubOptions()
        Controller.ReloadAllSubOptions()
    End Sub
    Private Sub ReloadSubOptions(Panel As StackPanel, AcceptCompressedFile As Boolean, AcceptFolder As Boolean, ParamArray Folders As String())
        Controller.ReloadSubOptions(Panel, AcceptCompressedFile, AcceptFolder, Folders)
    End Sub
    Private Sub RefreshAllOptionsUI()
        Controller.RefreshOptions()
    End Sub
    Private Function StandardizeLines(Raw As IEnumerable(Of String), AddSuffixStarToFolderPath As Boolean) As IEnumerable(Of String)
        Return ExportPipeline.StandardizeLines(Raw, AddSuffixStarToFolderPath)
    End Function
    Private Function GetAllOptions(IncludeHidden As Boolean) As IEnumerable(Of MyCheckBox)
        Return Controller.AllOptions(IncludeHidden)
    End Function
    Private Function GetExportOption(CheckBox As MyCheckBox) As ExportOption
        Return Controller.GetOption(CheckBox)
    End Function
    Private Property RulesOverrides As List(Of String)
        Get
            Return Controller.RulesOverrides
        End Get
        Set(Value As List(Of String))
            Controller.RulesOverrides = Value
        End Set
    End Property
    Private Function GetAllRules() As IEnumerable(Of String)
        Return Controller.Rules()
    End Function
    Private Function GetExtraFileLines() As IEnumerable(Of String)
        Return Controller.ExtraFiles()
    End Function
    Private Sub ResetConfigOverrides()
        Controller.ResetConfigOverrides()
    End Sub
    Private Sub CardOptions_MouseLeftButtonDown() Handles CardOptions.MouseLeftButtonDown
        Controller.OptionsMouseDown()
    End Sub
    Private Sub ExportConfig() Handles BtnAdvancedExport.Click
        Controller.ExportConfig()
    End Sub
    Private Sub ImportConfig() Handles BtnAdvancedImport.Click
        Controller.ImportConfig()
    End Sub
    Private Sub StartExport() Handles BtnExport.Click
        Controller.StartExport()
    End Sub
    Private Sub RefreshJavaInfo() Handles Me.Loaded
        Controller.RefreshJavaInfo()
    End Sub
    Private Sub TextExportName_GotFocus() Handles TextExportName.GotFocus
        Controller.NameGotFocus()
    End Sub
    Private Sub CheckAdvancedInclude_Change(sender As Object, user As Boolean) Handles CheckAdvancedInclude.Change
        Controller.IncludeChanged()
    End Sub
    Private Sub CheckAdvancedModrinth_Change(sender As Object, user As Boolean) Handles CheckAdvancedModrinth.Change, CheckOptionsPcl.Change
        Controller.ModrinthChanged()
    End Sub
End Class
