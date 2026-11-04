param([Parameter(Mandatory=$true)][string]$ExecutablePath)
# Synthetic encrypted values under a GUID registry branch. Never reads real accounts or contacts Microsoft.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$type=$asm.GetType('PCL.Settings');$flags=[Reflection.BindingFlags]'Public,NonPublic,Static'
$entries=$type.GetField('Entries',$flags).GetValue($null);$name=$type.GetMethod('RegistryValueName',$flags);$snapshot=$type.GetMethod('ReadPclAccountRegistrySnapshot',$flags)
if(-not $name -or -not $snapshot){throw 'Microsoft registry isolation is missing'}
$encode=$type.GetMethod('DesEncrypt',$flags);$decode=$type.GetMethod('DesDecrypt',$flags);$id='AAAA-BBBB-CCCC-DDDD'
function Cipher([string]$text){return $encode.Invoke($null,@($text,('PCL'+$id)))}
function Stored([string]$key){return $name.Invoke($null,@($entries[$key]))}
$path='Software\LivingPCL-MicrosoftIsolationTests\'+[Guid]::NewGuid().ToString('N')
$store=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($path+'\Current');$old=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($path+'\Legacy')
try{
 $keys=@($entries.Keys|Where-Object {$_ -eq 'LoginMsJson' -or $_.StartsWith('CacheMs')})
 foreach($key in $keys){if((Stored $key) -cne ($key+'_LF')){throw ('Microsoft key is still shared: '+$key)}}
 foreach($key in @('Identify','LoginType','CacheAuthAccess','CacheNidePass','LaunchFolders','UiLauncherTheme')){if((Stored $key) -cne $key){throw ('Unrelated configuration was renamed: '+$key)}}
 $foreign=Cipher '{"Foreign":"foreign-refresh"}';$store.SetValue('LoginMsJson',$foreign);$store.SetValue('CacheMsV2OAuthRefresh',(Cipher 'foreign-session'))
 $raw=$snapshot.Invoke($null,@($store,$false));if($raw.ContainsKey('LoginMsJson') -or $raw.ContainsKey('CacheMsV2OAuthRefresh')){throw 'Unsuffixed foreign Microsoft credentials were imported'}
 $old.SetValue('LoginMsJson',$foreign);$oldRaw=$snapshot.Invoke($null,@($old,$true));if($oldRaw.ContainsKey('LoginMsJson') -or $old.GetValue('LoginMsJson') -cne $foreign){throw 'Unattributed private legacy Microsoft credentials were imported or removed'}
 $plan=[Activator]::CreateInstance($asm.GetType('PCL.Settings+PclAccountMigrationPlan'));$plan.CurrentIdentify=$id
 $plan.Values['LoginMsJson']=Cipher '{"Fork":"fork-refresh"}';$plan.Values['CacheMsV2OAuthRefresh']=Cipher 'fork-session';$plan.Values['CacheMsV2Migrated']='True'
 $apply=$type.GetMethod('ApplyPclAccountMigration',$flags);$apply.Invoke($null,@($plan,$store,$null))|Out-Null
 if($store.GetValue('LoginMsJson') -cne $foreign -or -not $store.GetValue('LoginMsJson_LF')){throw 'Saving LF accounts changed the shared account list'}
 $raw=$snapshot.Invoke($null,@($store,$false));if(($decode.Invoke($null,@($raw['LoginMsJson'],('PCL'+$id)))) -cne '{"Fork":"fork-refresh"}'){throw 'LF account readback selected foreign credentials'}
 # Replacing LF credentials and logging out preserve every foreign key byte for byte.
 $plan.Values['LoginMsJson']=Cipher '{}';$plan.Values['CacheMsV2OAuthRefresh']=Cipher '';$apply.Invoke($null,@($plan,$store,$null))|Out-Null
 if($store.GetValue('LoginMsJson') -cne $foreign -or ($decode.Invoke($null,@($store.GetValue('CacheMsV2OAuthRefresh'),('PCL'+$id)))) -cne 'foreign-session'){throw 'LF logout altered foreign Microsoft credentials'}
 # An explicit LF logout must not be revived from an old private cache.
 $legacy=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal);$legacy['LoginMsJson']=Cipher '{"OldFork":"stale"}'
 $raw=$snapshot.Invoke($null,@($store,$false));$migration=$type.GetMethod('PlanPclAccountMigration',$flags).Invoke($null,@($raw,$legacy,[string[]]@($id),[string[]]@($id),$id))
 if($migration.Values.Count -or $migration.DeleteLegacy.Contains('LoginMsJson')){throw 'Old private cache overwrote an existing LF logout'}
 $reset=$type.GetMethod('ResetRegistryPreference',$flags);$reset.Invoke($null,@($store,$entries['LoginMsJson']))|Out-Null
 if($store.GetValue('LoginMsJson_LF',$null) -ne $null -or $store.GetValue('LoginMsJson') -cne $foreign){throw 'Reset targeted the foreign registry key'}
 Write-Output ('Microsoft registry isolation passed: '+$keys.Count+' namespaced keys, shared-preference preservation, foreign-token exclusion, save/logout/reset isolation and stale-cache guards.')
}finally{
 $store.Dispose();$old.Dispose()
 if($path -notmatch '^Software\\LivingPCL-MicrosoftIsolationTests\\[a-f0-9]{32}$'){throw 'Unsafe registry fixture cleanup'}
 [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($path,$false)
}
