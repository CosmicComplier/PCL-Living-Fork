param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$PreviewDirectory)
# Isolated JARs, mocked platform responses/downloads and offscreen PCL controls only.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.IO.Compression,System.IO.Compression.FileSystem,System.Net.Http
$root=Split-Path $PSScriptRoot -Parent
foreach($p in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $p))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true);$app.InitializeComponent()
[Windows.Application].GetField('_startupUri',[Reflection.BindingFlags]'NonPublic,Instance').SetValue($app,$null)
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
[Windows.Application]::ResourceAssembly=$asm
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='launch-dependencies-fixture'
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclLaunchDependencies-'+[Guid]::NewGuid().ToString('N'))
$inspector=$asm.GetType('PCL.ModDependencyInspector');$service=$asm.GetType('PCL.ModDependencyAutoFill');$session=$asm.GetType('PCL.LaunchDependencyCheck');$none=[Threading.CancellationToken]::None
$asm.GetType('PCL.ModDependencyIndex').GetProperty('CacheDirectory',$flags).SetValue($null,(Join-Path $fixture 'dependency-index'),$null)
function Call($type,$name,[object[]]$arguments){$values=New-Object object[] $arguments.Count;for($i=0;$i -lt $values.Length;$i++){if($null -ne $arguments[$i]){$values[$i]=$arguments[$i].PSObject.BaseObject}};return ,$type.GetMethod($name,$flags).Invoke($null,$values)}
function Json($value){return ,([Newtonsoft.Json.Linq.JToken]::Parse((ConvertTo-Json -InputObject $value -Depth 25 -Compress)))}
function Jar($path,$id,$depends){[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))|Out-Null;$zip=[IO.Compression.ZipFile]::Open($path,[IO.Compression.ZipArchiveMode]::Create);try{$w=[IO.StreamWriter]::new($zip.CreateEntry('fabric.mod.json').Open());try{$w.Write((@{schemaVersion=1;id=$id;name=$id;version='1.0.0';depends=$depends}|ConvertTo-Json -Depth 10))}finally{$w.Dispose()}}finally{$zip.Dispose()}}
function Scan{return ,(Call $inspector 'Inspect' @($mods,$context,$none))}
function Reject([scriptblock]$work,$reason){$failed=$false;try{& $work|Out-Null}catch{$failed=$true};if(-not $failed){throw $reason}}
function Options{
 $options=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyOnline+LookupOptions'));$options.Request=$request;$options.Fingerprint=[Func[string,uint32]]{param($p) return [uint32]123};$options.Cache=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyOnline+LookupCache'));return $options
}
function Plan{return ,(Call $service 'BuildPlan' @($mods,(Scan),$none,(Options)))}
function VersionData($path,$project,$version,$dependencies){return @{id=$version;project_id=$project;name='1.0.0';version_number='1.0.0';date_published='2026-11-01T00:00:00Z';version_type='release';downloads=10;game_versions=@('1.21.1');loaders=@('fabric');dependencies=$dependencies;files=@(@{primary=$true;filename=($project+'.jar');url=('https://cdn.modrinth.com/data/'+$project+'.jar');size=(Get-Item $path).Length;hashes=@{sha1=(Get-FileHash $path -Algorithm SHA1).Hash.ToLowerInvariant()}})}}
function CfData($path,$project,$id,$dependencies){return @{gameId=432;modId=$project;id=$id;displayName='1.0.0';fileDate='2026-11-01T00:00:00Z';releaseType=1;downloadCount=10;fileName=($project.ToString()+'.jar');fileLength=(Get-Item $path).Length;fileFingerprint=123;isAvailable=$true;downloadUrl=('https://edge.forgecdn.net/files/2000/'+$id+'/'+$project+'.jar');gameVersions=@('1.21.1','Fabric');hashes=@(@{algo=1;value=(Get-FileHash $path -Algorithm SHA1).Hash.ToLowerInvariant()});dependencies=$dependencies}}
function Render($element,$name,$width=740,$height=560){if(-not $PreviewDirectory){return};[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null;$hostPanel=[Windows.Controls.Grid]::new();$hostPanel.Background=[Windows.Media.BrushConverter]::new().ConvertFromString('#e7effb');$hostPanel.Children.Add($element)|Out-Null;$hostPanel.Measure([Windows.Size]::new($width,$height));$hostPanel.Arrange([Windows.Rect]::new(0,0,$width,$height));$hostPanel.UpdateLayout();$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($width,$height,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($hostPanel);$enc=[Windows.Media.Imaging.PngBitmapEncoder]::new();$enc.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$out=[IO.File]::Create((Join-Path $PreviewDirectory $name));try{$enc.Save($out)}finally{$out.Dispose()};$hostPanel.Children.Clear()}
try{
 [IO.Directory]::CreateDirectory($fixture)|Out-Null;$mods=Join-Path $fixture 'mods';$source=Join-Path $mods 'consumer.jar';$library=Join-Path $fixture 'packages/library.jar';$base=Join-Path $fixture 'packages/base.jar'
 Jar $source 'consumer' @{lib_core='>=1.0.0'};Jar $library 'lib_core' @{base_core='>=1.0.0'};Jar $base 'base_core' @{}
 $context=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionContext'));$context.MinecraftVersion='1.21.1';$context.LoaderKnown=$true;$context.LoaderVersions.Add('fabricloader','0.16.10')
 $parentData=VersionData $source 'parent01' 'parentv1' @(@{project_id='libproj1';version_id=$null;dependency_type='required'},@{project_id='optional';dependency_type='optional'})
 $libraryData=VersionData $library 'libproj1' 'libver01' @(@{project_id='basepr01';version_id=$null;dependency_type='required'})
 $baseData=VersionData $base 'basepr01' 'basevr01' @()
 $script:VersionMap=@{};foreach($data in @($parentData,$libraryData,$baseData)){$script:VersionMap[$data.files[0].hashes.sha1]=$data}
 $script:BadLoader=$false;$script:PinFixture=$false;$script:Requests=[Collections.Generic.List[string]]::new()
 $request=[Func[string,Net.Http.HttpMethod,string,Threading.CancellationToken,Newtonsoft.Json.Linq.JToken]]{
  param($url,$method,$body,$token);$token.ThrowIfCancellationRequested();$script:Requests.Add($url)
  if($url.Contains('curseforge.com')){return ,(Json @{data=@{exactMatches=@()}})}
  if($url.Contains('/version_files')){$response=@{};foreach($hash in ($body|ConvertFrom-Json).hashes){if($script:VersionMap.ContainsKey($hash)){$response[$hash]=$script:VersionMap[$hash]}};return ,(Json $response)}
  if($url.Contains('/projects?')){return ,(Json @(@{id='parent01';project_type='mod';title='Consumer'},@{id='pinned01';project_type='mod';title='Pinned consumer'},@{id='libproj1';project_type='mod';title='Core library'},@{id='basepr01';project_type='mod';title='Base library'}))}
  if($url.EndsWith('/version/libver01')){return ,(Json $libraryData)}
  if($url -match '/project/(libproj1|basepr01)(/version\?|$)'){$id=$matches[1];if($url.Contains('/version?')){$data=if($id -eq 'libproj1'){$libraryData.Clone()}else{$baseData.Clone()};if($script:BadLoader){$data.loaders=@('forge')};if($script:FallbackData -and $id -eq 'libproj1'){return ,(Json @($script:FallbackData,$data))};if($script:PinFixture -and $id -eq 'libproj1'){$newer=$data.Clone();$newer.id='libver02';$newer.date_published='2026-11-02T00:00:00Z';return ,(Json @($newer,$data))};return ,(Json @($data))};return ,(Json @{id=$id;project_type='mod';title=$(if($id -eq 'libproj1'){'Core library'}else{'Base library'})})}
  throw ('Unexpected mock API URL: '+$url)
 }
 $report=Scan
 if(-not $report.HasErrors){throw 'Missing dependency fixture does not trigger startup checks'}
 Call $session 'Accept' @($mods,$report)|Out-Null
 if(-not (Call $session 'WasAccepted' @($mods,(Scan)))){throw 'Unchanged accepted problems prompt again'}
 $context.JavaVersion='21.0.2';if(Call $session 'WasAccepted' @($mods,(Scan))){throw 'Java context change did not invalidate confirmation'};$context.JavaVersion=$null
 Call $session 'SettingsChanged' @()|Out-Null
 if(Call $session 'WasAccepted' @($mods,(Scan))){throw 'Changing launch switches retained prior acknowledgements'}
 if((Call $inspector 'MatchVersion' @('1.21.1','[1.21.1,1.21.1]','maven')).ToString() -ne 'Satisfied'){throw 'Closed Minecraft range rejects its own version'}
 foreach($case in @(@('0.8.7','Satisfied'),@('0.8.11','Satisfied'),@('0.8.12','Satisfied'),@('0.8.14','Satisfied'),@('0.9.0','Unsatisfied'))){if((Call $inspector 'MatchVersion' @($case[0],'0.8.x','fabric')).ToString() -ne $case[1]){throw ('Sodium wildcard version range failed: '+$case[0])}}
 foreach($case in @(@('1.21.1','Satisfied'),@('1.21-beta.1','Satisfied'),@('1.21.2-alpha.1','Unsatisfied'),@('1.21.2','Unsatisfied'),@('1.20.6','Unsatisfied'))){if((Call $inspector 'MatchVersion' @($case[0],'>=1.21- <1.21.2-','fabric')).ToString() -ne $case[1]){throw ('Fabric empty-prerelease boundary failed: '+$case[0])}}
 $plan=Plan
 if($plan.Items.Count -ne 2){throw ('Required and transitive dependencies not resolved: '+($plan.Notes -join '; ')+'; online='+($plan.Report.OnlineNotes -join '; '))}
 if(@($script:Requests|Where-Object {$_ -like '*project/optional*'}).Count){throw 'Optional dependency was auto-selected'}
 $pinned=Join-Path $mods 'pinned-consumer.jar';Jar $pinned 'pinned_consumer' @{lib_core='>=1.0.0'}
 $pinnedData=VersionData $pinned 'pinned01' 'pinnedv1' @(@{project_id='libproj1';version_id='libver01';dependency_type='required'})
 $pinnedHash=$pinnedData.files[0].hashes.sha1;$script:VersionMap[$pinnedHash]=$pinnedData;$script:PinFixture=$true
 $pinnedPlan=Plan
 if($pinnedPlan.Items.Count -ne 2 -or ($pinnedPlan.Items|Where-Object ProjectId -eq 'libproj1'|Select-Object -First 1).Version.Id -ne 'libver02'){throw ('Unpinned plus older pinned declaration incorrectly blocks a compatible candidate: '+($pinnedPlan.Notes -join '; '))}
 $parentData.dependencies[0].version_id='libver01'
 $pinnedPlan=Plan
 if($pinnedPlan.Items.Count -ne 2 -or ($pinnedPlan.Items|Where-Object ProjectId -eq 'libproj1'|Select-Object -First 1).Version.Id -ne 'libver02'){throw 'Runtime version ranges are incorrectly replaced by platform file records'}
 $parentData.dependencies[0].version_id=$null
 $script:PinFixture=$false;$script:VersionMap.Remove($pinnedHash);[IO.File]::Delete($pinned)
 $cfParent=CfData $source 1001 2000001 @(@{modId=1002;relationType=3});$cfLibrary=CfData $library 1002 2000002 @(@{modId=1003;relationType=3});$cfBase=CfData $base 1003 2000003 @()
 $mrRequest=$request
 $request=[Func[string,Net.Http.HttpMethod,string,Threading.CancellationToken,Newtonsoft.Json.Linq.JToken]]{
  param($url,$method,$body,$token)
  if($url.Contains('modrinth.com')){return ,(Json @{})}
  if($url.Contains('/fingerprints/')){return ,(Json @{data=@{exactMatches=@(@{id=1001;file=$cfParent})}})}
  $projects=@(@{id=1001;gameId=432;classId=6;name='Consumer'},@{id=1002;gameId=432;classId=6;name='Core library'},@{id=1003;gameId=432;classId=6;name='Base library'})
  if($url -eq 'https://api.curseforge.com/v1/mods'){return ,(Json @{data=$projects})}
  if($url -match '/mods/(1002|1003)(/files\?|$)'){$id=[int]$matches[1];if($url.Contains('/files?')){return ,(Json @{data=@($(if($id -eq 1002){$cfLibrary}else{$cfBase}))})};return ,(Json @{data=($projects|Where-Object id -eq $id)})}
  throw ('Unexpected CurseForge API '+$url)
 }
 $cfPlan=Plan
 if($cfPlan.Items.Count -ne 2 -or @($cfPlan.Items|Where-Object Platform -ne 'CurseForge').Count){throw ('CurseForge transitive repair plan failed: '+($cfPlan.Notes -join '; '))}
 $request=$mrRequest
 $downloadType=[Action``2].MakeGenericType([Collections.Generic.List``1].MakeGenericType($asm.GetType('PCL.ModNet+NetFile')),[Threading.CancellationToken])
 $download=[Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{param($files,$token);for($i=0;$i -lt $files.Count;$i++){$token.ThrowIfCancellationRequested();[IO.File]::Copy($(if($i -eq 0){$library}else{$base}),$files[$i].LocalPath)}},$downloadType)
 $before=(Get-FileHash $source).Hash
 $badDownload=[Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{param($files,$token);foreach($file in $files){[IO.File]::WriteAllText($file.LocalPath,'bad')}},$downloadType)
 Reject {Call $service 'Install' @($plan,$none,$badDownload)} 'Corrupt dependency download was installed'
 $cancel=[Threading.CancellationTokenSource]::new();$cancel.Cancel();try{Reject {Call $service 'Install' @($plan,$cancel.Token,$download)} 'Canceled repair was installed'}finally{$cancel.Dispose()}
 $collision=Join-Path $mods 'basepr01.jar';[IO.Directory]::CreateDirectory($collision)|Out-Null
 Reject {Call $service 'Install' @($plan,$none,$download)} 'A same-name directory was overwritten'
 if(Test-Path -LiteralPath (Join-Path $mods 'libproj1.jar')){throw 'Failed dependency batch did not roll back its new file'};[IO.Directory]::Delete($collision)
 $count=Call $service 'Install' @($plan,$none,$download)
 if($count -ne 2 -or (Scan).HasErrors -or (Get-FileHash $source).Hash -ne $before){throw 'Repair did not satisfy dependencies while preserving original mods'}
 if((Plan).Items.Count -ne 0){throw 'Complete dependency set still offers a repair'}
 [IO.File]::Delete((Join-Path $mods 'libproj1.jar'));[IO.File]::Delete((Join-Path $mods 'basepr01.jar'))
 $script:BadLoader=$true;$unavailable=Plan;if($unavailable.Items.Count -ne 0){throw 'Wrong-loader dependency selected'};$script:BadLoader=$false
 $sourceBytes=[IO.File]::ReadAllBytes($source);$savedMap=$script:VersionMap.Clone()
 [IO.File]::Delete($source);Jar $source 'consumer' @{lib_core='>=2.0.0'}
 $incompatibleParent=VersionData $source 'parent01' 'parentv1' @(@{project_id='libproj1';dependency_type='required'})
 $script:VersionMap[$incompatibleParent.files[0].hashes.sha1]=$incompatibleParent
 $invalidPlan=Plan
 Reject {Call $service 'Install' @($invalidPlan,$none,$download)} 'Downloaded dependency outside the runtime version range was installed'
 if(Test-Path -LiteralPath (Join-Path $mods 'libproj1.jar')){throw 'Incompatible range repair modified the mods directory'}
 [IO.File]::WriteAllBytes($source,$sourceBytes);$script:VersionMap=$savedMap
 $savedMap=$script:VersionMap.Clone();[IO.File]::Delete($source);Jar $source 'consumer' @{lib_core='>=1.0.0 <2.0.0'}
 $rangeParent=VersionData $source 'parent01' 'parentv1' @(@{project_id='libproj1';dependency_type='required'});$script:VersionMap[$rangeParent.files[0].hashes.sha1]=$rangeParent
 $newerJar=Join-Path $fixture 'packages/newer.jar'
 $z=[IO.Compression.ZipFile]::Open($newerJar,[IO.Compression.ZipArchiveMode]::Create);try{$w=[IO.StreamWriter]::new($z.CreateEntry('fabric.mod.json').Open());try{$w.Write('{"schemaVersion":1,"id":"lib_core","name":"Core library","version":"2.0.0","depends":{"base_core":">=1.0.0"}}')}finally{$w.Dispose()}}finally{$z.Dispose()}
 $script:FallbackData=VersionData $newerJar 'libproj1' 'libver02' @(@{project_id='basepr01';dependency_type='required'});$script:FallbackData.date_published='2026-11-02T00:00:00Z';$script:FallbackData.files[0].filename='lib-newer.jar'
 $fallbackPlan=Plan;$script:FallbackAttempts=0
 $fallbackDownload=[Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{param($files,$token);$script:FallbackAttempts++;foreach($file in $files){$copy=if($file.LocalPath.EndsWith('-lib-newer.jar')){$newerJar}elseif($file.LocalPath.EndsWith('-basepr01.jar')){$base}else{$library};[IO.File]::Copy($copy,$file.LocalPath)}},$downloadType)
 Call $service 'Install' @($fallbackPlan,$none,$fallbackDownload)|Out-Null
 if($script:FallbackAttempts -ne 2 -or (Scan).HasErrors -or [IO.File]::Exists((Join-Path $mods 'lib-newer.jar'))){throw 'Actual JAR ranges did not select an older common-compatible candidate'}
 [IO.File]::Delete((Join-Path $mods 'libproj1.jar'));[IO.File]::Delete((Join-Path $mods 'basepr01.jar'));[IO.File]::WriteAllBytes($source,$sourceBytes);$script:VersionMap=$savedMap;$script:FallbackData=$null
 $plan=Plan;Jar (Join-Path $mods 'new.jar') 'new_mod' @{};Reject {Call $service 'Install' @($plan,$none,$download)} 'Stale repair plan accepted changed mods';[IO.File]::Delete((Join-Path $mods 'new.jar'))
 [IO.File]::Copy($library,(Join-Path $mods 'libproj1.jar.disabled'));if(-not @((Plan).Notes|Where-Object {$_ -like '*已安装但被禁用*'}).Count){throw 'Disabled existing dependency is not distinguished from missing files'};[IO.File]::Delete((Join-Path $mods 'libproj1.jar.disabled'))
 Write-Output 'PASS: exact-platform required/transitive repair, no-op complete set, optional/loader/disabled guards, checksum/cancel/collision/stale checks and rollback.'

 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $checkView=$asm.GetType('PCL.ModDependencyCheckView');$blockedView=($checkView.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 2}|Select-Object -First 1).Invoke(@($unavailable.Report,$unavailable))
 if(-not @($checkView.GetField('Groups',$flags).GetValue($blockedView).Children|Where-Object {$_.GetType().Name -eq 'MyHint' -and $_.Text -like '暂不能自动补全*'}).Count){throw 'Unavailable repair reason is hidden in reading diagnostics'}
 $page=[Activator]::CreateInstance($asm.GetType('PCL.PageLaunchRight'));$page.FindName('PanLog').Visibility='Collapsed'
 $settings['LaunchModDependencyCheck'].ValueCache['']=$true;$settings['LaunchModDependencyAutoFill'].ValueCache['']=$true
 $page.GetType().GetMethod('RefreshDependencyOptions',$flags).Invoke($page,@())|Out-Null
 $page.GetType().GetMethod('ApplyDependencyOptionsState',$flags).Invoke($page,@($false,$false,$true))|Out-Null
 $page.GetType().GetMethod('ShowRecentWorld',$flags).Invoke($page,@($null))|Out-Null
 if($page.FindName('CardDependencyChecks').Visibility -ne 'Visible'){throw 'Missing recent-world history hides the launch switches'}
 if($page.FindName('CheckAutoFillDependencies').Visibility -ne 'Visible' -or -not $page.FindName('CheckModDependencies').Checked){throw 'Native launch switches not refreshed'}
 $page.FindName('CardRecentWorlds').Visibility='Visible';$page.FindName('CardRecentWorlds').Opacity=1
 $page.FindName('TransRecentWorld').X=0;$page.FindName('ItemRecentWorld').Title='新的世界';$page.FindName('ItemRecentWorld').Info='1.21.1 · 测试版本'
 Render $page 'launch-switches.png' 560 380
 $settings['LaunchModDependencyCheck'].ValueCache['']=$false;$page.GetType().GetMethod('RefreshDependencyOptions',$flags).Invoke($page,@())|Out-Null
 if($page.FindName('CheckAutoFillDependencies').Visibility -ne 'Collapsed'){throw 'Auto repair switch remains visible with precheck disabled'}
 Call ($asm.GetType('PCL.ModLaunch')) 'McLaunchDependencyCheck' @($null)|Out-Null
 $owner=[PCL.ModMinecraft+McInstance]::new((Join-Path $fixture 'versions/fixture'));$emptyPlan=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyAutoFill+Plan'));$emptyPlan.Root=Join-Path $owner.PathIndie 'mods'
 try{Call ($asm.GetType('PCL.ModLaunch')) 'InstallLaunchDependencies' @($owner,$emptyPlan,$none)|Out-Null}catch{throw $_.Exception.ToString()}
 if([PCL.ModMinecraft+McInstanceLock]::IsLocked($owner)){throw 'Completed dependency operation retained its instance lock'}
 $settings['LaunchModDependencyCheck'].ValueCache['']=$true
 for($i=0;$i -lt 7;$i++){Jar (Join-Path $mods ('example'+$i+'.jar')) ('example'+$i) @{minecraft='<1.21'}}
 $report=Scan;for($i=0;$i -lt 79;$i++){$report.Notices.Add('Synthetic metadata reading note '+$i)}
 $viewType=$asm.GetType('PCL.ModDependencyCheckView');$view=($viewType.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 2}|Select-Object -First 1).Invoke(@($report,$plan))
 if(-not $viewType.GetField('Details',$flags).GetValue($view).IsSwapped){throw 'Reading diagnostics are expanded by default'}
 Render $view 'dependency-check.png' 740 500
 if($PreviewDirectory){
  $converter=[Activator]::CreateInstance($asm.GetType('PCL.ModMain+MyMsgBoxConverter'));$converter.Title='启动前模组检查';$converter.Text='请检查以下模组问题。';$converter.Button1='补全并启动';$converter.Button2='取消启动';$converter.Button3='仍然启动';$converter.IsWarn=$true;$converter.WarningButton=3
  $converter.ContentFactory=[Func[Windows.FrameworkElement]]{return $view}
  $modal=[Activator]::CreateInstance($asm.GetType('PCL.MyMsgText'),@($converter));$modal.FindName('TransformRotate').Angle=0;$modal.FindName('TransformPos').Y=0
  if($modal.FindName('Btn1').ColorType.ToString() -eq 'Red' -or $modal.FindName('Btn3').ColorType.ToString() -ne 'Red'){throw 'Danger styling is not attached to the explicit continue-launch action'}
  Render $modal 'dependency-dialog.png' 820 620
 }
 $foreignRoot=Join-Path $fixture 'foreign-mods';[IO.Directory]::CreateDirectory($foreignRoot)|Out-Null
 foreach($loader in @('forge','neoforge')){
  $zip=[IO.Compression.ZipFile]::Open((Join-Path $foreignRoot ($loader+'-only.jar')),[IO.Compression.ZipArchiveMode]::Create)
  try{$metadata=if($loader -eq 'forge'){'META-INF/mods.toml'}else{'META-INF/neoforge.mods.toml'};$writer=[IO.StreamWriter]::new($zip.CreateEntry($metadata).Open());try{$writer.Write("[[mods]]`nmodId=`"$($loader)_fixture`"`nversion=`"1.0`"`ndisplayName=`"$loader fixture`"")}finally{$writer.Dispose()}}finally{$zip.Dispose()}
 }
 $foreignReport=Call $inspector 'Inspect' @($foreignRoot,$context,$none)
 if(@($foreignReport.Issues|Where-Object {$_.Kind.ToString() -eq 'LoaderMismatch'}).Count -ne 2){throw 'Startup scan did not detect Forge/NeoForge files in a Fabric instance'}
 $requestCount=$script:Requests.Count;$foreignPlan=Call $service 'BuildPlan' @($foreignRoot,$foreignReport,$none,(Options))
 if($foreignPlan.Items.Count -or $foreignPlan.Enables.Count -or $script:Requests.Count -ne $requestCount){throw 'A wrong-loader mod triggered dependency downloads or automatic activation'}
 $foreignView=($viewType.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 2}|Select-Object -First 1).Invoke(@($foreignReport,$foreignPlan))
 if(-not @($viewType.GetField('Groups',$flags).GetValue($foreignView).Children|Where-Object {$_.GetType().Name -eq 'MyCard' -and $_.Title -like '加载器不匹配*'}).Count){throw 'Loader mismatch has no separate native UI group'}
 Render $foreignView 'loader-mismatch.png' 740 500
 if($PreviewDirectory){
  $converter=[Activator]::CreateInstance($asm.GetType('PCL.ModMain+MyMsgBoxConverter'));$converter.Title='启动前模组检查';$converter.Text='请检查以下模组问题。';$converter.Button1='返回 Mod 管理';$converter.Button2='取消启动';$converter.Button3='仍然启动';$converter.IsWarn=$true;$converter.WarningButton=3
  $converter.ContentFactory=[Func[Windows.FrameworkElement]]{return $foreignView}
  $modal=[Activator]::CreateInstance($asm.GetType('PCL.MyMsgText'),@($converter));$modal.FindName('TransformRotate').Angle=0;$modal.FindName('TransformPos').Y=0
  Render $modal 'loader-mismatch-dialog.png' 820 620
 }
 $runtimeRoot=Join-Path $fixture 'runtime-components';[IO.Directory]::CreateDirectory($runtimeRoot)|Out-Null
 foreach($id in @('runtime_one','runtime_two')){
  $zip=[IO.Compression.ZipFile]::Open((Join-Path $runtimeRoot ($id+'.jar')),[IO.Compression.ZipArchiveMode]::Create)
  try{$writer=[IO.StreamWriter]::new($zip.CreateEntry('META-INF/mods.toml').Open());try{$writer.Write("modLoader=`"javafml`"`nloaderVersion=`"[48,)`"`n[[mods]]`nmodId=`"$id`"`nversion=`"1`"`n[[dependencies.$id]]`nmodId=`"forge`"`nmandatory=true`nversionRange=`"[47,)`"`n[[dependencies.$id]]`nmodId=`"lowcodefml`"`nmandatory=true`nversionRange=`"[48,)`"")}finally{$writer.Dispose()}}finally{$zip.Dispose()}
 }
 $runtimeContext=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionContext'));$runtimeContext.MinecraftVersion='1.20.1';$runtimeContext.LoaderKnown=$true;$runtimeContext.LoaderVersions.Add('forge','47.2.0')
 $runtimeReport=Call $inspector 'Inspect' @($runtimeRoot,$runtimeContext,$none);$requestCount=$script:Requests.Count
 $runtimePlan=Call $service 'BuildPlan' @($runtimeRoot,$runtimeReport,$none,(Options))
 if($runtimePlan.Items.Count -or $script:Requests.Count -ne $requestCount){throw 'Loader internal components were selected as independent mod downloads'}
 $runtimeView=($viewType.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 2}|Select-Object -First 1).Invoke(@($runtimeReport,$runtimePlan))
 $runtimeCards=@($viewType.GetField('Groups',$flags).GetValue($runtimeView).Children|Where-Object {$_.GetType().Name -eq 'MyCard' -and $_.Title -like '运行环境要求不满足*'})
 if($runtimeCards.Count -ne 1 -or $runtimeCards[0].Title -notlike '*（1）*'){throw 'Internal FML requirements were not merged under the owning loader'}
 if($runtimeReport.ToSummary().Contains('javafml') -or $runtimeReport.ToSummary().Contains('lowcodefml')){throw 'Internal component IDs leaked into the player-facing summary'}
 $runtimeGraphType=$asm.GetType('PCL.ModDependencyGraphView');$runtimeGraph=($runtimeGraphType.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 2}|Select-Object -First 1).Invoke(@($runtimeReport,[string](Join-Path $runtimeRoot 'runtime_one.jar')))
 $graphCanvas=$runtimeGraph.GetType().GetField('GraphCanvas',$flags).GetValue($runtimeGraph);$runtimeNodes=@($graphCanvas.Children|Where-Object {$_ -is [Windows.Controls.Border]}|ForEach-Object {$_.Tag})
 if(@($runtimeNodes|Where-Object {$_.Title -eq 'Forge'}).Count -ne 1 -or @($runtimeNodes|Where-Object {$_.Title -in @('javafml','lowcodefml')}).Count){throw 'Dependency graph shows loader components as separate mods'}
 Render $runtimeView 'loader-components-grouped.png' 740 420
 Write-Output 'PASS: loader-owned components grouped as one runtime requirement and one graph node; no independent component downloads.'

 $modPage=[Activator]::CreateInstance($asm.GetType('PCL.PageInstanceMod'));$indexButton=$modPage.FindName('BtnManageIndex')
 if($null -eq $indexButton -or $indexButton.Text -ne '更新依赖索引'){throw 'Manual dependency-tree indexing is absent from Mod management'}
 $manage=$modPage.FindName('PanManage');([Windows.Controls.Panel][Windows.LogicalTreeHelper]::GetParent($manage)).Children.Remove($manage)
 Render $manage 'mod-dependency-index-actions.png' 740 130
 $nativeRows=[Windows.Controls.StackPanel]::new();$nativeRows.Margin=[Windows.Thickness]::new(15)
 $rowType=$asm.GetType('PCL.MyLocalModItem');$entryType=$asm.GetType('PCL.LocalResourceFile');$refreshWarning=$rowType.GetMethod('RefreshLoaderWarning',$flags)
 foreach($file in [IO.Directory]::GetFiles($foreignRoot,'*.jar')){
  $row=[Activator]::CreateInstance($rowType);$row.Entry=$entryType.GetConstructor(@([string])).Invoke(@([string]$file));$row.Title=[IO.Path]::GetFileNameWithoutExtension($file);$row.Description=[IO.Path]::GetFileName($file)
  $refreshWarning.Invoke($row,@([string]$foreignRoot,$context))|Out-Null
  if($row.FindName('LoaderWarningBorder').Visibility -ne 'Visible' -or $row.FindName('LoaderWarningBorder').IsHitTestVisible -or -not $row.ToolTip.ToString().Contains('Fabric')){throw 'Wrong-loader mod lacks a non-blocking red frame and explanation in the native mod list'}
  $row.Checked=$true;$row.Checked=$false
  if($row.FindName('LoaderWarningBorder').Visibility -ne 'Visible'){throw 'Selecting a mod cleared the loader warning'}
  $nativeRows.Children.Add($row)|Out-Null
 }
 $normal=Join-Path $foreignRoot 'fabric-correct.jar';Jar $normal 'correct_loader' @{};Call $inspector 'Inspect' @($foreignRoot,$context,$none)|Out-Null
 $row=[Activator]::CreateInstance($rowType);$row.Entry=$entryType.GetConstructor(@([string])).Invoke(@([string]$normal));$row.Title='Fabric compatible';$row.Description='fabric-correct.jar';$refreshWarning.Invoke($row,@([string]$foreignRoot,$context))|Out-Null
 if($row.FindName('LoaderWarningBorder').Visibility -ne 'Collapsed'){throw 'A compatible Fabric mod was marked red'};$nativeRows.Children.Add($row)|Out-Null
 Render $nativeRows 'loader-mismatch-mod-list.png' 740 230
 $wrongRow=$nativeRows.Children[0];$changedContext=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionContext'));$changedContext.MinecraftVersion='1.21.1';$changedContext.LoaderKnown=$true;$changedContext.LoaderVersions.Add('neoforge','21.1.100')
 $refreshWarning.Invoke($wrongRow,@([string]$foreignRoot,$changedContext))|Out-Null
 if($wrongRow.FindName('LoaderWarningBorder').Visibility -ne 'Collapsed'){throw 'Changing the loader kept a red frame from the prior inspection'}
 $enabledPath=$wrongRow.Entry.File.FullName;$disabledPath=$enabledPath+'.disabled';[IO.File]::Move($enabledPath,$disabledPath);$wrongRow.Entry=$entryType.GetConstructor(@([string])).Invoke(@([string]$disabledPath));$refreshWarning.Invoke($wrongRow,@([string]$foreignRoot,$context))|Out-Null
 if($wrongRow.FindName('LoaderWarningBorder').Visibility -ne 'Collapsed'){throw 'A disabled wrong-loader mod retained its red frame'}
 Write-Output 'PASS: native mod-list red frames, tooltip, selection/hover hit-testing, compatible rows and disabled/changed-loader clearing.'
 $legacyContext=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionContext'));$legacyContext.MinecraftVersion='1.12.2';$legacyContext.LoaderKnown=$true;$legacyContext.LoaderVersions.Add('forge','14.23.5.2855')
 $emptyLegacy=Call $inspector 'Inspect' @((Join-Path $fixture 'empty-legacy'),$legacyContext,$none)
 $description=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+ModInfo'));$description.Id='legacy_fixture';$description.Enabled=$true;$description.RootPath=Join-Path $fixture 'legacy-fixture.jar';$emptyLegacy.Mods.Add($description)
 $requestCount=$script:Requests.Count;$legacyPlan=Call $service 'BuildPlan' @($foreignRoot,$emptyLegacy,$none,(Options))
 if($legacyPlan.Items.Count -or $script:Requests.Count -ne $requestCount){throw 'A complete legacy pack queried platform records and proposed extra dependencies'}
 $uncertain=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionIssue'));$uncertain.Kind=[Enum]::Parse($uncertain.Kind.GetType(),'MissingRequired');$uncertain.Owner=$description;$uncertain.Requirement=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$uncertain.Requirement.Id='runtime_unconfirmed';$emptyLegacy.MetadataIncomplete=$true;$emptyLegacy.Issues.Add($uncertain)
 $legacyPlan=Call $service 'BuildPlan' @($foreignRoot,$emptyLegacy,$none,(Options))
 if($legacyPlan.Items.Count -or $script:Requests.Count -ne $requestCount -or -not @($legacyPlan.Notes|Where-Object {$_ -like '*运行时声明尚未确认*'}).Count){throw 'Unconfirmed legacy runtime providers triggered speculative dependency downloads'}
 foreach($key in @('LaunchModDependencyCheck','LaunchModDependencyAutoFill')){if($settings[$key].Source.ToString() -ne 'Normal'){throw 'Launch preference is not persistent local configuration'}}
 Write-Output 'PASS: native homepage placement, conditional/persistent switches, disabled launch bypass, grouped modal and collapsed diagnostics.'
}finally{
 $app.Shutdown();$full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '\APclLaunchDependencies-[a-f0-9]{32}\z'){throw 'Unsafe dependency fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
