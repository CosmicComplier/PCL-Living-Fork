using System;
using System.Collections.Generic;

namespace PCLCS;

// Behavior source: LF ModBase.vb at ebffef884d2b49b275a3379e62c170d892679db8.
// LF lineage f4aa0fae62c3ee8af24c81e61844c8fdd13c8983; PCL baseline 5ef81e490ae46b1100d61e33d0183094c7a80ca8;
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain Craft Launcher 2/Modules/Base/ModBase.cs
// is a same-origin migration reference only: its extra INI lock, paths and policies are not imported.
public interface ILegacyBaseSearchSource {
    string[] Aliases { get; }
    double Weight { get; }
}
public interface ILegacyBaseSearchEntry<TSource> where TSource : ILegacyBaseSearchSource {
    List<TSource> Sources { get; }
    double Similarity { get; set; }
    bool AbsoluteRight { get; set; }
}

/// <summary>Only actual clipboard/UI primitives. Cache, UUID, random and UI-thread fields remain single legacy stores.</summary>
public sealed class LegacyBaseHost {
    public Func<bool> OnUiThread = null!;
    public Action<Action> DispatchWait = null!;
    public Action ClearClipboard = null!;
    public Action<string> WriteClipboardText = null!;
    public Func<bool> ClipboardContainsText = null!;
    public Func<string> ReadClipboardText = null!;
    public Action<string> SuccessHint = null!;
    public Action<string, string> Message = null!;
}
