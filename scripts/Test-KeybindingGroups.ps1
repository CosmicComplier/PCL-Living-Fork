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
$controllerChecks = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PCLCS;

public static class PclKeybindingControllerChecks {
    private const string Original = "lang:zh_cn\r\nkey_key.forward:key.keyboard.w\r\nkey_key.other:key.keyboard.c\r\n";
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Fixture {
        internal readonly object First = new object(), Second = new object(), Gate = new object();
        internal readonly string RootA, RootB;
        internal readonly KeybindingPageView<InstanceKeybindingData> View;
        internal readonly KeybindingPageController<object, InstanceKeybindingData> Controller;
        internal readonly InstanceKeybindingData Entry = new InstanceKeybindingData {
            Key = "key_key.forward", Name = "Forward", Owners = "Minecraft", Value = "key.keyboard.w", Normalized = "key.keyboard.w", SharedCount = 1
        };
        internal readonly IOException ProfileError = new IOException("synthetic profile capture failure");
        internal object Current, ProfileOwner;
        internal string SavedFolder, LoadedFolder;
        internal int SaveCalls, Invalidations, ProfileCalls, Loads, Messages, Successes, Errors, Warnings, CaptureCount;
        internal Exception LastError, LastWarning;
        internal bool FailProfile;
        internal Func<int> Confirm;
        internal Fixture(string root) {
            RootA = Path.Combine(root, "first"); RootB = Path.Combine(root, "second"); Current = First;
            Directory.CreateDirectory(RootA); Directory.CreateDirectory(RootB);
            File.WriteAllText(Path.Combine(RootA, "options.txt"), Original, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(RootB, "options.txt"), Original, new UTF8Encoding(false));
            Grid page = new Grid(), card = new Grid(); StackPanel rows = new StackPanel();
            Button cancel = new Button(), clear = new Button();
            card.Children.Add(rows); page.Children.Add(card); page.Children.Add(cancel); page.Children.Add(clear);
            View = new KeybindingPageView<InstanceKeybindingData> {
                Root = page, CardBindings = card, Bindings = rows, CancelButton = cancel, ClearButton = clear,
                Empty = new TextBlock(), Capture = new TextBlock(), Search = delegate { return ""; },
                SetAllText = delegate(string text) { }, SetConflictsText = delegate(string text) { }, ScrollHome = delegate { },
                CreateButton = CreateButton, CreateGroup = CreateGroup,
                RefreshCaptureBar = delegate(int count, int previous, bool animate) { CaptureCount = count; return count; }
            };
            KeybindingPageHost<object> host = new KeybindingPageHost<object> {
                CurrentInstance = delegate { return Current; }, GameFolder = Folder, VersionPath = Folder,
                AllowsInstance = delegate(object instance) { return true; }, IsLocked = delegate(object instance) { return false; },
                VersionValid = delegate(object instance) { return true; }, VersionMajor = delegate(object instance) { return 20; },
                HasRunningGame = delegate { return false; }, IsCurrentPage = delegate { return true; },
                AnimationEnabled = delegate { return false; }, LoaderFailed = delegate { return false; },
                StartLoader = delegate(string folder) { Loads++; LoadedFolder = folder; }, CancelLoader = delegate { },
                BindExternalEvents = delegate(bool attach, KeyEventHandler key, MouseButtonEventHandler mouse) { },
                BindingForVersion = PersonalOptionsRules.BindingForVersion,
                Save = Save,
                CaptureEditedBinding = delegate(object instance, string key, string encoded) {
                    ProfileCalls++; ProfileOwner = instance; if (FailProfile) throw ProfileError;
                },
                Message = delegate(string text, string title, string first, string second, bool warning) {
                    Messages++;
                    Require(Controller.Captured == null && CaptureCount == 0, "Capture was still active when the modal confirmation opened.");
                    return Confirm == null ? 1 : Confirm();
                },
                Hint = delegate(string text, bool success) { if (success) Successes++; },
                Warn = delegate(Exception error, string message) { Warnings++; LastWarning = error; },
                Error = delegate(Exception error, string message) { Errors++; LastError = error; }
            };
            Controller = new KeybindingPageController<object, InstanceKeybindingData>(View, host);
            cancel.Click += delegate { Controller.CancelCapture(); };
            clear.Click += delegate { Controller.ClearBinding(); };
            Controller.RefreshBindings();
            Controller.BindingsLoaded(new List<InstanceKeybindingData> {
                Entry, new InstanceKeybindingData { Key = "key_key.other", Name = "Other", Owners = "Minecraft", Value = "key.keyboard.c", Normalized = "key.keyboard.c", SharedCount = 1 }
            });
        }
        private string Folder(object owner) {
            if (object.ReferenceEquals(owner, First)) return RootA;
            if (object.ReferenceEquals(owner, Second)) return RootB;
            throw new InvalidOperationException("Unknown synthetic owner.");
        }
        private void Save(string folder, string key, string previous, string replacement) {
            Require(folder == RootA || folder == RootB, "Controller attempted to save outside its two synthetic roots.");
            SaveCalls++; SavedFolder = folder;
            InstanceKeybindingFiles.Save(folder, key, previous, replacement, Gate, delegate { return false; },
                delegate(string root, string path) {
                    Require(Path.GetFullPath(path) == Path.Combine(Path.GetFullPath(root), "options.txt"), "Unexpected options path.");
                }, delegate(string path) { Invalidations++; });
        }
        private static KeybindingPageButton CreateButton(InstanceKeybindingData entry, string text, bool conflict, string tooltip, Action clicked) {
            Button button = new Button { Tag = entry, Content = text, ToolTip = tooltip };
            button.Click += delegate { clicked(); };
            return new KeybindingPageButton(button, delegate(string value) { button.Content = value; },
                delegate(KeybindingPageButtonState state) { button.Background = state == KeybindingPageButtonState.Capturing ? Brushes.Gold : Brushes.Transparent; });
        }
        private static KeybindingPageGroup CreateGroup(string title, string normalized) {
            GroupBox group = new GroupBox { Header = title, Tag = normalized };
            return new KeybindingPageGroup(group, delegate(Panel rows) { group.Content = rows; });
        }
    }
    private static void Saved(Fixture fixture, string folder, string value, int loads) {
        string file = Path.Combine(folder, "options.txt");
        Require(File.ReadAllText(file) == Original.Replace("key_key.forward:key.keyboard.w", "key_key.forward:" + value), "The real options transaction did not write the expected binding.");
        Require(File.ReadAllText(file + ".pcl-backup") == Original, "The original options backup was not preserved.");
        Require(fixture.SaveCalls == 1 && fixture.Invalidations == 1 && fixture.ProfileCalls == 1 && fixture.Successes == 1 && fixture.Errors == 0,
            "Successful save did not complete cache/profile/success callbacks exactly once.");
        Require(fixture.Controller.Captured == null && fixture.CaptureCount == 0 && fixture.Loads == loads && fixture.LoadedFolder == folder,
            "Successful save left capture active or failed to refresh the active root.");
    }
    private static bool PreviewMouse(Fixture fixture, UIElement source) {
        bool called = false, handled = false;
        MouseButtonEventHandler preview = delegate(object sender, MouseButtonEventArgs args) {
            called = true;
            Require(args.OriginalSource is DependencyObject, "The routed mouse event has no real original source.");
            fixture.Controller.HandleCapturedMouse(sender, args); handled = args.Handled;
        };
        fixture.View.Root.AddHandler(UIElement.PreviewMouseDownEvent, preview);
        try {
            MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left);
            args.RoutedEvent = UIElement.PreviewMouseDownEvent; source.RaiseEvent(args);
        } finally { fixture.View.Root.RemoveHandler(UIElement.PreviewMouseDownEvent, preview); }
        Require(called, "The preview mouse event did not reach the real controller.");
        return handled;
    }
    public static void Run(string root) {
        Fixture escape = new Fixture(Path.Combine(root, "escape"));
        escape.Controller.BeginCapture(escape.Entry);
        Require(object.ReferenceEquals(escape.Controller.Captured, escape.Entry) && escape.CaptureCount == 1, "BeginCapture did not update capture state.");
        KeyEventArgs key = new KeyEventArgs(Keyboard.PrimaryDevice, null, Environment.TickCount, Key.Escape);
        escape.Controller.CaptureKey(escape.View.Root, key);
        Require(key.Handled && escape.Controller.Captured == null && escape.CaptureCount == 0 && escape.SaveCalls == 0 && escape.Successes == 0 && escape.Errors == 0,
            "Escape did not cancel capture without saving.");
        Require(File.ReadAllText(Path.Combine(escape.RootA, "options.txt")) == Original, "Escape changed options.");

        Fixture cancel = new Fixture(Path.Combine(root, "cancel"));
        cancel.Controller.BeginCapture(cancel.Entry); cancel.View.CancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(cancel.Controller.Captured == null && cancel.CaptureCount == 0 && cancel.SaveCalls == 0, "The standard cancel click did not cancel capture.");

        Fixture clear = new Fixture(Path.Combine(root, "clear"));
        clear.Controller.BeginCapture(clear.Entry); clear.View.ClearButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Saved(clear, clear.RootA, "key.keyboard.unknown", 2);
        Require(clear.Messages == 0 && clear.Warnings == 0, "Clearing a binding unexpectedly opened a conflict dialog or warned.");

        Fixture mouse = new Fixture(Path.Combine(root, "mouse"));
        mouse.Controller.BeginCapture(mouse.Entry);
        Require(PreviewMouse(mouse, mouse.View.Root), "A regular captured mouse press was not handled.");
        Saved(mouse, mouse.RootA, "key.mouse.left", 2);

        Fixture reject = new Fixture(Path.Combine(root, "reject"));
        reject.Confirm = delegate { return 0; };
        reject.Controller.BeginCapture(reject.Entry); reject.Controller.SaveBinding("key.keyboard.c");
        Require(reject.Messages == 1 && reject.Controller.Captured == null && reject.CaptureCount == 0 && reject.SaveCalls == 0 && reject.ProfileCalls == 0 &&
            reject.Successes == 0 && reject.Errors == 0 && reject.Loads == 1, "Rejecting a conflict saved, refreshed or left capture active.");
        Require(File.ReadAllText(Path.Combine(reject.RootA, "options.txt")) == Original && !File.Exists(Path.Combine(reject.RootA, "options.txt.pcl-backup")),
            "Rejecting a conflict touched the options transaction.");

        Fixture change = new Fixture(Path.Combine(root, "owner-change"));
        change.Confirm = delegate {
            change.Current = change.Second; change.Controller.RefreshBindings();
            Require(object.ReferenceEquals(change.Controller.Owner, change.Second) && change.Controller.GameFolder == change.RootB,
                "The modal callback did not change the real owner/folder state.");
            return 1;
        };
        change.Controller.BeginCapture(change.Entry); change.Controller.SaveBinding("key.keyboard.c");
        Saved(change, change.RootB, "key.keyboard.c", 3);
        Require(change.Messages == 1 && change.SavedFolder == change.RootB && object.ReferenceEquals(change.ProfileOwner, change.Second) && change.Warnings == 0,
            "Saving after a modal callback reused the old owner or root.");
        Require(File.ReadAllText(Path.Combine(change.RootA, "options.txt")) == Original && !File.Exists(Path.Combine(change.RootA, "options.txt.pcl-backup")),
            "The previous owner's options were changed after the modal callback.");

        Fixture profile = new Fixture(Path.Combine(root, "profile-failure"));
        profile.FailProfile = true; profile.Controller.BeginCapture(profile.Entry); profile.Controller.SaveBinding("key.keyboard.v");
        Saved(profile, profile.RootA, "key.keyboard.v", 2);
        Require(profile.Warnings == 1 && object.ReferenceEquals(profile.LastWarning, profile.ProfileError), "A profile capture failure did not warn while retaining save success and refresh.");

        Fixture failure = new Fixture(Path.Combine(root, "save-failure"));
        string changed = Original.Replace("key_key.forward:key.keyboard.w", "key_key.forward:key.keyboard.z");
        File.WriteAllText(Path.Combine(failure.RootA, "options.txt"), changed, new UTF8Encoding(false));
        failure.Controller.BeginCapture(failure.Entry); failure.Controller.SaveBinding("key.keyboard.v");
        Require(failure.SaveCalls == 1 && failure.Errors == 1 && failure.LastError is IOException && failure.Invalidations == 0 && failure.ProfileCalls == 0 &&
            failure.Successes == 0 && failure.Warnings == 0 && failure.Loads == 1 && failure.Controller.Captured == null && failure.CaptureCount == 0,
            "A real stale-file save failure was not reported, falsely succeeded or refreshed.");
        Require(File.ReadAllText(Path.Combine(failure.RootA, "options.txt")) == changed && !File.Exists(Path.Combine(failure.RootA, "options.txt.pcl-backup")),
            "A failed save changed the current options or created a backup.");
    }
}
'@

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
 [PclKeybindingControllerChecks]::Run($fixture)
 Write-Output 'Shared-key UI passed: native cards/buttons, title counts, complete rows, original order, search and conflict filtering; real C# controller passed Escape, clear/cancel clicks, captured mouse save, capture reset before conflict, rejection, modal owner switch, profile warning and actual options save failure checks.'
}finally {
 $app.Shutdown()
 $full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if($full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($full).StartsWith('PclKeybindingController-') -and [IO.Directory]::Exists($full)){Remove-Item -LiteralPath $full -Recurse -Force}
}
