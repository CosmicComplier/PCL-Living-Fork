using System;
using System.IO;

namespace PCL
{

    internal sealed class MinecraftFolderPolicy
    {
        internal static string PackFolder(string LauncherFolder)
        {
            return Path.GetFullPath(Path.Combine(LauncherFolder, ".minecraft")).TrimEnd('\\') + @"\";
        }

        private static string Normalize(string Value)
        {
            if (Value.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            {
                Value = @"\\" + Value.Substring(8);
            }
            else if (Value.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                Value = Value.Substring(4);
            }
            return Path.GetFullPath(Value.Replace("/", @"\")).TrimEnd('\\');
        }

        internal static bool IsPackVersion(string LauncherFolder, string InstanceFolder)
        {
            if (string.IsNullOrWhiteSpace(InstanceFolder))
                return false;
            try
            {
                return string.Equals(Path.GetDirectoryName(Normalize(InstanceFolder)), Normalize(Path.Combine(PackFolder(LauncherFolder), "versions")), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is NotSupportedException)
            {
                return false;
            }
        }

    }
}