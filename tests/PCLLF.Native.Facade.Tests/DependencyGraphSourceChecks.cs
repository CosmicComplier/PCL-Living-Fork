namespace PCLLF.Native.Facade.Tests;
// LF f4/main50 graph assertions; sources are not read at runtime.
internal static class DependencyGraphSourceChecks {
    private static readonly Dictionary<int,string> Original = new() {
        [80] = "foreach($title in @('Selected mod','Minecraft','Fabric Loader','Renderer','Consumer','Old Iris','Reverse conflict')){if($title -notin $titles){throw ('Missing graph relation: '+$title)}}",
        [81] = "if('Unrelated renderer user' -in $titles){throw 'An unselected bundled copy was mistaken for the real dependency provider'}",
        [82] = "if($nodes.Count -ne 7 -or @($titles|Where-Object {$_ -eq 'Consumer'}).Count -ne 1){throw 'Graph duplicates a referring file or includes optional/unmatched relations'}",
        [84] = "if($containerNodes.Count -ne 1 -or @($titles|Where-Object {$_ -like 'Internal * component'}).Count){throw 'Bundled dependencies were not collapsed to their installed container JAR'}",
        [85] = "foreach($format in @('fabric','quilt','forge','neoforge')){if(-not $containerNodes[0].Tag.Details.Contains('internal_'+$format)){throw 'Container grouping discarded an internal dependency condition'}}",
        [88] = "if($null -eq $icon -or -not $icon.IsFrozen -or $icon.PixelWidth -ne 32){throw 'Local container icon did not decode to a frozen thumbnail'}",
        [90] = "if($null -ne $iconReader.Invoke($null,@([string](Join-Path $fixture 'renderer.jar'),'../icon.png')) -or $null -ne $iconReader.Invoke($null,@([string](Join-Path $fixture 'renderer.jar'),'fabric.mod.json'))){throw 'Unsafe or invalid icon content did not fall back safely'}",
        [91] = "if(@($nodes|Where-Object {$_.Tag.IsConflict}).Count -ne 2){throw 'Graph lost a real conflict or rendered an unmatched version limit as a conflict'}",
        [93] = "if($loaderNode.Count -ne 1 -or -not $loaderNode[0].Tag.Condition.Contains('>=0.18') -or -not $loaderNode[0].Tag.Condition.Contains('>=0.19')){throw 'Repeated dependencies were not merged with all distinct constraints preserved'}",
        [95] = "foreach($box in $nodes){if($box.Tag.IsConflict -and ([Windows.Controls.Canvas]::GetLeft($box) -ne [Windows.Controls.Canvas]::GetLeft($center) -or [Windows.Controls.Canvas]::GetTop($box) -le [Windows.Controls.Canvas]::GetTop($center)+$center.Height)){throw 'Conflict nodes are not below the selected mod'}}",
        [97] = "if(@($canvas.Children|Where-Object {$_ -is [Windows.Shapes.Path]}).Count -ne 6 -or @($canvas.Children|Where-Object {$_ -is [Windows.Shapes.Polygon]}).Count -ne 6){throw 'Graph arrows do not match direct relations'}",
        [98] = "foreach($box in $nodes){$x=[Windows.Controls.Canvas]::GetLeft($box);$y=[Windows.Controls.Canvas]::GetTop($box);if($x -lt 0 -or $y -lt 0 -or $x+$box.Width -gt $canvas.Width -or $y+$box.Height -gt $canvas.Height){throw 'A graph node lies outside the scrollable canvas'}}",
        [102] = "if($a.IntersectsWith($b)){throw 'Dependency graph nodes overlap'}",
        [116] = "if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'Config library' -and $_.Tag.IsPlatformRequirement}).Count -ne 1 -or @($platformNodes|Where-Object {$_.Tag.Title -eq 'Renderer'}).Count -ne 1 -or @($platformNodes|Where-Object {$_.Tag.Title -eq 'Filename guess'}).Count){throw 'Exact platform requirements were omitted, duplicated or confused with filename guesses'}",
        [117] = "if($report.Issues.Count -ne $localErrors -or $report.Mods.Count -ne $localMods){throw 'Platform graph supplementation changed local startup checks'}",
        [120] = "if(@($fallbackNodes|Where-Object {$_.Tag.Title -eq 'local_only'}).Count -ne 1){throw 'No exact platform declaration did not enable local reference fallback'}",
        [122] = "if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'local_only'}).Count){throw 'A local-only mod requirement overrode the exact platform declaration'}",
        [123] = "if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'Fabric Loader' -or $_.Tag.Title -eq 'Minecraft'}).Count){throw 'Successful platform lookup still displayed local environment references'}",
        [126] = "if(@($timeoutNodes|Where-Object {$_.Tag.Title -eq 'local_only'}).Count -ne 1){throw 'Network timeout did not restore local reference declarations'}",
        [131] = "if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'Consumer'}).Count){throw 'Local reverse requirements overrode an exact platform declaration'}",
        [134] = "if(@($platformNodes|Where-Object {$_.Tag.Title -eq 'Modern Iris' -and $_.Tag.IsConflict}).Count -ne 1){throw 'Exact platform conflict was omitted'}",
        [139] = "if($attention.Count -ne 1 -or -not $attention[0].Tag.NeedsAttention -or -not $attention[0].Tag.Details.Contains('Modrinth') -or -not $attention[0].Tag.Details.Contains('CurseForge')){throw 'Disabled dual-platform declarations were not merged and highlighted'}",
        [155] = "if($sodiumNodes.Count -ne 1 -or $pinnedNodes.Count -ne 2){throw 'A pinned platform version duplicated the installed Sodium project as a missing mod'}",
        [156] = "if($sodiumNodes[0].Tag.NeedsAttention -or -not $sodiumNodes[0].Tag.Condition.Contains('0.8.12') -or -not $sodiumNodes[0].Tag.Details.Contains('PinnedOlderVersion') -or -not $sodiumNodes[0].Tag.Details.Contains('CurseForge')){throw 'A platform version difference became a missing-mod error or lost either source declaration'}",
        [157] = "if($pinned.OnlineFiles[0].Matches[0].Dependencies[0].EnabledPaths.Count -ne 0){throw 'An installed different version incorrectly satisfied a pinned platform version'}",
        [159] = "if(@($incomingPinned|Where-Object {$_.Tag.Title -eq 'Iris' -and $_.Tag.IsCandidate}).Count -ne 1){throw 'Reverse relationships lost a known project when its pinned version differed'}",
        [164] = "if($unknownNodes.Count -ne 2 -or @($unknownNodes|Where-Object {$_.Tag.NeedsAttention -or -not $_.Tag.IsCandidate}).Count){throw 'Unverified same-name platform projects were merged or falsely highlighted as definite missing mods'}",
        [169] = "if(@($externalNodes|Where-Object {$_.Tag.Title -in @('external-one.jar','external-two.jar')}).Count -ne 2){throw 'External dependencies without project IDs were blank or incorrectly merged'}",
        [182] = "if((Nodes $isolated).Count -ne 1){throw 'Shared bundled candidates created a false API-to-mod or mod-to-API dependency'}",
        [188] = "if($choice.Count -ne 1 -or -not $choice[0].Tag.Title.Contains('api_bundle') -or -not $choice[0].Tag.Title.Contains('menu_bundle')){throw 'Ambiguous external providers were incorrectly resolved to the first candidate'}",
        [191] = "if($possible.Count -ne 1 -or -not $possible[0].Tag.IsCandidate){throw 'Possible incoming relation disappeared instead of being identified as a candidate'}",
        [211] = "if($nestedReport.HasErrors){throw 'A compatible nested config library did not satisfy other mods'}",
        [214] = "if($hostNodes.Count -ne 2 -or $embedded.Count -ne 1 -or $embedded[0].Tag.Title -ne 'Config library' -or $embedded[0].Tag.Condition -ne '3.8.1' -or $embedded[0].Tag.NeedsAttention){throw 'A selected bundled library was not shown as its own embedded node'}",
        [215] = "if([Windows.Controls.Canvas]::GetTop($embedded[0]) -le [Windows.Controls.Canvas]::GetTop($hostCenter[0])+$hostCenter[0].Height){throw 'Embedded mods are not below the current mod'}",
        [219] = "if($embeddedHeader.Count -ne 1 -or $downArrows.Count -ne 1){throw 'The embedded group lost its title or downward arrow'}",
        [221] = "if($line.StartPoint.X -ne $end.X -or $end.Y -le $line.StartPoint.Y -or [Math]::Abs($line.StartPoint.Y-([Windows.Controls.Canvas]::GetTop($hostCenter[0])+$hostCenter[0].Height)) -gt 1 -or $end.Y -ge [Windows.Controls.Canvas]::GetTop($embeddedHeader[0])){throw 'The embedded arrow does not point down from the current node to its embedded group'}",
        [223] = "if($provider.Count -ne 1 -or $provider[0].Tag.NeedsAttention -or -not $provider[0].Tag.Condition.Contains('Config library')){throw 'Another mod did not show the selected nested-library container as its satisfied provider'}",
        [225] = "if($disabledCenter.Count -ne 1 -or -not $disabledCenter[0].Tag.Condition.Contains('已禁用') -or -not $disabledCenter[0].Tag.Details.Contains('Controller host')){throw 'Disabled standalone detail did not explain its available nested alternative'}",
        [232] = "if($bundledBox.Count -ne 1 -or $conflictBox.Count -ne 1 -or [Windows.Controls.Canvas]::GetTop($conflictBox[0]) -le [Windows.Controls.Canvas]::GetTop($bundledBox[0])+$bundledBox[0].Height){throw 'The offline embedded group is missing or overlaps conflicts'}",
        [236] = "if(-not $nestedReport.HasErrors){throw 'An incompatible nested version incorrectly suppressed startup dependency errors'}",
        [238] = "if(-not $nestedReport.HasErrors){throw 'Removing all enabled providers incorrectly suppressed startup errors'}",
        [246] = "if($largeNodes.Count -ne 100){throw ('Relationship groups were truncated: expected 100 nodes, got '+$largeNodes.Count)}",
        [250] = "if($longTitle.ActualHeight -le 25 -or $longTitle.DesiredSize.Height -gt $longBox.ActualHeight-8){throw 'A long node title is not fully laid out inside its box'}",
        [253] = "if([Windows.Controls.Canvas]::GetTop($last)+$last.Height -gt $largeScroll.VerticalOffset+$largeScroll.ViewportHeight+1){throw 'The last required node cannot be reached by scrolling'}",
        [259] = "if(@($platformImpacts|Where-Object Name -eq 'Selected mod').Count -ne 1 -or @($platformImpacts|Where-Object Name -eq 'Consumer').Count -ne 1){throw 'Platform-required but locally recommended dependency lost direct or transitive impact warnings'}",
        [262] = "if($impactItems.Count -ne 32){throw 'Impact cards lost affected mods beyond the old text limit'}",
        [266] = "if($cards.ColumnDefinitions.Count -ne 2 -or $cards.Children.Count -ne 32){throw 'Impact warning is not a complete two-column card layout'}",
        [267] = "for($i=0;$i -lt $cards.Children.Count;$i++){if([Windows.Controls.Grid]::GetColumn($cards.Children[$i]) -ne ($i%2) -or $cards.Children[$i].BorderThickness.Left -le 0){throw 'Impact cards lost their border or two-column placement'}}",
        [278] = "if($impactModal.FindName('Btn1').ColorType.ToString() -ne 'Red' -or $impactModal.FindName('Btn2').Text -ne $impactConverter.Button2 -or $impactConverter.Result){throw 'Impact confirmation lost warning/cancel controls or implicitly confirmed'}",
        [296] = "if($modal.FindName('PanCaption').Content.GetType() -ne $graphType){throw 'Native message dialog did not install graph content'}",
        [297] = "if($modal.FindName('Btn1').Text -ne $converter.Button1){throw 'Native message dialog lost its normal close button'}",
        [315] = "if($modal.ActualWidth -gt 471 -or $modal.ActualHeight -gt 371 -or $position.Y+$button.ActualHeight -gt $modal.ActualHeight+1){throw (\"Small-window dialog bounds: size=\"+$modal.ActualWidth+','+$modal.ActualHeight+'; button bottom='+($position.Y+$button.ActualHeight))}",
        [318] = "if(($extent.Width -gt $scroll.ViewportWidth+1 -and $scroll.ScrollableWidth -le 0) -or ($extent.Height -gt $scroll.ViewportHeight+1 -and $scroll.ScrollableHeight -le 0)){throw 'Small-window graph cannot scroll to all nodes'}",
    };
    private static readonly HashSet<int> Seen = new();
    internal static void Reset() => Seen.Clear();
    internal static void Check(int id, bool condition) {
        Native.Check(Original.TryGetValue(id, out string? original), "Unknown graph assertion: " + id);
        Native.Check(condition, "Test-ModDependencyGraph.ps1:" + id + " " + original); Seen.Add(id);
    }
    internal static void Complete() {
        Native.Check(Seen.SetEquals(Original.Keys), "Graph assertion coverage missing=" + string.Join(',', Original.Keys.Except(Seen)));
        Console.WriteLine("PASS dependency-graph original assertions " + Seen.Count + "/" + Original.Count);
    }
}
