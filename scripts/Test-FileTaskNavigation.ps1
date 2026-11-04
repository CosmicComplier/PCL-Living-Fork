param([Parameter(Mandatory=$true)][string]$ExecutablePath)
# Exercise the real PageBack path on native controls without constructing a launcher window.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$entries=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $entries.Values){$entry.ValueCache['']=$entry.DefaultValue};$entries['Identify'].ValueCache['']='file-navigation-fixture'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true);$app.InitializeComponent()
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclFileNavigation-'+[Guid]::NewGuid().ToString('N'))
$asm.GetType('PCL.LocalGameData').GetProperty('TestRoot',$flags).SetValue($null,(Join-Path $fixture 'profile'),$null)
$asm.GetType('PCL.EnvironmentHistory').GetProperty('TestRoot',$flags).SetValue($null,(Join-Path $fixture 'history'),$null)
$asm.GetType('PCL.WorldBackups').GetProperty('TestRoot',$flags).SetValue($null,(Join-Path $fixture 'world-backups'),$null)
$mainType=$asm.GetType('PCL.FormMain');$modMain=$asm.GetType('PCL.ModMain');$leftType=$asm.GetType('PCL.PageInstanceLeft');$pageType=$asm.GetType('PCL.FormMain+PageType');$subType=$asm.GetType('PCL.FormMain+PageSubType');$stackType=$asm.GetType('PCL.FormMain+PageStackData')
function Stack([string]$name){$item=[Activator]::CreateInstance($stackType);$item.Page=[Enum]::Parse($pageType,$name);return $item}
function Set-Control($owner,[string]$name,$value){$owner.GetType().GetProperty($name,$flags).SetValue($owner,$value,$null)}
function Stop-Animations {foreach($name in @('FrmMain Titlebar SubLayer','FrmMain PageChangeLeft','FrmMain PageChangeRight','PageLeft PageChange','FrmMain LeftChange')){$asm.GetType('PCL.ModAnimation').GetMethod('AniStop',$flags).Invoke($null,@($name))|Out-Null}}
$worker=$null
try{
 $game=Join-Path $fixture 'minecraft/versions/fixture';[IO.Directory]::CreateDirectory((Join-Path $game 'PCL'))|Out-Null
 [IO.File]::WriteAllText((Join-Path $game 'fixture.json'),'{"id":"fixture","type":"release","mainClass":"fixture.Main","libraries":[]}')
 [IO.File]::WriteAllText((Join-Path $game 'PCL/Setup.ini'),'VersionArgumentIndieV2:True')
 $instance=[Activator]::CreateInstance($asm.GetType('PCL.ModMinecraft+McInstance'),@([string]$game));$leftType.GetField('Instance',$flags).SetValue($null,$instance)
 foreach($entry in $entries.Values|Where-Object {$_.Source.ToString() -eq 'Instance'}){$entry.ValueCache[$instance.PathVersion]=$entry.DefaultValue};$entries['VersionArgumentIndieV2'].ValueCache[$instance.PathVersion]=$true
 $main=[Runtime.Serialization.FormatterServices]::GetUninitializedObject($mainType);$modMain.GetField('FrmMain',$flags).SetValue($null,$main)
 foreach($name in @('PanTitleMain','PanTitleInner','LabTitleInner')){Set-Control $main $name ([Activator]::CreateInstance($mainType.GetProperty($name,$flags).PropertyType))}
 foreach($name in @('BtnExtraDownload','BtnExtraApril','BtnExtraBack')){Set-Control $main $name ([Runtime.Serialization.FormatterServices]::GetUninitializedObject($mainType.GetProperty($name,$flags).PropertyType))}
 $left=[Activator]::CreateInstance($leftType);$modMain.GetField('FrmInstanceLeft',$flags).SetValue($null,$left)
 $locks=$asm.GetType('PCL.ModMinecraft+McInstanceLock');$worker=[MeloongCore.RedoableWorker]::new([Action]{})
 if(-not $locks.GetMethod('TryAcquireSnapshot',$flags).Invoke($null,@($worker,$instance.PathVersion))){throw 'Fixture snapshot reservation unavailable'}
 foreach($mode in @('snapshot','completed')){
  if($mode -eq 'completed'){$worker.Start([Threading.CancellationToken]::None,$null);$worker.WaitIfRunning(5000,[Threading.CancellationToken]::None)|Out-Null;$worker=$null}
  foreach($name in @('InstanceRestorePoints','InstanceClone','InstanceSaves')){
   Stop-Animations
   $id=[Enum]::Parse($subType,$name);$left.PageID=$id;$item=$leftType.GetMethod('NavigationItem',$flags).Invoke($left,@($id));$item.SetChecked($true,$false,$false)
   $expected=$left.PageGet($id)
   $panLeft=[Windows.Controls.Border]::new();$panLeft.Child=[Activator]::CreateInstance($asm.GetType('PCL.MyPageLeft'));Set-Control $main 'PanMainLeft' $panLeft
   $panRight=[Windows.Controls.Border]::new();$panRight.Child=[Activator]::CreateInstance($asm.GetType('PCL.MyPageRight'));Set-Control $main 'PanMainRight' $panRight
   $main.PageLeft=$panLeft.Child;$main.PageRight=$panRight.Child;$main.PageCurrent=Stack 'TaskManager'
   $main.PageStack=[Activator]::CreateInstance($mainType.GetField('PageStack',$flags).FieldType);$main.PageStack.Add((Stack 'InstanceSetup'))
   $main.PageBack()
   $actual=$main.PageRight.GetType().Name
   if(-not [object]::ReferenceEquals($expected,$main.PageRight) -or $left.PageID -ne $id -or -not $item.Checked){throw ($mode+' return to '+$name+' opened '+$actual+' instead of the originating subpage')}
   Write-Output ('PASS '+$mode+' task return: '+$name+'; sidebar and content agree')
  }
  if($mode -eq 'snapshot'){
   # Clicking a temporarily restricted editor must preserve both the old content and its highlight.
   $previous=$main.PageRight;$previousId=$left.PageID;$oldItem=$leftType.GetMethod('NavigationItem',$flags).Invoke($left,@($previousId))
   $editId=[Enum]::Parse($subType,'InstanceSetup');$edit=$leftType.GetMethod('NavigationItem',$flags).Invoke($left,@($editId));$edit.SetChecked($true,$true,$false)
   if($left.PageID -ne $previousId -or -not $oldItem.Checked -or $edit.Checked -or -not [object]::ReferenceEquals($previous,$main.PageRight)){throw 'Rejected editor click left sidebar and content inconsistent'}
   # A genuine fallback from a previously open editor selects Overview as well as showing it.
   $left.PageID=$editId;$edit.SetChecked($true,$false,$false);$main.PageCurrent=Stack 'TaskManager';$main.PageStack.Clear();$main.PageStack.Add((Stack 'InstanceSetup'))
   $main.PageBack();$overview=[Enum]::Parse($subType,'InstanceOverall');$overviewItem=$leftType.GetMethod('NavigationItem',$flags).Invoke($left,@($overview))
   if($left.PageID -ne $overview -or -not $overviewItem.Checked -or $edit.Checked -or -not [object]::ReferenceEquals($left.PageGet($overview),$main.PageRight)){throw 'Restricted-editor fallback selected a different page from its content'}
   Write-Output 'PASS restricted-editor rejection and fallback keep navigation consistent'
  }
 }
}finally{
 Stop-Animations
 if($worker){$worker.Start([Threading.CancellationToken]::None,$null);$worker.WaitIfRunning(5000,[Threading.CancellationToken]::None)|Out-Null}
 $app.Shutdown();$full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^PclFileNavigation-[a-f0-9]{32}$'){throw 'Unsafe navigation fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
