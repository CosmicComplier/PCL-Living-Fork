using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using MeloongCore.Extensions;
using ThrottleDebounce;

namespace PCLLF.Presentation.Controls;

// Author: uye (owner of the MaaAssistantArknights team)
// Original Source: https://github.com/MaaAssistantArknights/MaaAssistantArknights
// License: Apache License 2.0 (this file only). Originally AGPL v3.0; its author expressly
// relicensed this specific clipboard fix for Plain Craft Launcher under Apache 2.0.
// LF Controls/Behaviors/ClipboardInterceptor.vb (2025-07-03) is the behavior source.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 corresponding .cs was compared;
// original LF retry counts, textbox newline/caret handling and warning severity are retained.
public static class ClipboardInterceptorView {
    private static Action<Exception, string, bool> warning = (error, message, toast) => MeloongCore.Logger.Warn(error, message, toast ? MeloongCore.LogBehavior.Toast : MeloongCore.LogBehavior.None);
    public static void ConfigureWarnings(Action<Exception, string, bool> callback) => warning = callback;
    public static readonly DependencyProperty EnableSafeClipboardProperty = DependencyProperty.RegisterAttached("EnableSafeClipboard", typeof(bool), typeof(ClipboardInterceptorView), new PropertyMetadata(false, Changed));
    public static void SetEnableSafeClipboard(DependencyObject element, bool value) => element.SetValue(EnableSafeClipboardProperty, value);
    public static bool GetEnableSafeClipboard(DependencyObject element) => (bool)element.GetValue(EnableSafeClipboardProperty);
    private static void Changed(DependencyObject value, DependencyPropertyChangedEventArgs args) {
        if (!(bool)args.NewValue) return;
        if (value is TextBox text) {
            text.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, CopyText));
            text.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, CutText, CanEditText));
            text.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, PasteText, CanEditText));
        } else if (value is RichTextBox rich) {
            rich.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, CopyRich));
            rich.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, CutRich));
            rich.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, PasteRich));
        } else if (value is DataGrid grid) grid.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, CopyGrid));
        // LF adds on every false->true transition and does not remove bindings on false.
    }
    private static void SetText(string text) {
        System.Windows.Forms.Clipboard.SetDataObject(text, false, 10, 100);
        try { System.Windows.Clipboard.Flush(); }
        catch (Exception error) { warning(error, $"剪贴板持久化失败（错误码：0x{error.HResult:X8}）", false); }
    }
    private static void Warn(Exception error, string action) => warning(error, $"{action}失败，请稍后再试（错误码：0x{error.HResult:X8}）", true);
    private static void CopyText(object sender, ExecutedRoutedEventArgs args) {
        if (sender is not TextBox text || text.SelectionLength <= 0) return;
        try { SetText(text.SelectedText); } catch (Exception error) { Warn(error, "复制"); }
        args.Handled = true;
    }
    private static void CanEditText(object sender, CanExecuteRoutedEventArgs args) {
        args.CanExecute = sender is TextBox text && !text.IsReadOnly && (args.Command != ApplicationCommands.Cut || text.SelectionLength > 0); args.Handled = true;
    }
    private static void CutText(object sender, ExecutedRoutedEventArgs args) {
        args.Handled = true;
        if (sender is not TextBox text || text.IsReadOnly || text.SelectionLength <= 0) return;
        try { SetText(text.SelectedText); } catch (Exception error) { Warn(error, "剪切"); return; }
        text.SelectedText = string.Empty;
    }
    private static void PasteText(object sender, ExecutedRoutedEventArgs args) {
        args.Handled = true; if (sender is not TextBox text || text.IsReadOnly) return;
        try {
            string? paste = Retrier.Attempt(func: _ => System.Windows.Forms.Clipboard.ContainsText() ? System.Windows.Forms.Clipboard.GetText() : null,
                maxAttempts: 10, delay: _ => TimeSpan.FromMilliseconds(100));
            if (paste is null) return;
            if (!text.AcceptsReturn) paste = paste.ReplaceLineEndings(" ", true);
            int start = text.SelectionStart; text.SelectedText = paste; text.CaretIndex = checked(start + paste.Length); text.SelectionLength = 0;
        } catch (Exception error) { Warn(error, "粘贴"); }
    }
    private static void CopyRich(object sender, ExecutedRoutedEventArgs args) {
        if (sender is not RichTextBox rich) return;
        TextRange selection = new(rich.Selection.Start, rich.Selection.End); if (string.IsNullOrEmpty(selection.Text)) return;
        try { SetText(selection.Text); } catch (Exception error) { Warn(error, "复制"); }
        args.Handled = true;
    }
    private static void CutRich(object sender, ExecutedRoutedEventArgs args) {
        if (sender is not RichTextBox rich) return;
        TextRange selection = new(rich.Selection.Start, rich.Selection.End); if (string.IsNullOrEmpty(selection.Text)) return; args.Handled = true;
        try { SetText(selection.Text); } catch (Exception error) { Warn(error, "剪切"); return; }
        selection.Text = string.Empty;
    }
    private static void PasteRich(object sender, ExecutedRoutedEventArgs args) {
        if (sender is not RichTextBox rich) return;
        try {
            string? paste = Retrier.Attempt(func: _ => System.Windows.Forms.Clipboard.ContainsText() ? System.Windows.Forms.Clipboard.GetText() : null,
                maxAttempts: 2, delay: _ => TimeSpan.FromMilliseconds(200));
            if (paste is null) return;
            TextSelection selection = rich.Selection; selection.Text = paste; TextPointer caret = selection.End; rich.CaretPosition = caret; rich.Selection.Select(caret, caret);
        } catch (Exception error) { Warn(error, "粘贴"); }
        args.Handled = true;
    }
    private static void CopyGrid(object sender, ExecutedRoutedEventArgs args) {
        if (sender is not DataGrid grid || grid.SelectedCells is null || grid.SelectedCells.Count == 0) return;
        StringBuilder builder = new();
        foreach (var row in grid.SelectedCells.GroupBy(cell => cell.Item))
            builder.AppendLine(string.Join("\t", row.Select(cell => cell.Column.GetCellContent(cell.Item) is TextBlock text ? text.Text : "")));
        string result = builder.ToString().TrimEnd('\r', '\n');
        try { SetText(result); } catch (Exception error) { Warn(error, "复制"); }
        args.Handled = true;
    }
}
