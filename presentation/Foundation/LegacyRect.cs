using PCLLF.Presentation.Animation;

namespace PCLLF.Presentation.Foundation;

// LF ModBase.MyRect, unchanged from fixed PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8.
public class LegacyRect {
    public double Width { get; set; }
    public double Height { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public LegacyRect() { }
    public LegacyRect(double left, double top, double width, double height) {
        Left = left; Top = top; Width = width; Height = height;
    }
    // Reuse the actual animation value contract; there is no second mutable store.
    public AnimationRect ToAnimationRect() => new(Left, Top, Width, Height);
}
