Imports PCLLF.Presentation.Accounts

'真实 LF UI、设置、登录加载器及网络服务适配；页面流程只在 C# 控制器中维护。
Friend NotInheritable Class LoginPageRuntime
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Friend Shared ReadOnly Accounts As New LegacyLoginHost(Of McLoginLegacy, McLoginMs, McLoginServer) With {
        .ReadString = Function(Key) Settings.Get(Of String)(Key), .ReadInstanceString = Function(Key) Settings.Get(Of String)(Key, Instance:=McInstanceSelected),
        .ReadBoolean = Function(Key) Settings.Get(Of Boolean)(Key), .ReadInteger = Function(Key) Settings.Get(Of Integer)(Key),
        .WriteSetting = Sub(Key, Value) Settings.Set(Key, Value), .AccountSnapshot = Function(Prefix) Settings.AccountSnapshot(Prefix),
        .SaveAccountGroup = Sub(Values) Settings.SaveAccountGroup(Values),
        .CreateLegacy = Function(Data) New McLoginLegacy With {.UserName = Data.UserName, .SkinType = Data.SkinType, .SkinName = Data.SkinName},
        .CreateMs = AddressOf CreateMicrosoftData, .CreateServer = AddressOf CreateServerData,
        .LegacyName = Function(Data) Data.UserName, .MsRefreshToken = Function(Data) Data.OAuthRefreshToken,
        .ServerName = Function(Data) Data.UserName, .ServerPassword = Function(Data) Data.Password,
        .ForceReselectProfile = Sub(Data, Value) Data.ForceReselectProfile = Value,
        .IsReselectingServer = Function(Data) TypeOf Data Is McLoginServer AndAlso DirectCast(Data, McLoginServer).ForceReselectProfile,
        .HasSelectedInstance = Function() McInstanceSelected IsNot Nothing,
        .SelectedVersionValid = Function() McInstanceSelected.Version.Valid, .SelectedVanillaVersion = Function() McInstanceSelected.Version.Vanilla,
        .AnimationDisabled = Function() AniControlEnabled, .HiddenForceShow = Function() PageSetupUI.HiddenForceShow,
        .HasLaunchPage = Function() FrmLaunchLeft IsNot Nothing,
        .OpenLaunchSettings = Sub() FrmMain.PageChange(FormMain.PageType.Setup, FormMain.PageSubType.SetupLaunch),
        .RefreshLaunchPage = Sub(KeepInput, Anim) FrmLaunchLeft.RefreshPage(KeepInput, Anim),
        .OpenWebsite = Sub(Address) OpenWebsite(Address), .Hint = AddressOf PublishHint,
        .RunInUi = Sub(Work) RunInUi(Work), .RunInUiWait = Sub(Work) RunInUiWait(Work), .RunInThread = Sub(Work) RunInThread(Work),
        .RunInNewThread = Sub(Work, Name) RunInNewThread(Work, Name),
        .MsLogin = TaskPort(Function() McLoginMsLoader), .NideLogin = TaskPort(Function() McLoginNideLoader),
        .AuthLogin = TaskPort(Function() McLoginAuthLoader), .MainLogin = TaskPort(Function() McLoginLoader), .Launch = TaskPort(Function() McLaunchLoader),
        .MsAccessToken = Function() McLoginMsLoader.Output.AccessToken, .MsUuid = Function() McLoginMsLoader.Output.Uuid,
        .MsProfileJson = Function() McLoginMsLoader.Output.ProfileJson, .SetMsProfileJson = Sub(Value) McLoginMsLoader.Output.ProfileJson = Value,
        .RequestRetry = AddressOf RequestRetry}
    Friend Shared ReadOnly Skins As New LegacySkinServices(Of McLoginLegacy, McLoginMs, McLoginServer, LoaderTask(Of (String, String), String))(
        New LegacySkinHost(Of McLoginLegacy, McLoginMs, McLoginServer, LoaderTask(Of (String, String), String)) With {
            .Accounts = Accounts, .Task = Function(Loader) TaskPort(Function() Loader), .Output = Function(Loader) Loader.Output,
            .Name = Function(Loader) Loader.Name, .SkinLoaders = Function() PageLaunchLeft.SkinLoaders,
            .LegacyLoader = Function() PageLaunchLeft.SkinLegacy, .MicrosoftLoader = Function() PageLaunchLeft.SkinMs,
            .ImagePath = Function() PathImage, .TempPath = Function() PathTemp, .CreateBitmap = Function(Address) New MyBitmap(Address),
            .SaveSkinPath = Function(Title, Name) Dialogs.SaveFile(Title, Name, filter:={("png", "皮肤图片文件")}),
            .ClearIniCache = Sub(Address) IniClearCache(Address), .WriteIni = Sub(Address, Key, Value) WriteIni(Address, Key, Value),
            .MsLoggedInData = AddressOf PageLoginMsSkin.GetLoginData, .SelectCape = AddressOf SelectCape})
    Private Shared Function CreateMicrosoftData(Data As LegacyLoginInput) As McLoginMs
        Dim Result As New McLoginMs
        If Data.PopulateMsBasic Then
            Result.OAuthRefreshToken = Data.RefreshToken
            Result.UserName = Data.UserName
        End If
        If Data.PopulateMsFull Then
            Result.AccessToken = Data.AccessToken
            Result.Uuid = Data.Uuid
            Result.ProfileJson = Data.ProfileJson
        End If
        Return Result
    End Function
    Private Shared Function CreateServerData(Data As LegacyLoginInput) As McLoginServer
        Return New McLoginServer(If(Data.Token = "Auth", McLoginType.Auth, McLoginType.Nide)) With {
            .Token = Data.Token, .BaseUrl = Data.BaseUrl, .UserName = Data.UserName, .Password = Data.Password,
            .Description = Data.Description, .Type = If(Data.Token = "Auth", McLoginType.Auth, McLoginType.Nide)}
    End Function
    Friend Shared Function TaskPort(Of TIn, TOut)(Read As Func(Of LoaderTask(Of TIn, TOut))) As LegacyLoginTaskPort
        Return New LegacyLoginTaskPort With {
            .State = Function() CType(CInt(Read().State), LegacyLoginTaskState), .Progress = Function() Read().Progress,
            .Error = Function() Read().Error, .Input = Function() Read().Input,
            .Start = Sub(Input, Force) Read().Start(Input, Force), .Wait = Sub(Input, Force) Read().WaitForExit(Input, IsForceRestart:=Force),
            .Cancel = Sub() Read().Cancel(), .ResetInput = Sub() Read().Input = Nothing}
    End Function
    Friend Shared Sub PublishHint(Text As String, Kind As LegacyLoginHint)
        Select Case Kind
            Case LegacyLoginHint.Red : Hint(Text, HintType.Red)
            Case LegacyLoginHint.Green : Hint(Text, HintType.Green)
            Case LegacyLoginHint.Blue : Hint(Text, HintType.Blue)
            Case Else : Hint(Text)
        End Select
    End Sub
    Private Shared Function RequestRetry(Url As String, Method As HttpMethod, Content As Object, ContentType As String, Headers As Dictionary(Of String, String)) As String
        Dim Values(Headers.Count - 1, 1) As String, Index As Integer
        For Each Pair In Headers
            Values(Index, 0) = Pair.Key : Values(Index, 1) = Pair.Value : Index += 1
        Next
        Return NetRequestByClientRetry(Url, Method, Content:=Content, ContentType:=ContentType, Headers:=Values)
    End Function
    Private Shared Function SelectCape(Choices As IReadOnlyList(Of LegacyCapeChoice)) As Integer?
        Dim Controls As New List(Of IMyRadio)
        For Each Choice In Choices
            Controls.Add(New MyRadioBox With {.Text = Choice.Text, .Checked = Choice.Checked})
        Next
        Return MyMsgBoxSelect(Controls, "选择披风", "确定", "取消")
    End Function
    Friend Shared Function SelectSkin() As LegacySelectedSkin
        Dim Data = McSkinSelect()
        Return New LegacySelectedSkin With {.IsVaild = Data.IsVaild, .IsSlim = Data.IsSlim, .LocalFile = Data.LocalFile}
    End Function
End Class
