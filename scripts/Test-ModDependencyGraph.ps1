param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$PreviewDirectory,[string]$ModFile,[string]$PlatformSnapshot)
# Native WPF controls and isolated metadata; optional real JARs are read only. No launcher entry point or HTTP.
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'Use powershell -NoProfile -STA.'}
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.IO.Compression,System.IO.Compression.FileSystem
foreach($p in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $root $p))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath));$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true);$app.InitializeComponent()
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
[Windows.Application]::ResourceAssembly=$asm
$entries=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null);foreach($entry in $entries.Values){$entry.ValueCache['']=$entry.DefaultValue};$entries['Identify'].ValueCache['']='dependency-graph-fixture'
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclDependencyGraph-'+[Guid]::NewGuid().ToString('N'))
$graphType=$asm.GetType('PCL.ModDependencyGraphView',$true);$inspector=$asm.GetType('PCL.ModDependencyInspector',$true)
function Jar([string]$id,[string]$name,[string]$version,[hashtable]$extra=@{}){
 $path=Join-Path $fixture ($id+'.jar');$zip=[IO.Compression.ZipFile]::Open($path,[IO.Compression.ZipArchiveMode]::Create)
 try{
  $data=@{schemaVersion=1;id=$id;name=$name;version=$version};foreach($key in $extra.Keys){$data[$key]=$extra[$key]}
  $w=[IO.StreamWriter]::new($zip.CreateEntry('fabric.mod.json').Open());try{$w.Write(($data|ConvertTo-Json -Depth 12))}finally{$w.Dispose()}
  if($data.icon -eq 'icon.png'){
   $drawing=[Windows.Media.DrawingVisual]::new();$context=$drawing.RenderOpen();$context.DrawRectangle([Windows.Media.Brushes]::Orange,$null,[Windows.Rect]::new(0,0,32,32));$context.Close()
   $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(32,32,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($drawing)
   $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$encoded=[IO.MemoryStream]::new();$output=$zip.CreateEntry('icon.png').Open();try{$encoder.Save($encoded);$encoded.Position=0;$encoded.CopyTo($output)}finally{$output.Dispose();$encoded.Dispose()}
  }
 }finally{$zip.Dispose()}
 return $path
}
function Scan([string]$folder){
 $context=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionContext'))
 $context.MinecraftVersion='1.21.11';$context.LoaderKnown=$true;$context.LoaderVersions.Add('fabricloader','0.19.3')
 return $inspector.GetMethod('Inspect',$flags).Invoke($null,@([string]$folder,$context.PSObject.BaseObject,[Threading.CancellationToken]::None))
}
function Graph($report,[string]$file){$ctor=$graphType.GetConstructors($flags)|Where-Object {-not $_.IsStatic -and $_.GetParameters().Count -eq 2}|Select-Object -First 1;return $ctor.Invoke(@($report.PSObject.BaseObject,[string]$file))}
function Canvas($graph){return $graphType.GetField('GraphCanvas',$flags).GetValue($graph)}
function Nodes($graph){return @((Canvas $graph).Children|Where-Object {$_ -is [Windows.Controls.Border]})}
function Render($element,[string]$path,[int]$width=1000,[int]$height=690){
 $previewHost=[Windows.Controls.Grid]::new();$previewHost.Background=[Windows.Media.BrushConverter]::new().ConvertFromString('#e7effb');$previewHost.Children.Add($element)|Out-Null
 $previewHost.Measure([Windows.Size]::new($width,$height));$previewHost.Arrange([Windows.Rect]::new(0,0,$width,$height));$previewHost.UpdateLayout()
 $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($width,$height,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($previewHost)
 $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$output=[IO.File]::Create($path)
 try{$encoder.Save($output)}finally{$output.Dispose()}
}
try{
 [IO.Directory]::CreateDirectory($fixture)|Out-Null
 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 $selected=Jar 'extra' 'Selected mod' '1.0' @{provides=@('extra-alias');depends=@{minecraft='>=1.21';fabricloader='>=0.18';renderer='>=1.0'};recommends=@{recommendation='*'};breaks=@{old_iris='<2.0';modern_iris='<1.0'}}
 $null=Jar 'renderer' 'Renderer' '1.2' @{icon='icon.png'};$null=Jar 'old_iris' 'Old Iris' '1.0';$null=Jar 'modern_iris' 'Modern Iris' '3.0'
 $null=Jar 'consumer' 'Consumer' '1.0' @{depends=@{extra='*';'extra-alias'='*'}}
 $null=Jar 'optional_consumer' 'Optional consumer' '1.0' @{suggests=@{extra='*'}}
 $null=Jar 'reverse_conflict' 'Reverse conflict' '1.0' @{breaks=@{extra='*'}}
 $null=Jar 'unrelated_renderer_user' 'Unrelated renderer user' '1.0' @{depends=@{renderer='*'}}
 $configJar=Jar 'recommendation' 'Config library' '1.1'
 $report=Scan $fixture
 $focusMod=$report.Mods|Where-Object Id -eq 'extra'|Select-Object -First 1
 $loaderRequirement=$focusMod.Dependencies|Where-Object Id -eq 'fabricloader'|Select-Object -First 1
 $focusMod.Dependencies.Add($loaderRequirement)
 $otherRange=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$otherRange.Id='fabricloader';$otherRange.Versions=[Newtonsoft.Json.Linq.JValue]::new('>=0.19')
 $focusMod.Dependencies.Add($otherRange)
 $bypassed=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$bypassed.Id='modmenu'
 $unless=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$unless.Id='renderer';$bypassed.Unless=$unless;$focusMod.Dependencies.Add($bypassed)
 $conditionalOwner=$report.Mods|Where-Object Id -eq 'optional_consumer'|Select-Object -First 1
 $conditional=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$conditional.Id='extra';$conditional.Unless=$unless;$conditionalOwner.Dependencies.Add($conditional)
 $internal=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$internal.Id='extra-alias';$focusMod.Dependencies.Add($internal)
 # Bundled modules may be inactive nested candidates, with no evaluated dependency edge.
 # An outer installed JAR wins over a bundled copy of the same ID. The copy is not a reverse dependency.
 $shadow=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+ModInfo'));$shadow.Id='renderer';$shadow.Name='Bundled renderer fallback';$shadow.Version='0.1';$shadow.Format='Fabric';$shadow.IsNested=$true;$shadow.Enabled=$true;$shadow.RootPath=$selected;$shadow.MetadataPath='nested/shadow';$report.Mods.Add($shadow)
 foreach($format in @('Fabric','Quilt','Forge','NeoForge')){
  $nested=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+ModInfo'))
  $nested.Id='internal_'+$format.ToLowerInvariant();$nested.Name='Internal '+$format+' component';$nested.Version='2.0';$nested.Format=$format;$nested.IsNested=$true;$nested.Enabled=$true
  $nested.RootPath=Join-Path $fixture 'renderer.jar';$nested.MetadataPath='nested/'+$format;$report.Mods.Add($nested)
  $requirement=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$requirement.Id=$nested.Id;$focusMod.Dependencies.Add($requirement)
 }
 $report.Edges.Clear();$report.Issues.Clear();$report.ReverseDependencies.Clear()
 $inspector.GetMethod('BuildReport',$flags).Invoke($null,@($report,[Threading.CancellationToken]::None))|Out-Null
 $graph=Graph $report ('\\?\'+$selected);$nodes=Nodes $graph
 $titles=@($nodes|ForEach-Object {$_.Tag.Title})
 foreach($title in @('Selected mod','Minecraft','Fabric Loader','Renderer','Consumer','Old Iris','Reverse conflict')){if($title -notin $titles){throw ('Missing graph relation: '+$title)}}
 if('Unrelated renderer user' -in $titles){throw 'An unselected bundled copy was mistaken for the real dependency provider'}
 if($nodes.Count -ne 7 -or @($titles|Where-Object {$_ -eq 'Consumer'}).Count -ne 1){throw 'Graph duplicates a referring file or includes optional/unmatched relations'}
 $containerNodes=@($nodes|Where-Object {$_.Tag.Title -eq 'Renderer'})
 if($containerNodes.Count -ne 1 -or @($titles|Where-Object {$_ -like 'Internal * component'}).Count){throw 'Bundled dependencies were not collapsed to their installed container JAR'}
 foreach($format in @('fabric','quilt','forge','neoforge')){if(-not $containerNodes[0].Tag.Details.Contains('internal_'+$format)){throw 'Container grouping discarded an internal dependency condition'}}
 $graphType.GetMethod('LoadIconsForPreview',$flags).Invoke($graph,@())|Out-Null
 $icon=$containerNodes[0].Child.Children[0].Source
 if($null -eq $icon -or -not $icon.IsFrozen -or $icon.PixelWidth -ne 32){throw 'Local container icon did not decode to a frozen thumbnail'}
 $iconReader=$asm.GetType('PCL.ModDependencyIcon').GetMethod('Read',$flags)
 if($null -ne $iconReader.Invoke($null,@([string](Join-Path $fixture 'renderer.jar'),'../icon.png')) -or $null -ne $iconReader.Invoke($null,@([string](Join-Path $fixture 'renderer.jar'),'fabric.mod.json'))){throw 'Unsafe or invalid icon content did not fall back safely'}
 if(@($nodes|Where-Object {$_.Tag.IsConflict}).Count -ne 2){throw 'Graph lost a real conflict or rendered an unmatched version limit as a conflict'}
 $loaderNode=@($nodes|Where-Object {$_.Tag.Title -eq 'Fabric Loader'})
 if($loaderNode.Count -ne 1 -or -not $loaderNode[0].Tag.Condition.Contains('>=0.18') -or -not $loaderNode[0].Tag.Condition.Contains('>=0.19')){throw 'Repeated dependencies were not merged with all distinct constraints preserved'}
 $center=$nodes|Where-Object {$_.Tag.Title -eq 'Selected mod'}|Select-Object -First 1
 foreach($box in $nodes){if($box.Tag.IsConflict -and ([Windows.Controls.Canvas]::GetLeft($box) -ne [Windows.Controls.Canvas]::GetLeft($center) -or [Windows.Controls.Canvas]::GetTop($box) -le [Windows.Controls.Canvas]::GetTop($center)+$center.Height)){throw 'Conflict nodes are not below the selected mod'}}
 $canvas=Canvas $graph
 if(@($canvas.Children|Where-Object {$_ -is [Windows.Shapes.Path]}).Count -ne 6 -or @($canvas.Children|Where-Object {$_ -is [Windows.Shapes.Polygon]}).Count -ne 6){throw 'Graph arrows do not match direct relations'}
 foreach($box in $nodes){$x=[Windows.Controls.Canvas]::GetLeft($box);$y=[Windows.Controls.Canvas]::GetTop($box);if($x -lt 0 -or $y -lt 0 -or $x+$box.Width -gt $canvas.Width -or $y+$box.Height -gt $canvas.Height){throw 'A graph node lies outside the scrollable canvas'}}
 for($i=0;$i -lt $nodes.Count;$i++){for($j=$i+1;$j -lt $nodes.Count;$j++){
  $a=[Windows.Rect]::new([Windows.Controls.Canvas]::GetLeft($nodes[$i]),[Windows.Controls.Canvas]::GetTop($nodes[$i]),$nodes[$i].Width,$nodes[$i].Height)
  $b=[Windows.Rect]::new([Windows.Controls.Canvas]::GetLeft($nodes[$j]),[Windows.Controls.Canvas]::GetTop($nodes[$j]),$nodes[$j].Width,$nodes[$j].Height)
  if($a.IntersectsWith($b)){throw 'Dependency graph nodes overlap'}
 }}
 # A platform-required dependency can be only recommended in local metadata; keep provenance and startup semantics.
 $localErrors=$report.Issues.Count;$localMods=$report.Mods.Count
 $localOnly=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$localOnly.Id='local_only';$focusMod.Dependencies.Add($localOnly)
 $onlineFile=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineFileInfo'));$onlineFile.FullPath=$selected;$onlineFile.Enabled=$true
 $match=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineProjectInfo'));$match.Platform='Modrinth';$match.ProjectId='ExactOwner';$match.IsExact=$true
 foreach($pair in @(@('ConfigP','Config library',$configJar),@('RendererP','Renderer',(Join-Path $fixture 'renderer.jar')))){
  $dep=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineDependency'));$dep.Platform='Modrinth';$dep.Kind='required';$dep.ProjectId=$pair[0];$dep.ProjectName=$pair[1];$dep.EnabledPaths.Add($pair[2]);$match.Dependencies.Add($dep)
 }
 $guess=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineProjectInfo'));$guess.IsExact=$false
 $dep=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineDependency'));$dep.Kind='required';$dep.ProjectName='Filename guess';$guess.Dependencies.Add($dep)
 $onlineFile.Matches.Add($match);$onlineFile.Matches.Add($guess);$report.OnlineFiles.Add($onlineFile);$report.OnlineAttempted=$true
 $platformNodes=Nodes (Graph $report $selected)
 if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'Config library' -and $_.Tag.IsPlatformRequirement}).Count -ne 1 -or @($platformNodes|Where-Object {$_.Tag.Title -eq 'Renderer'}).Count -ne 1 -or @($platformNodes|Where-Object {$_.Tag.Title -eq 'Filename guess'}).Count){throw 'Exact platform requirements were omitted, duplicated or confused with filename guesses'}
 if($report.Issues.Count -ne $localErrors -or $report.Mods.Count -ne $localMods){throw 'Platform graph supplementation changed local startup checks'}
 $oldOnline=$report.OnlineFiles;$report.OnlineFiles=[Activator]::CreateInstance($report.GetType().GetField('OnlineFiles',$flags).FieldType)
 $fallbackNodes=Nodes (Graph $report $selected)
 if(@($fallbackNodes|Where-Object {$_.Tag.Title -eq 'local_only'}).Count -ne 1){throw 'No exact platform declaration did not enable local reference fallback'}
 $report.OnlineFiles=$oldOnline
 if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'local_only'}).Count){throw 'A local-only mod requirement overrode the exact platform declaration'}
 if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'Fabric Loader' -or $_.Tag.Title -eq 'Minecraft'}).Count){throw 'Successful platform lookup still displayed local environment references'}
 $report.OnlineTimedOut=$true
 $timeoutNodes=Nodes (Graph $report $selected)
 if(@($timeoutNodes|Where-Object {$_.Tag.Title -eq 'local_only'}).Count -ne 1){throw 'Network timeout did not restore local reference declarations'}
 $report.OnlineTimedOut=$false
 $platformPeer=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineFileInfo'));$platformPeer.FullPath=Join-Path $fixture 'consumer.jar';$platformPeer.Enabled=$true
 $peerMatch=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineProjectInfo'));$peerMatch.IsExact=$true;$platformPeer.Matches.Add($peerMatch);$report.OnlineFiles.Add($platformPeer)
 $platformNodes=Nodes (Graph $report $selected)
 if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'Consumer'}).Count){throw 'Local reverse requirements overrode an exact platform declaration'}
 $conflict=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineDependency'));$conflict.Platform='Modrinth';$conflict.Kind='incompatible';$conflict.ProjectName='Modern Iris';$conflict.EnabledPaths.Add((Join-Path $fixture 'modern_iris.jar'));$match.Dependencies.Add($conflict)
 $platformNodes=Nodes (Graph $report $selected)
 if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'Modern Iris' -and $_.Tag.IsConflict}).Count -ne 1){throw 'Exact platform conflict was omitted'}
 # Both sources refer to the same disabled JAR: one attention card, both source declarations retained.
 $disabledPath=Join-Path $fixture 'disabled_config.jar.disabled';$disabledMod=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+ModInfo'));$disabledMod.Id='disabled_config';$disabledMod.Name='Disabled config';$disabledMod.RootPath=$disabledPath;$disabledMod.Enabled=$false;$report.Mods.Add($disabledMod)
 foreach($platform in @('Modrinth','CurseForge')){$d=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+OnlineDependency'));$d.Platform=$platform;$d.Kind='required';$d.ProjectId=$platform+'Config';$d.ProjectName='Disabled config';$d.MatchedPaths.Add($disabledPath);$match.Dependencies.Add($d)}
 $attention=@((Nodes (Graph $report $selected))|Where-Object {$_.Tag.Title -eq 'Disabled config'})
 if($attention.Count -ne 1 -or -not $attention[0].Tag.NeedsAttention -or -not $attention[0].Tag.Details.Contains('Modrinth') -or -not $attention[0].Tag.Details.Contains('CurseForge')){throw 'Disabled dual-platform declarations were not merged and highlighted'}
 # Two bundles carry a selectable copy of the same library. Neither bundle requires the other.
 $bundles=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionReport'));$bundles.Context=$report.Context
 foreach($name in @('api_bundle','menu_bundle')){
  $outer=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+ModInfo'));$outer.Id=$name;$outer.Name=$name;$outer.RootPath=Join-Path $fixture ($name+'.jar');$outer.MetadataPath='fabric.mod.json';$outer.Version='1';$outer.Enabled=$true
  $library=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+ModInfo'));$library.Id='shared_api';$library.Name='Bundled API';$library.RootPath=$outer.RootPath;$library.MetadataPath='nested/library';$library.Version='1';$library.Enabled=$true;$library.IsNested=$true
  $requires=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$requires.Id='shared_api';$outer.Dependencies.Add($requires)
  $bundles.Mods.Add($outer);$bundles.Mods.Add($library)
 }
 $inspector.GetMethod('BuildReport',$flags).Invoke($null,@($bundles,[Threading.CancellationToken]::None))|Out-Null
 foreach($name in @('api_bundle','menu_bundle')){
  $isolated=Graph $bundles (Join-Path $fixture ($name+'.jar'))
  if((Nodes $isolated).Count -ne 1){throw 'Shared bundled candidates created a false API-to-mod or mod-to-API dependency'}
 }
 $external=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+ModInfo'));$external.Id='external_consumer';$external.Name='External consumer';$external.RootPath=Join-Path $fixture 'external.jar';$external.MetadataPath='fabric.mod.json';$external.Version='1';$external.Enabled=$true
 $requires=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$requires.Id='shared_api';$external.Dependencies.Add($requires);$bundles.Mods.Add($external)
 $bundles.Edges.Clear();$bundles.Issues.Clear();$bundles.ReverseDependencies.Clear();$inspector.GetMethod('BuildReport',$flags).Invoke($null,@($bundles,[Threading.CancellationToken]::None))|Out-Null
 $ambiguous=Nodes (Graph $bundles $external.RootPath);$choice=@($ambiguous|Where-Object {$_.Tag.Identity -like 'choice:*'})
 if($choice.Count -ne 1 -or -not $choice[0].Tag.Title.Contains('api_bundle') -or -not $choice[0].Tag.Title.Contains('menu_bundle')){throw 'Ambiguous external providers were incorrectly resolved to the first candidate'}
 $providerView=Nodes (Graph $bundles (Join-Path $fixture 'api_bundle.jar'))
 $possible=@($providerView|Where-Object {$_.Tag.Title -eq 'External consumer'})
 if($possible.Count -ne 1 -or -not $possible[0].Tag.IsCandidate){throw 'Possible incoming relation disappeared instead of being identified as a candidate'}
 # Larger than the old per-group display cap; names must fit without ellipsis or clipped last rows.
 $large=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+InspectionReport'));$large.Context=$report.Context
 function Model([string]$id,[string]$name){$m=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+ModInfo'));$m.Id=$id;$m.Name=$name;$m.Version='1';$m.Enabled=$true;$m.RootPath=Join-Path $fixture ($id+'.jar');$m.MetadataPath='fabric.mod.json';return $m}
 function Required([string]$id){$d=[Activator]::CreateInstance($asm.GetType('PCL.ModDependencyInspector+Dependency'));$d.Id=$id;return $d}
 $longName='A very long installed mod name that must wrap completely rather than disappear behind an ellipsis'
 $largeFocus=Model 'large_focus' $longName;$large.Mods.Add($largeFocus)
 for($i=0;$i -lt 40;$i++){$m=Model ('need_'+$i) ('Needed '+$i);$large.Mods.Add($m);$largeFocus.Dependencies.Add((Required $m.Id))}
 for($i=0;$i -lt 32;$i++){$m=Model ('consumer_'+$i) ('Consumer '+$i);$m.Dependencies.Add((Required $largeFocus.Id));$large.Mods.Add($m)}
 for($i=0;$i -lt 27;$i++){$m=Model ('conflict_'+$i) ('Conflict '+$i);$large.Mods.Add($m);$d=Required $m.Id;$d.Kind=[Enum]::Parse($asm.GetType('PCL.ModDependencyInspector+DependencyKind'),'Incompatible');$largeFocus.Dependencies.Add($d)}
 $inspector.GetMethod('BuildReport',$flags).Invoke($null,@($large,[Threading.CancellationToken]::None))|Out-Null
 $largeGraph=Graph $large $largeFocus.RootPath;$largeNodes=Nodes $largeGraph
 if($largeNodes.Count -ne 100){throw ('Relationship groups were truncated: expected 100 nodes, got '+$largeNodes.Count)}
 $largeGraph.Measure([Windows.Size]::new(720,480));$largeGraph.Arrange([Windows.Rect]::new(0,0,720,480));$largeGraph.UpdateLayout()
 $longBox=$largeNodes|Where-Object {$_.Tag.Title -eq $longName}|Select-Object -First 1
 $longTitle=$longBox.Child.Children[1].Children[0]
 if($longTitle.ActualHeight -le 25 -or $longTitle.DesiredSize.Height -gt $longBox.ActualHeight-8){throw 'A long node title is not fully laid out inside its box'}
 $largeScroll=$largeGraph.Children[0];$largeScroll.ScrollToBottom();$largeGraph.UpdateLayout()
 $last=$largeNodes|Where-Object {$_.Tag.Title -eq 'Needed 39'}|Select-Object -First 1
 if([Windows.Controls.Canvas]::GetTop($last)+$last.Height -gt $largeScroll.VerticalOffset+$largeScroll.ViewportHeight+1){throw 'The last required node cannot be reached by scrolling'}
 if($ModFile){$report=Scan ([IO.Path]::GetDirectoryName($ModFile));$selected=$ModFile}
 $impactType=$asm.GetType('PCL.ModDependencyImpactView',$true)
 $impactItems=$impactType.GetMethod('Collect',$flags).Invoke($null,@($large,[string[]]@((Join-Path $fixture 'large_focus.jar'))))
 if($impactItems.Count -ne 32){throw 'Impact cards lost affected mods beyond the old text limit'}
 $impactView=$impactType.GetConstructors($flags)|Where-Object {-not $_.IsStatic}|Select-Object -First 1
 $impactView=$impactView.Invoke(@($impactItems,'disable'))
 $cards=$impactType.GetField('Cards',$flags).GetValue($impactView)
 if($cards.ColumnDefinitions.Count -ne 2 -or $cards.Children.Count -ne 32){throw 'Impact warning is not a complete two-column card layout'}
 for($i=0;$i -lt $cards.Children.Count;$i++){if([Windows.Controls.Grid]::GetColumn($cards.Children[$i]) -ne ($i%2) -or $cards.Children[$i].BorderThickness.Left -le 0){throw 'Impact cards lost their border or two-column placement'}}
 if($PreviewDirectory -and $ModFile){
  [IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null
  $actualImpacts=$impactType.GetMethod('Collect',$flags).Invoke($null,@($report,[string[]]@($selected)))
  $impactConverter=[Activator]::CreateInstance($asm.GetType('PCL.ModMain+MyMsgBoxConverter'))
  $impactConverter.Title=-join ([char[]]@(0x6A21,0x7EC4,0x4F9D,0x8D56,0x5F71,0x54CD));$impactConverter.Text='Impact preview';$impactConverter.IsWarn=$true
  $impactConverter.Button1=-join ([char[]]@(0x7EE7,0x7EED,0x7981,0x7528));$impactConverter.Button2=-join ([char[]]@(0x53D6,0x6D88))
  $actionName=-join ([char[]]@(0x7981,0x7528));$impactCtor=$impactType.GetConstructors($flags)|Where-Object {-not $_.IsStatic}|Select-Object -First 1
  $impactFactory={$impactCtor.Invoke(@($actualImpacts.PSObject.BaseObject,[string]$actionName))}.GetNewClosure();$impactConverter.ContentFactory=[Func[Windows.FrameworkElement]]$impactFactory
  $impactModal=[Activator]::CreateInstance($asm.GetType('PCL.MyMsgText'),@($impactConverter));$impactModal.FindName('TransformRotate').Angle=0;$impactModal.FindName('TransformPos').Y=0
  if($impactModal.FindName('Btn1').ColorType.ToString() -ne 'Red' -or $impactModal.FindName('Btn2').Text -ne $impactConverter.Button2 -or $impactConverter.Result){throw 'Impact confirmation lost warning/cancel controls or implicitly confirmed'}
  Render $impactModal (Join-Path $PreviewDirectory 'dependency-impact.png') 830 670
 }
 if($PlatformSnapshot){
  $listType=$report.GetType().GetField('OnlineFiles',$flags).FieldType
  $report.OnlineFiles=[Newtonsoft.Json.JsonConvert]::DeserializeObject([IO.File]::ReadAllText([IO.Path]::GetFullPath($PlatformSnapshot)),$listType)
  $report.OnlineAttempted=$true
 }
 $converter=[Activator]::CreateInstance($asm.GetType('PCL.ModMain+MyMsgBoxConverter'))
 $converter.Title=-join ([char[]]@(0x6A21,0x7EC4,0x4F9D,0x8D56,0x5173,0x7CFB));$converter.Text=$report.DescribeFile($selected)
 $graphCtor=$graphType.GetConstructors($flags)|Where-Object {-not $_.IsStatic -and $_.GetParameters().Count -eq 2}|Select-Object -First 1
 $factoryBlock={$graphCtor.Invoke(@($report.PSObject.BaseObject,[string]$selected))}.GetNewClosure()
 $factory=[Func[Windows.FrameworkElement]]$factoryBlock
 $converter.ContentFactory=$factory
 $modal=[Activator]::CreateInstance($asm.GetType('PCL.MyMsgText'),@($converter))
 if($modal.FindName('PanCaption').Content.GetType() -ne $graphType){throw 'Native message dialog did not install graph content'}
 if($modal.FindName('Btn1').Text -ne $converter.Button1){throw 'Native message dialog lost its normal close button'}
 # Render the resting dialog, rather than its initial rotated entrance-animation pose.
 $modal.FindName('TransformRotate').Angle=0;$modal.FindName('TransformPos').Y=0
 $graphType.GetMethod('LoadIconsForPreview',$flags).Invoke($modal.FindName('PanCaption').Content,@())|Out-Null
 if($PreviewDirectory){[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null;Render $modal (Join-Path $PreviewDirectory 'dependency-graph.png') ([int]$modal.Width+70) ([int]$modal.FindName('PanCaption').Content.Height+220)}
 # Inject only the modal host field; never run FormMain's constructor or launcher initialization.
 $mainField=$asm.GetType('PCL.ModMain').GetField('FrmMain',$flags);$oldMain=$mainField.GetValue($null)
 try{
  if($modal.Parent){$modal.Parent.Children.Remove($modal)}
  $smallHost=[Windows.Controls.Grid]::new();$smallHost.Measure([Windows.Size]::new(520,420));$smallHost.Arrange([Windows.Rect]::new(0,0,520,420))
  $shell=[Runtime.Serialization.FormatterServices]::GetUninitializedObject($asm.GetType('PCL.FormMain'))
  $backButton=[Runtime.Serialization.FormatterServices]::GetUninitializedObject($asm.GetType('PCL.MyExtraButton'))
  $shell.GetType().GetProperty('BtnExtraBack',$flags).SetValue($shell,$backButton,$null)
  $shell.GetType().GetProperty('PanMsg',$flags).SetValue($shell,$smallHost,$null);$mainField.SetValue($null,$shell)
  $modal.GetType().GetMethod('RefreshContentBounds',$flags).Invoke($modal,@())|Out-Null
  $smallHost.Children.Add($modal)|Out-Null;$smallHost.Measure([Windows.Size]::new(520,420));$smallHost.Arrange([Windows.Rect]::new(0,0,520,420));$smallHost.UpdateLayout()
  $button=$modal.FindName('Btn1');$position=$button.TransformToAncestor($modal).Transform([Windows.Point]::new(0,0))
  # Allow one DIP for WPF layout rounding at the machine's display scaling.
  if($modal.ActualWidth -gt 471 -or $modal.ActualHeight -gt 371 -or $position.Y+$button.ActualHeight -gt $modal.ActualHeight+1){throw ("Small-window dialog bounds: size="+$modal.ActualWidth+','+$modal.ActualHeight+'; button bottom='+($position.Y+$button.ActualHeight))}
  $scroll=$modal.FindName('PanCaption').Content.Children[0]
  $extent=Canvas $modal.FindName('PanCaption').Content
  if(($extent.Width -gt $scroll.ViewportWidth+1 -and $scroll.ScrollableWidth -le 0) -or ($extent.Height -gt $scroll.ViewportHeight+1 -and $scroll.ScrollableHeight -le 0)){throw 'Small-window graph cannot scroll to all nodes'}
 }finally{$mainField.SetValue($null,$oldMain)}
 Write-Output 'Dependency graph passed: loader/container ownership, required/candidate relations, all 100 long-list nodes reachable, wrapped text, frozen local icons and safe fallback, matched conflicts and native dialog bounds.'
}finally{
 $app.Shutdown();$full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '\APclDependencyGraph-[a-f0-9]{32}\z'){throw 'Unsafe graph fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
