#requires -Version 5.1
param(
 [string]$UpgradeScriptPath=(Join-Path (Split-Path $PSScriptRoot -Parent) 'Test-NativeUpdateBridge.ps1'),
 [string]$SmokeSupportRoot=(Join-Path (Split-Path $PSScriptRoot -Parent) 'native-smoke')
)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT){throw 'Stage-file release regression requires actual Windows PowerShell.'}
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath($UpgradeScriptPath),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count -ne 0){throw 'Actual upgrade source failed to parse.'}
$functions=@($ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Check-InstallFileRelease'},$true))
if($functions.Count -ne 1){throw 'Expected the actual single release assertion.'}
. ([scriptblock]::Create($functions[0].Extent.Text))
if($null -eq ('PclLfNativeSmoke.FileRelease' -as [type])){Add-Type -Path (Join-Path $SmokeSupportRoot 'NativeSmokeFileRelease.cs')}
$id=[Guid]::NewGuid().ToString('N')
$tempPrefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
$fixture=[IO.Path]::GetFullPath((Join-Path $tempPrefix ('PclNativeStageRelease-'+$id)))
if(-not $fixture.StartsWith($tempPrefix,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path $fixture -Leaf) -cne ('PclNativeStageRelease-'+$id) -or [IO.Directory]::Exists($fixture)){throw 'Unsafe/nonfresh stage release fixture.'}
[IO.Directory]::CreateDirectory($fixture)|Out-Null
$marker=Join-Path $fixture 'owner.txt';[IO.File]::WriteAllText($marker,$id)
$script:target=Join-Path $fixture 'owned target.bin';$helper=Join-Path $fixture 'owned extracted.bin'
[IO.File]::WriteAllText($script:target,$id);[IO.File]::WriteAllText($helper,$id)
$typeName='PclOwnedStageLease'+$id
$lockerSource=@'
using System;
using System.IO;
using System.Threading;
public sealed class TYPE_NAME : IDisposable {
 private readonly FileStream stream;
 private readonly Timer timer;
 public TYPE_NAME(string path,int milliseconds) {
  stream=File.Open(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
  timer=new Timer(delegate(object state){stream.Dispose();},null,milliseconds,Timeout.Infinite);
 }
 public void Dispose(){timer.Dispose();stream.Dispose();}
}
'@
Add-Type -TypeDefinition $lockerSource.Replace('TYPE_NAME',$typeName)
$lease=$null
try{
 $script:caseRecord=[ordered]@{Name='manual'}
 $script:installCloseWatch=[Diagnostics.Stopwatch]::StartNew()
 $watch=$script:installCloseWatch
 Check-InstallFileRelease
 if(-not $caseRecord.ExclusiveFileRelease.Observation.Succeeded -or $caseRecord.ExclusiveFileRelease.Observation.TimeoutMilliseconds -ne 15000){throw 'Original target exclusivity/deadline changed.'}
 $lease=New-Object $typeName -ArgumentList @($helper,250)
 Check-InstallFileRelease -Path $helper -Role 'extracted-helper'
 $observed=$caseRecord.StageFileReleases[0].Observation
 if(-not $observed.Succeeded -or $observed.Attempts -lt 2 -or -not [object]::ReferenceEquals($watch,$script:installCloseWatch)){throw 'Sharing violation was not observed/released inside the original close deadline.'}
 $lease.Dispose();$lease=$null
 Check-InstallFileRelease -Path $helper -Role 'staged-0'
 if($caseRecord.StageFileReleases.Count -ne 2 -or $caseRecord.StageFileReleases[1].Observation.StartElapsedMilliseconds -lt $observed.ElapsedMilliseconds){throw 'Staged file restarted the preceding close deadline.'}
 $script:caseRecord=[ordered]@{Name='invalid-inputs'}
 $script:installCloseWatch=$null;$script:helperStopVerificationWatch=$null
 Check-InstallFileRelease
 $helperWatch=$script:helperStopVerificationWatch
 Check-InstallFileRelease -Path $helper -Role 'staged-0'
 if(-not [object]::ReferenceEquals($helperWatch,$script:helperStopVerificationWatch) -or $caseRecord.StageFileReleases[0].BudgetSource -cne 'helper-stopped verification'){throw 'Helper-only case restarted its verification deadline.'}
 $script:caseRecord=[ordered]@{Name='manual'}
 $script:installCloseWatch=[Diagnostics.Stopwatch]::StartNew()
 [Threading.Thread]::Sleep(15010)
 $expired=$script:installCloseWatch;$refused=$false
 try{Check-InstallFileRelease -Path $helper -Role 'staged-0'}catch{$refused=$true}
 $observation=$caseRecord.StageFileReleases[0].Observation
 if(-not $refused -or $observation.Succeeded -or -not $observation.TimedOut -or $observation.Attempts -ne 0 -or -not [object]::ReferenceEquals($expired,$script:installCloseWatch)){throw 'Expired deadline was reset or an unopened file was accepted.'}
 $script:caseRecord=[ordered]@{Name='restart-rollback'}
 $script:helperStopVerificationWatch=$null
 Check-InstallFileRelease
 $rollbackWatch=$script:helperStopVerificationWatch
 Check-InstallFileRelease -Path $helper -Role 'staged-0'
 if([object]::ReferenceEquals($expired,$rollbackWatch) -or -not [object]::ReferenceEquals($expired,$script:installCloseWatch) -or -not [object]::ReferenceEquals($rollbackWatch,$script:helperStopVerificationWatch) -or $caseRecord.ExclusiveFileRelease.Observation.StartElapsedMilliseconds -ge 15000){throw 'Rollback reused the expired old-close watch or reset its helper-stopped checkpoint.'}
 Write-Output 'PASS actual PS5 stage-file exclusivity: owned transient lock observed/released, target/helper/stage share one unchanged deadline, helper-only checkpoint reused, and expired budget refuses an unlocked file without opening it. No Main/profile/window/process action.'
}finally{
 if($null -ne $lease){$lease.Dispose()}
 if(-not $fixture.StartsWith($tempPrefix,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path $fixture -Leaf) -cne ('PclNativeStageRelease-'+$id) -or [IO.File]::ReadAllText($marker) -cne $id -or (([IO.File]::GetAttributes($fixture) -band [IO.FileAttributes]::ReparsePoint) -ne 0)){throw 'Unsafe own stage-release cleanup.'}
 [IO.Directory]::Delete($fixture,$true)
}

