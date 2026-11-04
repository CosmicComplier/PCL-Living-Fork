Imports System.Reflection
Imports System.Windows.Documents

'The pinned core renders a separate Popup, bypassing the WPF ToolTip style.
'Only its actual shared card is adapted; core animation, content borrowing and placement stay intact.
Friend Module ModernTooltipTheme
    Private ReadOnly CardField As FieldInfo = GetType(MeloongCore.Wpf.ModernTooltipService).GetField("popupCard", BindingFlags.NonPublic Or BindingFlags.Static)
    Private Installed As Boolean
    Private TrackedCard As Border
    Private TrackedText As TextBlock

    Private Function CurrentCard() As Border
        If CardField Is Nothing OrElse CardField.FieldType IsNot GetType(Border) Then Return Nothing
        Return TryCast(CardField.GetValue(Nothing), Border)
    End Function
    Friend Sub Install()
        If Not Installed Then
            Installed = True
            EventManager.RegisterClassHandler(GetType(Border), FrameworkElement.LoadedEvent, New RoutedEventHandler(AddressOf CardLoaded))
        End If
        ApplyCard(CurrentCard())
    End Sub
    Private Sub ApplyCard(Card As Border)
        If Card Is Nothing Then Return
        Card.SetResourceReference(Border.BackgroundProperty, "ColorBrushPopup")
        Card.SetResourceReference(Border.BorderBrushProperty, "ColorBrushGray5")
        Card.SetResourceReference(TextElement.ForegroundProperty, "ColorBrush1")
        If TypeOf Card.Child Is TextBlock Then DirectCast(Card.Child, TextBlock).SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1")
        If Not ReferenceEquals(TrackedCard, Card) Then
            If TrackedCard IsNot Nothing Then
                RemoveHandler TrackedCard.LayoutUpdated, AddressOf ContentLayoutUpdated
                RemoveHandler TrackedCard.Unloaded, AddressOf CardUnloaded
            End If
            TrackedCard = Card : TrackedText = Nothing
            AddHandler Card.LayoutUpdated, AddressOf ContentLayoutUpdated
            AddHandler Card.Unloaded, AddressOf CardUnloaded
        End If
    End Sub
    Private Sub CardLoaded(Sender As Object, Args As RoutedEventArgs)
        Dim Card = CurrentCard()
        If ReferenceEquals(Sender, Card) Then ApplyCard(Card)
    End Sub
    Private Sub ContentLayoutUpdated(Sender As Object, Args As EventArgs)
        Dim Text = TryCast(TrackedCard?.Child, TextBlock)
        If ReferenceEquals(Text, TrackedText) Then Return
        TrackedText = Text
        If Text IsNot Nothing Then Text.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1")
    End Sub
    Private Sub CardUnloaded(Sender As Object, Args As RoutedEventArgs)
        TrackedText = Nothing
    End Sub
End Module
