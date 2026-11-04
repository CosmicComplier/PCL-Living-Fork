using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
namespace PclLfNativeSmoke {
 public sealed class WindowRow {
  public IntPtr Handle; public string Title; public string ClassName;
  public int Left,Top,Right,Bottom;
 }
 public sealed class ScreenshotRow {public bool Saved;public bool NonuniformSample;public string Error;}
 public static class Win32 {
  private delegate bool EnumCallback(IntPtr hwnd,IntPtr state);
  [DllImport("user32.dll")]private static extern bool EnumWindows(EnumCallback callback,IntPtr state);
  [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint id);
  [DllImport("user32.dll")]private static extern bool IsWindowVisible(IntPtr hwnd);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetWindowText(IntPtr hwnd,StringBuilder text,int count);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr hwnd,StringBuilder text,int count);
  [StructLayout(LayoutKind.Sequential)]private struct Rect {public int Left,Top,Right,Bottom;}
  [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
  [DllImport("user32.dll")]private static extern bool PrintWindow(IntPtr hwnd,IntPtr hdc,uint flags);
  [DllImport("user32.dll",SetLastError=true)]private static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr first,IntPtr second);
  public static bool PostClose(IntPtr hwnd) {return PostMessage(hwnd,0x0010,IntPtr.Zero,IntPtr.Zero);}
  public static WindowRow[] Windows(int processId) {
   List<WindowRow> result=new List<WindowRow>();
   EnumWindows(delegate(IntPtr hwnd,IntPtr unused) {
    uint owner;GetWindowThreadProcessId(hwnd,out owner);
    if(owner!=(uint)processId || !IsWindowVisible(hwnd))return true;
    StringBuilder title=new StringBuilder(1024),type=new StringBuilder(256);Rect rect;
    GetWindowText(hwnd,title,title.Capacity);GetClassName(hwnd,type,type.Capacity);GetWindowRect(hwnd,out rect);
    result.Add(new WindowRow {Handle=hwnd,Title=title.ToString(),ClassName=type.ToString(),Left=rect.Left,Top=rect.Top,Right=rect.Right,Bottom=rect.Bottom});return true;
   },IntPtr.Zero);return result.ToArray();
  }
  public static ScreenshotRow CaptureOwnWindow(IntPtr hwnd,string fileName) {
   ScreenshotRow result=new ScreenshotRow();
   try {
    Rect rect;if(!GetWindowRect(hwnd,out rect))throw new InvalidOperationException("Window bounds unavailable.");
    int width=rect.Right-rect.Left,height=rect.Bottom-rect.Top;if(width<1 || height<1 || width>8192 || height>8192)throw new InvalidOperationException("Unsupported screenshot bounds.");
    using(Bitmap bitmap=new Bitmap(width,height)) {
     using(Graphics graphics=Graphics.FromImage(bitmap)) {
      IntPtr hdc=graphics.GetHdc();bool printed;try{printed=PrintWindow(hwnd,hdc,2);}finally{graphics.ReleaseHdc(hdc);}
      if(!printed)throw new InvalidOperationException("PrintWindow did not capture this product window.");
     }
     int first=bitmap.GetPixel(0,0).ToArgb();for(int y=0;y<height;y+=Math.Max(1,height/16))for(int x=0;x<width;x+=Math.Max(1,width/16))if(bitmap.GetPixel(x,y).ToArgb()!=first)result.NonuniformSample=true;
     bitmap.Save(fileName,ImageFormat.Png);result.Saved=true;
    }
   }catch(Exception error){result.Error=error.GetType().Name+": "+error.Message;}
   return result;
  }
 }
}
