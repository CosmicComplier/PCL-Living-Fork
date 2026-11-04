using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices;
using MeloongCore;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class MinecraftInstallEngine<TLoader, TNetFile, TInstance, TState, TCard, TOptiFine, TForgeLike, TForge, TNeo, TLite, TLib, TResource, TRequest> {
    private List<TNetFile> ForgelikeFiles(TForgeLike info, string target, bool installing) {
        if (info.IsNeoForge) {
            TNeo neo = (TNeo)info;
            string url = MinecraftDownloadEntries.NeoForgeUrlBase(neo, neo.ApiName) + "-installer.jar";
            return new() { File(new[] { url.Replace("maven.neoforged.net/releases", "bmclapi2.bangbang93.com/maven"), url }, target, new MinecraftDownloadCheck { MinSize = 64 * 1024 }) };
        }
        TForge forge = (TForge)info;
        string inherit = installing ? forge.Inherit.Replace("-", "_") : forge.Inherit;
        string name = inherit + "-" + forge.FileVersion + "/forge-" + inherit + "-" + forge.FileVersion + "-" + forge.Category + "." + forge.FileExtension;
        return new() { File(new[] { "https://bmclapi2.bangbang93.com/maven/net/minecraftforge/forge/" + name, "https://files.minecraftforge.net/maven/net/minecraftforge/forge/" + name }, target, new MinecraftDownloadCheck { MinSize = 64 * 1024, Hash = forge.Hash }) };
    }
    public List<TLoader> McDownloadForgelikeLoader(bool isNeoForge, string loaderVersion, string newInstanceName, string inherit, TForgeLike? info, string mcFolder, TLoader clientDownloadLoader, string clientFolder) {
        if (isNeoForge && info is null) info = host.NeoForge(Same(inherit, "1.20.1") && !loaderVersion.StartsWithF("1.20.1-") ? "1.20.1-" + loaderVersion : loaderVersion);
        if (!isNeoForge && loaderVersion.StartsWithF("1.") && loaderVersion.Contains("-")) { inherit = loaderVersion.BeforeFirst("-"); loaderVersion = loaderVersion.AfterLast("-"); }
        string loaderName = isNeoForge ? "NeoForge" : "Forge", installerAddress = host.RequestTempFolder(true) + "forge_installer.jar", versionFolder = mcFolder + "versions\\" + newInstanceName + "\\";
        List<TLoader> loaders = new();
        if (info is null) loaders.Add(Task<string, string>("获取 " + loaderName + " 详细信息", task => {
            List<TForge> versions = host.ForgeList(inherit); task.Progress = 0.8;
            foreach (TForge version in versions) if (McUtils.CompareVersion(version.Version.ToString(), loaderVersion) == 0) { info = version; return; }
            throw new Exception("未能找到 " + loaderName + " " + inherit + "-" + loaderVersion + " 的详细信息！");
        }, 3));
        loaders.Add(Task<string, List<TNetFile>>("准备下载 " + loaderName, task => task.Output = ForgelikeFiles(info!, installerAddress, true), 0.5, false));
        loaders.Add(Download("下载 " + loaderName + " 主文件", new(), 9));
        string currentMcFolder = host.MinecraftFolder();
        if (isNeoForge || Conversions.ToDouble(loaderVersion.BeforeFirst(".")) >= 20) {
            Logger.Info("检测为" + (isNeoForge ? " Neo" : "新版 ") + "Forge：" + loaderVersion);
            List<TLib>? libraries = null;
            loaders.Add(Task<string, List<TNetFile>>("分析 " + loaderName + " 支持库文件", task => {
                task.Output = new();
                ZipArchive? installer = null;
                try {
                    installer = FileUtils.OpenZip(installerAddress); task.Progress = 0.2;
                    JObject json = (JObject)installer.GetEntry("install_profile.json")!.Open().ReadString().DeserializeJson()!;
                    JObject json2 = (JObject)installer.GetEntry("version.json")!.Open().ReadString().DeserializeJson()!; json.Merge(json2);
                    libraries = host.ReadLibraryList(json, true, currentMcFolder);
                    if (json["data"] is not null && json["data"]!["MOJMAPS"] is not null) {
                        task.Progress = 0.4;
                        JObject raw = (JObject)host.LoaderRequest(host.LauncherUrls(host.ClientJsonUrl(inherit)), true).DeserializeJson()!;
                        string original = json["data"]!["MOJMAPS"]!["client"]!.ToString().Trim("[]".ToCharArray()).BeforeFirst("@");
                        string address = Path.ChangeExtension(host.LibraryPath(original, currentMcFolder), "." + json["data"]!["MOJMAPS"]!["client"]!.ToString().Trim("[]".ToCharArray()).Split('@')[1]);
                        JToken mappings = raw["downloads"]!["client_mappings"]!;
                        libraries.Add(new TLib { IsNatives = false, LocalPath = address, OriginalName = original, Url = (string?)mappings["url"], Size = (long)mappings["size"]!, SHA1 = (string?)mappings["sha1"] });
                        Logger.Info("需要下载 Mappings：" + mappings["url"] + " (SHA1: " + mappings["sha1"] + ")");
                    }
                    task.Progress = 0.8;
                    for (int index = 0; index <= libraries.Count - 1; index++) {
                        if (libraries[index].LocalPath.EndsWithF(loaderName.Lower() + "-" + inherit + "-" + loaderVersion + ".jar") || libraries[index].LocalPath.EndsWithF(loaderName.Lower() + "-" + inherit + "-" + loaderVersion + "-client.jar")) {
                            Logger.Warn("已从待下载 " + loaderName + " 支持库中移除：" + libraries[index].LocalPath); libraries.RemoveAt(index); break;
                        }
                    }
                    task.Output = host.LibraryNetFiles(libraries, currentMcFolder);
                } catch (Exception error) { throw new Exception("获取" + (isNeoForge ? " Neo" : "新版 ") + "Forge 支持库列表失败", error); }
                finally { installer?.Dispose(); }
            }, 1));
            loaders.Add(Download("下载 " + loaderName + " 支持库文件", new(), 12));
            loaders.Add(Task<List<TNetFile>, bool>("获取 " + loaderName + " 支持库文件", task => {
                foreach (TLib library in libraries!) {
                    string realPath = Path.Combine(mcFolder, library.LocalPath.Substring(currentMcFolder.Length));
                    if (!FileUtils.Exists(realPath)) FileUtils.Copy(library.LocalPath, realPath);
                    Logger.Trace("复制的 " + loaderName + " 支持库文件：" + library.LocalPath);
                }
                WaitAndCopyVanilla(loaderName, mcFolder, inherit, clientDownloadLoader, clientFolder, task);
            }, 0.1, false));
            loaders.Add(Task<bool, bool>(isNeoForge ? "安装 NeoForge" : "安装 Forge（方式 A）", task => {
                try {
                    Logger.Info("开始进行 Forgelike 安装：" + installerAddress);
                    List<string> oldList = DirectoryUtils.EnumerateDirectories(mcFolder + "versions\\").ToList();
                    DirectoryUtils.Create(versionFolder); task.Progress = 0.04; host.CreateLauncherProfiles(mcFolder); task.Progress = 0.05;
                    bool wrapper = true;
                    while (true) {
                        try {
                            host.ExtractResources(host.TempFolder() + "Cache\\forge_installer.jar", "ForgeInstaller"); task.Progress = 0.06;
                            ForgelikeInjector(installerAddress, task, mcFolder, wrapper, isNeoForge); task.Progress = 0.97; break;
                        } catch (Exception error) {
                            if (wrapper) { Logger.Warn(error, "使用 JavaWrapper 安装 " + loaderName + " 失败，将不使用 JavaWrapper 并重试"); wrapper = false; }
                            else throw new Exception("运行 " + loaderName + " 安装器失败", error);
                        }
                    }
                    // LF uses SkipWhile, including its original order sensitivity.
                    List<DirectoryInfo> delta = DirectoryUtils.EnumerateDirectories(mcFolder + "versions\\").SkipWhile(path => oldList.Contains(path)).Select(DirectoryUtils.GetInfo).ToList();
                    if (delta.Count > 1) delta = delta.Where(folder => folder.Name.ContainsIgnoreCase("forge") && folder.EnumerateFiles().Any()).ToList();
                    if (delta.Count == 1) {
                        FileInfo json = delta[0].EnumerateFiles().First(); FileUtils.Copy(json.FullName, versionFolder + newInstanceName + ".json");
                        Logger.Info("已复制新增的版本 JSON 文件：" + json.FullName + " -> " + versionFolder + newInstanceName + ".json");
                    } else if (delta.Count > 1) Logger.Info("有多个疑似的新增版本，无法确定：" + string.Join(";", delta.Select(folder => folder.Name)));
                    else Logger.Info("未找到新增的版本文件夹");
                } catch (Exception error) { throw new Exception("安装新 " + loaderName + " 版本失败", error); }
                finally { try { FileUtils.Delete(installerAddress); } catch (Exception error) { Logger.Warn(error, "安装 " + loaderName + " 清理文件时出错"); } }
            }, 10));
        } else {
            Logger.Info("检测为非新版 Forge：" + loaderVersion);
            loaders.Add(Task<List<TNetFile>, bool>("安装 " + loaderName + "（方式 B）", task => {
                ZipArchive? installer = null;
                try {
                    installer = FileUtils.OpenZip(installerAddress); task.Progress = 0.2;
                    JObject json = (JObject)installer.GetEntry("install_profile.json")!.Open().ReadString().DeserializeJson()!;
                    task.Progress = 0.4; DirectoryUtils.Create(versionFolder); task.Progress = 0.5;
                    if (json["install"] is null) {
                        Logger.Info("开始进行 Forge 安装，Legacy 方式 1：" + installerAddress);
                        JObject version = (JObject)installer.GetEntry(json["json"]!.ToString().TrimStart('/'))!.Open().ReadString().DeserializeJson()!;
                        version["id"] = newInstanceName; FileUtils.Write(versionFolder + newInstanceName + ".json", version.ToString()); task.Progress = 0.6;
                        installer.Dispose(); FileUtils.ExtractToDirectory(installerAddress, installerAddress + "_unrar\\");
                        if (DirectoryUtils.Exists(installerAddress + "_unrar\\maven\\")) DirectoryUtils.Copy(installerAddress + "_unrar\\maven\\", mcFolder + "libraries\\");
                        DirectoryUtils.Delete(installerAddress + "_unrar\\");
                    } else {
                        Logger.Info("开始进行 Forge 安装，Legacy 方式 2：" + installerAddress);
                        string jar = host.LibraryPath((string)json["install"]!["path"]!, mcFolder);
                        FileUtils.Write(jar, installer.GetEntry((string)json["install"]!["filePath"]!)!.Open()); task.Progress = 0.9;
                        json["versionInfo"]!["id"] = newInstanceName;
                        if (json["versionInfo"]!["inheritsFrom"] is null) ((JObject)json["versionInfo"]!).Add("inheritsFrom", inherit);
                        FileUtils.Write(versionFolder + newInstanceName + ".json", json["versionInfo"]!.ToString());
                    }
                } catch (Exception error) { throw new Exception("非新版方式安装 Forge 失败", error); }
                finally {
                    try { installer?.Dispose(); FileUtils.Delete(installerAddress); DirectoryUtils.Delete(installerAddress + "_unrar\\"); }
                    catch (Exception error) { Logger.Warn(error, "非新版方式安装 Forge 清理文件时出错"); }
                }
            }, 1));
        }
        return loaders;
    }
}
