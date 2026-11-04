using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MeloongCore;
using MeloongCore.Extensions;
using PCLCS;

namespace PCLLF.Presentation.Controls;

// LF Controls/MyImage.vb is the behavior source. CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5
// Controls/MyImage.cs was reviewed; its async HTTP/download-task deduplication/clip policy are not reused.
// Preserve LF's worker/live-source/fallback reads, shared FileUtils and real LegacyBitmap stream/cache policy.
public class MyImageView : Image {
    public TimeSpan FileCacheExpiredTime = new(7, 0, 0, 0);
    public bool EnableCache { get => (bool)GetValue(EnableCacheProperty); set => SetValue(EnableCacheProperty, value); }
    public static readonly DependencyProperty EnableCacheProperty = DependencyProperty.Register(nameof(EnableCache), typeof(bool), typeof(MyImageView), new PropertyMetadata(true));
    private static bool Same(string? left, string? right) => string.Equals(left ?? "", right ?? "", StringComparison.Ordinal);
    private string? source = "";
    public new string? Source {
        get => source;
        set { if (Same(value, "")) value = null; if (Same(source, value)) return; source = value; if (!IsInitialized) return; Load(); }
    }
    public new static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(string), typeof(MyImageView),
        new PropertyMetadata((sender, args) => { if (sender is not null) ((MyImageView)sender).Source = args.NewValue!.ToString(); }));
    public string? FallbackSource { get; set; }
    public string? LoadingSource { get; set; } = "pack://application:,,,/images/Icons/NoIcon.png";
    private string? actualSource;
    public string? ActualSource {
        get => actualSource;
        set {
            if (Same(value, "")) value = null; if (Same(actualSource, value)) return; actualSource = value;
            try { ImageSource? bitmap = value is null ? null : ReadBitmap(value); UiPost(() => base.Source = bitmap); }
            catch (Exception error) {
                Warn(error, "加载图片失败（" + value + "）");
                try { if (value.StartsWithF(TempRoot, true)) DeleteFile(value!); } catch { }
            }
        }
    }
    public MyImageView() { Initialized += (_, _) => Load(); }
    protected virtual int NewUuid() => ControlServices.Current.NewUuid();
    protected virtual string TempRoot => throw new InvalidOperationException("The LF product temporary-directory host is required for online images.");
    protected virtual ImageSource ReadBitmap(string address) => new LegacyBitmap(address,
        LegacyBitmapHost.ForApplication(Application.Current.GetType(), Warn)).ToImageSource();
    protected virtual void DeleteFile(string path) => FileUtils.Delete(path);
    protected virtual FileInfo FileInformation(string path) => FileUtils.GetInfo(path);
    protected virtual void UiPost(Action work) { if (Dispatcher.CheckAccess()) work(); else Dispatcher.InvokeAsync(work); }
    protected virtual void Warn(Exception error, string message) => ControlServices.Current.Warn(error, message);
    protected virtual void Error(Exception error, string message) => ControlServices.Current.Error(error, message);
    protected virtual void Download(string?[] addresses, string path) => throw new InvalidOperationException("The LF loader/download host is required for online images.");
    protected virtual void StartWorker(Action work, string name, ThreadPriority priority) {
        new Thread(() => { try { work(); } catch (Exception error) { Error(error, name + "：线程执行失败"); } }) { Name = name, Priority = priority }.Start();
    }
    private void Load() {
        if (Source is null) { ActualSource = null; return; }
        if (!Source.StartsWithF("http")) { ActualSource = Source; return; }
        bool cache = EnableCache;
        string temp = TempRoot + @"MyImage\" + Source.GetStableHashCode() + ".png" + (cache ? "" : NewUuid().ToString());
        FileInfo file = FileInformation(temp);
        if (cache && file.Exists) { ActualSource = temp; if (DateTime.Now - file.LastWriteTime < FileCacheExpiredTime) return; }
        else ActualSource = LoadingSource;
        StartWorker(() => {
            bool localFallback = !string.IsNullOrEmpty(FallbackSource) && !FallbackSource.StartsWithF("http");
            try {
                Download(FallbackSource is not null && FallbackSource.StartsWithF("http") && !Same(Source, FallbackSource) ? new[] { Source, FallbackSource } : new[] { Source }, temp);
                ActualSource = temp; if (!cache) DeleteFile(temp);
            } catch (Exception error) {
                try { if (temp is not null) DeleteFile(temp); } catch { }
                if (localFallback) {
                    try { ActualSource = FallbackSource; Warn(error, "下载图片失败，使用本地备用图片（" + Source + "）"); return; }
                    catch (Exception fallbackError) { Error(error, "下载图片失败（" + Source + "）"); Error(fallbackError, "加载备用图片失败（" + FallbackSource + "）"); }
                } else Error(error, "下载图片失败（" + Source + "，备用：" + FallbackSource + "）");
            }
        }, "MyImage PicLoader " + NewUuid() + "#", ThreadPriority.BelowNormal);
    }
}
