using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MeloongCore.Extensions;

namespace PCLCS;

public partial class ResourceProjectModelData {
    // LF's title/suffix rules. The same-source CE CompProject C# routine is a reference,
    // but its localized strings and loader policy do not replace LF's original captions.
    public (string? Title, string SubTitle) GetControlTitle(bool hasModLoaderDescription) {
        string? title = RawName;
        List<string> subtitleList;
        if (Same(TranslatedName, RawName)) {
            List<string> nameLists = TranslatedName!.Split(new[] { " | ", " - ", "(", ")", "[", "]", "{", "}" }, true)
                .Select(value => value.Trim(" /\\".ToCharArray())).Where(value => !string.IsNullOrEmpty(value)).ToList();
            if (nameLists.Count == 1) return (title, "");
            subtitleList = new List<string>();
            List<string> normalNameList = new();
            foreach (string name in nameLists) {
                string lowerName = name.ToLowerInvariant();
                if (name == name.ToUpperInvariant() && name != "FPS" && name != "HUD") subtitleList.Add(name);
                else if (new[] { "neoforge", "forge", "fabric", "quilt" }.Any(lowerName.Contains) &&
                    !Regex.IsMatch(lowerName.Replace("neoforge", "").Replace("forge", "").Replace("fabric", "").Replace("quilt", ""), "[a-z]+")) subtitleList.Add(name);
                else normalNameList.Add(name);
            }
            if (!normalNameList.Any() || !subtitleList.Any()) return (title, "");
            title = string.Join(" - ", normalNameList);
        } else {
            title = TranslatedName!.Contains(" (") ? TranslatedName.BeforeFirst(" (") : TranslatedName.BeforeLast(" - ");
            string suffix = "";
            if (TranslatedName.AfterLast(")").Contains(" - ")) suffix = TranslatedName.AfterLast(")").AfterLast(" - ");
            string englishName = TranslatedName;
            if (suffix != "") englishName = englishName.Replace(" - " + suffix, "");
            englishName = englishName.Replace(title, "").Trim('(', ')', ' ');
            subtitleList = englishName.Split(new[] { " | ", " - ", "(", ")", "[", "]", "{", "}" }, true)
                .Select(value => value.Trim(" /".ToCharArray())).Where(value => !string.IsNullOrEmpty(value)).ToList();
            if (subtitleList.Count > 1 && !subtitleList.Any(value => value.ToLowerInvariant().Contains("forge") || value.ToLowerInvariant().Contains("fabric") || value.ToLowerInvariant().Contains("quilt")) &&
                !(subtitleList.Count == 2 && subtitleList.Last().ToUpperInvariant() == subtitleList.Last())) subtitleList = new List<string> { englishName };
            if (suffix != "") subtitleList.Add(suffix);
        }
        subtitleList = subtitleList.Distinct().ToList();
        string subtitle = "";
        foreach (string initial in subtitleList) {
            string value = initial;
            bool loaderDescription = value.ToLowerInvariant().Contains("neoforge") || value.ToLowerInvariant().Contains("forge") || value.ToLowerInvariant().Contains("fabric") || value.ToLowerInvariant().Contains("quilt");
            if (!hasModLoaderDescription && loaderDescription) continue;
            if (value.Length < 16 && value.ToLowerInvariant().Contains("fabric") && value.ToLowerInvariant().Contains("forge")) continue;
            if (loaderDescription && !value.Contains("版") && value.ToLowerInvariant().Replace("neoforge", "").Replace("forge", "").Replace("fabric", "").Replace("quilt", "").Length <= 3)
                value = value.Replace("Edition", "").Replace("edition", "").Trim().Capitalize() + " 版";
            value = value.Replace("neoforge", "NeoForge").Replace("forge", "Forge").Replace("neo", "Neo").Replace("fabric", "Fabric").Replace("quilt", "Quilt");
            subtitle += "  |  " + value.Trim();
        }
        return (title, subtitle);
    }
}
