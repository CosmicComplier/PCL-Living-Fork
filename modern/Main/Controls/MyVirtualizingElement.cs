using System;
using System.Windows;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyVirtualizingElement<T> : MyVirtualizingElementView<T> where T : FrameworkElement
    {

        public MyVirtualizingElement(Func<T> Initializer) : base(Initializer)
        {
        }
        public static implicit operator T(MyVirtualizingElement<T> Virtualized)
        {
            return Virtualized.Init();
        }
    }

    public class MyVirtualizingElement : MyVirtualizingElementView
    {

        public MyVirtualizingElement(Func<FrameworkElement> Initializer) : base(Initializer)
        {
        }
        public static new FrameworkElement TryInit(FrameworkElement Element)
        {
            return MyVirtualizingElementView.TryInit(Element);
        }
    }
}