using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MeloongCore;
using MeloongCore.Extensions;
using PCLCS;

namespace PCL
{

    internal class InstanceKeybinding : InstanceKeybindingData
    {
    }

    internal static class InstanceKeybindings
    {
        internal static List<InstanceKeybinding> ReadBindings(string GameFolder, CancellationToken Token)
        {
            return InstanceKeybindingReader.ReadBindings(GameFolder, Token, (Root, FileName) => InstanceResourceStore.EnsureWithin(Root, FileName), (ErrorValue, Message) => Logger.Warn(ErrorValue, Message), Title => Title.ReplaceLineEndings(" ", false), () => new InstanceKeybinding()).Cast<InstanceKeybinding>().ToList();
        }
        internal static void Save(string GameFolder, string Key, string Previous, string Replacement)
        {
            InstanceKeybindingFiles.Save(GameFolder, Key, Previous, Replacement, LocalGameData.Gate, () => ModWatcher.HasRunningMinecraft, (Root, FileName) => InstanceResourceStore.EnsureWithin(Root, FileName), ModBase.IniClearCache);
        }
        private static List<InstanceKeybinding> DefaultBindings()
        {
            return InstanceKeybindingReader.DefaultBindings(() => new InstanceKeybinding()).Cast<InstanceKeybinding>().ToList();
        }
    }
}