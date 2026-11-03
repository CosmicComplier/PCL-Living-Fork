Imports System.Threading

Friend Class InstanceKeybinding
    Inherits PCLCS.InstanceKeybindingData
End Class

Friend Module InstanceKeybindings
    Friend Function ReadBindings(GameFolder As String, Token As CancellationToken) As List(Of InstanceKeybinding)
        Return PCLCS.InstanceKeybindingReader.ReadBindings(GameFolder, Token,
            Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName),
            Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            Function(Title) MeloongCore.Extensions.StringExtensions.ReplaceLineEndings(Title, " ", False),
            Function() New InstanceKeybinding).Cast(Of InstanceKeybinding)().ToList()
    End Function
    Friend Sub Save(GameFolder As String, Key As String, Previous As String, Replacement As String)
        PCLCS.InstanceKeybindingFiles.Save(GameFolder, Key, Previous, Replacement, LocalGameData.Gate,
            Function() HasRunningMinecraft, Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName), AddressOf IniClearCache)
    End Sub
    Private Function DefaultBindings() As List(Of InstanceKeybinding)
        Return PCLCS.InstanceKeybindingReader.DefaultBindings(Function() New InstanceKeybinding).Cast(Of InstanceKeybinding)().ToList()
    End Function
End Module
