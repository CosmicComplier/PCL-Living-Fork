# 迁移状态与续跑入口

目标见 [MIGRATION_GOAL.md](MIGRATION_GOAL.md)，完整背景见 [MIGRATION.md](MIGRATION.md)，模块对应关系见 [MODULE_MAP.md](migration/MODULE_MAP.md)。本文件是持续任务的进度账本；每次执行后在原条目上更新，不重复建立迁移计划。

## 当前状态

- 目标状态：`in_progress`。当前仅推进既有 LF 的 C#／.NET 10／WPF 与等价回归。
- 集成分支：`migration/csharp-modern-dotnet`。工作起点为 `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`；`main` 不参与提交或合并。
- 最新已完整验证的生产检查点 `33a1c6aebfac08088de06a3d0d06c7aa959e47e9`：Windows Debug／Release 各 41 步成功；.NET 10 迁移矩阵 Ubuntu 189／189、Windows 194／194 通过。C002／C003／C004／C005a／b 核心和适配验收完成；主产品仍为 net48，整项现代运行时和实际交互验收尚未完成。
- 当前推进 C005c1：按键页面纯显示、搜索、计数和分组规则；整页后台／交互 C005c2 仍缺 WPF 前置，不隐式关闭母项。此前各核心有下方实际证据。
- 续跑原则：先收尾 `in_progress` 中依赖满足的最早任务，再选择任务表中最早可执行项。被阻塞项保留原编号与原因，独立任务继续推进。

## 证据与基线

| 项目 | 实际结果与证据 | 适用范围及限制 |
| --- | --- | --- |
| LF／PCL 来源 | LF `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`；PCL `5ef81e490ae46b1100d61e33d0183094c7a80ca8`；`git merge-base --is-ancestor` 返回 0 | PCL 基线在 LF 可达历史中，完整历史 checkout 可重复清单；不读取工作树改动 |
| CE 来源 | `v2.15.1` / `1a7653127d578efdb2a9388999a3fdc67d9e49d5` | 同源参考；具体复用仍逐文件记录 |
| 初始源码清单 | [source-inventory.baseline.json](migration/source-inventory.baseline.json)：249 个源文件、62,474 行；VB 168／55,900 行，C# 10／1,034 行，XAML 71／5,540 行 | Git 固定树统计；其中产品 248 文件，更新桥工具 1 文件。不是迁移完成度百分比 |
| 相对 PCL 同路径变化 | 新增 66，修改 67，未变 116；另有 2 个上游源路径在 LF 中移除 | 按 Git blob／mode 比较，不推测重命名；详细路径见 JSON |
| 项目声明 | 原 VB 项目显式项、PCLCS SDK 默认项；4 个 LegacyLink 条件源保留；唯一无项目声明源为 `scripts/update-bridge/Program.cs` | XML 源码声明，不是 MSBuild 求值；生成凭据项保留为未求值表达式；更新桥由构建脚本动态生成项目 |
| 初始 Windows 自动化 | [Build run 41656492901](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41656492901)，Release job `125988076604`、Debug job `125988077228` 均 success | 外部 GitHub Actions 证据，每 job 40 步成功、含 25 个 Verify 步骤及 legacy bridge；不是当前 Linux 本地运行，也不证明迁移后 GUI 或账号端到端已验证 |
| 清单工具重复运行 | 固定 LF `--ref` 输出成功；CI 基线核查批次独立重复运行并与提交候选基线逐文本一致 | Python 标准库和 Git；无时间、机器路径、凭据或子模块内容；新批次 `--ref HEAD` 需在提交后另产当前清单 |
| 首批 C# 解析器测试 | `dotnet build tests/PCLLF.Migration.Tests/PCLLF.Migration.Tests.csproj --configuration Release --no-restore -warnaserror` 零警告／零错误；`dotnet run --project tests/PCLLF.Migration.Tests/PCLLF.Migration.Tests.csproj --configuration Release --no-build` 27／27 组通过 | Linux .NET SDK 10.0.403，编译链接真实生产解析器；覆盖现代／旧世界、16 字段、signed 值、UTF-8、未知类型、截断、短路、64 层与 16 MiB 边界及流所有权；不代替 Windows 外壳与 GUI 回归 |
| 现有辅助库编译 | `dotnet build PCLCS/PCLCS.csproj --configuration Release -p:EnableWindowsTargeting=true` 成功，零警告／零错误 | Linux 对现有 net48 的 PCLCS／MeloongCore 编译验证；不是 WPF 主程序运行结果。修正资源 Include 的磁盘大小写，原 LogicalName 保持不变 |
| 首批独立自审 | NBT 旧语义、PCL.WorldMetadata 16 字段／类型及 Read／ReadDetails／Describe 反射入口保持；所有字段映射完整，Describe 文案未变 | 源码逐段对照与独立复核；后续 C002／C003 实现也已通过下列各自 Windows／迁移矩阵，不能将这一结论外推到尚未实施的模块 |
| C002／C003 当前清单 | `--ref cfd4cbf23bb276fba800e930f8cb67c39b11c426`：255 个源文件，其中产品 251、测试 3、工具 1；产品 VB 仍为 168 文件／55,692 行 | 当前清单在忽略的 artifacts 下重新生成；初始基线不改写。兼容门面仍是产品 VB，不能按 C# 增量或总行数声称完成度 |
| C004 当前清单 | `--ref 1c1841f00298579fca865e703af04b8bc08fc521`：261 个源文件，其中产品 254、测试 6、工具 1；产品 VB 仍为 168 文件／55,540 行 | 已解耦的核心不代表 VB 适配、页面、控件或现代主工程已转换；最终源码归零仍由 R001 实际核查 |

本轮已推送的实现与验证检查点：

| 检查点 | 修改及实际验证 |
| --- | --- |
| `c3a2838` | 固定 goal、提交／PR 策略、LF／PCL／CE 模块映射、可重复源码清单和进度账本；后续规则按用户补充继续完善 |
| `e47fc55` | 修正 OptiFine 哈希资源磁盘路径大小写，保持 LogicalName；现有 net48 辅助库在 Linux 编译通过 |
| `bdad3a7` | 将 NBT 引擎迁入 `PCLCS.WorldMetadataReader`，保留 PCL.WorldMetadata 字段和生产入口；新增真实 NBT 测试 |
| `d27f394` | 新增限定迁移分支的 .NET 10／源码清单 CI；以下两次远端运行均针对该完整提交 |
| `c7cb287` | 固定详细 commit／PR 正文要求、模型边界和 .NET 10 兼容审计；现代运行门禁仍保留 |
| `0388bd6` | C002a 文件／gzip／详情文案迁入 C#；15 组真实文件样本加入既有 27 组测试；Windows 原详情脚本增强全部字段、精确格式、共享／只读及真实符号链接检查 |
| `cfd4cbf` | C003 持久化／原子写入／进程身份迁入 `LocalGameDataStore`；保留原 Gate／动态 TestRoot／所有入口。新增 20 组跨平台实际样本与 2 组 Windows 文件语义；EngineOnly 夹具及其专门 CI 接通 |
| `8a9cd4f` | 固定 C002／C003 已通过的完整远端证据，关闭对应核心与适配验收；拆出 C004a／b／c，原产品仍为 net48 |
| `758a3c2` | C004a 个人选项纯规则迁入 `PersonalOptionsRules`，保留原动态设置；窄适配 .NET 10 的 existing FOV 非有限解析变化，新增 28 组规则测试和 42 条 CLR4 原始解析 oracle |
| `35114dd` | C004b options.txt 原始读取和合并写回进入 `PersonalOptionsFiles`；18 组跨平台／20 组 Windows 文件样本，保留 no-op／BOM／hash／备份及成功后 cache 顺序 |
| `1c1841f` | C004c 导入／准备／跟踪／捕获／恢复进入 `PersonalOptionsFlow`，惰性实例适配、原 Store／Gate 和派生字段 DTO 接通 23 个原入口；27 组新增真实流程样本及 Windows 原会话字段／对象状态断言 |
| `f36c6e6` | 关闭 C004 三批核心／适配验收，记录完整证据与当前清单，拆出 C005a／b／c 及原按键／标题换行约束；现代主工程和最终适配删除仍未完成 |
| `a726586` | C005a Save 迁入 `InstanceKeybindingFiles`，21 组跨平台／23 组 Windows 文件样本，原 VB 门面接通；原脚本验证 same-value backup 轮换，模型／读取／默认表未改 |
| `33a1c6a` | C005b 模型／默认／读取归属进入 `InstanceKeybindingReader`，33 组 Linux／32 组 Windows 读取 ZIP 样本；补 Framework 压缩引用，原模型字段与实际 Meloong 委托验证接通，并拆开 WPF 宿主前置与最终汇总依赖 |
| [Windows Build 41696728260](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260) | [Debug job 126129118107](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260/job/126129118107) 与 [Release job 126129118529](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260/job/126129118529) 各 40 步 success，包含原有自动化回归、凭据保护、旧更新桥和产物上传；主程序仍为 net48，不能据此关闭 N003 或 R003 |
| [Migration validation 41696728492](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728492) | Ubuntu job `126129118456`、Windows job `126129119218` 均 success：固定清单一致，.NET 10 真实生产解析器测试通过；这只验证已提取的解析器范围 |
| [C002 Windows Build 41705399746](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41705399746) | 提交 `0388bd6f6f99ac142a3a6d88032a70ba727fe6f3`：Debug job `126159247355`、Release job `126159247567` 各 40 步 success；增强后的真实 WorldMetadata 门面、文件链接校验、只读／共享、精确文案及旧回归均运行通过 |
| [C002 Migration validation 41705399845](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41705399845) | Ubuntu job `126159247961`、Windows job `126159248400` 均 success，42／42 组生产解析器／文件／文案测试及固定源码基线核查通过 |
| [C003 Windows Build 41705751541](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41705751541) | 提交 `cfd4cbf23bb276fba800e930f8cb67c39b11c426`：Debug job `126160481124`、Release job `126160481435` 各 41 步 success；含原完整回归、两种方式的真实备份引擎、个人习惯／Quick Play、还原／克隆／取消恢复、凭据保护、旧更新桥和产物上传 |
| [C003 Migration validation 41705751480](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41705751480) | Ubuntu job `126160481337` 62／62、Windows job `126160481670` 64／64 组通过，固定清单一致；日志确认两个符号链接用例均实际执行，Windows 共享锁和只读拒绝两组也通过，没有 SKIP。这些是功能样本，不代替完整 GUI／真实账号／游戏运行验收 |
| [C004a Windows Build 41708039034](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41708039034) | 提交 `758a3c2d1631cd30f4619e6a5fc9a520f776c763`：Debug job `126168470323`、Release job `126168469671` 均 success，原完整功能链及独立备份引擎通过。日志中的 42 条 CLR4 oracle 确认 `1e400`、特殊字面大小写／符号与空白的旧接受语义，包含 profile／会话和按键实际消费者回归 |
| [C004a Migration validation 41708039126](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41708039126) | Ubuntu job `126168470971` 90／90、Windows job `126168471703` 92／92 组通过；含 28 组规则样本、原文件语义与固定源码清单。现代输入收窄行为与上述真实旧运行时结果对应；不声称复现全部有限值极端精度 |
| [C004b Windows Build 41709819895](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41709819895) | 提交 `35114dd4af209eda009b47e027f9ce19525b8994`：Debug job `126174639661`、Release job `126174640224` 各 41 步 success；原完整消费者、独立备份引擎、CLR4 oracle 与旧更新桥通过 |
| [C004b Migration validation 41709819781](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41709819781) | Ubuntu job `126174639198` 108／108、Windows job `126174639572` 112／112 组通过，固定清单一致；Windows 日志确认原读取共享行为和只读写失败两组实际通过，无链接 SKIP |
| [C004c Windows Build 41712282261](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41712282261) | 提交 `1c1841f00298579fca865e703af04b8bc08fc521`：Debug job `126183120514`、Release job `126183119989` 各 41 步 success；原 VB 会话类型、六继承字段、原对象 Tracked 更新、个人设置／按键／日志恢复、独立备份引擎及旧更新桥等完整回归通过 |
| [C004c Migration validation 41712282087](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41712282087) | Ubuntu job `126183119631` 135／135、Windows job `126183120114` 139／139 组通过；含 27 组真实流程、六字段派生 DTO／factory 身份、Monitor 重入、失败后日志保留等样本，固定源码清单一致，无 SKIP |
| [C005a Windows Build 41713871974](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41713871974) | 提交 `a726586a6e9b21b4930f6e595017218c2bdc5076`：Debug job `126188672461`、Release job `126188672911` 各 41 步 success；原按键消费者、same-value backup 哨兵、stale 编辑、完整原回归及旧更新桥通过 |
| [C005a Migration validation 41713872243](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41713872243) | Ubuntu job `126188673706` 156／156、Windows job `126188673254` 162／162 组通过；新共享写失败／只读失败两组实际执行，链接无 SKIP，固定清单一致 |
| [C005b Windows Build 41716125675](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41716125675) | 提交 `33a1c6aebfac08088de06a3d0d06c7aa959e47e9`：Debug job `126196457564`、Release job `126196457417` 各 41 步 success；实际 VB 类型／七继承字段／31 默认、Meloong CRLF／VT 标题委托、原归属／冲突／保存／页面消费者及旧更新桥通过 |
| [C005b Migration validation 41716125643](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41716125643) | Ubuntu job `126196457531` 189／189、Windows job `126196458304` 194／194 组通过，固定清单一致；读取新增 33／32 组差异是 Windows 不创建仅大小写不同文件的单项 fixture，非异常／取消能力被跳过 |

清单重新生成：

```bash
python3 scripts/Inventory-Migration.py --ref f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 --output docs/migration/source-inventory.baseline.json
python3 scripts/Inventory-Migration.py --ref HEAD --output artifacts/migration/source-inventory.current.json
```

初始基线固定不随迁移提交变化；当前清单用于与初始清单对照。工具只读指定 Git 提交树，未提交的转换不会反映在 `HEAD` 清单里。JSON 的项目条件、属性、导入和 Remove／Update 不执行求值；以实际 MSBuild 和 Windows 构建作为编译范围的最终证据。

## 开始前已有改动与协作归属

首次进入本轮任务时已存在 `README.md` 修改和未跟踪的 `docs/MIGRATION.md`，不将其回滚或覆盖。后续出现的代码、测试和工作流改动属于当前并行批次；集成前核对实际差异和所有者，未知变更另行记录。

| 范围 | 当前批次责任 | 集成约束 |
| --- | --- | --- |
| `docs/MIGRATION_GOAL.md`、`docs/MIGRATION_STATUS.md`、清单工具和初始 JSON | 目标与源码清单 | 集成者统一审查及提交 |
| `docs/migration/MODULE_MAP.md` | 模块映射 | 不与目标账本互相覆盖 |
| `.github/workflows/migration-validation.yml` | CI 基线核查 | 保留既有 `build.yml` 的 Windows 完整回归 |
| `PCLCS/Minecraft/WorldMetadataReader.cs`、`tests/PCLLF.Migration.Tests/` | C# 生产解析器与样本 | 无 WPF 测试须连接生产源码 |
| `WorldMetadata.vb` 适配、其余集成与远端操作 | 集成者 | 保持原生产入口；核实新代码实际调用和构建 |
| `InstanceKeybindingView.cs`、`InstanceKeybindingViewTests.cs`、按键页面的 DisplayKey／RenderList 规则接线 | C005c1 子代理已交付，主执行者集成 | 子代理不再写；XAML、其它方法、事件／Loader／捕获不改，父统一接线／提交，审查者只读 |

续跑时重新检查此表和 `git status --short`；此处的所有权不代表未知新变更可被覆盖。

## 任务依赖与验收

状态只有 `todo`、`in_progress`、`blocked`、`done`、`deferred`。`done` 必须关联实际改动或证据。以下顺序用于选择最早可执行任务；每项仍须按功能链拆成可审查的小批次，拆分结果写在原编号下。

| 编号 | 状态 | 依赖 | 工作与完成证据 |
| --- | --- | --- | --- |
| G001 | done | 无 | 固定 LF／PCL 源清单工具与初始 JSON；可重复输出、条件源和项目声明已记录，见基线表 |
| G002 | done | G001 | LF／PCL／CE 模块映射、生产入口、依赖和逐组迁移边界；核对源清单覆盖，并记录 CE 参考位置及未查证处 |
| G003 | done | 固定 LF 来源 | 初始 Windows Debug／Release 构建与既有自动化回归；证据为 run `41656492901`，GUI 对照仍归 R003 |
| G004 | done | G001 | 迁移 CI 已在双平台通过：固定源码清单重现和 27 组纯 C# 生产解析器测试；见 run 41696728492 |
| B001 | done | G002 | 模块映射已固定实例／组件／依赖／启动／文件计划与适配边界，存档流接口已有生产调用与样本验证；其余模型生产实现随各功能任务完成，不声称整个 PCLCS 已无 WPF |
| C001 | done | G001、G003 | WorldMetadata NBT 引擎已进入 C# 生产类，保留 VB 兼容外壳；27 组真实样本与边界通过，Windows Debug／Release 原消费者完整回归通过 |
| C002 | done | B001、C001 | 文件／gzip／详情文案已迁入 C#，原三个消费者经兼容入口接通；42 组双平台样本与 Windows Debug／Release 完整回归通过。最终删除类型映射／路径日志 VB 门面仍由 C006／C010／C029／C031、R001 验收，不声称页面后台已转换 |
| C003 | done | B001 | `LocalGameDataStore` 已接通所有原持久化入口，保留 Gate、动态 TestRoot、旧 JSON／编码、摘要检查、备份与失败清理；Windows 两种备份引擎与完整旧回归通过，.NET 10 Ubuntu 20／Windows 22 组对应样本通过。剩余 VB 转发门面归最终主工程验收 |
| C004 | done | C003 | 规则／文件／会话三批均已接通真实消费者并通过各自 Windows Debug／Release、双平台矩阵和必要 CLR4 oracle；原数据、Gate、会话字段／类型适配保留。VB 兼容门面与设置／实例平台适配的最终 C# 替换仍归后续主工程／R001，不据此关闭整项迁移 |
| C005 | in_progress | C004 | a／b 已通过全部真实门禁，c1 正在提取页面纯规则；c2 整页仍因 C027／C029a 阻塞，单独后端或呈现规则不关闭母项 |
| C006 | todo | C002、C003 | 迁移 `QuickPlayWorlds` 与最近世界入口；版本门槛、路径、参数、缺失世界与首页动画顺序回归 |
| C007 | todo | B001 | 迁移本地资源路径和文件模型、`InstanceResourceStore`；现有资源种类、隔离目录、边界和列表行为回归 |
| C008 | todo | C007 | 迁移地图安装、回收站与对应入口；归档路径、同名保护、取消、还原及彻底删除回归 |
| C009 | todo | C007 | 迁移 `InstanceResourceUpdates` 与替换流程；平台匹配、用户文件保留、下载进度和失败恢复回归 |
| C010 | todo | C002、C003、C007 | 迁移 `WorldBackups` 与备份界面链；退出自动备份、更新前备份、手动保护、分组和保留策略回归 |
| C011 | todo | C003、C007 | 迁移 `EnvironmentHistory`／`RecentEnvironmentChanges` 与还原点界面链；恢复前留档、保留当前存档和崩溃对照回归 |
| C012 | todo | C003、C007 | 迁移实例测试副本、来源分组与导出版本隔离；克隆、路径、版本选择折叠和导出隔离回归 |
| C013 | todo | B001 | 迁移本地模组声明解析与 `ModDependencyInspector`；可靠声明、内嵌 JAR、依赖和冲突样本回归 |
| C014 | todo | C013 | 迁移在线依赖匹配与 `ModDependencyOnline`；精确平台匹配、超时、本地降级与缓存语义回归 |
| C015 | todo | C013、C014 | 迁移 `LaunchDependencyCheck`／`ModDependencyAutoFill` 与开关入口；启用禁用文件、确认、补全、复查和本次会话去重回归 |
| C016 | todo | C013、C015 | 迁移依赖图标、关系图、影响确认与检查控件；保持交互、分组和原生动效，完整调用链回归 |
| C017 | todo | B001 | 迁移 Java、版本和组件识别；覆盖 `ModJava`、`ModMinecraft`、PCLCS 现有模型与加载器版本，既有 Java 选择和组件显示回归 |
| C018 | todo | B001 | 迁移资源项目／版本／搜索和在线下载规则；保留网络适配与下载任务生命周期，资源入口和详情回归 |
| C019 | todo | C017、C018 | 迁移下载与安装的现有完整功能链；按加载器和原版安装拆子任务，安装、失败、取消及目录隔离回归 |
| C020 | todo | C007、C011、C019 | 迁移 `ModModpack`／更新／更新流／整合包模式及对应页面；保持现有 MMC 等格式，个人修改保留、回滚和实际进度回归 |
| C021 | todo | C004、C006、C010、C015、C017 | 迁移 `ModLaunch`／`ModWatcher`／`ModCrash` 的生命周期与适配；按预检、进程、监控拆子任务，启动、退出和崩溃对照回归 |
| C022 | todo | B001 | 迁移设置存储、兼容和账号规则；覆盖 `Settings.*`／`SettingService`、共享 `Setup.ini`、旧账号数据和证书策略，保留格式和密钥边界 |
| C023 | todo | C017、C021、C022 | 迁移登录页、皮肤和主页的生产调用；脱敏账号旧数据、取消、错误、刷新及启动交接验证，实际账号行为单独记录 |
| C024 | todo | B001 | 迁移基础校验、网络和日志规则及其适配；旧异常语义、重定向凭据、编码和日志回归 |
| C025 | todo | C024 | 迁移 Loader 与文件操作生命周期；等待实际任务结束、取消、回滚、恢复和版本占用的故障场景回归 |
| C026 | todo | C025 | 迁移基础动画、位图与 WPF 适配；覆盖线程、资源、动画打断和叠加，实际交互对照 |
| C027 | todo | C026 | 迁移通用控件、行为和第三方控件适配；按控件族拆分，事件、绑定、焦点、滚动和禁用状态回归 |
| C028 | todo | C018、C019、C027 | 完成下载页面的 C# 后台与 XAML 调用，确认没有残留 VB 功能实现和重复入口 |
| C029a | todo | C027、C005b | 建立可编译 net48 C# WPF 呈现库及惰性 host 契约，引用方向为 VB main→Presentation→Core，Presentation 不引用 main；先用按键页验证程序集／资源／控件／路由接线，不依赖 C005 母项或 C029 汇总 |
| C029 | todo | C005c2、C008、C010、C011、C012、C016、C020、C027 | 完成实例页面和资源页面 C# 后台／XAML 的最终汇总，已有先行页面只验收集成，不与 C029a 宿主前置相互等待 |
| C030 | todo | C022、C027 | 完成设置、个性化、工具、帮助、关于及现有可选联机页面的迁移；默认关闭项单独验收，不静默删除 |
| C031 | todo | C023、C025、C026、C027、C028、C029、C030 | 成组完成 `Application`、`FormMain`、`ModMain`、事件／音乐／维护等剩余入口；窗口、路由、单实例和关闭生命周期回归 |
| N001 | done | G002、G003 | [兼容审计](migration/DOTNET10_COMPATIBILITY.md) 已完成工程／依赖清单、12 项门禁与小批次建议；仅表示审计交付，实际现代运行验证仍由 N002–N005／R002 关闭 |
| N002 | todo | N001、C024、C025 | 将 LF 自有 C# 库适配 .NET 10；实际项目构建、依赖加载和纯业务测试通过，框架过渡不遮蔽旧路径 |
| N003 | todo | N002、C031 | 主程序切换 C# SDK WPF／.NET 10，处理 My／VB 生成代码、XAML、资源名和程序启动；Windows Debug／Release 实构建与运行 |
| N004 | todo | N003、C022 | 验证旧账号和配置数据、共享目录、版本隔离、备份／还原点与旧格式写回，保留脱敏样本和实际结果 |
| N005 | todo | N003、N004 | 完成凭据生成、资源依赖、发布脚本、产物命名、单实例与旧版更新桥；测试发布产物和旧更新入口交接 |
| R001 | todo | N003、N005 | 逐项处理初始清单中所有 VB／生成／条件源码，检查当前产品 VB 数为零、项目声明完整及许可来源齐全 |
| R002 | todo | R001 | 迁移后的完整 Windows Debug／Release 自动化回归、现有 MMC 和普通整合包样本；每项保存真实命令、提交和 CI 结果 |
| R003 | todo | R002 | 实际 Windows 运行与新旧 UI 对照；账号、游戏启动／退出、文件中断恢复、按钮、滚动、页面及动画行为验收 |
| R004 | todo | R003 | 汇总全部完成证据和剩余限制，自审最终差异与远端状态；仅在目标全部验收后标 `completed` 并暂停续跑 |
| D001 | deferred | 当前目标完成后另立目标 | 新 MMC／Prism 组件规则、补丁语义、Java 交集、LWJGL 与 GTNH 增强 |
| D002 | deferred | 当前目标完成后另立目标 | 性能优化；当前只保留必要基线与等价回归 |
| D003 | deferred | 当前目标完成后另立目标 | 新 Rust／C++ 模块或其他原生化改造 |

任务存在交叉文件时，以最先完成的功能链为准更新后续任务的剩余范围，不重复转换。例如 C010 已完成备份页面后台，则 C029 只验收该入口集成并处理尚余页面。较大的 C017、C019、C020、C021、C027、C031 在执行前按可运行入口拆出子编号；子任务完成不会隐式关闭母项的其他验收。

C004 按实际生产入口拆分如下，不提前改变原会话与文件事务：

| 子任务 | 状态 | 依赖 | 实现与验收边界 |
| --- | --- | --- | --- |
| C004a | done | C003 | `758a3c2`：白名单、解析、键码／FOV／修饰符、严格 UTF-8 解码与合并已进入 C#；保留原动态 Settings 和所有入口，28 组新增生产样本与完整 Windows 功能链通过，42 条 CLR4 oracle 验证必要的现代运行时适配 |
| C004b | done | C004a | `35114dd`：原始读取／写回已接入 `PersonalOptionsFiles`，复用 C003 原子写入；18 组 Linux／20 组 Windows 对应文件样本及完整 Windows Debug／Release 消费者通过 |
| C004c | done | C004b | `1c1841f`：Flow／惰性接口／兼容 DTO 接通原 23 入口，新增 27 组流程样本与实际 VB 反射断言；Windows Debug／Release 各 41 步与 Ubuntu 135／Windows 139 组总矩阵通过 |

C005 先按实际生产入口拆分，Save 与个人习惯合并写回的语义不同：

| 子任务 | 状态 | 依赖 | 实现与验收边界 |
| --- | --- | --- | --- |
| C005a | done | C004 | `a726586`：Save 核心和原四参数入口已接通，Windows Debug／Release 各 41 步、Ubuntu 156／Windows 162 组总矩阵与原 same-value backup 消费者断言通过；读／默认表／模型未在此批改动 |
| C005b | done | C005a | `33a1c6a`：模型／31 默认／读取归属及原泛型／Save 保持，Windows 各 41 步与 Ubuntu 189／Windows 194 组总矩阵通过；七继承字段、原类型与实际标题委托已验证 |
| C005c1 | in_progress | C005b | `InstanceKeybindingView` 已接通原两方法，21 组新金值令 Linux 总矩阵 210／210 通过；泛型保原项／精确比较／全局总数，net48 核心／VB 索引桥与独立终审通过，待 Windows 真实 KeybindingGroups |
| C005c2 | blocked | C005c1、C027、C029a | 整页 C# 后台／XAML 与 Loader／捕获／订阅／动画／确认接线；当前主项目只编译 VB partial、PCLCS 不能反向引用 main，自定义控件和路由仍在 main。待单向呈现库／host 可编译后迁入页面并去除旧 VB 后台，不以未编译文件占位完成 |

页面前置以实际项目为证：`PCL-Living-Fork.vbproj` 显式编译旧后台及 XAML，主程序已经引用 PCLCS；页面继承主程序集的 `MyPageRight`，还依赖同程序集的自定义控件、`LocalResourceUi`、Loader、动画、实例与 `FormMain`。因此 `C029a` 是独立前置子任务，不继承 C029 的汇总依赖；`C005c2→C029a→C027` 与 `C029→C005c2` 无环。不得把 `C005c2` 依赖改成整个 C029／N003，否则会经 C031 回到自身。c2 阻塞期间母项不能标 done，按 goal 继续 C006 等最早 ready 核心任务；这不是需要用户处理的持久阻塞。

## 当前批次与下一步

| 批次 | 当前进度 | 仍需完成 |
| --- | --- | --- |
| G001 | 已随 `c3a2838` 推送，双平台 CI 固定清单重现一致 | 当前固定基线不随后续迁移改写；需要时为新提交生成独立当前清单 |
| G002 | 模块映射完成：14 条功能链、固定源码清单全部 249 文件归组；53 个链接目标、46 个脚本名核实 | 已完成映射交付，后续按实际迁移更新对应行，不代表功能已迁移 |
| G004 | 已随 `d27f394` 推送，Ubuntu／Windows 迁移矩阵通过 | 后续生产改动继续使用该工作流，并按实际依赖扩展测试 |
| C001 | C# 解析器、原 VB 适配和 27 组测试已提交；独立自审、net48 辅助库编译、完整 Windows Debug／Release 及双平台迁移矩阵通过 | 本项完成。文件／gzip／格式与消费者的进一步 C# 迁移属于 C002，VB 兼容外壳仍纳入最终源码清单 |
| C002a | 已随 `0388bd6` 推送；42／42 双平台与 Windows Debug／Release 完整回归均通过，真实路径适配／文件链接、共享／只读、16 字段、精确 CRLF 和句柄释放已实测；独立源码审查通过 | 本批完成。尚未转换详情页／Quick Play／备份 UI 后台及最终门面，不据此关闭 C006／C010／C029／C031、R001 或整项迁移 |
| C003a | 已随 `cfd4cbf` 推送；完整 Windows Debug／Release 和 EngineOnly 通过；Ubuntu 62／Windows 64 组总矩阵通过，其中本机资料 20／22 组，链接用例无跳过。独立源码复核与 net48 编译通过 | 本批完成。陈旧摘要测试是顺序模拟外部编辑，进程权限不明分支只有源码核对，不声称多线程竞争／真实进程权限错误已实测；VB 转发门面仍在最终源码清单中 |
| C004a | 已随 `758a3c2` 推送，Ubuntu 90／Windows 92 组总矩阵与完整 Windows Debug／Release 均通过；真实 CLR4 oracle 42 条通过，独立源码审查和 net48 编译通过 | 本批完成；固定微软来源与有限值极端精度限制见兼容审计，C004b／c 不因此完成 |
| C004b | 已随 `35114dd` 推送，Ubuntu 108／Windows 112 组总矩阵与完整 Windows Debug／Release 各 41 步通过；独立审查与 net48 编译通过，新增共享读取／只读语义两组和链接实际运行 | 本批完成；cache 回调异常仍是写入完成后抛错，不新增回滚或多线程竞态保证 |
| C004c | 已随 `1c1841f` 推送，Linux／Ubuntu 135、Windows 139 组及完整 Windows Debug／Release 各 41 步通过；原 VB 类型／六字段／Tracked 实测通过，独立生产／测试／PS 审查和 net48 编译通过 | 本批及 C004 核心验收完成；枚举外层异常位置由源码复核，权限未知和真实 Minecraft／多线程竞态不在本批实测范围，最后适配门面仍纳入 R001 |
| C005a | 已随 `a726586` 推送；Ubuntu 156／Windows 162 组与完整 Windows Debug／Release 各 41 步通过，same-value backup 原消费者断言、独立终审和 net48 编译通过 | 本批完成；不声称真实多线程竞态或游戏运行已实测，页面与读取后继继续 C005b／c |
| C005b | 已随 `33a1c6a` 推送，Linux／Ubuntu 189、Windows 194 组及完整 Windows 各 41 步成功；原 VB 型／七字段／31 默认／实际 Meloong 标题和原消费者通过，独立终审及 net48 编译通过 | 本批完成；一项仅大小写不同文件 fixture 仅Linux执行，不声称所有文件系统一致或实际游戏／目录权限故障已验 |
| C005c1 | View／结果／显式 Block 已真实接入原页面，21 组生产 DTO 金值加入 Main，Linux 210／210 组通过；net48 PCLCS 与 VB 泛型索引桥零警告／错误，独立生产／golden 终审通过。页面 BOM 与 RenderList／DisplayKey 外代码逐文本未变 | 待完整 Windows Debug／Release、实际 KeybindingGroups／页面消费者及双平台新矩阵；控件／Tag／Margin／文案和数值文化仍由原页面执行，不动捕获／Loader／KeyboardName，不替代 c2 整页迁移 |

下次入口：核对分支、远端和 C005c1 检查点，先收尾其 Windows／双平台矩阵与真实 KeybindingGroups；代码及21组金值已接通，不重复提取。a／b 和 C004 均已通过。c1 通过后若 c2 仍缺 C027／独立 C029a，母项标 blocked 保留具体前置，继续最早 ready C006（C002／C003 已满足），不因整页暂阻而停止工程，也不将整页假标完成。

当前仍需完成：剩余产品源码迁移、现代项目和第三方／子模块兼容实现、迁移后完整 GUI／真实账号／游戏启动与旧更新桥验收。初始与 C001 检查点均有独立通过证据，后续任务仍须对各自提交重新验证；N001 审计交付不代表这些运行门禁已经通过。
