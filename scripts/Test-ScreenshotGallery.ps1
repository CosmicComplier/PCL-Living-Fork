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
# C# delegates run safely on the worker pool; PowerShell script blocks have no background runspace.
$controllerChecks = @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PCLCS;

public static class PclScreenshotControllerChecks {
    private sealed class Tile {
        internal readonly Grid Root = new Grid { Width = 160, Height = 80 };
        internal readonly Image Thumbnail = new Image();
        internal readonly Border Selection = new Border { RenderTransform = new ScaleTransform(.8, .8) };
        internal readonly Border Check = new Border();
        internal bool Checked, Animate, Pressed;
        internal InstanceResourceEntryData Entry;
        internal ResourceThumbnailController Controller;
        internal int Changes, ClickCount, Dispatches;
    }
    private sealed class Work {
        internal readonly ManualResetEventSlim Initial = new ManualResetEventSlim(false);
        internal readonly ManualResetEventSlim Replacement = new ManualResetEventSlim(false);
        internal readonly IOException Failure = new IOException("synthetic thumbnail fault");
        internal readonly ImageSource Normal = Bitmap(20), NewImage = Bitmap(200);
        internal int Active, Maximum, Calls, Completed, ReplacementStarted, Warnings, BadWidth;
        internal Exception Observed;
        internal ImageSource Load(string name, int width) {
            Interlocked.Increment(ref Calls);
            if (width != 640) Interlocked.Increment(ref BadWidth);
            int active = Interlocked.Increment(ref Active), old;
            do { old = Volatile.Read(ref Maximum); if (active <= old) break; }
            while (Interlocked.CompareExchange(ref Maximum, active, old) != old);
            try {
                if (name == "fault") throw Failure;
                if (name == "replacement") {
                    Interlocked.Increment(ref ReplacementStarted);
                    if (!Replacement.Wait(8000)) throw new TimeoutException("Replacement loader gate timed out.");
                    return NewImage;
                }
                if (!Initial.Wait(8000)) throw new TimeoutException("Initial loader gate timed out.");
                return Normal;
            } finally { Interlocked.Decrement(ref Active); Interlocked.Increment(ref Completed); }
        }
    }
    private static ImageSource Bitmap(byte value) {
        BitmapSource result = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null,
            new byte[] { value, 0, 0, 255 }, 4);
        result.Freeze();
        return result;
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void PumpUntil(Func<bool> condition, int timeout, string message) {
        if (condition()) return;
        DispatcherFrame frame = new DispatcherFrame();
        Stopwatch elapsed = Stopwatch.StartNew();
        bool done = false;
        DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
        timer.Interval = TimeSpan.FromMilliseconds(10);
        timer.Tick += delegate { done = condition(); if (done || elapsed.ElapsedMilliseconds >= timeout) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Require(done, message);
    }
    private static void PumpFor(int milliseconds) {
        Stopwatch elapsed = Stopwatch.StartNew();
        PumpUntil(delegate { return elapsed.ElapsedMilliseconds >= milliseconds; }, milliseconds + 2000,
            "The dispatcher did not process the observation interval.");
    }
    private static Tile Create(Work work) {
        Tile tile = new Tile();
        TextBlock name = new TextBlock(), info = new TextBlock();
        tile.Root.Children.Add(tile.Thumbnail);
        ResourceThumbnailController.View view = new ResourceThumbnailController.View(tile.Root,
            tile.Thumbnail, name, info, tile.Selection, tile.Check,
            delegate { return tile.Checked; }, delegate(bool value) { tile.Checked = value; },
            delegate { return tile.Entry; }, delegate(InstanceResourceEntryData value) { tile.Entry = value; });
        ResourceThumbnailController.Host host = new ResourceThumbnailController.Host(work.Load,
            delegate(Action action) { tile.Root.Dispatcher.BeginInvoke(new Action(delegate { action(); tile.Dispatches++; }), DispatcherPriority.Normal); },
            delegate(Exception error, string message) {
                Interlocked.Exchange(ref work.Observed, error); Interlocked.Increment(ref work.Warnings);
            },
            delegate(Border back, bool hovered, bool selected, bool pressed) { tile.Pressed = pressed; },
            delegate { return tile.Animate; }, delegate { });
        tile.Controller = new ResourceThumbnailController(view, host);
        tile.Controller.Changed += delegate { tile.Changes++; };
        tile.Controller.ClickRequested += delegate(int count) { tile.ClickCount = count; };
        return tile;
    }
    public static void Run() {
        int workers, completion;
        ThreadPool.GetMinThreads(out workers, out completion);
        // Allow four worker delegates to expose a missing semaphore rather than thread-pool starvation.
        ThreadPool.SetMinThreads(Math.Max(workers, 8), completion);
        Work work = new Work();
        Canvas viewport = new Canvas();
        Window window = new Window { Content = viewport, Width = 500, Height = 600,
            Left = -20000, Top = -20000, ShowActivated = false, ShowInTaskbar = false,
            WindowStyle = WindowStyle.None };
        Application app = Application.Current;
        Window previousMain = app.MainWindow;
        ShutdownMode shutdown = app.ShutdownMode;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        List<Tile> tiles = new List<Tile>();
        int submitted = 0;
        try {
            for (int index = 0; index < 4; index++) {
                Tile tile = Create(work);
                tiles.Add(tile); viewport.Children.Add(tile.Root); Canvas.SetTop(tile.Root, index * 90);
                tile.Controller.Bind(new InstanceResourceEntryData { FullPath = "old-" + index,
                    Name = "tile-" + index, Description = "synthetic controller view" });
            }
            window.Show(); window.UpdateLayout();
            PumpUntil(delegate { return tiles.TrueForAll(delegate(Tile tile) { return tile.Root.IsLoaded; }) && viewport.ActualHeight > 0; },
                4000, "The offscreen controller views did not load.");
            foreach (Tile tile in tiles) { tile.Controller.UpdateViewport(viewport); submitted++; }
            PumpUntil(delegate { return Volatile.Read(ref work.Active) == 2; }, 4000, "Two thumbnail decodes did not start.");
            PumpFor(100);
            Require(Volatile.Read(ref work.Calls) == 2 && Volatile.Read(ref work.Maximum) == 2,
                "More than two thumbnail decoders ran while the first two were blocked.");
            Tile first = tiles[0];
            first.Controller.ReleaseThumbnail();
            first.Controller.Bind(new InstanceResourceEntryData { FullPath = "replacement", Name = "new", ModifiedUtc = DateTime.UtcNow });
            first.Controller.UpdateViewport(viewport); submitted++;
            work.Initial.Set();
            PumpUntil(delegate { return Volatile.Read(ref work.Completed) >= 4 && Volatile.Read(ref work.ReplacementStarted) == 1 && first.Dispatches >= 1 &&
                object.ReferenceEquals(tiles[1].Thumbnail.Source, work.Normal) && object.ReferenceEquals(tiles[2].Thumbnail.Source, work.Normal) &&
                object.ReferenceEquals(tiles[3].Thumbnail.Source, work.Normal); }, 6000, "Queued thumbnail requests did not complete.");
            Require(first.Thumbnail.Source == null, "A released/rebound revision repainted the old thumbnail.");
            work.Replacement.Set();
            PumpUntil(delegate { return object.ReferenceEquals(first.Thumbnail.Source, work.NewImage); }, 4000,
                "The new revision did not publish its thumbnail.");
            Require(work.Maximum == 2 && work.BadWidth == 0 && work.Warnings == 0,
                "Concurrency, decode width or successful dispatch changed.");
            first.Controller.Checked = true; first.Controller.CheckChanged();
            Require(first.Controller.Checked && first.Changes == 1 && first.Selection.Opacity == 1,
                "Checkbox selection did not update the view and change callback.");
            first.Animate = true;
            MouseButtonEventArgs down = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left);
            down.RoutedEvent = UIElement.MouseLeftButtonDownEvent; first.Root.RaiseEvent(down);
            Require(down.Handled && first.ClickCount == down.ClickCount && first.Pressed,
                "Mouse selection did not preserve click count, handled state or press feedback.");
            MouseButtonEventArgs up = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left);
            up.RoutedEvent = UIElement.MouseLeftButtonUpEvent; first.Root.RaiseEvent(up);
            Require(!first.Pressed, "Pointer release did not clear press feedback.");
            Canvas.SetTop(tiles[1].Root, 1000); window.UpdateLayout(); tiles[1].Controller.UpdateViewport(viewport);
            Require(tiles[1].Thumbnail.Source == null, "Leaving the viewport did not release the thumbnail.");
            viewport.Children.Remove(tiles[2].Root);
            PumpUntil(delegate { return !tiles[2].Root.IsLoaded && tiles[2].Thumbnail.Source == null; }, 4000,
                "Unloading did not release the thumbnail.");
            Tile fault = tiles[3];
            fault.Controller.Bind(new InstanceResourceEntryData { FullPath = "fault", Name = "fault" });
            int faultDispatch = fault.Dispatches + 1;
            fault.Controller.UpdateViewport(viewport); submitted++;
            PumpUntil(delegate { return Volatile.Read(ref work.Warnings) == 1 && Volatile.Read(ref work.Active) == 0 && fault.Dispatches >= faultDispatch; }, 4000,
                "A decoder fault did not reach the UI completion and warning path.");
            Require(object.ReferenceEquals(work.Observed, work.Failure) && fault.Thumbnail.Source == null,
                "Fault identity or thumbnail state changed.");
            int calls = work.Calls;
            fault.Controller.UpdateViewport(viewport); fault.Controller.UpdateViewport(viewport); PumpFor(100);
            Require(work.Calls == calls, "A completed fault retried on every viewport update.");
        } finally {
            work.Initial.Set(); work.Replacement.Set(); window.Close();
            try { PumpUntil(delegate { return Volatile.Read(ref work.Completed) >= submitted && Volatile.Read(ref work.Active) == 0; },
                6000, "Thumbnail workers did not drain after gate release."); PumpFor(30); }
            finally { app.MainWindow = previousMain; app.ShutdownMode = shutdown; ThreadPool.SetMinThreads(workers, completion); }
        }
    }
}
'@

$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclScreenshotGallery-'+[Guid]::NewGuid().ToString('N'))
try {
 $kind=Kind 'Screenshots';$shots=Join-Path $fixture 'screenshots'
 foreach($name in @('one.png','two.png','three.png')){Write-Png (Join-Path $shots $name)}
 $items=Scan $shots $kind
 $asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh').Invoke($null,@(0))|Out-Null
 $type=$asm.GetType('PCL.PageInstanceResources');$page=$type.GetConstructor($flags,$null,[type[]]@($kindType),$null).Invoke(@($kind))
 $type.GetMethod('ShowResources',$flags).Invoke($page,[object[]]@([string]$shots,[string]$shots,$items.PSObject.BaseObject))|Out-Null
 $grid=$page.FindName('PanScreenshots')
 if($grid.Columns -ne 2 -or $grid.Children.Count -ne 3 -or $grid.Visibility -ne 'Visible' -or $page.FindName('CardPreview')){throw 'Screenshot page is not a two-column gallery without an internal preview'}
 $script:opened=0;$script:info=$null
 $open=[Action[Diagnostics.ProcessStartInfo]]{param($info) $script:opened++;$script:info=$info}
 $click=$type.GetMethod('ScreenshotClick',$flags)
 $click.Invoke($page,[object[]]@($items[0],1,$open))|Out-Null
 if($script:opened -ne 0 -or -not $grid.Children[0].Checked){throw 'Single click opened a viewer or did not select'}
 $click.Invoke($page,[object[]]@($items[0],2,$open))|Out-Null
 if($script:opened -ne 1 -or -not $script:info.UseShellExecute -or $script:info.FileName -cne $items[0].FullPath -or $script:info.Arguments -ne ''){throw 'Double click did not dispatch the image to the system file association'}
 $click.Invoke($page,[object[]]@($items[1],1,$open))|Out-Null
 if($grid.Children[0].Checked -or -not $grid.Children[1].Checked -or $script:opened -ne 1){throw 'Second single-click selection failed'}
 $type.GetMethod('ChangeAllSelected',$flags).Invoke($page,@($true))|Out-Null
 if(@($grid.Children|Where-Object Checked).Count -ne 3 -or $page.FindName('CardSelect').Visibility -ne 'Visible' -or -not $page.FindName('BtnExport').IsEnabled){throw 'Gallery multiselect or bulk footer unavailable'}
 $type.GetMethod('ChangeAllSelected',$flags).Invoke($page,@($false))|Out-Null
 if($page.FindName('CardSelect').Visibility -ne 'Collapsed'){throw 'Gallery deselection did not collapse the footer'}
 $page.FindName('SearchBox').Text='two'
 if($grid.Children.Count -ne 1){throw 'Screenshot gallery search failed'}
 $page.FindName('SearchBox').Text=''
 $page.FindName('PanLoad').Visibility='Collapsed';$page.FindName('PanAllBack').Visibility='Visible';$page.FindName('PanAllBack').Opacity=1
 foreach($tile in $grid.Children){$entry=$tile.GetType().GetField('Entry',$flags).GetValue($tile);$tile.FindName('Thumbnail').Source=Invoke-Store 'LoadImage' @($entry.FullPath,640)}
 $panel=[Windows.Controls.Grid]::new();$panel.Background=[Windows.Media.BrushConverter]::new().ConvertFromString('#e4effa');$panel.Children.Add($page)|Out-Null
 if($PreviewDirectory){[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null;Render-Png $panel (Join-Path $PreviewDirectory 'screenshot-gallery.png') 780 720}
 $type.GetMethod('ScreenshotClick',$flags).Invoke($page,[object[]]@($items[0],1,$open))|Out-Null
 if($script:opened -ne 1){throw 'Selection unexpectedly launched another viewer'}
 Add-Type -TypeDefinition $controllerChecks -ReferencedAssemblies @('System.dll','System.Core.dll','System.Xaml.dll',
  [Windows.FrameworkElement].Assembly.Location,[Windows.UIElement].Assembly.Location,
  [Windows.Point].Assembly.Location,[PCLCS.ResourceThumbnailController].Assembly.Location)
 [PclScreenshotControllerChecks]::Run()
 Write-Output 'Screenshot gallery passed: two-column native cards, no in-app preview, single selection, double-click shell association, bulk footer, deselection, search, frozen thumbnails and real C# async controller concurrency/revision/viewport/unload/fault behavior. No external viewer was started.'
}finally {
 $app.Shutdown()
 $full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if($full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($full).StartsWith('PclScreenshotGallery-')){Remove-Item -LiteralPath $full -Recurse -Force}
}