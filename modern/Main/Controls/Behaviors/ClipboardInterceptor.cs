using System.Windows;
using MeloongCore;
using PCLLF.Presentation.Controls;

// Original Author uye / MaaAssistantArknights; Apache-2.0 for this file.
// Exact author relicensing notice remains in the migrated C# source.
namespace PCL.Controls.Behaviors
{
    public sealed class ClipboardInterceptor
    {
        private ClipboardInterceptor()
        {
        }
        static ClipboardInterceptor()
        {
            ClipboardInterceptorView.ConfigureWarnings((ErrorValue, Message, Toast) => { if (Toast) { Logger.Warn(ErrorValue, Message, LogBehavior.Toast); } else { Logger.Warn(ErrorValue, Message); } });
        }
        public static readonly DependencyProperty EnableSafeClipboardProperty = ClipboardInterceptorView.EnableSafeClipboardProperty.AddOwner(typeof(ClipboardInterceptor));
        public static void SetEnableSafeClipboard(DependencyObject Element, bool Value)
        {
            ClipboardInterceptorView.SetEnableSafeClipboard(Element, Value);
        }
        public static bool GetEnableSafeClipboard(DependencyObject Element)
        {
            return ClipboardInterceptorView.GetEnableSafeClipboard(Element);
        }
    }
}