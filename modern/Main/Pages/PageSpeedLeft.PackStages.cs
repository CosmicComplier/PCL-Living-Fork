using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MeloongCore.Extensions;
using PCLCS;

namespace PCL;

public partial class PageSpeedLeft {
    private static void RefreshPackStages(Grid rows, PackUpdateStageSnapshot[] stages, Exception error) {
        string detail = error?.GetDisplay(true);
        string signature = string.Join("\n", stages.Select(stage => stage.State + ":" + stage.Detail + ":" + stage.Progress)) + "\n" + detail;
        if (Equals(rows.Tag, signature)) return;
        rows.Tag = signature;
        rows.Children.Clear(); rows.RowDefinitions.Clear();
        foreach (var stage in stages) {
            int index = rows.RowDefinitions.Count;
            rows.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = 26 });
            string state = stage.State switch {
                PackUpdateStageState.Running when stage.Progress.HasValue => Math.Floor(stage.Progress.Value * 100) + "%", PackUpdateStageState.AwaitingConfirmation => "待确认",
                PackUpdateStageState.Skipped => "跳过", PackUpdateStageState.Canceled => "已取消", _ => null
            };
            FrameworkElement marker;
            if (state is not null) {
                marker = new TextBlock { Text = state, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) };
                marker.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3");
            } else {
                bool done = stage.State == PackUpdateStageState.Finished;
                bool failed = stage.State == PackUpdateStageState.Failed;
                var shape = new System.Windows.Shapes.Path {
                    Data = Geometry.Parse(done ? "F1 M23.7501,33.25 L34.8334,44.3333 52.2499,22.1668 56.9999,26.9168 34.8334,53.8333 19.0001,38 Z"
                        : failed ? "F1 M2.5,0 L0,2.5 7.5,10 0,17.5 2.5,20 10,12.5 17.5,20 20,17.5 12.5,10 20,2.5 17.5,0 10,7.5 Z"
                        : "F1 M5,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 Z"),
                    Stretch = Stretch.Uniform, Width = done || failed ? 15 : 18, Height = done ? 16 : failed ? 15 : 6,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, done ? 3 : failed ? 1 : 7, 0, 0)
                };
                shape.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "ColorBrush3"); marker = shape;
            }
            marker.Tag = stage.State; marker.ToolTip = stage.State switch {
                PackUpdateStageState.Finished => "已完成", PackUpdateStageState.Waiting => "等待执行",
                PackUpdateStageState.Failed => "失败", _ => stage.Detail ?? state
            };
            Grid.SetRow(marker, index); rows.Children.Add(marker);
            var label = new StackPanel { Margin = new Thickness(0, 0, 0, 5), Tag = stage.Stage };
            var title = new TextBlock { Text = stage.Name + (stage.State == PackUpdateStageState.Skipped ? "（无需更新）" : ""), TextWrapping = TextWrapping.Wrap };
            title.SetResourceReference(TextBlock.ForegroundProperty, stage.State == PackUpdateStageState.Running ? "ColorBrush3" : "ColorBrush1");
            label.Children.Add(title);
            label.ToolTip = stage.Detail;
            Grid.SetColumn(label, 1); Grid.SetRow(label, index); rows.Children.Add(label);
        }
        if (detail is not null) {
            int index = rows.RowDefinitions.Count;
            rows.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var message = new TextBlock { Text = detail, TextWrapping = TextWrapping.Wrap, ToolTip = "单击复制错误详情", Margin = new Thickness(0, 4, 0, 5), Tag = "pack-update-error" };
            message.MouseLeftButtonDown += (_, _) => { ModBase.ClipboardSet(message.Text, false); ModMain.Hint("已复制错误详情！", ModMain.HintType.Green); };
            Grid.SetColumn(message, 1); Grid.SetRow(message, index); rows.Children.Add(message);
        }
    }
}
