Imports PCLLF.Presentation.Launch

'原 LF 数据、窗口和 Aa/Ani 原语接线；两页完整流程及四类皮肤决策由 C# 承载。
Friend NotInheritable Class LaunchPageRuntime
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Friend Shared Function Animations() As LegacyLaunchAnimations(Of AniData)
        Return New LegacyLaunchAnimations(Of AniData) With {
            .Opacity = Function(Target, Value, Time, Delay, Ease, After) AaOpacity(Target, Value, Time, Delay, ToEase(Ease), After),
            .ScaleTransform = Function(Target, Value, Time, Delay, Ease, After) AaScaleTransform(Target, Value, Time, Delay, ToEase(Ease), After),
            .Width = Function(Target, Value, Time, Delay, Ease, After) AaWidth(Target, Value, Time, Delay, ToEase(Ease), After),
            .Height = Function(Target, Value, Time, Delay, Ease, After) AaHeight(Target, Value, Time, Delay, ToEase(Ease), After),
            .GridWidth = Function(Target, Value, Time, Delay, Ease, After) AaGridLengthWidth(Target, Value, Time, Delay, ToEase(Ease), After),
            .TranslateX = Function(Target, Value, Time, Delay, Ease, After) AaTranslateX(Target, Value, Time, Delay, ToEase(Ease), After),
            .Code = Function(Work, Delay, After) AaCode(Sub() Work(), Delay, After),
            .Start = Sub(Frames, Name, RefreshTime) AniStart(Frames, Name, RefreshTime), .Stop = Sub(Name) AniStop(Name), .IsRunning = AddressOf AniIsRun}
    End Function
    Private Shared Function ToEase(Ease As LegacyLaunchEase) As AniEase
        Select Case Ease
            Case LegacyLaunchEase.OutFluent : Return New AniEaseOutFluent
            Case LegacyLaunchEase.InFluent : Return New AniEaseInFluent
            Case LegacyLaunchEase.OutBackWeak : Return New AniEaseOutBack(AniEasePower.Weak)
            Case LegacyLaunchEase.OutFluentWeak : Return New AniEaseOutFluent(AniEasePower.Weak)
            Case LegacyLaunchEase.InFluentWeak : Return New AniEaseInFluent(AniEasePower.Weak)
            Case Else : Return Nothing
        End Select
    End Function
    Friend Shared ReadOnly Skins As New LegacyLaunchSkins(Of LoaderTask(Of (String, String), String))(
        New LegacyLaunchSkinHost(Of LoaderTask(Of (String, String), String)) With {
            .ReadString = Function(Key) Settings.Get(Of String)(Key), .ReadInteger = Function(Key) Settings.Get(Of Integer)(Key),
            .WriteSetting = Sub(Key, Value) Settings.Set(Key, Value),
            .OfflinePageReloaded = Function() FrmLoginLegacy IsNot Nothing AndAlso FrmLoginLegacy.IsReloaded,
            .OfflineName = Function() FrmLoginLegacy.ComboName.Text,
            .Input = Function(Task) Task.Input, .ResetInput = Sub(Task) Task.Input = Nothing, .Output = Sub(Task, Value) Task.Output = Value,
            .Canceled = Function(Task) Task.IsCanceled, .Dispatch = Sub(Work) RunInUi(Work),
            .ClearSkin = AddressOf ClearSkin, .PageExists = AddressOf SkinPageExists, .RenderSkin = AddressOf RenderSkin,
            .ImagePath = Function() PathImage, .AppDataPath = Function() Paths.AppDataThenName,
            .OfflineUuid = AddressOf McLoginLegacyUuid, .SkinSex = AddressOf McSkinSex, .DownloadSkin = AddressOf McSkinDownload,
            .SkinAddress = AddressOf McSkinGetAddress, .MojangUuid = AddressOf McLoginMojangUuid, .Hint = Sub(Text) Hint(Text)})
    Private Shared Sub ClearSkin(Kind As String)
        Select Case Kind
            Case "Legacy" : If FrmLoginLegacy IsNot Nothing AndAlso FrmLoginLegacy.Skin IsNot Nothing Then FrmLoginLegacy.Skin.Clear()
            Case "Ms" : If FrmLoginMsSkin IsNot Nothing AndAlso FrmLoginMsSkin.Skin IsNot Nothing Then FrmLoginMsSkin.Skin.Clear()
            Case "Nide" : If FrmLoginNideSkin IsNot Nothing AndAlso FrmLoginNideSkin.Skin IsNot Nothing Then FrmLoginNideSkin.Skin.Clear()
            Case "Auth" : If FrmLoginAuthSkin IsNot Nothing AndAlso FrmLoginAuthSkin.Skin IsNot Nothing Then FrmLoginAuthSkin.Skin.Clear()
        End Select
    End Sub
    Private Shared Function SkinPageExists(Kind As String) As Boolean
        Select Case Kind
            Case "Legacy" : Return FrmLoginLegacy IsNot Nothing
            Case "Ms" : Return FrmLoginMsSkin IsNot Nothing
            Case "Nide" : Return FrmLoginNideSkin IsNot Nothing
            Case Else : Return FrmLoginAuthSkin IsNot Nothing
        End Select
    End Function
    Private Shared Function RenderSkin(Kind As String) As Action
        Select Case Kind
            Case "Legacy" : Return AddressOf FrmLoginLegacy.Skin.Load
            Case "Ms" : Return AddressOf FrmLoginMsSkin.Skin.Load
            Case "Nide" : Return AddressOf FrmLoginNideSkin.Skin.Load
            Case Else : Return AddressOf FrmLoginAuthSkin.Skin.Load
        End Select
    End Function
    Friend Shared Function ReadPage(Kind As LegacyLoginPageKind) As FrameworkElement
        Select Case Kind
            Case LegacyLoginPageKind.Legacy : Return FrmLoginLegacy
            Case LegacyLoginPageKind.Ms : Return FrmLoginMs
            Case LegacyLoginPageKind.MsSkin : Return FrmLoginMsSkin
            Case LegacyLoginPageKind.Nide : Return FrmLoginNide
            Case LegacyLoginPageKind.NideSkin : Return FrmLoginNideSkin
            Case LegacyLoginPageKind.Auth : Return FrmLoginAuth
            Case LegacyLoginPageKind.AuthSkin : Return FrmLoginAuthSkin
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
    End Function
    Friend Shared Function CreatePage(Kind As LegacyLoginPageKind) As FrameworkElement
        Select Case Kind
            Case LegacyLoginPageKind.Legacy : Return New PageLoginLegacy
            Case LegacyLoginPageKind.Ms : Return New PageLoginMs
            Case LegacyLoginPageKind.MsSkin : Return New PageLoginMsSkin
            Case LegacyLoginPageKind.Nide : Return New PageLoginNide
            Case LegacyLoginPageKind.NideSkin : Return New PageLoginNideSkin
            Case LegacyLoginPageKind.Auth : Return New PageLoginAuth
            Case LegacyLoginPageKind.AuthSkin : Return New PageLoginAuthSkin
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
    End Function
    Friend Shared Sub StorePage(Kind As LegacyLoginPageKind, Page As FrameworkElement)
        Select Case Kind
            Case LegacyLoginPageKind.Legacy : FrmLoginLegacy = CType(Page, PageLoginLegacy)
            Case LegacyLoginPageKind.Ms : FrmLoginMs = CType(Page, PageLoginMs)
            Case LegacyLoginPageKind.MsSkin : FrmLoginMsSkin = CType(Page, PageLoginMsSkin)
            Case LegacyLoginPageKind.Nide : FrmLoginNide = CType(Page, PageLoginNide)
            Case LegacyLoginPageKind.NideSkin : FrmLoginNideSkin = CType(Page, PageLoginNideSkin)
            Case LegacyLoginPageKind.Auth : FrmLoginAuth = CType(Page, PageLoginAuth)
            Case LegacyLoginPageKind.AuthSkin : FrmLoginAuthSkin = CType(Page, PageLoginAuthSkin)
        End Select
    End Sub
    Friend Shared Function Job(Loader As LoaderBase, Optional Items As Func(Of IEnumerable(Of LoaderBase)) = Nothing) As LegacyLaunchJobPort
        Return New LegacyLaunchJobPort With {
            .State = Function() CType(CInt(Loader.State), LegacyLaunchPanelState), .Progress = Function() Loader.Progress,
            .Wait = Sub(Force) Loader.WaitForExit(IsForceRestart:=Force), .Cancel = Sub() Loader.Cancel(),
            .Items = If(Items Is Nothing, Nothing, Function() Items().Select(Function(Value) Item(Value)))}
    End Function
    Friend Shared Function Item(Loader As LoaderBase) As LegacyLaunchItemPort
        If Loader Is Nothing Then Return Nothing
        Return New LegacyLaunchItemPort With {.State = Function() CType(CInt(Loader.State), LegacyLaunchPanelState),
            .Name = Function() Loader.Name, .Parent = Function() Item(Loader.RealParent)}
    End Function
End Class
