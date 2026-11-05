param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$PreviewDirectory)
# Offscreen PCL controls and GUID temporary files only; never start the launcher or touch game directories.
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'Use powershell -NoProfile -STA.'}
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.IO.Compression,System.IO.Compression.FileSystem
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath))
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true)
$app.InitializeComponent()
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
[Windows.Application]::ResourceAssembly=$asm
$entries=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null)
foreach($entry in $entries.Values){$entry.ValueCache['']=$entry.DefaultValue}
$entries['Identify'].ValueCache['']='synthetic-resource-view'
$store=$asm.GetType('PCL.InstanceResourceStore',$true)
$kindType=$asm.GetType('PCL.InstanceResourceKind',$true)
$none=[Threading.CancellationToken]::None
function Invoke-Store($name,[object[]]$arguments){
 $values=New-Object object[] $arguments.Length
 for($i=0;$i -lt $values.Length;$i++){if($null -ne $arguments[$i]){$values[$i]=$arguments[$i].PSObject.BaseObject}}
 return ,$store.GetMethod($name,$flags).Invoke($null,$values)
}
function Kind([string]$name){return [Enum]::Parse($kindType,$name)}
function Put([string]$path,[string]$text){[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))|Out-Null;[IO.File]::WriteAllText($path,$text)}
function Assert-Rejected([scriptblock]$action,[string]$message){$rejected=$false;try{& $action|Out-Null}catch{$rejected=$true};if(-not $rejected){throw $message}}
function New-Zip([string]$path,[hashtable]$items){
 $archive=[IO.Compression.ZipFile]::Open($path,[IO.Compression.ZipArchiveMode]::Create)
 try{foreach($name in $items.Keys){$writer=[IO.StreamWriter]::new($archive.CreateEntry($name).Open());try{$writer.Write($items[$name])}finally{$writer.Dispose()}}}finally{$archive.Dispose()}
}
function Write-Png([string]$path){
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))|Out-Null
 $visual=[Windows.Media.DrawingVisual]::new();$drawing=$visual.RenderOpen()
 $drawing.DrawRectangle([Windows.Media.Brushes]::CornflowerBlue,$null,[Windows.Rect]::new(0,0,96,64));$drawing.Close()
 $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(96,64,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($visual)
 $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $stream=[IO.File]::Create($path);try{$encoder.Save($stream)}finally{$stream.Dispose()}
}
function Scan([string]$folder,$kind){return ,(Invoke-Store 'Scan' @($folder,$folder,$kind,$none))}
function Render-Png($element,[string]$path,[int]$width,[int]$height){
 $element.Width=$width;$element.Height=$height
 $element.Measure([Windows.Size]::new($width,$height));$element.Arrange([Windows.Rect]::new(0,0,$width,$height));$element.UpdateLayout()
 $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($width,$height,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($element)
 $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $stream=[IO.File]::Create($path);try{$encoder.Save($stream)}finally{$stream.Dispose()}
}
# Only these two migrated page states may be backed by forwarding properties.
function Get-KeybindingPageStateMember([type]$pageType,[string]$name){
 if($pageType.FullName -cne 'PCL.PageInstanceKeybindings' -or $name -cnotin @('Entries','KeyButtons')){throw 'State hooks are limited to PageInstanceKeybindings Entries/KeyButtons'}
 $member=$pageType.GetField($name,$flags)
 if($null -eq $member){$member=$pageType.GetProperty($name,$flags)}
 if($null -eq $member){throw [MissingMemberException]::new($pageType.FullName,$name)}
 return $member
}
function Get-KeybindingPageStateType([type]$pageType,[string]$name){
 $member=Get-KeybindingPageStateMember $pageType $name
 if($member -is [Reflection.FieldInfo]){return $member.FieldType}
 return $member.PropertyType
}
function Get-KeybindingPageState($page,[string]$name){
 $target=$page.PSObject.BaseObject
 $member=Get-KeybindingPageStateMember ($target.GetType()) $name
 if($member -is [Reflection.FieldInfo]){return ,$member.GetValue($target)}
 return ,$member.GetValue($target,$null)
}
function Set-KeybindingPageState($page,[string]$name,$value){
 $target=$page.PSObject.BaseObject
 $member=Get-KeybindingPageStateMember ($target.GetType()) $name
 if($null -ne $value){$value=$value.PSObject.BaseObject}
 if($member -is [Reflection.FieldInfo]){$member.SetValue($target,$value)}else{$member.SetValue($target,$value,$null)}
}

# C#5 delegates exercise the real controller; native PCL cards/buttons are still checked below.
$controllerChecks = [IO.File]::ReadAllText((Join-Path $root 'tests/PCLLF.Windows.Tests/KeybindingControllerChecks.cs'))

$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclKeybindingController-'+[Guid]::NewGuid().ToString('N'))
try {
 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh').Invoke($null,@(0))|Out-Null
 $page=[Activator]::CreateInstance($asm.GetType('PCL.PageInstanceKeybindings'));$bindingType=$asm.GetType('PCL.InstanceKeybinding')
 $itemsType=Get-KeybindingPageStateType ($page.GetType()) 'Entries'
 if($itemsType -ne [Collections.Generic.List``1].MakeGenericType($bindingType)){throw 'Keybinding page Entries changed its concrete item type'}
 $items=[Activator]::CreateInstance($itemsType)
 foreach($sample in @(@('保存快捷栏','c',3),@('复制位置','c',3),@('调试崩溃','c',3),@('投影菜单','v',2),@('地图菜单','v',2),@('打开物品栏','e',1))){
  $item=[Activator]::CreateInstance($bindingType)
  foreach($pair in @(@('Key',('key_fixture.'+$items.Count)),@('Name',$sample[0]),@('Owners','Minecraft'),@('Value',('key.keyboard.'+$sample[1])),@('Normalized',('key.keyboard.'+$sample[1])),@('SharedCount',$sample[2]))){$bindingType.GetField($pair[0],$flags).SetValue($item,$pair[1])}
  $items.Add($item)
 }
 Set-KeybindingPageState $page 'Entries' $items
 $render=$page.GetType().GetMethod('RenderList',$flags);$render.Invoke($page,@())|Out-Null
 $all=$page.FindName('PanBindings')
 if($all.Children.Count -ne 6 -or @($all.Children|Where-Object {$_.GetType().Name -eq 'MyCard'}).Count){throw 'All bindings should remain a flat list in default order'}
 for($i=0;$i -lt $items.Count;$i++){if($all.Children[$i].Children[0].Children[0].Text -cne $bindingType.GetField('Name',$flags).GetValue($items[$i])){throw 'All-bindings order was changed'}}
 $page.FindName('BtnFilterConflicts').Checked=$true
 $list=$page.FindName('PanBindings');$groups=@($list.Children|Where-Object {$_.GetType().Name -eq 'MyCard'})
 if($groups.Count -ne 2 -or $list.Children.Count -ne 2){throw 'Shared keys were not grouped independently from single keys'}
 $c=$groups|Where-Object {$_.Title -like 'C*'}|Select-Object -First 1
 if($c.Title -notlike '*与 2 项共用*'){throw 'Shared-key group count is wrong'}
 $rows=$c.Children|Where-Object {$_ -is [Windows.Controls.StackPanel]}|Select-Object -First 1
 if($rows.Children.Count -ne 3 -or (Get-KeybindingPageState $page 'KeyButtons').Count -ne 5){throw 'Grouping lost binding rows or edit buttons'}
 foreach($row in $rows.Children){if($row.Children[1].GetType().Name -cne 'MyButton'){throw 'Grouped edit buttons must remain native MyButton controls'}}
 $page.FindName('SearchBox').Text='复制位置'
 $groups=@($list.Children|Where-Object {$_.GetType().Name -eq 'MyCard'})
 if($groups.Count -ne 1 -or $groups[0].Title -notlike '*与 2 项共用*'){throw 'Search lost shared-key context'}
 $page.FindName('SearchBox').Text='';$page.FindName('BtnFilterConflicts').Checked=$true
 if($list.Children.Count -ne 2){throw 'Shared-only filter left unshared rows'}
 $page.FindName('PanLoad').Visibility='Collapsed';$page.FindName('PanBack').Visibility='Visible';$page.FindName('PanBack').Opacity=1
 $panel=[Windows.Controls.Grid]::new();$panel.Background=[Windows.Media.BrushConverter]::new().ConvertFromString('#e4effa');$panel.Children.Add($page)|Out-Null
 if($PreviewDirectory){[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null;Render-Png $panel (Join-Path $PreviewDirectory 'keybinding-groups.png') 780 720}
 Add-Type -TypeDefinition $controllerChecks -ReferencedAssemblies @('System.dll','System.Core.dll','System.Xaml.dll',
  [Windows.FrameworkElement].Assembly.Location,[Windows.UIElement].Assembly.Location,
  [Windows.Point].Assembly.Location,[PCLCS.InstanceKeybindingData].Assembly.Location)
 try{[PclKeybindingControllerChecks]::Run($fixture)}catch{throw ($_.Exception.GetBaseException().ToString())}
 Write-Output 'Shared-key UI passed: native cards/buttons, title counts, complete rows, original order, search and conflict filtering; real C# controller passed Escape, clear/cancel clicks, captured mouse save, capture reset before conflict, rejection, modal owner switch, profile warning and actual options save failure checks.'
}finally {
 $app.Shutdown()
 $full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if($full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($full).StartsWith('PclKeybindingController-') -and [IO.Directory]::Exists($full)){Remove-Item -LiteralPath $full -Recurse -Force}
}
