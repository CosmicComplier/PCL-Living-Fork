using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;

namespace PCL
{

    public static class ModLauncherUpdate
    {
        private const string Repository = ModBase.LauncherRepository;
        private const string CompatibilityRepository = "DDZmumo/LivingPCL2-Re";
        private const string ReleasePage = ModBase.LauncherReleasesUrl + "/latest";
        private static int CheckRunning;

        internal static bool NewerRelease(string Tag, string Current)
        {
            Version LatestVersion = null;
            Version CurrentVersion = null;
            if (!Version.TryParse(Tag?.TrimStart('v', 'V'), out LatestVersion) || !Version.TryParse(Current, out CurrentVersion))
                return false;
            LatestVersion = new Version(LatestVersion.Major, LatestVersion.Minor, Math.Max(0, LatestVersion.Build), Math.Max(0, LatestVersion.Revision));
            CurrentVersion = new Version(CurrentVersion.Major, CurrentVersion.Minor, Math.Max(0, CurrentVersion.Build), Math.Max(0, CurrentVersion.Revision));
            return LatestVersion > CurrentVersion;
        }

        internal static bool ValidAssetUrl(string Value)
        {
            Uri Url = null;
            return Uri.TryCreate(Value, UriKind.Absolute, out Url) && Url.Scheme == "https" && Url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) && (Url.AbsolutePath.StartsWith("/" + Repository + "/releases/download/", StringComparison.OrdinalIgnoreCase) || Url.AbsolutePath.StartsWith("/" + CompatibilityRepository + "/releases/download/", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrEmpty(Url.UserInfo) && Url.IsDefaultPort;
        }

        internal static bool ShouldPromptForUpdate(string Tag, string Current, string IgnoredTag)
        {
            return NewerRelease(Tag, Current) && !string.Equals(Tag, IgnoredTag, StringComparison.Ordinal);
        }

        internal static string IgnoreTagAfterChoice(string IgnoredTag, string Tag, int Choice)
        {
            if (Choice == 2)
                return Tag;
            if (Choice == 1 && string.Equals(IgnoredTag, Tag, StringComparison.Ordinal))
                return "";
            return IgnoredTag;
        }

        internal static JObject ReleaseExecutable(JObject Info)
        {
            JArray Assets = Info["assets"] as JArray;
            if (Assets is null)
                throw new Exception("发布中没有下载附件。");
            var Executables = Assets.OfType<JObject>().Where(Asset =>
                {
                    string Name = Asset["name"]?.ToString();
                    return Name is not null && Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
                }).ToList();
            var Candidates = Executables.Where(Asset => Asset["name"].ToString().StartsWith("PCL-Living-Fork-", StringComparison.OrdinalIgnoreCase)).ToList();
            if (Candidates.Count == 0)
                Candidates = Executables.Where(Asset => Asset["name"].ToString().StartsWith("PCL Living Fork - ", StringComparison.OrdinalIgnoreCase)).ToList();
            if (Candidates.Count == 0)
                Candidates = Executables.Where(Asset => Asset["name"].ToString().StartsWith("LivingPCL2-Re-", StringComparison.OrdinalIgnoreCase)).ToList();
            if (Candidates.Count != 1)
                throw new Exception("无法确定本版本的启动器 EXE。");
            if (!ValidAssetUrl(Candidates[0]["browser_download_url"]?.ToString()))
                throw new Exception("下载地址不属于本项目。");
            return Candidates[0];
        }

        internal static string ManifestHash(string Manifest, string Name)
        {
            foreach (var Line in Manifest.Replace(Constants.vbCr, "").Split(Constants.vbLf))
            {
                var Match = System.Text.RegularExpressions.Regex.Match(Line, @"\A([0-9a-fA-F]{64})\s+\*?(.+?)\s*\z");
                if (Match.Success && (Match.Groups[2].Value ?? "") == (Name ?? ""))
                    return Match.Groups[1].Value.ToLowerInvariant();
            }
            throw new Exception("发布附件缺少对应的 SHA256 校验值。");
        }

        internal static void CheckForUpdates(bool Startup)
        {
            if (Interlocked.CompareExchange(ref CheckRunning, 1, 0) != 0)
            {
                if (!Startup)
                    ModMain.Hint("正在检查更新，请稍候……");
                return;
            }
            if (!Startup)
                ModMain.Hint("正在检查 PCL Living Fork 的 GitHub Releases……");
            ModBase.RunInNewThread(() => { try { JObject Info = (JObject)ModNet.NetRequestByClientRetry("https://api.github.com/repos/" + Repository + "/releases/latest", Accept: "application/vnd.github+json", RequireJson: true).DeserializeJson(); string Tag = Info["tag_name"]?.ToString(); Version ParsedVersion = null; if (string.IsNullOrWhiteSpace(Tag) || !Version.TryParse(Tag.TrimStart('v', 'V'), out ParsedVersion)) throw new Exception("发布标签不是可识别的版本号。"); if ((bool)(Info["draft"] ?? false) || (bool)(Info["prerelease"] ?? false)) return; if (!NewerRelease(Tag, ModBase.VersionBaseName)) { if (!Startup) ModMain.MyMsgBox($"当前版本：{ModBase.VersionBaseName}{Constants.vbCrLf}最新正式版：{Tag}{Constants.vbCrLf}{Constants.vbCrLf}当前没有更新的正式版本。", "PCL Living Fork 更新"); return; } string IgnoredTag = Settings.Get<string>("ToolLauncherUpdateIgnoredTag"); if (Startup && !ShouldPromptForUpdate(Tag, ModBase.VersionBaseName, IgnoredTag)) return; int Choice = ModMain.MyMsgBox($"当前版本：{ModBase.VersionBaseName}{Constants.vbCrLf}最新正式版：{Tag}{Constants.vbCrLf}{Constants.vbCrLf}下载并校验更新后，启动器将自动替换并重新启动。", "发现 PCL Living Fork 新版本", "下载并更新", "暂不更新"); if (Choice == 1) { Settings.Set("ToolLauncherUpdateIgnoredTag", IgnoreTagAfterChoice(IgnoredTag, Tag, Choice)); DownloadAndUpdate(Info); } else if (Choice == 2) { Settings.Set("ToolLauncherUpdateIgnoredTag", IgnoreTagAfterChoice(IgnoredTag, Tag, Choice)); } } catch (Exception ex) { if (Startup) { Logger.Info("启动更新检查未完成，将在下次启动时重试。"); } else { UpdateFailure(ex.Message); } } finally { Interlocked.Exchange(ref CheckRunning, 0); } }, "PCL Living Fork 更新检查", ThreadPriority.BelowNormal);
        }

        private static void DownloadAndUpdate(JObject Info)
        {
            try
            {
                var Asset = ReleaseExecutable(Info);
                var HashAsset = (Info["assets"] as JArray).OfType<JObject>().SingleOrDefault(Item => Item["name"]?.ToString() == "SHA256SUMS.txt");
                if (HashAsset is null || !ValidAssetUrl(HashAsset["browser_download_url"]?.ToString()))
                    throw new Exception("发布中没有可信的校验文件。");
                string Manifest = ModNet.NetRequestByClientRetry(HashAsset["browser_download_url"].ToString());
                string ExpectedHash = ManifestHash(Manifest, Asset["name"].ToString());
                string Folder = Path.Combine(Paths.Base, "PCL", "Updates");
                Directory.CreateDirectory(Folder);
                string Staged = Path.Combine(Folder, ModBase.LauncherExecutableName);
                ModMain.Hint("正在下载 PCL Living Fork 更新……");
                ModNet.NetDownloadByLoader(Asset["browser_download_url"].ToString(), Staged, Check: new FileChecker() { Hash = ExpectedHash, ActualSize = (long)Asset["size"] });
                ValidateExecutable(Staged, ExpectedHash);
                if (ModNet.HasDownloadingTask())
                    throw new Exception("仍有下载任务正在进行。请等待任务结束后重试更新。");
                string Target = Process.GetCurrentProcess().MainModule.FileName;
                string Arguments = $"--livingpcl-update {Process.GetCurrentProcess().Id} \"{Path.GetFileName(Target)}\" {ExpectedHash}";
                var Helper = Process.Start(new ProcessStartInfo(Staged, Arguments) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = Folder });
                if (Helper is null)
                    throw new Exception("无法启动更新替换程序。");
                if (Helper.WaitForExit(500))
                    throw new Exception("更新替换程序未能正常启动。");
                ModBase.RunInUi(() => ModMain.FrmMain.EndProgram(SendWarning: false));
            }
            catch (Exception ex)
            {
                UpdateFailure(ex.Message);
            }
        }

        private static void UpdateFailure(string Reason)
        {
            if (ModMain.MyMsgBox("更新未完成，现有启动器已保留。" + Constants.vbCrLf + Constants.vbCrLf + Reason, "PCL Living Fork 更新失败", "打开发布页", "取消", IsWarn: true) == 1)
                ModBase.OpenWebsite(ReleasePage);
        }

        internal static void ValidateExecutable(string FileName, string ExpectedHash)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(ExpectedHash ?? "", @"\A[0-9a-fA-F]{64}\z"))
                throw new Exception("更新校验值无效。");
            using (var Stream = File.OpenRead(FileName))
            using (var Hash = SHA256.Create())
            {
                string Actual = BitConverter.ToString(Hash.ComputeHash(Stream)).Replace("-", "");
                if (!Actual.Equals(ExpectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new Exception("更新文件 SHA256 校验失败。");
            }
            if (!IsLivingExecutable(FileName))
                throw new Exception("更新文件不是 PCL Living Fork 启动器。");
        }

        // 兼容新旧构建的程序集名称，且产品信息须属于 LivingPCL，避免覆盖官方程序。
        internal static bool IsLivingExecutable(string FileName)
        {
            try
            {
                // Preserve the original managed candidate validation exactly.
                string Identity = AssemblyName.GetAssemblyName(FileName).Name;
                return (Identity == "LivingPCL" || Identity == "Plain Craft Launcher 2") && new[] { "LivingPCL", ModBase.LauncherDisplayName }.Contains(FileVersionInfo.GetVersionInfo(FileName).ProductName, StringComparer.Ordinal);
            }
            catch (BadImageFormatException)
            {
                // A native apphost has no managed assembly header. Its bounded
                // v6 manifest and actual entry DLL must prove LF identity; a
                // VersionInfo caption alone cannot authorize replacement.
                return ModernBundleIdentity.IsLivingExecutable(FileName, ModBase.LauncherDisplayName);
            }
        }

        // 沿用官方 PCL 的流程：运行已下载的新 EXE，由新进程替换退出后的旧 EXE。
        internal static void ReplaceExecutable(string Staged, string Target, string ExpectedHash, bool Restart)
        {
            ValidateExecutable(Staged, ExpectedHash);
            string Temporary = Target + ".updating-" + Guid.NewGuid().ToString("N");
            string Backup = Target + ".update-backup-" + Guid.NewGuid().ToString("N");
            bool Replaced = false;
            bool HadTarget = File.Exists(Target);
            if (HadTarget && !IsLivingExecutable(Target))
                throw new Exception("更新目标不是 PCL Living Fork 启动器。");
            try
            {
                File.Copy(Staged, Temporary);
                ValidateExecutable(Temporary, ExpectedHash);
                if (HadTarget)
                {
                    File.Replace(Temporary, Target, Backup, true);
                }
                else
                {
                    File.Move(Temporary, Target);
                }
                Replaced = true;
                if (Restart)
                {
                    var Started = Process.Start(new ProcessStartInfo(Target) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(Target) });
                    if (Started is null || Started.WaitForExit(3000))
                        throw new Exception("新版本启动失败。");
                }
                if (File.Exists(Backup))
                    File.Delete(Backup);
            }
            catch
            {
                if (Replaced)
                {
                    if (HadTarget && File.Exists(Backup))
                    {
                        File.Copy(Backup, Target, true);
                        File.Delete(Backup);
                    }
                    else if (!HadTarget && File.Exists(Target))
                    {
                        File.Delete(Target);
                    }
                }
                throw;
            }
            finally
            {
                if (File.Exists(Temporary))
                    File.Delete(Temporary);
            }
        }

        internal static bool HandleUpdateCommand(string[] Arguments)
        {
            if (Arguments.Length == 0 || Arguments[0] != "--livingpcl-update")
                return false;
            try
            {
                if (Arguments.Length != 4)
                    throw new Exception("更新参数不完整。");
                int ParentId;
                if (!int.TryParse(Arguments[1], out ParentId) || ParentId <= 0)
                    throw new Exception("更新进程号无效。");
                // Single-file EntryAssembly.Location is empty. Use the actual
                // running apphost, keeping the original four-argument protocol.
                string Staged = Environment.ProcessPath;
                if (string.IsNullOrEmpty(Staged)) throw new Exception("无法确定更新程序的位置。");
                string UpdateFolder = Path.GetDirectoryName(Staged);
                string PclFolder = Path.GetDirectoryName(UpdateFolder);
                if (!Path.GetFileName(UpdateFolder).Equals("Updates", StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(PclFolder).Equals("PCL", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("更新程序位置无效。");
                string Name = Arguments[2];
                if ((Path.GetFileName(Name) ?? "") != (Name ?? "") || !Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("更新目标无效。");
                string Target = Path.Combine(Path.GetDirectoryName(PclFolder), Name);
                ValidateExecutable(Staged, Arguments[3]);
                Process Parent = null;
                try
                {
                    Parent = Process.GetProcessById(ParentId);
                }
                catch (ArgumentException)
                {
                }
                if (Parent is not null)
                {
                    if (!string.Equals(Parent.MainModule.FileName, Target, StringComparison.OrdinalIgnoreCase))
                        throw new Exception("更新进程与目标不一致。");
                    if (!Parent.WaitForExit(20000))
                        throw new Exception("原启动器尚未退出。");
                }
                ReplaceExecutable(Staged, Target, Arguments[3], true);
            }
            catch (Exception ex)
            {
                if (MessageBox.Show("自动更新未完成，请从发布页手动更新。" + Constants.vbCrLf + ex.Message, "PCL Living Fork 更新失败", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(ReleasePage) { UseShellExecute = true });
                }
            }
            Environment.Exit(0);
            return true;
        }
    }
}