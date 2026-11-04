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
using PCLCS;
using global::PCLLF.Presentation.Launch;
using ThrottleDebounce;

namespace PCL
{

    // 原 LF 数据、窗口和 Aa/Ani 原语接线；两页完整流程及四类皮肤决策由 C# 承载。
    internal sealed class LaunchPageRuntime
    {
        static LaunchPageRuntime()
        {
            global::PCL.PresentationRuntime.EnsureConfigured();
        }
        internal static global::PCLLF.Presentation.Launch.LegacyLaunchAnimations<global::PCL.ModAnimation.AniData> Animations()
        {
            return new global::PCLLF.Presentation.Launch.LegacyLaunchAnimations<global::PCL.ModAnimation.AniData>()
            {
                Opacity = new Func<global::System.Object, global::System.Double, global::System.Int32, global::System.Int32, global::PCLLF.Presentation.Launch.LegacyLaunchEase, global::System.Boolean, global::PCL.ModAnimation.AniData>((Target, Value, Time, Delay, Ease, After) => global::PCL.ModAnimation.AaOpacity(Target, Value, Time, Delay, global::PCL.LaunchPageRuntime.ToEase(Ease), After)),
                ScaleTransform = new Func<global::System.Object, global::System.Double, global::System.Int32, global::System.Int32, global::PCLLF.Presentation.Launch.LegacyLaunchEase, global::System.Boolean, global::PCL.ModAnimation.AniData>((Target, Value, Time, Delay, Ease, After) => global::PCL.ModAnimation.AaScaleTransform(Target, Value, Time, Delay, global::PCL.LaunchPageRuntime.ToEase(Ease), After)),
                Width = new Func<global::System.Object, global::System.Double, global::System.Int32, global::System.Int32, global::PCLLF.Presentation.Launch.LegacyLaunchEase, global::System.Boolean, global::PCL.ModAnimation.AniData>((Target, Value, Time, Delay, Ease, After) => global::PCL.ModAnimation.AaWidth(Target, Value, Time, Delay, global::PCL.LaunchPageRuntime.ToEase(Ease), After)),
                Height = new Func<global::System.Object, global::System.Double, global::System.Int32, global::System.Int32, global::PCLLF.Presentation.Launch.LegacyLaunchEase, global::System.Boolean, global::PCL.ModAnimation.AniData>((Target, Value, Time, Delay, Ease, After) => global::PCL.ModAnimation.AaHeight(Target, Value, Time, Delay, global::PCL.LaunchPageRuntime.ToEase(Ease), After)),
                GridWidth = new Func<global::System.Object, global::System.Double, global::System.Int32, global::System.Int32, global::PCLLF.Presentation.Launch.LegacyLaunchEase, global::System.Boolean, global::PCL.ModAnimation.AniData>((Target, Value, Time, Delay, Ease, After) => global::PCL.ModAnimation.AaGridLengthWidth(Target, Value, Time, Delay, global::PCL.LaunchPageRuntime.ToEase(Ease), After)),
                TranslateX = new Func<global::System.Object, global::System.Double, global::System.Int32, global::System.Int32, global::PCLLF.Presentation.Launch.LegacyLaunchEase, global::System.Boolean, global::PCL.ModAnimation.AniData>((Target, Value, Time, Delay, Ease, After) => global::PCL.ModAnimation.AaTranslateX(Target, Value, Time, Delay, global::PCL.LaunchPageRuntime.ToEase(Ease), After)),
                Code = new Func<global::System.Action, global::System.Int32, global::System.Boolean, global::PCL.ModAnimation.AniData>((Work, Delay, After) => global::PCL.ModAnimation.AaCode(() => Work(), Delay, After)),
                Start = new Action<global::System.Collections.Generic.IEnumerable<global::PCL.ModAnimation.AniData>, global::System.String, global::System.Boolean>((Frames, Name, RefreshTime) => global::PCL.ModAnimation.AniStart((global::System.Collections.IList)Frames, Name, RefreshTime)),
                Stop = new Action<global::System.String>(Name => global::PCL.ModAnimation.AniStop(Name)),
                IsRunning = global::PCL.ModAnimation.AniIsRun
            };
        }
        private static global::PCL.ModAnimation.AniEase ToEase(global::PCLLF.Presentation.Launch.LegacyLaunchEase Ease)
        {
            switch (Ease)
            {
                case global::PCLLF.Presentation.Launch.LegacyLaunchEase.OutFluent:
                    {
                        return new global::PCL.ModAnimation.AniEaseOutFluent();
                    }
                case global::PCLLF.Presentation.Launch.LegacyLaunchEase.InFluent:
                    {
                        return new global::PCL.ModAnimation.AniEaseInFluent();
                    }
                case global::PCLLF.Presentation.Launch.LegacyLaunchEase.OutBackWeak:
                    {
                        return new global::PCL.ModAnimation.AniEaseOutBack(global::PCL.ModAnimation.AniEasePower.Weak);
                    }
                case global::PCLLF.Presentation.Launch.LegacyLaunchEase.OutFluentWeak:
                    {
                        return new global::PCL.ModAnimation.AniEaseOutFluent(global::PCL.ModAnimation.AniEasePower.Weak);
                    }
                case global::PCLLF.Presentation.Launch.LegacyLaunchEase.InFluentWeak:
                    {
                        return new global::PCL.ModAnimation.AniEaseInFluent(global::PCL.ModAnimation.AniEasePower.Weak);
                    }

                default:
                    {
                        return (global::PCL.ModAnimation.AniEase)null;
                    }
            }
        }
        internal static readonly global::PCLLF.Presentation.Launch.LegacyLaunchSkins<global::PCL.ModLoader.LoaderTask<(global::System.String, global::System.String), global::System.String>> Skins = new global::PCLLF.Presentation.Launch.LegacyLaunchSkins<global::PCL.ModLoader.LoaderTask<(global::System.String, global::System.String), global::System.String>>(new global::PCLLF.Presentation.Launch.LegacyLaunchSkinHost<global::PCL.ModLoader.LoaderTask<(global::System.String, global::System.String), global::System.String>>()
        {
            ReadString = new Func<global::System.String, global::System.String>(Key => global::PCL.Settings.Get<string>(Key)),
            ReadInteger = new Func<global::System.String, global::System.Int32>(Key => global::PCL.Settings.Get<int>(Key)),
            WriteSetting = new Action<global::System.String, global::System.Object>((Key, Value) => global::PCL.Settings.Set(Key, Value)),
            OfflinePageReloaded = new Func<global::System.Boolean>(() => (global::PCL.ModMain.FrmLoginLegacy is not null && (global::PCL.ModMain.FrmLoginLegacy.IsReloaded))),
            OfflineName = new Func<global::System.String>(() => global::PCL.ModMain.FrmLoginLegacy.ComboName.Text),
            Input = new Func<global::PCL.ModLoader.LoaderTask<(global::System.String, global::System.String), global::System.String>, (global::System.String, global::System.String)>(Task => Task.Input),
            ResetInput = new Action<global::PCL.ModLoader.LoaderTask<(global::System.String, global::System.String), global::System.String>>(Task => Task.Input = ((global::System.String, global::System.String))(default((global::System.String, global::System.String)))),
            Output = new Action<global::PCL.ModLoader.LoaderTask<(global::System.String, global::System.String), global::System.String>, global::System.String>((Task, Value) => Task.Output = Value),
            Canceled = new Func<global::PCL.ModLoader.LoaderTask<(global::System.String, global::System.String), global::System.String>, global::System.Boolean>(Task => Task.IsCanceled),
            Dispatch = new Action<global::System.Action>(Work => global::PCL.ModBase.RunInUi(Work)),
            ClearSkin = global::PCL.LaunchPageRuntime.ClearSkin,
            PageExists = global::PCL.LaunchPageRuntime.SkinPageExists,
            RenderSkin = global::PCL.LaunchPageRuntime.RenderSkin,
            ImagePath = new Func<global::System.String>(() => global::PCL.ModBase.PathImage),
            AppDataPath = new Func<global::System.String>(() => global::MeloongCore.Paths.AppDataThenName),
            OfflineUuid = Name => global::Microsoft.VisualBasic.CompilerServices.Conversions.ToString(global::PCL.ModLaunch.McLoginLegacyUuid(Name)),
            SkinSex = global::PCL.ModMinecraft.McSkinSex,
            DownloadSkin = global::PCL.ModMinecraft.McSkinDownload,
            SkinAddress = global::PCL.ModMinecraft.McSkinGetAddress,
            MojangUuid = (Name, ThrowOnNotFound) => global::Microsoft.VisualBasic.CompilerServices.Conversions.ToString(global::PCL.ModLaunch.McLoginMojangUuid(Name, ThrowOnNotFound)),
            Hint = new Action<global::System.String>(Text => global::PCL.ModMain.Hint(Text))
        });
        private static void ClearSkin(global::System.String Kind)
        {
            switch ((Kind ?? ""))
            {
                case "Legacy":
                    {
                        if ((global::PCL.ModMain.FrmLoginLegacy is not null && global::PCL.ModMain.FrmLoginLegacy.Skin is not null))
                            global::PCL.ModMain.FrmLoginLegacy.Skin.Clear();
                        break;
                    }
                case "Ms":
                    {
                        if ((global::PCL.ModMain.FrmLoginMsSkin is not null && global::PCL.ModMain.FrmLoginMsSkin.Skin is not null))
                            global::PCL.ModMain.FrmLoginMsSkin.Skin.Clear();
                        break;
                    }
                case "Nide":
                    {
                        if ((global::PCL.ModMain.FrmLoginNideSkin is not null && global::PCL.ModMain.FrmLoginNideSkin.Skin is not null))
                            global::PCL.ModMain.FrmLoginNideSkin.Skin.Clear();
                        break;
                    }
                case "Auth":
                    {
                        if ((global::PCL.ModMain.FrmLoginAuthSkin is not null && global::PCL.ModMain.FrmLoginAuthSkin.Skin is not null))
                            global::PCL.ModMain.FrmLoginAuthSkin.Skin.Clear();
                        break;
                    }
            }
        }
        private static global::System.Boolean SkinPageExists(global::System.String Kind)
        {
            switch ((Kind ?? ""))
            {
                case "Legacy":
                    {
                        return global::PCL.ModMain.FrmLoginLegacy is not null;
                    }
                case "Ms":
                    {
                        return global::PCL.ModMain.FrmLoginMsSkin is not null;
                    }
                case "Nide":
                    {
                        return global::PCL.ModMain.FrmLoginNideSkin is not null;
                    }

                default:
                    {
                        return global::PCL.ModMain.FrmLoginAuthSkin is not null;
                    }
            }
        }
        private static global::System.Action RenderSkin(global::System.String Kind)
        {
            switch ((Kind ?? ""))
            {
                case "Legacy":
                    {
                        return global::PCL.ModMain.FrmLoginLegacy.Skin.Load;
                    }
                case "Ms":
                    {
                        return global::PCL.ModMain.FrmLoginMsSkin.Skin.Load;
                    }
                case "Nide":
                    {
                        return global::PCL.ModMain.FrmLoginNideSkin.Skin.Load;
                    }

                default:
                    {
                        return global::PCL.ModMain.FrmLoginAuthSkin.Skin.Load;
                    }
            }
        }
        internal static global::System.Windows.FrameworkElement ReadPage(global::PCLLF.Presentation.Launch.LegacyLoginPageKind Kind)
        {
            switch (Kind)
            {
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Legacy:
                    {
                        return global::PCL.ModMain.FrmLoginLegacy;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Ms:
                    {
                        return global::PCL.ModMain.FrmLoginMs;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.MsSkin:
                    {
                        return global::PCL.ModMain.FrmLoginMsSkin;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Nide:
                    {
                        return global::PCL.ModMain.FrmLoginNide;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.NideSkin:
                    {
                        return global::PCL.ModMain.FrmLoginNideSkin;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Auth:
                    {
                        return global::PCL.ModMain.FrmLoginAuth;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.AuthSkin:
                    {
                        return global::PCL.ModMain.FrmLoginAuthSkin;
                    }

                default:
                    {
                        throw new global::System.ArgumentOutOfRangeException(nameof(Kind));
                    }
            }
        }
        internal static global::System.Windows.FrameworkElement CreatePage(global::PCLLF.Presentation.Launch.LegacyLoginPageKind Kind)
        {
            switch (Kind)
            {
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Legacy:
                    {
                        return new global::PCL.PageLoginLegacy();
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Ms:
                    {
                        return new global::PCL.PageLoginMs();
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.MsSkin:
                    {
                        return new global::PCL.PageLoginMsSkin();
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Nide:
                    {
                        return new global::PCL.PageLoginNide();
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.NideSkin:
                    {
                        return new global::PCL.PageLoginNideSkin();
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Auth:
                    {
                        return new global::PCL.PageLoginAuth();
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.AuthSkin:
                    {
                        return new global::PCL.PageLoginAuthSkin();
                    }

                default:
                    {
                        throw new global::System.ArgumentOutOfRangeException(nameof(Kind));
                    }
            }
        }
        internal static void StorePage(global::PCLLF.Presentation.Launch.LegacyLoginPageKind Kind, global::System.Windows.FrameworkElement Page)
        {
            switch (Kind)
            {
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Legacy:
                    {
                        global::PCL.ModMain.FrmLoginLegacy = ((global::PCL.PageLoginLegacy)(Page));
                        break;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Ms:
                    {
                        global::PCL.ModMain.FrmLoginMs = ((global::PCL.PageLoginMs)(Page));
                        break;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.MsSkin:
                    {
                        global::PCL.ModMain.FrmLoginMsSkin = ((global::PCL.PageLoginMsSkin)(Page));
                        break;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Nide:
                    {
                        global::PCL.ModMain.FrmLoginNide = ((global::PCL.PageLoginNide)(Page));
                        break;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.NideSkin:
                    {
                        global::PCL.ModMain.FrmLoginNideSkin = ((global::PCL.PageLoginNideSkin)(Page));
                        break;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.Auth:
                    {
                        global::PCL.ModMain.FrmLoginAuth = ((global::PCL.PageLoginAuth)(Page));
                        break;
                    }
                case global::PCLLF.Presentation.Launch.LegacyLoginPageKind.AuthSkin:
                    {
                        global::PCL.ModMain.FrmLoginAuthSkin = ((global::PCL.PageLoginAuthSkin)(Page));
                        break;
                    }
            }
        }
        internal static global::PCLLF.Presentation.Launch.LegacyLaunchJobPort Job(global::PCL.ModLoader.LoaderBase Loader, global::System.Func<global::System.Collections.Generic.IEnumerable<global::PCL.ModLoader.LoaderBase>> Items = null)
        {
            return new global::PCLLF.Presentation.Launch.LegacyLaunchJobPort()
            {
                State = new Func<global::PCLLF.Presentation.Launch.LegacyLaunchPanelState>(() => ((global::PCLLF.Presentation.Launch.LegacyLaunchPanelState)(((global::System.Int32)Loader.State)))),
                Progress = new Func<global::System.Double>(() => Loader.Progress),
                Wait = new Action<global::System.Boolean>(Force => Loader.WaitForExit(IsForceRestart: Force)),
                Cancel = new Action(() => Loader.Cancel()),
                Items = Items is null ? null : new Func<global::System.Collections.Generic.IEnumerable<global::PCLLF.Presentation.Launch.LegacyLaunchItemPort>>(() => Items().Select(Value => global::PCL.LaunchPageRuntime.Item(Value)))
            };
        }
        internal static global::PCLLF.Presentation.Launch.LegacyLaunchItemPort Item(global::PCL.ModLoader.LoaderBase Loader)
        {
            if (Loader is null)
                return (global::PCLLF.Presentation.Launch.LegacyLaunchItemPort)null;
            return new global::PCLLF.Presentation.Launch.LegacyLaunchItemPort()
            {
                State = new Func<global::PCLLF.Presentation.Launch.LegacyLaunchPanelState>(() => ((global::PCLLF.Presentation.Launch.LegacyLaunchPanelState)(((global::System.Int32)Loader.State)))),
                Name = new Func<global::System.String>(() => Loader.Name),
                Parent = new Func<global::PCLLF.Presentation.Launch.LegacyLaunchItemPort>(() => global::PCL.LaunchPageRuntime.Item(Loader.RealParent))
            };
        }
    }
}