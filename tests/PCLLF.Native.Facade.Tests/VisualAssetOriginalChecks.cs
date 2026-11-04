namespace PCLLF.Native.Facade.Tests;
internal static class VisualAssetOriginalChecks
{
    internal static readonly Dictionary<string, Dictionary<int,int>> Minimum = new(StringComparer.Ordinal)
    {
        ["gallery"] = new() { [61] = 1, [66] = 1, [68] = 1, [70] = 1, [72] = 1, [74] = 1, [76] = 1, [83] = 1, [87] = 1 },
        ["background"] = new() { [17] = 1, [21] = 1, [34] = 2, [35] = 2, [37] = 2, [38] = 2, [40] = 4, [51] = 1, [53] = 1 },
        ["thumbnail"] = new() { [118] = 1, [121] = 1, [123] = 1, [130] = 1, [133] = 1, [135] = 1, [137] = 1, [140] = 1, [145] = 1, [149] = 1, [151] = 1, [153] = 1, [159] = 1, [161] = 1, [165] = 1, [168] = 1 },
        ["icon"] = new() { [33] = 1, [38] = 13, [40] = 13, [107] = 65, [111] = 8, [116] = 2, [117] = 2, [122] = 8, [125] = 1, [127] = 1, [134] = 6, [161] = 1, [162] = 3, [163] = 1, [164] = 1, [165] = 1, [166] = 1, [173] = 8, [181] = 1, [192] = 1, [193] = 1, [194] = 1, [200] = 2 },
    };
    internal static readonly Dictionary<string, HashSet<int>> Expected = Minimum.ToDictionary(pair => pair.Key, pair => pair.Value.Keys.ToHashSet(), StringComparer.Ordinal);
}
