# .NET 10／WPF 兼容审计（N001）

本报告是 2026-11-03 的源码与依赖审计，供 N002–N005 拆分实施和验收使用。LF 行为基线仍为 `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`；目标与任务状态以 [迁移目标](../MIGRATION_GOAL.md)、[账本](../MIGRATION_STATUS.md) 和 [模块映射](MODULE_MAP.md) 为准。本报告记录源码与依赖证据；现代运行时的实际实现和运行结果由后续任务分别验收。

CE 稳定参考固定为 [`v2.15.1`／`1a7653127d578efdb2a9388999a3fdc67d9e49d5`](https://github.com/PCL-Community/PCL-CE/tree/1a7653127d578efdb2a9388999a3fdc67d9e49d5)；较新的拆分参考固定为 [`dev`／`a8589e07486a7a6fd4af92c9bcfccc291730739b`](https://github.com/PCL-Community/PCL-CE/tree/a8589e07486a7a6fd4af92c9bcfccc291730739b)。两者的框架、包和模块位置分别核对，不能把 dev 的 `PCL.Core.Wpf`／`PCL.Launcher.Core` 拆分当作稳定版现状，也不替换 LF 底座。本报告未引入 CE 实现。

已有证据：[初始 Windows Build](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41656492901) 的 Debug、Release 均成功；账本另有 Linux 上现有 net48 辅助库编译与 .NET 10 纯 NBT 测试记录。本次仅实查 SDK `10.0.403`、Linux `Microsoft.NETCore.App 10.0.14` 可用，没有重新构建，也没有 Windows Desktop 运行结果。以下“阻塞”指对应现代框架／发布验收尚不能关闭，不阻止独立的 C# 等价转换继续推进。

2026-11-04 N002a实施更新：新增LF管理的[现代PCLCS工程](../../modern/PCLCS/PCLCS.Modern.csproj)，链接当前全部52份真实C#源码及原4资源、保AssemblyName与固定Shared完整依赖图；实查MeloongCore.Wpf只是旧项目默认using／引用，无真实Core调用，现代工程不需要该依赖。旧net48工程／子模块源码不改。SDK10.0.403在Linux真实构建现代库及[STA WPF组件验收工程](../../tests/PCLLF.Modern.Wpf.Tests/PCLLF.Modern.Wpf.Tests.csproj)为0警告／错误，实际Windows双配置资源精确名称／字节、解码冻结和同源live控制器执行待[新工作流](../../.github/workflows/modern-wpf.yml)。现代选中Newtonsoft13.0.3/net6.0、ThrottleDebounce2.0.2/netstandard2.0与WPF reference10.0.14。Java遍历父目录只补nullable局部，不变行为。以下初始审计表保原时间背景；N002／N003和全产品GUI／账号／发布仍未关闭。Linux生成的资源名称为平台正斜线，旧net48本地亦如此，Windows发布仍必须实际检查原反斜线LogicalName和Shared精确读取，不能用交叉编译冒充资源运行验收。

N002a后继实际Windows证据：`3f04b63346a23dfeee4a9b301cc0778d7aec07fa` [Modern41804541772](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41804541772) Debug126488198216／Release126488197906均11步成功。严格反斜线4资源名／原字节及Shared读取、实际图片解码／Freeze／文件句柄、同源缩略图与按键实际options事务全部运行通过；旧Frame按键／截图及更新桥定向复验通过。前两次合成键盘fixture缺PresentationSource／RoutedEvent失败保留，不修改生产逻辑绕过。该证据只覆盖当前52份Core与WPF组件，原LF主题／主窗／账号／真实游戏／现代发布仍由N003–N005／R002–R005完成。

## 当前工程与依赖边界

| 实际入口 | 当前状态 | 现代目标与待证实条件 |
| --- | --- | --- |
| [LF 主工程](../../Plain%20Craft%20Launcher%202/PCL-Living-Fork.vbproj#L1) | 非 SDK VB／WPF，`net48`，`RootNamespace=PCL`，程序集名 `LivingPCL`；显式 Compile／Page／Resource 项 | 转 C# SDK 工程，候选 `net10.0-windows`、`UseWPF=true`。按实际 WinForms 使用确定 `UseWindowsForms`；保留程序集／产品身份、图标、manifest 和条件功能。具体 Windows API 目标版本仍待选择。阻塞 N003。 |
| [PCLCS](../../PCLCS/PCLCS.csproj#L1) | SDK C#13／`net48`；引用 Shared 和 Wpf，默认 Using 也包含 `MeloongCore.Wpf` | 先核实整库的实际 WPF 依赖，选择纯 `net10.0` 或 Windows 目标／独立适配边界。单个 NBT 文件可跨平台链接，尚不能证明整库可移除 Wpf 引用。阻塞 N002。 |
| [MeloongCore Shared][mc-shared-project] | 固定 gitlink `c774ae4c85fd94e0eaa171db21e78a65870df19b`，SDK／`netstandard2.0`／C#13 | .NET 10 可消费 .NET Standard 2.0；Registry、shell32、bcrypt 等实现仍有 Windows 条件。可候选保留此 TFM，须在实际现代消费者中验证所选包资产与调用。 |
| [MeloongCore Wpf][mc-wpf-project] | 同一固定 gitlink，SDK／`net48`；手工引用 WPF／WinForms，无 `UseWPF` 声明 | 尚无已验证的现代 WPF 构建契约。后续单独选择固定兼容版本、LF 管理的兼容构建或实际依赖收缩；记录来源和许可。当前不修改子模块、不向其上游推送。阻塞 N002／N003 依赖闭包。 |

主工程没有待迁移的 `packages.config`；主要问题是手工 DLL 引用、资源中的 DLL 副本与子模块 NuGet 依赖并存。不能用“改成 PackageReference”代替逐个依赖验证。

同一 Reference 段还列有 System.Configuration／Management／Runtime.Caching／ServiceModel／Drawing／WinForms。现代 SDK 的框架引用、受支持包和旧 Framework 引用须分开核对；当前查到 Configuration 的实际入口是空设置生成类，ServiceModel／Management／Runtime.Caching 未定位到明确业务调用，仍须由完整编译／条件源检查确认后处置。Drawing 的位图／Graphics 与 WinForms／COM 对话框留在 Windows 边界；PCLCS 的 Microsoft.VisualBasic 引用也按实际转换辅助 API 核查，不据框架名批量删除或补包。

| 依赖及实际源位置 | 当前版本／资产证据 | 候选处理与最低验证；门禁 |
| --- | --- | --- |
| [主工程 Reference 段](../../Plain%20Craft%20Launcher%202/PCL-Living-Fork.vbproj#L135)：CacheCow.Client／Client.FileStore／Common | 汇编引用 `2.13.1.0`；仓库 DLL 可读 TFM 标记均为 `.NETFramework v4.6.2` | 先核实同版本有无现代兼容资产；必要时单批替换适配，保留缓存键、条件请求、文件占用和重定向语义。执行 [ModNet](../../Plain%20Craft%20Launcher%202/Modules/Base/ModNet.vb#L225) 的实际缓存命中／失效／并发／取消路径。阻塞网络链和 N002。 |
| 同段：System.Net.Http.Formatting | 汇编引用 `5.2.7.0`；DLL 标记 `.NETFramework v4.5` | 查清 CacheCow 的实际依赖闭包及异常资源探测；不能仅删除看似没有直接调用的引用。验证冷启动、缓存读取失败和缺失 satellite resource 的原有回退。阻塞相关网络链。 |
| 同段：NAudio、Imazen.WebP、libwebp64 | NAudio 汇编引用 `1.7.3.0`，未从 DLL 字节串找到 TFM；Imazen 引用 `10.0.1.0`、标记 net45；`libwebp64.dll` 为 x86-64 native PE | 核实可加载版本和 API，分别回归 [ModMusic](../../Plain%20Craft%20Launcher%202/Modules/ModMusic.vb#L266) 播放／暂停／停止／无设备，以及 [MyBitmap](../../Plain%20Craft%20Launcher%202/Modules/Base/MyBitmap.vb#L90) 实际 WebP 解码。初批 `win-x64` 与现有 native 资产匹配；Arm64 未验证。阻塞对应 UI 链和发布。 |
| 同段：Newtonsoft.Json、ThrottleDebounce；[Shared NuGet][mc-shared-project] | 主引用 Newtonsoft 汇编 `13.0.0.0`、DLL 标记 net45；Shared 包 `13.0.3`。Throttle 主引用 `2.0.2.0`、包 `2.0.2`，DLL 标记 netstandard2.0 | 优先固定现有语义，核对现代包资产与最终只加载的程序集版本。JSON 日期／数值／大小写／空值和节流尾调用、取消须走实际入口。JSON 替换不是升级 .NET 的必要条件。阻塞受影响数据／UI 验收。 |
| 主工程 Ookii.Dialogs.Wpf 与 [Wpf NuGet][mc-wpf-project] | 主引用汇编 `1.0.0.0`、仓库 DLL 标记 net462；Wpf 包为 `5.0.1`，包版本与汇编版本不是同一概念 | 核实 [Dialogs][mc-dialogs] 使用的 API 和实际加载身份，统一包资产、资源副本与发布闭包；验证文件／目录选择、取消、所有者窗口。阻塞 WPF 依赖验证。 |
| Shared：Microsoft.Win32.Registry `5.0.0`、PolySharp `1.15.0`；Wpf：PolySharp `1.15.0`；PCLCS 同版本 PolySharp | PolySharp 为 PrivateAssets 构建依赖；Registry 同时出现在 LF 动态装载名单中 | 核查 `project.assets.json`／`.deps.json`，避免把旧 Registry 副本覆盖框架资产。PolySharp 是否仍需要按目标 API 决定，不先批量升级／删除。验证注册表和发布加载。阻塞相关现代消费者验收。 |

上述 TFM 标记来自仓库 DLL 的只读字节检查，版本来自项目声明；没有据此声称程序集已在 .NET 10 加载或运行。旧 TFM 库可能通过兼容路径工作，必须记录选中资产、编译警告、运行调用和发布结果。

## 逐项兼容条件与验证

### A1．项目格式、宏与源码项（N002／N003 阻塞）

来源：[VB 属性与配置](../../Plain%20Craft%20Launcher%202/PCL-Living-Fork.vbproj#L1)、[LegacyLink 条件](../../Plain%20Craft%20Launcher%202/PCL-Living-Fork.vbproj#L125)、[版本分支](../../Plain%20Craft%20Launcher%202/Modules/Base/ModBase.vb#L18)、[完整原始清单](source-inventory.baseline.json)。SDK 默认包含 Compile／Page／ApplicationDefinition／resx，[规则见微软文档][desktop-sdk]；直接保留全部旧 Include 会重复编译，直接使用全量默认项又会启用原来关闭的 LegacyLink 源／页面。

候选：先显式保留旧求值范围，再逐类改用 SDK 默认项；关闭项与相应定义、调用、XAML 成组处理。原 VB Debug 配置 `DefineDebug=false`、定义 `SNAPSHOT`，C# SDK 默认 `DEBUG`／`TRACE` 会改变 `BuildType` 和开发初始化路径，须显式映射原有行为。保留 `AssemblyName=LivingPCL`、版本与产品信息；处理 SDK 自动 AssemblyInfo 与现有 [AssemblyInfo](../../Plain%20Craft%20Launcher%202/My%20Project/AssemblyInfo.vb#L11) 的重复属性。

最低验证：Windows 上分别求值 Debug／Release、LegacyLink false／true 的源码和资源项，与 G001／G002 分配逐项对照；实际构建验证定义分支、程序集身份和条件页面。不能靠排除未转换文件减少 VB 数量。

### A2．嵌入程序集、资源与加载上下文（N002／N003／N005 阻塞）

来源：[Application 构造器](../../Plain%20Craft%20Launcher%202/Application.xaml.vb#L225) 用 `AssemblyResolve`、前缀匹配及 `Assembly.Load(byte[])` 加载 [Resources.resx](../../Plain%20Craft%20Launcher%202/My%20Project/Resources.resx#L121) 的 13 类依赖；resx 还从 `PCLCS/bin` 读取构建产物。现代 .NET 仍有 `AssemblyResolve`，但字节加载会建立新的加载上下文，[加载算法见官方说明][assembly-loading]，不能假定和 SDK 默认依赖解析中的实例、类型身份及框架版本一致。旧 [App.config](../../Plain%20Craft%20Launcher%202/App.config#L1) 的 bindingRedirect 也不能承担现代 `.deps.json` 的职责。

候选：先以 SDK 正常依赖闭包和多文件输出来验证；确定仍需嵌入的项目及单一加载上下文，再调整资源与 resolver。改动须保留并发惰性初始化、缺失依赖的诊断和启动早期不递归加载的约束。保留 `PCL.Resources` 管理器名、[PCLCS LogicalName](../../PCLCS/PCLCS.csproj#L57)、Java JAR／Help／字体／图片名称及 `pack://` URI；磁盘大小写与逻辑资源名分别核对。

最低验证：列出实际 publish 文件和所有程序集／WPF 资源名；从干净输出目录冷启动，调用 JSON、注册表、对话框、音频、WebP 和 JAR 释放入口，记录同名程序集身份与加载上下文。禁用开发 bin 搜索路径后重复；并发图片解码和资源释放应无重复加载／写入冲突。resx 实际条目为文件引用的字节数组，不因模板注释就认定依赖 BinaryFormatter；生成后的资源仍须逐个读取验证。

### A3．WPF、My 和生成代码（N003 阻塞）

来源：[Application.xaml](../../Plain%20Craft%20Launcher%202/Application.xaml#L1)、[FormMain.xaml](../../Plain%20Craft%20Launcher%202/FormMain.xaml#L1)、[MyWpfExtension](../../Plain%20Craft%20Launcher%202/My%20Project/MyExtensions/MyWpfExtension.vb#L1)、[Resources.Designer](../../Plain%20Craft%20Launcher%202/My%20Project/Resources.Designer.vb#L16)、[Settings.Designer](../../Plain%20Craft%20Launcher%202/My%20Project/Settings.Designer.vb#L20)。VB 的短 `x:Class` 依赖 `RootNamespace=PCL`；C# 的 RootNamespace 不会自动给类声明补命名空间。My 提供 Application／屏幕／内存／键盘／剪贴板；当前 `Settings.settings` 本身没有设置项，产品的实际配置仍在 `Settings.*`／Setup.ini。

候选：C# partial 类与完整 `PCL.*` 的 `x:Class`、事件签名同步转换，核对 XAML 中 `assembly=mscorlib` 等 CLR 命名空间在现代引用集中的解析并重新生成 BAML。显式 `[STAThread]` 启动与 WPF Application 生命周期，保留 `StartupUri`、`OnExplicitShutdown`、异常处理、单实例及辅助命令的初始化顺序。My 调用逐个接到 WPF／Win32／现有适配器；资源生成类和空设置生成类明确替换或保留依据，不能自动引入一套新配置存储。

最低验证：Windows Debug／Release 实构建全部页面／模板；实际打开主要页面、剪贴板、文件对话框、皮肤／字体、屏幕定位、缩放、多显示器、滚动和焦点；对照动画打断／叠加、启动／关闭、窗口唤醒。编译通过不关闭 R003 交互验收。

### A4．Win32、COM、注册表与位数（N002／N003 相关调用阻塞）

来源：[窗口唤醒](../../Plain%20Craft%20Launcher%202/Modules/LauncherWindowActivation.vb#L8)、[拖放 COM](../../Plain%20Craft%20Launcher%202/Modules/ThirdParty/DragHelper.vb#L60)、[内存权限](../../Plain%20Craft%20Launcher%202/Modules/Base/NativeMemoryOptimizer.vb#L12)、[Shared 路径][mc-paths]／[Registry][mc-registry]、[文件回收 COM][mc-files]、[bcrypt][mc-bcrypt]。Windows 目标保留这些能力；`netstandard2.0` 本身不保证被调用 API 可在 Linux 执行。

候选：保留实际 Windows 边界、STA、结构布局、Boolean／字符串封送、句柄宽度和 Win32 错误检查；逐项核对旧 Declare 的 Integer／Long 与 C# int／long／IntPtr，而非按文本对应。x64 先与现有 libwebp64、Java 进程架构和权限辅助流程一起验证；暂不承诺 Arm64 原生构建。

最低验证：运行现有 `Test-LauncherActivation.ps1`、`Test-ToolboxMemory.ps1`、`Test-MapInstallAndRecycle.ps1` 的现代宿主版本；补实际 Unicode／长路径、回收站、拖放、权限被拒与提升后退出场景。不能为完成迁移而要求日常主程序始终管理员运行。

### A5．旧账号、加密与 JSON（N004 阻塞）

来源：[Settings DES 与密钥派生](../../Plain%20Craft%20Launcher%202/Pages/PageSetup/Settings.vb#L711)、[账号迁移](../../Plain%20Craft%20Launcher%202/Pages/PageSetup/Settings.Accounts.vb#L1)、[MeloongCore AES][mc-crypto]、[PCLCS Configs](../../PCLCS/Configs.cs#L1)。`DESCryptoServiceProvider` 在现代 .NET 有 SYSLIB0021；[官方候选为基类 Create][crypto-obsolete]，这不授权改变旧密文格式。LF 的 DES、Shared 的 AES 和构建凭据的 AES/HMAC 是不同数据边界。

候选：保持稳定哈希、标识派生、UTF-8、IV／模式／填充、Base64、旧键名及 JSON 解析／写回语义；替换 provider 时显式设置并以既有字节样本核对。先保留 Newtonsoft 语义，不在框架迁移中附带替换序列化器或重置旧账号。

最低验证：使用脱敏旧 Setup.ini／账号样本进行 net48→net10 读取、写回和约定方向的旧版复读；覆盖空值、损坏／部分迁移、Unicode、共享目录、锁、原子写入失败与重启恢复。现有 `Test-Release.ps1`／`Test-AuditFixes.ps1` 经宿主适配后的结果另记，实际登录刷新／取消仍单独验收。

### A6．编码、数值与 Java 输出（N002／N004 相关链阻塞）

来源：[默认编码判断](../../Plain%20Craft%20Launcher%202/Modules/Base/ModBase.vb#L81)、[Java 编码选择](../../Plain%20Craft%20Launcher%202/Modules/Minecraft/ModLaunch.vb#L1447)、[文件编码回退][mc-files]、[GB18030 动画文本](../../Plain%20Craft%20Launcher%202/Modules/Base/ModAnimation.vb#L1023)。现代 [Encoding.Default 固定 UTF-8][encoding-default]；GB18030／GBK／ANSI code page 需要确认 [CodePages provider][codepages] 在首次调用前注册。保留 `IsGBKEncoding` 的表达式而不处理运行时差异会改变路径选择。

候选：为旧数据、旧网页和 Java 输出显式指定原有编码策略；在入口统一注册所需 provider，并把“系统活动代码页”与“UTF-8 默认”分别表达。不能直接把所有文件改写为 UTF-8。

最低验证：中文与英文系统、系统 UTF-8 开／关、Java 8／17／21 的输出和 `native.encoding`／COMPAT；旧 GB18030 配置／网页／日志、非 ASCII 路径与字符替换。记录实际字节与显示文本，回归参数和用户文件写回。

数值补充（C004a）：现代 `Double.TryParse` 可将 `1e400` 解析为 Infinity，并接受更多特殊值字面；旧 Framework 对溢出返回失败。这会改变原 `MergeText` 根据 existing FOV 推断归一值／角度的结果。`PersonalOptionsRules` 仅在这一推断处保留旧特殊值成功条件：Trim 后精确匹配 invariant `Infinity`／`-Infinity`／`NaN`，其余非有限结果沿用原默认格式。依据固定 [Reference Source Double.TryParse](https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/mscorlib/system/double.cs#L274) 的公开行为核对，独立实现适配，没有引入其内部解析器。新增 .NET 10 样本与 Windows PowerShell CLR4 原始 TryParse oracle 对照；后者实际结果见账本。此处理不声称复现所有有限值极端舍入／Unicode 版本差异，其他数值入口仍需随所属功能对照。

路径补充（C006a）：Framework 的 [GetFileName](https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/mscorlib/system/io/path.cs#L1007)／Combine 会先调用 CheckInvalidPathChars；[固定非法字符与 device 例外](https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/mscorlib/system/io/pathinternal.cs#L328) 不等同于现代 [Path.GetFileName](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs#L221) 或 GetInvalidPathChars。直接保留源码 API 调用会把 NUL／引号／尖括号／竖线等的 ArgumentException 改为 LF 自定义 IOException，影响 Quick Play 日志逐项捕获边界。`QuickPlayWorldRules` 仅在现代 Windows 的对应两处独立补回旧检查，保留 UseLegacyPathHandling／device 例外及名称检查先于 folder 的顺序；net48 继续使用原 API，Linux 不模拟 Windows 文件名规则。不复制内部解析器或资源，现代手动 ArgumentException 使用固定英文信息，仅承诺原异常类型与顺序，不声称复制系统所有本地化文案。跨平台真实文件／参数测试与独立 Windows 旧 API oracle 分别验证，不把新旧运行时自己的宽松规则相互当作预期。

资源路径补充（C007b）：[137 例／52 候选的真实 CLR4 对照](RESOURCE_PATH_ORACLE.md) 已将旧异常／trim／受限长度、设备根与 Unicode Ordinal 差异落实为局部现代适配，并在 RequireEquivalent 下归零。FullPath 的检查必须在系统归约后进行，不能复用前述原串预检；旧 BCL／真实 Shared／原 config 和 manifest 继续独立注入验证。有限样本、短名时序与全部 raw BCL 输入限制保留，不能据此关闭整个 N002／R002。

扩展筛选补充（C007c3）：原 `GetExtension` 同样先检查 36 个旧 path 非法字符，区别于 SafeName 的 41 个 filename 字符；当前 `LegacyPathCharacters` 原样共享 C006a 检查，现代 Windows 在 GetExtension 前使用，net48 留给 BCL。旧 GetExtension 还把冒号视作分隔符，现代行为不同，但新增含冒号的扩展不可能等于原 9 项白名单；这里只保 AcceptFile 布尔结果与 null／未知 kind 之前的异常，不实现通用 GetExtension 替代。PowerShell CLR4 host 的路径开关可能与 LF 不同，device 消费者断言对照真实 host API，不能将其冒称 LF-default 独立进程。

### A7．HTTP、证书、取消与重定向（N002／N004 相关链阻塞）

来源：[HTTP client 与缓存](../../Plain%20Craft%20Launcher%202/Modules/Base/ModNet.vb#L225)、[启动网络配置](../../Plain%20Craft%20Launcher%202/Application.xaml.vb#L117)、[证书回调](../../Plain%20Craft%20Launcher%202/Modules/Base/ModNet.vb#L1929)、[LoginCertificatePolicy](../../Plain%20Craft%20Launcher%202/Modules/Base/LoginCertificatePolicy.vb#L4)、[SafeRedirectHandler](../../Plain%20Craft%20Launcher%202/Modules/Base/SafeRedirectHandler.vb#L1)。旧网络 API 存在[废弃及行为变化][webrequest]。不能假定 ServicePointManager 的全部配置按旧方式影响现代 handler；其中证书回调从 .NET 9 有到 SocketsHttpHandler 的[映射说明][certificate-callback]，仍须验证回调对象、主机和实际请求。

候选：按实际 handler 显式接入相同证书策略、代理、超时、解压与取消；把请求 URI 传给策略，保留严格模式及原有有限兼容域。保留每跳重新判断 CurseForge header 的流程，不扩大证书例外、凭据发送域或支持的 MMC 规则。

最低验证：本地可控 HTTPS／代理／跨域跳转场景验证证书成功、失败、严格开关、允许域与伪装域；验证取消后负载结束、缓存文件关闭、下载进度与失败回退。`Test-AuditFixes.ps1` 的策略反射和 `Test-ReviewFixes.ps1` 的 handler 测试之外，还需真实 HTTP handler 的集成验证。

### A8．进程、应用路径与提权（N003／N005 阻塞）

来源：[PathExe](../../Plain%20Craft%20Launcher%202/Modules/Base/ModBase.vb#L49)、[RunAsAdmin](../../Plain%20Craft%20Launcher%202/Modules/Base/ModBase.vb#L822)、[StartProcess](../../Plain%20Craft%20Launcher%202/Modules/Base/ModBase.vb#L889)、[Java 启动](../../Plain%20Craft%20Launcher%202/Modules/Minecraft/ModLaunch.vb#L2188)、[Paths.Base][mc-base]。`UseShellExecute` 在 Framework 默认 true、现代 .NET 默认 false（[官方定义][shell-execute]）；当前通用 wrapper 和 runas 路径依赖默认值，Java 则已显式 false。

候选：浏览器／文件关联／runas 显式走 shell，Java／带重定向进程显式不走 shell；保留工作目录、引号、环境和返回码。生产 apphost 路径候选为 `Environment.ProcessPath`，相邻数据目录沿用 `AppContext.BaseDirectory` 的明确含义。使用 `dotnet app.dll` 时 ProcessPath 是 dotnet 宿主，测试宿主也不能把自身路径当产品路径；须定义支持的启动方式并给测试传入真实产物路径。

最低验证：带空格／Unicode 的自定义 EXE 名、不同 cwd、文件／网站打开、runas 取消／成功、Java 标准流／退出码／环境变量、单实例唤醒、内存辅助命令；从最终 apphost 运行并核对实际路径，不能只反射构造 StartInfo。

### A9．CredentialBuild 与生成源（N005 阻塞）

来源：[Build-Release](../../scripts/Build-Release.ps1#L7)、[Write-BuildCredentials](../../scripts/Write-BuildCredentials.ps1#L1)、[BuildCredentials](../../Plain%20Craft%20Launcher%202/Modules/Base/BuildCredentials.vb#L6)、[现有回归](../../scripts/Test-BuildCredentials.ps1#L1)、[生成源项目项](../../Plain%20Craft%20Launcher%202/PCL-Living-Fork.vbproj#L1073)。生成器当前输出 `.g.vb`／VB partial 构造器；只换主项目扩展名无法编译新工厂。

候选：C# partial／静态构造器和生成器一起迁移，保留空公开回退、同名运行时覆盖、host 判断、v1 AES-CBC/HMAC 包装、独立 GUID 中间目录、构建锁及 finally 清理；SDK Compile 项须只包含一次生成文件。使用合成值验证，不改变真实凭据来源或打印值。

最低验证：无输入／仅一项／两项、Unicode／引号、非法多行、损坏认证数据、重复读回、运行时空覆盖；构建失败／生成失败／并发锁时没有遗留凭据源。多文件发布时检查全部交付 DLL／内容，不能只扫描 native apphost 后就报告主程序无明文；单文件也检查实际发布产物及对应托管内容。保留源码树不被生成器改写的检查。

### A10．回归测试宿主与 CI（N003／R002 阻塞）

来源：[现有 Build workflow](../../.github/workflows/build.yml#L1) 多处显式运行 `powershell -STA`，如 [Test-Release](../../scripts/Test-Release.ps1#L7)／[Test-WorldDetails](../../scripts/Test-WorldDetails.ps1#L6) 在宿主进程内 `Assembly.LoadFrom(EXE)`，凭据回归还用 VB CodeDom 编译。Windows PowerShell 5.1 使用 .NET Framework，不能直接承载 net10 主程序；native apphost 也不是可 LoadFrom 的托管程序集。

候选：优先建立引用真实生产程序集的 `net10.0-windows` WPF／STA 测试宿主，PowerShell 继续编排子进程、fixture 和退出码。可评估已基于 .NET 10 的 PowerShell 7.6（[官方版本对应][powershell]），先确认其 WPF 解析和 STA；单换命令名或用 .NET 8／9 的旧 pwsh 不算完成适配。CodeDom 动态编译改为受控 SDK／Roslyn fixture；需要反射时加载正确的托管 DLL，不加载 apphost。

最低验证：逐脚本登记“源码断言／纯规则／窗口 STA／真实子进程”，保留原断言和有效失败退出码；先迁移 WorldDetails、Release、BuildCredentials、UpdateBridge 四类代表用例，再覆盖原有全部测试。每次保存宿主的 `$PSVersionTable`、`RuntimeInformation.FrameworkDescription`、实际程序集与产物身份，不输出环境全集。保留旧 net48 基线 workflow，现代 Windows Debug／Release 构建／运行另有真实门禁；[迁移矩阵](../../.github/workflows/migration-validation.yml#L1) 的 .NET 10 无 WPF 测试只证明对应纯源码。

### A11．更新器、产物与旧版更新桥（N005 发布阻塞）

来源：[IsLivingExecutable](../../Plain%20Craft%20Launcher%202/Modules/ModLauncherUpdate.vb#L134)、[HandleUpdateCommand](../../Plain%20Craft%20Launcher%202/Modules/ModLauncherUpdate.vb#L178)、[Build-UpdateBridge](../../scripts/Build-UpdateBridge.ps1#L8)、[桥执行入口](../../scripts/update-bridge/Program.cs#L13)、[Build-Release 命名](../../scripts/Build-Release.ps1#L25)。当前协议下载、校验、嵌入和原子替换一个 EXE，原桥继续承担旧程序集／产品身份。

已确认的兼容阻点：

1. 现代 [apphost 是 native 可执行文件][publishing]。更新器和桥构建脚本对 EXE 调用 `AssemblyName.GetAssemblyName` 的身份检查不能直接用于 apphost／单文件产物；移除检查也不能作为替代方案。
2. `Assembly.GetExecutingAssembly().Location` 在多文件部署指向托管 DLL，在[单文件部署返回空字符串][single-file]。当前暂存路径、目录约束、SHA256 与替换对象均需要重新接到真实 EXE，不能只修复显示路径。
3. 普通多文件发布还包含 DLL、`.deps.json`、`.runtimeconfig.json` 和 native 资产；当前只复制一个 EXE／上传一个 EXE 的流程不会部署整个闭包。Framework-dependent 还需要 .NET 10 Windows Desktop Runtime；旧客户端只有 Framework 时不能假定新载荷可启动。

候选最小方向：先以无裁剪的多文件输出验证依赖，随后单独确定可保持旧单 EXE 协议的 `win-x64` self-contained／single-file 产物，连同 native 提取一起实测。若选多文件发布，必须先完成完整包的验证、事务替换和回滚，不能先切换产物再补更新。WPF／反射／资源路径尚未审计通过前不启用 trimming／NativeAOT；这不是当前性能任务。

桥继续单独用旧 CLR4／Framework 工具链构建，记录实际 reference assemblies 版本，保留 `LivingPCL` 身份、旧参数与 SHA256 交接；现代载荷校验应从受控发布输出取得托管身份与产品／版本信息，再绑定实际交付文件的 hash，覆盖旧目标与新目标的身份判定。现代主程序的运行时变化不要求旧桥机械升级。

最低验证：用实际发布产物完成“旧 LF→桥→现代载荷”和“现代→现代”，包含自定义旧文件名、父进程匹配、桥 hash／载荷 hash 区分、并发占用、超时、错误目录、非法名称、损坏文件、启动失败回滚和重启。先复用 [Test-UpdateBridge](../../scripts/Test-UpdateBridge.ps1#L1) fixture，再增加旧／新真实构建；只有合成 Framework EXE 的桥测试不足以验收现代载荷。隔离 Windows 环境分别测试有／无 Desktop Runtime，确认打包声明与结果一致。

### A12．Windows 支持范围（N003／N005 发布阻塞）

来源：[启动中的 Framework／Windows 7 检查](../../Plain%20Craft%20Launcher%202/Application.xaml.vb#L54)、[app.manifest](../../Plain%20Craft%20Launcher%202/app.manifest#L1)、现有 AnyCPU／`Prefer32Bit=false` 与 x64 WebP。微软 [.NET 10 支持表][supported-os]（查阅时更新于 2026-09-28）按 Windows 版本、edition、架构与生命周期列出支持；Windows 7／8.1 不在其中，不能沿用旧回退宣称兼容。Runtime 对 Server Core／Nano 的支持也不能推出 WPF 桌面可用。

候选：先验证并明确 `win-x64` 的桌面 Windows 支持矩阵，再按实际 API 设置 Windows TFM 版本／SupportedOSPlatformVersion、manifest、启动提示与发布说明。保留必要旧入口兼容，不把 Framework 4.8 注册表检查当现代运行时检测；self-contained 不会扩展 .NET 10 的 OS 支持范围。x86／Arm64 支持需各自依赖和进程实测。

最低验证：在所声明最低 Windows 版本和当前支持的 Windows 11 上运行最终 Debug／Release 与发布包；检查 DPI、长路径、权限、证书、音频、WebP 和更新。最新 `windows-latest` 的单一结果不能替代最低版本验收。

## 建议的小批次与退出条件

以下是现有 N002–N005／R002 内的拆分建议，实际编号和状态由账本登记；本报告不会关闭这些运行任务。

| 批次 | 最小边界与依赖 | 可审查的退出证据 |
| --- | --- | --- |
| N002 依赖探针 | 固定子模块与包资产；只处理 Shared／PCLCS 实际消费者，先于主窗口切换 | 现代消费者构建、选中依赖资产与 Windows 真实调用；MeloongCore.Wpf 兼容产物来源明确，既有 net48 回归继续通过。 |
| N002 规则适配 | 编码／旧数据 provider 与网络 handler 分开；不改业务格式和可信域 | 旧数据字节样本、实际 HTTP／取消／重定向样本、跨运行时对照，逐警告解释；与 C024／C025 的实际入口衔接。 |
| N003 项目与最小 WPF 宿主 | 在 C031／N002 满足后处理宏、项目项、My／生成源、资源名与程序集加载 | Windows Debug／Release、关闭／启用条件项构建；启动、页面资源、STA、单实例和最小控件链实际运行。保留完整交互后续验收。 |
| N003／R002 测试宿主 | 先 WorldDetails／Release／Credential／Bridge 四类，再逐项迁移现有脚本 | 与真实现代程序集／apphost 连接，保留有效失败返回和所有原断言；完整列表没有因宿主不兼容而静默跳过。 |
| N004 数据闭环 | 旧账号／设置、版本隔离、备份还原与原子写入逐功能链 | 脱敏旧数据双向约定、损坏／中断／恢复和实际账号操作结果；兼容承诺与已验证范围相符。 |
| N005 发布闭环 | 凭据生成与失败清理、现代路径／身份、产物闭包、旧桥各自小批完成，最后集成 | 最终交付文件清单／摘要；旧→新和新→新真实更新、失败回滚、无预装运行时测试；账号／生产配置保持原契约。 |

审计结论：有明确的迁移路径，当前项目格式、依赖闭包、测试宿主和更新／发布门禁仍需实际实施与 Windows 证据。继续推进 LF 自有代码的等价转换；不因此启动新增 MMC 规则、性能优化或新原生模块。

## 官方资料

- [.NET Desktop SDK 的默认项目项与 WPF 属性][desktop-sdk]；[WPF 与 Framework 的差异][wpf-differences]。
- [程序集加载算法][assembly-loading]；[发布与 native apphost][publishing]；[单文件部署的 API／native 提取限制][single-file]。
- [Encoding.Default][encoding-default]；[CodePagesEncodingProvider][codepages]；[UseShellExecute 默认值][shell-execute]。
- [旧网络 API 的废弃与行为变化][webrequest]；[证书回调的版本说明][certificate-callback]；[加密 provider 废弃说明][crypto-obsolete]。
- [Windows PowerShell 与现代 PowerShell 的运行时差异][powershell]；[.NET 10 OS 支持表][supported-os]。

[mc-shared-project]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Shared/MeloongCore.csproj#L1
[mc-wpf-project]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Wpf/MeloongCore.Wpf.csproj#L1
[mc-dialogs]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Wpf/General/Dialogs.cs#L73
[mc-paths]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Shared/Utils/PathUtils.cs#L108
[mc-base]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Shared/Utils/PathUtils.cs#L277
[mc-registry]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Shared/Utils/RegistryUtils.cs#L4
[mc-files]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Shared/Utils/FileUtils.cs#L278
[mc-crypto]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Shared/Utils/CryptographyUtils.cs#L63
[mc-bcrypt]: https://github.com/Meloong-Git/MeloongCore/blob/c774ae4c85fd94e0eaa171db21e78a65870df19b/Shared/Utils/CryptographyUtils.cs#L191
[desktop-sdk]: https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props-desktop
[wpf-differences]: https://learn.microsoft.com/dotnet/desktop/wpf/migration/differences-from-net-framework
[assembly-loading]: https://learn.microsoft.com/dotnet/core/dependency-loading/loading-managed
[publishing]: https://learn.microsoft.com/dotnet/core/deploying/
[single-file]: https://learn.microsoft.com/dotnet/core/deploying/single-file/overview
[encoding-default]: https://learn.microsoft.com/dotnet/api/system.text.encoding.default?view=net-10.0
[codepages]: https://learn.microsoft.com/dotnet/api/system.text.codepagesencodingprovider?view=net-10.0
[shell-execute]: https://learn.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute?view=net-10.0
[webrequest]: https://learn.microsoft.com/dotnet/core/compatibility/networking/6.0/webrequest-deprecated
[certificate-callback]: https://learn.microsoft.com/dotnet/api/system.net.servicepointmanager.servercertificatevalidationcallback?view=net-10.0
[crypto-obsolete]: https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib0021
[powershell]: https://learn.microsoft.com/powershell/scripting/whats-new/differences-from-windows-powershell
[supported-os]: https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md

资源包元数据补充（C007d1b）：原式C#提取的真实CLR4 v5首轮对照（`4eb50fb`）发现18个JSON数值差异，完整原net48消费者仍通过。局部现代reader保持固定Newtonsoft完整Parse顺序，区别真实Infinity字面量和数值溢出，逐token还原负零；仅可见标量有限double采用G15，容器继续原writer。出处、实测异常／partial ref及artifact哈希见[对照记录](RESOURCE_PATH_ORACLE.md)。后继291／56包含CRLF／buffer／覆盖字段与DoubleMax／epsilon／R／G15中点探针，尚待真实Frame确认；G15格式名本身不能证明所有旧舍入或有限解析位值等价，不能将本批关闭成整个序列化／N002验收。

C007d1b后继：`93c51e2` 真实291／56虽消除最初18项，但新增4项旧有限解析位值／R／中点差异，促成仅metadata内的MIT旧转换子集、独立15／17文本及保15位尾零的R回读选择。新增v6的397／56用原Frame分别比较解析位值和rawbits格式，等待实际结果。保留原CRT尚未全域证明的限制，不据局部port改全局数字行为，也不将旧NumberToDouble的有意低位损失当成性能问题优化掉。

C007d1b验收：`476eb42` 的真实v6 397／56（metadata101／parse34／format72）、367／399单元与Windows各41步通过，四失败数组空，旧R保15位尾零与数值边界均由实际Frame确认；两轮失败及修复见[报告](RESOURCE_PATH_ORACLE.md)。局部批次已完成，仍不将有限样本外推所有CRT／文化／架构或完整现代WPF；N002／R002保持待办。
