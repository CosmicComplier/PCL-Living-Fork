param([Parameter(Mandatory=$true)][string]$ExecutablePath)
# Real updater methods, mocked transports and GUID files only. No live update or external network.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
foreach($file in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $file))|Out-Null}
$ExecutablePath=[IO.Path]::GetFullPath($ExecutablePath);$asm=[Reflection.Assembly]::LoadFrom($ExecutablePath);$type=$asm.GetType('PCL.ModLauncherUpdate',$true);$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='update-sources-fixture'
$cnb=$type.GetField('CnbManifestUrl',$flags).GetRawConstantValue();$github=$type.GetField('GitHubLatestUrl',$flags).GetRawConstantValue()
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclUpdateSources-'+[Guid]::NewGuid().ToString('N'))
$hash=(Get-FileHash $ExecutablePath -Algorithm SHA256).Hash.ToLowerInvariant();$size=(Get-Item $ExecutablePath).Length
# Fixtures represent stable update offers even when the client under test is a prerelease.
$version=([version][Diagnostics.FileVersionInfo]::GetVersionInfo($ExecutablePath).FileVersion).ToString(3)
function Call($name,[object[]]$arguments){$values=[object[]]::new($arguments.Count);for($i=0;$i -lt $values.Length;$i++){$values.SetValue($arguments[$i],$i)};return ,$type.GetMethod($name,$flags).Invoke($null,$values)}
function Json($value){return ,([Newtonsoft.Json.Linq.JObject]::Parse(($value|ConvertTo-Json -Depth 10 -Compress)))}
function Release($platform,$v){
 $name='PCL-Living-Fork-'+$v+'.exe';$base=if($platform -eq 'CNB'){'https://cnb.cool/ddzmumo/PCL-Living-Fork/-/releases/download/v'}else{'https://github.com/DDZmumo/PCL-Living-Fork/releases/download/v'}
 return ,(Json @{tag_name=('v'+$v);draft=$false;prerelease=$v.Contains('-');assets=@(@{name=$name;size=$size;browser_download_url=($base+$v+'/'+$name)},@{name='SHA256SUMS.txt';size=200;browser_download_url=($base+$v+'/SHA256SUMS.txt')})})
}
function Transport($request,$text=$null,$download=$null,$catalog=$null){
 $value=[Activator]::CreateInstance($asm.GetType('PCL.ModLauncherUpdate+UpdateTransport'),$true)
 $value.GetType().GetField('ReadJson',$flags).SetValue($value,([Func[string,Newtonsoft.Json.Linq.JObject]]$request))
 $listReader=if($catalog){$catalog}else{{param($url) throw 'Unexpected catalog request; live HTTP is disabled in this fixture'}}
 $value.GetType().GetField('ReadReleaseList',$flags).SetValue($value,([Func[string,Newtonsoft.Json.Linq.JArray]]$listReader))
 if($text){$value.GetType().GetField('ReadText',$flags).SetValue($value,([Func[string,string]]$text))}
 if($download){$value.GetType().GetField('Download',$flags).SetValue($value,([Action[string,string,MeloongCore.FileChecker]]$download))}
 return $value
}
function Reject([scriptblock]$action,$message){$failed=$false;try{& $action|Out-Null}catch{$failed=$true};if(-not $failed){throw $message}}
try{
 [IO.Directory]::CreateDirectory($fixture)|Out-Null
 $requests=[Collections.Generic.List[string]]::new()
 $preference=$settings['ToolLauncherUpdateSource']
 if($preference.DefaultValue -ne 0 -or $preference.Source.ToString() -ne 'Normal' -or (Call 'PreferredUpdateSource' @()).ToString() -ne 'Cnb'){throw 'Update source default is not persistent CNB'}
 $preference.ValueCache['']=1
 $transport=Transport {param($url) $requests.Add($url);if($url -eq $github){return ,(Release 'GitHub' '9.0.0')};throw 'CNB must not be preferred when GitHub is selected'}
 $latest=Call 'FindLatestRelease' @('1.0.0',$transport)
 if($requests.Count -ne 1 -or $requests[0] -ne $github -or (Call 'PreferredUpdateSource' @()).ToString() -ne 'GitHub'){throw 'Saved GitHub preference was ignored'}
 $requests.Clear();$transport=Transport {param($url) $requests.Add($url);if($url -eq $github){return ,(Release 'GitHub' '1.0.0')};throw 'A successful GitHub no-update result should be respected'}
 Call 'FindLatestRelease' @('1.0.0',$transport)|Out-Null
 if($requests.Count -ne 1){throw 'GitHub selection checked another source after a valid no-update result'}
 $requests.Clear();$transport=Transport {param($url) $requests.Add($url);if($url -eq $github){throw 'GitHub unavailable'};return ,(Release 'CNB' '9.0.0')}
 $latest=Call 'FindLatestRelease' @('1.0.0',$transport)
 if($requests.Count -ne 2 -or $requests[0] -ne $github -or -not $latest['assets'][0]['browser_download_url'].ToString().StartsWith('https://cnb.cool/')){throw 'Selected GitHub failure did not fall back to CNB'}
 $preference.ValueCache['']=999
 if((Call 'PreferredUpdateSource' @()).ToString() -ne 'Cnb'){throw 'Invalid stored update source did not fall back to CNB'}
 $preference.ValueCache['']=0;$requests.Clear()
 $transport=Transport {param($url) $requests.Add($url);if($url -eq $cnb){return ,(Release 'CNB' '9.0.0')};throw 'GitHub should not be requested'}
 $latest=Call 'FindLatestRelease' @('1.0.0',$transport)
 if($latest['tag_name'].ToString() -ne 'v9.0.0' -or $requests.Count -ne 1){throw 'CNB is not preferred'}
 foreach($reason in @('timeout','stale','prerelease','malformed','bad-url','missing-flags')){
  $requests.Clear()
  $transport=Transport {param($url) $requests.Add($url);if($url -eq $github){return ,(Release 'GitHub' '9.0.0')};if($reason -eq 'timeout'){throw 'network unavailable'};$r=Release 'CNB' '1.0.0';switch($reason){'prerelease'{$r['prerelease']=[Newtonsoft.Json.Linq.JValue]::new($true)}'malformed'{$r['tag_name']=[Newtonsoft.Json.Linq.JValue]::new('bad-tag')}'bad-url'{$r['assets'][0]['browser_download_url']=[Newtonsoft.Json.Linq.JValue]::new('https://cnb.cool/other/repo/-/releases/download/v1.0.0/a.exe')}'missing-flags'{$r.Remove('draft')|Out-Null}};return ,$r}
  $latest=Call 'FindLatestRelease' @('1.0.0',$transport)
  if($latest['tag_name'].ToString() -ne 'v9.0.0' -or $requests.Count -ne 2){throw ('GitHub fallback failed for '+$reason)}
 }
 $transport=Transport {param($url) if($url -eq $cnb){return ,(Release 'CNB' '1.0.0')};throw 'GitHub unavailable'}
 if((Call 'FindLatestRelease' @('1.0.0',$transport))['tag_name'].ToString() -ne 'v1.0.0'){throw 'Valid no-update CNB result was discarded'}
 $transport=Transport {param($url) throw 'all unavailable'};Reject {Call 'FindLatestRelease' @('1.0.0',$transport)} 'Missing sources did not report failure'
 foreach($url in @('http://cnb.cool/ddzmumo/PCL-Living-Fork/-/releases/download/v1.0.0/a.exe','https://cnb.cool/other/PCL-Living-Fork/-/releases/download/v1.0.0/a.exe','https://cnb.cool/ddzmumo/PCL-Living-Fork-other/-/releases/download/v1.0.0/a.exe','https://cnb.cool.evil.invalid/ddzmumo/PCL-Living-Fork/-/releases/download/v1.0.0/a.exe','https://user@cnb.cool/ddzmumo/PCL-Living-Fork/-/releases/download/v1.0.0/a.exe','https://cnb.cool:444/ddzmumo/PCL-Living-Fork/-/releases/download/v1.0.0/a.exe')){if(Call 'ValidAssetUrl' @($url)){throw 'Foreign/insecure CNB URL accepted'}}
 $cnbRelease=Release 'CNB' $version;$githubRelease=Release 'GitHub' $version;$staged=Join-Path $fixture 'update.exe';$downloads=[Collections.Generic.List[string]]::new()
 $text={param($url) return $hash+'  PCL-Living-Fork-'+$version+'.exe'}
 $download={param($url,$target,$check) $downloads.Add($url);if($url.StartsWith('https://cnb.cool/')){throw 'CNB download unavailable'};[IO.File]::Copy($ExecutablePath,$target,$true)}
 $transport=Transport {param($url) if($url -ne ('https://api.github.com/repos/DDZmumo/PCL-Living-Fork/releases/tags/v'+$version)){throw 'Fallback queried an unconfirmed version'};return ,$githubRelease} $text $download
 if((Call 'DownloadUpdate' @($cnbRelease,$staged,$transport)) -cne $hash -or $downloads.Count -ne 2 -or (Get-FileHash $staged -Algorithm SHA256).Hash.ToLowerInvariant() -cne $hash){throw 'Same-version download fallback failed'}
 $downloads.Clear();$transport=Transport {param($url) return ,(Release 'GitHub' '99.0.0')} $text $download
 Reject {Call 'DownloadUpdate' @($cnbRelease,$staged,$transport)} 'Fallback silently changed the confirmed version'
 if($downloads.Count -ne 1){throw 'Wrong-version fallback was downloaded'}
 $downloads.Clear();$badText={param($url) return $(if($url.StartsWith('https://github.com/')){'0'*64}else{$hash})+'  PCL-Living-Fork-'+$version+'.exe'}
 $transport=Transport {param($url) return ,$githubRelease} $badText $download
 Reject {Call 'DownloadUpdate' @($cnbRelease,$staged,$transport)} 'Conflicting mirror checksums were accepted'
 if($downloads.Count -ne 1){throw 'Hash-conflicting fallback was downloaded'}
 $downloads.Clear();$transport=Transport {param($url) throw 'Cancellation must not start fallback'} $text {param($url,$target,$check) $downloads.Add($url);throw [OperationCanceledException]::new()}
 Reject {Call 'DownloadUpdate' @($cnbRelease,$staged,$transport)} 'Cancellation was ignored'
 if($downloads.Count -ne 1){throw 'Canceled update retried a download'}
 $downloads.Clear();$reverseDownload={param($url,$target,$check) $downloads.Add($url);if($url.StartsWith('https://github.com/')){throw 'GitHub download unavailable'};if(-not $url.Contains('/v'+$version+'/')){throw 'CNB fallback switched release version'};[IO.File]::Copy($ExecutablePath,$target,$true)}
 $transport=Transport {param($url) throw 'GitHub-to-CNB retry must use the confirmed version, not a latest-release lookup'} $text $reverseDownload
 if((Call 'DownloadUpdate' @($githubRelease,$staged,$transport)) -cne $hash -or $downloads.Count -ne 2 -or -not $downloads[0].StartsWith('https://github.com/') -or -not $downloads[1].StartsWith('https://cnb.cool/')){throw 'GitHub-to-CNB same-version download retry failed'}
 $downloads.Clear();$reverseBadText={param($url) return $(if($url.StartsWith('https://cnb.cool/')){'0'*64}else{$hash})+'  PCL-Living-Fork-'+$version+'.exe'}
 $transport=Transport {param($url) throw 'No alternate version lookup expected'} $reverseBadText $reverseDownload
 Reject {Call 'DownloadUpdate' @($githubRelease,$staged,$transport)} 'GitHub-to-CNB checksum mismatch was ignored'
 if($downloads.Count -ne 1){throw 'Hash-conflicting CNB fallback was downloaded'}
 $sourceType=$asm.GetType('PCL.ModLauncherUpdate+UpdateSource');$cnbChoice=[Enum]::ToObject($sourceType,0);$githubChoice=[Enum]::ToObject($sourceType,1)
 $preference.ValueCache['']=0;$requests.Clear()
 $transport=Transport {param($url) $requests.Add($url);if(-not $url.EndsWith('/releases/tags/v'+$version)){throw 'Temporary source lookup changed the confirmed version'};return ,$githubRelease}
 $chosen=Call 'ReleaseForDownloadSource' @($cnbRelease,$githubChoice,$transport)
 if($requests.Count -ne 1 -or -not $chosen['assets'][0]['browser_download_url'].ToString().StartsWith('https://github.com/') -or (Call 'PreferredUpdateSource' @()).ToString() -ne 'Cnb'){throw 'Temporary GitHub selection was ignored or saved permanently'}
 $preference.ValueCache['']=1;$requests.Clear();$transport=Transport {param($url) throw 'Temporary CNB selection must not ask for latest metadata'}
 $chosen=Call 'ReleaseForDownloadSource' @($githubRelease,$cnbChoice,$transport)
 if(-not $chosen['assets'][0]['browser_download_url'].ToString().StartsWith('https://cnb.cool/') -or (Call 'PreferredUpdateSource' @()).ToString() -ne 'GitHub'){throw 'Temporary CNB selection was ignored or saved permanently'}
 $transport=Transport {param($url) return ,(Release 'GitHub' '9.9.9')}
 Reject {Call 'ReleaseForDownloadSource' @($cnbRelease,$githubChoice,$transport)} 'Temporary source selection silently changed the confirmed version'
 $channel=$settings['ToolLauncherUpdateChannel'];if($channel.DefaultValue -ne 0 -or $channel.Source.ToString() -ne 'Normal' -or (Call 'PreferredUpdateChannel' @()).ToString() -ne 'Stable'){throw 'Update channel is not stable-only by default'}
 $channelType=$asm.GetType('PCL.ModLauncherUpdate+UpdateChannel');$both=[Enum]::ToObject($channelType,1);$stableOnly=[Enum]::ToObject($channelType,0)
 foreach($case in @(@('v1.3.5-pre-2','1.3.5-pre-1',1),@('v1.3.5-pre-10','1.3.5-pre-2',1),@('v1.3.5','1.3.5-pre-10',1),@('v1.3.6-pre-1','1.3.5',1),@('v1.3.5-pre-1','1.3.5-pre-1',0),@('v1.3.4','1.3.5-pre-1',-1),@('v1.3.5.0','1.3.5',0))){
  $actual=[Math]::Sign([int](Call 'CompareReleaseVersions' @($case[0],$case[1])));if($actual -ne $case[2]){throw ('Release ordering failed: '+$case[0]+' / '+$case[1])}
 }
 if(Call 'NewerReleaseForChannel' @('v1.3.5-pre-2','1.3.4',$stableOnly)){throw 'Stable channel offered a prerelease'}
 if(-not (Call 'NewerReleaseForChannel' @('v1.3.5-pre-2','1.3.4',$both))){throw 'Opt-in channel rejected a newer prerelease'}
 $preview=$type.GetField('CnbPreviewManifestUrl',$flags).GetRawConstantValue();$catalogUrl=$type.GetField('GitHubReleaseListUrl',$flags).GetRawConstantValue();$channel.ValueCache['']=1;$preference.ValueCache['']=0;$requests.Clear()
 $transport=Transport {param($url) $requests.Add($url);if($url -ne $preview){throw 'Unexpected metadata endpoint'};return ,(Release 'CNB' '1.3.5-pre-10')}
 $selected=Call 'FindLatestRelease' @('1.3.4',$transport)
 if($selected['tag_name'].ToString() -ne 'v1.3.5-pre-10' -or $requests.Count -ne 1 -or $requests[0] -eq $cnb){throw 'Opt-in CNB checks did not use the isolated preview feed'}
 $items=[Newtonsoft.Json.Linq.JArray]::new();foreach($v in @('1.3.5-pre-1','1.3.5-pre-10','1.3.5-pre-2')){$items.Add((Release 'GitHub' $v))}
 $draft=Release 'GitHub' '99.0.0';$draft['draft']=[Newtonsoft.Json.Linq.JValue]::new($true);$items.Add($draft)
 $requests.Clear();$preference.ValueCache['']=1
 $transport=Transport {param($url) throw 'A complete GitHub catalog must not read other endpoints'} $null $null {param($url) $requests.Add($url);return ,$items}
 $selected=Call 'FindLatestRelease' @('1.3.5-pre-1',$transport)
 if($selected['tag_name'].ToString() -ne 'v1.3.5-pre-10' -or $requests.Count -ne 1 -or $requests[0] -ne $catalogUrl){throw 'GitHub opt-in catalog ordering or draft filtering failed'}
 $items.Add((Release 'GitHub' '1.3.5'));$selected=Call 'FindLatestRelease' @('1.3.5-pre-10',$transport)
 if($selected['tag_name'].ToString() -ne 'v1.3.5'){throw 'A stable release did not supersede its prereleases'}
 $channel.ValueCache['']=0;$preference.ValueCache['']=0;$requests.Clear()
 $transport=Transport {param($url) $requests.Add($url);if($url -ne $cnb){throw 'Stable channel reached the preview feed'};return ,(Release 'CNB' '1.3.4')}
 $selected=Call 'FindLatestRelease' @('1.3.3',$transport)
 if($selected['tag_name'].ToString() -ne 'v1.3.4' -or $requests.Count -ne 1){throw 'Returning to stable did not restore the stable endpoint'}
 $pre=Release 'GitHub' '1.3.5-pre-2';$transport=Transport {param($url) throw 'Source conversion must not look for another version'}
 $pinned=Call 'ReleaseForDownloadSource' @($pre,$cnbChoice,$transport)
 if($pinned['tag_name'].ToString() -ne 'v1.3.5-pre-2' -or -not $pinned['assets'][0]['browser_download_url'].ToString().Contains('/v1.3.5-pre-2/')){throw 'A confirmed prerelease was lost when selecting a one-time source'}
 Write-Output 'PASS: persistent/default update sources and channels; stable-only filtering; natural prerelease ordering; isolated CNB preview feed; GitHub catalog/draft filtering; stable promotion; per-attempt same-version fallback, checksums and cancellation. No live HTTP.'
}finally{
 $full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^PclUpdateSources-[a-f0-9]{32}$'){throw 'Unsafe updater fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
