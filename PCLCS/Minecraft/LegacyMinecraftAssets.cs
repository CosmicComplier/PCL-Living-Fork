using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class LegacyMinecraftService<TInstance, TFolder, TLibrary, TNetFile, TState, TCard, TFolderKind>
    where TInstance : LegacyMcInstance<TInstance, TState, TCard>
    where TFolder : LegacyMinecraftFolder<TFolderKind>, new() where TLibrary : LegacyMinecraftLibrary, new()
    where TNetFile : class where TState : struct, Enum where TCard : struct, Enum where TFolderKind : struct, Enum {
    public JToken McAssetsGetIndex(TInstance instance, bool returnLegacyOnError = false, bool checkUrlEmpty = false) {
        string? assetsName = null;
        try {
            while (true) {
                JToken? index = instance.JsonObject["assetIndex"];
                if (index != null && index["id"] != null) return index;
                if (instance.JsonObject["assets"] != null) assetsName = instance.JsonObject["assets"]!.ToString();
                if (checkUrlEmpty && index!["url"] != null) return index;
                if (string.IsNullOrEmpty(instance.InheritName)) break;
                instance = host.Instance.CreateInstance(McFolderSelected + "versions\\" + instance.InheritName);
            }
        } catch { }
        if (returnLegacyOnError) {
            Logger.Info("无法获取资源文件索引下载地址，使用默认的 legacy 下载地址");
            return "{\n \"id\":\"legacy\",\n \"sha1\":\"c0fd82e8ce9fbc93119e40d96d5a4e62cfa3f729\",\n \"size\":134284,\n \"url\":\"https://launchermeta.mojang.com/mc-staging/assets/legacy/c0fd82e8ce9fbc93119e40d96d5a4e62cfa3f729/legacy.json\",\n \"totalSize\":111220701\n}".DeserializeJson()!;
        }
        throw new Exception("该版本不存在资源文件索引信息");
    }

    public string McAssetsGetIndexName(TInstance instance) {
        try {
            while (true) {
                if (instance.JsonObject["assetIndex"] != null && instance.JsonObject["assetIndex"]!["id"] != null)
                    return instance.JsonObject["assetIndex"]!["id"]!.ToString();
                if (instance.JsonObject["assets"] != null) return instance.JsonObject["assets"]!.ToString();
                if (string.IsNullOrEmpty(instance.InheritName)) break;
                instance = host.Instance.CreateInstance(McFolderSelected + "versions\\" + instance.InheritName);
            }
        } catch (Exception ex) { Logger.Warn(ex, "获取资源文件索引名失败"); }
        return "legacy";
    }

    private struct McAssetsToken {
        public string LocalPath;
        public string SourcePath;
        public long Size;
        public string Hash;
        public override readonly string ToString() => StringUtils.FormatByteSize(Size) + " | " + LocalPath;
    }
    private List<McAssetsToken> McAssetsListGet(TInstance instance) {
        string indexName = McAssetsGetIndexName(instance);
        try {
            if (!FileUtils.Exists($"{McFolderSelected}assets\\indexes\\{indexName}.json"))
                throw new FileNotFoundException("未找到 Asset Index", McFolderSelected + "assets\\indexes\\" + indexName + ".json");
            List<McAssetsToken> result = [];
            JObject json = (JObject)FileUtils.ReadAsJson($"{McFolderSelected}assets\\indexes\\{indexName}.json")!;
            foreach (JProperty file in json["objects"]!.Children()) {
                string localPath;
                if (json["map_to_resources"] != null && json["map_to_resources"]!.ToObject<bool>()) {
                    localPath = $"{instance.PathIndie}resources\\{file.Name}";
                } else if (json["virtual"] != null && json["virtual"]!.ToObject<bool>()) {
                    localPath = $"{McFolderSelected}assets\\virtual\\legacy\\{file.Name}";
                } else {
                    localPath = $"{McFolderSelected}assets\\objects\\{Strings.Left(file.Value["hash"]!.ToString(), 2)}\\{file.Value["hash"]}";
                }
                result.Add(new McAssetsToken {
                    LocalPath = localPath.Replace("/", "\\"), SourcePath = file.Name,
                    Hash = file.Value["hash"]!.ToString(), Size = Conversions.ToLong(file.Value["size"]!.ToString())
                });
            }
            return result;
        } catch (Exception ex) {
            Logger.Warn(ex, $"获取资源文件列表失败：{indexName}");
            throw;
        }
    }

    public List<TNetFile> McAssetsFixList(TInstance instance, bool checkHash, Action<double>? progressFeed = null) {
        if (checkHash) return McAssetsListGet(instance).Select(token => host.CreateNetFile(
            host.AssetSources($"https://resources.download.minecraft.net/{Strings.Left(token.Hash, 2)}/{token.Hash}"), token.LocalPath,
            new FileChecker { ActualSize = token.Size == 0 ? -1 : token.Size, Hash = token.Hash })).ToList();
        List<TNetFile> result = [];
        try {
            List<McAssetsToken> assetsList = McAssetsListGet(instance);
            progressFeed?.Invoke(0.04);
            for (int i = 0; i < assetsList.Count; i++) {
                McAssetsToken token = assetsList[i];
                progressFeed?.Invoke(0.05 + 0.94 * i / assetsList.Count);
                FileInfo file = FileUtils.GetInfo(token.LocalPath);
                if (file.Exists && (token.Size == 0 || token.Size == file.Length)) continue;
                result.Add(host.CreateNetFile(host.AssetSources($"https://resources.download.minecraft.net/{Strings.Left(token.Hash, 2)}/{token.Hash}"),
                    token.LocalPath, new FileChecker { ActualSize = token.Size == 0 ? -1 : token.Size, Hash = token.Hash }));
            }
        } catch (Exception ex) { Logger.Warn(ex, "获取版本缺失的资源文件下载列表失败"); }
        progressFeed?.Invoke(0.99);
        return result;
    }
}
