using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MeloongCore;
using MeloongCore.Extensions;

namespace PCLCS;

public sealed partial class MinecraftInstallEngine<TLoader, TNetFile, TInstance, TState, TCard, TOptiFine, TForgeLike, TForge, TNeo, TLite, TLib, TResource, TRequest> {
    // The original two independent output handlers and canceled-state-only kill check
    // are retained. No silent-process cancellation policy or exit-code policy is added.
    private void RunInstallerProcess(ProcessStartInfo info, string loaderName, Func<bool> canceled, Action<string> consume, Action finish) {
        Process process = new() { StartInfo = info };
        using AutoResetEvent output = new(false);
        using AutoResetEvent error = new(false);
        void Receive(DataReceivedEventArgs args, AutoResetEvent completed, bool standardError) {
            try { if (args.Data is null) completed.Set(); else consume(args.Data); }
            catch (ObjectDisposedException) { }
            catch (Exception exception) { Logger.Warn(exception, "读取 " + loaderName + " 安装器" + (standardError ? "错误信息失败" : "信息失败")); }
            try { if (canceled() && !process.HasExited) { Logger.Info("由于任务取消，已中止 " + loaderName + " 安装"); process.Kill(); } } catch { }
        }
        process.OutputDataReceived += (_, args) => Receive(args, output, false);
        process.ErrorDataReceived += (_, args) => Receive(args, error, true);
        process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
        while (!process.HasExited) Thread.Sleep(10);
        output.WaitOne(10000); error.WaitOne(10000);
        process.Dispose(); finish();
    }
    public void McDownloadOptiFineInstall(string baseMcFolderHome, string target, MinecraftInstallTask<List<TNetFile>, bool, TLoader> task, bool useJavaWrapper) {
        ValueRange<Version> javaRange = ValueRange<Version>.AtLeast(new Version(21, 0));
        try {
            using var installer = FileUtils.OpenZip(target);
            var installerClass = installer.GetEntry("optifine/Installer.class") ?? throw new FileNotFoundException("未找到 optifine/Installer.class");
            using Stream stream = installerClass.Open();
            using BinaryReader reader = new(stream);
            byte[] header = reader.ReadBytes(8);
            if (header.Length < 8) throw new IOException("Installer.class 长度不足");
            if (header[0] != 0xCA || header[1] != 0xFE || header[2] != 0xBA || header[3] != 0xBE) throw new InvalidDataException("Installer.class 文件头无效");
            int version = checked(header[6] * 0x100 + header[7]);
            if (version < 49) throw new InvalidDataException("Installer.class 版本过低");
            if (version > 100) throw new InvalidDataException("Installer.class 版本过高");
            javaRange = ValueRange<Version>.AtLeast(new Version(version - 44, 0));
            Logger.Info("OptiFine 安装器 class 版本：" + version + "，需要 Java " + javaRange);
        } catch (Exception error) { Logger.Warn(error, "读取 OptiFine 安装器 Java 版本失败"); }
        Java? java = host.SelectJava(javaRange, task.Cancellation(), task.ProgressProvider(0, 0.2));
        if (task.IsCanceled) return;
        if (java is null) throw new OperationCanceledException();
        string arguments = useJavaWrapper && !host.BooleanSetting("LaunchAdvanceDisableJLW")
            ? "-Doolloo.jlw.tmpdir=\"" + PathUtils.RemoveSlashSuffix(host.PureFolder()) + "\" -Duser.home=\"" + PathUtils.RemoveSlashSuffix(baseMcFolderHome) + "\" -cp \"" + target + "\" -jar \"" + LaunchUtils.ExtractPatch("Launch\\JavaWrapper.jar", host.PureFolder()) + "\" optifine.Installer"
            : "-Duser.home=\"" + PathUtils.RemoveSlashSuffix(baseMcFolderHome) + "\" -cp \"" + target + "\" optifine.Installer";
        if (java.Version!.Major >= 9) arguments = "--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED " + arguments;
        lock (InstallSyncLock) {
            ProcessStartInfo info = new() { FileName = java.JavaExePath, Arguments = arguments, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true, WorkingDirectory = PathUtils.ToShortPath(baseMcFolderHome) };
            if (info.EnvironmentVariables.ContainsKey("appdata")) info.EnvironmentVariables["appdata"] = baseMcFolderHome; else info.EnvironmentVariables.Add("appdata", baseMcFolderHome);
            Logger.Info("开始安装 OptiFine：" + target);
            int total = 0; string last = "";
            RunInstallerProcess(info, "OptiFine", () => task.IsCanceledState, line => { last = line; Logger.Trace(last); total = checked(total + 1); task.Progress += 0.7 / 7000; },
                () => { if (total < 1000 || last.Contains("at ")) throw new Exception("安装器运行出错，末行为 " + last); });
        }
    }
    public void ForgelikeInjector(string target, MinecraftInstallTask<bool, bool, TLoader> task, string mcFolder, bool useJavaWrapper, bool isNeoForge) {
        Java? java = host.SelectJava(ValueRange<Version>.AtLeast(new Version(8, 0, 60)), task.Cancellation(), task.ProgressProvider(0, 0.2));
        if (task.IsCanceled) return;
        if (java is null) throw new OperationCanceledException();
        string arguments = useJavaWrapper && !host.BooleanSetting("LaunchAdvanceDisableJLW")
            ? "-Doolloo.jlw.tmpdir=\"" + PathUtils.RemoveSlashSuffix(host.PureFolder()) + "\" -cp \"" + host.TempFolder() + "Cache\\forge_installer.jar;" + target + "\" -jar \"" + LaunchUtils.ExtractPatch("Launch\\JavaWrapper.jar", host.PureFolder()) + "\" com.bangbang93.ForgeInstaller \"" + mcFolder
            : "-cp \"" + host.TempFolder() + "Cache\\forge_installer.jar;" + target + "\" com.bangbang93.ForgeInstaller \"" + mcFolder;
        if (java.Version!.Major >= 9) arguments = "--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED " + arguments;
        lock (InstallSyncLock) {
            ProcessStartInfo info = new() { FileName = java.JavaExePath, Arguments = arguments, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
            string name = isNeoForge ? "NeoForge" : "Forge";
            Logger.Info("开始安装 " + name + "：" + arguments);
            Queue<string> last = new();
            RunInstallerProcess(info, name, () => task.IsCanceledState, line => { last.Enqueue(line); if (last.Count > 100) last.Dequeue(); ForgelikeInjectorLine(line, task); }, () => {
                if (last.Reverse().Take(5).Any(line => Same(line, "true"))) return;
                Logger.Warn(string.Join("\r\n", last)); string tail = "";
                for (int index = Math.Max(0, last.Count - 5); index <= last.Count - 1; index++) tail += "\r\n" + last.ElementAt(index);
                throw new Exception(name + " 安装器出错，日志结束部分为：" + tail);
            });
        }
    }
    public void ForgelikeInjectorLine(string content, MinecraftInstallTask<bool, bool, TLoader> task) {
        switch (content) {
            case "Extracting json": task.Progress = 0.27; break;
            case "Downloading libraries": task.Progress = 0.28; break;
            case "  File exists: Checksum validated.": task.Progress += 0.003; break;
            case "Building Processors": task.Progress = 0.38; break;
            case "Task: DOWNLOAD_MOJMAPS": task.Progress = 0.4; break;
            case "Task: MERGE_MAPPING": task.Progress = 0.5; break;
            case "Splitting: ": task.Progress = 0.55; break;
            case "Parameter Annotations": task.Progress = 0.6; break;
            case "Processing Complete": case "log: null": task.Progress = 0.67; break;
            case "Sorting": task.Progress = 0.8; break;
            case "Remapping final jar": task.Progress = 0.85; break;
            case "Remapping jar... 50%": task.Progress = 0.9; break;
            case "Remapping jar... 100%": task.Progress = 0.95; break;
            case "Injecting profile": task.Progress = 0.98; break;
            default: Logger.Trace(content); return;
        }
        Logger.Info(content);
    }
}
