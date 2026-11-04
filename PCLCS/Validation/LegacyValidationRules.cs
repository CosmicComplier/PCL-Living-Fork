using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLCS.Validation;

// Behavioral source: LF f4aa0fae62c3ee8af24c81e61844c8fdd13c8983,
// Plain Craft Launcher 2/Modules/Base/ModValidate.vb, SHA256
// 56128d0b9a951280680bfcbd183acc49c771651a3e46a517dcaa9a041c4f88e7.
// That module is identical in PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 PCL.Core/Utils/Validate
// was reviewed as a C# organization reference only (CE Apache LICENSE/PCL LICENCE).
// Its FluentValidation rules, lengths, messages, reserved names and root policy
// differ; none of those policy changes is reused. LF repository LICENCE applies.
public static class LegacyValidation {
    public static string Validate<TRule>(string? text, IEnumerable<TRule> rules) where TRule : LegacyValidationRule {
        string? result = "";
        foreach (TRule rule in rules) {
            result = rule.Validate(text);
            if (result is null) return "";
            if (result != "") return result;
        }
        return result;
    }
}

/// <summary>Null means stop the remaining ordered rules and accept, as in LF.</summary>
public abstract class LegacyValidationRule {
    public abstract string? Validate(string? value);
}

public sealed class LegacyValidateNullable : LegacyValidationRule {
    public override string? Validate(string? value) => string.IsNullOrEmpty(value) ? null : "";
}
public sealed class LegacyValidateNullOrEmpty : LegacyValidationRule {
    public override string Validate(string? value) => string.IsNullOrEmpty(value) ? "输入内容不能为空！" : "";
}
public sealed class LegacyValidateNullOrWhiteSpace : LegacyValidationRule {
    public override string Validate(string? value) => string.IsNullOrWhiteSpace(value) ? "输入内容不能为空！" : "";
}
public sealed class LegacyValidateFunc : LegacyValidationRule {
    private readonly Func<string?, string?> function;
    public LegacyValidateFunc(Func<string?, string?> function) => this.function = function;
    public override string? Validate(string? value) => function(value);
}
public sealed class LegacyValidateRegex : LegacyValidationRule {
    public string? Regex { get; set; }
    public string? ErrorDescription { get; set; } = "正则检查失败！";
    public LegacyValidateRegex() { }
    public LegacyValidateRegex(string? regex, string? errorDescription = "正则检查失败！") {
        Regex = regex; ErrorDescription = errorDescription;
    }
    public override string? Validate(string? value) => StringExtensions.RegexCheck(value!, Regex!) ? "" : ErrorDescription;
}
public sealed class LegacyValidateHttp : LegacyValidationRule {
    public override string Validate(string? value) {
        if (StringExtensions.EndsWithF(value, "/")) value = value!.Substring(0, value.Length - 1);
        return StringExtensions.RegexCheck(value!, "^(http[s]?)\\://") ? "" : "输入的网址无效！";
    }
}
public sealed class LegacyValidateInteger : LegacyValidationRule {
    public int Min { get; set; }
    public int Max { get; set; } = int.MaxValue;
    public LegacyValidateInteger() { }
    public LegacyValidateInteger(int min, int max) { Min = min; Max = max; }
    public override string Validate(string? value) {
        if (value!.Length > 9) return "请输入一个大小合理的数字！";
        if (!int.TryParse(value, out int parsed)) return "请输入一个整数！";
        if (parsed > Max) return "不可超过 " + Max + "！";
        if (parsed < Min) return "不可低于 " + Min + "！";
        return "";
    }
}
public sealed class LegacyValidateLength : LegacyValidationRule {
    public int Min { get; set; }
    public int Max { get; set; } = int.MaxValue;
    public LegacyValidateLength() { }
    public LegacyValidateLength(int min, int max = int.MaxValue) { Min = min; Max = max; }
    public override string Validate(string? value) {
        int length = value?.Length ?? 0; // Microsoft.VisualBasic.Len(String Nothing) is zero.
        if (length != Max && Max == Min) return $"长度必须为 {Max} 个字符！";
        if (length > Max) return $"长度最长为 {Max} 个字符！";
        if (length < Min) return $"长度至少需 {Min} 个字符！";
        return "";
    }
}
public sealed class LegacyValidateExcept : LegacyValidationRule {
    public Collection<string?> Excepts { get; set; } = new();
    public string? ErrorMessage { get; set; } = "输入内容不能包含 %";
    public LegacyValidateExcept() { }
    public LegacyValidateExcept(Collection<string?> excepts, string? errorMessage = "输入内容不能包含 %") {
        Excepts = excepts; ErrorMessage = errorMessage;
    }
    public LegacyValidateExcept(IEnumerable<string?> excepts, string? errorMessage = "输入内容不能包含 %")
        : this(new Collection<string?>(new List<string?>(excepts)), errorMessage) { }
    public LegacyValidateExcept(IEnumerable<char> excepts, string? errorMessage = "输入内容不能包含 %")
        : this(new Collection<string?>(excepts.Select(character => (string?)character.ToString()).ToList()), errorMessage) { }
    public override string Validate(string? value) {
        foreach (string? fragment in Excepts) {
            if (StringExtensions.IndexOfF(value!, fragment!, true) >= 0) {
                if (ErrorMessage is null) ErrorMessage = "";
                return ErrorMessage.Replace("%", fragment);
            }
        }
        return "";
    }
}
public sealed class LegacyValidateExceptSame : LegacyValidationRule {
    public Collection<string?> Excepts { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool IgnoreCase { get; set; }
    public LegacyValidateExceptSame() { }
    public LegacyValidateExceptSame(Collection<string?> excepts, string? errorMessage = "输入内容不能为 %", bool ignoreCase = false) {
        Excepts = excepts; ErrorMessage = errorMessage; IgnoreCase = ignoreCase;
    }
    public LegacyValidateExceptSame(IEnumerable<string?> excepts, string? errorMessage = "输入内容不能为 %", bool ignoreCase = false)
        : this(new Collection<string?>(new List<string?>(excepts)), errorMessage, ignoreCase) { }
    public LegacyValidateExceptSame(string? except, string? errorMessage = "输入内容不能为 %", bool ignoreCase = false)
        : this(new Collection<string?> { except }, errorMessage, ignoreCase) { }
    public override string Validate(string? value) {
        if (value is null) return ErrorMessage!.Replace("%", "null");
        foreach (string? fragment in Excepts) {
            bool same = IgnoreCase
                ? string.Equals(StringExtensions.Lower(value), StringExtensions.Lower(fragment!), StringComparison.Ordinal)
                : value.Equals(fragment);
            if (same) return ErrorMessage!.Replace("%", fragment);
        }
        return "";
    }
}
public sealed class LegacyValidateSame : LegacyValidationRule {
    public string? Required { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IgnoreCase { get; set; }
    public LegacyValidateSame() { }
    public LegacyValidateSame(string? required, string? errorMessage = "输入内容有误", bool ignoreCase = false) {
        Required = required; ErrorMessage = errorMessage; IgnoreCase = ignoreCase;
    }
    public override string? Validate(string? value) => string.Compare(value, Required, IgnoreCase) == 0 ? "" : ErrorMessage;
}

public sealed class LegacyValidateFolderName : LegacyValidationRule {
    public string? Folder { get; set; }
    public bool UseMinecraftCharCheck { get; set; } = true;
    private readonly IEnumerable<string>? pathIgnore;
    private readonly List<string>? ignoreList;
    public LegacyValidateFolderName() { }
    public LegacyValidateFolderName(string? folder, bool useMinecraftCharCheck = true, List<string>? ignoreList = null) {
        Folder = folder; UseMinecraftCharCheck = useMinecraftCharCheck;
        // Original On Error Resume Next covers this statement, then continues
        // assigning IgnoreList even if eager directory discovery failed.
        try { pathIgnore = DirectoryUtils.EnumerateDirectories(folder!); } catch (Exception) { }
        this.ignoreList = ignoreList;
    }
    // The original VB facade supplies this captured enumerable through its
    // On Error Resume Next adapter, retaining the thread-local VB Err contract.
    public LegacyValidateFolderName(string? folder, bool useMinecraftCharCheck, List<string>? ignoreList, IEnumerable<string>? capturedPaths) {
        Folder = folder; UseMinecraftCharCheck = useMinecraftCharCheck;
        pathIgnore = capturedPaths; this.ignoreList = ignoreList;
    }
    public override string Validate(string? value) {
        try {
            string lengthCheck = new LegacyValidateNullOrWhiteSpace().Validate(value);
            if (lengthCheck != "") return lengthCheck;
            if (StringExtensions.StartsWithF(value, " ")) return "文件夹名不能以空格开头！";
            if (StringExtensions.EndsWithF(value, " ")) return "文件夹名不能以空格结尾！";
            lengthCheck = new LegacyValidateLength(1, 100).Validate(value);
            if (lengthCheck != "") return lengthCheck;
            if (StringExtensions.EndsWithF(value, ".")) return "文件夹名不能以小数点结尾！";
            string characterCheck = new LegacyValidateExcept(LegacyValidationFileRules.InvalidCharacters(UseMinecraftCharCheck), "文件夹名不可包含 % 字符！").Validate(value);
            if (characterCheck != "") return characterCheck;
            string reservedCheck = new LegacyValidateExceptSame(LegacyValidationFileRules.ReservedNames, "文件夹名不可为 %！", true).Validate(value);
            if (reservedCheck != "") return reservedCheck;
            if (StringExtensions.RegexCheck(value!, ".{2,}~\\d")) return "文件夹名不能包含这一特殊格式！";
            List<string> names = new();
            if (pathIgnore is not null) {
                foreach (string folderPath in pathIgnore) {
                    string folderName = PathUtils.GetLastPart(folderPath);
                    if (ignoreList is null || !ignoreList.Contains(folderName, StringComparer.OrdinalIgnoreCase)) names.Add(folderName);
                }
            }
            string duplicateCheck = new LegacyValidateExceptSame(names, "不可与现有文件夹重名！", true).Validate(value);
            if (duplicateCheck != "") return duplicateCheck;
            return "";
        } catch (Exception error) {
            Logger.Warn(error, "检查文件夹名出错");
            return "错误：" + error.Message;
        }
    }
}
public sealed class LegacyValidateFileName : LegacyValidationRule {
    public string? Name { get; set; }
    public bool UseMinecraftCharCheck { get; set; } = true;
    public bool IgnoreCase { get; set; } = true;
    public string? ParentFolder { get; set; }
    public object? RequireParentFolderExists { get; set; } = true;
    public LegacyValidateFileName() { }
    public LegacyValidateFileName(string? name, bool useMinecraftCharCheck = true, bool ignoreCase = true) {
        Name = name; IgnoreCase = ignoreCase; UseMinecraftCharCheck = useMinecraftCharCheck;
    }
    public override string Validate(string? value) {
        try {
            string lengthCheck = new LegacyValidateNullOrWhiteSpace().Validate(value);
            if (lengthCheck != "") return lengthCheck;
            if (StringExtensions.StartsWithF(value, " ")) return "文件名不能以空格开头！";
            if (StringExtensions.EndsWithF(value, " ")) return "文件名不能以空格结尾！";
            lengthCheck = new LegacyValidateLength(1, 253).Validate(value + (ParentFolder ?? ""));
            if (lengthCheck != "") return lengthCheck;
            if (StringExtensions.EndsWithF(value, ".")) return "文件名不能以小数点结尾！";
            string characterCheck = new LegacyValidateExcept(LegacyValidationFileRules.InvalidCharacters(UseMinecraftCharCheck), "文件名不可包含 % 字符！").Validate(value);
            if (characterCheck != "") return characterCheck;
            string reservedCheck = new LegacyValidateExceptSame(LegacyValidationFileRules.ReservedNames, "文件名不可为 %！", true).Validate(value);
            if (reservedCheck != "") return reservedCheck;
            if (StringExtensions.RegexCheck(value!, ".{2,}~\\d")) return "文件名不能包含这一特殊格式！";
            if (ParentFolder is not null) {
                DirectoryInfo parent = DirectoryUtils.GetInfo(ParentFolder);
                if (parent.Exists) {
                    string duplicateCheck = new LegacyValidateExceptSame(parent.EnumerateFiles("*").Select(file => file.Name), "不可与现有文件重名！", IgnoreCase).Validate(value);
                    if (duplicateCheck != "") return duplicateCheck;
                } else if (Conversions.ToBoolean(RequireParentFolderExists)) return $"父文件夹不存在：{ParentFolder}";
            }
            return "";
        } catch (Exception error) {
            Logger.Warn(error, "检查文件名出错");
            return "错误：" + error.Message;
        }
    }
}
public sealed class LegacyValidateFolderPath : LegacyValidationRule {
    public bool UseMinecraftCharCheck { get; set; } = true;
    public LegacyValidateFolderPath() { }
    public LegacyValidateFolderPath(bool useMinecraftCharCheck) => UseMinecraftCharCheck = useMinecraftCharCheck;
    public override string Validate(string? value) {
        value = value!.Replace("/", "\\");
        if (!StringExtensions.EndsWithF(PathUtils.RemoveSlashSuffix(value), ":")) value = PathUtils.RemoveSlashSuffix(value);
        string lengthCheck = new LegacyValidateNullOrWhiteSpace().Validate(value);
        if (lengthCheck != "") return lengthCheck;
        lengthCheck = new LegacyValidateLength(1, 254).Validate(value);
        if (lengthCheck != "") return lengthCheck;
        string? match = StringExtensions.RegexSeek(value, ".{2,}~\\d");
        if (match is not null) return $"文件夹名不能包含 {match} 这一特殊格式！";
        bool validRoot = StringExtensions.StartsWithF(value, "\\\\Mac\\");
        if (!validRoot) {
            foreach (string drive in Environment.GetLogicalDrives().Where(DirectoryUtils.Exists)) {
                string driveName = StringExtensions.Upper(drive).First() + ":\\";
                if (StringExtensions.Upper(value) == driveName) return "";
                if (StringExtensions.StartsWithF(value, driveName, true)) { validRoot = true; break; }
            }
        }
        if (!validRoot) return "文件夹路径头存在错误！";
        string[] segments = value.Split(new[] { '\\' }, StringSplitOptions.None);
        for (int index = StringExtensions.StartsWithF(value, "\\\\Mac\\") ? 2 : 1; index < segments.Length; index++) {
            string segment = segments[index];
            string segmentLength = new LegacyValidateNullOrWhiteSpace().Validate(segment);
            if (segmentLength != "") return "文件夹路径存在错误！";
            string characterCheck = new LegacyValidateExcept(LegacyValidationFileRules.InvalidCharacters(UseMinecraftCharCheck), "路径中存在无效字符！").Validate(segment);
            if (characterCheck != "") return characterCheck;
            if (StringExtensions.StartsWithF(segment, " ")) return "文件夹名不能以空格开头！";
            if (StringExtensions.EndsWithF(segment, " ")) return "文件夹名不能以空格结尾！";
            if (StringExtensions.EndsWithF(segment, ".")) return "文件夹名不能以小数点结尾！";
            // LF path segments deliberately use the case-sensitive default here.
            string reservedCheck = new LegacyValidateExceptSame(LegacyValidationFileRules.ReservedNames, "文件夹名不可为 %！").Validate(segment);
            if (reservedCheck != "") return reservedCheck;
        }
        return "";
    }
}
internal static class LegacyValidationFileRules {
    internal static readonly string[] ReservedNames = {
        "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };
    internal static string InvalidCharacters(bool minecraft) => new string(Path.GetInvalidFileNameChars()) + (minecraft ? "!;" : "");
}
