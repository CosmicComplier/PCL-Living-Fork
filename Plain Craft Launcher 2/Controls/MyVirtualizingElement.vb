Imports PCLLF.Presentation.Controls

Public Class MyVirtualizingElement(Of T As FrameworkElement)
    Inherits MyVirtualizingElementView(Of T)

    Public Sub New(Initializer As Func(Of T))
        MyBase.New(Initializer)
    End Sub
    Public Shared Overloads Widening Operator CType(Virtualized As MyVirtualizingElement(Of T)) As T
        Return Virtualized.Init()
    End Operator
End Class

Public Class MyVirtualizingElement
    Inherits MyVirtualizingElementView

    Public Sub New(Initializer As Func(Of FrameworkElement))
        MyBase.New(Initializer)
    End Sub
    Public Shared Shadows Function TryInit(Element As FrameworkElement) As FrameworkElement
        Return MyVirtualizingElementView.TryInit(Element)
    End Function
End Class
