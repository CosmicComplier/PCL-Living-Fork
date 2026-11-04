Option Strict On
Imports PCLLF.Presentation.Controls

Public Class LazyLoadBehavior
    '真实旧owner注册到同一个DP；不创建第二份附加状态，不把非继承静态别名当作owner。
    Private Shared ReadOnly IsInViewportProperty As DependencyProperty = LazyLoadBehaviorView.IsInViewportProperty.AddOwner(GetType(LazyLoadBehavior))
    Public Shared Sub OnFirstEnterScrollViewerViewport(obj As DependencyObject, value As Action)
        obj.SetValue(IsInViewportProperty, value)
    End Sub
End Class
