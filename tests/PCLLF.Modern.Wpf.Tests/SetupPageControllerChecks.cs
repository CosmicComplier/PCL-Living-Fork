#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PCLCS;
using PCLLF.Presentation.Controls;

// Run only on the actual WPF STA with LF ControlServices/palette already configured.
// UI flow boundaries are controlled; no real user settings, cleanup, update, UAC,
// process/page launch, skin files or login/network requests are touched.
public static class PclSetupPageControllerChecks {
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Instance { public string Name = "synthetic-A"; }
    private struct Skin { public bool Valid; }
    private sealed class Fixture {
        public int Suppression, MessageResult=2, Messages, Writes, Imports, Deletes, HiddenWrites;
        public bool Force;
        public Instance Selected = new Instance(), Imported;
        public Action DuringMessage;
        public readonly List<LegacySetupAnimation> Animations = new List<LegacySetupAnimation>();
        public readonly LegacySetupPageHost Common;
        public readonly LegacySetupLaunchHost<Instance,Skin> Host;
        public readonly LegacyPageSetupLaunchRuntime<Instance,Skin> Runtime;
        public Fixture() {
            Common = new LegacySetupPageHost {
                Read = key => key == "UiBackgroundMode" ? (object)1 : key == "LaunchRamType" ? 1 : key == "LaunchRamCustom" ? 12 : 0,
                Write = (key,value) => Writes++, AnimationSuppression=()=>Suppression,
                Message = (text,title,b1,b2,b3,warn) => { Messages++; DuringMessage?.Invoke(); return MessageResult; },
                Hint=(text,kind,log)=>{}, UserDataFolder=()=>System.IO.Path.Combine(System.IO.Path.GetTempPath(),"pcllf-nonexistent-"+Guid.NewGuid())+System.IO.Path.DirectorySeparatorChar,
                BaseFolder=()=>"synthetic-root\\", DeleteDirectory=path=>Deletes++,
                StartAnimations=(plans,name)=>Animations.AddRange(plans),StopAnimation=name=>{},AnimationRunning=name=>false
            };
            Host = new LegacySetupLaunchHost<Instance,Skin> {
                Common=Common, SelectedInstance=()=>Selected, InstanceName=i=>i.Name,
                PersonalProfileExists=()=>false,ImportPersonalOptions=i=>{Imports++;Imported=i;},
                SkinValid=s=>s.Valid, SelectSkin=()=>new Skin {Valid=false},
                AvailableMemory=()=>2UL*1024*1024*1024,TotalMemory=()=>8UL*1024*1024*1024,
                IsCurrentRamPage=()=>true
            };
            Runtime = new LegacyPageSetupLaunchRuntime<Instance,Skin>(Host);
        }
    }
    public static void Run() {
        Require(Application.Current != null && Application.Current.Dispatcher.CheckAccess(), "A real WPF STA application is required.");
        // Constructors below use real BAML controls and actual configured host services.
        var fixture = new Fixture();
        var check = new MyCheckBoxView { Checked=true };
        var launch = new LegacyPageSetupLaunchController<Instance,Skin>(fixture.Runtime, new LegacyPageSetupLaunchView { CheckPersonalOptionsSync=()=>check });
        launch.PersonalSyncChanged(check,false);
        fixture.Suppression=1; launch.PersonalSyncChanged(check,true);
        Require(fixture.Messages==0 && fixture.Writes==0 && check.Checked,"Non-user/reentrant personal-sync callback changed persisted state.");
        fixture.Suppression=0; launch.PersonalSyncChanged(check,true);
        Require(fixture.Messages==1 && fixture.Writes==0 && !check.Checked && fixture.Imports==0,"Canceled personal import persisted or remained checked.");
        Instance first=fixture.Selected, second=new Instance {Name="synthetic-B"};
        fixture.MessageResult=1;fixture.DuringMessage=()=>fixture.Selected=second;
        Require(launch.ImportPersonalOptions() && ReferenceEquals(first,fixture.Imported),"Modal reentry imported a different selected instance.");
        bool handled=false;launch.PreviewCustomSkin(false,ref handled);Require(!handled,"Programmatic skin preview was handled.");
        launch.PreviewCustomSkin(true,ref handled);Require(handled && fixture.Writes==0,"Skin picker cancellation changed settings or did not cancel.");
        fixture.DuringMessage=null; fixture.MessageResult=2;
        var uiHost=new LegacySetupUiHost { Common=fixture.Common,HasMain=()=>false,CurrentUI=()=>null,
            ReadHiddenForceShow=()=>fixture.Force, WriteHiddenForceShow=value=>{fixture.Force=value;fixture.HiddenWrites++;},
            Shell=element=>new Grid() };
        var uiRuntime=new LegacyPageSetupUIRuntime(uiHost);
        var ui=new LegacyPageSetupUIController(uiRuntime,new LegacyPageSetupUIView());
        ui.ClearBackground();Require(fixture.Deletes==0,"Canceled background cleanup invoked the physical deletion boundary.");
        fixture.MessageResult=1;ui.ClearBackground();Require(fixture.Deletes==1,"Confirmed cleanup did not invoke its real typed deletion boundary exactly once.");
        uiRuntime.HiddenForceShow=true;uiRuntime.HiddenForceShow=true;
        Require(fixture.Force && fixture.HiddenWrites==2,"Repeated LF hidden-force assignment skipped original write/refresh behavior.");
        var thread=new MySliderView();var speed=new MySliderView();var debug=new MySliderView();
        var system=new LegacyPageSetupSystemController(new LegacySetupSystemHost{Common=fixture.Common},new LegacyPageSetupSystemView {
            SliderDownloadThread=()=>thread,SliderDownloadSpeed=()=>speed,SliderDebugAnim=()=>debug
        });
        system.SliderLoad();
        Require(thread.GetHintText is Func<object,object> && speed.GetHintText is Func<object,object> && debug.GetHintText is Func<object,object>,"Legacy slider tooltip delegate ABI changed.");
        CultureInfo prior=CultureInfo.CurrentCulture;
        try {
            foreach(string name in new[]{"en-US","fr-FR"}) {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(name);
                string point=name=="fr-FR"?",":".";
                Require((string)((Func<object,object>)speed.GetHintText)(0)=="0"+point+"1 M/s","Low download speed formatting changed.");
                Require((string)((Func<object,object>)speed.GetHintText)(15)=="2 M/s","Middle speed boundary changed.");
                Require((string)((Func<object,object>)speed.GetHintText)(32)=="11 M/s","High speed boundary changed.");
                Require((string)((Func<object,object>)speed.GetHintText)(42)=="无限制","Unlimited speed boundary changed.");
                Require((string)((Func<object,object>)debug.GetHintText)(29)=="3x" && (string)((Func<object,object>)debug.GetHintText)(30)=="关闭","Animation tooltip limit changed.");
                Require(Convert.ToInt32(((Func<object,object>)thread.GetHintText)(null))==1,"VB null tooltip arithmetic changed.");
            }
        } finally { CultureInfo.CurrentCulture=prior; }
        CultureInfo originalRamCulture=CultureInfo.CurrentCulture;
        try {
        CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("en-US");
        var used=new ColumnDefinition();var game=new ColumnDefinition();var empty=new ColumnDefinition();
        var gameLabel=new TextBlock();var usedLabel=new TextBlock();var totalLabel=new TextBlock();var custom=new MySliderView();
        var ram=new LegacyPageSetupLaunchController<Instance,Skin>(fixture.Runtime,new LegacyPageSetupLaunchView {
            LabRamGame=()=>gameLabel,LabRamUsed=()=>usedLabel,LabRamTotal=()=>totalLabel,SliderRamCustom=()=>custom,
            ColumnRamUsed=()=>used,ColumnRamGame=()=>game,ColumnRamEmpty=()=>empty
        });
        ram.RefreshRam(false);
        Require(gameLabel.Text=="1.5 GB" && usedLabel.Text=="6.0 GB" && totalLabel.Text==" / 8.0 GB", "RAM labels changed.");
        Require(used.Width.Value==6 && game.Width.Value==1.5 && empty.Width.Value==0.5 && custom.MaxValue==25,"LF RAM geometry/slider bounds changed.");
        ram.RefreshRam(true);
        Require(fixture.Animations.Count==3 && fixture.Animations.All(plan=>plan.Duration==800 && plan.Ease==LegacySetupEase.OutFluentStrong && plan.Delta==0),"RAM animation contract changed.");
        } finally { CultureInfo.CurrentCulture=originalRamCulture; }
    }
}
