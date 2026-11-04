using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCLLF.Presentation.Controls;

// LF MyVirtualizingElement.vb and LazyLoadBehavior.vb. CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5
// MyVirtualizingElement.cs was compared as a C# structure reference; LF viewport/initialization semantics remain.
public interface IControlVirtualElement { FrameworkElement InitializeElement(); }

public class MyVirtualizingElementView<T> : FrameworkElement, IControlVirtualElement where T : FrameworkElement {
    private readonly Func<T> initializer;
    public MyVirtualizingElementView(Func<T> initializer) {
        this.initializer = initializer;
        bool handled = false;
        EventHandler? handler = null;
        handler = (_, _) => {
            if (handled || RenderSize.Width < double.Epsilon || !IsVisible) return;
            DependencyObject? ancestor = this;
            while (ancestor is not null && ancestor is not ScrollViewer) ancestor = VisualTreeHelper.GetParent(ancestor);
            if (ancestor is not ScrollViewer scroll) return;
            if (!new Rect(0, 0, scroll.ViewportWidth, scroll.ViewportHeight).IntersectsWith(
                TransformToAncestor(scroll).TransformBounds(new Rect(new Point(0, 0), RenderSize)))) return;
            handled = true;
            Init();
            LayoutUpdated -= handler;
        };
        LayoutUpdated += handler;
    }
    public T Init() {
        T element = initializer();
        if (Parent is not null) {
            if (Parent is not Panel panel) throw new Exception("MyVirtualizingElement 的父级必须是一个 Panel");
            int index = panel.Children.IndexOf(this);
            panel.Children.RemoveAt(index);
            panel.Children.Insert(index, element);
        }
        return element;
    }
    FrameworkElement IControlVirtualElement.InitializeElement() => Init();
    public static implicit operator T(MyVirtualizingElementView<T> virtualized) => virtualized.Init();
}

public class MyVirtualizingElementView : MyVirtualizingElementView<FrameworkElement> {
    public MyVirtualizingElementView(Func<FrameworkElement> initializer) : base(initializer) { }
    public static FrameworkElement TryInit(FrameworkElement element) =>
        element is IControlVirtualElement virtualized ? virtualized.InitializeElement() : element;
}
