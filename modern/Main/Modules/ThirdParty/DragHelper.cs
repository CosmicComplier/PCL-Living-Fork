// 来源：https://blog.csdn.net/simpleman2000/article/details/140952294
// Preserve the LF event, POINT and property identities; the production C# runtime owns Win32 policy.
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using PCLCS.Startup;
using PCLLF.Presentation.Windowing;

namespace PCL
{
    public class DragHelper
    {
        public event EventHandler DragDrop;
        private string[] _DropFilePathsBackingField;
        public string[] DropFilePaths
        {
            get => _DropFilePathsBackingField;
            private set => _DropFilePathsBackingField = value;
        }
        private POINT _DropPointBackingField;
        public POINT DropPoint
        {
            get => _DropPointBackingField;
            private set => _DropPointBackingField = value;
        }
        public HwndSource HwndIntPtrSource { get; set; }
        private readonly LegacyDragHelperRuntime Service;

        public DragHelper()
        {
            Service = new LegacyDragHelperRuntime(new LegacyDragHelperHost
            {
                Source = () => HwndIntPtrSource,
                WritePoint = value => DropPoint = new POINT { X = value.X, Y = value.Y },
                WriteFiles = value => DropFilePaths = value,
                EmitDrop = PublishDrop
            });
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private void PublishDrop() => DragDrop?.Invoke(this, EventArgs.Empty);
        public void AddHook() => Service.AddHook(WndProc);
        public void RemoveDragHook() => Service.RemoveDragHook(WndProc);
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            => Service.WndProc(hwnd, msg, wParam, lParam, ref handled);
        private static void ChangeMessageFilter(IntPtr handle) => LegacyDragHelperRuntime.ChangeMessageFilter(handle);
        private static bool TryGetDropInfo(int msg, IntPtr wParam, ref string[] dropFilePaths, ref POINT dropPoint)
        {
            dropPoint = new POINT();
            var nativePoint = new LegacyDropPoint();
            try
            {
                return LegacyDragHelperRuntime.TryGetDropInfo(msg, wParam, ref dropFilePaths, ref nativePoint);
            }
            finally
            {
                dropPoint = new POINT { X = nativePoint.X, Y = nativePoint.Y };
            }
        }
    }
}
