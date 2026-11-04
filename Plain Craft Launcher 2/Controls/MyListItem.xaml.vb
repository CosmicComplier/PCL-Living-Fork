Imports System.Windows.Markup
Imports PCLLF.Presentation.Controls

<ContentProperty("Inlines")>
Public Class MyListItem
    Inherits MyListItemView
    Implements IMyRadio

    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shared Shadows ReadOnly TitleProperty As DependencyProperty = MyListItemView.TitleProperty
    Public Shared Shadows ReadOnly FontSizeProperty As DependencyProperty = MyListItemView.FontSizeProperty
    Public Shared Shadows ReadOnly ForegroundProperty As DependencyProperty = MyListItemView.ForegroundProperty.AddOwner(GetType(MyListItem), New PropertyMetadata(CType(Color1, SolidColorBrush)))
    Public Shadows Event Click(sender As Object, e As MouseButtonEventArgs)
    Public Shadows Event LogoClick(sender As Object, e As MouseButtonEventArgs)
    Public Shadows Event Check(sender As Object, e As RouteEventArgs) Implements IMyRadio.Check
    Public Shadows Event Changed(sender As Object, e As RouteEventArgs) Implements IMyRadio.Changed
    Public Shadows Enum CheckType
        None
        Clickable
        RadioBox
        CheckBox
    End Enum
    Public Shadows Property Type As CheckType
        Get
            Return CType(CInt(MyBase.Type), CheckType)
        End Get
        Set(value As CheckType)
            MyBase.Type = CType(CInt(value), MyListItemView.CheckType)
        End Set
    End Property
    Public Shadows Property Buttons As IEnumerable(Of MyIconButton)
        Get
            Return DirectCast(MyBase.Buttons, IEnumerable(Of MyIconButton))
        End Get
        Set(value As IEnumerable(Of MyIconButton))
            MyBase.Buttons = value
        End Set
    End Property
    Public Shadows ContentHandler As Action(Of MyListItem, EventArgs)
    Protected Overrides ReadOnly Property HasContentHandler As Boolean
        Get
            Return ContentHandler IsNot Nothing
        End Get
    End Property
    Protected Overrides Sub InvokeContentHandler(Args As EventArgs)
        ContentHandler(Me, Args)
    End Sub
    Protected Overrides Sub ClearContentHandler()
        ContentHandler = Nothing
    End Sub
    Protected Overrides Sub OnClick(sender As Object, Args As MouseButtonEventArgs)
        RaiseEvent Click(sender, Args)
    End Sub
    Protected Overrides Sub OnLogoClick(sender As Object, Args As MouseButtonEventArgs)
        RaiseEvent LogoClick(sender, Args)
    End Sub
    Protected Overrides Function OnCheck(User As Boolean) As Boolean
        Dim e As New RouteEventArgs(User)
        RaiseEvent Check(Me, e)
        Return e.Handled
    End Function
    Protected Overrides Function OnChanged(User As Boolean) As Boolean
        Dim e As New RouteEventArgs(User)
        RaiseEvent Changed(Me, e)
        Return e.Handled
    End Function
    Protected Overrides Function WpfSize(Pixels As Double) As Double
        Return GetWPFSize(Pixels)
    End Function
    Protected Overrides Function CreateOnlineImage(Address As String) As FrameworkElement
        Return New MyImage With {.Source = Address}
    End Function
    Protected Overrides Function ReadBitmap(Address As String) As Brush
        Return CType(New MyBitmap(Address), Brush)
    End Function
    Protected Overrides Function ReadIdleBrush(Hover As Boolean) As Brush
        Return CType(If(Hover, ColorBg1, Color7), Brush)
    End Function
    Protected Overrides Function HasCustomEvent(sender As Object) As Boolean
        Return CustomEventService.GetEventType(DirectCast(sender, DependencyObject)) <> CustomEvent.EventType.None OrElse CustomEventService.GetEvents(DirectCast(sender, DependencyObject)).Events.Count > 0
    End Function
    Protected Overrides ReadOnly Property IsHelpEvent As Boolean
        Get
            Return CustomEventService.GetEventType(Me) = CustomEvent.EventType.打开帮助
        End Get
    End Property
    Protected Overrides Sub PopulateHelpItem()
        Dim Entry As New HelpEntry(CustomEvent.GetAbsoluteUrls(CustomEventService.GetEventData(Me), CustomEventService.GetEventType(Me))(0))
        Entry.SetToListItem(Me)
    End Sub
    Protected Overrides Sub ClearHelpEvent()
        CustomEventService.SetEventType(Me, CustomEvent.EventType.None)
        CustomEventService.SetEventData(Me, "")
    End Sub
    Protected Overrides Sub ReportHelpError(ErrorValue As Exception)
        Logger.Error(ErrorValue, "设置帮助 MyListItem 失败", LogBehavior.Alert)
    End Sub
End Class
