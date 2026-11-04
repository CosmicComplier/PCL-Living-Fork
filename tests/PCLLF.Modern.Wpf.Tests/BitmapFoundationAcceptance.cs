using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using PCLCS;
using PCLLF.Presentation.Foundation;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingColor = System.Drawing.Color;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;

// Runs in the actual modern STA test program against production bitmap/color types.
// The test project must link the existing LF Images/Blocks/Grass.png as WPF
// Resource with Link=Images/Blocks/Grass.png to exercise real compiled pack lookup.
internal static class BitmapFoundationAcceptance {
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Near(double first, double second, string message) => Require(Math.Abs(first - second) < .00001, message);
    private static void Pixel(DrawingBitmap bitmap, int x, int y, DrawingColor expected) =>
        Require(bitmap.GetPixel(x, y).ToArgb() == expected.ToArgb(), "Actual GDI pixel changed at " + x + "," + y + ".");

    internal static void Run(string repositoryRoot, string fixtureRoot) {
        Require(Application.Current != null && Application.Current.Dispatcher.CheckAccess(), "Bitmap checks require the real STA application dispatcher.");
        ColorAndRect();
        Conversions();
        Transforms();
        FilesAndResources(fixtureRoot);
        WebP(repositoryRoot, fixtureRoot);
    }

    private static void ColorAndRect() {
        CultureInfo before = CultureInfo.CurrentCulture;
        try {
            foreach (string name in new[] { "en-US", "fr-FR" }) {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                LegacyColor precise = new(1.2345678901234567, -0.0, 282589933980712.5, double.PositiveInfinity);
                string first = name == "fr-FR" ? "1,23456789012346" : "1.23456789012346";
                Require(precise.ToString() == "(" + first + ",0,282589933980713,∞)", "LF finite G15 precision, zero or current culture changed.");
                // Original CLR4 UTF16 values: numeric-en-US/fr-FR nan=[78,97,78],
                // infinity=[8734], negative-infinity=[45,8734], measured in the
                // fixed LF v7 oracle at the 0bee590 accepted checkpoint. The
                // expected symbols come from that evidence, not modern defaults.
                Require(new LegacyColor(double.NaN, double.PositiveInfinity, double.NegativeInfinity, +0.0).ToString() == "(NaN,∞,-∞,0)",
                    "LF nonfinite symbols or positive-zero formatting changed.");
            }
        } finally { CultureInfo.CurrentCulture = before; }
        Require(LegacyColor.ClampToByte(-double.PositiveInfinity) == 0 && LegacyColor.ClampToByte(double.PositiveInfinity) == 255 &&
            LegacyColor.ClampToByte(.5) == 0 && LegacyColor.ClampToByte(1.5) == 2, "Original channel clamp or midpoint-to-even rounding changed.");
        bool rejected = false; try { LegacyColor.ClampToByte(double.NaN); } catch (OverflowException) { rejected = true; }
        Require(rejected, "NaN channel silently became a byte.");
        LegacyColor nan = new(255, double.NaN, 0, 0);
        Require(!LegacyColor.AreEqual(nan, nan) && LegacyColor.AreEqual(null, null) && !LegacyColor.AreEqual(nan, null), "Original color equality/null/NaN semantics changed.");
        LegacyColor mutable = new(10, 20, 30, 40); int hash = mutable.GetHashCode();
        Require(ReferenceEquals(mutable.FromHSL(0, 100, 50), mutable) && mutable.A == 255 && mutable.R == 255 && mutable.G == 0 && mutable.B == 0 && mutable.GetHashCode() == hash,
            "HSL lost same-instance mutation, original red or identity hashing.");
        LegacyColor white = new((object)null);
        Require(white.A == 255 && white.R == 255 && white.G == 255 && white.B == 255, "Original object null-to-white conversion changed.");
        var signed = LegacyColor.Lerp(new LegacyColor(10, -1, 300, 0), new LegacyColor(20, 2, 100, 1), .333333333333);
        Require(signed.A == 13.333333 && signed.R == 0 && signed.G == 233.333333 && signed.B == .333333, "Signed six-decimal color interpolation changed.");
        LegacyRect rectangle = new(-4, 7, -3, double.NaN);
        var actual = rectangle.ToAnimationRect();
        Require(rectangle.Left == -4 && actual.Left == -4 && actual.Top == 7 && actual.Width == -3 && double.IsNaN(actual.Height), "Rectangle state was clamped or duplicated.");
        Console.WriteLine("PASS bitmap foundation: measured LF color formatting, signed/NaN/rounding/HSL state and mutable rectangle contract.");
    }

    private static void Conversions() {
        using DrawingBitmap bitmap = new(2, 2, DrawingPixelFormat.Format32bppArgb);
        bitmap.SetResolution(144, 120);
        bitmap.SetPixel(0, 0, DrawingColor.FromArgb(255, 10, 20, 30)); bitmap.SetPixel(1, 0, DrawingColor.FromArgb(128, 40, 50, 60));
        bitmap.SetPixel(0, 1, DrawingColor.FromArgb(255, 70, 80, 90)); bitmap.SetPixel(1, 1, DrawingColor.FromArgb(255, 100, 110, 120));
        LegacyBitmap original = new(bitmap), imageAlias = new((System.Drawing.Image)bitmap);
        Require(ReferenceEquals(original.Pic, bitmap) && ReferenceEquals(imageAlias.Pic, bitmap), "GDI constructors cloned the original image.");
        var source = original.ToImageSource();
        Require(source.IsFrozen && source.PixelWidth == 2 && source.PixelHeight == 2 && source.Format == PixelFormats.Bgra32, "GDI conversion lost dimensions, BGRA32 or freezing.");
        Near(bitmap.HorizontalResolution, source.DpiX, "Horizontal DPI changed."); Near(bitmap.VerticalResolution, source.DpiY, "Vertical DPI changed.");
        byte[] pixels = new byte[16]; source.CopyPixels(pixels, 8, 0);
        Require(pixels.Take(8).SequenceEqual(new byte[] { 30, 20, 10, 255, 60, 50, 40, 128 }), "Straight-alpha/BGRA channel order changed.");
        LegacyBitmap pngRoundTrip = new(source);
        try { Require(!ReferenceEquals(pngRoundTrip.Pic, bitmap), "ImageSource constructor reused the original Bitmap."); Pixel(pngRoundTrip.Pic!, 1, 0, bitmap.GetPixel(1, 0)); }
        finally { pngRoundTrip.Pic?.Dispose(); }
        ImageBrush brush = original.ToImageBrush();
        Require(!brush.IsFrozen && brush.ImageSource.IsFrozen, "Original mutable brush/frozen source boundary changed.");
        // Use opaque pixels for the original BMP-encoder route; it intentionally
        // retains LF's codec behavior rather than replacing it with PNG.
        using DrawingBitmap opaque = new(1, 1); opaque.SetPixel(0, 0, DrawingColor.FromArgb(255, 12, 34, 56));
        LegacyBitmap bmpRoundTrip = new(new LegacyBitmap(opaque).ToImageBrush());
        try { Pixel(bmpRoundTrip.Pic!, 0, 0, opaque.GetPixel(0, 0)); } finally { bmpRoundTrip.Pic?.Dispose(); }
        Console.WriteLine("PASS bitmap foundation: actual GDI aliases, WPF BGRA/alpha/DPI/freeze and PNG/BMP conversion routes.");
    }

    private static void Transforms() {
        using DrawingBitmap source = new(3, 2, DrawingPixelFormat.Format32bppArgb); source.SetResolution(144, 120);
        for (int y = 0; y < 2; y++) for (int x = 0; x < 3; x++) source.SetPixel(x, y, DrawingColor.FromArgb(255, x * 40, y * 50, x + y));
        LegacyBitmap original = new(source), crop = original.Clip(1, 0, 1, 2), rotate = original.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
        try {
            Require(!ReferenceEquals(crop.Pic, source) && !ReferenceEquals(rotate.Pic, source) && crop.Pic!.Width == 1 && crop.Pic!.Height == 2 && rotate.Pic!.Width == 2 && rotate.Pic!.Height == 3,
                "Crop or rotation lost independent output dimensions.");
            Pixel(crop.Pic!, 0, 0, source.GetPixel(1, 0)); Pixel(crop.Pic!, 0, 1, source.GetPixel(1, 1)); Pixel(rotate.Pic!, 1, 0, source.GetPixel(0, 0));
            Near(source.HorizontalResolution, crop.Pic!.HorizontalResolution, "Crop DPI changed."); Near(source.VerticalResolution, rotate.Pic!.VerticalResolution, "Rotate DPI changed.");
            crop.Pic!.SetPixel(0, 0, DrawingColor.White); Require(source.GetPixel(1, 0).ToArgb() != DrawingColor.White.ToArgb(), "Editing crop changed source pixels.");
        } finally { crop.Pic?.Dispose(); rotate.Pic?.Dispose(); }
        Console.WriteLine("PASS bitmap foundation: actual independent nearest-neighbor crop and rotate outputs with original DPI.");
    }

    private static void FilesAndResources(string fixtureRoot) {
        Directory.CreateDirectory(fixtureRoot);
        int warnings = 0; LegacyBitmapHost host = LegacyBitmapHost.ForApplication(typeof(BitmapFoundationAcceptance), (_, _) => warnings++);
        using DrawingBitmap original = new(1, 1); original.SetPixel(0, 0, DrawingColor.FromArgb(255, 90, 80, 70));
        string path = Path.Combine(fixtureRoot, "nested", "pixel.png"); new LegacyBitmap(original).Save(path);
        Require(File.ReadAllBytes(path).Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "Save did not write the original PNG format.");
        LegacyBitmap loaded = new(path, host);
        try {
            Pixel(loaded.Pic!, 0, 0, original.GetPixel(0, 0));
            using (FileStream exclusive = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            loaded.Save(path); using (FileStream exclusive = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        } finally { loaded.Pic?.Dispose(); }
        string corrupt = Path.Combine(fixtureRoot, "corrupt.png"); File.WriteAllBytes(corrupt, new byte[] { 1, 2, 3 });
        bool rejected = false; try { _ = new LegacyBitmap(corrupt, host); } catch (Exception error) { rejected = error.InnerException is ArgumentException && error.Message == "图片格式不支持，或图片文件损坏（" + corrupt + "）"; }
        Require(rejected && warnings == 0, "Original invalid-image classification changed.");
        using (FileStream exclusive = new(corrupt, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        string key = "PclBitmapFallback\0" + Guid.NewGuid().ToString("N");
        Application.Current.Resources[key] = original;
        try { Require(ReferenceEquals(new LegacyBitmap(key, host).Pic, original) && warnings == 1, "Resource fallback lost actual dictionary identity or warning."); }
        finally { Application.Current.Resources.Remove(key); }
        string uri = "pack://application:,,,/images/Blocks/Grass.png";
        LegacyBitmap first = new(uri, host), second = new(uri, host);
        Require(ReferenceEquals(first.Pic, second.Pic) && first.Pic!.Width > 0, "Original shared pack-resource cache stopped sharing the actual Bitmap.");
        Require(LegacyBitmap.NormalizeResourceUri("/Plain%20Craft%20Launcher%202;component/Images/Blocks/Grass.png", host.ResourceAssemblyName) ==
            "pack://application:,,,/" + host.ResourceAssemblyName + ";component/Images/Blocks/Grass.png", "LF old assembly URI normalization changed.");
        Require(!AppDomain.CurrentDomain.GetAssemblies().Any(value => value.GetName().Name == "Imazen.WebP"), "Non-WebP loading prematurely resolved Imazen.WebP.");
        // Cached images remain cache-owned, just as in LF; do not dispose that shared Pic.
        Console.WriteLine("PASS bitmap foundation: actual PNG overwrite, exclusive reopen, original fallback/URI/cache and lazy WebP boundary.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WebP(string repositoryRoot, string fixtureRoot) {
        string native = Path.Combine(repositoryRoot, "Plain Craft Launcher 2", "Resources", "libwebp64.dll");
        Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(native))).Equals("DC14A3500D5A7763191D1CED09EFBEEB7DBE79A762811EA2CDB1D2F47F1032A6", StringComparison.Ordinal), "The pinned LF native WebP payload changed.");
        IntPtr module = NativeLibrary.Load(native);
        // Keep this fixed read-only repository module loaded for process lifetime;
        // cached P/Invoke thunks must not point at an unloaded or replaced DLL.
        IntPtr Resolve(string name, Assembly _, DllImportSearchPath? __) =>
            name.Equals("libwebp", StringComparison.OrdinalIgnoreCase) || name.Equals("libwebp.dll", StringComparison.OrdinalIgnoreCase) || name == "pcllf-fixture-webp" ? module : IntPtr.Zero;
        NativeLibrary.SetDllImportResolver(typeof(BitmapFoundationAcceptance).Assembly, Resolve);
        NativeLibrary.SetDllImportResolver(Assembly.Load("Imazen.WebP"), Resolve);
        byte[] rgba = { 12, 34, 56, 255 }; IntPtr encoded = IntPtr.Zero;
        try {
            ulong count = EncodeLossless(rgba, 1, 1, 4, out encoded).ToUInt64();
            Require(encoded != IntPtr.Zero && count > 0 && count <= int.MaxValue, "The original native encoder could not create the real WebP fixture.");
            byte[] bytes = new byte[(int)count]; Marshal.Copy(encoded, bytes, 0, bytes.Length);
            string path = Path.Combine(fixtureRoot, "native.webp"); File.WriteAllBytes(path, bytes);
            LegacyBitmap actual = new(path, LegacyBitmapHost.ForApplication(typeof(BitmapFoundationAcceptance), (_, _) => throw new InvalidOperationException("Valid WebP unexpectedly used fallback.")));
            try { Require(actual.Pic!.Width == 1 && actual.Pic!.Height == 1, "Original WebP decoder dimensions changed."); Pixel(actual.Pic!, 0, 0, DrawingColor.FromArgb(255, 12, 34, 56)); }
            finally { actual.Pic?.Dispose(); }
            using (FileStream exclusive = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        } finally { if (encoded != IntPtr.Zero) Free(encoded); }
        Console.WriteLine("PASS bitmap foundation: pinned native lossless WebP fixture through original Imazen decoder and released input file.");
    }
    [DllImport("pcllf-fixture-webp", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessRGBA")]
    private static extern UIntPtr EncodeLossless(byte[] rgba, int width, int height, int stride, out IntPtr output);
    [DllImport("pcllf-fixture-webp", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFree")]
    private static extern void Free(IntPtr pointer);
}
