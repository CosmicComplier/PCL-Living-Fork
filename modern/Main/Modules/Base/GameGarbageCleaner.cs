using System;
using System.Collections.Generic;
using PCLCS;

namespace PCL
{
    internal sealed class GameGarbageCleaner
    {
        internal sealed class CleanupResult : LegacyGameGarbageCleanupResult
        {
        }
        private GameGarbageCleaner()
        {
        }
        internal static CleanupResult Clean(IEnumerable<string> GameFolders, Func<bool> ShouldStop = null)
        {
            return LegacyGameGarbageCleaner.Clean(GameFolders, () => new CleanupResult(), ShouldStop);
        }
    }
}