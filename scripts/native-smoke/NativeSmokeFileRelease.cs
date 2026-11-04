using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PclLfNativeSmoke {
 public sealed class FileLockOwnerRow {
  public uint ProcessId;public ulong ProcessStartFileTime;public string ApplicationName,ServiceName;
  public int ApplicationType;public uint Status,SessionId;public bool Restartable;
 }
 public sealed class FileLockOwnersRow {
  public string Path,Source="Restart Manager/RmRegisterResources(single owned file)/RmGetList";
  public bool QuerySucceeded;public uint Win32Error,EndSessionError;public string ErrorType,Error;
  public int ListAttempts;public long ElapsedMilliseconds;public FileLockOwnerRow[] Owners=new FileLockOwnerRow[0];
 }
 public sealed class FileReleaseAttemptRow {public int Attempt,Win32Error,HResult;public long ElapsedMilliseconds;}
 public sealed class FileReleaseRow {
  public string Path;public bool Succeeded,TimedOut;public int Attempts,TimeoutMilliseconds,LastWin32Error,LastHResult;
  public long StartElapsedMilliseconds,ElapsedMilliseconds,TotalElapsedMilliseconds;public string ErrorType,Error;
  public FileReleaseAttemptRow[] RetrySamples=new FileReleaseAttemptRow[0];public bool RetrySamplesTruncated;
  public FileLockOwnersRow[] OwnerObservations=new FileLockOwnersRow[0];
 }
 public static class FileRelease {
  // Win32 ERROR_SHARING_VIOLATION=32 and ERROR_LOCK_VIOLATION=33 only.
  // No localized message matching, access-denied retry, alternate share mode,
  // RmShutdown/RmRestart, privileged process access or process termination.
  public static FileReleaseRow WaitForExclusiveRelease(string path,Stopwatch closeWatch,int timeoutMilliseconds) {
   if(String.IsNullOrEmpty(path))throw new ArgumentException("Owned file path is required.","path");
   if(closeWatch==null || !closeWatch.IsRunning)throw new ArgumentException("Running close deadline required.","closeWatch");
   if(timeoutMilliseconds<1 || timeoutMilliseconds>30000)throw new ArgumentOutOfRangeException("timeoutMilliseconds");
   return WaitCore(path,closeWatch,timeoutMilliseconds,delegate {
    using(FileStream stream=File.Open(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) { }
   },delegate {return ObserveOwners(path);});
  }
  private static FileReleaseRow WaitCore(string path,Stopwatch watch,int timeout,Action open,Func<FileLockOwnersRow> observe) {
   FileReleaseRow result=new FileReleaseRow {Path=path,TimeoutMilliseconds=timeout,StartElapsedMilliseconds=watch.ElapsedMilliseconds};
   List<FileReleaseAttemptRow> errors=new List<FileReleaseAttemptRow>();List<FileLockOwnersRow> owners=new List<FileLockOwnersRow>();
   while(watch.ElapsedMilliseconds<timeout) {
    result.Attempts++;
    try {open();result.ElapsedMilliseconds=watch.ElapsedMilliseconds;
     // A completed exclusive Open+Dispose is mandatory. Deadline expiry never
     // becomes success just because one operation returned after the budget.
     if(result.ElapsedMilliseconds<timeout)result.Succeeded=true;
     else {result.TimedOut=true;result.ErrorType=typeof(TimeoutException).FullName;result.Error="Exclusive file open finished after the close deadline.";}
     break;
    }catch(Exception error) {
     result.LastHResult=error.HResult;result.LastWin32Error=error.HResult&65535;result.ErrorType=error.GetType().FullName;result.Error=error.Message;
     if(!(error is IOException) || (error.HResult!=unchecked((int)0x80070020) && error.HResult!=unchecked((int)0x80070021)))break;
     if(errors.Count<16)errors.Add(new FileReleaseAttemptRow {Attempt=result.Attempts,Win32Error=result.LastWin32Error,HResult=result.LastHResult,ElapsedMilliseconds=watch.ElapsedMilliseconds});else result.RetrySamplesTruncated=true;
     if(owners.Count==0)owners.Add(SafeObserve(path,observe));
     int remaining=timeout-(int)Math.Min(Int32.MaxValue,watch.ElapsedMilliseconds);
     if(remaining<=0)break;
     Thread.Sleep(Math.Min(50,remaining));
    }
   }
   result.ElapsedMilliseconds=watch.ElapsedMilliseconds;
   if(!result.Succeeded && result.ElapsedMilliseconds>=timeout) {
    result.TimedOut=true;
    if(result.ErrorType==null) {result.ErrorType=typeof(TimeoutException).FullName;result.Error="Close deadline expired before an exclusive open could succeed.";}
    if(result.LastWin32Error==32 || result.LastWin32Error==33)owners.Add(SafeObserve(path,observe));
   }
   result.RetrySamples=errors.ToArray();result.OwnerObservations=owners.ToArray();result.TotalElapsedMilliseconds=watch.ElapsedMilliseconds;return result;
  }
  private static FileLockOwnersRow SafeObserve(string path,Func<FileLockOwnersRow> observe) {
   try {return observe();}catch(Exception error) {return new FileLockOwnersRow {Path=path,ErrorType=error.GetType().FullName,Error=error.Message};}
  }
  [StructLayout(LayoutKind.Sequential)]private struct FileTime {public uint Low,High;}
  [StructLayout(LayoutKind.Sequential)]private struct UniqueProcess {public uint Id;public FileTime Started;}
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct ProcessInfo {
   public UniqueProcess Process;
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)]public string ApplicationName;
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)]public string ServiceName;
   public int ApplicationType;public uint Status,SessionId;
   [MarshalAs(UnmanagedType.Bool)]public bool Restartable;
  }
  [DllImport("rstrtmgr.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern uint RmStartSession(out uint session,uint flags,StringBuilder key);
  [DllImport("rstrtmgr.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern uint RmRegisterResources(uint session,uint files,[MarshalAs(UnmanagedType.LPArray,ArraySubType=UnmanagedType.LPWStr)]string[] names,uint applications,IntPtr processes,uint services,IntPtr serviceNames);
  [DllImport("rstrtmgr.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern uint RmGetList(uint session,out uint needed,ref uint count,[In,Out,MarshalAs(UnmanagedType.LPArray,SizeParamIndex=2)]ProcessInfo[] processes,ref uint rebootReasons);
  [DllImport("rstrtmgr.dll",ExactSpelling=true)]private static extern uint RmEndSession(uint session);
  public static FileLockOwnersRow ObserveOwners(string path) {
   FileLockOwnersRow result=new FileLockOwnersRow {Path=path};Stopwatch elapsed=Stopwatch.StartNew();uint session=0;bool started=false;
   try {
    uint code=RmStartSession(out session,0,new StringBuilder(33));result.Win32Error=code;if(code!=0)return result;started=true;
    code=RmRegisterResources(session,1,new string[] {path},0,IntPtr.Zero,0,IntPtr.Zero);result.Win32Error=code;if(code!=0)return result;
    uint needed=0,count=0,reasons=0;ProcessInfo[] list=null;
    for(int attempt=0;attempt<3;attempt++) {
     result.ListAttempts++;code=RmGetList(session,out needed,ref count,list,ref reasons);result.Win32Error=code;
     if(code==0) {
      if(count>(list==null?0:list.Length)) {result.Error="Restart Manager returned an invalid owner count.";return result;}
      FileLockOwnerRow[] rows=new FileLockOwnerRow[count];
      for(int index=0;index<count;index++) {ProcessInfo item=list[index];rows[index]=new FileLockOwnerRow {ProcessId=item.Process.Id,ProcessStartFileTime=((ulong)item.Process.Started.High<<32)|item.Process.Started.Low,ApplicationName=item.ApplicationName,ServiceName=item.ServiceName,ApplicationType=item.ApplicationType,Status=item.Status,SessionId=item.SessionId,Restartable=item.Restartable};}
      result.Owners=rows;result.QuerySucceeded=true;return result;
     }
     if(code!=234)return result; // ERROR_MORE_DATA only; bound concurrent-list changes.
     if(needed==0 || needed>256) {result.Error="Restart Manager owner list exceeds the diagnostic bound.";return result;}
     list=new ProcessInfo[needed];count=needed;
    }
    result.Error="Restart Manager owner list changed during all bounded reads.";return result;
   }catch(Exception error) {result.ErrorType=error.GetType().FullName;result.Error=error.Message;return result;}
   finally {if(started)result.EndSessionError=RmEndSession(session);result.ElapsedMilliseconds=elapsed.ElapsedMilliseconds;}
  }
 }
}
