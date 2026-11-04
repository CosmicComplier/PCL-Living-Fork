' LF 公开入口与原界面/隐私过滤服务；完整日志导入、检测与报告流程由 C# 承载。
Public Class CrashAnalyzer
    Implements PCLCS.ILegacyCrashAnalyzer
    Private ReadOnly Native As PCLCS.LegacyCrashAnalyzer(Of McInstance, RecentEnvironmentChangesReport)
    Private Shared ReadOnly Host As New PCLCS.LegacyCrashHost(Of McInstance, RecentEnvironmentChangesReport) With {
        .RequestTaskTempFolder = Function() RequestTaskTempFolder(),
        .TempPath = Function() PathTemp, .LauncherIssuesUrl = Function() LauncherIssuesUrl, .NewUuid = Function() GetUuid(),
        .ShowWindowToTop = Sub() FrmMain.ShowWindowToTop(), .FeedbackInfo = Sub() FeedbackInfo(),
        .BuildRecentChanges = Function(Instance) RecentEnvironmentChanges.BuildInstance(Instance, CancellationToken.None),
        .AppendRecentAnalysis = AddressOf RecentEnvironmentChanges.AppendAnalysis,
        .HasBaseline = Function(Report) Report.HasBaseline, .ChangesText = Function(Report, Maximum) Report.ToAnalysisText(Maximum),
        .OpenRestorePoint = AddressOf OpenRestorePoint,
        .MessageBox = Function(Detail, Title, Button1, Button2, Button3, Button2Action) MyMsgBox(Detail, Title, Button1, Button2, Button3, Button2Action:=Button2Action),
        .SaveReportPath = Function(Title, Name) Dialogs.SaveFile(Title, Name, filter:={("zip", "Minecraft 错误报告")}),
        .RunInUi = Sub(Work) RunInUi(Work), .RunInUiWait = Sub(Work) RunInUiWait(Work),
        .StartProcess = Sub(Path) StartProcess(Path), .OpenExplorer = Sub(Path) OpenExplorer(Path),
        .ExportedHint = Sub(Text) Hint(Text, HintType.Green),
        .FilterUserName = Function(Text, Mask) FilterUserName(Text, Mask), .FilterAccessToken = Function(Text, Mask) FilterAccessToken(Text, Mask)}
    Public Sub New(Optional TargetInstance As McInstance = Nothing)
        Native = New PCLCS.LegacyCrashAnalyzer(Of McInstance, RecentEnvironmentChangesReport)(Host, TargetInstance)
    End Sub
    Public Sub Collect(VersionPathIndie As String, Optional LatestLog As IList(Of String) = Nothing) Implements PCLCS.ILegacyCrashAnalyzer.Collect
        Native.Collect(VersionPathIndie, LatestLog)
    End Sub
    Public Sub Import(FilePath As String)
        Native.Import(FilePath)
    End Sub
    Public Function Prepare() As Boolean Implements PCLCS.ILegacyCrashAnalyzer.Prepare
        Return Native.Prepare()
    End Function
    Public Sub Analyze() Implements PCLCS.ILegacyCrashAnalyzer.Analyze
        Native.Analyze()
    End Sub
    Public Sub Output(IsManualAnalyze As Boolean, Optional ExtraFiles As List(Of String) = Nothing) Implements PCLCS.ILegacyCrashAnalyzer.Output
        Native.Output(IsManualAnalyze, ExtraFiles)
    End Sub
    Private Shared Sub OpenRestorePoint(TargetInstance As McInstance, Report As RecentEnvironmentChangesReport)
        PageInstanceLeft.Instance = TargetInstance
        If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
        Dim Page = DirectCast(FrmInstanceLeft.PageGet(FormMain.PageSubType.InstanceRestorePoints), PageInstanceRestorePoints)
        Page.FocusPoint(Report.BaselineId)
        FrmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.InstanceRestorePoints)
    End Sub
End Class
