# 迁移状态与续跑入口

目标见 [MIGRATION_GOAL.md](MIGRATION_GOAL.md)，完整背景见 [MIGRATION.md](MIGRATION.md)，模块对应关系见 [MODULE_MAP.md](migration/MODULE_MAP.md)。本文件是持续任务的进度账本；每次执行后在原条目上更新，不重复建立迁移计划。

## 当前状态

- 目标状态：`in_progress`。当前仅推进既有 LF 的 C#／.NET 10／WPF 与等价回归。
- 集成分支：`migration/csharp-modern-dotnet`。工作起点为 `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`；`main` 不参与提交或合并。
- 初始 Windows 基线通过；首批提交 `d27f394580a83ea50077a96adda056c141dc8748` 的 Windows Debug／Release 完整自动化与双平台迁移矩阵也已实际通过。整项 .NET 10 产品迁移和实际交互验收仍在后续任务中。
- 首批并行工作：固定源码清单、模块映射、迁移专用 CI，以及 WorldMetadata 生产解析器的 C# 提取和 VB 适配接入。各批次结果以下方证据为准。
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
| 首批独立自审 | NBT 旧语义、PCL.WorldMetadata 16 字段／类型及 Read／ReadDetails／Describe 反射入口保持；所有字段映射完整，Describe 文案未变 | 源码逐段对照与独立复核；新提交完整 Windows Build 和迁移矩阵仍待推送后运行 |

本轮已推送的实现与验证检查点：

| 检查点 | 修改及实际验证 |
| --- | --- |
| `c3a2838` | 固定 goal、提交／PR 策略、LF／PCL／CE 模块映射、可重复源码清单和进度账本；后续规则按用户补充继续完善 |
| `e47fc55` | 修正 OptiFine 哈希资源磁盘路径大小写，保持 LogicalName；现有 net48 辅助库在 Linux 编译通过 |
| `bdad3a7` | 将 NBT 引擎迁入 `PCLCS.WorldMetadataReader`，保留 PCL.WorldMetadata 字段和生产入口；新增真实 NBT 测试 |
| `d27f394` | 新增限定迁移分支的 .NET 10／源码清单 CI；以下两次远端运行均针对该完整提交 |
| `c7cb287` | 固定详细 commit／PR 正文要求、模型边界和 .NET 10 兼容审计；现代运行门禁仍保留 |
| `0388bd6` | C002a 文件／gzip／详情文案迁入 C#；15 组真实文件样本加入既有 27 组测试；Windows 原详情脚本增强全部字段、精确格式、共享／只读及真实符号链接检查 |
| [Windows Build 41696728260](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260) | [Debug job 126129118107](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260/job/126129118107) 与 [Release job 126129118529](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260/job/126129118529) 各 40 步 success，包含原有自动化回归、凭据保护、旧更新桥和产物上传；主程序仍为 net48，不能据此关闭 N003 或 R003 |
| [Migration validation 41696728492](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728492) | Ubuntu job `126129118456`、Windows job `126129119218` 均 success：固定清单一致，.NET 10 真实生产解析器测试通过；这只验证已提取的解析器范围 |

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
| C002 | in_progress | B001、C001 | C002a：将文件／gzip／详情文案迁入 C#，VB 保留类型映射与路径／日志适配；接通原详情页、最近世界和备份消费者，新增真实文件样本并运行原 Windows 回归。最终删除 VB 门面随消费者和主工程转换验收 |
| C003 | in_progress | B001 | 独立子代理负责 `LocalGameDataStore.cs`、原 VB 门面与独立测试文件；主执行者集成项目／脚本。迁移持久化与原子写入，保留旧数据、并发变更、临时清理和失败恢复语义 |
| C004 | todo | C003 | 迁移 `PersonalGameSettings` 规则及生产调用；字段白名单、双向同步、换行、备份、旧键值与进程恢复回归 |
| C005 | todo | C004 | 迁移 `InstanceKeybindings` 与其页面功能链；模组归属、冲突分组、重新绑定和陈旧写入保护回归 |
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
| C029 | todo | C005、C008、C010、C011、C012、C016、C020、C027 | 完成实例页面和资源页面的 C# 后台与 XAML 调用，既有功能链全回归 |
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

## 当前批次与下一步

| 批次 | 当前进度 | 仍需完成 |
| --- | --- | --- |
| G001 | 已随 `c3a2838` 推送，双平台 CI 固定清单重现一致 | 当前固定基线不随后续迁移改写；需要时为新提交生成独立当前清单 |
| G002 | 模块映射完成：14 条功能链、固定源码清单全部 249 文件归组；53 个链接目标、46 个脚本名核实 | 已完成映射交付，后续按实际迁移更新对应行，不代表功能已迁移 |
| G004 | 已随 `d27f394` 推送，Ubuntu／Windows 迁移矩阵通过 | 后续生产改动继续使用该工作流，并按实际依赖扩展测试 |
| C001 | C# 解析器、原 VB 适配和 27 组测试已提交；独立自审、net48 辅助库编译、完整 Windows Debug／Release 及双平台迁移矩阵通过 | 本项完成。文件／gzip／格式与消费者的进一步 C# 迁移属于 C002，VB 兼容外壳仍纳入最终源码清单 |
| C002a | 文件／gzip 与原中文详情文案进入 `WorldMetadataFiles`，原 VB 仅保留 16 字段映射和路径／日志委托；三个原消费者继续调用同一入口。新增 15 组真实文件／完整文案样本，Linux .NET 10 合计 42／42 通过，net48 PCLCS 编译零警告／错误；独立源码审查通过 | 原 Windows `Test-WorldDetails.ps1` 已增强全部字段、精确 CRLF、基本／详情区别、共享／只读、真实文件链接检查及句柄释放，待推送 CI 实际验证。尚未转换详情页／Quick Play／备份 UI 后台，不据此关闭 C006／C010／C029 或整项迁移 |
| C003a | `LocalGameDataStore` 与 VB 转发已接通，测试项目链接真实生产源码及 LF 原 Newtonsoft DLL；Linux .NET 10 共 62／62 组通过，其中本机资料 20 组。独立源码复核通过；net48 辅助库编译零警告／错误 | Windows 增加共享锁／只读两组，链接权限不足会显式 SKIP，必须按实际输出记录；原完整回归及 EngineOnly 夹具待 CI。陈旧摘要测试是顺序模拟外部编辑，权限不明进程分支只有源码核对，不声称多线程竞争／真实进程权限错误已实测 |

下次入口：核对迁移分支、远端和正在运行的 CI，先收尾 C002a／C003a 的 Windows 与双平台结果，按实际失败修复并记录；两批生产实现已接通，不重复迁移 NBT／文件／文案或本机资料核心。原消费者仅保留适配门面，页面／Quick Play／备份后台和最终 VB 门面删除分别由 C006／C010／C029／C031、R001 承接。C002／C003 相关门禁通过后从 C004 的 PersonalGameSettings 继续，先参考 MODULE_MAP 及兼容审计；每批保留实际命令、结果和详细提交正文。

当前仍需完成：剩余产品源码迁移、现代项目和第三方／子模块兼容实现、迁移后完整 GUI／真实账号／游戏启动与旧更新桥验收。初始与 C001 检查点均有独立通过证据，后续任务仍须对各自提交重新验证；N001 审计交付不代表这些运行门禁已经通过。
