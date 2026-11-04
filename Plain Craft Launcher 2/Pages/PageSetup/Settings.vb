Imports PCLCS

'LF 的原名记录、枚举、回调与实际宿主边界；全部存储/转换/迁移业务在 C#。
Partial Public Class Settings
    Private Shared ReadOnly Backend As New LegacySettingsEngine(Of McInstance, Setting, PclAccountMigrationPlan)(
        Function() Entries, Function() SharedAccountSync, Function() New PclAccountMigrationPlan(),
        New LegacySettingsHost(Of McInstance) With {
            .InstancePath = Function(Instance) Instance.PathVersion,
            .Identify = Function() Identify,
            .RegistryFolder = Function() RegFolder,
            .ReadIni = Function(File, Key, Fallback) ModBase.ReadIni(File, Key, Fallback),
            .WriteIni = Sub(File, Key, Value) ModBase.WriteIni(File, Key, Value),
            .DeleteIni = Sub(File, Key) ModBase.DeleteIniKey(File, Key),
            .HasIni = Function(File, Key) ModBase.HasIniKey(File, Key),
            .ReadRegistry = Function(Folder, Key, Fallback) CStr(RegistryUtils.TryRead(Folder, Key, Fallback)),
            .WriteRegistry = Sub(Folder, Key, Value) RegistryUtils.Write(Folder, Key, Value),
            .HasRegistry = Function(Folder, Key) RegistryUtils.Has(Folder, Key),
            .OpenRegistry = AddressOf LegacySettingsRegistryStore.Open,
            .IdentifyCandidates = AddressOf LegacySettingsRegistryStore.IdentifyCandidates,
            .Log = AddressOf LogSettings
        })

    Private Shared Sub LogSettings(ErrorValue As Exception, Message As String, Level As LegacySettingsLogLevel, Toast As Boolean)
        Select Case Level
            Case LegacySettingsLogLevel.Trace : Logger.Trace(Message)
            Case LegacySettingsLogLevel.Info : Logger.Info(Message)
            Case LegacySettingsLogLevel.Warning
                If ErrorValue Is Nothing Then
                    If Toast Then Logger.Warn(Message, LogBehavior.Toast) Else Logger.Warn(Message)
                Else
                    If Toast Then Logger.Warn(ErrorValue, Message, LogBehavior.Toast) Else Logger.Warn(ErrorValue, Message)
                End If
            Case LegacySettingsLogLevel.Error
                If ErrorValue Is Nothing Then
                    If Toast Then Logger.Error(Message, LogBehavior.Toast) Else Logger.Error(Message)
                Else
                    If Toast Then Logger.Error(ErrorValue, Message, LogBehavior.Toast) Else Logger.Error(ErrorValue, Message)
                End If
        End Select
    End Sub

    Friend Shared Function IsPclAccountSetting(Entry As Setting) As Boolean
        Return LegacySettingsEngine(Of McInstance, Setting, PclAccountMigrationPlan).IsAccountSetting(Entry)
    End Function
    Friend Shared Function GetPclSourceIdentify(HardwareConfig As String, Seed As String) As String
        Return LegacySettingsCrypto.SourceIdentify(HardwareConfig, Seed)
    End Function
    Private Shared Function DecodePclAccountValue(Entry As Setting, RawValue As String, DeviceIdentify As String) As String
        Return Backend.DecodeAccountValue(Entry, RawValue, DeviceIdentify)
    End Function
    Private Shared Function RegistryFolderFor(Entry As Setting) As String
        Return Backend.RegistryFolderFor(Entry)
    End Function
    Friend Shared Function PclIdentifyCandidates(Seed As String) As List(Of String)
        Return LegacySettingsRegistryStore.IdentifyCandidates(Seed)
    End Function
    Private Shared Function DecodePclAccountWithKeys(Entry As Setting, RawValue As String, Keys As IEnumerable(Of String)) As String
        Return Backend.DecodeAccountWithKeys(Entry, RawValue, Keys)
    End Function
    Private Shared Function AccountGroup(Key As String) As String
        Return LegacySettingsEngine(Of McInstance, Setting, PclAccountMigrationPlan).AccountGroup(Key)
    End Function
    Friend Class PclAccountMigrationPlan
        Inherits LegacyAccountMigrationPlan
    End Class
    Friend Shared Function PlanPclAccountMigration(Official As IDictionary(Of String, String), Legacy As IDictionary(Of String, String),
                                                  OfficialKeys As IEnumerable(Of String), LegacyKeys As IEnumerable(Of String), CurrentIdentify As String) As PclAccountMigrationPlan
        Return Backend.PlanAccountMigration(Official, Legacy, OfficialKeys, LegacyKeys, CurrentIdentify)
    End Function
    Friend Shared Sub ApplyPclAccountMigration(Plan As PclAccountMigrationPlan, Official As Microsoft.Win32.RegistryKey, Legacy As Microsoft.Win32.RegistryKey)
        Backend.ApplyAccountMigration(Plan, New LegacySettingsRegistryStore(Official), If(Legacy Is Nothing, Nothing, New LegacySettingsRegistryStore(Legacy)))
    End Sub
    Friend Shared Sub ImportCompatiblePclAccounts()
        Backend.ImportCompatibleAccounts()
    End Sub
    Private Shared Sub ImportCompatiblePclAccountsCore()
        Backend.ImportCompatibleAccountsCore()
    End Sub
    Friend Shared Function IsCompatiblePclSetting(Entry As Setting) As Boolean
        Return LegacySettingsEngine(Of McInstance, Setting, PclAccountMigrationPlan).IsCompatibleSetting(Entry)
    End Function
    Private Shared Function ReadRegistrySetting(Entry As Setting, DefaultValue As String) As String
        Return Backend.ReadRegistrySetting(Entry, DefaultValue)
    End Function

    Public Shared ReadOnly Entries As Dictionary(Of String, Setting) = LegacySettingsDefinitions.Create(Of Setting)(
        Function(Key, Value, Source, Encrypted, Changed) New Setting(Key, Value, CType(Source, Sources), Encrypted, Changed),
        AddressOf EntryHostDefault, EntryCallbacks())

    Private Shared Function EntryCallbacks() As Dictionary(Of String, Action(Of Object))
        Return New Dictionary(Of String, Action(Of Object)) From {
            {"SystemDebugMode", Sub() Logger.Instance.MinLevel = If(ModeDebug, LogLevel.Trace, LogLevel.Info)},
            {"LivingModpackMode", AddressOf ModModpackMode.OnChanged},
            {"LaunchSkinID", Sub() PageLaunchLeft.SkinLegacy.Start()},
            {"LaunchSkinType", AddressOf PageSetupLaunch.UpdateSkinType},
            {"LaunchRecentWorlds", AddressOf QuickPlayWorlds.OnEnabledChanged},
            {"LaunchModDependencyCheck", Sub() LaunchDependencyCheck.SettingsChanged()},
            {"LaunchModDependencyAutoFill", Sub() LaunchDependencyCheck.SettingsChanged()},
            {"LaunchRamType", AddressOf PageSetupLaunch.UpdateRamType},
            {"ToolDownloadSpeed", AddressOf ModNet.UpdateNetTaskSpeedLimitHigh},
            {"ToolDownloadCert", AddressOf ModNet.ShouldValidateSslCertificateOnLogin},
            {"UiLauncherTransparent", Sub(Value As Integer) If FrmMain IsNot Nothing Then FrmMain.Opacity = Value / 1000 + 0.4},
            {"UiLauncherTheme", AddressOf ThemeRefresh},
            {"UiBackgroundMode", Sub() PageSetupUI.BackgroundRefresh(False, True)},
            {"UiBackgroundColorful", Sub() ThemeRefresh()},
            {"UiBackgroundOpacity", AddressOf FormMain.UpdateBackgroundAndTitleBar},
            {"UiBackgroundBlur", AddressOf FormMain.UpdateBackgroundAndTitleBar},
            {"UiBackgroundSuit", AddressOf FormMain.UpdateBackgroundAndTitleBar},
            {"UiCustomType", AddressOf PageSetupUI.OnMainPageTypeChanged},
            {"UiLogoType", AddressOf FormMain.UpdateBackgroundAndTitleBar},
            {"UiLogoText", AddressOf FormMain.UpdateBackgroundAndTitleBar},
            {"UiLogoLeft", AddressOf FormMain.UpdateBackgroundAndTitleBar},
            {"UiHiddenPageDownload", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenPageLink", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenPageSetup", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenPageOther", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenFunctionSelect", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenFunctionModUpdate", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenFunctionInstanceModify", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenFunctionHidden", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenSetupLaunch", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenSetupUi", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenSetupLink", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenSetupSystem", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenOtherHelp", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenOtherFeedback", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenOtherVote", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenOtherAbout", AddressOf PageSetupUI.HiddenRefresh},
            {"UiHiddenOtherTest", AddressOf PageSetupUI.HiddenRefresh},
            {"VersionRamType", AddressOf PageInstanceSetup.OnVersionRamTypeChanged},
            {"VersionServerLogin", AddressOf PageInstanceSetup.OnVersionServerLoginChanged}
        }
    End Function

    Private Shared Function EntryHostDefault(Key As String) As Object
        Select Case Key
            Case "LoginType" : Return McLoginType.Legacy
            Case "UiLauncherHue" : Return DefaultThemeHue
            Case "UiLauncherSat" : Return DefaultThemeSat
            Case "UiLauncherDelta" : Return DefaultThemeDelta
            Case "UiLauncherLight" : Return DefaultThemeLight
            Case "UiBackgroundOpacity" : Return DefaultBackgroundOpacity
            Case Else : Throw New KeyNotFoundException(Key)
        End Select
    End Function

    Public Enum Sources
        Normal
        Registry
        Instance
    End Enum
    Public Class Setting
        Inherits LegacySettingData(Of McInstance)
        Public Source As Sources
        Public Sub New(Key As String, Value As Object, Optional Source As Sources = Sources.Normal, Optional Encrypted As Boolean = False, Optional OnChanged As Action(Of Object) = Nothing)
            MyBase.New(Key, Value, Encrypted, OnChanged, Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message))
            Me.Source = Source
        End Sub
        Public Overrides Function GetSourceCode() As LegacySettingSource
            Return CType(Source, LegacySettingSource)
        End Function
        Protected Overrides Function GetInstancePath(Instance As McInstance) As String
            Return Instance.PathVersion
        End Function
        Public Overrides Sub Save(Optional Instance As McInstance = Nothing)
            Backend.SaveEntry(Me, Instance)
        End Sub
    End Class

    Public Shared Sub [Set](Key As String, Value As Object, Optional Instance As McInstance = Nothing)
        Backend.Set(Key, Value, Instance)
    End Sub
    Private Shared Sub SetCore(Key As String, Value As Object, Instance As McInstance)
        Backend.SetCore(Key, Value, Instance)
    End Sub
    Public Shared Sub SetSafe(Key As String, Value As Object, Optional Instance As McInstance = Nothing)
        Backend.SetSafe(Key, Value, Instance)
    End Sub
    Public Shared Function [Get](Key As String, Optional Instance As McInstance = Nothing)
        Return Backend.Get(Key, Instance)
    End Function
    Private Shared Function GetCore(Key As String, Instance As McInstance, Optional ReadRaw As Func(Of Setting, String) = Nothing, Optional DeviceIdentify As String = Nothing) As Object
        Return Backend.GetCore(Key, Instance, ReadRaw, DeviceIdentify)
    End Function
    Public Shared Function [Get](Of T)(Key As String, Optional Instance As McInstance = Nothing) As T
        Return [Get](Key, Instance)
    End Function
    Public Shared Function GetSafe(Key As String, Optional Instance As McInstance = Nothing)
        Return Backend.GetSafe(Key, Instance)
    End Function
    Public Shared Sub Reset(Key As String, Optional Instance As McInstance = Nothing)
        Backend.Reset(Key, Instance)
    End Sub
    Private Shared Sub ResetCore(Key As String, Instance As McInstance)
        Backend.ResetCore(Key, Instance)
    End Sub
    Public Shared Function GetDefault(Key As String) As String
        Return Backend.GetDefault(Key)
    End Function
    Public Shared Function HasSaved(Key As String, Optional Instance As McInstance = Nothing) As Boolean
        Return Backend.HasSaved(Key, Instance)
    End Function
    Private Shared Function DesEncrypt(sourceString As String, Optional key As String = Nothing) As String
        Return LegacySettingsCrypto.Encrypt(sourceString, key)
    End Function
    Private Shared Function DesDecrypt(encryptedString As String, Optional key As String = Nothing) As String
        Return LegacySettingsCrypto.Decrypt(encryptedString, key)
    End Function
End Class
