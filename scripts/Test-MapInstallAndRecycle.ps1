param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$PreviewDirectory)
# Synthetic ZIPs/worlds and offscreen native controls only; never start a game or access real profiles.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.IO.Compression,System.IO.Compression.FileSystem
foreach($p in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $p))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true);$app.InitializeComponent()
# The dispatcher pump below must not follow Application.xaml's real StartupUri.
[Windows.Application].GetField('_startupUri',[Reflection.BindingFlags]'NonPublic,Instance').SetValue($app,$null)
$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
[Windows.Application]::ResourceAssembly=$asm
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='map-recycle-fixture'
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclMapRecycle-'+[Guid]::NewGuid().ToString('N'))
$none=[Threading.CancellationToken]::None
$store=$asm.GetType('PCL.InstanceResourceStore');$recycle=$asm.GetType('PCL.ResourceRecycleStore');$installer=$asm.GetType('PCL.MapDownloadInstaller')
$kind=[Enum]::Parse($asm.GetType('PCL.InstanceResourceKind'),'Saves')
function Call($type,[string]$method,[object[]]$arguments){
 $values=New-Object object[] $arguments.Length
 for($i=0;$i -lt $values.Length;$i++){if($null -ne $arguments[$i]){$values[$i]=$arguments[$i].PSObject.BaseObject}}
 try{return ,$type.GetMethod($method,$flags).Invoke($null,$values)}catch{throw ($method+': '+$_.Exception.ToString())}
}
function Put([string]$p,[string]$s){[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($p))|Out-Null;[IO.File]::WriteAllText($p,$s)}
function Zip([string]$p,[hashtable]$entries){$z=[IO.Compression.ZipFile]::Open($p,[IO.Compression.ZipArchiveMode]::Create);try{foreach($name in $entries.Keys){$w=[IO.StreamWriter]::new($z.CreateEntry($name).Open());try{$w.Write($entries[$name])}finally{$w.Dispose()}}}finally{$z.Dispose()}}
function Reject([scriptblock]$work,[string]$message){$failed=$false;try{& $work|Out-Null}catch{$failed=$true};if(-not $failed){throw $message}}
function Text([string]$p,[string]$expected){if(-not [IO.File]::Exists($p) -or [IO.File]::ReadAllText($p) -cne $expected){throw ('Wrong world contents: '+$p)}}
try{
 [IO.Directory]::CreateDirectory($fixture)|Out-Null;$saves=Join-Path $fixture 'game/saves'
 $archives=Call $installer 'GetDownloadFolder' @($saves)
 if($archives -ne (Join-Path $saves 'PCLLFWorldDownload') -or -not [IO.Directory]::Exists($archives)){throw 'Map archive directory was not created under saves'}
 if((Call $installer 'GetDownloadFolder' @($saves)) -ne $archives){throw 'Existing archive directory was not reused'}
 $flat=Join-Path $archives 'Flat map.zip';Zip $flat @{'level.dat'='flat-world';'region/data.txt'='region-data'}
 $hash=(Get-FileHash -LiteralPath $flat).Hash;$world=Call $installer 'Install' @($flat,$saves,$none)
 if($world -ne (Join-Path $saves 'Flat map') -or [IO.Directory]::Exists((Join-Path $archives 'Flat map'))){throw 'Map was extracted inside the ZIP download directory'}
 Text (Join-Path $world 'level.dat') 'flat-world';Text (Join-Path $world 'region/data.txt') 'region-data'
 if((Get-FileHash -LiteralPath $flat).Hash -cne $hash){throw 'Auto install changed the original downloaded ZIP'}
 $second=Call $installer 'Install' @($flat,$saves,$none)
 if($world -ceq $second){throw 'Repeat map download reused an existing world'};Text (Join-Path $world 'level.dat') 'flat-world'
 $wrapped=Join-Path $archives 'Wrapped.zip';Zip $wrapped @{'outer/world/level.dat'='wrapped-world';'outer/world/region/data.txt'='wrapped-data';'README.txt'='readme'}
 $step=$asm.GetType('PCL.PageDownloadResourceDetail').GetMethod('CreateMapInstallLoader',$flags).Invoke($null,@([string]$wrapped,[string]$saves))
 $step.WaitForExit(1,$null,$false,$none,$null,$true)
 Text (Join-Path $saves 'Wrapped/level.dat') 'wrapped-world'
 $bad=Join-Path $archives 'Bad.zip';Zip $bad @{'../outside.txt'='bad';'level.dat'='bad'};Reject {Call $installer 'Install' @($bad,$saves,$none)} 'Map traversal accepted'
 if(Test-Path -LiteralPath (Join-Path $fixture 'outside.txt')){throw 'Map wrote outside staging'}
 $multiple=Join-Path $archives 'Multiple.zip';Zip $multiple @{'one/level.dat'='one';'two/level.dat'='two'};Reject {Call $installer 'Install' @($multiple,$saves,$none)} 'Multi-world archive silently selected a world'
 $missing=Join-Path $archives 'Not a world.zip';Zip $missing @{'README.txt'='not a world'};Reject {Call $installer 'Install' @($missing,$saves,$none)} 'Map without level.dat was installed'
 $cancel=[Threading.CancellationTokenSource]::new();$cancel.Cancel();try{Reject {Call $installer 'Install' @($flat,$saves,$cancel.Token)} 'Canceled map installed'}finally{$cancel.Dispose()}
 if(@(Get-ChildItem -LiteralPath (Join-Path $fixture 'game') -Directory -Filter '.pcl-import-*').Count){throw 'Map left incomplete staging folders'}
 Write-Output 'PASS: map download completion installs flat/wrapped worlds, preserves ZIP/existing worlds, rejects traversal/ambiguous archives and cleans canceled staging.'

 $trash=Join-Path $fixture 'game/PCL/ResourceRecycle/Saves'
 $items=Call $store 'Scan' @($saves,$saves,$kind,$none)
 if(@($items|Where-Object Name -eq 'PCLLFWorldDownload').Count){throw 'ZIP download directory is displayed as a world'}
 $matches=@($items|Where-Object {$_.IsDirectory -and $_.Name -in @('Flat map','Flat map (2)','Wrapped')})
 $targets=[Array]::CreateInstance($asm.GetType('PCL.InstanceResourceEntry'),$matches.Count)
 for($i=0;$i -lt $matches.Count;$i++){$targets.SetValue($matches[$i].PSObject.BaseObject,$i)}
 Call $store 'RecycleMany' @($saves,$targets,$trash,$none)|Out-Null
 $deleted=Call $recycle 'List' @($saves,$trash,$kind)
 if($deleted.Count -ne 3){throw 'Recycle list lost deleted worlds'}
 $first=$deleted|Where-Object Name -eq 'Flat map'|Select-Object -First 1
 Call $recycle 'Restore' @($saves,$trash,[string[]]@($first.RecycleId),$none)|Out-Null;Text (Join-Path $saves 'Flat map/level.dat') 'flat-world'
 $deleted=Call $recycle 'List' @($saves,$trash,$kind);$wrappedRecord=$deleted|Where-Object Name -eq 'Wrapped'|Select-Object -First 1
 Put (Join-Path $saves 'Wrapped/level.dat') 'new-progress'
 Reject {Call $recycle 'Restore' @($saves,$trash,[string[]]@($wrappedRecord.RecycleId),$none)} 'Restore overwrote a new same-name world'
 Call $recycle 'Purge' @($saves,$trash,[string[]]@($wrappedRecord.RecycleId),$none)|Out-Null;Text (Join-Path $saves 'Wrapped/level.dat') 'new-progress'
 Reject {Call $recycle 'Purge' @($saves,$trash,[string[]]@('../outside'),$none)} 'Invalid recycle ID accepted'
 $other=Join-Path $fixture 'other/saves';Put (Join-Path $other 'Other/level.dat') 'other-world'
 $otherItems=Call $store 'Scan' @($other,$other,$kind,$none);Call $store 'RecycleMany' @($other,$otherItems,$trash,$none)|Out-Null
 if((Call $recycle 'List' @($saves,$trash,$kind)).Count -ne 1){throw 'Recycle list exposed another game directory'}
 $otherRecord=(Call $recycle 'List' @($other,$trash,$kind))[0]
 Reject {Call $recycle 'Purge' @($saves,$trash,[string[]]@($otherRecord.RecycleId),$none)} 'Purge accepted another directory record'
 Text (Join-Path $otherRecord.FullPath 'level.dat') 'other-world'
 $broken=Join-Path $trash ([Guid]::NewGuid().ToString('N'));Put (Join-Path $broken 'resource.json') ('{"root":'+(ConvertTo-Json $saves -Compress)+'}')
 Put (Join-Path $broken 'content/level.dat') 'unreadable-record'
 if((Call $recycle 'List' @($saves,$trash,$kind)).Count -ne 1){throw 'Malformed metadata broke the recycle list'}
 Text (Join-Path $broken 'content/level.dat') 'unreadable-record'
 Write-Output 'PASS: recycle listing, selected restore, same-name protection, permanent deletion and game-directory boundaries.'

 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $owner=[PCL.ModMinecraft+McInstance]::new((Join-Path $fixture 'game/versions/example'))
 $owner.Version=[PCLCS.McVersion]::new('1.21.11',$true)
 foreach($entry in $settings.Values|Where-Object {$_.Source.ToString() -eq 'Instance'}){$entry.ValueCache[$owner.PathVersion]=$entry.DefaultValue}
 $settings['VersionArgumentIndieV2'].ValueCache[$owner.PathVersion]=$true
 $type=$asm.GetType('PCL.PageInstanceResources');$page=$type.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 1}|Select-Object -First 1;$page=$page.Invoke(@($kind))
 $type.GetField('Owner',$flags).SetValue($page,$owner);$type.GetField('RecycleRoot',$flags).SetValue($page,$trash);$type.GetProperty('IsRecycleMode',$flags).SetValue($page,$true,$null)
 $deleted=Call $recycle 'List' @($saves,$trash,$kind);$type.GetMethod('ShowResources',$flags).Invoke($page,@([string]$saves,[string]$saves,$deleted))|Out-Null
 if($page.FindName('PanWorlds').Visibility -ne 'Collapsed' -or $page.FindName('PanList').Children.Count -ne 1){throw 'Recycle mode did not reuse the native resource list'}
 if($page.FindName('BtnDownload').Visibility -ne 'Collapsed' -or $page.FindName('BackupSettings').Visibility -ne 'Collapsed' -or $page.FindName('BtnBackupAll').Visibility -ne 'Visible'){throw 'Recycle toolbar still exposes ordinary install/backup actions'}
 $back=$page.FindName('BtnRecycleBack');$search=$page.FindName('SearchBox')
 if(-not $back -or $back.GetType().Name -ne 'MyIconTextButton' -or $back.Visibility -ne 'Visible' -or $back.Parent -ne $search.Parent -or $back.Parent.Children.IndexOf($back) -ge $back.Parent.Children.IndexOf($search)){throw 'Native back action is not above the search box'}
 if($page.FindName('BtnRestore').Visibility -ne 'Collapsed' -or $page.FindName('BtnOpenFolder').Text -ne '打开回收站文件夹'){throw 'Recycle toolbar retains the old back action or folder caption'}
 $type.GetMethod('ChangeAllSelected',$flags).Invoke($page,@($true))|Out-Null
 if(-not $page.FindName('BtnExport').IsEnabled -or -not $page.FindName('BtnDelete').IsEnabled){throw 'Recycle selection does not enable restore and permanent deletion'}
 if($page.FindName('PanAllBack').Background){throw 'Recycle mode overrides the global background'}
 $page.FindName('PanLoad').Visibility='Collapsed';$page.FindName('PanAllBack').Visibility='Visible';$page.FindName('PanAllBack').Opacity=1
 $previewHost=[Windows.Controls.Grid]::new();$previewHost.Background=[Windows.Media.Brushes]::White;$previewHost.Children.Add($page)|Out-Null
 foreach($width in @(540,780)){
  $previewHost.Measure([Windows.Size]::new($width,620));$previewHost.Arrange([Windows.Rect]::new(0,0,$width,620));$previewHost.UpdateLayout()
  $toolbar=$page.FindName('PanManageButtons');$occupied=@{}
  foreach($button in $toolbar.Children|Where-Object Visibility -eq 'Visible'){
   $cell=([Windows.Controls.Grid]::GetRow($button)).ToString()+':'+[Windows.Controls.Grid]::GetColumn($button)
   if($occupied.ContainsKey($cell) -or $button.ActualWidth -lt $button.MinWidth){throw 'Recycle toolbar buttons overlap or become too narrow'};$occupied[$cell]=$true
  }
  $visibleButtons=@($toolbar.Children|Where-Object Visibility -eq 'Visible')
  for($index=1;$index -lt $visibleButtons.Count;$index++){
   $previousButton=$visibleButtons[$index-1];$button=$visibleButtons[$index]
   if([Windows.Controls.Grid]::GetRow($previousButton) -eq [Windows.Controls.Grid]::GetRow($button)){
    $gap=$button.TranslatePoint([Windows.Point]::new(0,0),$toolbar).X-$previousButton.TranslatePoint([Windows.Point]::new(0,0),$toolbar).X-$previousButton.ActualWidth
    if([Math]::Abs($gap-15) -gt 0.1){throw 'Toolbar actions are spread across the row instead of packed to the left'}
   }
  }
  $origin=[Windows.Point]::new(0,0)
  if($back.TranslatePoint($origin,$page).Y+$back.ActualHeight -gt $search.TranslatePoint($origin,$page).Y){throw 'Back action overlaps the search box'}
 }
 if($PreviewDirectory){
  [IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null
  $null=$page.FindName('CardList').MainTextBlock
  foreach($name in @('BtnRecycleBack','BtnExport','BtnDelete','BtnDeselect')){$button=$page.FindName($name);$button.GetType().GetMethod('RefreshColor',$flags).Invoke($button,@($null,$false))|Out-Null}
  $animation=$asm.GetType('PCL.ModAnimation');$tick=$animation.GetMethod('AniTimer',$flags);$last=$animation.GetField('AniLastTick',$flags)
  for($i=0;$i -lt 45;$i++){$last.SetValue($null,[long]::MaxValue);$tick.Invoke($null,@([double]16))|Out-Null}
  $previewHost.Measure([Windows.Size]::new(780,620));$previewHost.Arrange([Windows.Rect]::new(0,0,780,620));$previewHost.UpdateLayout()
  $title=$page.FindName('CardList').MainTextBlock;$filter=$page.FindName('PanFilter');$origin=[Windows.Point]::new(0,0)
  if($filter.TranslatePoint($origin,$page.FindName('CardList')).Y -lt ($title.TranslatePoint($origin,$page.FindName('CardList')).Y+$title.ActualHeight+5)){throw 'Recycle heading overlaps the native filters'}
  $image=[Windows.Media.Imaging.RenderTargetBitmap]::new(780,620,96,96,[Windows.Media.PixelFormats]::Pbgra32);$image.Render($previewHost)
  $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($image));$output=[IO.File]::Create((Join-Path $PreviewDirectory 'resource-recycle.png'));try{$encoder.Save($output)}finally{$output.Dispose()}
 }
 $packKind=[Enum]::Parse($asm.GetType('PCL.InstanceResourceKind'),'ResourcePacks')
 $packPage=($type.GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq 1}|Select-Object -First 1).Invoke(@($packKind))
 $type.GetField('Owner',$flags).SetValue($packPage,$owner);$type.GetProperty('IsRecycleMode',$flags).SetValue($packPage,$true,$null)
 $packEntry=[Activator]::CreateInstance($asm.GetType('PCL.InstanceResourceEntry'));$packEntry.IsDirectory=$false;$packEntry.FullPath=Join-Path $fixture 'missing-pack.zip'
 $packEntries=[Activator]::CreateInstance($type.GetField('Entries',$flags).FieldType);$packEntries.Add($packEntry)
 $type.GetField('Entries',$flags).SetValue($packPage,$packEntries);$type.GetMethod('CheckUpdates',$flags).Invoke($packPage,@())|Out-Null
 if($type.GetField('Checking',$flags).GetValue($packPage) -or $type.GetField('OnlineCancellation',$flags).GetValue($packPage)){throw 'Recycle resources started an online update lookup'}
 $type.GetMethod('RestoreRecycleSelection',$flags).Invoke($page,@($null))|Out-Null
 $timer=[Diagnostics.Stopwatch]::StartNew()
 while($type.GetField('Busy',$flags).GetValue($page) -and $timer.ElapsedMilliseconds -lt 10000){
  $frame=[Windows.Threading.DispatcherFrame]::new()
  [Windows.Threading.Dispatcher]::CurrentDispatcher.BeginInvoke([Windows.Threading.DispatcherPriority]::Background,[Action]{$frame.Continue=$false})|Out-Null
  [Windows.Threading.Dispatcher]::PushFrame($frame);[Threading.Thread]::Sleep(5)
 }
 if($type.GetField('Busy',$flags).GetValue($page)){throw 'Resource restore did not finish'}
 Text (Join-Path $saves 'Flat map (2)/level.dat') 'flat-world'
 $hints=$asm.GetType('PCL.ModMain').GetField('HintWaiting',$flags).GetValue($null).ToArray()
 if(-not @($hints|Where-Object {$_.Text -eq '已还原资源' -and $_.Type.ToString() -eq 'Green'}).Count -or $page.FindName('LabOperation').Visibility -ne 'Collapsed'){throw ('Successful restore did not replace inline status with the native corner toast; status='+$page.FindName('LabOperation').Text+'; visibility='+$page.FindName('LabOperation').Visibility+'; hints='+($hints|ConvertTo-Json -Compress))}
 $empty=[Activator]::CreateInstance($type.GetField('Entries',$flags).FieldType)
 $type.GetMethod('ShowResources',$flags).Invoke($page,@([string]$saves,[string]$saves,$empty))|Out-Null
 if($page.FindName('LabEmpty').Text -ne '回收站为空。' -or $page.FindName('BtnBackupAll').IsEnabled -or $back.Visibility -ne 'Visible' -or $page.FindName('CardSelect').Visibility -ne 'Collapsed'){throw 'Empty recycle bin lost its empty state, return action or selection cleanup'}
 $type.GetMethod('ReturnToResources',$flags).Invoke($page,@())|Out-Null
 $resources=Call $store 'Scan' @($saves,$saves,$kind,$none)
 $type.GetMethod('ShowResources',$flags).Invoke($page,@([string]$saves,[string]$saves,$resources))|Out-Null
 if($back.Visibility -ne 'Collapsed' -or $page.FindName('BtnRestore').Visibility -ne 'Visible' -or $page.FindName('BtnOpenFolder').Text -ne '打开文件夹' -or $page.FindName('BackupSettings').Visibility -ne 'Visible' -or $page.FindName('PanWorlds').Visibility -ne 'Visible' -or $page.FindName('PanFilter').Margin.Top -ne 13){throw 'Returning from recycle mode did not restore normal native controls'}
 Write-Output 'PASS: native recycle list, selection actions, top toolbar and hidden ordinary resource controls.'
}finally{
 $app.Shutdown();$full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '\APclMapRecycle-[a-f0-9]{32}\z'){throw 'Unsafe map/recycle fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
