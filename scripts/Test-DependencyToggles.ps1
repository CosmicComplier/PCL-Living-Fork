param([Parameter(Mandatory=$true)][string]$ExecutablePath)
# Hosts only the detached options and recent-world cards in an offscreen window. Never loads the launcher page or writes real settings.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
foreach($p in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $p))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true);$app.InitializeComponent()
[Windows.Application].GetField('_startupUri',[Reflection.BindingFlags]'NonPublic,Instance').SetValue($app,$null)
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown;[Windows.Application]::ResourceAssembly=$asm
$entries=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $entries.Values){$entry.ValueCache['']=$entry.DefaultValue};$entries['Identify'].ValueCache['']='first-launch-switch-fixture'
$window=$null
try{
 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $page=[Activator]::CreateInstance($asm.GetType('PCL.PageLaunchRight'))
 $master=$page.FindName('CheckModDependencies');$auto=$page.FindName('CheckAutoFillDependencies');$card=$page.FindName('CardDependencyChecks')
 # Simulates first-page content, which skips PageEnter. No manual refresh is called here.
 if(-not $master.Checked -or -not $auto.Checked -or $auto.Visibility -ne 'Visible'){throw 'First page construction did not initialize both default-on switches'}
 $anchor=$page.FindName('PanDependencyOptionsAnchor');$recent=$page.FindName('CardRecentWorlds')
 $anchor.Parent.Children.Remove($anchor);$recent.Parent.Children.Remove($recent)
 $hostPanel=[Windows.Controls.Grid]::new();$hostPanel.Children.Add($anchor)|Out-Null;$hostPanel.Children.Add($recent)|Out-Null
 $card.Visibility='Visible';$card.Opacity=1;$recent.Visibility='Visible';$recent.Opacity=1
 $window=[Windows.Window]::new();$window.Content=$hostPanel;$window.Width=320;$window.Height=300;$window.Left=-30000;$window.Top=-30000;$window.ShowInTaskbar=$false;$window.ShowActivated=$false;$window.Show()
 $window.UpdateLayout()
 if(-not $master.IsLoaded){throw 'Toggle fixture was not connected to a presentation source'}
 $origin=[Windows.Point]::new(0,0)
 foreach($value in @($false,$true,$false,$true)){
  # The setting already equals the clicked value: Settings.Set must return without a callback or disk write.
  $entries['LaunchModDependencyCheck'].ValueCache['']=$value
  $master.SetChecked($value,$true);$window.UpdateLayout()
  if($auto.Visibility -ne $(if($value){'Visible'}else{'Collapsed'})){throw 'Manual checkbox change did not immediately expand/collapse its child when the settings callback was skipped'}
  $gap=$recent.TranslatePoint($origin,$hostPanel).Y-$card.TranslatePoint($origin,$hostPanel).Y-$card.ActualHeight
  if([Math]::Abs($gap-10) -gt 0.1){throw ('Options card did not keep its bottom gap when expanded/collapsed: '+$gap)}
  if($value -and $auto.TranslatePoint($origin,$window).Y -lt ($master.TranslatePoint($origin,$window).Y+$master.ActualHeight)){throw 'Child checkbox is not below its parent'}
 }
 $entries['LaunchModDependencyAutoFill'].ValueCache['']=$false;$auto.SetChecked($false,$true)
 $page.GetType().GetMethod('RefreshDependencyOptions',$flags).Invoke($page,@())|Out-Null
 if($auto.Checked -or -not $master.Checked -or $auto.Visibility -ne 'Visible'){throw 'Refresh overwrote the player choice to disable autofill'}
 $entries['LaunchModDependencyCheck'].ValueCache['']=$false;$master.SetChecked($false,$true)
 $page.GetType().GetMethod('RefreshDependencyOptions',$flags).Invoke($page,@())|Out-Null
 if($master.Checked -or $auto.Visibility -ne 'Collapsed'){throw 'Refresh overwrote the player choice to disable precheck'}
 $entries['LaunchModDependencyAutoFill'].ValueCache['']=$false
 $reopened=[Activator]::CreateInstance($asm.GetType('PCL.PageLaunchRight'))
 if($reopened.FindName('CheckModDependencies').Checked -or $reopened.FindName('CheckAutoFillDependencies').Checked -or $reopened.FindName('CheckAutoFillDependencies').Visibility -ne 'Collapsed'){throw 'Recreating the homepage overwrote saved-off preferences'}
 if(-not $entries['LaunchModDependencyCheck'].DefaultValue -or -not $entries['LaunchModDependencyAutoFill'].DefaultValue){throw 'First-use defaults are not both enabled'}
 Write-Output 'PASS: default-on first page, real loaded-control clicks without settings callbacks, repeated child expansion and preservation of player choices.'
}finally{if($window){$window.Close()};$app.Shutdown()}
