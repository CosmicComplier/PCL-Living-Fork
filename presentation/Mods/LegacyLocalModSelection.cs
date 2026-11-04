using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCLLF.Presentation.Controls;

namespace PCLLF.Presentation.Mods;

// Original LF LocalResourceUi.AppendItemSelection animation plan, using the
// existing real ControlAnimations host; no second animator or changed timings.
public static class LegacyLocalModSelection {
    public static void Append(ControlAnimations animation,List<ControlAnimation> frames,Border back,bool hovered,bool selected,bool pressed,bool tintBackground=true){
        int time=hovered?120:180;
        if(hovered||selected){
            if(tintBackground)frames.Add(animation.ColorResource(back,Border.BackgroundProperty,pressed?"ColorBrush6":"ColorBrushBg1",new(time)));
            frames.Add(animation.Opacity(back,1-back.Opacity,new(time,ease:ControlEase.OutFluent)));
            frames.Add(animation.ScaleTransform(back,(pressed ? .996 : 1)-((ScaleTransform)back.RenderTransform).ScaleX,new(time*1.2,ease:ControlEase.OutFluent)));
        }else{
            frames.Add(animation.Opacity(back,-back.Opacity,new(time)));
            frames.Add(animation.ScaleTransform(back,.996-((ScaleTransform)back.RenderTransform).ScaleX,new(time,ease:ControlEase.OutFluent)));
            frames.Add(animation.ScaleTransform(back,-.196,new(1,after:true)));
        }
    }
}
