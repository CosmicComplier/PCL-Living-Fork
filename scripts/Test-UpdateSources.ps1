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
 return ,(Json @{tag_name=('v'+$v);draft=$false;prerelease=$false;assets=@(@{name=$name;size=$size;browser_download_url=($base+$v+'/'+$name)},@{name='SHA256SUMS.txt';size=200;browser_download_url=($base+$v+'/SHA256SUMS.txt')})})
}
function Transport($request,$text=$null,$download=$null){
 $value=[Activator]::CreateInstance($asm.GetType('PCL.ModLauncherUpdate+UpdateTransport'),$true)
 $value.GetType().GetField('ReadJson',$flags).SetValue($value,([Func[string,Newtonsoft.Json.Linq.JObject]]$request))
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
 Write-Output 'PASS: saved/default source preference, temporary per-attempt choice pinned to the confirmed version, CNB/GitHub priority and failure fallback, bidirectional downloads, foreign URL rejection, checksum agreement and cancellation.'
}finally{
 $full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^PclUpdateSources-[a-f0-9]{32}$'){throw 'Unsafe updater fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
