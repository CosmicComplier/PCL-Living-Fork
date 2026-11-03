param([Parameter(Mandatory=$true)][string]$ExecutablePath)
# Replays the real first-run options and Mojang Quick Play JSON shapes in owned temporary files.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.IO.Compression
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='game-regression-fixture'
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclGameRegression-'+[Guid]::NewGuid().ToString('N'))
function Call($type,$name,[object[]]$arguments){
 $values=New-Object object[] $arguments.Length
 for($i=0;$i -lt $values.Length;$i++){if($null -ne $arguments[$i]){if($arguments[$i] -is [Newtonsoft.Json.Linq.JToken]){$values[$i]=$arguments[$i]}else{$values[$i]=$arguments[$i].PSObject.BaseObject}}}
 return ,$type.GetMethod($name,$flags).Invoke($null,$values)
}
$data=$asm.GetType('PCL.LocalGameData');$quick=$asm.GetType('PCL.QuickPlayWorlds');$bindings=$asm.GetType('PCL.InstanceKeybindings')
try{
 [IO.Directory]::CreateDirectory($fixture)|Out-Null;$data.GetProperty('TestRoot',$flags).SetValue($null,(Join-Path $fixture 'profile'),$null)
 $version=Join-Path $fixture '.minecraft/versions/test';$world=Join-Path $version 'saves/Test world';[IO.Directory]::CreateDirectory($world)|Out-Null
 [IO.File]::WriteAllText((Join-Path $world 'level.dat'),'world-fixture')
 [IO.File]::WriteAllText((Join-Path $version 'options.txt'),"lang:zh_cn`n")
 $rows=Call $bindings 'ReadBindings' @($version,[Threading.CancellationToken]::None)
 if($rows.Count -ne 31){throw 'First-run binding catalog changed from the original 31 defaults'}
 foreach($row in $rows){
  if($row.GetType().FullName -cne 'PCL.InstanceKeybinding'){throw 'Binding reader did not preserve the original VB row type'}
  foreach($name in @('Key','Name','Owners','Value','Normalized','SharedCount','Saved')){if($null -eq $row.GetType().GetField($name,$flags)){throw ('Binding field reflection contract changed: '+$name)}}
 }
 $blank=$rows.Count -eq 0
 $instanceType=$asm.GetType('PCL.ModMinecraft+McInstance');$instance=$instanceType.GetConstructor([type[]]@([string])).Invoke(@([string]$version));$instanceType.GetProperty('Version').SetValue($instance,[PCLCS.McVersion]::new('1.20.1',$true),$null)
 $settings['VersionArgumentIndieV2'].ValueCache[$instance.PathVersion]=$true
 [IO.Directory]::CreateDirectory((Join-Path $version 'PCL'))|Out-Null;[IO.File]::WriteAllText((Join-Path $version 'PCL/Setup.ini'),'VersionArgumentIndieV2:True')
 $arguments=[Collections.Generic.List[string]]::new();$session=Call $quick 'Prepare' @($instance,$null,$arguments,$false)
 if($null -eq $session -or $session.GetType().FullName -cne 'PCL.QuickPlaySession'){throw 'Quick Play did not preserve the original VB session type'}
 foreach($name in @('Id','VersionPath','GameFolder','LogFile')){if($null -eq $session.GetType().GetField($name,$flags)){throw ('Quick Play session field reflection contract changed: '+$name)}}
 if($session.GetType().GetField('Id',$flags).GetValue($session) -cnotmatch '\A[a-f0-9]{32}\z'){throw 'Quick Play session ID initialization changed'}
 $log=$session.GetType().GetField('LogFile',$flags).GetValue($session)
 [IO.File]::WriteAllText($log,'[{"type":"singleplayer","id":"Test world","name":"Test world","lastPlayedTime":"2026-10-24T00:00:00Z","gamemode":"survival"}]')
 Call $quick 'Finish' @($session)|Out-Null
 $recent=Call $quick 'Recent' @();$missing=$recent.Count -eq 0
 if($recent.Count -ne 1 -or $recent[0].GetType().FullName -cne 'PCL.RecentWorld'){throw 'Recent worlds did not preserve the original VB row type'}
 foreach($name in @('VersionPath','GameFolder','Identifier','Name','PlayedUtc')){if($null -eq $recent[0].GetType().GetField($name,$flags)){throw ('Recent world field reflection contract changed: '+$name)}}
 $played=$recent[0].GetType().GetField('PlayedUtc',$flags).GetValue($recent[0].PSObject.BaseObject)
 if($played.Kind -ne [DateTimeKind]::Utc -or $played -ne [DateTime]::new(2026,10,24,0,0,0,[DateTimeKind]::Utc)){throw 'Mojang Quick Play history lost its actual UTC timestamp'}
 # Quick Play recovery reads live logs; exited sessions and failed Finish still clean both files.
 $liveArguments=[Collections.Generic.List[string]]::new();$liveSession=Call $quick 'Prepare' @($instance,$null,$liveArguments,$false)
 $liveLog=$liveSession.GetType().GetField('LogFile',$flags).GetValue($liveSession.PSObject.BaseObject)
 $liveId=$liveSession.GetType().GetField('Id',$flags).GetValue($liveSession.PSObject.BaseObject)
 $journalName='quickplay-sessions/'+$liveId+'.json';$journalPath=Call $data 'Address' @($journalName)
 Call $quick 'Track' @($liveSession,[Diagnostics.Process]::GetCurrentProcess())|Out-Null
 [IO.File]::WriteAllText($liveLog,'[{"type":"singleplayer","id":"Test world","name":"Live recovered world","lastPlayedTime":"2026-10-25T00:00:00Z"}]')
 Call $quick 'RecoverExitedSessions' @()|Out-Null
 $liveRecent=Call $quick 'Recent' @()
 if($liveRecent.Count -ne 1 -or $liveRecent[0].GetType().GetField('Name',$flags).GetValue($liveRecent[0].PSObject.BaseObject) -cne 'Live recovered world'){throw 'Quick Play live recovery did not read the real log'}
 if(-not [IO.File]::Exists($liveLog) -or -not [IO.File]::Exists($journalPath) -or -not (Call $quick 'HasRunningSessions' @())){throw 'Quick Play live recovery deleted the tracked session'}
 $enabledBefore=$settings['LaunchRecentWorlds'].ValueCache['']
 try{$settings['LaunchRecentWorlds'].ValueCache['']=$false;if(-not (Call $quick 'HasRunningSessions' @())){throw 'Disabled recent-world UI hid a live Quick Play session'}}finally{$settings['LaunchRecentWorlds'].ValueCache['']=$enabledBefore}
 $journal=Call $data 'ReadJson' @($journalName);$journal['pid']=[Newtonsoft.Json.Linq.JValue]::new(2147483646);$journal['started']=[Newtonsoft.Json.Linq.JValue]::new([long]1)
 Call $data 'WriteJson' @($journalName,$journal)|Out-Null
 Call $quick 'RecoverExitedSessions' @()|Out-Null
 if([IO.File]::Exists($liveLog) -or [IO.File]::Exists($journalPath) -or (Call $quick 'HasRunningSessions' @())){throw 'Quick Play exited-session recovery did not finish cleanup'}
 $badArguments=[Collections.Generic.List[string]]::new();$badSession=Call $quick 'Prepare' @($instance,$null,$badArguments,$false)
 Call $quick 'Track' @($badSession,[Diagnostics.Process]::GetCurrentProcess())|Out-Null
 $badLog=$badSession.GetType().GetField('LogFile',$flags).GetValue($badSession.PSObject.BaseObject)
 $badId=$badSession.GetType().GetField('Id',$flags).GetValue($badSession.PSObject.BaseObject)
 $badJournal=Call $data 'Address' @('quickplay-sessions/'+$badId+'.json');[IO.File]::WriteAllText($badLog,'{')
 $finishFailed=$false;try{Call $quick 'Finish' @($badSession)|Out-Null}catch{$finishError=$_.Exception;while($null -ne $finishError.InnerException){$finishError=$finishError.InnerException};if($finishError -isnot [Newtonsoft.Json.JsonReaderException]){throw};$finishFailed=$true}
 if(-not $finishFailed -or [IO.File]::Exists($badLog) -or [IO.File]::Exists($badJournal)){throw 'Quick Play failed-log Finish lost its original finally cleanup'}
 [pscustomobject]@{FirstRunKeyPageBlank=$blank;MojangIdLogLost=$missing}|ConvertTo-Json -Compress
 if($blank -or $missing){throw 'Observed first-run/Quick Play failure reproduced'}
 $defaults=@($rows|Where-Object {$_.GetType().GetField('Saved',$flags).GetValue($_.PSObject.BaseObject) -eq $false})
 if($defaults.Count -lt 20){throw 'First-run defaults were not distinguished from saved bindings'}
 Call $bindings 'Save' @($version,'key_key.forward',$null,'key.keyboard.r')|Out-Null
 if([IO.File]::ReadAllText((Join-Path $version 'options.txt')) -notmatch 'key_key.forward:key.keyboard.r'){throw 'Editing a default did not create only the requested binding'}
 $afterEdit=Call $bindings 'ReadBindings' @($version,[Threading.CancellationToken]::None)
 if($afterEdit.Count -lt 20){throw 'Editing one default hid the remaining default bindings'}
 $history=Call $data 'Address' @('recent-worlds.json');[IO.File]::Delete([string]$history)
 $level=Join-Path $world 'level.dat';$stream=[IO.FileStream]::new($level,[IO.FileMode]::Create,[IO.FileAccess]::Write);$zip=[IO.Compression.GZipStream]::new($stream,[IO.Compression.CompressionMode]::Compress)
 function Write-Name($targetStream,[string]$text){$bytes=[Text.Encoding]::UTF8.GetBytes($text);$targetStream.WriteByte([byte]($bytes.Length -shr 8));$targetStream.WriteByte([byte]($bytes.Length -band 255));$targetStream.Write($bytes,0,$bytes.Length)}
 try{
  $zip.WriteByte(10);Write-Name $zip ''; $zip.WriteByte(10);Write-Name $zip 'Data'
  $zip.WriteByte(8);Write-Name $zip 'LevelName';Write-Name $zip 'Saved display name'
  $zip.WriteByte(4);Write-Name $zip 'LastPlayed';$millis=[DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds();$time=[BitConverter]::GetBytes([long]$millis);if([BitConverter]::IsLittleEndian){[Array]::Reverse($time)};$zip.Write($time,0,8);$zip.WriteByte(0);$zip.WriteByte(0)
 }finally{$zip.Dispose();$stream.Dispose()}
 Call $quick 'DiscoverExistingWorlds' @($instance)|Out-Null
 $recent=Call $quick 'Recent' @()
 if($recent.Count -ne 1 -or $recent[0].GetType().GetField('Name',$flags).GetValue($recent[0].PSObject.BaseObject) -cne 'Saved display name'){throw 'An existing isolated world was not recovered from actual level.dat metadata'}
 $badMetadata=Join-Path $version 'saves/Corrupt gzip/level.dat';[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($badMetadata))|Out-Null;[IO.File]::WriteAllText($badMetadata,'not-a-gzip-fixture')
 $badMetadataBytes=[Convert]::ToBase64String([IO.File]::ReadAllBytes($badMetadata))
 $metadataRejected=$false;try{Call $quick 'DiscoverExistingWorlds' @($instance)|Out-Null}catch{$metadataError=$_.Exception;while($null -ne $metadataError.InnerException){$metadataError=$metadataError.InnerException};if($metadataError -isnot [IO.InvalidDataException]){throw};$metadataRejected=$true}
 if(-not $metadataRejected -or [Convert]::ToBase64String([IO.File]::ReadAllBytes($badMetadata)) -cne $badMetadataBytes){throw 'Isolated-world discovery changed the original InvalidDataException boundary or world bytes'}
 'Game regressions passed: first-run default bindings, inherited VB fields, Mojang id/UTC, live and exited Quick Play recovery, failed-log finally cleanup, isolated-world discovery and corrupt-gzip exception boundary.'
}finally{
 $data.GetProperty('TestRoot',$flags).SetValue($null,$null,$null)
 $full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '\APclGameRegression-[a-f0-9]{32}\z'){throw 'Unsafe regression fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
