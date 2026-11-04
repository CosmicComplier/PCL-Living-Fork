Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports PCLCS.Validation
Imports MeloongCore

'LF 原校验类型和 XAML/调用面；全部规则与有序执行位于 C#。
Public Module ModValidate
    Public Function Validate(Text As String, ValidateRules As IEnumerable(Of Validate)) As String
        Return LegacyValidation.Validate(Text, ValidateRules)
    End Function
End Module

Public MustInherit Class Validate
    Inherits LegacyValidationRule
    Public MustOverride Overrides Function Validate(Str As String) As String
End Class

Public Class ValidateNullable
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateNullable
    Public Sub New()
        Rule = New LegacyValidateNullable()
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateNullOrEmpty
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateNullOrEmpty
    Public Sub New()
        Rule = New LegacyValidateNullOrEmpty()
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateNullOrWhiteSpace
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateNullOrWhiteSpace
    Public Sub New()
        Rule = New LegacyValidateNullOrWhiteSpace()
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateHttp
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateHttp
    Public Sub New()
        Rule = New LegacyValidateHttp()
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateFunc
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateFunc
    Public Sub New(F As Func(Of String, String))
        Rule = New LegacyValidateFunc(F)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateRegex
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateRegex
    Public Property Regex As String
        Get
            Return Rule.Regex
        End Get
        Set(Value As String)
            Rule.Regex = Value
        End Set
    End Property
    Public Property ErrorDescription As String
        Get
            Return Rule.ErrorDescription
        End Get
        Set(Value As String)
            Rule.ErrorDescription = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateRegex()
    End Sub
    Public Sub New(Regex As String, Optional ErrorDescription As String = "正则检查失败！")
        Rule = New LegacyValidateRegex(Regex, ErrorDescription)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateInteger
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateInteger
    Public Property Min As Integer
        Get
            Return Rule.Min
        End Get
        Set(Value As Integer)
            Rule.Min = Value
        End Set
    End Property
    Public Property Max As Integer
        Get
            Return Rule.Max
        End Get
        Set(Value As Integer)
            Rule.Max = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateInteger()
    End Sub
    Public Sub New(Min As Integer, Max As Integer)
        Rule = New LegacyValidateInteger(Min, Max)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateLength
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateLength
    Public Property Min As Integer
        Get
            Return Rule.Min
        End Get
        Set(Value As Integer)
            Rule.Min = Value
        End Set
    End Property
    Public Property Max As Integer
        Get
            Return Rule.Max
        End Get
        Set(Value As Integer)
            Rule.Max = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateLength()
    End Sub
    Public Sub New(Min As Integer, Optional Max As Integer = Integer.MaxValue)
        Rule = New LegacyValidateLength(Min, Max)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateExcept
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateExcept
    Public Property Excepts As ObjectModel.Collection(Of String)
        Get
            Return Rule.Excepts
        End Get
        Set(Value As ObjectModel.Collection(Of String))
            Rule.Excepts = Value
        End Set
    End Property
    Public Property ErrorMessage As String
        Get
            Return Rule.ErrorMessage
        End Get
        Set(Value As String)
            Rule.ErrorMessage = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateExcept()
    End Sub
    Public Sub New(Excepts As ObjectModel.Collection(Of String), Optional ErrorMessage As String = "输入内容不能包含 %")
        Rule = New LegacyValidateExcept(Excepts, ErrorMessage)
    End Sub
    Public Sub New(Excepts As IEnumerable(Of String), Optional ErrorMessage As String = "输入内容不能包含 %")
        Rule = New LegacyValidateExcept(Excepts, ErrorMessage)
    End Sub
    Public Sub New(Excepts As IEnumerable(Of Char), Optional ErrorMessage As String = "输入内容不能包含 %")
        Rule = New LegacyValidateExcept(Excepts, ErrorMessage)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateExceptSame
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateExceptSame
    Public Property Excepts As ObjectModel.Collection(Of String)
        Get
            Return Rule.Excepts
        End Get
        Set(Value As ObjectModel.Collection(Of String))
            Rule.Excepts = Value
        End Set
    End Property
    Public Property ErrorMessage As String
        Get
            Return Rule.ErrorMessage
        End Get
        Set(Value As String)
            Rule.ErrorMessage = Value
        End Set
    End Property
    Public Property IgnoreCase As Boolean
        Get
            Return Rule.IgnoreCase
        End Get
        Set(Value As Boolean)
            Rule.IgnoreCase = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateExceptSame()
    End Sub
    Public Sub New(Excepts As ObjectModel.Collection(Of String), Optional ErrorMessage As String = "输入内容不能为 %", Optional IgnoreCase As Boolean = False)
        Rule = New LegacyValidateExceptSame(Excepts, ErrorMessage, IgnoreCase)
    End Sub
    Public Sub New(Excepts As IEnumerable(Of String), Optional ErrorMessage As String = "输入内容不能为 %", Optional IgnoreCase As Boolean = False)
        Rule = New LegacyValidateExceptSame(Excepts, ErrorMessage, IgnoreCase)
    End Sub
    Public Sub New(Except As String, Optional ErrorMessage As String = "输入内容不能为 %", Optional IgnoreCase As Boolean = False)
        Rule = New LegacyValidateExceptSame(Except, ErrorMessage, IgnoreCase)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateSame
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateSame
    Public Property Required As String
        Get
            Return Rule.Required
        End Get
        Set(Value As String)
            Rule.Required = Value
        End Set
    End Property
    Public Property ErrorMessage As String
        Get
            Return Rule.ErrorMessage
        End Get
        Set(Value As String)
            Rule.ErrorMessage = Value
        End Set
    End Property
    Public Property IgnoreCase As Boolean
        Get
            Return Rule.IgnoreCase
        End Get
        Set(Value As Boolean)
            Rule.IgnoreCase = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateSame()
    End Sub
    Public Sub New(Required As String, Optional ErrorMessage As String = "输入内容有误", Optional IgnoreCase As Boolean = False)
        Rule = New LegacyValidateSame(Required, ErrorMessage, IgnoreCase)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateFolderName
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateFolderName
    Public Property Folder As String
        Get
            Return Rule.Folder
        End Get
        Set(Value As String)
            Rule.Folder = Value
        End Set
    End Property
    Public Property UseMinecraftCharCheck As Boolean
        Get
            Return Rule.UseMinecraftCharCheck
        End Get
        Set(Value As Boolean)
            Rule.UseMinecraftCharCheck = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateFolderName()
    End Sub
    Public Sub New(Folder As String, Optional UseMinecraftCharCheck As Boolean = True, Optional IgnoreList As List(Of String) = Nothing)
        Rule = New LegacyValidateFolderName(Folder, UseMinecraftCharCheck, IgnoreList, CapturePaths(Folder))
    End Sub
    Private Shared Function CapturePaths(Folder As String) As IEnumerable(Of String)
        On Error Resume Next
        Return DirectoryUtils.EnumerateDirectories(Folder)
    End Function
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateFileName
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateFileName
    Public Property Name As String
        Get
            Return Rule.Name
        End Get
        Set(Value As String)
            Rule.Name = Value
        End Set
    End Property
    Public Property UseMinecraftCharCheck As Boolean
        Get
            Return Rule.UseMinecraftCharCheck
        End Get
        Set(Value As Boolean)
            Rule.UseMinecraftCharCheck = Value
        End Set
    End Property
    Public Property IgnoreCase As Boolean
        Get
            Return Rule.IgnoreCase
        End Get
        Set(Value As Boolean)
            Rule.IgnoreCase = Value
        End Set
    End Property
    Public Property ParentFolder As String
        Get
            Return Rule.ParentFolder
        End Get
        Set(Value As String)
            Rule.ParentFolder = Value
        End Set
    End Property
    Public Property RequireParentFolderExists As Object
        Get
            Return Rule.RequireParentFolderExists
        End Get
        Set(Value As Object)
            Rule.RequireParentFolderExists = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateFileName()
    End Sub
    Public Sub New(Name As String, Optional UseMinecraftCharCheck As Boolean = True, Optional IgnoreCase As Boolean = True)
        Rule = New LegacyValidateFileName(Name, UseMinecraftCharCheck, IgnoreCase)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class

Public Class ValidateFolderPath
    Inherits Validate
    Private ReadOnly Rule As LegacyValidateFolderPath
    Public Property UseMinecraftCharCheck As Boolean
        Get
            Return Rule.UseMinecraftCharCheck
        End Get
        Set(Value As Boolean)
            Rule.UseMinecraftCharCheck = Value
        End Set
    End Property
    Public Sub New()
        Rule = New LegacyValidateFolderPath()
    End Sub
    Public Sub New(UseMinecraftCharCheck As Boolean)
        Rule = New LegacyValidateFolderPath(UseMinecraftCharCheck)
    End Sub
    Public Overrides Function Validate(Str As String) As String
        Return Rule.Validate(Str)
    End Function
End Class
