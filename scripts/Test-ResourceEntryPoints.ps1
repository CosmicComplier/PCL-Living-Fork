param([Parameter(Mandatory=$true)][string]$ExecutablePath)
# Exercises the real detail-page filter with isolated loader output; no app entry point or real profile.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='map-details-fixture'
$module=$asm.GetType('PCL.ModMain');$message=$module.GetMethod('MyMsgBox',$flags)
$result=$message.Invoke($null,[object[]]@('fixture local-world details','详情','确定',$null,$null,$false,$true,$false,$null,$null,$null))
if($result -ne 1){throw 'Single-button details did not queue normally'}
$queued=$module.GetField('WaitingMyMsgBox',$flags).GetValue($null).ToArray()|Select-Object -Last 1
if($queued.Button2 -cne '' -or $queued.Button3 -cne ''){throw 'Null button captions were not normalized'}
$project=[PCL.ResourceProject]::new([Newtonsoft.Json.Linq.JObject]::Parse('{"Tags":[],"Platform":1,"Types":8,"Slug":"fixture-shader","Id":"fixture","RawName":"Fixture shader","Description":"Fixture","Website":"https://example.invalid/fixture","LastUpdate":"2026-10-25T00:00:00Z","DownloadCount":1}'))
$build=$asm.GetType('PCL.MyResourceItem').GetMethod('BuildDetailArguments',$flags)
$enum=$asm.GetType('PCL.FormMain+PageType')
$download=[Enum]::Parse($enum,'Download');$detail=[Enum]::Parse($enum,'ResourceDetail');$instancePage=[Enum]::Parse($enum,'InstanceSetup')
$instanceType=$asm.GetType('PCL.ModMinecraft+McInstance');$instance=$instanceType.GetConstructor([type[]]@([string])).Invoke(@([string]'D:\Fixture\versions\example'))
$instanceType.GetProperty('Version').SetValue($instance,[PCLCS.McVersion]::new('1.20.1',$true),$null)
$args=$build.Invoke($null,[object[]]@($project,$instancePage,$null,$null,$instance))
if($args[2] -cne '1.20.1' -or $args[4].ToString() -cne 'Shader' -or $args[3].ToString() -cne 'None'){throw 'Version-page resource entry lost type/version or added an invalid loader filter'}
foreach($empty in @($null,[object[]]@(),[object[]]@('incomplete'))){$args=$build.Invoke($null,[object[]]@($project,$detail,$null,$empty,$null));if($args[4].ToString() -cne 'Any'){throw 'Dependency route changed'}}
$request=[PCL.ResourceSearcher+SearchRequest]::new([PCLCS.ResourceTypes]::Map,[PCL.ResourceSearcher+SearchResult]::new(),40);$request.GameVersion='1.20.2';$request.ModLoaders=[PCLCS.ModLoaders]::Fabric
$args=$build.Invoke($null,[object[]]@($project,$download,$request,$null,$null))
if($args[4].ToString() -cne 'Map' -or $args[2] -cne '1.20.2' -or $args[3].ToString() -cne 'None'){throw 'Download map route did not preserve search context'}
$args=$build.Invoke($null,[object[]]@($project,$download,$null,$null,$null))
if($args[4].ToString() -cne 'Shader'){throw 'Missing search input did not fall back to project type'}
Write-Output 'Resource entry regressions passed: null-caption local details, version-page context, missing dependency arguments, download search context and missing search-input fallback. No windows or profiles accessed.'
if($args.Length -ne 6){throw 'Resource detail arguments cannot carry the target instance'}
$request.Type=[PCLCS.ResourceTypes]::Mod;$request.GameVersion='1.20.1'
$bound=$build.Invoke($null,[object[]]@($project,$download,$request,$null,$instance))
if(-not [object]::ReferenceEquals($bound[5],$instance)){throw 'Version-bound mod search lost its exact instance'}
$dependency=$build.Invoke($null,[object[]]@($project,$detail,$null,$bound,$null))
if(-not [object]::ReferenceEquals($dependency[5],$instance)){throw 'A dependency-detail link lost its download target'}
$request.GameVersion='1.20.2';$changed=$build.Invoke($null,[object[]]@($project,$download,$request,$null,$instance));if($null -ne $changed[5]){throw 'A changed version filter retained an incompatible target instance'}
$downloadFixture=Join-Path ([IO.Path]::GetTempPath()) ('PclDownloadTarget-'+[Guid]::NewGuid().ToString('N'))
try{
 $targets=@();foreach($name in @('selected','other')){
  $path=Join-Path $downloadFixture ('versions/'+$name);[IO.Directory]::CreateDirectory((Join-Path $path 'PCL'))|Out-Null
  [IO.File]::WriteAllText((Join-Path $path 'PCL/Setup.ini'),"VersionArgumentIndieV2:True`r`n",[Text.UTF8Encoding]::new($false))
  $targets+=,$instanceType.GetConstructor([type[]]@([string])).Invoke(@([string]$path))
 }
 $chooser=$asm.GetType('PCL.PageDownloadResourceDetail').GetMethod('ChooseModDownloadFolder',$flags)
 $suitableType=[Func``2].MakeGenericType($instanceType,[bool]);$suitable=[Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{param($value) return $true},$suitableType)
 $cached=Join-Path $downloadFixture '.minecraft'
 $folder=$chooser.Invoke($null,[object[]]@($targets[0].PSObject.BaseObject,$targets[1].PSObject.BaseObject,[string]$cached,$suitable.PSObject.BaseObject))
 if(-not [IO.Path]::GetFullPath($folder).TrimEnd('\').EndsWith('\versions\selected\mods')){throw 'Cached root folder overrode the explicit version mods folder'}
 $folder=$chooser.Invoke($null,[object[]]@($null,$targets[1].PSObject.BaseObject,[string]$cached,$suitable.PSObject.BaseObject));if(-not $folder.TrimEnd('\').EndsWith('\versions\other\mods')){throw 'Current selected instance did not override a stale root-folder cache'}
 if($chooser.Invoke($null,[object[]]@($null,$null,[string]$cached,$suitable.PSObject.BaseObject)) -ne $cached){throw 'Unbound downloads lost their deliberate custom folder'}
 $folderState=$asm.GetType('PCL.ModMinecraft').GetField('_McFolderSelected',$flags);$previousFolder=$folderState.GetValue($null)
 try{
  $shared=(Join-Path $downloadFixture '.minecraft')+'\';$folderState.SetValue($null,[string]$shared)
  [IO.File]::WriteAllText((Join-Path $targets[0].PathVersion 'PCL/Setup.ini'),"VersionArgumentIndieV2:False`r`n",[Text.UTF8Encoding]::new($false))
  $asm.GetType('PCL.ModBase').GetMethod('IniClearCache',$flags).Invoke($null,@([string](Join-Path $targets[0].PathVersion 'PCL/Setup.ini')))|Out-Null
  $settings['VersionArgumentIndieV2'].ValueCache.Clear()
  $folder=$chooser.Invoke($null,[object[]]@($targets[0].PSObject.BaseObject,$null,[string]$cached,$suitable.PSObject.BaseObject))
  if($folder.TrimEnd('\') -ne (Join-Path $shared 'mods')){throw 'Non-isolated instance did not resolve to the shared Minecraft mods directory'}
 }finally{$folderState.SetValue($null,$previousFolder)}
}finally{
 $absolute=[IO.Path]::GetFullPath($downloadFixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $absolute.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($absolute) -notmatch '^PclDownloadTarget-[a-f0-9]{32}$'){throw 'Unsafe download-target fixture cleanup'}
 if([IO.Directory]::Exists($absolute)){[IO.Directory]::Delete($absolute,$true)}
}
Write-Output 'PASS: exact instance retained through search/dependency navigation; selected instance mods directory takes priority over stale folder cache.'
