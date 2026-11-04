using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLCS;
using InspectionCore = PCLCS.ModDependencyInspection<PCL.ModDependencyInspector.InspectionContext, PCL.ModDependencyInspector.ModInfo, PCL.ModDependencyInspector.Dependency, PCL.ModDependencyInspector.DependencyEdge, PCL.ModDependencyInspector.InspectionIssue, PCL.ModDependencyInspector.RemovalImpact, PCL.ModDependencyInspector.InspectionReport, PCL.ModDependencyInspector.OnlineProjectInfo, PCL.ModDependencyInspector.OnlineDependency, PCL.ModDependencyInspector.OnlineFileInfo>;

namespace PCL
{

    // 原 LF 本地依赖类型与调用入口；声明解析、图、可靠度及版本规则由 C# 维护。
    internal sealed class ModDependencyInspector
    {
        private ModDependencyInspector()
        {
        }

        internal enum MatchState
        {
            Unknown,
            Satisfied,
            Unsatisfied
        }

        internal enum FileProviderKind
        {
            None,
            Required,
            Possible
        }

        internal enum DependencyKind
        {
            Required,
            Optional,
            Recommended,
            Suggested,
            Incompatible,
            Conflict
        }

        internal enum IssueKind
        {
            MissingRequired,
            DuplicateId,
            VersionMismatch,
            Incompatible,
            Recommendation
        }

        internal class InspectionContext : InspectionCore.ContextData
        {
        }

        internal class ModInfo : InspectionCore.ModData
        {
        }

        internal class Dependency : InspectionCore.DependencyData
        {

            public DependencyKind Kind;
            [JsonIgnore]
            public override ModDependencyKind KindValue
            {
                get
                {
                    return (ModDependencyKind)(int)Kind;
                }
                set
                {
                    Kind = (DependencyKind)(int)value;
                }
            }
        }

        internal class DependencyEdge : InspectionCore.EdgeData
        {

            public MatchState State;
            [JsonIgnore]
            public override ModDependencyMatchState StateValue
            {
                get
                {
                    return (ModDependencyMatchState)(int)State;
                }
                set
                {
                    State = (MatchState)(int)value;
                }
            }
        }

        internal class InspectionIssue : InspectionCore.IssueData
        {

            public IssueKind Kind;
            [JsonIgnore]
            public override ModDependencyIssueKind KindValue
            {
                get
                {
                    return (ModDependencyIssueKind)(int)Kind;
                }
                set
                {
                    Kind = (IssueKind)(int)value;
                }
            }
        }

        internal class RemovalImpact : InspectionCore.ImpactData
        {
        }

        internal class InspectionReport : InspectionCore.ReportData
        {
        }

        internal class OnlineProjectInfo : InspectionCore.OnlineProjectData
        {
        }

        internal class OnlineDependency : InspectionCore.OnlineDependencyData
        {
        }

        internal class OnlineFileInfo : InspectionCore.OnlineFileData
        {
        }

        private static readonly ModDependencyInstanceHost<ModMinecraft.McInstance> InstanceHost = new ModDependencyInstanceHost<ModMinecraft.McInstance>()
        {
            Reliable = new Func<ModMinecraft.McInstance, bool>(Instance => Instance.Version.Reliable),
            VanillaName = new Func<ModMinecraft.McInstance, string>(Instance => Instance.Version.VanillaName),
            ModLoaderVersions = new Func<ModMinecraft.McInstance, IEnumerable<KeyValuePair<string, string>>>(Instance => Instance.Version.ModLoaderVersions.Select(Pair => new KeyValuePair<string, string>(Pair.Key.ToString(), Pair.Value))),
            JsonObject = new Func<ModMinecraft.McInstance, JObject>(Instance => Instance.JsonObject),
            HasOptiFine = new Func<ModMinecraft.McInstance, bool>(Instance => Instance.Version.HasOptiFine),
            OptiFineAsMod = new Func<ModMinecraft.McInstance, bool>(Instance => Instance.Version.OptiFineAsMod),
            OptiFine = new Func<ModMinecraft.McInstance, string>(Instance => Instance.Version.OptiFine)
        };

        internal static InspectionContext ContextForInstance(ModMinecraft.McInstance Instance, string JavaVersion = null)
        {
            return InspectionCore.ContextForInstance(Instance, InstanceHost, JavaVersion);
        }
        internal static string FilePathKey(string FullPath)
        {
            return InspectionCore.FilePathKey(FullPath);
        }
        internal static bool SameFilePath(string First, string Second)
        {
            return InspectionCore.SameFilePath(First, Second);
        }
        internal static InspectionReport Inspect(string ModsFolder, InspectionContext Context = null, CancellationToken Token = default)
        {
            return InspectionCore.Inspect(ModsFolder, Context, Token);
        }
        internal static Dictionary<string, string> Identities(ModInfo Item)
        {
            return InspectionCore.Identities(Item);
        }
        internal static bool FileSatisfiesDependency(InspectionReport Report, string FullPath, Dependency Requirement)
        {
            return InspectionCore.FileSatisfiesDependency(Report, FullPath, Requirement);
        }
        internal static InspectionReport WithEnabledFiles(InspectionReport Report, IEnumerable<string> Paths, CancellationToken Token)
        {
            return InspectionCore.WithEnabledFiles(Report, Paths, Token);
        }
        internal static bool IsRequiredFileProvider(InspectionReport Report, DependencyEdge Edge, string FullPath)
        {
            return InspectionCore.IsRequiredFileProvider(Report, Edge, FullPath);
        }
        internal static FileProviderKind FileProviderRelation(InspectionReport Report, DependencyEdge Edge, string FullPath)
        {
            return (FileProviderKind)(int)InspectionCore.FileProviderRelation(Report, Edge, FullPath);
        }
        internal static void BuildReport(InspectionReport Report, CancellationToken Token)
        {
            InspectionCore.BuildReport(Report, Token);
        }
        internal static List<RemovalImpact> GetImpacts(InspectionReport Report, IEnumerable<string> RemovedPaths)
        {
            return InspectionCore.GetImpacts(Report, RemovedPaths);
        }
        internal static string DescribeImpacts(InspectionReport Report, IEnumerable<string> RemovedPaths)
        {
            return InspectionCore.DescribeImpacts(Report, RemovedPaths);
        }
        internal static bool DependencyMentions(Dependency Requirement, IEnumerable<string> Ids)
        {
            return InspectionCore.DependencyMentions(Requirement, Ids);
        }
        internal static MatchState MatchVersion(string Actual, string Requirement, string Syntax = "fabric")
        {
            return (MatchState)(int)InspectionCore.MatchVersion(Actual, Requirement, Syntax);
        }
    }
}