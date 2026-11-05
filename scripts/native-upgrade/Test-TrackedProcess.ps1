[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$TrackingScriptPath,
 [Parameter(Mandatory=$true)][string]$EvidenceRoot,
 [string]$SmokeSupportRoot
)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT){throw 'Tracked-process regression requires actual Windows PowerShell/.NET Framework.'}
$TrackingScriptPath=[IO.Path]::GetFullPath($TrackingScriptPath);$EvidenceRoot=[IO.Path]::GetFullPath($EvidenceRoot)
if(-not [IO.File]::Exists($TrackingScriptPath) -or [IO.Directory]::Exists($EvidenceRoot) -or [IO.File]::Exists($EvidenceRoot)){throw 'Tracking source missing or regression directory already exists.'}
if(-not $SmokeSupportRoot){$SmokeSupportRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'native-smoke'}
if((Split-Path $EvidenceRoot -Leaf) -cne 'tracked-process-regression' -or (Split-Path (Split-Path $EvidenceRoot -Parent) -Leaf) -cnotmatch '^NativeUpgrade-[a-f0-9]{32}$'){throw 'Regression evidence must be inside its exact owned NativeUpgrade GUID fixture.'}
$id=[Guid]::NewGuid().ToString('N');$EvidenceRoot=Join-Path $EvidenceRoot ('TrackedProcess-'+$id);$marker=Join-Path $EvidenceRoot 'fixture.owner'
[IO.Directory]::CreateDirectory($EvidenceRoot)|Out-Null;[IO.File]::WriteAllText($marker,$id)
$ownedProcesses=[Collections.Generic.List[Diagnostics.Process]]::new();$records=@();$cleanupErrors=@();$allStopped=$true
$report=[ordered]@{Result='pending';FixtureId=$id;TrackingSourceSha256=(Get-FileHash -LiteralPath $TrackingScriptPath -Algorithm SHA256).Hash.ToLowerInvariant();Runtime=[Environment]::Version.ToString();PowerShell=$PSVersionTable.PSVersion.ToString();Cases=@();CleanupErrors=@();Scope='Exact Track-Process source; owned harmless exit0/23 processes; retained original handle, image and strict exit code. Unpinned controls are diagnostic only.'}
try{
 # Parse and execute only the exact function under test. Never dot-source the
 # actual upgrade entry or maintain a second tracking implementation in tests.
 $tokens=$null;$parseErrors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($TrackingScriptPath,[ref]$tokens,[ref]$parseErrors)
 if($parseErrors.Count -ne 0){throw 'Actual tracking source does not parse.'}
 $functions=@($ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Track-Process'},$true))
 if($functions.Count -ne 1){throw 'Expected exactly one actual Track-Process definition.'}
 . ([scriptblock]::Create($functions[0].Extent.Text))
 if($null -eq ('PclLfNativeSmoke.Win32' -as [type])){Add-Type -Path (Join-Path $SmokeSupportRoot 'NativeSmokeWin32.cs') -ReferencedAssemblies System.Drawing.dll}
 $executable=Join-Path $EvidenceRoot ('OwnedExit-'+$id+'.exe')
 $typeName='PclLfOwnedTrackedExit'+$id
 $source=@'
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
public static class TYPE_NAME {
 public static int Main(string[] args) {
  if(args.Length != 4) return 97;
  File.WriteAllText(args[0] + ".pending", args[3] + "|" + Process.GetCurrentProcess().Id);
  File.Move(args[0] + ".pending", args[0]);
  DateTime deadline=DateTime.UtcNow.AddSeconds(10);
  while(!File.Exists(args[1])) { if(DateTime.UtcNow >= deadline) return 98; Thread.Sleep(10); }
  return File.ReadAllText(args[1]) == args[3] ? int.Parse(args[2]) : 99;
 }
}
'@
 Add-Type -TypeDefinition $source.Replace('TYPE_NAME',$typeName) -OutputAssembly $executable -OutputType ConsoleApplication
 foreach($code in @(0,23)){
  foreach($pin in @($true,$false)){
   $caseName=([string]$code)+'-'+$(if($pin){'pinned'}else{'unpinned-control'});$ready=Join-Path $EvidenceRoot ($caseName+'.ready');$release=Join-Path $EvidenceRoot ($caseName+'.release')
   $info=[Diagnostics.ProcessStartInfo]::new($executable,('"'+$ready+'" "'+$release+'" '+$code+' '+$id));$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.WorkingDirectory=$EvidenceRoot
   $started=[Diagnostics.Process]::Start($info);if($null -eq $started){throw 'Owned regression child failed to start.'};$ownedProcesses.Add($started)
   $watch=[Diagnostics.Stopwatch]::StartNew();while(-not [IO.File]::Exists($ready)){if($started.HasExited -or $watch.ElapsedMilliseconds -ge 5000){throw 'Owned regression child did not reach its nonce/PID readiness barrier.'};Start-Sleep -Milliseconds 10}
   if([IO.File]::ReadAllText($ready) -cne ($id+'|'+$started.Id)){throw 'Owned regression child readiness identity differs.'}
   if($pin){$observed=Track-Process $started.Id;[IntPtr]$beforeHandle=$observed.Handle;$image=[PclLfNativeSmoke.Win32]::ObserveProcessImage($observed,$false);if(-not $image.QuerySucceeded -or -not [string]::Equals($image.Path,$executable,[StringComparison]::OrdinalIgnoreCase)){throw 'Pinned original process handle does not identify the owned actual image.'}}
   else{$observed=[Diagnostics.Process]::GetProcessById($started.Id);$ownedProcesses.Add($observed);$beforeHandle=$null;$image=$null}
   [IO.File]::WriteAllText($release+'.pending',$id);[IO.File]::Move($release+'.pending',$release)
   $finished=$observed.WaitForExit(5000);$exit=$observed.ExitCode
   $record=[ordered]@{Name=$caseName;ProcessId=$started.Id;ExpectedExitCode=$code;WaitCompleted=$finished;ExitCode=$exit;ExitCodeType=$(if($null -eq $exit){$null}else{$exit.GetType().FullName});OriginalHandleRetained=$null;Image=$image;Result='diagnostic-control'}
   if($pin){$record.OriginalHandleRetained=$observed.Handle -eq $beforeHandle;if(-not $finished -or $exit -isnot [int] -or $exit -ne $code -or -not $record.OriginalHandleRetained){throw 'Pinned original handle did not retain the exact completed exit0/23 contract.'};$record.Result='passed'}
   elseif(-not $finished){throw 'Unpinned diagnostic child did not complete its own exit.'}
   $records+=,$record
  }
 }
 $report.Result='passed'
}catch{$report.Result='failed';$report.Error=$_.Exception.Message;throw}
finally{
 foreach($process in $ownedProcesses){try{if(-not $process.HasExited){$process.Kill();if(-not $process.WaitForExit(5000)){$allStopped=$false}}}catch{$allStopped=$false;$cleanupErrors+=('Owned process cleanup: '+$_.Exception.Message)}finally{$process.Dispose()}}
 $report.Cases=$records;$report.CleanupErrors=$cleanupErrors
 if(-not $allStopped -or $cleanupErrors.Count -ne 0){$report.Result='failed'}
 if([IO.File]::ReadAllText($marker) -cne $id){$report.Result='failed';$report.CleanupErrors+=,'Regression owner marker changed.'}
 [IO.File]::WriteAllText((Join-Path $EvidenceRoot 'evidence.json'),($report|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
}
if($report.Result -cne 'passed'){throw 'Tracked-process regression failed; inspect its owned evidence.'}
Write-Output 'PASS actual Windows PowerShell Track-Process: retained same image/handle and strict exit0/23; unpinned observations recorded without weakening assertions.'
