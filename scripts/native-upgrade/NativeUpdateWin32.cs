using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
namespace PclLfNativeUpgrade {
 public static class Dialogs {
  private delegate bool EnumCallback(IntPtr hwnd,IntPtr state);
  [DllImport("user32.dll")]private static extern bool EnumChildWindows(IntPtr parent,EnumCallback callback,IntPtr state);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetWindowText(IntPtr hwnd,StringBuilder text,int capacity);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr hwnd,StringBuilder text,int capacity);
  [DllImport("user32.dll")]private static extern IntPtr GetDlgItem(IntPtr hwnd,int id);
  [DllImport("user32.dll")]private static extern bool IsWindowEnabled(IntPtr hwnd);
  [DllImport("user32.dll",SetLastError=true)]private static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr first,IntPtr second);
  [DllImport("shell32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern IntPtr CommandLineToArgvW(string value,out int count);
  [DllImport("kernel32.dll")]private static extern IntPtr LocalFree(IntPtr value);
  public static string[] Arguments(string commandLine){int count;IntPtr data=CommandLineToArgvW(commandLine,out count);if(data==IntPtr.Zero)throw new InvalidOperationException("Cannot read actual helper arguments.");try{string[] result=new string[count];for(int i=0;i<count;i++)result[i]=Marshal.PtrToStringUni(Marshal.ReadIntPtr(data,i*IntPtr.Size));return result;}finally{LocalFree(data);}}
  public static string Body(IntPtr hwnd){StringBuilder result=new StringBuilder();EnumChildWindows(hwnd,delegate(IntPtr child,IntPtr unused){StringBuilder type=new StringBuilder(128);GetClassName(child,type,type.Capacity);if(type.ToString()=="Static"){StringBuilder text=new StringBuilder(8192);GetWindowText(child,text,text.Capacity);result.AppendLine(text.ToString());}return true;},IntPtr.Zero);return result.ToString();}
  // Only an already-identified, owned native updater Yes/No failure dialog.
  // IDNO declines opening the fallback browser; never accepts an update.
  public static bool DeclineFailure(IntPtr hwnd){IntPtr no=GetDlgItem(hwnd,7);return no!=IntPtr.Zero&&IsWindowEnabled(no)&&PostMessage(hwnd,0x0111,new IntPtr(7),no);}
 }
 public sealed class RollbackProbe:IDisposable {
  private readonly string target;private readonly byte[] expected;private readonly Thread sampler;private readonly FileSystemWatcher watcher;private volatile bool stopping;
  public volatile bool NativeTargetObserved;public volatile bool BackupObserved;
  public RollbackProbe(string targetPath,byte[] nativePrefix){target=targetPath;expected=nativePrefix;string prefix=Path.GetFileName(target)+".update-backup-";watcher=new FileSystemWatcher(Path.GetDirectoryName(target));watcher.NotifyFilter=NotifyFilters.FileName;
   watcher.Created+=delegate(object sender,FileSystemEventArgs e){if(e.Name.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))BackupObserved=true;};watcher.Renamed+=delegate(object sender,RenamedEventArgs e){if(e.Name.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))BackupObserved=true;};watcher.EnableRaisingEvents=true;
   sampler=new Thread(Sample);sampler.IsBackground=true;sampler.Start();
  }
  private void Sample(){byte[] buffer=new byte[expected.Length];while(!stopping){try{using(FileStream file=new FileStream(target,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){int count=0;while(count<buffer.Length){int read=file.Read(buffer,count,buffer.Length-count);if(read==0)break;count+=read;}bool equal=count==expected.Length;for(int i=0;equal&&i<expected.Length;i++)if(buffer[i]!=expected[i])equal=false;if(equal)NativeTargetObserved=true;}}catch(IOException){}catch(UnauthorizedAccessException){}Thread.Sleep(1);}}
  public void Dispose(){stopping=true;sampler.Join(2000);watcher.Dispose();}
 }
}
