Imports System.Windows.Media
Imports Inspector = PCL.ModDependencyInspector
Imports CheckController = PCLCS.ModDependencyCheckViewController(Of PCL.ModDependencyInspector.InspectionContext, PCL.ModDependencyInspector.ModInfo, PCL.ModDependencyInspector.Dependency, PCL.ModDependencyInspector.DependencyEdge, PCL.ModDependencyInspector.InspectionIssue, PCL.ModDependencyInspector.RemovalImpact, PCL.ModDependencyInspector.InspectionReport, PCL.ModDependencyInspector.OnlineProjectInfo, PCL.ModDependencyInspector.OnlineDependency, PCL.ModDependencyInspector.OnlineFileInfo, PCL.ResourceVersion, PCL.ModDependencyAutoFill.Item, PCL.ModDependencyActivation.Item, PCL.ModDependencyAutoFill.Plan, PCL.MyCard)
Imports CheckHost = PCLCS.ModDependencyCheckViewHost(Of PCL.ResourceVersion, PCL.ModDependencyInspector.InspectionIssue, PCL.MyCard)

Friend NotInheritable Class ModDependencyCheckView
    Inherits Grid

    Friend ReadOnly Property PreferredWidth As Double = CheckController.PreferredWidth
    Friend ReadOnly Groups As New StackPanel
    Friend ReadOnly Details As MyCard

    Friend Sub New(Report As Inspector.InspectionReport, Optional Plan As ModDependencyAutoFill.Plan = Nothing)
        Details = CheckController.Build(Me, Groups, Report, Plan, CreateHost())
    End Sub

    '仅原 LF 自定义控件、实际旧 DTO 及资源平台原语，不保留第二份分组／确认逻辑。
    Private Shared Function CreateHost() As CheckHost
        Return New CheckHost With {
            .VersionDisplay = Function(Version) Version.Display,
            .IssueKey = AddressOf ModDependencyActivation.IssueKey,
            .CreateCard = Function(Title, Margin) New MyCard With {.Title = Title, .Margin = Margin},
            .AddCardChild = Sub(Card, Child) Card.Children.Add(Child),
            .CollapseDetails = AddressOf CollapseDetails,
            .EnsureCaption = Sub(Card)
                                 Dim Caption = Card.MainTextBlock
                             End Sub,
            .CreateScroll = Function(Content) New MyScrollViewer With {.Content = Content},
            .CreateWarningHint = Function(Text, Margin) New MyHint With {.Text = Text, .Theme = MyHint.Themes.Yellow, .Margin = Margin},
            .DefaultIcon = Function() CType(New MyBitmap(PathImage & "Icons/NoIcon.png"), ImageSource)}
    End Function
    Private Shared Sub CollapseDetails(Card As MyCard, Content As UIElement)
        Card.CanSwap = True
        Card.Children.Add(Content)
        Card.SwapControl = DirectCast(Content, FrameworkElement)
        Card.IsSwapped = True
    End Sub
End Class
