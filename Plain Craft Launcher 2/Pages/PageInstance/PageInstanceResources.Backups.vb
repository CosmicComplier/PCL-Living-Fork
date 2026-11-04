Imports ResourceBackupGroup = PCLCS.IResourcePageBackupGroup(Of PCL.ModMinecraft.McInstance)

Partial Public Class PageInstanceResources
    Private Sub BackupAll() Handles BtnBackupAll.Click
        Controller.BackupAll()
    End Sub
    Private Sub RestoreWorldBackup() Handles BtnBackupRestore.Click
        Controller.RestoreWorldBackup()
    End Sub

    Private NotInheritable Class BackupGroupAdapter
        Implements ResourceBackupGroup
        Private ReadOnly Group As WorldBackupGroup
        Public Sub New(SelectionBar As FrameworkElement, Changed As Action, BeforeCreate As Func(Of Boolean))
            Group = New WorldBackupGroup With {.SelectionBar = SelectionBar, .Margin = New Thickness(39, 5, 0, 10), .BeforeCreate = BeforeCreate}
            AddHandler Group.SelectionChanged, Sub() Changed()
        End Sub
        Public ReadOnly Property Element As FrameworkElement Implements ResourceBackupGroup.Element
            Get
                Return Group
            End Get
        End Property
        Public ReadOnly Property FolderName As String Implements ResourceBackupGroup.FolderName
            Get
                Return Group.FolderName
            End Get
        End Property
        Public ReadOnly Property SelectedIds As IEnumerable(Of String) Implements ResourceBackupGroup.SelectedIds
            Get
                Return Group.SelectedIds
            End Get
        End Property
        Public Sub ClearSelection(Animate As Boolean) Implements ResourceBackupGroup.ClearSelection
            Group.ClearSelection(Animate)
        End Sub
        Public Sub Bind(Owner As McInstance, Name As String, Points As List(Of JObject)) Implements ResourceBackupGroup.Bind
            Group.Bind(Owner, Name, Points)
        End Sub
        Public Sub PrefixTitle(Name As String) Implements ResourceBackupGroup.PrefixTitle
            Dim Card = DirectCast(Group.FindName("CardBackups"), MyCard)
            Card.Title = Name & " · " & Card.Title
        End Sub
    End Class

    Private NotInheritable Class CardAdapter
        Implements PCLCS.IResourcePageCard
        Private ReadOnly Card As MyCard
        Private ReadOnly Content As Panel
        Public Sub New(Card As MyCard, Optional Content As Panel = Nothing)
            Me.Card = Card
            Me.Content = If(Content, Card)
        End Sub
        Public ReadOnly Property Element As FrameworkElement Implements PCLCS.IResourcePageCard.Element
            Get
                Return Card
            End Get
        End Property
        Public ReadOnly Property Children As Panel Implements PCLCS.IResourcePageCard.Children
            Get
                Return Content
            End Get
        End Property
        Public Sub InitializeHeader() Implements PCLCS.IResourcePageCard.InitializeHeader
            Dim Header = Card.MainTextBlock
        End Sub
        Public Property Title As String Implements PCLCS.IResourcePageCard.Title
            Get
                Return Card.Title
            End Get
            Set(Value As String)
                Card.Title = Value
            End Set
        End Property
    End Class
    Private Shared Function CreateBackupCard(Content As Panel) As PCLCS.IResourcePageCard
        Dim Card As New MyCard With {.Margin = New Thickness(0, 10, 0, 10), .SwapControl = Content, .IsSwapped = True}
        Card.Children.Add(Content)
        Return New CardAdapter(Card, Content)
    End Function
End Class
