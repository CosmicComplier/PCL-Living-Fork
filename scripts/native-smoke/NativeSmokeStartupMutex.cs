using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace PclLfNativeSmoke {
 // Only the observer's own invisible HWND and two initially absent launcher
 // mutexes. The genuine separate product process still runs its original
 // WaitingMutex -> FindWindowW -> PostMessageW(6402) -> Exit(2) path.
 public sealed class StartupMutexFixture : IDisposable {
  private readonly uint thread=GetCurrentThreadId();
  private readonly List<Mutex> held=new List<Mutex>();
  private readonly Callback callback;
  private readonly string className;
  private readonly IntPtr module;
  private IntPtr window;
  private ushort atom;
  private int activationMessages;
  private bool disposed;
  public StartupMutexFixture(string fixtureId) {
   Guid owner;if(!Guid.TryParseExact(fixtureId,"N",out owner) || owner.ToString("N")!=fixtureId)throw new ArgumentException("Expected the current owned GUID fixture.","fixtureId");
   className="PclLfNativeSmokeActivation-"+fixtureId;callback=WindowProc;module=GetModuleHandleW(null);
   try {
    foreach(string caption in new[]{"LivingPCL\u3000","Plain Craft Launcher\u3000","Plain Craft Launcher 2\u3000"})if(FindWindowW(null,caption)!=IntPtr.Zero)throw new InvalidOperationException("Refusing a pre-existing activation HWND.");
    foreach(string name in new[]{"PCL_SingletonMutex","LivingPCL_SingletonMutex"}) {
     bool created;Mutex gate=new Mutex(true,name,out created);
     if(!created){gate.Dispose();throw new InvalidOperationException("Refusing an existing launcher mutex: "+name);}held.Add(gate);
    }
    WindowClass cls=new WindowClass();cls.Size=(uint)Marshal.SizeOf(typeof(WindowClass));cls.Procedure=Marshal.GetFunctionPointerForDelegate(callback);cls.Instance=module;cls.ClassName=className;
    atom=RegisterClassExW(ref cls);if(atom==0)throw new Win32Exception(Marshal.GetLastWin32Error(),"Owned activation class registration failed.");
    window=CreateWindowExW(0,className,"LivingPCL\u3000",0,0,0,1,1,IntPtr.Zero,IntPtr.Zero,module,IntPtr.Zero);
    if(window==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error(),"Owned invisible activation HWND creation failed.");
    uint pid;uint tid=GetWindowThreadProcessId(window,out pid);
    if(pid!=(uint)Process.GetCurrentProcess().Id || tid!=thread)throw new InvalidOperationException("Activation HWND does not belong to the verified observer thread/process.");
   }catch {Dispose();throw;}
  }
  public long Handle {get{return window.ToInt64();}}
  public int ActivationMessages {get{return Volatile.Read(ref activationMessages);}}
  public string Caption {get{return "LivingPCL\u3000";}}
  public void PumpOwnMessages() {
   CheckThread();Message row;Stopwatch limit=Stopwatch.StartNew();int count=0;
   while(window!=IntPtr.Zero && count++<64 && limit.ElapsedMilliseconds<25 && PeekMessageW(out row,window,0,0,1)) {TranslateMessage(ref row);DispatchMessageW(ref row);}
  }
  private IntPtr WindowProc(IntPtr hwnd,uint message,IntPtr first,IntPtr second) {if(message==6402)Interlocked.Increment(ref activationMessages);return DefWindowProcW(hwnd,message,first,second);}
  private void CheckThread(){if(GetCurrentThreadId()!=thread)throw new InvalidOperationException("Owned HWND/mutex operations require their original observer thread.");}
  public void Dispose() {
   if(disposed)return;CheckThread();disposed=true;
   try {
    if(window!=IntPtr.Zero){if(!DestroyWindow(window))throw new Win32Exception(Marshal.GetLastWin32Error(),"Owned activation HWND destruction failed.");window=IntPtr.Zero;}
    if(atom!=0){if(!UnregisterClassW(className,module))throw new Win32Exception(Marshal.GetLastWin32Error(),"Owned activation class cleanup failed.");atom=0;}
   }finally {for(int i=held.Count-1;i>=0;i--){try{held[i].ReleaseMutex();}finally{held[i].Dispose();}}held.Clear();GC.KeepAlive(callback);}
  }
  private delegate IntPtr Callback(IntPtr window,uint message,IntPtr first,IntPtr second);
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct WindowClass {public uint Size,Style;public IntPtr Procedure;public int ClassExtra,WindowExtra;public IntPtr Instance,Icon,Cursor,Background;public string MenuName,ClassName;public IntPtr SmallIcon;}
  [StructLayout(LayoutKind.Sequential)]private struct Message {public IntPtr Window;public uint Id;public UIntPtr First;public IntPtr Second;public uint Time;public int X,Y;public uint Private;}
  [DllImport("kernel32.dll",ExactSpelling=true)]private static extern uint GetCurrentThreadId();
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern IntPtr GetModuleHandleW(string name);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern IntPtr FindWindowW(string className,string caption);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,ExactSpelling=true,SetLastError=true)]private static extern ushort RegisterClassExW(ref WindowClass windowClass);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,ExactSpelling=true,SetLastError=true)]private static extern IntPtr CreateWindowExW(uint extended,string className,string caption,uint style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr module,IntPtr parameter);
  [DllImport("user32.dll",ExactSpelling=true,SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool DestroyWindow(IntPtr window);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,ExactSpelling=true,SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool UnregisterClassW(string className,IntPtr module);
  [DllImport("user32.dll",ExactSpelling=true)]private static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
  [DllImport("user32.dll",ExactSpelling=true)]private static extern IntPtr DefWindowProcW(IntPtr window,uint message,IntPtr first,IntPtr second);
  [DllImport("user32.dll",ExactSpelling=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool PeekMessageW(out Message message,IntPtr window,uint minimum,uint maximum,uint flags);
  [DllImport("user32.dll",ExactSpelling=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool TranslateMessage(ref Message message);
  [DllImport("user32.dll",ExactSpelling=true)]private static extern IntPtr DispatchMessageW(ref Message message);
 }
}
