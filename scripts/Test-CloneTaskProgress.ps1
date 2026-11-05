param([Parameter(Mandatory=$true)][string]$ExecutablePath)
# Native task entry and synthetic version files only; no window, game, network or real profile.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$ExecutablePath=[IO.Path]::GetFullPath($ExecutablePath)
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
$refs=@($ExecutablePath,'System.dll','System.Core.dll','System.Xaml.dll',[Windows.Application].Assembly.Location,[Windows.Media.Brushes].Assembly.Location,[Windows.Threading.DispatcherObject].Assembly.Location)
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null;$refs+=(Join-Path $root $path)}
$refs+=(Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll')
$asm=[Reflection.Assembly]::LoadFrom($ExecutablePath);$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$entries=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $entries.Values){$entry.ValueCache['']=$entry.DefaultValue};$entries['Identify'].ValueCache['']='clone-progress-fixture'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclCloneProgress-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($fixture)|Out-Null
$asm.GetType('PCL.LocalGameData').GetProperty('TestRoot',$flags).SetValue($null,(Join-Path $fixture 'profile'),$null)
$source=@'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Collections.Generic;
using PCL;
public static class CloneProgressChecks {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static object Call(Type type,string name,params object[] args){try{return type.GetMethod(name,F).Invoke(null,args);}catch(TargetInvocationException error){throw error.InnerException;}}
 public static void Run(Assembly asm,string root){
  ModBase.RunInUi();
  string game=Path.Combine(root,"minecraft/versions/original");Directory.CreateDirectory(Path.Combine(game,"mods"));Directory.CreateDirectory(Path.Combine(game,"PCL"));
  File.WriteAllText(Path.Combine(game,"original.json"),"{\"id\":\"original\",\"type\":\"release\",\"mainClass\":\"fixture.Main\",\"libraries\":[]}");
  File.WriteAllText(Path.Combine(game,"PCL/Setup.ini"),"VersionArgumentIndieV2:True");
  var bytes=new byte[1024*1024];new Random(824).NextBytes(bytes);string source=Path.Combine(game,"mods/large.jar");
  using(var output=File.Create(source))for(int i=0;i<32;i++)output.Write(bytes,0,bytes.Length);
  var engine=asm.GetType("PCL.InstanceCloner");string before=(string)Call(engine,"Digest",source,CancellationToken.None);
  var instance=new ModMinecraft.McInstance(game);foreach(var entry in Settings.Entries.Values)if(entry.Source.ToString()=="Instance")entry.ValueCache[instance.PathVersion]=entry.DefaultValue;Settings.Entries["VersionArgumentIndieV2"].ValueCache[instance.PathVersion]=true;
  var shell=(FormMain)FormatterServices.GetUninitializedObject(typeof(FormMain));var extra=(MyExtraButton)FormatterServices.GetUninitializedObject(typeof(MyExtraButton));typeof(FormMain).GetProperty("BtnExtraDownload",F).SetValue(shell,extra,null);ModMain.FrmMain=shell;
  var options=Activator.CreateInstance(asm.GetType("PCL.InstanceCloneOptions"),true);var ui=asm.GetType("PCL.InstanceClonerUi");
  Call(ui,"Start",instance,"native-clone",options);
  var loader=ModLoader.LoaderTaskbar.Last();var seen=new List<double>();bool held=false;var deadline=DateTime.UtcNow.AddSeconds(30);
  var navigation=typeof(ModMinecraft.McInstanceLock).GetMethod("BlocksNavigation",F);
  while(loader.State==ModBase.LoadState.Waiting||loader.State==ModBase.LoadState.Loading){seen.Add(loader.Progress);held|=ModMinecraft.McInstanceLock.IsLocked(instance);Require(!(bool)navigation.Invoke(null,new object[]{instance}),"Read-only clone source blocks returning to its page");if(DateTime.UtcNow>deadline){loader.Cancel();throw new Exception("Clone task timed out");}Thread.Sleep(1);}
  seen.Add(loader.Progress);while((bool)ui.GetProperty("IsBusy",F).GetValue(null,null)&&DateTime.UtcNow<deadline)Thread.Sleep(1);
  Require(loader.State==ModBase.LoadState.Finished,"Clone task failed: "+loader.Error);
  Require(held&&!ModMinecraft.McInstanceLock.IsLocked(instance),"Clone lost its source reservation or retained it after completion");
  Require(before==(string)Call(engine,"Digest",source,CancellationToken.None),"Clone changed the source file");
  string copied=Path.Combine(root,"minecraft/versions/native-clone/mods/large.jar");Require(before==(string)Call(engine,"Digest",copied,CancellationToken.None),"Clone copied different bytes");
  Require(seen.Any(x=>x>0&&x<1),"Native clone task only reports 0/100; no intermediate progress");
  for(int i=1;i<seen.Count;i++)Require(seen[i]>=seen[i-1],"Native clone progress regressed");
  var reported=new List<double>();bool copying=false;string versions=Path.GetDirectoryName(game);
  Action<double> report=x=>{
   reported.Add(x);
   if(x>0.25&&x<0.6){var stage=Directory.GetDirectories(versions,".pcl-clone-*").Single();var stagedFile=Path.Combine(stage,"mods/large.jar");if(File.Exists(stagedFile)){long length=new FileInfo(stagedFile).Length;copying|=length>0&&length<32L*1024*1024;}}
   if(x<1)Require(!Directory.Exists(Path.Combine(versions,"measured-clone")),"Clone became visible before validation completed");
  };
  Call(engine,"CloneWithProgress",game,game,"measured-clone",options,CancellationToken.None,report,null);
  Require(copying,"Progress did not advance while a large file was actually being copied");
  Require(reported.Any(x=>x>0.02&&x<0.22)&&reported.Any(x=>x>0.85&&x<0.99)&&reported.Last()==1,"Clone hashing or final validation progress is missing");
  for(int i=1;i<reported.Count;i++)Require(reported[i]>=reported[i-1]&&reported[i]<=1,"Engine clone progress regressed or exceeded completion");
  for(int phase=0;phase<3;phase++)using(var cancel=new CancellationTokenSource()){
   double threshold=new[]{0.06,0.35,0.98}[phase];string target="canceled-"+phase;bool canceled=false;var values=new List<double>();
   Action<double> abort=x=>{values.Add(x);if(x>=threshold)cancel.Cancel();};
   try{Call(engine,"CloneWithProgress",game,game,target,options,cancel.Token,abort,null);}catch(OperationCanceledException){canceled=true;}
   Require(canceled&&!values.Contains(1)&&!Directory.Exists(Path.Combine(versions,target))&&!Directory.GetDirectories(versions,".pcl-clone-*").Any(),"Cancellation left a visible or partial clone at phase "+phase);
   Require(before==(string)Call(engine,"Digest",source,CancellationToken.None),"Cancellation changed the source version");
  }
  Console.WriteLine("PASS: native clone progress ("+seen.Distinct().Count()+" samples), source browsing, byte-level copy/hash/validation progress, atomic publication and cancellation in all three I/O phases.");
 }
}
'@
try{
 $file=Join-Path $fixture 'Checks.cs';[IO.File]::WriteAllText($file,$source)
 $output=Join-Path $fixture 'Checks.dll';$compiler=Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe'
 & $compiler /nologo /target:library (('/out:')+$output) @($refs|ForEach-Object {'/reference:'+$_}) $file
 if($LASTEXITCODE -ne 0){throw 'Clone progress fixture compilation failed'}
 [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($output))|Out-Null
 [CloneProgressChecks]::Run($asm,$fixture)
}finally{
 $app.Shutdown();$full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^PclCloneProgress-[a-f0-9]{32}$'){throw 'Unsafe clone fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
