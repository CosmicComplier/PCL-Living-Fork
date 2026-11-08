using System;
using System.IO;
using MeloongCore;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// Original LF host implementation, under the repository LICENCE.
public partial class LegacyMcInstance<TSelf, TState, TCard>
    where TSelf : LegacyMcInstance<TSelf, TState, TCard>
    where TState : struct, Enum where TCard : struct, Enum {
    private bool ApplyManagedView(MmcInstanceInspection snapshot) {
        if (!snapshot.IsConsistent) {
            StateCode = 0;
            Info = snapshot.Error ?? "MMC 环境尚未通过校验，请恢复或修复实例。";
            Logo = host.ImagePath() + "Blocks/RedstoneBlock.png";
            return false;
        }
        try {
            JsonText = snapshot.ProjectionJson!;
            JsonObject = JObject.Parse(snapshot.ProjectionJson!);
            Version = new McVersion(snapshot.Manifest!.Components);
            StateCode = Version.ModLoaders.HasFlag(ModLoaders.Fabric) ? 9 :
                Version.ModLoaders.HasFlag(ModLoaders.Forge) ? 6 :
                Version.ModLoaders.HasFlag(ModLoaders.NeoForge) ? 7 : 1;
            Info = VersionDisplayName() + ", MMC";
            // Read preferences with the fixed instance identity; no ordinary
            // manifest refresh or isolation migration is allowed on this path.
            MmcIniConfiguration.WithInstanceContext(Config, () => {
                string? custom = Ini("CustomInfo");
                if (!string.IsNullOrEmpty(custom)) Info = custom;
                IsStar = bool.TryParse(Ini("IsStar", "False"), out bool star) && star;
                CardCode = int.TryParse(Ini("DisplayType", "0"), out int card) ? card : 0;
                Logo = host.ReadVersionLogo(PathVersion + "PCL\\Setup.ini", "");
            });
            if (string.IsNullOrEmpty(Logo)) Logo = host.ImagePath() + (StateCode == 6 ? "Blocks/Anvil.png" : StateCode == 9 ? "Blocks/Fabric.png" : "Blocks/Grass.png");
            return true;
        } catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or ArgumentException or Newtonsoft.Json.JsonException) {
            StateCode = 0; Info = "MMC 实例读取失败：" + error.Message;
            Logo = host.ImagePath() + "Blocks/RedstoneBlock.png";
            return false;
        }
    }
}
