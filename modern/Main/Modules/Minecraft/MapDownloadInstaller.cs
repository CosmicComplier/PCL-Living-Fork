using System.Threading;
using MeloongCore;
using PCLCS;

namespace PCL
{

    internal sealed class MapDownloadInstaller
    {
        private static readonly MapDownloadFiles Files = new MapDownloadFiles(PathUtils.RemoveExtendedPrefix);
        internal const string DownloadFolderName = MapDownloadFiles.DownloadFolderName;

        internal static string GetDownloadFolder(string SavesFolder)
        {
            return Files.GetDownloadFolder(SavesFolder);
        }

        internal static string Install(string ArchivePath, string SavesFolder, CancellationToken Token)
        {
            return Files.Install(ArchivePath, SavesFolder, Token);
        }
    }
}