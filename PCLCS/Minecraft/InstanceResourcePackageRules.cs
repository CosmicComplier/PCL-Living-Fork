using System.IO;
using System.IO.Compression;
using System.Linq;

namespace PCLCS;

/// <summary>LF 原资源包／光影包结构检查；不读取或解析条目内容。</summary>
public static class InstanceResourcePackageRules {
    // LF f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 InstanceResourceStore.ValidatePackage。
    public static void ValidatePackage(string fileName, int kind) {
        using (ZipArchive archive = OpenRead(fileName)) {
            if (kind == 2 && archive.GetEntry("pack.mcmeta") is null)
                throw new IOException("资源包缺少 pack.mcmeta，未替换原文件。");
            if (kind == 3 && !archive.Entries.Any(entry => InstanceResourcePathText.StartsWithPathText(entry.FullName, "shaders/")))
                throw new IOException("光影包缺少 shaders 目录，未替换原文件。");
        }
    }

    internal static ZipArchive OpenRead(string fileName) {
        LegacyPathCharacters.Check(fileName);
#if !NETFRAMEWORK
        // 旧 FileStream 公有构造器先经 GetFileName 检查原串，Init 再归约完整路径。
        // 不移除前缀或去除目录尾斜线；null／empty 保留 ZipFile 自身参数异常。
        if (Path.DirectorySeparatorChar == '\\' && !string.IsNullOrEmpty(fileName))
            fileName = InstanceResourcePathCompatibility.GetFullPath(fileName);
#endif
        return ZipFile.OpenRead(fileName);
    }
}
