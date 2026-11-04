using System;
using System.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLCS;

/// <summary>Exact LF global-page RAM policy; no new Java/version/pack rule.</summary>
public sealed class LegacySetupLaunchMemory<TInstance> where TInstance : class {
    private readonly Func<string, object?> read;
    private readonly Func<ulong> availableMemory;
    private readonly Func<TInstance, bool> loaded, modable, optiFine;
    private readonly Action<TInstance> load;
    private readonly Func<TInstance, string> indiePath;
    public LegacySetupLaunchMemory(Func<string, object?> read, Func<ulong> availableMemory,
        Func<TInstance, bool> loaded, Action<TInstance> load, Func<TInstance, bool> modable,
        Func<TInstance, bool> optiFine, Func<TInstance, string> indiePath) {
        this.read = read; this.availableMemory = availableMemory; this.loaded = loaded;
        this.load = load; this.modable = modable; this.optiFine = optiFine; this.indiePath = indiePath;
    }
    public double GetRam(TInstance? instance, bool useVersionJavaSetup) {
        // LF retains this old parameter without reading it; do not import CE rules.
        double give = 0;
        if (Conversions.ToInteger(read("LaunchRamType")) == 0) {
            double available = Math.Round((double)availableMemory() / 1024 / 1024 / 1024 * 10) / 10;
            double minimum, target1, target2, target3;
            if (instance is not null && !loaded(instance)) load(instance);
            if (instance is not null && modable(instance)) {
                var directory = DirectoryUtils.GetInfo(indiePath(instance) + "mods\\");
                int mods = directory.Exists ? directory.GetFiles().Count(file => new[] { ".jar", ".zip", ".litemod" }.Contains(file.Extension.Lower())) : 0;
                minimum = 0.5 + mods / 150d;
                target1 = 1.5 + mods / 90d;
                target2 = 2.7 + mods / 50d;
                target3 = 4.5 + mods / 25d;
            } else if (instance is not null && optiFine(instance)) {
                minimum = 0.5; target1 = 1.5; target2 = 3; target3 = 5;
            } else {
                minimum = 0.5; target1 = 1.5; target2 = 2.5; target3 = 4;
            }
            double delta = target1;
            give += Math.Min(available, delta); available -= delta;
            if (available < 0.1) goto FinishedAllocation;
            delta = target2 - target1;
            give += Math.Min(available * 0.7, delta); available -= delta / 0.7;
            if (available < 0.1) goto FinishedAllocation;
            delta = target3 - target2;
            give += Math.Min(available * 0.4, delta); available -= delta / 0.4;
            if (available < 0.1) goto FinishedAllocation;
            delta = target3;
            give += Math.Min(available * 0.15, delta); available -= delta / 0.15;
            if (available < 0.1) goto FinishedAllocation;
            FinishedAllocation:
            give = Math.Round(Math.Max(give, minimum), 1);
        } else {
            int value = Conversions.ToInteger(read("LaunchRamCustom"));
            if (value <= 12) give = value * 0.1 + 0.3;
            else if (value <= 25) give = checked(value - 12) * 0.5 + 1.5;
            else if (value <= 33) give = checked((value - 25) * 1 + 8);
            else give = checked((value - 33) * 2 + 16);
        }
        return give;
    }
}
