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
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLCS;
using FolderCore = global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>;
using ThrottleDebounce;

namespace PCL
{
    public partial class PageSelectLeft : global::PCL.IRefreshable
    {
        private global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType> Controller;
        private global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType> Service
        {
            get
            {
                if (this.Controller is null)
                    this.Controller = new global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>(global::PCL.PageSelectLeft.CreateHost(this));
                return this.Controller;
            }
        }

        public PageSelectLeft()
        {
            __LfAttachOwnHandles();
            this.InitializeComponent();
        }

        private static global::PCLCS.FolderSelectionPageHost<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McInstance> CreateHost(global::PCL.PageSelectLeft Owner)
        {
            return new global::PCLCS.FolderSelectionPageHost<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McInstance>()
            {
                Panel = new Func<global::System.Windows.Controls.Panel>(() => global::PCL.ModMain.FrmSelectLeft.PanList),
                OwnerPanel = new Func<global::System.Windows.Controls.Panel>(() => Owner.PanList),
                Folders = new Func<global::System.Collections.Generic.List<global::PCL.ModMinecraft.McFolder>>(() => global::PCL.ModMinecraft.McFolderList),
                ModpackEnabled = new Func<global::System.Boolean>(() => global::PCL.ModModpackMode.Enabled),
                CurrentPageExists = new Func<global::System.Boolean>(() => global::PCL.ModMain.FrmSelectLeft is not null),
                AnyLocked = new Func<global::System.Boolean>(() => global::PCL.ModMinecraft.McInstanceLock.IsAnyLocked),
                Downloading = new Func<global::System.Boolean, global::System.Boolean>(IncludeHidden => global::PCL.ModNet.HasDownloadingTask(IncludeHidden)),
                PackFolder = new Func<global::PCL.ModMinecraft.McFolder>(() => global::PCL.ModModpackMode.CreatePackFolderList()[0]),
                BaseFolder = new Func<global::System.String>(() => global::MeloongCore.Paths.Base),
                SelectedFolder = new Func<global::System.String>(() => global::PCL.ModMinecraft.McFolderSelected),
                SetSelectedFolder = new Action<global::System.String>(Value => global::PCL.ModMinecraft.McFolderSelected = Value),
                ReadFolders = new Func<global::System.String>(() => global::PCL.Settings.Get<string>("LaunchFolders")),
                WriteFolders = new Action<global::System.String>(Value => global::PCL.Settings.Set("LaunchFolders", Value)),
                SubscribePreviewFinish = new Action<global::System.Action>(Finish => global::PCL.ModMinecraft.McFolderListLoader.PreviewFinish += _ => Finish()),
                UiWait = new Action<global::System.Action>(Work => global::PCL.ModBase.RunInUiWait(() => Work())),
                RunThread = new Action<global::System.Action>(Work => global::PCL.ModBase.RunInThread(() => Work())),
                RunNewThread = new Action<global::System.Action, global::System.String, global::System.Threading.ThreadPriority>((Work, Name, Priority) => global::PCL.ModBase.RunInNewThread(() => Work(), Name, Priority)),
                RefreshFolders = new Action(() => global::PCL.ModMinecraft.McFolderListLoader.Start(IsForceRestart: true)),
                RefreshInstances = global::PCL.PageSelectLeft.RefreshNativeInstances,
                Permission = new Func<global::System.String, global::System.Boolean>(Path => global::PCL.ModBase.CheckPermission(Path)),
                SelectFolder = new Func<global::System.String, global::System.Boolean, global::System.Collections.Generic.IEnumerable<global::System.String>>((Title, Multi) => global::MeloongCore.Wpf.Dialogs.SelectFolder(Title, Multi)),
                Input = global::PCL.PageSelectLeft.NativeInput,
                Message = new Func<global::PCLCS.SelectionPageMessage, global::System.Int32>(Request => global::PCL.ModMain.MyMsgBox(Request.Text, Request.Title, Request.Button1, Request.Button2, Request.Button3, Request.IsWarn)),
                Hint = global::PCL.PageSelectLeft.NativeHint,
                ParseMenu = new Func<global::System.String, global::System.Windows.Controls.ContextMenu>(Markup => (global::System.Windows.Controls.ContextMenu)global::PCL.ModBase.GetObjectFromXML(Markup)),
                BindMenu = global::PCL.PageSelectLeft.BindNativeMenu,
                FolderRow = new Func<global::PCL.ModMinecraft.McFolder, global::System.Windows.Controls.ContextMenu, global::System.Windows.FrameworkElement>((Folder, Menu) => new global::PCL.MyListItem() { IsScaleAnimationEnabled = false, Type = global::PCL.MyListItem.CheckType.RadioBox, MinPaddingRight = 30, Title = Folder.Name, Info = Folder.Location, Height = 40d, ContextMenu = Menu, Tag = Folder }),
                ActionRow = global::PCL.PageSelectLeft.CreateActionRow,
                ConfigureButton = global::PCL.PageSelectLeft.CreateConfigureButton,
                SetButtons = new Action<global::System.Windows.FrameworkElement, global::System.Collections.Generic.IEnumerable<global::System.Windows.FrameworkElement>>((Row, Buttons) => ((global::PCL.MyListItem)Row).Buttons = Buttons.Cast<MyIconButton>().ToArray()),
                BindChanged = new Action<global::System.Windows.FrameworkElement, global::System.Action<global::System.Windows.FrameworkElement, global::System.Boolean, global::System.Action>>((Row, Changed) => ((global::PCL.MyListItem)Row).Changed += (Sender, Args) => Changed((global::System.Windows.FrameworkElement)Sender, Args.RaiseByMouse, () => Args.Handled = true)),
                BindClick = new Action<global::System.Windows.FrameworkElement, global::System.Action>((Row, Click) => ((global::PCL.MyListItem)Row).Click += (_, __) => Click()),
                CheckRow = global::PCL.PageSelectLeft.CheckNativeRow,
                RowChecked = new Func<global::System.Windows.FrameworkElement, global::System.Boolean>(Row => ((global::PCL.MyListItem)Row).Checked),
                MenuRow = global::PCL.PageSelectLeft.NativeMenuRow,
                RowInfo = new Func<global::System.Windows.FrameworkElement, global::System.String>(Row => ((global::PCL.MyListItem)Row).Info),
                InstallPack = new Action(() => global::PCL.ModModpack.ModpackInstall()),
                NewInstance = new Func<global::System.String, global::PCL.ModMinecraft.McInstance>(Path => new global::PCL.ModMinecraft.McInstance(Path)),
                WriteInstanceInteger = new Action<global::System.String, global::System.Int32, global::PCL.ModMinecraft.McInstance>((Key, Value, Instance) => global::PCL.Settings.Set(Key, Value, Instance: Instance)),
                WriteInstanceBoolean = new Action<global::System.String, global::System.Boolean, global::PCL.ModMinecraft.McInstance>((Key, Value, Instance) => global::PCL.Settings.Set(Key, Value, Instance: Instance)),
                CreateProfiles = global::PCL.ModMinecraft.McFolderLauncherProfilesJsonCreate,
                OpenExplorer = new Action<global::System.String>(Path => global::PCL.ModBase.OpenExplorer(Path)),
                WriteIni = new Action<global::System.String, global::System.String, global::System.Object>((File, Key, Value) => global::PCL.ModBase.WriteIni(File, Key, Conversions.ToString(Value))),
                NextUuid = global::PCL.ModBase.GetUuid
            };
        }

        private static void RefreshNativeInstances(global::System.Boolean Force)
        {
            global::PCL.ModLoader.LoaderFolderRun(global::PCL.ModMinecraft.McInstanceListLoader, global::PCL.ModMinecraft.McFolderSelected, (Force) ? (global::PCL.ModLoader.LoaderFolderRunType.ForceRun) : (global::PCL.ModLoader.LoaderFolderRunType.RunOnUpdated), MaxDepth: 1, ExtraPath: @"versions\");
        }

        private static void NativeHint(global::System.String Text, global::PCLCS.SelectionPageHint Kind)
        {
            switch (Kind)
            {
                case global::PCLCS.SelectionPageHint.Red:
                {
                    global::PCL.ModMain.Hint(Text, global::PCL.ModMain.HintType.Red);
                    break;
                }

                case global::PCLCS.SelectionPageHint.Blue:
                {
                    global::PCL.ModMain.Hint(Text, global::PCL.ModMain.HintType.Blue);
                    break;
                }

                default:
                {
                    global::PCL.ModMain.Hint(Text, global::PCL.ModMain.HintType.Green);
                    break;
                }
            }
        }

        private static global::System.String NativeInput(global::PCLCS.SelectionPageInput Request)
        {
            var Rules = new global::System.Collections.ObjectModel.Collection<global::PCL.Validate>();
            if (Request.DeleteConfirmation)
            {
                Rules.Add(new global::PCL.ValidateSame("Potato", $"请输入 {(global::PCL.ModBase.vbLQ)}Potato{(global::PCL.ModBase.vbRQ)}", IgnoreCase: true));
            }
            else
            {
                Rules.Add(new global::PCL.ValidateNullOrWhiteSpace());
                Rules.Add(new global::PCL.ValidateLength(1, 30));
                Rules.Add(new global::PCL.ValidateExcept(new[] { ">", "|" }));
            }

            return global::PCL.ModMain.MyMsgBoxInput(Request.Title, Request.Caption, Request.Default, Rules, Button1: Request.Button1, Button2: Request.Button2, IsWarn: Request.IsWarn);
        }

        private static void BindNativeMenu(global::System.Windows.Controls.ContextMenu Menu, global::System.String Name)
        {
            switch ((Name ?? ""))
            {
                case "Remove":
                {
                    ((global::PCL.MyMenuItem)Menu.FindName(Name)).AddHandler(global::System.Windows.Controls.MenuItem.ClickEvent, new global::System.Windows.RoutedEventHandler(global::PCL.ModMain.FrmSelectLeft.Remove_Click));
                    break;
                }

                case "Open":
                {
                    ((global::PCL.MyMenuItem)Menu.FindName(Name)).AddHandler(global::System.Windows.Controls.MenuItem.ClickEvent, new global::System.Windows.RoutedEventHandler(global::PCL.ModMain.FrmSelectLeft.Open_Click));
                    break;
                }

                case "Delete":
                {
                    ((global::PCL.MyMenuItem)Menu.FindName(Name)).AddHandler(global::System.Windows.Controls.MenuItem.ClickEvent, new global::System.Windows.RoutedEventHandler(global::PCL.ModMain.FrmSelectLeft.Delete_Click));
                    break;
                }

                case "Rename":
                {
                    ((global::PCL.MyMenuItem)Menu.FindName(Name)).AddHandler(global::System.Windows.Controls.MenuItem.ClickEvent, new global::System.Windows.RoutedEventHandler(global::PCL.ModMain.FrmSelectLeft.Rename_Click));
                    break;
                }

                case "Refresh":
                {
                    ((global::PCL.MyMenuItem)Menu.FindName(Name)).AddHandler(global::System.Windows.Controls.MenuItem.ClickEvent, new global::System.Windows.RoutedEventHandler(global::PCL.ModMain.FrmSelectLeft.Refresh_Click));
                    break;
                }

                default:
                {
                    throw new global::System.ArgumentOutOfRangeException(nameof(Name));
                }
            }
        }

        private static global::System.Windows.FrameworkElement NativeMenuRow(global::System.Object Sender)
        {
            return ((global::PCL.MyListItem)(((global::System.Windows.Controls.Primitives.Popup)(((global::System.Windows.Controls.ContextMenu)(((dynamic)Sender).Parent)).Parent)).PlacementTarget));
        }

        private static void CheckNativeRow(global::System.Windows.FrameworkElement Row, global::System.Boolean Checked, global::System.Boolean Silent)
        {
            if (Silent)
            {
                ((global::PCL.MyListItem)Row).SetChecked(Checked, false, false);
            }
            else
            {
                ((global::PCL.MyListItem)Row).Checked = Checked;
            }
        }

        private static global::System.Windows.FrameworkElement CreateConfigureButton(global::System.Action Click)
        {
            var Button = new global::PCL.MyIconButton()
            {
                Logo = global::PCL.ModBase.Logo.IconButtonSetup,
                LogoScale = 1.1d
            };
            Button.Click += (_, __) => Click();
            return Button;
        }

        private static global::System.Windows.FrameworkElement CreateActionRow(global::System.Int32 Kind)
        {
            switch (Kind)
            {
                case 0:
                {
                    return new global::PCL.MyListItem()
                    {
                        IsScaleAnimationEnabled = false,
                        Type = global::PCL.MyListItem.CheckType.Clickable,
                        Title = "新建 .minecraft 文件夹",
                        Height = 34d,
                        ToolTip = "在 PCL 当前所在文件夹下创建新的 .minecraft 文件夹",
                        LogoScale = 0.9d,
                        Logo = "M103.331925 384.978025l25.805736 0L129.137661 161.847132c0-18.313088 14.905478-33.718963 33.718963-33.718963l0.969071 0 253.006318 0c10.82044 0 20.218484 4.797259 26.500561 12.257162l117.579929 126.753869 297.819966 0c18.297738 0 33.736359 15.179724 33.736359 33.977859l0 0.952698 0 82.909292 25.547863 0c18.538215 0 34.187637 15.179724 34.187637 33.977859 0 2.163269-0.469698 3.617387-0.469698 5.539156l-54.437843 432.971086c-1.210571 10.382465-7.007601 19.056008-14.968923 24.352641-6.249331 5.765307-14.680351 9.624195-23.595394 9.624195l-0.969071 0-694.906773 0c-9.155521 0-17.344017-3.858888-23.626094-9.155521-8.67252-5.765307-14.453177-14.939247-15.389502-25.758664L69.597613 423.040922c-2.165316-18.313088 10.868535-35.414581 29.665647-38.062897L103.331925 384.978025 103.331925 384.978025zM196.576609 384.978025 196.576609 384.978025l627.938546 0 0-49.625234L546.461371 335.352791l0 0c-9.400091 0-18.329461-4.117784-25.048489-11.110035L402.363486 196.067514 196.576609 196.067514 196.576609 384.978025 196.576609 384.978025zM879.469767 452.916347 879.469767 452.916347l-20.267603 0-0.469698 0-0.969071 0-694.906773 0-0.984421 0-20.218484 0 45.781696 366.728382 646.218888 0L879.469767 452.916347 879.469767 452.916347z"
                    };
                }

                case 1:
                {
                    return new global::PCL.MyListItem()
                    {
                        IsScaleAnimationEnabled = false,
                        Type = global::PCL.MyListItem.CheckType.Clickable,
                        Title = "添加已有文件夹",
                        Height = 34d,
                        ToolTip = "将一个已有的 Minecraft 文件夹添加到列表",
                        Logo = "M512.277 954.412c-118.89 0-230.659-46.078-314.73-129.73S67.12 629.666 67.12 511.222s46.327-229.744 130.398-313.427 195.82-129.73 314.73-129.73 230.659 46.078 314.72 129.73S957.397 392.81 957.397 511.183 911.078 740.96 826.97 824.642s-195.8 129.77-314.692 129.77z m0-822.784c-101.972 0-197.809 39.494-269.865 111.222s-111.7 166.997-111.7 268.373 39.653 196.695 111.67 268.335S410.246 890.78 512.248 890.78s197.809-39.484 269.865-111.222 111.7-166.998 111.67-268.374c-0.03-101.375-39.654-196.665-111.67-268.303S614.22 131.628 512.277 131.628z m222.585 347.8H544.073V288.64c-0.76-17.561-15.613-31.18-33.173-30.419-16.495 0.714-29.704 13.924-30.419 30.419v190.787H289.703c-17.56 0.761-31.179 15.614-30.419 33.174 0.715 16.494 13.924 29.703 30.42 30.418H480.48v190.788c0.761 17.56 15.614 31.179 33.174 30.419 16.494-0.715 29.703-13.925 30.418-30.42V543.02h190.788c17.56 0.762 32.413-12.857 33.173-30.418 0.762-17.561-12.858-32.414-30.419-33.174a31.683 31.683 0 0 0-2.753 0z"
                    };
                }

                case 2:
                {
                    return new global::PCL.MyListItem()
                    {
                        IsScaleAnimationEnabled = false,
                        Type = global::PCL.MyListItem.CheckType.Clickable,
                        Title = "导入整合包",
                        Height = 34d,
                        ToolTip = "在当前选择的 Minecraft 文件夹下安装整合包",
                        Logo = "M512 40.96C249.344 40.96 35.84 252.416 35.84 512s213.504 471.04 476.16 471.04c103.424 0 202.752-33.28 286.72-96.256l1.536-1.536c5.12-5.632 7.68-12.8 7.68-19.968 0-16.896-13.824-30.208-30.72-30.208-7.68 0-15.36 2.56-20.992 7.68h-0.512c-71.68 52.224-155.648 79.36-243.712 79.36-227.328 0-412.16-182.784-412.16-407.552 0-224.768 184.832-407.552 412.16-407.552s412.16 182.784 412.16 407.552c0 68.608-15.872 132.608-46.592 190.464-0.512 1.024-1.024 2.048-1.024 3.072-0.512 2.048-1.536 4.608-1.536 8.192 0 16.896 13.824 30.208 30.72 30.208 12.288 0 23.04-7.168 28.16-18.432 35.84-68.608 53.76-141.312 53.76-216.064 0.512-259.584-212.992-471.04-475.648-471.04z M812.032 483.328c-31.744-20.992-71.68 1.536-78.848 6.144-1.024 0.512-104.448 61.44-128 74.752-8.192 4.608-22.528-0.512-27.136-4.096-31.232-36.352-54.272-70.656-68.608-102.4-13.312-29.184 0.512-41.472 3.072-43.52 7.168-4.608 114.688-68.608 143.36-83.456 24.064-12.288 40.96-25.088 46.08-45.056 3.072-13.312 0-27.136-9.216-39.936-22.016-31.744-172.544-84.992-311.296-3.584-157.184 91.648-152.064 242.688-150.528 292.352v9.216c0 18.944-12.8 37.376-14.848 40.448l-20.992 21.504c-6.144 6.144-9.216 13.824-9.216 22.528 0 8.704 3.584 16.384 9.728 22.528 12.8 12.288 32.768 11.776 45.056-0.512l22.528-23.552 0.512-0.512c3.072-3.584 30.208-38.4 30.208-81.92l-0.512-11.264c-1.536-44.544-5.632-162.816 119.296-235.52 88.064-51.2 173.056-32.256 208.896-19.968-36.864 19.456-143.36 83.456-144.896 84.48-22.016 14.336-55.808 58.88-26.112 122.88 17.408 37.376 43.52 76.8 80.896 120.32 14.336 17.408 62.976 37.376 103.424 15.36 24.576-13.312 125.44-73.216 130.048-75.776 2.048-1.024 4.608-2.56 7.68-3.584 0 2.56-0.512 6.144-1.024 10.752-5.632 35.84-35.328 155.136-191.488 181.76-49.664 8.704-89.6 3.584-121.856-0.512h-0.512c-37.888-4.608-73.216-9.216-101.888 14.336-31.232 26.112-40.96 34.304-35.84 54.272 3.584 14.336 16.384 24.064 30.72 24.064 2.56 0 5.12-0.512 7.68-1.024 6.656-1.536 12.8-5.632 16.896-10.752 2.048-2.048 7.68-6.656 20.992-18.432 6.656-5.632 25.088-3.584 52.736 0 34.816 4.608 81.92 10.24 141.312 0.512 157.184-26.624 228.864-138.752 243.2-234.496 7.68-38.912 0-64.512-21.504-78.336z"
                    };
                }

                default:
                {
                    throw new global::System.ArgumentOutOfRangeException(nameof(Kind));
                }
            }
        }

        private void PageSelectLeft_Initialized(global::System.Object sender, global::System.EventArgs e)
        {
            this.Service.Initialize();
        }

        private void PageSelectLeft_Loaded(global::System.Object sender, global::System.Windows.RoutedEventArgs e)
        {
            this.Service.Loaded();
        }

        private void McFolderListUI()
        {
            this.Service.Render();
        }

        internal void RefreshForModpackMode()
        {
            this.Service.RefreshForModpackMode();
        }

        internal void RenderPackFolderList()
        {
            this.Service.RenderPackFolderList();
        }

        public void Add_Click()
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.Add(global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public static void AddFolder(global::System.String FolderPath, global::System.String DisplayName, global::System.Boolean ShowHint)
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.AddFolder(FolderPath, DisplayName, ShowHint, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public void Create_Click()
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.Create(global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public static void CreateMcFolderInCurrentPath()
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.CreateInCurrentPath(global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public void Remove_Click(global::System.Object sender, global::System.Windows.RoutedEventArgs e)
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.Remove(sender, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public void Delete_Click(global::System.Object sender, global::System.Windows.RoutedEventArgs e)
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.Delete(sender, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        private static global::System.String RemoveFolderFromSetup(global::PCL.ModMinecraft.McFolder Target)
        {
            return global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.RemoveFromSetup(Target, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public void Open_Click(global::System.Object sender, global::System.Windows.RoutedEventArgs e)
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.Open(sender, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public void Refresh_Click(global::System.Object sender, global::System.Windows.RoutedEventArgs e)
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.RefreshClick(sender, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public void RefreshCurrent()
        {
            global::PCL.PageSelectLeft.RefreshCurrent(global::PCL.ModMinecraft.McFolderSelected);
        }

        void global::PCL.IRefreshable.Refresh() => this.RefreshCurrent();
        public static void RefreshCurrent(global::System.String Folder)
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.Refresh(Folder, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public void Rename_Click(global::System.Object sender, global::System.Windows.RoutedEventArgs e)
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.Rename(sender, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }

        public void Folder_Change(global::PCL.MyListItem sender, global::PCL.ModBase.RouteEventArgs e)
        {
            global::PCLCS.FolderSelectionPageController<global::PCL.ModMinecraft.McFolder, global::PCL.ModMinecraft.McFolder.Types, global::PCL.ModMinecraft.McInstance, global::PCL.ModMinecraft.McInstance.McInstanceState, global::PCL.ModMinecraft.McInstanceCardType>.ChangeFolder(sender, e.RaiseByMouse, () => e.Handled = true, global::PCL.PageSelectLeft.CreateHost((global::PCL.PageSelectLeft)null));
        }
    }
}
