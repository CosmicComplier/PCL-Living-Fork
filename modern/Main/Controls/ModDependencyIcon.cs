using System.Windows.Media;
using PCLCS;

namespace PCL
{

    internal sealed class ModDependencyIcon
    {
        // 保留原图视图／异步图标宿主入口，解码与 JAR 生命周期由 C# 实现。
        internal static ImageSource Read(string FileName, string EntryName)
        {
            return ModDependencyIconFiles.Read(FileName, EntryName);
        }
    }
}