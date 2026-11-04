param([Parameter(Mandatory=$true)][string]$ExecutablePath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));Set-Location $root
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
$refs=@($ExecutablePath,'System.dll','System.Core.dll','System.Xaml.dll',[Windows.Application].Assembly.Location,[Windows.Media.Brushes].Assembly.Location,[Windows.Threading.DispatcherObject].Assembly.Location)
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null;$refs+=(Join-Path $root $path)}
$refs+=(Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll')
$asm=[Reflection.Assembly]::LoadFrom($ExecutablePath);$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$entries=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $entries.Values){$entry.ValueCache['']=$entry.DefaultValue};$entries['Identify'].ValueCache['']='backup-progress-fixture'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclBackupProgress-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($fixture)|Out-Null
$asm.GetType('PCL.LocalGameData').GetProperty('TestRoot',$flags).SetValue($null,(Join-Path $fixture 'profile'),$null)
$asm.GetType('PCL.WorldBackups').GetProperty('TestRoot',$flags).SetValue($null,(Join-Path $fixture 'backups'),$null)
$source=@'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using PCL;
using Newtonsoft.Json.Linq;
using MeloongCore;
public static class BackupProgressRepro {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static object Call(Assembly asm,string type,string name,params object[] args){try{return asm.GetType(type).GetMethod(name,F).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static void ProgressIsReal(List<double> values,string operation){Require(values.Count>3&&values.Any(x=>x>0&&x<1),operation+" has no streaming progress");for(int i=1;i<values.Count;i++)Require(values[i]>=values[i-1]&&values[i]<=1,operation+" progress regressed");Require(values.Last()==1,operation+" did not finish its progress");}
 static void WaitForUi(Assembly asm,string type,ModLoader.LoaderBase loader){var end=DateTime.UtcNow.AddSeconds(25);while(loader.State==ModBase.LoadState.Loading||loader.State==ModBase.LoadState.Waiting){Require(DateTime.UtcNow<end,"Native task timed out");Thread.Sleep(2);}while((bool)asm.GetType(type).GetProperty("IsBusy",F).GetValue(null,null)&&DateTime.UtcNow<end)Thread.Sleep(2);Require(loader.State==ModBase.LoadState.Finished,"Native task failed: "+loader.Error);}
 public static string Run(Assembly asm,string root) {
  ModBase.RunInUi();
  var game=Path.Combine(root,"minecraft/versions/fixture");Directory.CreateDirectory(Path.Combine(game,"saves/World"));Directory.CreateDirectory(Path.Combine(game,"PCL"));
  File.WriteAllText(Path.Combine(game,"fixture.json"),"{\"id\":\"fixture\",\"type\":\"release\",\"mainClass\":\"fixture.Main\",\"libraries\":[],\"arguments\":{\"game\":[],\"jvm\":[]}}");
  File.WriteAllText(Path.Combine(game,"PCL/Setup.ini"),"VersionArgumentIndieV2:True");File.WriteAllText(Path.Combine(game,"saves/World/level.dat"),"fixture");
  var bytes=new byte[1024*1024];new Random(417).NextBytes(bytes);
  using(var output=File.Create(Path.Combine(game,"saves/World/data.bin")))for(int i=0;i<32;i++)output.Write(bytes,0,bytes.Length);
  var instance=new ModMinecraft.McInstance(game);
  var entries=Settings.Entries;foreach(var entry in entries.Values)if(entry.Source.ToString()=="Instance")entry.ValueCache[instance.PathVersion]=entry.DefaultValue;
  entries["VersionArgumentIndieV2"].ValueCache[instance.PathVersion]=true;
  var shell=(FormMain)FormatterServices.GetUninitializedObject(typeof(FormMain));
  var extra=(MyExtraButton)FormatterServices.GetUninitializedObject(typeof(MyExtraButton));typeof(FormMain).GetProperty("BtnExtraDownload",F).SetValue(shell,extra,null);ModMain.FrmMain=shell;
  var type=asm.GetType("PCL.WorldBackupsUi");type.GetMethod("Start",F).Invoke(null,new object[]{instance,null,null,"World"});
  var loader=ModLoader.LoaderTaskbar.Last();var seen=new HashSet<double>();bool held=false;bool checkedList=false;var deadline=DateTime.UtcNow.AddSeconds(25);
  var navigation=typeof(ModMinecraft.McInstanceLock).GetMethod("BlocksNavigation",F);
  while(loader.State==ModBase.LoadState.Waiting||loader.State==ModBase.LoadState.Loading){
   seen.Add(loader.Progress);held|=ModMinecraft.McInstanceLock.IsLocked(instance);
   Require(navigation!=null&&!(bool)navigation.Invoke(null,new object[]{instance}),"Read-only backup blocks version navigation");
   if(!checkedList&&loader.Progress>0.04&&loader.Progress<0.6){
    var read=Task.Factory.StartNew(()=>Call(asm,"PCL.WorldBackups","Points",game),CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
    var fast=read.Wait(300);if(!fast)WaitForUi(asm,"PCL.WorldBackupsUi",loader);
    Require(fast,"Backup list browsing waits for the entire backup");checkedList=true;
   }
   if(DateTime.UtcNow>deadline){loader.Cancel();throw new Exception("Backup fixture timed out");}Thread.Sleep(2);
  }
  seen.Add(loader.Progress);while((bool)type.GetProperty("IsBusy",F).GetValue(null,null)&&DateTime.UtcNow<deadline)Thread.Sleep(2);
  if(loader.State!=ModBase.LoadState.Finished)throw new Exception("Backup failed: "+loader.Error);
  if(!seen.Any(x=>x>0&&x<1))throw new Exception("Native backup task reported only 0/100 progress");
  Require(held&&!ModMinecraft.McInstanceLock.IsLocked(instance),"Backup lost its write exclusion or retained it after stopping");
  var points=(IEnumerable<JObject>)Call(asm,"PCL.WorldBackups","Points",game);var point=points.First()["id"].ToString();
  var worldProgress=new List<double>();Action<double> worldReport=x=>worldProgress.Add(x);
  File.WriteAllText(Path.Combine(game,"saves/World/data.bin"),"new world progress");
  Call(asm,"PCL.WorldBackups","RestoreWithProgress",game,point,5,CancellationToken.None,worldReport,null);
  ProgressIsReal(worldProgress,"World restore");Require(new FileInfo(Path.Combine(game,"saves/World/data.bin")).Length==32L*1024*1024,"World restore contents differ");
  Directory.CreateDirectory(Path.Combine(game,"saves/not-a-world"));Directory.CreateDirectory(Path.Combine(game,"saves/.pcl-pending"));
  var allWorlds=new List<double>();Action<double> allReport=x=>allWorlds.Add(x);
  Call(asm,"PCL.WorldBackups","CaptureFolderWithProgress",game,"manual",5,CancellationToken.None,allReport,null);
  ProgressIsReal(allWorlds,"All-world backup");Require(allWorlds.Any(x=>x>0.5&&x<0.65),"Non-world folders distorted backup progress");
  // The actual environment UI bridge must also update the native task, without a launcher entry point.
  Directory.CreateDirectory(Path.Combine(game,"mods"));using(var output=File.Create(Path.Combine(game,"mods/library.jar")))for(int i=0;i<8;i++)output.Write(bytes,0,bytes.Length);
  asm.GetType("PCL.EnvironmentHistory").GetProperty("TestRoot",F).SetValue(null,Path.Combine(root,"history"),null);
  Call(asm,"PCL.EnvironmentHistoryUi","Start",instance,false,null,null);
  var envLoader=ModLoader.LoaderTaskbar.Last();var envSamples=new HashSet<double>();
  while(envLoader.State==ModBase.LoadState.Loading||envLoader.State==ModBase.LoadState.Waiting){envSamples.Add(envLoader.Progress);Require(!(bool)navigation.Invoke(null,new object[]{instance}),"Environment snapshot blocks navigation");Require(DateTime.UtcNow<deadline,"Environment snapshot timed out");Thread.Sleep(2);}
  WaitForUi(asm,"PCL.EnvironmentHistoryUi",envLoader);Require(envSamples.Any(x=>x>0&&x<1),"Native environment task reported only 0/100 progress");
  var envPoints=(IEnumerable<JObject>)Call(asm,"PCL.EnvironmentHistory","Points",game);var envId=envPoints.First()["id"].ToString();
  File.WriteAllText(Path.Combine(game,"mods/library.jar"),"changed mod");var envProgress=new List<double>();Action<double> envReport=x=>envProgress.Add(x);
  Call(asm,"PCL.EnvironmentHistory","RestoreWithProgress",game,game,Path.Combine(root,"minecraft"),envId,CancellationToken.None,envReport,null,null);
  ProgressIsReal(envProgress,"Environment restore");Require(new FileInfo(Path.Combine(game,"mods/library.jar")).Length==8L*1024*1024,"Environment restore contents differ");
  // Pausing a real snapshot mid-stream must not block published-point readers.
  using(var entered=new ManualResetEventSlim(false))using(var release=new ManualResetEventSlim(false)){
   Action<double> paused=x=>{if(x>0.05&&x<0.6&&!entered.IsSet){entered.Set();release.Wait();}};
   var work=Task.Run(()=>Call(asm,"PCL.EnvironmentHistory","CaptureWithProgress",game,game,"fixture snapshot",CancellationToken.None,paused,null,"fixture"));
   try{Require(entered.Wait(5000),"Snapshot did not report intermediate I/O");var read=Task.Factory.StartNew(()=>Call(asm,"PCL.EnvironmentHistory","Points",game),CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);Require(read.Wait(300),"Environment list readers wait for the snapshot lock");}
   finally{release.Set();Require(work.Wait(10000),"Paused snapshot did not finish");}
  }
  // Cancel from the actual I/O callback: no half-created point may be published.
  var before=((IEnumerable<JObject>)Call(asm,"PCL.WorldBackups","Points",game)).Count();
  using(var cancel=new CancellationTokenSource()){
   Action<double> abort=x=>{if(x>0.05&&x<0.6)cancel.Cancel();};bool stopped=false;
   try{Call(asm,"PCL.WorldBackups","CaptureWorldWithProgress",game,"World","manual",5,cancel.Token,abort,null);}catch(OperationCanceledException){stopped=true;}
   Require(stopped&&((IEnumerable<JObject>)Call(asm,"PCL.WorldBackups","Points",game)).Count()==before,"Canceled backup published partial data");
  }
  // Cancel at the shared-library copy boundary: a zero-length/partial final library must never remain.
  var shared=Path.Combine(root,"minecraft/libraries/fixture/support.jar");Directory.CreateDirectory(Path.GetDirectoryName(shared));File.WriteAllBytes(shared,bytes);
  var dependencies=new Dictionary<string,string>{{"libraries/fixture/support.jar",shared}};
  var libraryPoint=(string)Call(asm,"PCL.EnvironmentHistory","CaptureWithProgress",game,game,"library fixture",CancellationToken.None,null,dependencies,"fixture");File.Delete(shared);
  using(var cancel=new CancellationTokenSource()){
   Action<double> abort=x=>{if(x>=0.62&&x<0.65)cancel.Cancel();};bool stopped=false;
   try{Call(asm,"PCL.EnvironmentHistory","RestoreWithProgress",game,game,Path.Combine(root,"minecraft"),libraryPoint,cancel.Token,abort,null,null);}catch(OperationCanceledException){stopped=true;}
   Require(stopped&&!File.Exists(shared),"Canceled environment restore left a partial shared library");
  }
  Require(!Directory.GetFiles(Path.GetDirectoryName(shared),"*.pcl-restore-*").Any(),"Canceled shared-library staging was not cleaned");
  // Snapshot reservation still blocks writers through cancellation/finally; destructive work blocks navigation.
  using(var entered=new ManualResetEventSlim(false))using(var release=new ManualResetEventSlim(false))using(var stopped=new ManualResetEventSlim(false)){
   var worker=new RedoableWorker((Action)(()=>{entered.Set();release.Wait();}));worker.Stopped+=()=>stopped.Set();
   Require((bool)typeof(ModMinecraft.McInstanceLock).GetMethod("TryAcquireSnapshot",F).Invoke(null,new object[]{worker,instance.PathVersion}),"Snapshot reservation unavailable");
   worker.Start();try{Require(entered.Wait(5000),"Snapshot reservation fixture did not start");Require(ModMinecraft.McInstanceLock.IsLocked(instance)&&!(bool)navigation.Invoke(null,new object[]{instance}),"Snapshot browsing policy is incorrect");Require(!ModMinecraft.McInstanceLock.TryAcquire(new RedoableWorker((Action)(()=>{})),instance.PathVersion),"Snapshot admitted a conflicting writer");worker.Cancel();Require(ModMinecraft.McInstanceLock.IsLocked(instance),"Cancel released a snapshot before its workload ended");}
   finally{release.Set();Require(stopped.Wait(5000),"Snapshot reservation did not stop");}
  }
  var exclusive=new RedoableWorker((Action)(()=>{}));Require(ModMinecraft.McInstanceLock.TryAcquire(exclusive,instance.PathVersion),"Write reservation unavailable");Require((bool)navigation.Invoke(null,new object[]{instance}),"Restore/write work no longer blocks navigation");exclusive.Start();Require(exclusive.WaitIfRunning(5000),"Write fixture did not stop");
  return new JObject(new JProperty("native_world_progress_samples",seen.Count),new JProperty("native_environment_progress_samples",envSamples.Count),new JProperty("world_restore_progress_events",worldProgress.Count),new JProperty("environment_restore_progress_events",envProgress.Count),new JProperty("snapshot_navigation_allowed",true),new JProperty("write_exclusion_retained",held),new JProperty("published_lists_nonblocking",true),new JProperty("cancel_keeps_points_intact",true)).ToString();
 }
}
'@
try{
 $file=Join-Path $fixture 'Probe.cs';[IO.File]::WriteAllText($file,$source)
 $output=Join-Path $fixture 'Probe.dll';$compiler=Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe'
 & $compiler /nologo /target:library (('/out:')+$output) @($refs|ForEach-Object {'/reference:'+$_}) $file
 if($LASTEXITCODE -ne 0){throw 'Probe compilation failed'}
 [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($output))|Out-Null
 [BackupProgressRepro]::Run($asm,$fixture)
}finally{
 $app.Shutdown();$full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^PclBackupProgress-[a-f0-9]{32}$'){throw 'Unsafe fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
