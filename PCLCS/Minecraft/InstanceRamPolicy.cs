using System;
using System.IO;
using System.Linq;
using MeloongCore;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLCS;

// LF PageInstanceSetup.GetRam. Global RAM policy remains the original global service.
public static class InstanceRamPolicy {
    public static double Manual(int value) {
        if (value <= 12) return value * 0.1 + 0.3;
        if (value <= 25) return checked(value - 12) * 0.5 + 1.5;
        if (value <= 33) return checked((value - 25) * 1 + 8);
        return checked((value - 33) * 2 + 16);
    }
    public static double Automatic(double availableGiB, int? modCount, bool optiFine) {
        double available = Math.Round(availableGiB * 10) / 10;
        double minimum, first, second, third;
        if (modCount.HasValue) { int count = modCount.Value; minimum = 0.5 + count / 150d; first = 1.5 + count / 90d; second = 2.7 + count / 50d; third = 4.5 + count / 25d; }
        else if (optiFine) { minimum = 0.5; first = 1.5; second = 3; third = 5; }
        else { minimum = 0.5; first = 1.5; second = 2.5; third = 4; }
        double give = Math.Min(available, first); available -= first;
        if (available >= 0.1) { double delta = second - first; give += Math.Min(available * 0.7, delta); available -= delta / 0.7;
            if (available >= 0.1) { delta = third - second; give += Math.Min(available * 0.4, delta); available -= delta / 0.4;
                if (available >= 0.1) { delta = third; give += Math.Min(available * 0.15, delta); available -= delta / 0.15; }
            }
        }
        return Math.Round(Math.Max(give, minimum), 1);
    }
    public static double Get<TInstance,TState,TCard>(TInstance? instance, Func<string,TInstance?,int> setting, Func<double> availableBytes, Func<TInstance?,double> global)
        where TInstance : LegacyMcInstance<TInstance,TState,TCard> where TState : struct, Enum where TCard : struct, Enum {
        if (setting("VersionRamType", instance) == 2) return global(instance);
        if (setting("VersionRamType", instance) != 0) return Manual(setting("VersionRamCustom", instance));
        double available = availableBytes() / 1024 / 1024 / 1024;
        if (instance is not null && !instance.IsLoaded) instance.Load();
        int? mods = null;
        if (instance is not null && instance.Modable) {
            DirectoryInfo directory = DirectoryUtils.GetInfo(instance.PathIndie + @"mods\");
            mods = directory.Exists ? directory.GetFiles().Count(file => new[] { ".jar", ".zip", ".litemod" }.Contains(file.Extension.ToLowerInvariant())) : 0;
        }
        return Automatic(available, mods, instance is not null && instance.Version.HasOptiFine);
    }
    public static int SliderMaximum(double total) => Conversions.ToInteger(total <= 1.5 ? Math.Max(Math.Floor((total - 0.3) / 0.1), 1) : total <= 8 ? Math.Floor((total - 1.5) / 0.5) + 12 : total <= 16 ? Math.Floor(total - 8) + 25 : Math.Floor((total - 16) / 2) + 33);
    public static string Amount(double value) => Conversions.ToString(value) + (value == Math.Floor(value) ? ".0" : "");
}
