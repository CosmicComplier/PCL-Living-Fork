using System.Threading;
using Inspector = PCL.ModDependencyInspector;
using PCLCS;

namespace PCL
{

    // 原静态入口保持；实际会话去重、指纹与已禁用前置启用规划使用同一 C# 服务。
    internal sealed class LaunchDependencyCheck
    {
        internal static void SettingsChanged()
        {
            ModDependencyAutoFill.Service.SettingsChanged();
        }
        internal static string Fingerprint(Inspector.InspectionReport Report)
        {
            return ModDependencyAutoFill.Service.Fingerprint(Report);
        }
        internal static bool WasAccepted(string Folder, Inspector.InspectionReport Report)
        {
            return ModDependencyAutoFill.Service.WasAccepted(Folder, Report);
        }
        internal static void Accept(string Folder, Inspector.InspectionReport Report)
        {
            ModDependencyAutoFill.Service.Accept(Folder, Report);
        }
    }

    internal sealed class ModDependencyActivation
    {
        internal class Item : DependencyActivationItemData<Inspector.ModInfo>
        {
        }
        internal static string IssueKey(Inspector.InspectionIssue Issue)
        {
            return ModDependencyAutoFill.Service.IssueKey(Issue);
        }
        internal static ModDependencyAutoFill.Plan Prepare(string Root, Inspector.InspectionReport Report, CancellationToken Token)
        {
            return ModDependencyAutoFill.Service.Prepare(Root, Report, Token);
        }
    }
}