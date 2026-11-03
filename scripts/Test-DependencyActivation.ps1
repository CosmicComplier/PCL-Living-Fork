param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$PreviewDirectory,[string]$FabricApiPath)
# Only GUID fixtures are renamed. An optional real Fabric API JAR is copied and read, never edited.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.IO.Compression,System.IO.Compression.FileSystem
foreach($p in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $p))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true);$app.InitializeComponent()
[Windows.Application].GetField('_startupUri',[Reflection.BindingFlags]'NonPublic,Instance').SetValue($app,$null)
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
[Windows.Application]::ResourceAssembly=$asm
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='disabled-provider-fixture'
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclDependencyActivation-'+[Guid]::NewGuid().ToString('N'))
$inspector=$asm.GetType('PCL.ModDependencyInspector');$activation=$asm.GetType('PCL.ModDependencyActivation');$service=$asm.GetType('PCL.ModDependencyAutoFill');$none=[Threading.CancellationToken]::None
function Call($type,$name,[object[]]$arguments){$values=New-Object object[] $arguments.Count;for($i=0;$i -lt $values.Length;$i++){if($null -ne $arguments[$i]){$values[$i]=$arguments[$i].PSObject.BaseObject}};return ,$type.GetMethod($name,$flags).Invoke($null,$values)}
function Jar($path,$metadata,[hashtable]$extra=@{}){
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))|Out-Null;$zip=[IO.Compression.ZipFile]::Open($path,[IO.Compression.ZipArchiveMode]::Create)
 try{foreach($name in @('fabric.mod.json')+@($extra.Keys)){$stream=$zip.CreateEntry($name).Open();try{$value=if($name -eq 'fabric.mod.json'){[Text.Encoding]::UTF8.GetBytes(($metadata|ConvertTo-Json -Depth 15 -Compress))}else{$extra[$name]};$stream.Write($value,0,$value.Length)}finally{$stream.Dispose()}}}finally{$zip.Dispose()}
}
function Scan{return ,(Call $inspector 'Inspect' @($mods,$context,$none))}
function Plan{return ,(Call $activation 'Prepare' @($mods,(Scan),$none))}
function Reject([scriptblock]$work,$message){$failed=$false;try{& $work|Out-Null}catch{$failed=$true};if(-not $failed){throw $message}}
function Render($view,$name){if(-not $PreviewDirectory){return};[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null;$hostPanel=[Windows.Controls.Grid]::new();$hostPanel.Background=[Windows.Media.BrushConverter]::new().ConvertFromString('#e7effb');$hostPanel.Children.Add($view)|Out-Null;$hostPanel.Measure([Windows.Size]::new(780,540));$hostPanel.Arrange([Windows.Rect]::new(0,0,780,540));$hostPanel.UpdateLayout();$image=[Windows.Media.Imaging.RenderTargetBitmap]::new(780,540,96,96,[Windows.Media.PixelFormats]::Pbgra32);$image.Render($hostPanel);$enc=[Windows.Media.Imaging.PngBitmapEncoder]::new();$enc.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($image));$out=[IO.File]::Create((Join-Path $PreviewDirectory $name));try{$enc.Save($out)}finally{$out.Dispose()};$hostPanel.Children.Clear()}
try{
 $mods=Join-Path $fixture 'mods';[IO.Directory]::CreateDirectory($mods)|Out-Null
 $context=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionContext'));$context.MinecraftVersion='1.21.11';$context.JavaVersion='21.0.2';$context.LoaderKnown=$true;$context.LoaderVersions.Add('fabricloader','0.19.3')
 $ids=@('fabric-resource-conditions-api-v1','fabric-events-interaction-v0','fabric-networking-api-v1')
 $entries=@{};$jars=@()
 for($i=0;$i -lt $ids.Count;$i++){$child=Join-Path $fixture ('child'+$i+'.jar');Jar $child @{schemaVersion=1;id=$ids[$i];name=$ids[$i];version='5.0.35+test'};$entry='META-INF/jars/child'+$i+'.jar';$entries[$entry]=[IO.File]::ReadAllBytes($child);$jars+=@{file=$entry}}
 $disabled=Join-Path $mods 'fabric-api-test.jar.disabled';Jar $disabled @{schemaVersion=1;id='fabric-api';name='Fabric API';version='0.141.6+1.21.11';jars=$jars;depends=@{minecraft='>=1.21.11';fabricloader='>=0.18.0'}} $entries
 Jar (Join-Path $mods 'lamb.jar') @{schemaVersion=1;id='lamb';name='LambDynamicLights (Runtime)';version='1.0';depends=@{$ids[0]='>=5.0.35'}}
 Jar (Join-Path $mods 'opti.jar') @{schemaVersion=1;id='opti';name='OptiGUI';version='1.0';depends=@{$ids[1]='*';$ids[2]='*'}}
 $report=Scan;$plan=Plan
 if($plan.Enables.Count -ne 1 -or $plan.Items.Count -ne 0 -or $plan.Enables[0].Requirements.Count -ne 3){throw ('Bundled disabled modules not grouped to one JAR: '+($plan.Notes -join '; '))}
 if($plan.GetType().GetProperty('ActionText',$flags).GetValue($plan) -ne '启用并启动'){throw 'Disabled dependencies do not expose enable-and-launch'}
 if(@($report.Mods|Where-Object {$_.RootPath -eq $disabled -and $_.Enabled}).Count){throw 'Preparing enable plan mutated the original report'}
 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $viewType=$asm.GetType('PCL.ModDependencyCheckView');$view=($viewType.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 2}|Select-Object -First 1).Invoke(@($report,$plan))
 $groups=$viewType.GetField('Groups',$flags).GetValue($view)
 if(@($groups.Children|Where-Object {$_.Title -like '缺少依赖*'}).Count -or -not @($groups.Children|Where-Object {$_.Title -like '可启用*1 个文件*'}).Count){throw 'Activation modal still shows bundled IDs as missing external mods'}
 Render $view 'disabled-fabric-api.png'
 if($PreviewDirectory){
  $converter=[Activator]::CreateInstance($asm.GetType('PCL.ModMain+MyMsgBoxConverter'));$converter.Title='启动前模组检查';$converter.Text='请检查以下模组问题。';$converter.Button1=$plan.GetType().GetProperty('ActionText',$flags).GetValue($plan);$converter.Button2='取消启动';$converter.Button3='仍然启动';$converter.IsWarn=$true;$converter.WarningButton=3;$converter.ContentFactory=[Func[Windows.FrameworkElement]]{return $view}
  $modal=[Activator]::CreateInstance($asm.GetType('PCL.MyMsgText'),@($converter));$modal.FindName('TransformRotate').Angle=0;$modal.FindName('TransformPos').Y=0;Render $modal 'enable-and-launch-dialog.png'
 }
 $sourceHash=(Get-FileHash $disabled).Hash;$dest=$plan.Enables[0].Destination
 $cancel=[Threading.CancellationTokenSource]::new();$cancel.Cancel();try{Reject {Call $service 'Install' @($plan,$cancel.Token,$null)} 'Canceled activation modified disabled mods'}finally{$cancel.Dispose()}
 if(-not [IO.File]::Exists($disabled) -or [IO.File]::Exists($dest)){throw 'Cancellation did not retain disabled state'}
 $count=Call $service 'Install' @($plan,$none,$null)
 if($count -ne 1 -or (Scan).HasErrors -or [IO.File]::Exists($disabled) -or (Get-FileHash $dest).Hash -ne $sourceHash){throw 'Outer JAR activation/recheck failed or changed its bytes'}
 [IO.File]::Move($dest,$disabled)
 $plan=Plan;$collision=$plan.Enables[0].Destination;[IO.File]::WriteAllText($collision,'existing-do-not-overwrite')
 Reject {Call $service 'Install' @($plan,$none,$null)} 'Activation overwrote an existing enabled target'
 if([IO.File]::ReadAllText($collision) -ne 'existing-do-not-overwrite' -or -not [IO.File]::Exists($disabled)){throw 'Conflicting activation damaged files'};[IO.File]::Delete($collision)

 # Force a later file-install failure after the enable rename, then check rollback.
 $plan=Plan;$extra=Join-Path $fixture 'extra.jar';Jar $extra @{schemaVersion=1;id='extra';name='Extra';version='1.0'}
 $item=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyAutoFill+Item'));$item.Name='Extra'
 $version=[PCL.ResourceVersion]::new();$version.Id='extra';$version.ResourceType=[PCLCS.ResourceTypes]::Mod;$version.ModLoaders=[PCLCS.ModLoaders]::Fabric;$version.GameVersions=[Collections.Generic.List[string]]@('1.21.11');$version.FileName='conflict.jar';$version.Size=(Get-Item $extra).Length;$version.Hash=(Get-FileHash $extra -Algorithm SHA1).Hash.ToLowerInvariant();$version.DownloadUrls=[Collections.Generic.List[string]]@('https://cdn.modrinth.com/data/extra.jar');$item.Version=$version;$plan.Items.Add($item)
 $conflict=Join-Path $mods 'conflict.jar';[IO.Directory]::CreateDirectory($conflict)|Out-Null
 $downloadType=[Action``2].MakeGenericType([Collections.Generic.List``1].MakeGenericType($asm.GetType('PCL.ModNet+NetFile')),[Threading.CancellationToken]);$download=[Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{param($files,$token);foreach($file in $files){[IO.File]::Copy($extra,$file.LocalPath)}},$downloadType)
 Reject {Call $service 'Install' @($plan,$none,$download)} 'Mixed activation/download ignored a target collision'
 if(-not [IO.File]::Exists($disabled) -or [IO.File]::Exists($dest) -or (Get-FileHash $disabled).Hash -ne $sourceHash){throw 'Failed batch did not restore original disabled state'};[IO.Directory]::Delete($conflict)
 $files=[Collections.Generic.List[PCL.ModNet+NetFile]]::new();$files.Add($version.ToNetFile((Join-Path $fixture 'target.jar')))
 $task=Call $service 'CreateDownloadTask' @($plan,$files)
 if($task.Loaders.Count -ne 1 -or $task.Loaders[0].GetType().Name -ne 'LoaderDownload' -or $task.Loaders[0].Name -notlike '*Extra*'){throw 'Dependency repair is not represented as native named download tasks'}
 $task.Cancel()
 if($task.State.ToString() -ne 'Canceled' -or $task.Loaders[0].State.ToString() -ne 'Canceled'){throw 'Native task cancellation does not reach the dependency download'}
 $context.MinecraftVersion='1.20.1';if((Plan).Enables.Count -ne 0){throw 'Incompatible disabled outer JAR can be auto-enabled'};$context.MinecraftVersion='1.21.11'
 Copy-Item -LiteralPath $disabled -Destination (Join-Path $mods 'another-api.jar.disabled');if((Plan).Enables.Count -ne 0){throw 'Ambiguous disabled providers were arbitrarily selected'};[IO.File]::Delete((Join-Path $mods 'another-api.jar.disabled'))
 Write-Output 'PASS: nested-module grouping, version validation, enable/recheck, cancellation, no-overwrite, mixed rollback, ambiguity and native download task construction.'
 if($FabricApiPath){
  $original=(Get-FileHash -LiteralPath $FabricApiPath).Hash
  [IO.File]::Delete($disabled);Copy-Item -LiteralPath $FabricApiPath -Destination $disabled
  $plan=Plan
  if($plan.Enables.Count -ne 1 -or $plan.Enables[0].Requirements.Count -ne 3){throw ('Real Fabric API disabled components not resolved: '+($plan.Notes -join '; '))}
  Render (($viewType.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 2}|Select-Object -First 1).Invoke(@((Scan),$plan))) 'real-disabled-fabric-api.png'
  Call $service 'Install' @($plan,$none,$null)|Out-Null
  if((Scan).HasErrors -or (Get-FileHash -LiteralPath $FabricApiPath).Hash -ne $original){throw 'Real fixture recheck failed or source JAR changed'}
  Write-Output 'PASS: read-only real Fabric API copy resolves all three pictured component IDs; temporary enable and recheck passed.'
 }
}finally{
 $app.Shutdown();$full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '\APclDependencyActivation-[a-f0-9]{32}\z'){throw 'Unsafe activation fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
