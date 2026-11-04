# LF 源码模块与迁移依赖对照

本图用于 G002 和后续小批次迁移。工程主线是现有 LF；PCL 是基础源码来源；CE 是可选择适配的同源参考。当前 goal 只包含 **C#／.NET 10／WPF 的完整等价迁移和必要重构**。新增 MMC 能力、性能优化及 Rust／C++ 方案均 deferred；现有 MMC 导入和现有原生调用仍须保留并回归。执行状态以 [MIGRATION_STATUS](../MIGRATION_STATUS.md) 为准，本图不表示代码迁移或 Windows 验收已经完成。

## 固定来源与阅读约定

| 来源 | 固定版本 | 使用方式 |
| --- | --- | --- |
| LF 起点 | `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`，1.3.4 | 当前仓库的功能、数据和交互验收主体；逐文件清单见 [source-inventory.baseline.json](source-inventory.baseline.json) |
| PCL 基线 | `5ef81e490ae46b1100d61e33d0183094c7a80ca8`，2.14.0.0 | 用本地 Git 比较来源；见 [NOTICE](../../NOTICE.md) 和 [固定上游树][pcl] |
| CE 参考 | 稳定 `v2.15.1`／`1a7653127d578efdb2a9388999a3fdc67d9e49d5` | 下文 CE 链接全部固定到该提交；更晚的 dev 拆分不能当作本稳定版现状 |

来源标记：**P** 为基线已有实现，**P+LF** 为基线实现加 LF 修改，**LF** 为 LF 新增文件。由 `git diff --name-status` 及实际调用点核对；标记说明文件来源，不裁定某个算法的原创归属。固定清单记录 168 个 VB／55,900 行、10 个 C#／1,034 行、71 个 XAML／5,540 行，含注释和空行、不含子模块内容；其中 LF 相对上游新增 56 个 VB／12,129 行，此外还有原文件修改。后续工作树中的 C# 提取不计入这个固定基线数字。

LF 路径简写：**B** = [Modules/Base][lf-base]，**G** = [Modules/Minecraft][lf-game]，**R** = [Modules/Resource][lf-resource]，**U** = [Controls][lf-controls]，**P** = [Pages][lf-pages]，均位于 `Plain Craft Launcher 2/` 下。下文脚本均位于 [scripts][scripts]。CE 的“候选”指需要核对行为、依赖和许可的源码；同名不代表等价，没有同名也不代表 CE 没有该能力。实际引入须记录提交、文件、署名、许可及 LF 修改。

## 当前批量迁移的固定来源与适配

下表接续原模块条目，仅记录本批实际采用的边界与来源；验收状态仍以进度账本为准。LF源码均固定于`f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`，PCL对照为`5ef81e490ae46b1100d61e33d0183094c7a80ca8`；本批CE参考均为稳定`1a7653127d578efdb2a9388999a3fdc67d9e49d5`，未改用CE工程或未固定dev。

CE许可按实际路径核对：`Plain Craft Launcher 2/**`使用该目录的`LICENCE`；`PCL.Core/**`使用根`LICENSE`（Apache-2.0），不能用根Apache许可概括应用源码。LF根`LICENCE`和原署名继续保留。下表“参考”仅表示审阅；选择性复用另行明确。

| 批次／LF源 | 相对PCL及保留范围 | CE固定路径／实际使用 |
| --- | --- | --- |
| C015 `LaunchDependencyCheck`／`ModDependencyAutoFill` | 两文件LF特有；保acceptance stamp、disabled-JAR闭包、候选重试、stage/hash/本地声明复查与混合回滚 | 应用`Modules/Minecraft/ModCompDependency.cs`及Core`Minecraft/ResourceProject/ModDependencyResolver.cs`仅确认/分层参考，不替换LF文件事务 |
| C016 四依赖控件 | PCL无对应；保图分组／同node Tag／原DTO与icon并发和unload generation | 应用`ModLocalComp.cs`／`ModCompDependency.cs`／`MyLocalModItem.xaml.cs`仅C#图像/控件边界参照，无等价图组件可直接替换 |
| C017 `ModJava`／`ModMinecraft` | Java原规则与PCL相同；Minecraft保LF pack scope、图标、recent worlds与取消安全扫描；原Java.cs/Configs/Shared不替换 | 应用`Modules/Minecraft/ModJava.cs`、`McInstance.cs`、`ModFolder.cs`、`ModInstanceList.cs`、`ModLibrary.cs`、`ModSkin.cs`仅C#迁移对照；不引新Java/MMC策略，GB18030仅局部provider |
| C018 五资源模块 | Version/LocalFile/LocalLoaders同PCL；Project/Searcher保LF Map `/worlds/`、`/maps/`和class17差异 | 应用`ModComp.cs`的Project/File/Request/Storage和`ModLocalComp.cs`为结构参考；不引STJ、SQLite、ConcurrentDictionary或CE检索/更新政策 |
| C019a `ModDownload` | 保LF删除19行更新通知及ToolUpdate写入；原6struct/20Loader字段不变 | 应用`Modules/Minecraft/ModDownload.cs`选择性适配C#构造/解析结构，保持LF分支和NeoForge格式；无Cleanroom或新源策略 |
| C019b `Pages/PageDownload/ModDownloadLib.vb` | 保LF MinecraftFolder、真实workload锁与修改前WorldBackups/History；原55入口/Req11字段、原进程/清理/提交次序 | 应用`Pages/PageDownload/ModDownloadLib.cs`仅选择性翻译/结构参考，保LF原有loader列表/Java与回滚行为，不加Quilt/Cleanroom/新规则 |
| C021a-b `ModLaunch`／`ModWatcher`／`ModCrash` | 当前LF与历史07dd350修复为准；保账号原子接口/依赖先确认/个人习惯/QuickPlay/备份/健康标记/中文报告与隐私过滤 | 应用`Modules/Minecraft/ModLaunch.cs`／`ModWatcher.cs`及`CrashAnalysis/{Logs/CrashLogPreparer,Presentation/CrashResultFormatter,CrashFileIo}.cs`仅参考，不换CE auth/分类/启动策略 |
| C024b `ModNet` | 保LF SafeRedirect/credential flag/canceled-merge/unknown-size EOF/无Telemetry；Static局部真实按实例保留 | 应用`Modules/Network/{Facade/ModNet,Http/Requester,Loaders/LoaderDownload}.cs`仅组织参考，不替换LF Thread/range/CacheCow引擎；NET10显式对两个handler转接唯一现SPM policy以保既有证书设置 |
| C031启动协调 | 两LF新增helper固定419909eb9c5a1404924d26f374eb8864f64f7521/22d549d24e125c356c07a88122b23ccc4e81839c；保官方/LF mutex、异常释放、Unicode/6402/三caption与原序 | 没有CE替代组件；直接迁LF BCL/Win32，不初始化Shared/账号/日志，原Application锁先于设置次序保持 |
| C027d 页面/窗口/剪贴板/剩余控件 | LF原15输入与f4逐字节同源，保完整Page/Loader状态、真实Win32flags/滚动/clipboard/extra/button/menu语义 | CE固定对应Controls文件仅C#参考（应用目录LICENCE），无CE MyResizer；Clipboard保uye特许Apache-2.0署名。实际固定Shared c774 `Wpf/General/ValueConverters.cs`四converter namespace适配，适用MeloongCore/LICENSE.txt Apache-2.0，未导旧WPF DLL到modern |
| C027c 六控件 | 原LF六控件/三XAML不改视觉/枚举/DP/事件/等待及线程/缓存时序，真实验证/位图/动画复用 | 应用`Controls/MyLoading.xaml.cs`、`MyRadioBox.xaml.cs`、`MySlider.xaml.cs`、`MyTextBox.cs`、`MyComboBox.cs`、`MyImage.cs`结构对照；不引CE FluentValidation/HintText DP/图片async/dedup策略 |
| C020a `ModModpack`／`ModModpackMode` | Modpack保LF模式导入限制/Source记录/隔离路由，Mode为LF特有；现有MMC等格式仅等价迁移 | 应用`Modules/Minecraft/ModModpack.cs`选择性翻译/结构参考，保LF识别与解包/worker/模式policy，不引CE新增格式/loader策略 |
| C020b `ModpackUpdate`／`ModpackUpdateFlow` | LF特有；除初始f4还保当前事务/取消修复`07dd3503b38b39095c8b92b40ae0454907387f9f`，同一作者基线/备份日志/反向回滚/真实负载锁 | CE固定stable应用`ModModpack.cs`仅导入分层参照，没有LF更新事务可等价替换，实际算法从当前LF转换 |
| C020c 导出/更新对话框/整合包设置页 | 导出保当前LF07dd350修复、exe/config/privacy/isolation/mode/Like及三stage loader；setup/dialog为LF独有 | 应用`Pages/PageInstance/PageInstanceExport.xaml.cs/.xaml`只翻译参考，不引CE DP/globbing/新导出政策；真实Windows完整导出未验 |
| C023b 两启动主页/四skin | Left保LF+5/-5、Right+150/-5；真实原static Loader字段/进度/取消/nullable hash/25预设及C006适配保持 | 应用`Pages/PageLaunch/PageLaunch{Left,Right}.xaml.cs`仅C#结构参考，按应用LICENCE；不引profile/本地化/新source/live-home-watchers，25原VB/C#literal逐值核对 |
| C030a UI/Launch/System设置 | LF相对PCL后台+22/-151、+37/-0、+13/-53；保LF默认背景/个人同步/模式/非赞助解锁/可选页默认/更新与原RAM/真实JavaList字段 | 应用`Pages/PageSetup/PageSetup{UI,Launch,LauncherMisc,Update}.xaml.cs`仅参考，按应用LICENCE；无Config.Profile/dark/video/初始RAM/新Javapolicy替换，39原VB RAM样本逐bit相等 |
| C023a 九登录/皮肤/设备页 | 保LF原C022 atomic账号/旧 DTO/Loader/cache与原页/BAML；不改格式/存储/错误/取消/HasCape原语义 | 应用`Pages/PageLaunch/PageLogin{Ms,Offline,Auth,ProfileSkin}.xaml.cs`及`MySkin.xaml.cs`仅同源参考，按目录LICENCE；不引CE profile/provider/本地化新策略 |
| C024c 剩余ModBase | LF原模块在ebffef884d2b49b275a3379e62c170d892679db8，全部46方法/INI/search/线程/资源/XML/24SVG及真实字段初始化保留 | 应用`Modules/Base/ModBase.cs`仅转换参考；固定Shared c774原API直接复用，GB18030只局部provider回退，自建PSI显式保Framework shell默认，无全局注册/新INI锁 |
| C022 `Settings.*`／`SettingService` | 保LF共享账号/偏好/旧DES及205定义40回调；真实注册表/INI和字段唯一 | 应用`ModSetup.cs`和Core`App/Configuration/ConfigItem.cs`、`Storage/CatIniFileProvider.cs`仅边界参考；不迁CE profile/AES/deferred/JSON存储政策 |
| C024a／C025 `ModValidate`／`ModLoader`／`LoaderFileOperation` | 校验同PCL，保Object父目录选项/Err；Loader保LF真实负载文件guard及取消收尾 | Core`Utils/Validate`仅分层参考，不换FluentValidation；应用`ModLoader.cs`仅C#组织参考，不换Task/Abort线程策略 |
| C026b `MyBitmap`／`ModBase.MyColor/MyRect` | 保LF pack URI扩展、静态cache、Imazen和原stream短读；Color/Rect同PCL | 应用`Modules/Base/MyBitmap.cs`／`ModBase.cs`仅转换参考；不引WIC/cache/Equals/HSL策略改变，复用LF既有G15适配 |
| C027b 六通用控件及三XAML | LF原视觉、状态、事件/字段/DP/动画/懒加载与shadow cache保持 | 应用`Controls/MyDropShadow.cs`的C#翻译选择性复用，保Microsoft Reference Source署名；其它控件参考后从LF转换，不引CE BlurBorder/theme/scale-marker |

## 现有工程边界与必须先守住的约束

| 当前工程 | 实际目标与引用 | 迁移含义 |
| --- | --- | --- |
| [LF 主程序][lf-project] | 旧式 VB WPF，.NET Framework 4.8，程序集 `LivingPCL`；引用 PCLCS、MeloongCore、MeloongCore.Wpf | `RootNamespace=PCL`、XAML `x:Class`、资源 URI、`My Project` 生成项和启动入口要一起核对 |
| [PCLCS][lf-cs-project] | `net48`、C# 13；引用 Shared 和 Wpf，并有 Wpf 全局 using | 已有 C# 辅助层，**不是已经完成 UI 解耦的核心**；新提取的纯解析器可以单独无 UI，不能据此概括整个程序集 |
| [MeloongCore.Shared][lf-shared-project]／[Wpf][lf-wpf-project] | Shared 为 `netstandard2.0`；Wpf 为 `net48`，引用 PresentationFramework 等 | 保留实际依赖并分别验证现代 .NET 兼容；不要以 Shared 可引用推断 Wpf 已兼容 |
| CE [主程序][ce-project]／[PCL.Core][ce-core-project] | 均为 `net10.0-windows`，均启用 WPF | 不能把其 DLL 直接加入 LF 的 net48 阶段；选择适用源码及依赖，或待运行时阶段再评估项目级引用 |

源码已经存在的交叉调用必须显式处理：

- `Application` 在普通启动中获取 `LauncherSessionLock`，随后才可使用共享账号／设置；启动时还恢复个人设置和 Quick Play 的已退出会话。更新命令在更早入口单独处理。
- `McLaunchStart` 组合 Java、账号、文件补全、存档恢复、依赖检查、参数／natives、预启动、Java 进程及结束处理；`ModWatcher` 根据实际进程与退出状态触发存档备份和环境正常标记。只转换参数构造不构成完整启动迁移。
- `EnvironmentHistory`、`InstanceCloner`、`RecentEnvironmentChanges` 使用 `ModpackUpdate` 的哈希／文件事务等能力。应先抽出这些叶子规则，避免新的通用文件层反向依赖“整合包更新服务”。
- `McInstanceLock` 绑定 `IWorker.Stopped` 释放；`ModLoader.Cancel` 先改变可见状态，子负载可能尚未结束。`LoaderFileOperation` 递归包装任务、禁止封闭后的迟到负载并等待运行计数归零。**版本占用必须覆盖实际负载及其 finally／回滚，不能改成看见 Canceled 就解锁。**
- `InstanceResourceEntry.Icon` 是 `ImageSource`，`InstanceResourceStore.LoadImage` 返回 `BitmapSource`。抽资源模型时要先分离图片呈现；不能把原文件整体搬入纯业务层后声称已无 WPF 依赖。

目标依赖方向是“页面／控件 → 应用流程 → 解析、模型、规则及文件计划”，网络、磁盘、Windows、Java 进程和 Dispatcher 通过适配入口提供。纯规则不得新增对 `FrmMain`、页面、`MyMsgBox`、`ImageSource`、全局选中实例或网络请求的依赖；过渡期可保留原门面转发。不要为语言转换先强行拆程序集，也不要新增 C# 辅助工程反向引用 VB 主程序的环。

## 共享模型与适配边界

以下是 B001 的边界定义；除已接入的存档读取接口外，其余模型的生产实现随所属功能任务迁移，避免先添加无人调用的平行框架。

| 边界 | 传递的数据与现有来源 | 外层适配及验收约束 |
| --- | --- | --- |
| 实例上下文 | 名称、版本目录、实际游戏目录、隔离策略、Minecraft／加载器版本；从当前 `McInstance` 显式取得 | 页面与启动入口构造上下文；规则不直接读取全局选中实例或访问 `FrmMain`，保留路径与共享目录判定 |
| 组件与安装清单 | 优先沿用 `PCLCS.McVersion`、`McComponents`、`McInstallManifest` 的现有字段和语义 | 按消费者需要分离日志、JSON 和文件访问，保留精确版本、受管文件及用户改动信息，避免创建第二套不一致的组件字典 |
| 依赖检查结果与补全计划 | 本地声明、平台来源及匹配可靠性、禁用／内嵌组件、问题指纹、拟启用和拟下载文件 | 查询、下载、缓存和确认由外层提供；规则产出计划，实际修改后重新检查，保持本地降级与会话内已接受问题语义 |
| 现有启动计划 | 原 Java 要求、选中 Java、JVM／游戏参数、classpath 顺序、natives 与工作目录、Quick Play 上下文 | 账号取得、进程创建、窗口和日志由适配层处理；保留参数语义和脱敏，真实凭据不写入样本或计划证据；本轮不新增 MMC 规则 |
| 文件变更与恢复计划 | 规范化相对路径、允许的根目录、变更前摘要、暂存与目标、事务日志及覆盖范围版本 | 操作持有者统一提供取消、进度、磁盘提交／回滚与锁；占用持续到真实收尾结束，不将 UI Canceled 状态当作工作完成 |
| 存档元数据 | `WorldMetadataReader.Read(Stream, bool)` 解析已解压 NBT；`WorldMetadataFiles.Read/Describe` 负责文件／gzip／详情，返回 `WorldMetadataInfo` 或原中文说明 | 原 VB 类型保留 16 字段映射与原调用入口；路径检查和日志通过委托接入现有适配，8 MiB 与异常边界不变。27 组流样本及 15 组真实文件／格式样本通过；Windows 原消费者结果另记账本，页面后台仍按对应 C 任务迁移 |
| 本机游戏资料 | `LocalGameDataStore` 接收动态根目录和原路径校验，负责 JObject／UTF-8／SHA256／原子写入和进程身份检查 | VB 仍持有原 `Gate`、动态 `TestRoot` 和全部兼容入口；不新增内部锁或改写调用端的锁范围。沿用 LF 自带 Newtonsoft DLL、`.pcl-backup`、陈旧摘要拒绝和临时文件清理；权限不明的进程检查仍保守保留会话 |
| 个人选项规则 | `PersonalOptionsRules` 解析、转换键码／FOV、解码和合并 options 文本；只读类别表，`MergeText` 接收动态选中谓词 | VB 继续读取原 Settings 并保留入口；文件事务与会话在 C004b／c 单独迁移。保留首冒号、大小写／别名、未知字段、重复行、混合换行和原末尾状态；对 .NET 10 非有限浮点接受变化作旧 FOV 格式推断的窄适配，不更换同步规则 |
| 个人选项文件 | `PersonalOptionsFiles.Read/ApplyMerged` 读取原始字节并提交已合并文本，复用现有 `LocalGameDataStore` 原子写入 | 4 MiB／链接检查和默认读取共享方式保持；无变化不写／不校验摘要／不清缓存，变化保留原 BOM 前缀和备份，成功后回调清缓存；调用者继续持有原 Gate，不增加锁或回滚策略 |
| 个人选项会话 | `PersonalOptionsFlow` 负责导入／Prepare／Track／Capture／Finish／恢复；`IPersonalOptionsInstance` 五项惰性属性及可继承字段 DTO 对接原实例／设置 | 复用原 Store 与 Monitor Gate；保留动态求值、恢复先行、日志状态和异常顺序。VB 原会话类型继承 DTO，由工厂返回同一派生对象；路径／设置／缓存／日志经具体委托适配，不新建通用存储／进程框架；实际 Windows 门面证据见账本 |
| 按键保存 | `InstanceKeybindingFiles.Save` 处理 Previous 检查、重复行更新、BOM／换行与原子写入，调用原 running／路径／缓存适配和同一 Gate | 复用规则解码／校验及 Hash／AtomicWrite；同值仍备份／写入／清 cache，不能套用个人习惯的 no-op。写原 Replacement，不归一键名；模型、读取归属与页面交互仍在 C005 后续批次 |
| 按键读取与归属 | `InstanceKeybindingReader` 负责原 options 读取、31 默认项、JAR 语言／metadata 归属及 SharedCount；原 VB 类型继承七字段 DTO，工厂与 Cast 保留项目对象 | 原路径、日志、固定 Meloong 标题换行经具体委托；保留 BOM 自动检测、原取消点／异常集合、阈值、顺序及当前文化排序，不跳过空 Targets 的扫描。坏 ZIP 仍外抛，模型读取完成不等于页面后台已转换 |
| 按键页面规则 | `InstanceKeybindingView.DisplayKey/BuildView<T>` 返回显示文本、行计数和平铺／分组块，泛型成员保原对象引用 | 原 VB 页面真实调用，继续管理控件／文案／数值文化／布局／捕获。保留 Ordinal 搜索／分块差异、VB null／空串等值、可见成员与全局总数、ALL 平铺；完整 WPF 后台仍待 C005c2／C029a，纯计划不是新 UI 框架 |

现阶段这些边界通过显式参数、结果对象和必要的委托／接口实现。程序集拆分以实际依赖闭包和构建验证为依据；当前 `PCLCS` 的 WPF 项目引用仍在，单个解析器无 WPF 不代表整个库已解耦。后续任务可以细化字段和接口，但须同步更新本表、生产调用与对应回归。

## 按实际功能链划分的小批次

### M01 基础框架、动画、控件与页面外壳

- **LF 源／来源：** B 的 `ModAnimation.vb`、`ModValidate.vb`（P）；`ModBase.vb`、`MyBitmap.vb`、`PclLogger.vb`，以及 `FormMain.xaml(.vb)`、`Modules/ModMain.vb`（P+LF）；U 的原生控件及 P 的页面基类／导航。LF 新控件另随 M06、M09、M10 转换；`Modules/ThirdParty/DragHelper.vb` 保留第三方来源。
- **CE 候选：** [Base][ce-base] 的 `ModBase.cs`、`ModAnimation.cs`、`MyBitmap.cs` 和 [Controls][ce-controls] 可对照 C# WPF 转换；[PCL.Core/UI/Animation][ce-animation] 有独立时钟、插值、缓动结构。后者的调度模型不能直接替换 LF 动画并默认手感等价。
- **前置／保留：** 核对依赖属性、`Handles`／`WithEvents`、默认属性、委托、Dispatcher、名称作用域、资源及图片格式。保留命名动画组的中断／叠加、页面重复进入、鼠标悬停／按下／禁用、滚轮和弹窗焦点行为；固定默认主题与背景选择沿用 LF。
- **批次／禁止倒依赖：** 先一个控件及直接消费者，再一个页面的 XAML／后台／资源绑定；基础全局状态和导航环最后成组适配。动画规则不要依赖具体页面，图片加载适配不要进入通用资源 DTO。
- **按键页已核实的结构前置：** 当前 VB main 同时拥有 XAML partial、MyPageRight／自定义控件／LocalResourceUi 和具体页面路由，且已引用 PCLCS，不能让 PCLCS 反向引用 main。先提取页面纯显示／分组规则（C005c1）；C027 基础层就绪后由独立 C029a 建立单向的 C# WPF 呈现库和惰性 host，再迁整页（C005c2）及实际路由。C029 只汇总最终页面，不成为宿主前置的依赖；主工程 C031／N003 在其后集成，避免循环或未编译的 C# 页面占位。
- **已实施边界（C026a／C027a）：** `presentation/Animation`与`modern/Presentation`编译同一完整LF动画内核；原AniData／AniGroups字段经约束struct接口直通，现代NativeRuntime使用真实C#状态与WPF宿主。原LF与固定PCL的ModAnimation逐行相同；固定CE稳定ModAnimation.cs仅缓动／C#迁移对照，未复用CE并发字典或其它调度语义。基础动画只需原UI／时钟／Net／日志primitive，不等待整个Loader C025；现代主窗口／主题及其余位图仍后续。前三控件保原视觉／原名typed壳，跨程序集BAML通过公开reader／writer装入同实例，98f81fe原消费者与cold构造已Windows通过，末端新族误判修复及真实现代五组仍待验。
- **回归：** `Test-BackgroundModes.ps1`、`Test-IconCompatibility.ps1`；相关页面脚本的预览检查，再在 Windows 人工对照滚动、切页、连续点击和动画取消。现有脚本没有覆盖所有动画时序，不能以截图代替交互验收。

### M02 加载器、Worker、取消、进度与文件占用

- **LF 源／来源：** [B/ModLoader.vb][lf-loader]（P+LF）、[B/LoaderFileOperation.vb][lf-file-operation]（LF），以及 G/`ModMinecraft.vb` 内 `McInstanceLock`、MeloongCore 的 `IWorker`／`RedoableWorker` 接口。运行时 UI 进度仍耦合主窗口。
- **CE 候选：** [Base/ModLoader.cs][ce-loader] 是另一套同源加载器参考，取消枚举／门面及网络加载器已经不同；不可仅按名称替换 LF Worker 完成语义。
- **前置／保留：** 把“请求取消”“显示状态终结”“负载确实结束”区分开；保留组合中的不同泛型子任务、隐藏子任务、启动前取消、实际进度、失败传播、重试及迟到任务封闭。`Thread.Interrupt` 的用法需在现代运行时逐项审核，不能用强行终止替代协作取消。
- **批次／禁止倒依赖：** 先转状态／取消桥和真实负载守卫，连同所有调用契约；再改任务栏 UI。文件锁只随真实 Worker 生命周期，底层任务不得经页面回调提前释放锁。
- **回归：** `Test-ReviewFixes.ps1 -Check Lock` 覆盖非可中断负载、嵌套任务、finally、迟到任务和开始前取消；再跑 `-Check Workflow` 及 `Test-VersionListCancellation.ps1`。迁移守卫后必须复跑真实调用链，不能只测一个取消令牌。

### M03 实例、版本、Java、库与组件模型

- **LF 源／来源：** G/`ModJava.vb`（P），[G/ModMinecraft.vb][lf-minecraft]（P+LF，含实例、目录、列表、库、assets、图标等多个职责）；[PCLCS][lf-cs] 的 `Java.cs`、`Minecraft/McVersion.cs`、`MCUtils.cs`、`McInstallManifest.cs`、`Configs.cs`、`Constants.cs`（P）；G/`InstalledComponentSummary.vb`、`MinecraftFolderPolicy.vb`、`MinecraftLogoCompatibility.vb`（LF）。入口包括 P/`PageSelect*` 和 `PageInstanceOverall.Components.vb`。
- **CE 候选：** [Minecraft][ce-game] 的 `McInstance.cs`、`ModFolder.cs`、`ModInstanceList.cs`、`ModLibrary.cs`、`ModAssets.cs`、`ModJava.cs`；[Core/Java][ce-java] 的扫描器、安装信息、用户选择规则及 `JavaService`；`PCL.Core/Minecraft/McVersionClassifier.cs`。稳定版已拆分部分职责，须对照实际 API 和 LF 的配置表示。
- **前置／保留：** 先固定版本与加载器模型、`PathVersion`／`PathIndie`、目录规范化和配置读取接口，再迁移发现／扫描。保留普通与隔离实例、Java 缓存／强制选择／取消、旧版本推断、库 rules／natives／路径／顺序、assets 与组件摘要；图标存储 URI 兼容旧 PCL 标识。
- **批次／禁止倒依赖：** 先版本和组件纯模型，再目录扫描及 Java 适配，最后实例列表和组件 UI；不得让 Java 扫描器反向引用选中页面或在扫描取消后继续更新旧列表。
- **回归：** `Test-VersionListCancellation.ps1`、`Test-InstalledComponentLoading.ps1`、`Test-IconCompatibility.ps1`；还需固定 Java 8／17／21 等现有支持样本，比较选择结果及真实启动（见 M04）。CE `JavaTest.cs` 是候选测试思路，不能替代 LF 配置兼容验证。

### M04 账号、启动、进程监视和崩溃分析

- **LF 源／来源：** [G/ModLaunch.vb][lf-launch]、G/`ModWatcher.vb`、`ModCrash.vb`（P+LF）；P/`PageLaunch` 下登录／皮肤／启动 UI，`Settings.Accounts.vb` 及账号路由（见 M12）；PCLCS/`Launch/LaunchUtils.cs`、随附 Java Wrapper／LWJGL agent（P）。
- **CE 候选：** [Minecraft][ce-game] 的 `ModLaunch.cs`、`ModWatcher.cs`、`ModSkin.cs` 和 [CrashAnalysis][ce-crash]；[Core/Profile][ce-profile] 的认证 Provider 与档案管理、[Core/Launch][ce-launch]。账号采用新的 profile 模型，不能覆盖 LF 的共享 PCL 账号存储。
- **前置／保留：** M02、M03、M12 的契约先明确；迁移时同步接入 M07 的 Prepare／Track／Finish、M08 的恢复和备份、M09 的启动检查、M11 的崩溃环境差异。保留离线／Microsoft／Auth／Nide 现有流程、令牌刷新及取消、参数替换和转义、自定义命令、工作目录、进程归属与退出状态；日志继续执行既有账号脱敏。
- **批次／禁止倒依赖：** 先参数和诊断结果模型，再认证适配及 Java 进程适配，最后成组转换启动 Loader、Watcher 与 UI 回调。规则层不读取全局 `McInstanceSelected`；Watcher 通知应用流程，不能由进程适配直接创建页面。
- **回归：** `Test-AuditFixes.ps1`、`Test-LaunchDependencies.ps1`、`Test-WorldBackups.ps1`、`Test-LocalGameFeatures.ps1` 和 M11 脚本；Windows 使用隔离测试账号／目录做普通实例、加载器实例、启动取消、正常退出、崩溃、启动器先退出后恢复的端到端对照。现有反射脚本不等于完整真实登录／Minecraft 启动验收。

### M05 网络、下载、在线资源与安装

- **LF 源／来源：** B/`ModNet.vb`、G/`ModDownload.vb`、R/`ResourceProject.vb`／`ResourceSearcher.vb`（P+LF），R 的 `ResourceVersion.vb`、`LocalResourceFile.vb`／`LocalResourceLoaders.vb` 和 PCLCS/`Resource`（P）；B/`SafeRedirectHandler.vb`、`LoginCertificatePolicy.vb`、`BuildCredentials.vb`（LF）；P/`PageDownload` 及 `ModDownloadLib.vb`／`PageDownloadModify.vb`。
- **CE 候选：** [Modules/Network][ce-network] 的下载模型、Requester、管理器与 Loader；[Core/IO/Net][ce-core-net]；[Minecraft/ModComp.cs][ce-comp] 及 [Core/ResourceProject][ce-resource] 的平台模型、G/`ModDownload.cs`。它们对应 LF 多个职责，不能只查找 `ResourceSearcher` 同名文件。
- **前置／保留：** 先 HTTP 请求／响应、凭据供应、下载计划、进度／取消和缓存边界；保留多源回退、断点／分段、校验、限速、中文搜索、分页合并、平台筛选及安装输出。跨源跳转时的认证头策略、登录证书规则和自有 Client ID／API Key 的构建注入需随流程迁移。
- **批次／禁止倒依赖：** 先平台 DTO 与请求构造，再网络／下载适配，最后安装 Loader 和资源页；下载引擎不弹框、不修改实例选择，网络 DTO 不引用控件。纯规则及合成 HTTP 测试通过后，再对公开服务做受控烟测。
- **回归：** `Test-ReviewFixes.ps1 -Check Redirects`、`Test-BuildCredentials.ps1`、`Test-ResourceEntryPoints.ps1`、`Test-MapDetails.ps1`；M06／M10 的安装和进度脚本。发布凭据只验证存在、隔离和行为，不输出实际值。

### M06 本地资源、NBT、地图、截图、回收站及资源更新

- **LF 源／来源：** G/`InstanceResourceStore.vb`、`WorldMetadata.vb`、`ResourceRecycleStore.vb`、`MapDownloadInstaller.vb`、`InstanceResourceUpdates.vb`（LF）；U/`LocalResourceUi.vb`、P/`PageInstanceResources*`、`MyScreenshotItem.xaml(.vb)`，P/`PageDownload/Resource/PageDownloadMap.xaml(.vb)`（LF）和资源详情页接入修改。
- **CE 候选：** [Core/Saves][ce-saves] 的解析器／`SaveManager`、[NbtFileHandler.cs][ce-nbt]、[SaveImportHelper.cs][ce-save-import]；[PageInstance][ce-instance-pages] 的 `PageInstanceSaves*`、`PageInstanceScreenshot*`、`PageInstanceCompResource*`。CE 有相应能力，但其 fNbt、异常、读写分享方式及数据模型与 LF 不同。
- **前置／保留：** 先路径、包校验、资源元数据和 NBT 读规则，再磁盘和图片适配；M02 提供异步取消。保留读取不创建目录、同名不覆盖、路径／链接限制、ZIP 根目录识别、原压缩包保留、回收恢复与清空、资源更新先暂存后校验替换、缺失／损坏 `level.dat` 降级和存档详情。NBT 提取应保持 LF 当前只读行为，不能顺带启用 CE 编辑功能。
- **批次／禁止倒依赖：** `WorldMetadataReader`／`WorldMetadataFiles`、`InstanceResourceEntryData`、`InstanceResourcePaths`（含现代路径／Ordinal 窄适配）、`InstanceResourceKindRules`／`InstanceResourceNameRules`／`InstanceResourceFileRules`／`InstanceResourcePackageRules` 已接 LF 原入口并验收，具体证据见 MIGRATION_STATUS／[路径与ZIP对照](RESOURCE_PATH_ORACLE.md)。大小／pack描述纯规则已验；当前Catalog扫描／pack读取／图片文件、Transactions导入导出与回收恢复、Windows图片解码与截图实际交互controller已成组进入C#并接原宿主，Windows批次验收仍在进行。主页面、回收站管理／更新等后继仍按链迁移，不据后端或单控件关闭整页。通用路径／事务能力不可依赖资源页面或 WPF 图片类型；[migration tests][lf-migration-tests] 只证明各自已链接的真实核心，不代替完整 GUI。
- **回归：** `Test-WorldDetails.ps1`、`Test-GameFeatureRegressions.ps1`、`Test-InstanceResources.ps1`、`Test-ScreenshotGallery.ps1`、`Test-MapInstallAndRecycle.ps1`、`Test-ResourceUpdates.ps1`；流解析器同时运行自身合成 NBT 用例。地图下载仍落到当前版本 `saves/PCLLFWorldDownload` 后安装。

### M07 本机个人配置、按键和 Quick Play

- **LF 源／来源：** [G/LocalGameData.vb][lf-local-data]、`PersonalGameSettings.vb`、`InstanceKeybindings.vb`、`QuickPlayWorlds.vb`（LF）；P/`PageInstanceKeybindings.xaml(.vb)`（LF），`PageSetupLaunch`、`PageLaunchRight`、启动／退出接入（P+LF）。
- **CE 候选：** [Core/Configuration][ce-config] 提供配置／存储组织参考，[Core/Saves][ce-saves] 和 G/`ModLaunch.cs` 可对照存档／启动接口。LF 的个人选项分类同步、冲突分组和本机会话不能由 CE 通用配置类直接取代；这里没有证明完整一对一替换候选。
- **前置／保留：** 依赖路径／原子写、实例实际游戏目录、版本判断和进程标识。资料根为 `%LOCALAPPDATA%/PCL Living Fork/GameData`，与包作者文件分离；保留换行、仅同步个人字段、`.pcl-backup`、预期旧哈希校验、版本例外、旧／新按键与 FOV 转换、首次默认按键、模组归属／同键冲突、运行中拒绝重写及 PID＋启动时间会话恢复。
- **批次／禁止倒依赖：** 先选项合并／按键编码，再配置文件／会话，再 Quick Play 参数与日志解析，最后两处世界入口及按键页。保留 `id`／`identifier` 日志形状、支持版本门限、空格世界名、互斥的服务器参数；最近世界索引不得创建或修改存档内容。规则层不查运行窗口、不调用启动按钮。
- **回归：** `Test-LocalGameFeatures.ps1`、`Test-GameFeatureRegressions.ps1`、`Test-KeybindingGroups.ps1`、`Test-RecentWorldCard.ps1`；与 M04 一起检查 Prepare／Track／Finish 和启动器重开后的会话恢复。

### M08 存档备份、环境还原点与文件事务

- **LF 源／来源：** G/`WorldBackups.vb`、`EnvironmentHistory.vb`（均含引擎与 Ui 类，LF），`ModpackUpdate.vb` 中 `ApplyTransaction`／`Recover`（LF）；P/`WorldBackupGroup*`、`WorldBackupSettings*`、`PageInstanceResources.Backups.vb`、`PageInstanceRestorePoints*`（LF）；启动／Watcher／更新前接入见 M04、M10、M11。
- **CE 候选：** [Core/Saves][ce-saves] 可对照存档读取；[Core/IO/Files.cs][ce-files] 可对照部分文件工具。不能以 CE 存档管理已存在，推定它等价覆盖 LF 的备份保留、环境还原点和事务日志；本图未确认完整替换实现。
- **前置／保留：** M02 的锁和真实负载守卫、M06 的元数据、M07 的本机资料／进程识别；先抽路径、摘要、文件清单、事务／恢复规则。正常退出自动备份的判定仍是实际运行到游戏状态且成功退出；更新 Minecraft 前额外备份；手动点不计自动保留上限。环境还原只恢复其已声明覆盖的环境文件，保留当前存档进度；恢复前留档。
- **批次／禁止倒依赖：** 先事务／故障恢复，再存档备份／恢复，再环境点／保留策略，最后 Ui 类及分组控件。保留 `coverageVersion`：旧点不删除从未覆盖目录，新点覆盖空目录，未知版本拒绝猜测；取消／失败要验证暂存、日志、已应用文件回滚及再次启动恢复。文件计划不得依赖整合包页面或直接设置 UI Busy 标志。
- **回归：** `Test-ReviewFixes.ps1 -Check Transaction` 与 `-Check Recovery`、`Test-WorldBackups.ps1`、`Test-WorldBackupGroups.ps1`、`Test-SaveBackupIntegration.ps1`、`Test-EnvironmentHistory.ps1`、`Test-EnvironmentCoverage.ps1`、`Test-BackupRetention.ps1`。合成临时目录中注入中途取消／失败，检查原文件及锁的最终状态。

### M09 本地／平台模组依赖、关系图与启动前补全

- **LF 源／来源：** G/`ModDependencyInspector.vb`、`ModDependencyOnline.vb`、`ModDependencyAutoFill.vb`、`LaunchDependencyCheck.vb`（后者同时声明 `ModDependencyActivation`，均 LF）；U/`ModDependencyGraphView.vb`、`ModDependencyCheckView.vb`、`ModDependencyImpactView.vb`、`ModDependencyIcon.vb`（LF）；P/`PageInstanceMod*`、`PageLaunchRight` 和 `ModLaunch` 接入。
- **CE 候选：** [Minecraft][ce-game] 的 `ModCompDependency.cs`、`ModJarInJar*.cs`、`McConstraintMatcher.cs`、`MavenComparableVersion.cs`、`ModLocalComp.cs`；[Core/ResourceProject/ModDependencyResolver.cs][ce-dependency] 和其 [测试][ce-dependency-test]。CE resolver 的平台项目请求／安装列表是参考，不能单独替换 LF 的本地声明、禁用文件、内嵌组件及交互确认全链。
- **前置／保留：** 先元数据／版本约束／提供者模型，再本地扫描、平台查询与缓存、补全文件计划；网络／下载适配来自 M05，目录／事务来自 M02、M06。保留不精确平台结果或超时回退、本地可靠声明、依赖／冲突／被依赖、内嵌组件按所属 JAR 归组、禁用且兼容前置可启用、精确环境指纹及本次进程内已接受问题缓存；首次默认开启并保留后续开关选择。
- **批次／禁止倒依赖：** 本地 Inspector → Online 适配 → Activation／AutoFill → 启动复查 → 图及影响确认；`LaunchDependencyCheck` 与 `AutoFill` 当前互相使用类型，应先提出共享计划／指纹模型。用户确认和两列红框留在 UI，规则不得主动请求平台或修改文件；补全确认后提交并复查才可启动。
- **已实施边界（C013）：** 本地Inspector完整解析／版本／图／可靠度／移除影响迁四份`PCLCS/Minecraft/ModDependencyInspection.*`／`ModDependencyVersions.cs`，原VB仅10个闭包DTO、旧enum映射、Context适配和入口。固定PCL无此LF模块；固定CE的JarInJar／LocalComp仅同源C#参照，其5层512节点及较大字节预算／缓存／选择策略不导入，LF保4层256个64MiB、strict UTF8／1MiB与三态。原DTO列表／引用身份／online缓存字段保持，C014网络未迁。两Core与Strict原适配／Online编译及六组真实文件测试通过，Windows原消费者待本批。
- **回归：** `Test-ModDependencies.ps1`、`Test-ModDependencyOnline.ps1`、`Test-ModDependencyGraph.ps1`、`Test-DependencyActivation.ps1`、`Test-LaunchDependencies.ps1`、`Test-DependencyToggles.ps1`；离线固定平台响应、版本不匹配、禁用文件及内嵌组件样本先通过，再做受控在线验证。

### M10 整合包导入、更新、导出、隔离与模式

- **LF 源／来源：** [G/ModModpack.vb][lf-modpack]（P+LF）；G/`ModpackUpdate.vb`、`ModpackUpdateFlow.vb`、`ExportedVersionIsolation.vb`、`MinecraftFolderPolicy.vb`，`Modules/ModModpackMode.vb`（LF）；U/`ModpackUpdateViews.vb`、P/`PageSetupModpack*`（LF），`PageInstanceExport*`、`PageInstanceOverall` 及导航接入（P+LF）。
- **CE 候选：** [Minecraft/ModModpack.cs][ce-modpack] 和 `ModDownload.cs` 可对照同源导入与运行时写法；导出页面见 [PageInstance][ce-instance-pages]。CE 的 MMC 补丁／Java 交集算法仅作为 deferred 兼容增强研究材料；当前批次保留 LF 的 `InstallPackMMC` 行为，不扩大格式能力。
- **前置／保留：** M03 实例／组件、M05 下载、M08 事务与更新前留档。更新读取作者基线并区分新增／更新／移除／保留个人修改，支持本地包与 CF／MR 来源、正式／测试版选择、真实下载进度、运行中拒绝更新和中断恢复；Minecraft／加载器变更同步安装输出，取消后等待真实文件处理和回滚。导出保留附带启动器选择、版本隔离选择、覆盖文件作用域及必要入口模式。
- **批次／禁止倒依赖：** 先包／manifest／差异计划（把通用事务能力移到下层），再 Commit／恢复和 Flow、预览／版本选择，最后导出与模式页面；不得让通用文件事务依赖 `ModpackUpdateFlow` 或 `ModModpackMode`。导入格式识别和现有 MMC 样本另作等价回归，不与新算法合并提交。
- **回归：** `Test-ModpackUpdate.ps1`、`Test-ModpackUpdateUx.ps1`、`Test-ReviewFixes.ps1 -Check All`、`Test-ExportVersionIsolation.ps1`、`Test-ModpackMode.ps1`、`Test-ModpackFolderScope.ps1`；旧导入格式和现有 MMC 在隔离目录比较生成 JSON、文件布局和启动结果。现有脚本没有证明更广的 MMC／Prism 兼容矩阵。

### M11 测试副本、来源分组与崩溃环境对照

- **LF 源／来源：** G/`InstanceCloner.vb`（含 Ui）、`InstanceCloneGroups.vb`、`RecentEnvironmentChanges.vb`，P/`PageInstanceClone*`（LF）；版本列表、组件修改／模组更新、崩溃分析与还原点入口接入（P+LF）。
- **CE 候选：** [Minecraft][ce-game] 的实例／目录模型及 [CrashAnalysis][ce-crash] 的证据与呈现分层可参考；不能由 CE 存档编辑或通用崩溃规则推定已覆盖 LF 的副本迁回／来源分组／环境基线。
- **前置／保留：** 依赖 M03 的实际游戏目录、M08 的摘要／事务和还原点；保留选择性克隆、来源记录、共享目录保护、文件链接限制、迁回差异预览／并发哈希复查、变更前备份、取消后清理；环境对照使用上次正常运行标记和该点的覆盖范围，报告连接到正确还原点。
- **批次／禁止倒依赖：** 副本模型／扫描 → Clone／PreviewMigration／ApplyMigration → 来源分组 → 环境差异结果 → UI／崩溃接入；差异规则不得调用崩溃弹窗或依赖列表控件，克隆事务不可复用上层页面 Busy 状态作为锁。
- **回归：** `Test-InstanceClone.ps1`、`Test-InstanceCloneGroups.ps1`、`Test-RecentEnvironmentChanges.ps1`、`Test-EnvironmentCoverage.ps1`、`Test-ReviewFixes.ps1 -Check Lock`；迁回前源文件被修改、共享／隔离目录和旧环境点均需保留样本。

### M12 设置、共享账号存储与用户数据兼容

- **LF 源／来源：** [P/PageSetup/Settings.vb][lf-settings]（P+LF）、`Settings.Accounts.vb`／`Settings.Compatibility.vb`（LF）、`SettingService.vb` 和 `PageSetup*`；PCLCS/`Configs.cs` 的 Java 配置及 MeloongCore 配置提供者。
- **CE 候选：** [Core/Configuration][ce-config] 的 `ConfigService`／迁移／存储提供者、`CatIniFileProvider.cs`，以及 [Core/Profile][ce-profile] 的认证档案；有架构参考价值，持久化格式和 LF 账号路由必须单独适配。
- **前置／保留：** 列出键名、默认值、类型、Source、加密和 OnChanged；账号及兼容偏好共享 `HKCU/Software/PCL`，分支状态隔离。保持 PCL 设备 Identify／旧密钥转换、账号组校验与事务、显式退出不被旧缓存复活、共享账号每次读取的新鲜度、兼容偏好仅补缺及失败回滚；同目录仍共用 `PCL/Setup.ini`，实例设置继续保留原作用域。
- **批次／禁止倒依赖：** 先可测试的迁移计划／编码／键定义，再注册表／INI／配置桥，最后设置页及订阅；不得用统一 JSON／CE profile 存储直接覆盖旧注册表和 INI。设置存储不得依赖页面类型，OnChanged 经应用层触发，防止初始化时倒向主窗口。
- **回归：** `Test-AuditFixes.ps1`、`Test-Release.ps1`、`Test-DependencyToggles.ps1`、`Test-BackgroundModes.ps1` 及 M03／M07；使用虚构账号、独立测试注册表和临时 INI，验证旧格式、损坏组、已清空值、失败回滚及双方读写兼容。

### M13 启动器单例、窗口唤醒、自更新、更新桥和发布

- **LF 源／来源：** `Application.xaml(.vb)`、`Modules/ModMain.vb`／`ModSecret.vb`（P+LF）；`Modules/LauncherSessionLock.vb`、`LauncherWindowActivation.vb`、[ModLauncherUpdate.vb][lf-update]（LF）；[scripts/update-bridge/Program.cs][lf-bridge]、`Build-UpdateBridge.ps1`／`Build-Release.ps1`、凭据脚本及 [.github/workflows][lf-workflows]。
- **CE 候选：** [Modules/Updates][ce-updates]、CE `Application.xaml.cs` 及 [Core/App][ce-app] 的生命周期结构。CE 更新源／渠道／身份不同，不能照搬覆盖 LF 发布端点、旧仓库入口或内部兼容标识。
- **前置／保留：** 共享账号前先获取 `PCL_SingletonMutex` 与 `LivingPCL_SingletonMutex`；保留等待／失败清理和已运行窗口唤醒。更新保留用户 EXE 文件名、SHA256／LF 身份校验、暂存于 `PCL/Updates`、等待正确父进程退出、替换／启动失败恢复；保留旧 `LivingPCL2-Re` 更新入口与独立第三方署名。
- **现代 .NET 必做适配：** 现在 `IsLivingExecutable`／桥构建使用 `AssemblyName.GetAssemblyName(exe)`，自更新和桥通过 `Assembly.Location` 找自身；它们假设托管 EXE。现代 apphost／单文件发布不能沿用该假设。须先确定发布产物、身份来源、载荷布局及桥协议，再同时适配验证器、构建脚本、旧桥→新版本、更新后重启及失败回滚。多文件发布还须验证所有运行时依赖的交付，不能只替换主 EXE 后宣布发布完成。
- **批次／禁止倒依赖：** 先单例／唤醒和更新计划模型；运行时阶段单独一批处理发布格式／桥／校验／资源和依赖加载。更新器不读取账号、不初始化普通页面；密钥只经既有构建供应入口。发布之前完成可审查构建和回归，状态据实记录。
- **回归：** `Test-LauncherActivation.ps1`、`Test-AuditFixes.ps1`、`Test-UpdateBridge.ps1`、`Test-LegacyUpdater.ps1`、`Test-Release.ps1`、`Test-BuildCredentials.ps1`、`Test-CnbTrigger.py`；真正发布产物另做干净 Windows 启动、旧版本升级、任意 EXE 名、替换失败、缺依赖及重启验证。

### M14 既有工具、音乐、自定义事件、帮助和联机等剩余入口

- **LF 源／来源：** B/`NativeMemoryOptimizer.vb`、`GameGarbageCleaner.vb`、`ToolboxDownloadSession.vb`，P/`PageOther/PageOtherTest.Tools.vb`（LF）；`Modules/ModMusic.vb`／`ModDevelop.vb`（P），`ModEvent.vb`、`ModSecret.vb`、P/`PageOther`／`PageLink`／`PageSetup/PageSetupLink` 等既有入口（P 或 P+LF，逐文件见清单）。
- **CE 候选：** CE `Modules/ModMusic.cs`、[Modules/Event][ce-event]、`Modules/ModLink.cs`、`Modules/ModWebServer.cs`、[Core/Link][ce-link] 和 [PageTools][ce-tools-pages] 可按功能查找；工具下载仍先核对 LF 的暂存／备份／目标占用语义。
- **前置／保留：** 清单中已编译、条件编译和可选入口都要明确迁移结果，不因不是主要启动链而遗漏。保留现有 NAudio、图片／WebP、对话框、P/Invoke 与 Java 辅助组件的行为和资源，检查其 Windows／位数／现代 .NET 依赖；此处是迁移已有能力，不扩展内存优化或引入新的原生实现。
- **批次／禁止倒依赖：** 下载／清理／内存调用适配与页面分别迁移；音乐、帮助、自定义事件、联机按入口形成独立小批次。Windows／原生调用封装在边界，不让通用模型依赖工具页面；自定义事件和 XAML 事件映射要核对委托及资源解析。
- **回归：** `Test-Toolbox.ps1`、`Test-ToolboxMemory.ps1`、`Test-ToolboxDownload.ps1`；音乐切歌／暂停、帮助加载、现有自定义主页事件和联机流程做 Windows 人工样本。没有专属自动脚本的入口明确标记人工待验，不能默认为通过。

## 固定清单覆盖与账本任务对应

下表分组与固定清单的 `by_group` 一致，合计 **249 个文件**，使默认关闭、生成和维护工具源码也有后续归属。M 编号仅为本图的功能链索引；执行继续使用 MIGRATION_STATUS 已有的 C／N／R 任务，不另建一份状态表。目录行下的具体迁移仍按上文功能链拆小批次；一个源文件含多个职责时同步保留其消费者。

| 固定清单分组（主程序内路径除外注明） | 文件数 | 功能链 | 现有账本承接任务 |
| --- | ---: | --- | --- |
| `Application`、`My Project`、`Resources` | 5＋4＋1 | M01、M13 | C031、N003、R001；含 `GlobalSuppressions.vb`、AssemblyInfo、MyWpfExtension、资源／设置生成类及 `Resources/Custom.xaml` |
| `Modules` | 9 | M01、M10、M13、M14 | C020、C031、N005 |
| `Modules/Base` | 14 | M01、M02、M05、M14 | C024–C027、C030、N001 |
| `Modules/Minecraft` | 31 | M03、M04、M06–M11 | C001–C021；具体前置按账本和对应功能链 |
| `Modules/Resource`、`PCLCS/Resource` | 5＋2 | M05 | C018、N002 |
| `Controls`、`Modules/ThirdParty` | 55＋1 | M01、M06、M09、M10 | C016、C027、C029；LF 新控件随对应流程，第三方来源单独保留 |
| `PCLCS`、`PCLCS/Launch`、`PCLCS/Minecraft` | 3＋1＋3 | M03、M04、M12 | C017、C021、C022、N002 |
| `Pages` 根目录 | 8 | M01、M03 | C017、C031；含版本选择和任务进度页 |
| `Pages/PageDownload` | 24 | M05、M06、M10 | C018、C019、C028 |
| `Pages/PageInstance` | 31 | M03、M06–M11 | C005、C008、C010–C012、C016、C020、C029 |
| `Pages/PageLaunch` | 22 | M04、M07、M09 | C006、C015、C023 |
| `Pages/PageSetup` | 16 | M01、M07、M10、M12、M14 | C022、C030 |
| `Pages/PageOther`、`Pages/PageLink` | 11＋2 | M14 | C030、C031；含可选联机和开发／工具入口 |
| `scripts/update-bridge` | 1 | M13 | N005；旧桥目标运行时按旧入口兼容需求决定，不要求随产品机械切换 |

清单还记录 LF 相对上游移除的两个 `MySliderDot` 源文件和唯一未列入产品项目的维护桥；前者须核实无残留项目／XAML 引用，后者由发布构建任务承接，不将“未被主项目引用”解释为可以遗漏。MeloongCore 子模块不计入上述 249 个文件，独立走 N001 的固定版本兼容审计。

## CE Maven／MMC 参考的使用限制

静态核对发现 [PCL.Core/Minecraft/MavenArtifact.cs][ce-maven] 的三段普通坐标路径缺少 version 目录，classifier／五段分支还出现 `packageId[1]`、`packageId[4]` 等对原始字符串取字符的写法。[MavenTest.cs][ce-maven-test] 主要打印输出，未断言预期路径。这一候选应先补坐标→预期路径的精确测试及真实调用验证，**不能列为可直接复用首选**。

在固定 CE 树对全部 C# 搜索 `MavenArtifact`，仅见类定义和该测试；真实库解析可见 `ModLibrary.McLibGet`，其普通路径包含 version 目录，并由库列表／下载处理调用。因此上述发现不能推断 CE 的 MMC 整体不能启动，也不能跳过真实库链直接替换 LF `McLibGet`。CE `ModModpack.InstallPackMMC` 的补丁、Java 集合、库及导入处理只用于理解现有链路和未来 deferred 增强；当前 goal 不据此增加 MMC 格式能力。

## 自动续跑时的选择与完成门槛

1. 先读 MIGRATION_STATUS、固定清单及当前工作树，选择尚未完成的一个模块内小批次；按上述前置关系推进，不机械地从 M01 整文件开始。推荐先选无 UI 的解析／模型／规则，再路径与事务，随后各应用流程与页面；启动生命周期、配置全局状态、基础加载器的耦合部分成组处理。
2. 每批记录旧入口、新入口、调用者、数据／异常／取消契约、CE 引入范围及对应脚本。先保留门面并接通真实生产调用；仅添加无人调用的 C# 类型不算迁移。
3. net48 阶段保留可用的 Windows 构建；现代 .NET 阶段将主程序、PCLCS、Wpf 依赖、资源加载、外部 DLL 和发布一起验证。CE 稳定版 DLL 不可跨目标框架直接接入。
4. 当前 PowerShell 回归大多在 **Windows PowerShell Desktop／STA** 中加载 net48 EXE、PCLCS 及 WPF，并反射 `PCL.*` 类型。新 .NET 10 产品不能直接由这个 net48 宿主加载；相关批次须将测试驱动适配为 .NET 10 Windows／STA 宿主或经验证的兼容方式，保留原合成夹具和断言。类型／成员重命名时同步更新测试入口，不能删除断言换取通过。`Test-ModpackFolderScope.ps1` 另有源码编译夹具，须同步适配。
5. 每批运行相关回归组和构建，文件写入批次额外做中途取消／失败／恢复及锁寿命检查；页面批次保留新旧交互对照。跨平台 migration tests 只证明其包含的纯逻辑；Linux 静态检查、CE 自有测试、Windows 构建成功均不单独证明完整 LF 运行等价。
6. 本图来自本地固定 Git 树、工程引用、源码调用点和测试脚本的阅读核对；没有把本次文档检查写成 Windows 测试执行结果。新增或改变行为、扩大 MMC、性能优化和 Rust／C++ 工作保持 deferred，完成记录归入当前 goal 后再选择下一目标。

[pcl]: https://github.com/Meloong-Git/PCL/tree/5ef81e490ae46b1100d61e33d0183094c7a80ca8
[lf-base]: <../../Plain Craft Launcher 2/Modules/Base>
[lf-game]: <../../Plain Craft Launcher 2/Modules/Minecraft>
[lf-resource]: <../../Plain Craft Launcher 2/Modules/Resource>
[lf-controls]: <../../Plain Craft Launcher 2/Controls>
[lf-pages]: <../../Plain Craft Launcher 2/Pages>
[lf-project]: <../../Plain Craft Launcher 2/PCL-Living-Fork.vbproj>
[lf-cs]: ../../PCLCS
[lf-cs-project]: ../../PCLCS/PCLCS.csproj
[lf-shared-project]: ../../MeloongCore/Shared/MeloongCore.csproj
[lf-wpf-project]: ../../MeloongCore/Wpf/MeloongCore.Wpf.csproj
[lf-loader]: <../../Plain Craft Launcher 2/Modules/Base/ModLoader.vb>
[lf-file-operation]: <../../Plain Craft Launcher 2/Modules/Base/LoaderFileOperation.vb>
[lf-minecraft]: <../../Plain Craft Launcher 2/Modules/Minecraft/ModMinecraft.vb>
[lf-launch]: <../../Plain Craft Launcher 2/Modules/Minecraft/ModLaunch.vb>
[lf-local-data]: <../../Plain Craft Launcher 2/Modules/Minecraft/LocalGameData.vb>
[lf-modpack]: <../../Plain Craft Launcher 2/Modules/Minecraft/ModModpack.vb>
[lf-settings]: <../../Plain Craft Launcher 2/Pages/PageSetup/Settings.vb>
[lf-update]: <../../Plain Craft Launcher 2/Modules/ModLauncherUpdate.vb>
[lf-bridge]: ../../scripts/update-bridge/Program.cs
[lf-workflows]: ../../.github/workflows
[lf-migration-tests]: ../../tests/PCLLF.Migration.Tests
[scripts]: ../../scripts
[ce-project]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Plain%20Craft%20Launcher%202.csproj
[ce-core-project]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/PCL.Core.csproj
[ce-base]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Base
[ce-controls]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Controls
[ce-animation]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/UI/Animation
[ce-loader]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Base/ModLoader.cs
[ce-game]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Minecraft
[ce-java]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/Java
[ce-crash]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Minecraft/CrashAnalysis
[ce-profile]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/Profile
[ce-launch]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/Launch
[ce-network]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Network
[ce-core-net]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/IO/Net
[ce-comp]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Minecraft/ModComp.cs
[ce-resource]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/ResourceProject
[ce-saves]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/Saves
[ce-nbt]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/NbtFileHandler.cs
[ce-save-import]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/SaveImportHelper.cs
[ce-instance-pages]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Pages/PageInstance
[ce-config]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/App/Configuration
[ce-files]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/IO/Files.cs
[ce-dependency]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/ResourceProject/ModDependencyResolver.cs
[ce-dependency-test]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core.Test/Minecraft/ResourceProject/ModDependencyResolverTest.cs
[ce-modpack]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Minecraft/ModModpack.cs
[ce-updates]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Updates
[ce-app]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/App
[ce-event]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Modules/Event
[ce-link]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Link
[ce-tools-pages]: https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5/Plain%20Craft%20Launcher%202/Pages/PageTools
[ce-maven]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core/Minecraft/MavenArtifact.cs
[ce-maven-test]: https://github.com/PCL-Community/PCL-CE/blob/1a7653127d578efdb2a9388999a3fdc67d9e49d5/PCL.Core.Test/MavenTest.cs
