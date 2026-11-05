using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using MeloongCore.Wpf;
using MeloongCore.Wpf.Extensions;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using global::PCLCS;
using PCLCS;
using ThrottleDebounce;

namespace PCL
{
    // Original LF XAML fields/Handles and public entry points remain typed; business lives in C#.
    public partial class PageSetupLaunch
    {
        private global::PCLCS.LegacyPageSetupLaunchController<global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McSkinInfo> _controller;
        internal global::PCLCS.LegacyPageSetupLaunchController<global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McSkinInfo> Controller
        {
            get
            {
                if (this._controller is null)
                {
                    this._controller = new global::PCLCS.LegacyPageSetupLaunchController<global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McSkinInfo>(global::PCL.SetupPageRuntime.Launch, new global::PCLCS.LegacyPageSetupLaunchView() { Page = new Func<global::System.Windows.DependencyObject>(() => this), PanBack = new Func<global::System.Windows.Controls.ScrollViewer>(() => this.PanBack), PanMain = new Func<global::System.Windows.Controls.StackPanel>(() => this.PanMain), CheckPersonalOptionsSync = new Func<global::PCLLF.Presentation.Controls.MyCheckBoxView>(() => this.CheckPersonalOptionsSync), BtnImportPersonalOptions = new Func<global::PCLLF.Presentation.Controls.MyButtonView>(() => this.BtnImportPersonalOptions), CardArgument = new Func<global::PCLLF.Presentation.Controls.MyCardView>(() => this.CardArgument), LabArgumentIndie = new Func<global::System.Windows.Controls.TextBlock>(() => this.LabArgumentIndie), ComboArgumentIndieV2 = new Func<global::PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboArgumentIndieV2), ComboArgumentVisibie = new Func<global::PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboArgumentVisibie), PanArgumentWindow = new Func<global::System.Windows.Controls.Grid>(() => this.PanArgumentWindow), ComboArgumentWindowType = new Func<global::PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboArgumentWindowType), TextArgumentWindowWidth = new Func<global::PCLLF.Presentation.Controls.MyTextBoxView>(() => this.TextArgumentWindowWidth), LabArgumentWindowMiddle = new Func<global::System.Windows.Controls.TextBlock>(() => this.LabArgumentWindowMiddle), TextArgumentWindowHeight = new Func<global::PCLLF.Presentation.Controls.MyTextBoxView>(() => this.TextArgumentWindowHeight), RadioRamType0 = new Func<global::PCLLF.Presentation.Controls.MyRadioBoxView>(() => this.RadioRamType0), RadioRamType1 = new Func<global::PCLLF.Presentation.Controls.MyRadioBoxView>(() => this.RadioRamType1), SliderRamCustom = new Func<global::PCLLF.Presentation.Controls.MySliderView>(() => this.SliderRamCustom), CheckArgumentRam = new Func<global::PCLLF.Presentation.Controls.MyCheckBoxView>(() => this.CheckArgumentRam), PanRamDisplay = new Func<global::System.Windows.Controls.Grid>(() => this.PanRamDisplay), ColumnRamUsed = new Func<global::System.Windows.Controls.ColumnDefinition>(() => this.ColumnRamUsed), ColumnRamGame = new Func<global::System.Windows.Controls.ColumnDefinition>(() => this.ColumnRamGame), ColumnRamEmpty = new Func<global::System.Windows.Controls.ColumnDefinition>(() => this.ColumnRamEmpty), RectRamUsed = new Func<global::System.Windows.Shapes.Rectangle>(() => this.RectRamUsed), RectRamGame = new Func<global::System.Windows.Shapes.Rectangle>(() => this.RectRamGame), RectRamEmpty = new Func<global::System.Windows.Shapes.Rectangle>(() => this.RectRamEmpty), LabRamUsedTitle = new Func<global::System.Windows.Controls.TextBlock>(() => this.LabRamUsedTitle), LabRamGameTitle = new Func<global::System.Windows.Controls.TextBlock>(() => this.LabRamGameTitle), LabRamUsed = new Func<global::System.Windows.Controls.TextBlock>(() => this.LabRamUsed), LabRamTotal = new Func<global::System.Windows.Controls.TextBlock>(() => this.LabRamTotal), LabRamGame = new Func<global::System.Windows.Controls.TextBlock>(() => this.LabRamGame), CardSkin = new Func<global::PCLLF.Presentation.Controls.MyCardView>(() => this.CardSkin), RadioSkinType0 = new Func<global::PCLLF.Presentation.Controls.MyRadioBoxView>(() => this.RadioSkinType0), RadioSkinType1 = new Func<global::PCLLF.Presentation.Controls.MyRadioBoxView>(() => this.RadioSkinType1), RadioSkinType2 = new Func<global::PCLLF.Presentation.Controls.MyRadioBoxView>(() => this.RadioSkinType2), RadioSkinType3 = new Func<global::PCLLF.Presentation.Controls.MyRadioBoxView>(() => this.RadioSkinType3), RadioSkinType4 = new Func<global::PCLLF.Presentation.Controls.MyRadioBoxView>(() => this.RadioSkinType4), PanSkinID = new Func<global::System.Windows.Controls.Grid>(() => this.PanSkinID), BtnSkinSave = new Func<global::PCLLF.Presentation.Controls.MyButtonView>(() => this.BtnSkinSave), BtnSkinCache = new Func<global::PCLLF.Presentation.Controls.MyButtonView>(() => this.BtnSkinCache), PanSkinChange = new Func<global::System.Windows.Controls.Grid>(() => this.PanSkinChange), BtnSkinChange = new Func<global::PCLLF.Presentation.Controls.MyButtonView>(() => this.BtnSkinChange), BtnSkinDelete = new Func<global::PCLLF.Presentation.Controls.MyButtonView>(() => this.BtnSkinDelete), BtnAdvanceJvmReset = new Func<global::PCLLF.Presentation.Controls.MyIconButtonView>(() => this.BtnAdvanceJvmReset), TextAdvanceJvm = new Func<global::PCLLF.Presentation.Controls.MyTextBoxView>(() => this.TextAdvanceJvm), TextAdvanceGame = new Func<global::PCLLF.Presentation.Controls.MyTextBoxView>(() => this.TextAdvanceGame), TextAdvanceRun = new Func<global::PCLLF.Presentation.Controls.MyTextBoxView>(() => this.TextAdvanceRun), CheckAdvanceRunWait = new Func<global::PCLLF.Presentation.Controls.MyCheckBoxView>(() => this.CheckAdvanceRunWait), PanAdvanceJava = new Func<global::System.Windows.Controls.Grid>(() => this.PanAdvanceJava), ComboAdvanceJava = new Func<global::PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboAdvanceJava), LabAdvanceJava = new Func<global::System.Windows.Controls.TextBlock>(() => this.LabAdvanceJava), BtnAdvanceJavaSearch = new Func<global::PCLLF.Presentation.Controls.MyIconButtonView>(() => this.BtnAdvanceJavaSearch), BtnSwitch = new Func<global::System.Windows.FrameworkElement>(() => this.BtnSwitch) });
                }

                return this._controller;
            }
        }

        public PageSetupLaunch()
        {
            __LfAttachOwnHandles();
            this.InitializeComponent();
        }

        private void PageSetupLaunch_Loaded(global::System.Object sender, global::System.Windows.RoutedEventArgs e)
        {
            this.Controller.Loaded();
        }

        public void Refresh()
        {
            this.Controller.Refresh();
        }

        public void Reset()
        {
            this.Controller.Reset();
        }

        private void PersonalSyncChanged(global::System.Object Sender, global::System.Boolean User)
        {
            this.Controller.PersonalSyncChanged(Sender, User);
        }

        private global::System.Boolean ImportPersonalOptions()
        {
            return this.Controller.ImportPersonalOptions();
        }

        private void ImportPersonalOptionsClick()
        {
            this.Controller.ImportPersonalOptionsClick();
        }

        private void BtnSkinChange_Click(global::System.Object sender, global::System.EventArgs e)
        {
            this.Controller.ChangeSkinClick();
        }

        private void RadioSkinType3_Check(global::System.Object sender, global::PCL.ModBase.RouteEventArgs e)
        {
            this.Controller.PreviewCustomSkin(e.RaiseByMouse, ref e.Handled);
        }

        private global::System.Boolean ChangeSkin(global::PCL.ModMinecraft.McSkinInfo SkinInfo)
        {
            return this.Controller.ChangeSkin(SkinInfo);
        }

        private void BtnSkinDelete_Click(global::System.Object sender, global::System.EventArgs e)
        {
            this.Controller.DeleteSkin();
        }

        private void BtnSkinSave_Click(global::System.Object sender, global::System.EventArgs e)
        {
            this.Controller.SaveSkin();
        }

        private void BtnSkinCache_Click(global::System.Object sender, global::System.EventArgs e)
        {
            this.Controller.RefreshSkinCache();
        }

        public static void UpdateSkinType()
        {
            global::PCL.SetupPageRuntime.Launch.UpdateSkinType();
        }

        public static void UpdateRamType()
        {
            global::PCL.SetupPageRuntime.Launch.UpdateRamType();
        }

        public void RefreshRam(global::System.Boolean ShowAnim)
        {
            this.Controller.RefreshRam(ShowAnim);
        }

        private void RefreshRam()
        {
            this.Controller.RefreshRam();
        }

        private void RefreshRamText()
        {
            this.Controller.RefreshRamText();
        }

        public static global::System.Double GetRam(global::PCL.ModMinecraft.McInstance Instance, global::System.Boolean UseVersionJavaSetup)
        {
            return global::PCL.SetupPageRuntime.Launch.GetRam(Instance, UseVersionJavaSetup);
        }

        public void UpdateJavaList()
        {
            this.Controller.UpdateJavaList();
        }

        private void MoveJavaInList(global::PCLCS.Java JavaEntry, global::System.Int32 Offset)
        {
            this.Controller.MoveJavaInList(JavaEntry, Offset);
        }

        private void BtnAdvanceJavaSearch_Click()
        {
            this.Controller.SearchJava();
        }

        private void WindowTypeUIRefresh()
        {
            this.Controller.WindowTypeUIRefresh();
        }

        private void ComboArgumentVisibie_SizeChanged(global::System.Object sender, global::System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.Controller.VisibilitySelectionChanged(e);
        }

        private void CheckArgumentRam_Change()
        {
            this.Controller.MemoryOptimizationChanged();
        }

        private void ComboArgumentIndie_SelectionChanged(global::System.Object sender, global::System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.Controller.IsolationSelectionChanged();
        }

        private void TextAdvanceRun_TextChanged(global::System.Object sender, global::System.Windows.Controls.TextChangedEventArgs e)
        {
            this.Controller.AdvanceRunChanged();
        }

        private void TextAdvanceJvm_TextChanged()
        {
            this.Controller.AdvanceJvmChanged();
        }

        private void BtnAdvanceJvmReset_Click(global::System.Object sender, global::System.EventArgs e)
        {
            this.Controller.ResetAdvanceJvm();
        }

        private void ReplaceEnter(global::PCL.MyTextBox sender, global::System.Windows.Controls.TextChangedEventArgs e)
        {
            this.Controller.ReplaceEnter(sender, e);
        }

        private void BtnSwitch_Click(global::System.Object sender, global::System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Controller.SwitchToInstance();
        }
    }
}
