using System.Collections.Generic;
using System.Threading;
using MeloongCore;
using PCLCS;

namespace PCL
{
    internal sealed class InstanceCloneGroupNode : InstanceCloneGroupNodeData<InstanceCloneGroupNode>
    {
    }

    internal sealed class InstanceCloneGroupSet : InstanceCloneGroupSetData<InstanceCloneGroupNode>
    {
        internal InstanceCloneGroupNode Find(string VersionFolder)
        {
            return InstanceCloneGroups.Service.Find(this, VersionFolder);
        }
    }

    // 只为显示解析来源关系；迁移仍必须通过 InstanceCloner 自身的完整校验。
    internal sealed class InstanceCloneGroups
    {
        internal static readonly InstanceCloneGroupsEngine<InstanceCloneGroupNode, InstanceCloneGroupSet> Service = new InstanceCloneGroupsEngine<InstanceCloneGroupNode, InstanceCloneGroupSet>(PathUtils.RemoveExtendedPrefix, InstanceCloner.EnsureNoLinks, () => new InstanceCloneGroupNode(), () => new InstanceCloneGroupSet());
        internal static string Key(string VersionFolder)
        {
            return Service.Key(VersionFolder);
        }
        internal static InstanceCloneGroupSet Build(IEnumerable<string> VisibleFolders, CancellationToken Token)
        {
            return Service.Build(VisibleFolders, Token);
        }
        internal static List<InstanceCloneGroupNode> Flatten(InstanceCloneGroupNode Node)
        {
            return Service.Flatten(Node);
        }
        internal static bool Contains(InstanceCloneGroupNode Node, string VersionFolder)
        {
            return Service.Contains(Node, VersionFolder);
        }
    }
}