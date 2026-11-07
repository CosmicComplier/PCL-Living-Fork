using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

internal static class ModpackStageLayoutChecks {
    internal static void Run(string root) {
        var progress = new PackUpdateStageProgress();
        progress.Set(PackUpdateStage.ReadRecord, PackUpdateStageState.Running);
        var before = progress.Snapshot();
        progress.Set(PackUpdateStage.PrepareFiles, PackUpdateStageState.Running, "检查模组");
        Native.Check(progress.Snapshot()[0].State == PackUpdateStageState.Running && before[3].State == PackUpdateStageState.Waiting,
            "Stage snapshots inferred completion from ordinal position or mutated a previously published snapshot.");
        progress.Stop(PackUpdateStageState.Canceled);
        Native.Check(progress.Snapshot().Where(item => item.State == PackUpdateStageState.Canceled).Count() == 2 &&
            progress.Snapshot().Where(item => item.Stage > PackUpdateStage.PrepareFiles).All(item => item.State == PackUpdateStageState.Waiting),
            "Stopping presentation progress changed phases which never started.");
        progress.Reset();
        Native.Check(progress.Snapshot().All(item => item.State == PackUpdateStageState.Waiting), "A real retry cannot reset presentation stages.");
        foreach (bool debug in new[] { false, true })
        foreach (int windowHeight in new[] { 634, 540 }) {
            var speed = new PageSpeedLeft();
            Native.Call(typeof(PageSpeedLeft), "UpdateMetricLayout", speed, debug);
            int height = windowHeight - 70;
            Save(speed, Path.Combine(root, "metrics-" + (debug ? "debug" : "normal") + "-1005x" + windowHeight + ".png"), 200, height);
            var metrics = (Grid)speed.FindName("PanMetrics");
            var blocks = metrics.Children.OfType<StackPanel>().Where(item => item.Visibility == Visibility.Visible).ToArray();
            Native.Check(blocks.Length == (debug ? 6 : 5), "The complete debug metric block was not hidden or restored.");
            Rect[] bounds = blocks.Select(item => item.TransformToAncestor(speed).TransformBounds(new Rect(item.RenderSize))).ToArray();
            double[] gaps = bounds.Skip(1).Select((bounds, index) => bounds.Top - blocks[index].TransformToAncestor(speed).TransformBounds(new Rect(blocks[index].RenderSize)).Bottom).ToArray();
            Native.Check(bounds.All(item => item.Top >= 0 && item.Bottom <= height + 1) && gaps.All(gap => gap >= 0) && gaps.Max() - gaps.Min() <= 1,
                "Native task metrics overlap, escape the compact body height or have unequal block spacing.");
            Native.Check(blocks.All(block => block.Children.OfType<TextBlock>().Count() == 3 && block.Children.OfType<TextBlock>().All(text =>
                text.TransformToAncestor(block).TransformBounds(new Rect(text.RenderSize)).Bottom <= block.ActualHeight + 1 &&
                text.ActualHeight >= text.DesiredSize.Height - text.Margin.Top - text.Margin.Bottom - 1)),
                "A metric title, separator or value was clipped.");
        }
        Console.WriteLine("PASS native task metrics: five/six independent equal-spacing blocks at default 1005x634 and compact 1005x540 body heights; immutable, explicit presentation stages.");
    }
    internal static void CaptureTask(ModLoader.LoaderCombo<string> task, string path) {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (!dispatcher.CheckAccess()) { dispatcher.Invoke(() => CaptureTask(task, path)); return; }
        PageSpeedRight? previous = ModMain.FrmSpeedRight;
        try {
            var left = new PageSpeedLeft(); var right = new PageSpeedRight(); ModMain.FrmSpeedRight = right;
            // This offscreen panel never starts its real Watcher (and therefore
            // never initializes global download managers). Use the live combo's
            // genuine aggregate progress for the captured value.
            ((TextBlock)left.FindName("LabProgress")).Text = (task.Progress * 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " %";
            left.TaskRefresh(task);
            var card = ((StackPanel)right.FindName("PanMain")).Children.OfType<MyCard>().Single();
            _ = card.MainTextBlock;
            var rows = (Grid)card.Children[3];
            Native.Check(rows.Children.OfType<StackPanel>().Count(panel => panel.Tag is PackUpdateStage) == 12 &&
                card.Children.OfType<MyIconButton>().Any(button => button.Name == "BtnCancel"), "Actual stage card lost its independent rows or native cancel button.");
            if (task.State == ModBase.LoadState.Failed)
                Native.Check(rows.Children.OfType<TextBlock>().Any(text => Equals(text.Tag, "pack-update-error") && text.ToolTip?.ToString() == "单击复制错误详情"), "Failed stage card lost its copyable error details.");
            var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(237, 243, 252)) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); root.ColumnDefinitions.Add(new ColumnDefinition());
            root.Children.Add(left); Grid.SetColumn(right, 1); root.Children.Add(right);
            Save(root, path, 1005, 634);
            Native.Call(typeof(PageSpeedLeft), "UpdateMetricLayout", left, true);
            Save(root, Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "-debug-compact.png"), 1005, 540);
            int count = rows.Children.Count;
            left.TaskRefresh(task);
            Native.Check(rows.Children.Count == count, "Refreshing an unchanged aggregate percentage duplicated stage rows.");
        }
        finally { ModMain.FrmSpeedRight = previous; }
    }
    internal static void Save(FrameworkElement element, string path, int width, int height) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        element.Measure(new Size(width, height)); element.Arrange(new Rect(0, 0, width, height)); element.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var backdrop = new DrawingVisual();
        using (DrawingContext drawing = backdrop.RenderOpen()) drawing.DrawRectangle(new SolidColorBrush(PCLLF.Presentation.Foundation.LegacyThemePalette.IsDarkTheme ? Color.FromRgb(18, 28, 43) : Color.FromRgb(237, 243, 252)), null, new Rect(0, 0, width, height));
        bitmap.Render(backdrop); bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (FileStream output = File.Create(path)) encoder.Save(output);
        string? previews = Environment.GetEnvironmentVariable("PCLLF_MODPACK_PREVIEW_DIR");
        if (!string.IsNullOrWhiteSpace(previews)) {
            Directory.CreateDirectory(previews); File.Copy(path, Path.Combine(previews, Path.GetFileName(path)), true);
        }
    }
}
