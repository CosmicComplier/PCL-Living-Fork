using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

internal static class ModpackStageLayoutChecks {
    internal static void Run(string root) {
        RepeatedTaskbarAdds(root);
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
        progress.Set(PackUpdateStage.PrepareFiles, PackUpdateStageState.Running, "下载文件");
        var report = progress.CreateProgressReporter(); report(.12);
        Native.Check(progress.Snapshot()[3].Progress == .12, "Native byte progress was lost.");
        progress.Set(PackUpdateStage.PrepareFiles, PackUpdateStageState.Running, "检查模组"); report(.9);
        Native.Check(progress.Snapshot()[3].Progress is null, "Late download progress marked a later CPU check as complete.");
        progress.Reset(); report(.8);
        Native.Check(progress.Snapshot().All(item => item.Progress is null), "Old progress survived retry reset.");
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
    private static void RepeatedTaskbarAdds(string folder) {
        var previousLeft = ModMain.FrmSpeedLeft; var previousRight = ModMain.FrmSpeedRight;
        var roots = new List<ModLoader.LoaderCombo<string>>();
        try {
            var right = new PageSpeedRight(); ModMain.FrmSpeedRight = right; ModMain.FrmSpeedLeft = null;
            PageSpeedLeft? left = null;
            for (int attempt = 0; attempt < 3; attempt++) {
                var step = (ModLoader.LoaderTask<string, string>)Native.Static("PCL.ModpackUpdateFlow", "CreateStep", "比较并更新整合包文件", new Action<ModLoader.LoaderTask<string, string>>(_ => throw new Exception("No fixture work should start.")))!;
                var combo = new ModLoader.LoaderCombo<string>("更新整合包：repeat fixture", new[] { step }); roots.Add(combo);
                ModLoader.LoaderTaskbarAdd(combo); // Production Add calls TaskRemove with this NEW root.
                left ??= new PageSpeedLeft(); ModMain.FrmSpeedLeft = left;
                var snapshots = (PackUpdateStageSnapshot[]?)Native.Static("PCL.ModpackUpdateFlow", "GetStageSnapshot", combo.GetLoaderList());
                Native.Check(snapshots?.Length == 12 && snapshots.All(stage => stage.State == PackUpdateStageState.Waiting), "A same-name taskbar addition lost its new stage list.");
                object engine = Native.Field(Native.Type("PCL.ModpackUpdateFlow"), "Engine").GetValue(null)!;
                object host = Native.Get(engine, "host")!;
                var stage = (Action<ModLoader.LoaderTask<string, string>, PackUpdateStage, PackUpdateStageState, string?>)Native.Get(host, "SetStage")!;
                var createProgress = (Func<ModLoader.LoaderTask<string, string>, double, double, MeloongCore.ProgressProvider>)Native.Get(host, "Progress")!;
                stage(step, PackUpdateStage.PrepareFiles, PackUpdateStageState.Running, "下载文件");
                var bytes = createProgress(step, .15, .65); bytes.Set(.12);
                left.TaskRefresh(combo);
                var card = ((StackPanel)right.FindName("PanMain")).Children.OfType<MyCard>().Single();
                var rows = (Grid)card.Children[3];
                Native.Check(rows.Children.OfType<StackPanel>().Count(panel => panel.Tag is PackUpdateStage) == 12 && rows.Children.OfType<TextBlock>().Any(text => text.Text == "12%"), "Retry did not render all stages and native percentage.");
                Native.Check(rows.Children.OfType<StackPanel>().Where(panel => panel.Tag is PackUpdateStage).All(panel => panel.Children.Count == 1), "Gray detail subtitles remained visible.");
                if (attempt == 1) { _ = card.MainTextBlock; Save(right, Path.Combine(folder, "retry-native-percent.png"), 780, 450); }
                bytes.Set(.22); left.TaskRefresh(combo);
                Native.Check(rows.Children.OfType<TextBlock>().Any(text => text.Text == "22%"), "Stage percentage stopped updating at unchanged aggregate state.");
                stage(step, PackUpdateStage.PrepareFiles, PackUpdateStageState.Running, "检查模组");
                bytes.Set(.9); left.TaskRefresh(combo);
                Native.Check(!rows.Children.OfType<TextBlock>().Any(text => text.Text.EndsWith("%", StringComparison.Ordinal)), "An unmeasured CPU phase displayed stale download percentage.");
                left.TaskRemove(combo); ModLoader.LoaderTaskbar.Remove(combo);
            }
        } finally {
            foreach (var root in roots) { ModMain.FrmSpeedLeft?.TaskRemove(root); ModLoader.LoaderTaskbar.Remove(root); }
            ModMain.FrmSpeedLeft = previousLeft; ModMain.FrmSpeedRight = previousRight;
        }
        Console.WriteLine("PASS repeated native taskbar additions: first/second/third same-name tasks retain 12 stages, native percentages refresh and subtitles stay hidden.");
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
