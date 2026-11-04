using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using PCLCS;

namespace PCLLF.Presentation.Mods;

// Real PageInstanceMod update stages, separated from WPF binding so the same
// production plan/replacement code can be exercised with owned files.
public sealed class LegacyLocalModFiles<TMod,TProject,TVersion,TDownload>
    where TMod : LocalResourceFileData<TProject,TVersion> where TProject : ResourceProjectModelData where TVersion : ResourceVersionModelData where TDownload : class {
    private readonly IEnumerable<TMod> mods;
    public readonly List<TDownload> Files=new();
    public readonly Dictionary<string,string> Copies=new();
    public readonly List<string> FinishedFileNames=new();
    public LegacyLocalModFiles(IEnumerable<TMod> mods,Func<string> tempPath,Func<TVersion,string,TDownload> toNetFile){
        this.mods=mods;
        foreach(TMod entry in mods){
            TVersion version=entry.UpdateFile!;if(!version.DownloadAvailable)continue;
            string current=PathUtils.GetFileNameWithoutExtension(entry.ProjectVersion!.FileName!),newest=PathUtils.GetFileNameWithoutExtension(entry.UpdateFile!.FileName!);
            List<string> currentParts=current.Split('-').ToList(),newParts=newest.Split('-').ToList();bool shortened=false;
            while(true){if(!currentParts.Any()||!newParts.Any())break;if(currentParts.First()!=newParts.First())break;currentParts.RemoveAt(0);newParts.RemoveAt(0);shortened=true;}
            while(true){if(!currentParts.Any()||!newParts.Any())break;if(currentParts.Last()!=newParts.Last())break;currentParts.RemoveAt(currentParts.Count-1);newParts.RemoveAt(newParts.Count-1);shortened=true;}
            if(shortened&&currentParts.Any()&&newParts.Any()){current=string.Join("-",currentParts);newest=string.Join("-",newParts);}
            string temp=tempPath()+@"mods\"+entry.File.Name.Replace(current,newest);
            string destination=Path.Combine(PathUtils.RemoveLastPart(entry.File.FullName),entry.File.Name.Replace(current,newest));
            Files.Add(toNetFile(version,temp));Copies[temp]=destination;
        }
    }
    public void Replace(Action captureHistory){
        // Original history capture is outside the cancellation catch.
        captureHistory();
        try{
            foreach(TMod entry in mods){if(FileUtils.Exists(entry.File.FullName))FileUtils.Delete(entry.File.FullName,true);else Logger.Warn($"未找到更新前的 Mod 文件，跳过对它的删除：{entry.File.FullName}");}
            foreach(KeyValuePair<string,string> entry in Copies){if(DirectoryUtils.Exists(PathUtils.RemoveLastPart(entry.Value))){FileUtils.Move(entry.Key,entry.Value);FinishedFileNames.Add(PathUtils.GetLastPart(entry.Value));}else Logger.Warn($"更新后的目标文件夹已被删除：{entry.Value}");}
        }catch(OperationCanceledException error){Logger.Warn(error,"替换旧版 Mod 文件时被主动取消");}
    }
    public void Clean(){try{foreach(string file in Copies.Keys)FileUtils.Delete(file);}catch(Exception error){Logger.Warn(error,"清理 Mod 更新缓存失败");}}
}
public static class LegacyLocalModText {
    public static string Compare<TMod,TProject,TVersion>(TMod entry,Func<TimeSpan,bool,string> formatTime)
        where TMod : LocalResourceFileData<TProject,TVersion> where TProject : ResourceProjectModelData where TVersion : ResourceVersionModelData {
        string current=(entry.ProjectVersion!.Version??entry.ProjectVersion.Display)!.Replace(".jar","");string newest=(entry.UpdateFile!.Version??entry.UpdateFile.Display)!.Replace(".jar","");
        List<string> currentSegments=current.Split("-_+ ".ToCharArray()).ToList(),newSegments=newest.Split("-_+ ".ToCharArray()).ToList();bool shortened=false;
        foreach(string segment in currentSegments.ToList()){if(!newSegments.Contains(segment))continue;currentSegments.Remove(segment);newSegments.Remove(segment);shortened=true;}
        if(shortened&&currentSegments.Any()&&newSegments.Any()){current=string.Join("-",currentSegments);newest=string.Join("-",newSegments);entry._Version=current;}
        return $"当前版本：{current}（{formatTime(entry.ProjectVersion.ReleaseDate-DateTime.Now,false)}）\r\n最新版本：{newest}（{formatTime(entry.UpdateFile.ReleaseDate-DateTime.Now,false)}）";
    }
}
