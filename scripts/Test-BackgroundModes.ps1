param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$PreviewDirectory)
# Offscreen native controls and in-memory settings only; no startup or real profiles.
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'Use powershell -NoProfile -STA.'}
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
foreach($p in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $p))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true);$app.InitializeComponent()
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
[Windows.Application]::ResourceAssembly=$asm
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue}
$settings['Identify'].ValueCache['']='background-modes-fixture'
try{
 $entry=$settings['UiBackgroundMode']
 if(-not $entry -or $entry.Source.ToString() -ne 'Normal' -or $entry.DefaultValue -ne 0 -or -not $entry.OnChanged){throw 'Background mode is not a persistent local setting retaining the existing default'}
 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $page=[Activator]::CreateInstance($asm.GetType('PCL.PageSetupUI'));$combo=$page.FindName('ComboBackgroundMode')
 $service=$asm.GetType('PCL.SettingService')
 if($service.GetMethod('GetKey').Invoke($null,@($combo)) -ne 'UiBackgroundMode' -or $combo.Items.Count -ne 2){throw 'Native background selector is not bound to the saved mode'}
 $hostPanel=[Windows.Controls.Grid]::new();$image=[Windows.Controls.Canvas]::new();$image.Background=[Windows.Media.Brushes]::CornflowerBlue;$image.Opacity=0.748
 $hostPanel.Children.Add($image)|Out-Null;$hostPanel.Children.Add($page)|Out-Null
 $render=$asm.GetType('PCL.ModSecret').GetMethod('ApplyMainBackground',$flags)
 $refreshUi=$page.GetType().GetMethod('BackgroundRefreshUI',$flags)
 $savedOpacity=$settings['UiBackgroundOpacity'].ValueCache[''];$savedColorful=$settings['UiBackgroundColorful'].ValueCache[''];$savedImage=$image.Background
 foreach($mode in @(1,0,1,0)){
  $entry.ValueCache['']=$mode
  $animation=$asm.GetType('PCL.ModAnimation');$enabled=$animation.GetProperty('AniControlEnabled',$flags);$previous=$enabled.GetValue($null,$null);$enabled.SetValue($null,($previous+1),$null)
  try{$service.GetMethod('RefreshSettings').Invoke($null,@($page))|Out-Null}finally{$enabled.SetValue($null,$previous,$null)}
  $render.Invoke($null,@($hostPanel,$image))|Out-Null
  $refreshUi.Invoke($page,@(($mode -eq 0),0))|Out-Null
  if($mode -eq 1){
   if($image.Visibility -ne 'Collapsed' -or $hostPanel.Background -isnot [Windows.Media.SolidColorBrush] -or $hostPanel.Background.Color -ne [Windows.Media.Colors]::White){throw 'Plain mode did not hide the global image and render white'}
   if($page.FindName('PanBackgroundOpacity').Visibility -ne 'Collapsed' -or $page.FindName('CheckBackgroundColorful').Visibility -ne 'Collapsed'){throw 'Plain mode exposes inactive image settings'}
  }else{
   if($image.Visibility -ne 'Visible' -or $hostPanel.Background -isnot [Windows.Media.LinearGradientBrush]){throw 'Switching back did not restore the original background'}
   if($page.FindName('PanBackgroundOpacity').Visibility -ne 'Visible'){throw 'Switching back lost image controls'}
  }
  if($settings['UiBackgroundOpacity'].ValueCache[''] -ne $savedOpacity -or $settings['UiBackgroundColorful'].ValueCache[''] -ne $savedColorful -or -not [object]::ReferenceEquals($image.Background,$savedImage)){throw 'Switching modes overwrote original background preferences or image'}
  if($PreviewDirectory){
   [IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null
   foreach($name in @('CardLauncher','CardBackground','CardMusic')){$null=$page.FindName($name).MainTextBlock}
   $hostPanel.Measure([Windows.Size]::new(800,660));$hostPanel.Arrange([Windows.Rect]::new(0,0,800,660));$hostPanel.UpdateLayout()
   $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(800,660,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($hostPanel)
   $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
   $stream=[IO.File]::Create((Join-Path $PreviewDirectory ('background-mode-'+$mode+'.png')));try{$encoder.Save($stream)}finally{$stream.Dispose()}
  }
 }
 $entry.ValueCache['']=1;$settings['UiBackgroundColorful'].ValueCache['']=$false;$render.Invoke($null,@($hostPanel,$image))|Out-Null
 if($hostPanel.Background.Color -ne [Windows.Media.Colors]::White){throw 'Plain mode depends on the colorful-background preference'}
 $entry.ValueCache['']=0;$render.Invoke($null,@($hostPanel,$image))|Out-Null
 if($hostPanel.Background.Color -ne [Windows.Media.Color]::FromRgb(245,245,245) -or $image.Visibility -ne 'Visible'){throw 'Existing non-colorful mode changed'}
 $settings['UiLauncherColorMode'].ValueCache['']=1
 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 foreach($mode in @(1,0,1,0)){
  $entry.ValueCache['']=$mode;$render.Invoke($null,@($hostPanel,$image))|Out-Null
  if($hostPanel.Background.Color.R -gt 80 -or $image.Visibility.ToString() -ne $(if($mode -eq 1){'Collapsed'}else{'Visible'})){throw 'Dark plain/image background retained a light surface or lost its saved mode'}
  if([Math]::Abs($image.Opacity-([double]$savedOpacity/1000*0.32)) -gt 0.001 -or $settings['UiBackgroundOpacity'].ValueCache[''] -ne $savedOpacity -or -not [object]::ReferenceEquals($image.Background,$savedImage)){throw 'Dark image dimming changed the original opacity preference or picture'}
 }
 $settings['UiLauncherColorMode'].ValueCache['']=0;$asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $render.Invoke($null,@($hostPanel,$image))|Out-Null
 if([Math]::Abs($image.Opacity-([double]$savedOpacity/1000)) -gt 0.001){throw 'Returning to light mode did not restore the saved picture opacity'}
 $theme=$asm.GetType('PCL.ModTheme');$builtIn=[Windows.Controls.Canvas]::new();$entry.ValueCache['']=0
 $theme.GetMethod('UseDefaultBackground',$flags).Invoke($null,@($builtIn))|Out-Null
 $day=$builtIn.Background.ImageSource;$builtIn.Background.Stretch='Fill';$builtIn.Background.AlignmentX='Left'
 if($day.PixelWidth -lt 1000 -or -not $theme.GetMethod('DefaultBackgroundResource',$flags).Invoke($null,@()).EndsWith('/DefaultBackground.png')){throw 'Built-in day background did not load as an embedded image'}
 $settings['UiLauncherColorMode'].ValueCache['']=1;$asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $render.Invoke($null,@($hostPanel,$builtIn))|Out-Null
 if([object]::ReferenceEquals($builtIn.Background.ImageSource,$day) -or $builtIn.Background.ImageSource.PixelWidth -lt 1000 -or -not $theme.GetMethod('DefaultBackgroundResource',$flags).Invoke($null,@()).EndsWith('/DefaultBackgroundNight.png')){throw 'Dark theme did not swap the built-in illustration to the night variant'}
 if([Math]::Abs($builtIn.Opacity-([double]$savedOpacity/1000)) -gt 0.001 -or $builtIn.Background.Stretch -ne 'Fill' -or $builtIn.Background.AlignmentX -ne 'Left'){throw 'The night artwork was over-dimmed or lost its saved image layout'}
 $settings['UiLauncherColorMode'].ValueCache['']=0;$asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $render.Invoke($null,@($hostPanel,$builtIn))|Out-Null
 if(-not $theme.GetMethod('DefaultBackgroundResource',$flags).Invoke($null,@()).EndsWith('/DefaultBackground.png')){throw 'Returning to light theme did not restore the original day illustration'}
 $theme.GetMethod('MarkCustomBackground',$flags).Invoke($null,@($builtIn))|Out-Null;$builtIn.Background=$savedImage
 $settings['UiLauncherColorMode'].ValueCache['']=1;$asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null;$render.Invoke($null,@($hostPanel,$builtIn))|Out-Null
 if(-not [object]::ReferenceEquals($builtIn.Background,$savedImage)){throw 'Theme switching replaced a custom background image'}
 Write-Output 'PASS: global plain/image modes, preserved custom image preferences, embedded day/night artwork switching, retained layout and proper night exposure.'
}finally{$app.Shutdown()}
