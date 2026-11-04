using System;
using System.Collections.Generic;
using MeloongCore;
using PCLCS.Validation;

namespace PCL
{

    // LF 原校验类型和 XAML/调用面；全部规则与有序执行位于 C#。
    public static class ModValidate
    {
        public static string Validate(string Text, IEnumerable<Validate> ValidateRules)
        {
            return LegacyValidation.Validate(Text, ValidateRules);
        }
    }

    public abstract class Validate : LegacyValidationRule
    {
        // The real Core abstract Validate method is inherited: C# cannot redeclare
        // a same-named member in class Validate. Public LF type identity is preserved.
    }

    public class ValidateNullable : Validate
    {
        private readonly LegacyValidateNullable Rule;
        public ValidateNullable()
        {
            Rule = new LegacyValidateNullable();
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateNullOrEmpty : Validate
    {
        private readonly LegacyValidateNullOrEmpty Rule;
        public ValidateNullOrEmpty()
        {
            Rule = new LegacyValidateNullOrEmpty();
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateNullOrWhiteSpace : Validate
    {
        private readonly LegacyValidateNullOrWhiteSpace Rule;
        public ValidateNullOrWhiteSpace()
        {
            Rule = new LegacyValidateNullOrWhiteSpace();
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateHttp : Validate
    {
        private readonly LegacyValidateHttp Rule;
        public ValidateHttp()
        {
            Rule = new LegacyValidateHttp();
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateFunc : Validate
    {
        private readonly LegacyValidateFunc Rule;
        public ValidateFunc(Func<string, string> F)
        {
            Rule = new LegacyValidateFunc(F);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateRegex : Validate
    {
        private readonly LegacyValidateRegex Rule;
        public string Regex
        {
            get
            {
                return Rule.Regex;
            }
            set
            {
                Rule.Regex = value;
            }
        }
        public string ErrorDescription
        {
            get
            {
                return Rule.ErrorDescription;
            }
            set
            {
                Rule.ErrorDescription = value;
            }
        }
        public ValidateRegex()
        {
            Rule = new LegacyValidateRegex();
        }
        public ValidateRegex(string Regex, string ErrorDescription = "正则检查失败！")
        {
            Rule = new LegacyValidateRegex(Regex, ErrorDescription);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateInteger : Validate
    {
        private readonly LegacyValidateInteger Rule;
        public int Min
        {
            get
            {
                return Rule.Min;
            }
            set
            {
                Rule.Min = value;
            }
        }
        public int Max
        {
            get
            {
                return Rule.Max;
            }
            set
            {
                Rule.Max = value;
            }
        }
        public ValidateInteger()
        {
            Rule = new LegacyValidateInteger();
        }
        public ValidateInteger(int Min, int Max)
        {
            Rule = new LegacyValidateInteger(Min, Max);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateLength : Validate
    {
        private readonly LegacyValidateLength Rule;
        public int Min
        {
            get
            {
                return Rule.Min;
            }
            set
            {
                Rule.Min = value;
            }
        }
        public int Max
        {
            get
            {
                return Rule.Max;
            }
            set
            {
                Rule.Max = value;
            }
        }
        public ValidateLength()
        {
            Rule = new LegacyValidateLength();
        }
        public ValidateLength(int Min, int Max = int.MaxValue)
        {
            Rule = new LegacyValidateLength(Min, Max);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateExcept : Validate
    {
        private readonly LegacyValidateExcept Rule;
        public System.Collections.ObjectModel.Collection<string> Excepts
        {
            get
            {
                return Rule.Excepts;
            }
            set
            {
                Rule.Excepts = value;
            }
        }
        public string ErrorMessage
        {
            get
            {
                return Rule.ErrorMessage;
            }
            set
            {
                Rule.ErrorMessage = value;
            }
        }
        public ValidateExcept()
        {
            Rule = new LegacyValidateExcept();
        }
        public ValidateExcept(System.Collections.ObjectModel.Collection<string> Excepts, string ErrorMessage = "输入内容不能包含 %")
        {
            Rule = new LegacyValidateExcept(Excepts, ErrorMessage);
        }
        public ValidateExcept(IEnumerable<string> Excepts, string ErrorMessage = "输入内容不能包含 %")
        {
            Rule = new LegacyValidateExcept(Excepts, ErrorMessage);
        }
        public ValidateExcept(IEnumerable<char> Excepts, string ErrorMessage = "输入内容不能包含 %")
        {
            Rule = new LegacyValidateExcept(Excepts, ErrorMessage);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateExceptSame : Validate
    {
        private readonly LegacyValidateExceptSame Rule;
        public System.Collections.ObjectModel.Collection<string> Excepts
        {
            get
            {
                return Rule.Excepts;
            }
            set
            {
                Rule.Excepts = value;
            }
        }
        public string ErrorMessage
        {
            get
            {
                return Rule.ErrorMessage;
            }
            set
            {
                Rule.ErrorMessage = value;
            }
        }
        public bool IgnoreCase
        {
            get
            {
                return Rule.IgnoreCase;
            }
            set
            {
                Rule.IgnoreCase = value;
            }
        }
        public ValidateExceptSame()
        {
            Rule = new LegacyValidateExceptSame();
        }
        public ValidateExceptSame(System.Collections.ObjectModel.Collection<string> Excepts, string ErrorMessage = "输入内容不能为 %", bool IgnoreCase = false)
        {
            Rule = new LegacyValidateExceptSame(Excepts, ErrorMessage, IgnoreCase);
        }
        public ValidateExceptSame(IEnumerable<string> Excepts, string ErrorMessage = "输入内容不能为 %", bool IgnoreCase = false)
        {
            Rule = new LegacyValidateExceptSame(Excepts, ErrorMessage, IgnoreCase);
        }
        public ValidateExceptSame(string Except, string ErrorMessage = "输入内容不能为 %", bool IgnoreCase = false)
        {
            Rule = new LegacyValidateExceptSame(Except, ErrorMessage, IgnoreCase);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateSame : Validate
    {
        private readonly LegacyValidateSame Rule;
        public string Required
        {
            get
            {
                return Rule.Required;
            }
            set
            {
                Rule.Required = value;
            }
        }
        public string ErrorMessage
        {
            get
            {
                return Rule.ErrorMessage;
            }
            set
            {
                Rule.ErrorMessage = value;
            }
        }
        public bool IgnoreCase
        {
            get
            {
                return Rule.IgnoreCase;
            }
            set
            {
                Rule.IgnoreCase = value;
            }
        }
        public ValidateSame()
        {
            Rule = new LegacyValidateSame();
        }
        public ValidateSame(string Required, string ErrorMessage = "输入内容有误", bool IgnoreCase = false)
        {
            Rule = new LegacyValidateSame(Required, ErrorMessage, IgnoreCase);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateFolderName : Validate
    {
        private readonly LegacyValidateFolderName Rule;
        public string Folder
        {
            get
            {
                return Rule.Folder;
            }
            set
            {
                Rule.Folder = value;
            }
        }
        public bool UseMinecraftCharCheck
        {
            get
            {
                return Rule.UseMinecraftCharCheck;
            }
            set
            {
                Rule.UseMinecraftCharCheck = value;
            }
        }
        public ValidateFolderName()
        {
            Rule = new LegacyValidateFolderName();
        }
        public ValidateFolderName(string Folder, bool UseMinecraftCharCheck = true, List<string> IgnoreList = null)
        {
            Rule = new LegacyValidateFolderName(Folder, UseMinecraftCharCheck, IgnoreList, CapturePaths(Folder));
        }
        private static IEnumerable<string> CapturePaths(string Folder)
        {
            // Original On Error Resume Next: clear a prior Err on entry, retain a
            // new error on failure and return Nothing; do not enumerate eagerly.
            Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError();
            try { return DirectoryUtils.EnumerateDirectories(Folder); }
            catch (Exception error) { Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(error); return null; }
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateFileName : Validate
    {
        private readonly LegacyValidateFileName Rule;
        public string Name
        {
            get
            {
                return Rule.Name;
            }
            set
            {
                Rule.Name = value;
            }
        }
        public bool UseMinecraftCharCheck
        {
            get
            {
                return Rule.UseMinecraftCharCheck;
            }
            set
            {
                Rule.UseMinecraftCharCheck = value;
            }
        }
        public bool IgnoreCase
        {
            get
            {
                return Rule.IgnoreCase;
            }
            set
            {
                Rule.IgnoreCase = value;
            }
        }
        public string ParentFolder
        {
            get
            {
                return Rule.ParentFolder;
            }
            set
            {
                Rule.ParentFolder = value;
            }
        }
        public object RequireParentFolderExists
        {
            get
            {
                return Rule.RequireParentFolderExists;
            }
            set
            {
                Rule.RequireParentFolderExists = value;
            }
        }
        public ValidateFileName()
        {
            Rule = new LegacyValidateFileName();
        }
        public ValidateFileName(string Name, bool UseMinecraftCharCheck = true, bool IgnoreCase = true)
        {
            Rule = new LegacyValidateFileName(Name, UseMinecraftCharCheck, IgnoreCase);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }

    public class ValidateFolderPath : Validate
    {
        private readonly LegacyValidateFolderPath Rule;
        public bool UseMinecraftCharCheck
        {
            get
            {
                return Rule.UseMinecraftCharCheck;
            }
            set
            {
                Rule.UseMinecraftCharCheck = value;
            }
        }
        public ValidateFolderPath()
        {
            Rule = new LegacyValidateFolderPath();
        }
        public ValidateFolderPath(bool UseMinecraftCharCheck)
        {
            Rule = new LegacyValidateFolderPath(UseMinecraftCharCheck);
        }
        public override string Validate(string Str)
        {
            return Rule.Validate(Str);
        }
    }
}