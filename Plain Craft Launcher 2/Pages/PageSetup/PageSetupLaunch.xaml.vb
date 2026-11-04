Imports PCLCS

' Original LF XAML fields/Handles and public entry points remain typed; business lives in C#.
Public Class PageSetupLaunch
    Private _controller As LegacyPageSetupLaunchController(Of McInstance, McSkinInfo)
    Friend ReadOnly Property Controller As LegacyPageSetupLaunchController(Of McInstance, McSkinInfo)
        Get
            If _controller Is Nothing Then
                _controller = New LegacyPageSetupLaunchController(Of McInstance, McSkinInfo)(SetupPageRuntime.Launch, New LegacyPageSetupLaunchView With {
                    .Page = Function() Me,
                    .PanBack = Function() PanBack,
                    .PanMain = Function() PanMain,
                    .CheckPersonalOptionsSync = Function() CheckPersonalOptionsSync,
                    .BtnImportPersonalOptions = Function() BtnImportPersonalOptions,
                    .CardArgument = Function() CardArgument,
                    .LabArgumentIndie = Function() LabArgumentIndie,
                    .ComboArgumentIndieV2 = Function() ComboArgumentIndieV2,
                    .ComboArgumentVisibie = Function() ComboArgumentVisibie,
                    .PanArgumentWindow = Function() PanArgumentWindow,
                    .ComboArgumentWindowType = Function() ComboArgumentWindowType,
                    .TextArgumentWindowWidth = Function() TextArgumentWindowWidth,
                    .LabArgumentWindowMiddle = Function() LabArgumentWindowMiddle,
                    .TextArgumentWindowHeight = Function() TextArgumentWindowHeight,
                    .RadioRamType0 = Function() RadioRamType0,
                    .RadioRamType1 = Function() RadioRamType1,
                    .SliderRamCustom = Function() SliderRamCustom,
                    .CheckArgumentRam = Function() CheckArgumentRam,
                    .PanRamDisplay = Function() PanRamDisplay,
                    .ColumnRamUsed = Function() ColumnRamUsed,
                    .ColumnRamGame = Function() ColumnRamGame,
                    .ColumnRamEmpty = Function() ColumnRamEmpty,
                    .RectRamUsed = Function() RectRamUsed,
                    .RectRamGame = Function() RectRamGame,
                    .RectRamEmpty = Function() RectRamEmpty,
                    .LabRamUsedTitle = Function() LabRamUsedTitle,
                    .LabRamGameTitle = Function() LabRamGameTitle,
                    .LabRamUsed = Function() LabRamUsed,
                    .LabRamTotal = Function() LabRamTotal,
                    .LabRamGame = Function() LabRamGame,
                    .CardSkin = Function() CardSkin,
                    .RadioSkinType0 = Function() RadioSkinType0,
                    .RadioSkinType1 = Function() RadioSkinType1,
                    .RadioSkinType2 = Function() RadioSkinType2,
                    .RadioSkinType3 = Function() RadioSkinType3,
                    .RadioSkinType4 = Function() RadioSkinType4,
                    .PanSkinID = Function() PanSkinID,
                    .BtnSkinSave = Function() BtnSkinSave,
                    .BtnSkinCache = Function() BtnSkinCache,
                    .PanSkinChange = Function() PanSkinChange,
                    .BtnSkinChange = Function() BtnSkinChange,
                    .BtnSkinDelete = Function() BtnSkinDelete,
                    .BtnAdvanceJvmReset = Function() BtnAdvanceJvmReset,
                    .TextAdvanceJvm = Function() TextAdvanceJvm,
                    .TextAdvanceGame = Function() TextAdvanceGame,
                    .TextAdvanceRun = Function() TextAdvanceRun,
                    .CheckAdvanceRunWait = Function() CheckAdvanceRunWait,
                    .PanAdvanceJava = Function() PanAdvanceJava,
                    .ComboAdvanceJava = Function() ComboAdvanceJava,
                    .LabAdvanceJava = Function() LabAdvanceJava,
                    .BtnAdvanceJavaSearch = Function() BtnAdvanceJavaSearch,
                    .BtnSwitch = Function() BtnSwitch
                })
            End If
            Return _controller
        End Get
    End Property

    Private Sub PageSetupLaunch_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub

    Public Sub Refresh()
        Controller.Refresh()
    End Sub

    Public Sub Reset()
        Controller.Reset()
    End Sub

    Private Sub PersonalSyncChanged(Sender As Object, User As Boolean) Handles CheckPersonalOptionsSync.Change
        Controller.PersonalSyncChanged(Sender, User)
    End Sub

    Private Function ImportPersonalOptions() As Boolean
        Return Controller.ImportPersonalOptions()
    End Function

    Private Sub ImportPersonalOptionsClick() Handles BtnImportPersonalOptions.Click
        Controller.ImportPersonalOptionsClick()
    End Sub

    Private Sub BtnSkinChange_Click(sender As Object, e As EventArgs) Handles BtnSkinChange.Click
        Controller.ChangeSkinClick()
    End Sub

    Private Sub RadioSkinType3_Check(sender As Object, e As RouteEventArgs) Handles RadioSkinType4.PreviewCheck
        Controller.PreviewCustomSkin(e.RaiseByMouse, e.Handled)
    End Sub

    Private Function ChangeSkin(SkinInfo As McSkinInfo) As Boolean
        Return Controller.ChangeSkin(SkinInfo)
    End Function

    Private Sub BtnSkinDelete_Click(sender As Object, e As EventArgs) Handles BtnSkinDelete.Click
        Controller.DeleteSkin()
    End Sub

    Private Sub BtnSkinSave_Click(sender As Object, e As EventArgs) Handles BtnSkinSave.Click
        Controller.SaveSkin()
    End Sub

    Private Sub BtnSkinCache_Click(sender As Object, e As EventArgs) Handles BtnSkinCache.Click
        Controller.RefreshSkinCache()
    End Sub

    Public Shared Sub UpdateSkinType()
        SetupPageRuntime.Launch.UpdateSkinType()
    End Sub

    Public Shared Sub UpdateRamType()
        SetupPageRuntime.Launch.UpdateRamType()
    End Sub

    Public Sub RefreshRam(ShowAnim As Boolean)
        Controller.RefreshRam(ShowAnim)
    End Sub

    Private Sub RefreshRam() Handles SliderRamCustom.Change, RadioRamType0.Check, RadioRamType1.Check
        Controller.RefreshRam()
    End Sub

    Private Sub RefreshRamText() Handles RectRamGame.SizeChanged, RectRamEmpty.SizeChanged, LabRamGame.SizeChanged
        Controller.RefreshRamText()
    End Sub

    Public Shared Function GetRam(Instance As McInstance, UseVersionJavaSetup As Boolean) As Double
        Return SetupPageRuntime.Launch.GetRam(Instance, UseVersionJavaSetup)
    End Function

    Public Sub UpdateJavaList()
        Controller.UpdateJavaList()
    End Sub

    Private Sub MoveJavaInList(JavaEntry As Java, Offset As Integer)
        Controller.MoveJavaInList(JavaEntry, Offset)
    End Sub

    Private Sub BtnAdvanceJavaSearch_Click() Handles BtnAdvanceJavaSearch.Click
        Controller.SearchJava()
    End Sub

    Private Sub WindowTypeUIRefresh() Handles ComboArgumentWindowType.SelectionChanged
        Controller.WindowTypeUIRefresh()
    End Sub

    Private Sub ComboArgumentVisibie_SizeChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentVisibie.SelectionChanged
        Controller.VisibilitySelectionChanged(e)
    End Sub

    Private Sub CheckArgumentRam_Change() Handles CheckArgumentRam.Change
        Controller.MemoryOptimizationChanged()
    End Sub

    Private Sub ComboArgumentIndie_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboArgumentIndieV2.SelectionChanged
        Controller.IsolationSelectionChanged()
    End Sub

    Private Sub TextAdvanceRun_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextAdvanceRun.TextChanged
        Controller.AdvanceRunChanged()
    End Sub

    Private Sub TextAdvanceJvm_TextChanged() Handles TextAdvanceJvm.ValidatedTextChanged
        Controller.AdvanceJvmChanged()
    End Sub

    Private Sub BtnAdvanceJvmReset_Click(sender As Object, e As EventArgs) Handles BtnAdvanceJvmReset.Click
        Controller.ResetAdvanceJvm()
    End Sub

    Private Sub ReplaceEnter(sender As MyTextBox, e As TextChangedEventArgs) Handles TextAdvanceJvm.TextChanged, TextAdvanceGame.TextChanged
        Controller.ReplaceEnter(sender, e)
    End Sub

    Private Sub BtnSwitch_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSwitch.Click
        Controller.SwitchToInstance()
    End Sub

End Class
