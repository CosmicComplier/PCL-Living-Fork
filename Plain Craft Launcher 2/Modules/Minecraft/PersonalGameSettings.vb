Friend Class PersonalOptionsSession
    Inherits PCLCS.PersonalOptionsSessionData
End Class

Friend Module PersonalGameSettings
    Private ReadOnly Flow As New PCLCS.PersonalOptionsFlow(
        LocalGameData.Service, LocalGameData.Gate, Function() HasRunningMinecraft,
        Function(VersionPath) Adapt(New McInstance(VersionPath)), AddressOf Selected,
        AddressOf IniClearCache, Function(First, Second) PathUtils.IsSame(First, Second),
        Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message), Function() New PersonalOptionsSession)

    Friend Function Enabled(Instance As McInstance) As Boolean
        Return Flow.Enabled(Adapt(Instance))
    End Function
    Private Function CanonicalKey(Key As String) As String
        Return PCLCS.PersonalOptionsRules.CanonicalKey(Key)
    End Function
    Private Function Selected(Key As String) As Boolean
        If Key.StartsWith("key_", StringComparison.Ordinal) Then Return Settings.Get(Of Boolean)("LaunchPersonalOptionsKeys") AndAlso PCLCS.PersonalOptionsRules.VanillaBinding(Key)
        If PCLCS.PersonalOptionsRules.IsMouseKey(Key) Then Return Settings.Get(Of Boolean)("LaunchPersonalOptionsMouse")
        If PCLCS.PersonalOptionsRules.IsViewKey(Key) Then Return Settings.Get(Of Boolean)("LaunchPersonalOptionsView")
        If PCLCS.PersonalOptionsRules.IsAccessibilityKey(Key) Then Return Settings.Get(Of Boolean)("LaunchPersonalOptionsAccessibility")
        Return Key.StartsWith("soundCategory_", StringComparison.Ordinal) AndAlso Settings.Get(Of Boolean)("LaunchPersonalOptionsAudio")
    End Function
    Friend Function Parse(Text As String) As Dictionary(Of String, String)
        Return PCLCS.PersonalOptionsRules.Parse(Text)
    End Function
    Private Function ReadOptions(FileName As String) As Byte()
        Return PCLCS.PersonalOptionsFiles.Read(FileName)
    End Function
    Private Function Decode(Bytes As Byte()) As String
        Return PCLCS.PersonalOptionsRules.Decode(Bytes)
    End Function
    Private Function Modern(Instance As McInstance) As Boolean
        Return Flow.Modern(Adapt(Instance))
    End Function
    Private Function CanonicalValue(Key As String, Value As String) As String
        Return PCLCS.PersonalOptionsRules.CanonicalValue(Key, Value)
    End Function
    Private Function ForVersion(Key As String, Value As String, IsModern As Boolean) As String
        Return PCLCS.PersonalOptionsRules.ForVersion(Key, Value, IsModern)
    End Function
    Friend Function MergeText(Text As String, Values As JObject, IsModern As Boolean, Keys As ISet(Of String), Optional FovDegrees As Boolean = True) As String
        Return PCLCS.PersonalOptionsRules.MergeText(Text, Values, IsModern, Keys, AddressOf Selected, FovDegrees)
    End Function
    Friend Function HasProfile() As Boolean
        Return Flow.HasProfile()
    End Function
    Friend Sub ImportFrom(Instance As McInstance)
        Flow.ImportFrom(Adapt(Instance))
    End Sub
    Friend Function Prepare(Instance As McInstance) As PersonalOptionsSession
        Return DirectCast(Flow.Prepare(Adapt(Instance)), PersonalOptionsSession)
    End Function
    Friend Sub Track(Session As PersonalOptionsSession, Game As Process)
        Flow.Track(Session, Game)
    End Sub
    Friend Function NormalizeBinding(Value As String) As String
        Return PCLCS.PersonalOptionsRules.NormalizeBinding(Value)
    End Function
    Friend Function BindingForVersion(Value As String, IsModern As Boolean) As String
        Return PCLCS.PersonalOptionsRules.BindingForVersion(Value, IsModern)
    End Function
    Friend Function VanillaBinding(Key As String) As Boolean
        Return PCLCS.PersonalOptionsRules.VanillaBinding(Key)
    End Function
    Friend Sub CaptureEditedBinding(Instance As McInstance, Key As String, Value As String)
        Flow.CaptureEditedBinding(Adapt(Instance), Key, Value)
    End Sub
    Friend Sub Finish(Session As PersonalOptionsSession)
        Flow.Finish(Session)
    End Sub
    Private Sub Capture(Session As PersonalOptionsSession)
        Flow.Capture(Session)
    End Sub
    Friend Sub RecoverExitedSessions()
        Flow.RecoverExitedSessions()
    End Sub
    Private Function HasActiveSession(GameFolder As String) As Boolean
        Return Flow.HasActiveSession(GameFolder)
    End Function
    Private Function CreateLegacyKeys() As Dictionary(Of Integer, String)
        Return PCLCS.PersonalOptionsRules.CreateLegacyKeys()
    End Function
    Private Function Adapt(Instance As McInstance) As PCLCS.IPersonalOptionsInstance
        Return If(Instance Is Nothing, Nothing, New PersonalOptionsInstance(Instance))
    End Function
    Private NotInheritable Class PersonalOptionsInstance
        Implements PCLCS.IPersonalOptionsInstance
        Private ReadOnly Owner As McInstance
        Friend Sub New(Instance As McInstance)
            Owner = Instance
        End Sub
        Public ReadOnly Property VersionPath As String Implements PCLCS.IPersonalOptionsInstance.VersionPath
            Get
                Return Owner.PathVersion
            End Get
        End Property
        Public ReadOnly Property GameFolder As String Implements PCLCS.IPersonalOptionsInstance.GameFolder
            Get
                Return Owner.PathIndie
            End Get
        End Property
        Public ReadOnly Property SyncEnabled As Boolean Implements PCLCS.IPersonalOptionsInstance.SyncEnabled
            Get
                Return Settings.Get(Of Boolean)("LaunchPersonalOptionsSync") AndAlso Settings.Get(Of Integer)("VersionPersonalOptionsSync", Instance:=Owner) = 0
            End Get
        End Property
        Public ReadOnly Property VersionValid As Boolean Implements PCLCS.IPersonalOptionsInstance.VersionValid
            Get
                Return Owner.Version.Valid
            End Get
        End Property
        Public ReadOnly Property VanillaMajor As Integer Implements PCLCS.IPersonalOptionsInstance.VanillaMajor
            Get
                Return Owner.Version.Vanilla.Major
            End Get
        End Property
    End Class
End Module
