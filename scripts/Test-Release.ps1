param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$LegacyExecutablePath,[string]$OfficialExecutablePath)
# 使用虚构账号和独立的测试注册表分支；不读取真实账号数据，不启动应用窗口。
$ExecutablePath=[IO.Path]::GetFullPath($ExecutablePath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
$root=Split-Path $PSScriptRoot -Parent
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll')) { [Reflection.Assembly]::LoadFrom((Join-Path $root $path)) | Out-Null }
$asm=[Reflection.Assembly]::LoadFrom($ExecutablePath)
$product=[Diagnostics.FileVersionInfo]::GetVersionInfo($ExecutablePath)
$executableName='PCL-Living-Fork-'+$product.ProductVersion+'.exe'
if([IO.Path]::GetFileName($ExecutablePath) -cne $executableName -or -not [IO.File]::Exists($ExecutablePath+'.config')){throw 'Versioned build executable/config naming mismatch'}
if($asm.GetType('PCL.ModBase').GetField('LauncherExecutableName').GetRawConstantValue() -cne $executableName){throw 'Build filename does not match export/update runtime naming'}
$type=$asm.GetType('PCL.Settings',$true)
$flags=[Reflection.BindingFlags]'NonPublic,Static'
$enc=$type.GetMethod('DesEncrypt',$flags);$dec=$type.GetMethod('DesDecrypt',$flags)
$makePlan=$type.GetMethod('PlanPclAccountMigration',$flags);$apply=$type.GetMethod('ApplyPclAccountMigration',$flags)
$entries=$type.GetField('Entries').GetValue($null)
function New-Map { return ,([Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)) }
function Encode([string]$value,[string]$id) { return $enc.Invoke($null,@($value,('PCL'+$id))) }
function Decode([string]$value,[string]$id) { return $dec.Invoke($null,@($value,('PCL'+$id))) }
$official=New-Map;$legacy=New-Map
$oldId='1111-2222-3333-4444';$legacyId='0000-0000-0000-0000';$currentId='AAAA-BBBB-CCCC-DDDD'
$official['LoginMsJson']=Encode '{"NativeOnly":"native-token","Shared":"old-token"}' $oldId
$official['CacheMsV2Name']=Encode 'OldActive' $oldId
$official['CacheMsV2OAuthRefresh']=Encode 'old-refresh' $oldId
$official['CacheAuthClient']=Encode 'auth-client' $oldId
$official['CacheAuthPass']=Encode 'auth-password' $oldId
$official['SystemCount']=Encode '7' $oldId
$legacy['LoginMsJson']=Encode '{"LocalOnly":"local-token","shared":"new-token"}' $legacyId
$legacy['CacheMsV2OAuthRefresh']=Encode 'new-refresh' $legacyId
$legacy['CacheMsV2Name']=Encode 'NewActive' $legacyId
$legacy['LoginLegacyName']=Encode 'PlayerA' $legacyId
$legacy['UiLauncherThemeHide']=Encode '0|1|2|3|4|7' $legacyId
$plan=$makePlan.Invoke($null,@($official,$legacy,[string[]]@($currentId,$oldId),[string[]]@($legacyId),$currentId))
$accounts=Decode $plan.Values['LoginMsJson'] $currentId | ConvertFrom-Json
if(@($accounts.PSObject.Properties).Count -ne 3 -or $accounts.shared -ne 'new-token' -or $accounts.NativeOnly -ne 'native-token') { throw 'Account list merge failed' }
if((Decode $plan.Values['CacheMsV2Name'] $currentId) -ne 'NewActive') { throw 'Cache group selection failed' }
if((Decode $plan.Values['CacheAuthPass'] $currentId) -ne 'auth-password') { throw 'Historical auth encryption conversion failed' }
if($plan.Values.ContainsKey('SystemCount') -or $plan.DeleteLegacy.Contains('UiLauncherThemeHide')) { throw 'Excluded settings were included' }
$testPath='Software\LivingPCL-CompatibilityTests\'+[Guid]::NewGuid().ToString('N')
$testNative=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($testPath+'\Native')
$testLegacy=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($testPath+'\Legacy')
try {
 foreach($kv in $official.GetEnumerator()) { $testNative.SetValue($kv.Key,$kv.Value) }
 foreach($kv in $legacy.GetEnumerator()) { $testLegacy.SetValue($kv.Key,$kv.Value) }
 $apply.Invoke($null,@($plan,$testNative,$testLegacy)) | Out-Null
 foreach($name in $plan.DeleteLegacy) { if($null -ne $testLegacy.GetValue($name,$null)) { throw 'Old account storage was retained' } }
 if($null -eq $testLegacy.GetValue('UiLauncherThemeHide',$null)) { throw 'Excluded state removed' }
 $native2=New-Map;$legacy2=New-Map
 foreach($name in $testNative.GetValueNames()) { $native2[$name]=[string]$testNative.GetValue($name) }
 foreach($name in $testLegacy.GetValueNames()) { $legacy2[$name]=[string]$testLegacy.GetValue($name) }
 $again=$makePlan.Invoke($null,@($native2,$legacy2,[string[]]@($currentId,$oldId),[string[]]@($legacyId),$currentId))
 if($again.Values.Count -ne 0 -or $again.DeleteLegacy.Count -ne 0) { throw 'Migration is not idempotent' }
 $bad=$makePlan.Invoke($null,@($official,$legacy,[string[]]@($currentId,$oldId),[string[]]@($legacyId),$currentId))
 $bad.Values['CacheAuthPass']='invalid-cipher'
 $snapshot=@{};foreach($name in $testNative.GetValueNames()) { $snapshot[$name]=$testNative.GetValue($name) }
 $failed=$false
 try { $apply.Invoke($null,@($bad,$testNative,$testLegacy)) | Out-Null } catch { $failed=$true }
 if(-not $failed) { throw 'Invalid migration was accepted' }
 foreach($name in $snapshot.Keys) { if($snapshot[$name] -cne $testNative.GetValue($name)) { throw 'Rollback failed' } }
 Write-Output 'Synthetic migration: account merge, cache grouping, official DES format, old-value deletion, exclusions, idempotence and rollback passed.'
} finally {
 $testNative.Dispose();$testLegacy.Dispose()
 if(-not $testPath.StartsWith('Software\LivingPCL-CompatibilityTests\')) { throw 'Invalid test cleanup path' }
 [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($testPath,$false)
}

$updater=$asm.GetType('PCL.ModLauncherUpdate',$true)
$new=$updater.GetMethod('NewerRelease',$flags)
foreach($case in @(@('v1.0.1','1.0.0',$true),@('v1.0.1.0','1.0.1',$false),@('v0.9.9','1.0.1',$false),@('v1.0.2-beta','1.0.1',$false))) {
 if($new.Invoke($null,@($case[0],$case[1])) -ne $case[2]) { throw 'Version comparison failed' }
}
$valid=$updater.GetMethod('ValidAssetUrl',$flags)
if(-not $valid.Invoke($null,@('https://github.com/DDZmumo/LivingPCL2-Re/releases/download/v1.0.1/LivingPCL2-Re-1.0.1.exe'))) { throw 'Own release URL rejected' }
if(-not $valid.Invoke($null,@('https://github.com/DDZmumo/PCL-Living-Fork/releases/download/v1.2.0/PCL-Living-Fork-1.2.0.exe'))) { throw 'Renamed repository release URL rejected' }
foreach($url in @('http://github.com/DDZmumo/LivingPCL2-Re/releases/download/v1.0.1/a.exe','https://github.com/Other/PCL/releases/download/v1.0.1/a.exe','https://example.com/a.exe','https://github.com/DDZmumo/LivingPCL2-Re-other/releases/download/a.exe')) {
 if($valid.Invoke($null,@($url))) { throw 'Untrusted release URL accepted' }
}
$oldAsset=@{name='LivingPCL2-Re-1.2.0.exe';browser_download_url='https://github.com/DDZmumo/LivingPCL2-Re/releases/download/v1.2.0/LivingPCL2-Re-1.2.0.exe'}
$newAsset=@{name='PCL-Living-Fork-1.2.0.exe';browser_download_url='https://github.com/DDZmumo/PCL-Living-Fork/releases/download/v1.2.0/PCL-Living-Fork-1.2.0.exe'}
$spacedAsset=@{name='PCL Living Fork - 1.2.0.exe';browser_download_url='https://github.com/DDZmumo/PCL-Living-Fork/releases/download/v1.2.0/PCL%20Living%20Fork%20-%201.2.0.exe'}
$versionedAsset=@{name=$executableName;browser_download_url=('https://github.com/DDZmumo/PCL-Living-Fork/releases/download/v'+$product.ProductVersion+'/'+[uri]::EscapeDataString($executableName))}
foreach($case in @(@{assets=@($oldAsset);expected=$oldAsset.name},@{assets=@($oldAsset,$newAsset);expected=$newAsset.name},@{assets=@($oldAsset,$spacedAsset,$versionedAsset);expected=$versionedAsset.name})){
 $info=[Newtonsoft.Json.Linq.JObject]::Parse((@{assets=$case.assets}|ConvertTo-Json -Depth 5))
 $selected=$updater.GetMethod('ReleaseExecutable',$flags).Invoke($null,[object[]](,$info))
 if($selected['name'].ToString() -cne $case.expected){throw 'Updater chose the wrong renamed/legacy release asset'}
}
$hash=[string](Get-FileHash -LiteralPath $ExecutablePath -Algorithm SHA256).Hash
$manifest=$hash.ToLowerInvariant()+'  LivingPCL2-Re-1.0.1.exe'
$parsed=$updater.GetMethod('ManifestHash',$flags).Invoke($null,@($manifest,'LivingPCL2-Re-1.0.1.exe'))
if($parsed -cne $hash.ToLowerInvariant()) { throw 'SHA256 manifest parsing failed' }
$parsed=$updater.GetMethod('ManifestHash',$flags).Invoke($null,@(($hash.ToLowerInvariant()+'  '+$spacedAsset.name),$spacedAsset.name))
if($parsed -cne $hash.ToLowerInvariant()){throw 'SHA256 parsing failed for a versioned filename with spaces'}
$folder=Join-Path ([IO.Path]::GetTempPath()) ('LivingPCL-UpdateTest-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($folder) | Out-Null
$target=Join-Path $folder 'LivingPCL.exe'
try {
 [IO.File]::Copy($ExecutablePath,$target)
 [IO.File]::AppendAllText($target,'synthetic-old-version-overlay')
 $before=[string](Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
 $replace=$updater.GetMethod('ReplaceExecutable',$flags)
 $rejected=$false
 try { $replace.Invoke($null,@($ExecutablePath,[string]$target,('0'*64),$false)) | Out-Null } catch { $rejected=$true }
 if(-not $rejected -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $before) { throw 'Invalid update failed to preserve the old EXE' }
 $replace.Invoke($null,@($ExecutablePath,[string]$target,[string]$hash,$false)) | Out-Null
 if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $hash) { throw 'EXE replacement failed' }
 if(@(Get-ChildItem -LiteralPath $folder).Count -ne 1) { throw 'Update left temporary files' }
 $naming=Join-Path $folder 'naming'
 try{
  $names=@('PCL-Living-Fork-1.3.3.exe','PCL Living Fork - 1.3.3.exe','LivingPCL2-Re-1.1.2.exe','My custom launcher.exe')
  for($i=0;$i -lt $names.Count;$i++){
   $caseFolder=Join-Path $naming ([string]$i);[IO.Directory]::CreateDirectory($caseFolder)|Out-Null
   $previous=Join-Path $caseFolder $names[$i];[IO.File]::Copy($ExecutablePath,$previous);[IO.File]::AppendAllText($previous,'synthetic-old-version')
   [IO.File]::WriteAllText(($previous+'.config'),'synthetic-runtime-config')
   $expected=Join-Path $caseFolder $(if($i -eq 3){$names[$i]}else{$executableName})
   if($updater.GetMethod('UpdatedExecutablePath',$flags).Invoke($null,@([string]$previous)) -cne $expected){throw 'Update filename selection is incorrect'}
   $replace.Invoke($null,@($ExecutablePath,[string]$previous,$hash,$false))|Out-Null
   if((Get-FileHash -LiteralPath $expected -Algorithm SHA256).Hash -cne $hash -or [IO.File]::ReadAllText(($expected+'.config')) -cne 'synthetic-runtime-config'){throw 'Renamed update lost executable/config content'}
   if($i -lt 3 -and ([IO.File]::Exists($previous) -or [IO.File]::Exists($previous+'.config'))){throw 'The standard filename still contains the previous version'}
   if(@([IO.Directory]::EnumerateFiles($caseFolder)).Count -ne 2){throw 'Renamed update left temporary files'}
  }
  foreach($collision in @('exe','config')){
   $caseFolder=Join-Path $naming ('collision-'+$collision);[IO.Directory]::CreateDirectory($caseFolder)|Out-Null
   $previous=Join-Path $caseFolder 'PCL-Living-Fork-0.0.1.exe';[IO.File]::Copy($ExecutablePath,$previous);[IO.File]::WriteAllText(($previous+'.config'),'previous-config')
   $occupied=(Join-Path $caseFolder $executableName)+$(if($collision -eq 'config'){'.config'}else{''});[IO.File]::WriteAllText($occupied,'existing-file-preserve')
   $rejected=$false;try{$replace.Invoke($null,@($ExecutablePath,[string]$previous,$hash,$false))|Out-Null}catch{$rejected=$true}
   if(-not $rejected -or [IO.File]::ReadAllText($occupied) -cne 'existing-file-preserve' -or -not [IO.File]::Exists($previous)){throw 'Update rename overwrote an occupied destination'}
  }
  $caseFolder=Join-Path $naming 'rollback';[IO.Directory]::CreateDirectory($caseFolder)|Out-Null
  $previous=Join-Path $caseFolder 'PCL-Living-Fork-0.0.1.exe';[IO.File]::Copy($ExecutablePath,$previous);[IO.File]::AppendAllText($previous,'previous-binary');$previousHash=(Get-FileHash $previous -Algorithm SHA256).Hash
  [IO.File]::WriteAllText(($previous+'.config'),'previous-config')
  $failure=[Action]{throw 'synthetic restart failure'};$rejected=$false
  try{$updater.GetMethod('ReplaceExecutableCore',$flags).Invoke($null,@($ExecutablePath,[string]$previous,$hash,$false,$failure.PSObject.BaseObject))|Out-Null}catch{$rejected=$true}
  if(-not $rejected -or (Get-FileHash $previous -Algorithm SHA256).Hash -cne $previousHash -or [IO.File]::ReadAllText(($previous+'.config')) -cne 'previous-config' -or @([IO.Directory]::EnumerateFiles($caseFolder)).Count -ne 2){throw 'Failed renamed update did not restore the original executable/config'}
  Write-Output 'Update naming: standard names/version/config synchronized, custom names preserved, collisions blocked and restart failure rolled back.'
 }finally{
  $full=[IO.Path]::GetFullPath($naming);$prefix=[IO.Path]::GetFullPath($folder).TrimEnd('\')+'\'
  if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe update-name fixture cleanup'}
  if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
 }
 if($LegacyExecutablePath){
  [IO.File]::Copy([IO.Path]::GetFullPath($LegacyExecutablePath),$target,$true)
  $replace.Invoke($null,@($ExecutablePath,[string]$target,[string]$hash,$false)) | Out-Null
  if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $hash){throw 'Legacy LivingPCL could not migrate to the new version'}
 }
 if($OfficialExecutablePath){
  $officialPath=[IO.Path]::GetFullPath($OfficialExecutablePath)
  $officialHash=[string](Get-FileHash -LiteralPath $officialPath -Algorithm SHA256).Hash
  $validate=$updater.GetMethod('ValidateExecutable',$flags)
  $rejected=$false
  try{$validate.Invoke($null,@($officialPath,$officialHash))|Out-Null}catch{$rejected=$true}
  if(-not $rejected){throw 'Official PCL was mistaken for LivingPCL update content'}
  [IO.File]::Copy($officialPath,$target,$true)
  $rejected=$false
  try{$replace.Invoke($null,@($ExecutablePath,[string]$target,[string]$hash,$false))|Out-Null}catch{$rejected=$true}
  if(-not $rejected -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $officialHash){throw 'Updater overwrote an official PCL target'}
 }
 Write-Output 'Updater: versions, own-repository URLs, SHA256 parsing, EXE replacement and invalid-update preservation passed.'
} finally {
 if([IO.File]::Exists($target)) { [IO.File]::Delete($target) }
 [IO.Directory]::Delete($folder,$false)
}
