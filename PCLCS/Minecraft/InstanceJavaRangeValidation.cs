using System;
using MeloongCore;
namespace PCLCS;
public static class InstanceJavaRangeValidation {
    public static string Validate(string text,string leftQuote,string rightQuote) {
        try {
            if (string.IsNullOrWhiteSpace(text)) return "不能为空";
            ValueRange<Version> range = ValueRange<Version>.FromString(text,StringUtils.ParseVersionWithDefaults);
            if (range.IsEmpty()) return "范围下限的版本号比上限更高，导致该范围无法匹配到任何版本";
            static bool OldStyle(Version v) => v.Major <= 1 && (v.Minor > 0 || v.Build > 0 || v.Revision > 0);
            if (range.HasLower && OldStyle(range.Lower!) || range.HasUpper && OldStyle(range.Upper!)) return "不应使用 1.x 格式进行匹配（例如，若想匹配 Java 17，应填写 17 而非 1.17）";
            if (range.HasUpper && range.Upper!.Major <= 4) return "该范围所要求的 Java 版本过低";
            if (range.HasUpper && range.IsUpperInclusive && range.Upper!.Minor <= 0 && range.Upper.Build <= 0) return $"右侧闭区间的意图并不明确。如果不想允许 Java {range.Upper.Major}，请改为 {leftQuote}{range.Upper.Major}){rightQuote}。如果想允许 Java {range.Upper.Major}，请改为 {leftQuote}{checked(range.Upper.Major + 1)}){rightQuote}。";
            if (range.Intersect(ValueRange<Version>.OpenClosed(new Version(5,0),new Version(99,0))).IsEmpty()) return "该范围无法匹配到任何常见的 Java";
            return "";
        } catch (Exception ex) { return ex.Message; }
    }
}
