using System.Collections.Generic;
using PCLCS;

namespace PCL
{
    internal sealed class InstalledComponentSummary
    {
        internal sealed class Item : InstalledComponentSummaryItemData
        {
            internal Item(string Name, string Version, string Icon) : base(Name, Version, Icon)
            {
            }
        }
        internal static readonly InstalledComponentSummaryEngine<Item> Engine = new InstalledComponentSummaryEngine<Item>((Name, Version, Icon) => new Item(Name, Version, Icon));
        internal static List<Item> Describe(McVersion Version, McComponents Components = null, IDictionary<string, string> LocalMods = null)
        {
            return Engine.Describe(Version, Components, LocalMods);
        }
        internal static Dictionary<string, string> ReadLocalMods(string ModsFolder)
        {
            return InstalledComponentSummaryEngine<Item>.ReadLocalMods(ModsFolder);
        }
    }
}