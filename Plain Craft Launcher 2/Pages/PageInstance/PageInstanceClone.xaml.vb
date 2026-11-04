Imports ClonePageController = PCLCS.InstanceClonePageController(Of PCL.ModMinecraft.McInstance, PCL.InstanceCloneOptions)
Imports ClonePageHost = PCLCS.InstanceClonePageHost(Of PCL.ModMinecraft.McInstance, PCL.InstanceCloneOptions)

Public Class PageInstanceClone
    Implements IRefreshable
    Private ReadOnly Controller As ClonePageController
    Private ReadOnly ChangedHandler As InstanceClonerUi.ChangedEventHandler = AddressOf Refresh

    Public Sub New()
        InitializeComponent()
        Dim View As New PCLCS.InstanceClonePageView With {
            .NameControl = TextName, .CloneButton = BtnClone, .MigrateButton = BtnMigrate, .OriginCard = CardOrigin,
            .Source = LabSource, .Origin = LabOrigin, .Validation = LabValidation,
            .NameText = Function() TextName.Text, .SetNameText = Sub(Value) TextName.Text = Value,
            .ClearValidationRules = Sub() TextName.ValidateRules.Clear(),
            .AddFolderRule = Sub(Folder) TextName.ValidateRules.Add(New ValidateFolderName(Folder)),
            .ValidateNativeName = Function(Value) ModValidate.Validate(Value, TextName.ValidateRules),
            .CopySaves = Function() CheckSaves.Checked, .CopyScreenshots = Function() CheckScreenshots.Checked,
            .CopyServers = Function() CheckServers.Checked, .ScrollHome = Sub() PanBack.ScrollToHome()}
        Dim Host As New ClonePageHost With {
            .CurrentInstance = Function() PageInstanceLeft.Instance,
            .Name = Function(Instance) Instance.Name, .VersionPath = Function(Instance) Instance.PathVersion,
            .SourceVersionFolder = AddressOf InstanceCloner.SourceVersionFolder, .SafeName = AddressOf InstanceResourceStore.SafeName,
            .IsBusy = Function() InstanceClonerUi.IsBusy, .BindChanged = AddressOf BindChanged,
            .CreateOptions = Function() New InstanceCloneOptions,
            .Start = AddressOf InstanceClonerUi.Start, .Migrate = AddressOf InstanceClonerUi.Migrate}
        Controller = New ClonePageController(View, Host)
    End Sub
    Private Sub BindChanged(Attach As Boolean)
        If Attach Then
            AddHandler InstanceClonerUi.Changed, ChangedHandler
        Else
            RemoveHandler InstanceClonerUi.Changed, ChangedHandler
        End If
    End Sub
    Private Property Owner As McInstance
        Get
            Return Controller.Owner
        End Get
        Set(Value As McInstance)
            Controller.Owner = Value
        End Set
    End Property
    Private Sub PageLoaded() Handles Me.Loaded
        Controller.PageLoaded()
    End Sub
    Private Sub PageUnloaded() Handles Me.Unloaded
        Controller.PageUnloaded()
    End Sub
    Public Sub Refresh() Implements IRefreshable.Refresh
        Controller.Refresh()
    End Sub
    Private Sub NameChanged() Handles TextName.TextChanged
        Controller?.ValidateName()
    End Sub
    Private Sub ValidateName()
        Controller.ValidateName()
    End Sub
    Private Sub CloneVersion() Handles BtnClone.Click
        Controller.CloneVersion()
    End Sub
    Private Sub MigrateVersion() Handles BtnMigrate.Click
        Controller.MigrateVersion()
    End Sub
    Private Sub UpdateTaskState()
        Controller.UpdateTaskState()
    End Sub
End Class
