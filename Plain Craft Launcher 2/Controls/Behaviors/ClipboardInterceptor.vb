Option Strict On
Imports PCLLF.Presentation.Controls

'Original Author uye / MaaAssistantArknights; Apache-2.0 for this file.
'Exact author relicensing notice remains in the migrated C# source.
Namespace Controls.Behaviors
    Public NotInheritable Class ClipboardInterceptor
        Private Sub New()
        End Sub
        Shared Sub New()
            ClipboardInterceptorView.ConfigureWarnings(Sub(ErrorValue, Message, Toast)
                                                          If Toast Then
                                                              Logger.Warn(ErrorValue, Message, LogBehavior.Toast)
                                                          Else
                                                              Logger.Warn(ErrorValue, Message)
                                                          End If
                                                      End Sub)
        End Sub
        Public Shared ReadOnly EnableSafeClipboardProperty As DependencyProperty = ClipboardInterceptorView.EnableSafeClipboardProperty
        Public Shared Sub SetEnableSafeClipboard(Element As DependencyObject, Value As Boolean)
            ClipboardInterceptorView.SetEnableSafeClipboard(Element, Value)
        End Sub
        Public Shared Function GetEnableSafeClipboard(Element As DependencyObject) As Boolean
            Return ClipboardInterceptorView.GetEnableSafeClipboard(Element)
        End Function
    End Class
End Namespace
