param([Parameter(Mandatory=$true)][string]$ExecutablePath)
# Exercises the real detail-page filter with isolated loader output; no app entry point or real profile.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='map-details-fixture'
Add-Type -AssemblyName System.IO.Compression
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclWorldDetails-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($fixture)|Out-Null
function Name($stream,[string]$value){$bytes=[Text.Encoding]::UTF8.GetBytes($value);$stream.WriteByte([byte]($bytes.Length -shr 8));$stream.WriteByte([byte]($bytes.Length -band 255));$stream.Write($bytes,0,$bytes.Length)}
function Header($stream,[int]$kind,[string]$name){$stream.WriteByte([byte]$kind);Name $stream $name}
function Number($stream,[int]$kind,[string]$name,$value){Header $stream $kind $name;if($kind -eq 1){$stream.WriteByte([byte]$value);return};$bytes=if($kind -eq 3){[BitConverter]::GetBytes([int]$value)}else{[BitConverter]::GetBytes([long]$value)};if([BitConverter]::IsLittleEndian){[Array]::Reverse($bytes)};$stream.Write($bytes,0,$bytes.Length)}
function Text($stream,[string]$name,[string]$value){Header $stream 8 $name;Name $stream $value}
function AssertReleased([string]$file){$exclusive=[IO.FileStream]::new($file,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$exclusive.Dispose()}
$type=$asm.GetType('PCL.WorldMetadata');$path=Join-Path $fixture 'level.dat'
try {
 $output=[IO.File]::Create($path);$zip=[IO.Compression.GZipStream]::new($output,[IO.Compression.CompressionMode]::Compress)
 try {
  Header $zip 10 '';Header $zip 10 'Data';Text $zip 'LevelName' 'Fixture world';Number $zip 4 'LastPlayed' ([long]1730000000000)
  # Important fields follow the two basic fields; detail reading must not stop early.
  Number $zip 3 'GameType' 1;Number $zip 1 'Difficulty' 3;Number $zip 1 'hardcore' 1;Number $zip 1 'allowCommands' 0;Number $zip 1 'WasModded' 1
  Number $zip 3 'DataVersion' 3465;Number $zip 3 'SpawnX' -32;Number $zip 3 'SpawnY' 70;Number $zip 3 'SpawnZ' 256;Number $zip 4 'DayTime' 48000;Number $zip 4 'Time' 92000
  Header $zip 10 'Version';Text $zip 'Name' '1.20.1';Number $zip 1 'Snapshot' 0;$zip.WriteByte(0)
  Header $zip 10 'Unknown';Number $zip 3 'Ignored' 42;$zip.WriteByte(0)
  Header $zip 10 'WorldGenSettings';Number $zip 4 'seed' ([long]-9876543210123);$zip.WriteByte(0)
  $zip.WriteByte(0);$zip.WriteByte(0)
 }finally{$zip.Dispose();$output.Dispose()}
 $before=(Get-FileHash -LiteralPath $path).Hash
 $result=$type.GetMethod('ReadDetails',$flags).Invoke($null,@([string]$fixture))
 foreach($pair in @(
  @('Name','Fixture world'),@('LastPlayedUtc',[DateTime]::new(2024,10,27,3,33,20,[DateTimeKind]::Utc)),@('VersionName','1.20.1'),@('DataVersion',3465),
  @('GameMode',1),@('Difficulty',3),@('Hardcore',$true),@('AllowCommands',$false),@('WasModded',$true),@('Snapshot',$false),
  @('Seed',[long]-9876543210123),@('SpawnX',-32),@('SpawnY',70),@('SpawnZ',256),@('DayTime',[long]48000),@('Time',[long]92000)
 )){if($type.GetField($pair[0],$flags).GetValue($result) -ne $pair[1]){throw ('World detail field mismatch: '+$pair[0])}}
 $caption=$type.GetMethod('Describe',$flags).Invoke($null,@([string]$fixture))
 foreach($text in @('Fixture world','1.20.1','创造','困难','极限模式：开启','允许作弊：关闭','-9876543210123','-32, 70, 256','第 2 天',$fixture)){if(-not $caption.Contains($text)){throw ('World caption missing: '+$text)}}
 $date=([DateTime]::new(2024,10,27,3,33,20,[DateTimeKind]::Utc)).ToLocalTime().ToString('yyyy-MM-dd HH:mm:ss')
 $expected=@('世界名称：Fixture world',('存档文件夹：'+[IO.DirectoryInfo]::new($fixture).Name),'游戏版本：1.20.1','数据版本：3465',('最后游玩：'+$date),
  '游戏模式：创造','难度：困难','极限模式：开启','允许作弊：关闭','模组环境记录：使用过','世界种子：-9876543210123','世界出生点：-32, 70, 256','游戏日：第 2 天','世界累计刻数：92000','',('位置：'+$fixture)) -join "`r`n"
 if($caption -cne $expected){throw 'World caption changed its exact field order, CRLF, spacing or wording'}
 $basic=$type.GetMethod('Read',$flags).Invoke($null,@([string]$fixture));if($null -eq $basic -or $type.GetField('Name',$flags).GetValue($basic) -ne 'Fixture world'){throw 'Recent-world metadata reading regressed'}
 foreach($field in @('VersionName','DataVersion','GameMode','Difficulty','Hardcore','AllowCommands','WasModded','Snapshot','Seed','SpawnX','SpawnY','SpawnZ','DayTime','Time')){if($null -ne $type.GetField($field,$flags).GetValue($basic)){throw ('Basic metadata unexpectedly read detail field: '+$field)}}
 AssertReleased $path
 $writer=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
 try{if($null -eq $type.GetMethod('ReadDetails',$flags).Invoke($null,@([string]$fixture))){throw 'World metadata could not share a game writer handle'}}finally{$writer.Dispose()}
 $attributes=[IO.File]::GetAttributes($path)
 try{[IO.File]::SetAttributes($path,($attributes -bor [IO.FileAttributes]::ReadOnly));if($type.GetMethod('Describe',$flags).Invoke($null,@([string]$fixture)) -cne $expected){throw 'Read-only world metadata changed'}}finally{[IO.File]::SetAttributes($path,$attributes)}
 AssertReleased $path
 if((Get-FileHash -LiteralPath $path).Hash -cne $before){throw 'Reading details modified the world'}
 $legacy=Join-Path $fixture 'legacy';[IO.Directory]::CreateDirectory($legacy)|Out-Null;$output=[IO.File]::Create((Join-Path $legacy 'level.dat'));$zip=[IO.Compression.GZipStream]::new($output,[IO.Compression.CompressionMode]::Compress)
 try{Header $zip 10 '';Header $zip 10 'Data';Text $zip 'LevelName' 'Old world';Number $zip 4 'RandomSeed' -42;$zip.WriteByte(0);$zip.WriteByte(0)}finally{$zip.Dispose();$output.Dispose()}
 $old=$type.GetMethod('Describe',$flags).Invoke($null,@([string]$legacy));if(-not $old.Contains('Old world') -or -not $old.Contains('-42') -or $old.Contains('游戏模式：')){throw 'Legacy/absent field handling is incorrect'}
 if($null -ne $type.GetMethod('Read',$flags).Invoke($null,@([string]$legacy))){throw 'Legacy world without LastPlayed became a recent-world entry'}
 # Exercise the real launcher path adapter; the C# core must not bypass reparse checks.
 $linkedFolder=Join-Path $fixture 'linked';[IO.Directory]::CreateDirectory($linkedFolder)|Out-Null;$linkedFile=Join-Path $linkedFolder 'level.dat'
 try{
  New-Item -ItemType SymbolicLink -Path $linkedFile -Target $path -ErrorAction Stop|Out-Null
  $rejected=$false;try{$type.GetMethod('ReadDetails',$flags).Invoke($null,@([string]$linkedFolder))|Out-Null}catch{$cause=$_.Exception;while($cause.InnerException){$cause=$cause.InnerException};if($cause -is [IO.IOException] -and $cause.Message.Contains('链接文件')){$rejected=$true}else{throw}}
  if(-not $rejected){throw 'World metadata bypassed the real file-link path guard'}
  if(-not $type.GetMethod('Describe',$flags).Invoke($null,@([string]$linkedFolder)).Contains('无法读取')){throw 'Rejected world link did not produce the corrupt/unavailable caption'}
 }finally{if([IO.File]::Exists($linkedFile)){[IO.File]::Delete($linkedFile)}}
 if((Get-FileHash -LiteralPath $path).Hash -cne $before){throw 'Link validation modified the target world'}
 [IO.File]::WriteAllText($path,'invalid gzip')
 $thrown=$false;try{$type.GetMethod('ReadDetails',$flags).Invoke($null,@([string]$fixture))|Out-Null}catch{$thrown=$true};if(-not $thrown){throw 'ReadDetails swallowed a corrupt-file error'}
 $bad=$type.GetMethod('Describe',$flags).Invoke($null,@([string]$fixture));if(-not $bad.Contains('无法读取') -or -not $bad.Contains($fixture)){throw 'Corrupt metadata did not produce local folder details'}
 AssertReleased $path
 $missing=$type.GetMethod('Describe',$flags).Invoke($null,@([string](Join-Path $fixture 'missing')));if(-not $missing.Contains('未找到')){throw 'Missing metadata was not explained'}
 Write-Output 'World details passed: all 16 fields, exact CRLF caption, basic/legacy distinctions, shared writer, read-only data, real file-link guard, missing/corrupt fallback and closed file handles.'
}finally{
 $full=[IO.Path]::GetFullPath($fixture);if($full.StartsWith([IO.Path]::GetTempPath(),[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($full).StartsWith('PclWorldDetails-')){Remove-Item -LiteralPath $full -Recurse -Force}
}
