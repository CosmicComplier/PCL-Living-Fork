using System;
using System.Collections.Generic;
using System.Globalization;

namespace PCLCS;

/// <summary>Shared cache identity for platform projects and their version lists.</summary>
public static class ResourceProjectKey {
    public static string Get(ResourcePlatforms platform, string projectId) {
        if (platform != ResourcePlatforms.CurseForge && platform != ResourcePlatforms.Modrinth) throw new ArgumentOutOfRangeException(nameof(platform));
        if (string.IsNullOrEmpty(projectId)) throw new ArgumentException("项目 ID 不能为空。", nameof(projectId));
        if (platform == ResourcePlatforms.CurseForge && long.TryParse(projectId, NumberStyles.None, CultureInfo.InvariantCulture, out long numeric))
            projectId = numeric.ToString(CultureInfo.InvariantCulture);
        return ((int)platform).ToString(CultureInfo.InvariantCulture) + ":" + projectId;
    }
    // Older callers only know the project ID. Remove every provider plus any old raw key.
    public static void Invalidate<T>(IDictionary<string, T> cache, string projectId) {
        cache.Remove(projectId);
        cache.Remove(Get(ResourcePlatforms.CurseForge, projectId));
        cache.Remove(Get(ResourcePlatforms.Modrinth, projectId));
    }
}
