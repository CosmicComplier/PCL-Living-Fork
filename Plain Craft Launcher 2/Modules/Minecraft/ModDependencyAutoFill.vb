Imports System.Collections.Generic
Imports System.Threading
Imports Newtonsoft.Json.Linq
Imports Inspector = PCL.ModDependencyInspector
Imports AutoFillCore = PCLCS.ModDependencyAutoFillEngine(Of PCL.ModDependencyInspector.InspectionContext, PCL.ModDependencyInspector.ModInfo, PCL.ModDependencyInspector.Dependency, PCL.ModDependencyInspector.DependencyEdge, PCL.ModDependencyInspector.InspectionIssue, PCL.ModDependencyInspector.RemovalImpact, PCL.ModDependencyInspector.InspectionReport, PCL.ModDependencyInspector.OnlineProjectInfo, PCL.ModDependencyInspector.OnlineDependency, PCL.ModDependencyInspector.OnlineFileInfo, PCL.ResourceVersion, PCL.ModNet.NetFile, PCL.ModDependencyAutoFill.Item, PCL.ModDependencyAutoFill.Plan, PCL.ModDependencyActivation.Item, PCL.ModDependencyOnline.LookupOptions, PCL.ModDependencyOnline.LookupCache)

'原 LF 对话框、版本模型、NetFile 与 Loader 的适配；规划、复查和事务在 C# 中执行。
Friend NotInheritable Class ModDependencyAutoFill
    Friend Class Item
        Inherits PCLCS.ModDependencyAutoFillItemData(Of ResourceVersion, Item)
    End Class
    Friend Class Plan
        Inherits PCLCS.ModDependencyAutoFillPlanData(Of Inspector.InspectionReport, Item, ModDependencyActivation.Item)
    End Class
    Friend Shared ReadOnly Service As New AutoFillCore(CreateHost())

    Private Class CandidateMismatchException
        Inherits PCLCS.DependencyCandidateMismatchException
        Friend Sub New(Index As Integer, Message As String)
            MyBase.New(Index, Message)
        End Sub
    End Class

    Private Shared Function CreateHost() As PCLCS.ModDependencyAutoFillHost(Of Inspector.InspectionReport, ResourceVersion, NetFile, Plan, ModDependencyOnline.LookupOptions)
        Return New PCLCS.ModDependencyAutoFillHost(Of Inspector.InspectionReport, ResourceVersion, NetFile, Plan, ModDependencyOnline.LookupOptions) With {
            .ParseVersion = Function(Raw) ResourceVersion.FromPlatformJson(Raw, ResourceTypes.Mod),
            .ResourceType = Function(Value) Value.ResourceType,
            .DownloadAvailable = Function(Value) Value.DownloadAvailable,
            .Size = Function(Value) Value.Size,
            .GameVersions = Function(Value) Value.GameVersions,
            .ModLoaders = Function(Value) Value.ModLoaders,
            .SetModLoaders = Sub(Value, Loaders) Value.ModLoaders = Loaders,
            .Id = Function(Value) Value.Id,
            .FileName = Function(Value) Value.FileName,
            .Hash = Function(Value) Value.Hash,
            .Display = Function(Value) Value.Display,
            .DownloadUrls = Function(Value) Value.DownloadUrls,
            .SetDownloadUrls = Sub(Value, Urls) Value.DownloadUrls = Urls,
            .ReleaseType = Function(Value) CInt(Value.ReleaseType),
            .ReleaseDate = Function(Value) Value.ReleaseDate,
            .ToNetFile = Function(Value, FileName, GameVersion, Loaders) Value.ToNetFile(FileName, ResourceVersion.DownloadReason.Dependency, GameVersion, Loaders),
            .Enrich = Sub(Report, Root, Token, Options) ModDependencyOnline.Enrich(Report, Root, Token, Options),
            .DefaultRequest = AddressOf ModDependencyOnline.DefaultRequest,
            .SafeName = Function(Name) InstanceResourceStore.SafeName(Name),
            .EnsureWithin = Sub(Root, Name) InstanceResourceStore.EnsureWithin(Root, Name),
            .DeleteStage = Sub(Root, Stage) InstanceResourceStore.DeleteStage(Root, Stage),
            .FileHash = Function(Name, Md5) InstanceResourceUpdates.FileHash(Name, Md5),
            .Download = AddressOf DownloadFiles,
            .Info = Sub(Message) Logger.Info(Message),
            .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            .RefreshOptions = Sub() RunInUi(Sub() FrmLaunchRight?.RefreshDependencyOptions()),
            .CandidateMismatch = Function(Index, Message) New CandidateMismatchException(Index, Message)}
    End Function

    Friend Shared Function BuildPlan(Root As String, Report As Inspector.InspectionReport, Token As CancellationToken,
        Optional Options As ModDependencyOnline.LookupOptions = Nothing) As Plan
        Return Service.BuildPlan(Root, Report, Token, Options)
    End Function
    Friend Shared Function Install(Plan As Plan, Token As CancellationToken, Optional Download As Action(Of List(Of NetFile), CancellationToken) = Nothing) As Integer
        Return Service.Install(Plan, Token, Download)
    End Function
    Friend Shared Function CreateDownloadTask(Plan As Plan, Files As List(Of NetFile)) As LoaderCombo(Of Integer)
        Dim Batch = Service.CreateDownloadTask(Plan, Files), Steps As New List(Of LoaderBase)
        For Each Entry In Batch.Steps
            Steps.Add(New LoaderDownload(Entry.Name, {Entry.File}) With {.Block = Entry.Block, .ProgressWeight = Entry.ProgressWeight})
        Next
        Return New LoaderCombo(Of Integer)(Batch.Name, Steps) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
    End Function
    Private Shared Sub DownloadFiles(Plan As Plan, Files As List(Of NetFile), Token As CancellationToken)
        If FrmMain Is Nothing Then
            Dim Work As New LoaderDownload("下载必需前置", Files)
            Using Registration = Token.Register(AddressOf Work.Cancel)
                Work.WaitForExit(c:=Token)
            End Using
        Else
            Dim Work = CreateDownloadTask(Plan, Files)
            RunInUiWait(Sub()
                Work.Start(1)
                LoaderTaskbarAdd(Work)
                FrmMain.BtnExtraDownload.ShowRefresh()
                FrmMain.BtnExtraDownload.Ribble()
            End Sub)
            Using Registration = Token.Register(AddressOf Work.Cancel)
                Work.WaitForExit(c:=Token, StartIfNeeded:=False)
            End Using
        End If
    End Sub
End Class
