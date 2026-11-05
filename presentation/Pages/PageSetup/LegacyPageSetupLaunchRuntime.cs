using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;
using PCLLF.Presentation.Controls;

namespace PCLCS;

public sealed class LegacyPageSetupLaunchRuntime<TInstance, TSkin> where TInstance : class where TSkin : struct {
    public readonly LegacySetupLaunchHost<TInstance, TSkin> Host;
    private readonly LegacySetupLaunchMemory<TInstance> memory;
    public LegacyPageSetupLaunchRuntime(LegacySetupLaunchHost<TInstance, TSkin> host) {
        Host = host;
        memory = new LegacySetupLaunchMemory<TInstance>(host.Common.Read, host.AvailableMemory, host.InstanceLoaded, host.LoadInstance, host.InstanceModable, host.InstanceOptiFine, host.InstanceIndiePath);
    }
    public double GetRam(TInstance? instance, bool useVersionJavaSetup) => memory.GetRam(instance, useVersionJavaSetup);
    public void UpdateSkinType() {
        Host.StartLegacySkin(false);
        if (Host.CurrentLaunch() is null) return;
        switch (Conversions.ToInteger(Host.Common.Read("LaunchSkinType"))) {
            case 0: case 1: case 2: Host.CurrentLaunch()!.View.PanSkinID().Visibility = Visibility.Collapsed; Host.CurrentLaunch()!.View.PanSkinChange().Visibility = Visibility.Collapsed; break;
            case 3: Host.CurrentLaunch()!.View.PanSkinID().Visibility = Visibility.Visible; Host.CurrentLaunch()!.View.PanSkinChange().Visibility = Visibility.Collapsed; break;
            case 4: Host.CurrentLaunch()!.View.PanSkinID().Visibility = Visibility.Collapsed; Host.CurrentLaunch()!.View.PanSkinChange().Visibility = Visibility.Visible; break;
        }
        Host.CurrentLaunch()!.View.CardSkin().TriggerForceResize();
    }
    public void UpdateRamType() {
        if (Host.CurrentLaunch()?.View.SliderRamCustom() is null) return;
        Host.CurrentLaunch()!.View.SliderRamCustom().IsEnabled = Conversions.ToInteger(Host.Common.Read("LaunchRamType")) == 1;
    }
}
