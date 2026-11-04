Imports PCLLF.Presentation.Controls

Public Class MyDropShadow
    Inherits MyDropShadowView

    Public Shared Shadows ReadOnly ColorProperty As DependencyProperty = MyDropShadowView.ColorProperty
    Public Shared Shadows ReadOnly ShadowRadiusProperty As DependencyProperty = MyDropShadowView.ShadowRadiusProperty
    Public Shared Shadows ReadOnly CornerRadiusProperty As DependencyProperty = MyDropShadowView.CornerRadiusProperty
End Class
