# 迁移状态与续跑入口

目标见 [MIGRATION_GOAL.md](MIGRATION_GOAL.md)，完整背景见 [MIGRATION.md](MIGRATION.md)，模块对应关系见 [MODULE_MAP.md](migration/MODULE_MAP.md)。本文件是持续任务的进度账本；每次执行后在原条目上更新，不重复建立迁移计划。

## 当前状态

- 目标状态：`in_progress`。当前仅推进既有 LF 的 C#／.NET 10／WPF 与等价回归。
- 集成分支：`migration/csharp-modern-dotnet`。工作起点为 `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`；`main` 不参与提交或合并。
- 最新迁移检查点 `348b0a516b2395d1559135cf0a0e9fb36bdec270`：Ubuntu 308／308、Windows 336／336；137-case 独立 CLR4 对照和 52 项原 String→Win32 候选实跑，四个失败数组均空、`equivalent=true`。原 8＋2 与后续 9 项差异已消除，见 [实际报告摘要](migration/RESOURCE_PATH_ORACLE.md)；有限样本不是全部 BCL／现代 WPF 验收，主产品仍 net48。
- C007b2 的 CI 与脚本默认切换 RequireEquivalent，等待严格模式提交的真实执行及完整消费者通过后关闭。本批不再扩面原始 BCL 输入，短名时序限制明确保留；同时准备独立 C007c1 标题／目录映射。C005c2／C006d 的界面母项保持 blocked。
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
| C005c1 当前清单 | `--ref 2f60f73afb085912708789551923d3de988f1751`：267 个源文件，其中产品 257、测试 9、工具 1；产品 VB 仍为 168 文件／55,371 行 | 仅以提交树核实当前范围；页面其余后台尚未迁移，不据总行数或 C# 增量计算完成率 |
| C006c 当前清单 | `--ref 6023f3dc3c0c544a8d7c12d189d676e90fb3377e`：273 个源文件，其中产品 260、测试 12、工具 1；产品 VB 仍为 168 文件／55,284 行 | 固定初始清单不改写；适配门面、控件和主工程仍是待迁产品源码，不据本表计算完成率 |
| C007a 当前清单 | `--ref 459bb0a8f8b1112187e884f1a9985014e58dc970`：275 个源文件，其中产品 261、测试 13、工具 1；产品 VB 仍为 168 文件／55,270 行 | 八数据字段和一个校验核心已迁，Icon／全局路径／Store 其余及主工程仍待；不是完成率 |
| C007b2 首批当前清单 | `--ref 8c64fcc52f6468ea254f73f8368d81b39f428538`：283 个源文件，其中产品 264、测试 18、工具 1；产品 VB 仍为 168 文件／55,263 行 | 总 VB 169 包括测试专用历史驱动 1 个；不算产品迁移增减，固定基线不改写，非完成率 |

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
| `2f60f73` | C005c1 原页面 DisplayKey／RenderList 的显示、搜索、计数与显式分组规则进入 `InstanceKeybindingView`，21 组金值验证精确比较、全局总数、原对象及输入不变；其余页面后台、控件、捕获与动画保留 |
| `8a5e707` | 关闭 C005c1 的真实 Windows 与矩阵证据，母项因整页前置 blocked；固定当前清单并拆出 C006a／b／c／d，长期目标仍在进行 |
| `046ed2d` | C006a 两路径／参数叶子进入 `QuickPlayWorldRules`，16 组跨平台／另 1 组 Windows 生产样本；窄补现代 Windows 原 ArgumentException 顺序，增强真实 Prepare 参数与 CLR4 路径 oracle，不改版本／日志／会话 |
| `f1fa26b` | C006b 历史／日志与双 DTO 进入 `QuickPlayHistory`，原四入口和派生旧类型接通；26 组真实文件样本、原 40／5 限额／两次读取／捕获边界、UTF16 BOM、mtime 完整性及 Windows 4＋5 字段／ID／UTC 消费者验证 |
| `257e98e` | C006c 原七入口进入 `QuickPlayFlow`／8 惰性 getter，31 组真实进程／文件／gzip／NBT／异常边界样本；Windows 脚本验证 live／dead／disabled probe、坏 JSON finally 和坏 gzip 原异常／字节，旧 Start／UI 回调保留 |
| `6023f3d` | 仅修正 Windows PrepareIdentity 金值的原内部斜杠写法并记录初次失败；生产与脚本树对 `257e98e` 无差异，保精确字符串断言；重跑双平台全通过 |
| `c3271f5` | 固定 C006 核心三批完整证据、保留初次失败与修正，母项待 WPF；C021 使用 C006c 核心依赖，开始 C007a／b 拆分 |
| `459bb0a` | C007a 原八资源字段进入纯 DTO，原 sealed VB 类／Icon 保留，ValidateEntry 接通原消费者；13＋1 平台样本与真实 Scan 继承字段／冻结 Icon、资源操作回归通过 |
| `028b9ed` | 关闭 C007a 的完整证据／清单，进入固定旧路径契约与真实 Windows oracle 审计 |
| `b12b6cf` | C007b1 路径两方法原控制流迁 C#，12 新组、独立固定原 VB／CLR4 与真实 .NET 10 对照及 Diagnostic CI；实际测出 8 路径＋2 Unicode 差异，母项保持未等价，见独立报告摘要 |
| `8c64fcc` | C007b2 首批路径／旧 Ordinal helper、16 Windows／8 Linux 新组与 oracle v2 已推送；实际原 8＋2 差异消除、52 Win32 候选通过，新增 9 路径边界仍待后继适配，不据 Diagnostic 关闭母项 |
| `348b0a5` | C007b2 后继设备根／UNC／leading-space 系统归约、GetDir limited 长度及原串错误顺序已推送；5 新 Windows 组与 137／52 对照全通过，新旧差异全部归零；后继启用严格 CI，不以本次 Diagnostic 模式代替门禁实跑 |
| [C007b2 首批 Windows Build 41736377697](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41736377697) | 提交 `8c64fcc`：Debug job `126266518133`、Release job `126266518659` 各 41 步 success；原资源／备份还原／更新／取消恢复、账号合成迁移、旧更新桥及发布产物检查通过，仍是旧 net48 产品，非现代 WPF 运行证据 |
| [C007b2 首批 Migration validation 41736377415](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41736377415) | Ubuntu job `126266516885` 308／308、Windows job `126266516510` 331／331，均 success；124-case 独立 CLR4／现代对照、52 项候选实跑，基线／候选失败 0、文本差异 0、新路径差异 9。Diagnostic 成功保留残差，不代表 RequireEquivalent，通过报告与 artifact 摘要可续接 |
| [C007b2 后继 Migration validation 41737707302](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41737707302) | Ubuntu job `126271080274` 308／308、Windows job `126271080675` 336／336，均 success；137-case／52 候选真实实跑，基线／候选／path／text 失败均 0、equivalent=true。新增长度、NT 父 NUL、canonical 根旧 NRE 均由真实 CLR4 证实；artifact `13280505603` 及哈希见摘要 |
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
| [C005c1 Windows Build 41717722719](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41717722719) | 提交 `2f60f73afb085912708789551923d3de988f1751`：Debug job `126202044008`、Release job `126202043635` 各 41 步 success；真实 KeybindingGroups 页面消费者、个人习惯／按键／Quick Play、独立备份引擎、凭据保护及旧更新桥通过 |
| [C005c1 Migration validation 41717722638](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41717722638) | Ubuntu job `126202044078` 210／210、Windows job `126202044337` 215／215 组通过，固定清单一致；新增 21 组精确显示／搜索／计数／分组金值均运行，无 SKIP；不是整页 C# 转换或完整 GUI 实机验收 |
| [C006a Windows Build 41719762439](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41719762439) | 提交 `046ed2dd90427537e48012bbab537cc32e80d587`：Debug job `126209114550`、Release job `126209113916` 各 41 步 success；真实 CLR4 GetFileName oracle 遍历 41 字符通过，原 Prepare 精确参数、缺失世界、个人习惯／日志／按键及旧更新桥等回归通过 |
| [C006a Migration validation 41719762399](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41719762399) | Ubuntu job `126209113899` 226／226、Windows job `126209114524` 232／232 组通过，固定清单一致；Windows-only 旧路径类型／顺序／device／显式开关样本实际通过，无 SKIP |
| [C006b Windows Build 41721885199](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41721885199) | 提交 `f1fa26b3705a002c237fba190c4c01bfe84a7442`：Debug job `126216509011`、Release job `126216508717` 各 41 步 success；实际旧 VB 会话／最近世界类型、4＋5 继承字段、GUID-N 和 Mojang ISO 日志 UTC 断言、Quick Play／个人习惯／按键／旧更新桥等完整回归通过 |
| [C006b Migration validation 41721885208](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41721885208) | Ubuntu job `126216508313` 252／252、Windows job `126216508853` 258／258 组通过，固定清单一致；26 组新历史／日志／JSON／锁样本含真 UTF16LE BOM 和逐项 mtime 完整性均实际运行，无 SKIP |
| [C006c 初次 Migration validation 41724453187](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41724453187) | 提交 `257e98ea14e2cd93d2c9a906c69c62d9709ad2d3`：Ubuntu job `126225449726` success，Windows job `126225449293` 为 288／289；唯一失败为 PrepareIdentity 金值将原 Address 的内部 `/` 规范化为 `\`。生产保留 LF 原相对名称是正确行为；只修正测试为原两参数 Combine 形状，随后重跑，不将此初次运行写为通过 |
| [C006c Windows Build 41724453366](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41724453366) | 生产提交 `257e98e`：Debug job `126225450403`、Release job `126225449900` 各 41 步 success；新流程原入口、继承字段、Mojang UTC、真实进程 live／dead 恢复、禁用下状态探测、精确坏 JSON finally、坏 gzip InvalidData／字节、原近期卡片动画 ticks／顺序及旧更新桥通过。卡片脚本没有启动应用窗口或 Minecraft，不替代 R003 |
| [C006c 修正后 Migration validation 41724887383](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41724887383) | 提交 `6023f3d`：Ubuntu job `126226953558` 283／283、Windows job `126226953958` 289／289 组通过，固定清单一致；原 raw path 精确金值和有效 NBT 只读／字节样本通过，无 SKIP。`git diff 257e98e 6023f3d -- PCLCS "Plain Craft Launcher 2" scripts` 为空，Windows 生产证据适用于同一代码树 |
| [C007a Windows Build 41726305503](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41726305503) | 提交 `459bb0a`：Debug job `126231854074`、Release job `126231854550` 各 41 步 success；真实 Scan 原 sealed VB 类／基类八字段／本类 ImageSource 与冻结图标、五资源页、Rename／Export／回收／更新、取消／陈旧保护及旧更新桥等完整回归通过，源图标加载仍在原 VB |
| [C007a Migration validation 41726305734](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41726305734) | Ubuntu job `126231855656` 296／296、Windows job `126231856008` 303／303 组通过，固定清单一致；新增 13 跨平台＋1 Windows 样本含真实只读／payload 独占句柄、原目录内容变化规则，Windows-only 实际通过；不声称并发竞态或完整现代 WPF 验收 |
| [C007b1 Windows Build 41730760410](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41730760410) | 提交 `b12b6cf`：Debug job `126247143719`、Release job `126247144148` 各 41 步 success；原资源／更新／备份／克隆／取消恢复及旧更新桥等真实消费者通过。原 SDK9／VS Build 工作流未修改，产品路径实现仍运行 Framework API |
| [C007b1 Migration validation 41730760349](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41730760349) | Ubuntu job `126247143804` 300／300、Windows job `126247144983` 315／315 单元组通过；Windows 独立 CLR4 v4.8 与 .NET 10.0.14 的 75-case／5 真实链接 oracle 成功运行，基线契约失败 0、路径差异 8、Unicode 差异 2，**Diagnostic 不是等价通过**。artifact `13276625509` 与全部差异／环境／下一步见 [摘要](migration/RESOURCE_PATH_ORACLE.md) |

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
| `InstanceResourcePathCompatibility.cs`、其同名测试 | 路径适配子代理 | 只拥有两新文件，保 NETFRAMEWORK／非 Win 原 BCL；其余 Core／VB／oracle 不改，父统一接线 |
| `InstanceResourcePathText.cs`、其测试、既有 oracle host／driver／脚本 | ordinal／oracle 子代理 | 局部 Win32 比较与新旧探针、扩展路径样本独占；Text 生产已冻结交父，父小修原 StartsWith 空值参数名为 value；路径 API 文件不改 |
| Core 三比较／两路径 hook、主测试链接、工作流／文档／所有提交 | 主执行者 | 不覆盖两代理未交付文件；文本／接线独立审查通过，完整 helper／新 oracle 仍未验证 |

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
| C005 | blocked | C004、C027、C029a | a／b／c1 已通过全部真实门禁；c2 整页仍因 C027／C029a 阻塞，单独后端或呈现规则不关闭母项 |
| C006 | blocked | C006c、C027、C029a | a／b／c 已全通过；d 首页入口／动画／启动宿主等待可引用的 C# WPF 前置，不能以核心代替原整项验收 |
| C007 | in_progress | B001 | 从 a 八字段模型／ValidateEntry 开始迁移本地资源路径和 `InstanceResourceStore`；全局 Normalize／EnsureWithin另批审计，现有资源种类、隔离目录、边界和列表行为均需回归 |
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
| C021 | todo | C004、C006c、C010、C015、C017 | 迁移 `ModLaunch`／`ModWatcher`／`ModCrash` 的生命周期与适配；Quick Play 所需核心已由 C006c提供，页面剩余归 C006d，不用 UI 母项阻塞核心；按预检、进程、监控拆子任务，启动、退出和崩溃对照回归 |
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
| C005c1 | done | C005b | `2f60f73`：View 已接通原两方法，21 组新金值令 Ubuntu 210／Windows 215 组总矩阵通过；net48 编译／VB 索引桥／独立终审、Windows 各 41 步及真实 KeybindingGroups 均通过 |
| C005c2 | blocked | C005c1、C027、C029a | 整页 C# 后台／XAML 与 Loader／捕获／订阅／动画／确认接线；当前主项目只编译 VB partial、PCLCS 不能反向引用 main，自定义控件和路由仍在 main。待单向呈现库／host 可编译后迁入页面并去除旧 VB 后台，不以未编译文件占位完成 |

页面前置以实际项目为证：`PCL-Living-Fork.vbproj` 显式编译旧后台及 XAML，主程序已经引用 PCLCS；页面继承主程序集的 `MyPageRight`，还依赖同程序集的自定义控件、`LocalResourceUi`、Loader、动画、实例与 `FormMain`。因此 `C029a` 是独立前置子任务，不继承 C029 的汇总依赖；`C005c2→C029a→C027` 与 `C029→C005c2` 无环。不得把 `C005c2` 依赖改成整个 C029／N003，否则会经 C031 回到自身。c2 阻塞期间母项不能标 done，按 goal 继续 C006 等最早 ready 核心任务；这不是需要用户处理的持久阻塞。

C006 按实际行为拆分；日志／历史与会话的清理顺序不同于 C004，不能直接套用个人习惯流程：

| 子任务 | 状态 | 依赖 | 实现与验收边界 |
| --- | --- | --- | --- |
| C006a | done | C002、C003 | `046ed2d`：原两个入口、16 组跨平台／另 1 组仅 Windows 生产样本；Ubuntu 226／Windows 232 组总矩阵、net48 编译、独立终审、Windows 各 41 步与实际 41 字符 CLR4 oracle 全通过；现代 Windows 窄补边界见兼容审计 |
| C006b | done | C006a | `f1fa26b`：History／双 DTO／原四入口及 26 新组，Ubuntu 252／Windows 258 组矩阵、net48／测试零警告编译、独立生产／测试终审、Windows 各 41 步与实际旧类型／字段／UTC 消费者全通过 |
| C006c | done | C006b | `257e98e`／纯测试修正 `6023f3d`：原七入口、8 惰性 getter／双 factory 和 31 新组已接通，Ubuntu 283／Windows 289 组、Windows 各 41 步／原流程／卡片消费者、独立终审与零警告编译全通过；初次矩阵失败及修正已保留证据 |
| C006d | blocked | C006c、C027、C029a | 最近世界页面／卡片 C# 后台与原动画／启动交接；等待可引用的控件／宿主，不能以核心转换代替真实页面迁移 |

C006 的启动生命周期依赖明确落到已完成 C006c，C021 不再等待 UI 母项；原 Start／OnEnabledChanged 与近期卡片后台的最终迁移仍归 C006d／C023。母项 blocked 仅指尚未转换的真实界面前置，不是需要用户处理的持久阻塞。

C007 当前只拆已勘查的实际入口，后续导入／导出／扫描／图片在执行前按生产依赖继续拆分，不以模型完成关闭整个 Store：

| 子任务 | 状态 | 依赖 | 实现与验收边界 |
| --- | --- | --- | --- |
| C007a | done | B001 | `459bb0a`：原八字段／Validator／消费者、13 跨平台＋1 Windows 组已接通，Ubuntu 296／Windows 303 组、Windows 各 41 步与实际 sealed／8 字段／冻结 Icon 和资源链、独立终审及零警告编译全通过 |
| C007b | in_progress | C007a | 固定源审计已给出边界；按 b1 原控制流＋独立诊断 oracle、b2 有证据的现代适配分开，最终必须关闭现代真实差异，不能以 diagnostic 成功代替等价 |
| C007b1 | done | C007a | `b12b6cf`：原 Core／VB 真委托／12 新组、原消费者 Windows 各 41 步和 Ubuntu 300／Windows 315 组通过；独立 CLR4 与现代 oracle 实际运行并保留 10 差异。done 仅指本批控制流／诊断交付，母项未等价 |
| C007b2 | in_progress | C007b1 | `348b0a5` 实际验证 137 例／52 候选全等价，308／336 单元通过；CI 与脚本默认转 RequireEquivalent，待严格提交／完整消费者结果。只关闭资源路径实际生产边界，不扩大 root／link 信任或泛称全部 BCL 等价 |
| C007c1 | in_progress | C007a | 独立标题／目录 5 项映射，保原 VB enum／Int32／CInt 桥／Root Combine 与无效值 IndexOutOfRange；三组生产规则及实际 VB enum／原入口／Root 错误顺序回归准备中，不等待路径门禁重复实现 |

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
| C005c1 | 已随 `2f60f73` 推送；Ubuntu 210／Windows 215 组、完整 Windows Debug／Release 各 41 步与实际 KeybindingGroups 通过；net48 PCLCS／VB 泛型桥零警告／错误，独立终审通过，页面 BOM 与其余后台逐文本未变 | 本批完成；控件／Tag／Margin／文案和数值文化仍由原页面执行，不动捕获／Loader／KeyboardName，不替代 c2 整页迁移 |
| C006a | 已随 `046ed2d` 推送；Ubuntu 226／Windows 232 组、完整 Windows Debug／Release 各 41 步、原 Prepare 和 41 字符 CLR4 oracle 通过；net48 PCLCS 零警告／错误、独立生产／测试／PS 终审通过 | 本批完成。不改变支持版本／日志／会话／首页；现代只保留 BCL 异常类型／顺序，不复制系统所有本地化文案 |
| C006b | 已随 `f1fa26b` 推送；Ubuntu 252／Windows 258 组、Windows Debug／Release 各 41 步与实际旧型／4＋5 字段／GUID-N／UTC 消费者全通过；生产／测试独立终审、net48／测试零警告编译通过，非本批方法逐字不变 | 本批完成。Root 每 Address 仍读两次；fresh 样本的 read/read/write 为 Address 回调计数，不冒称 Root 只调用三次。不是多线程竞态或真实 Minecraft 验证 |
| C006c | 已随 `257e98e`／`6023f3d` 推送，Ubuntu 283／Windows 289 组、Windows Debug／Release 各 41 步及真实新流程／旧卡片消费者通过；有效 NBT 只读／字节、原坏 gzip、精确坏 JSON finally 都有实际样本 | 本批完成，C006d／母项仍等待 WPF；未知进程权限与枚举器失败位置仅原契约／源码审查，不伪称竞态或实游戏验收 |
| C007a | 已随 `459bb0a` 推送；Ubuntu 296／Windows 303 组、Windows 各 41 步与原类型／继承字段／Icon 和完整资源消费者通过，独立终审／零警告编译通过；其余 Store／路径逐字未改，脚本 BOM 保留 | 本批完成，不新增 hash／类型检查或文件并发保证；全局路径与其余 Store 继续 C007b 及后续实际拆分 |
| C007b1 | 已随 `b12b6cf` 推送；Windows 各 41 步、Ubuntu 300／Windows 315 单元组和独立 CLR4／现代 75-case 对照运行成功，Diagnostic 明确输出 8＋2 差异，报告已从 artifact核实并摘要入库 | 本批完成，母项仍未等价。历史 LegacyDriver.vb 只用于原源注入的测试，不在产品编译链，不用于降低产品 VB 统计 |
| C007b2 | `348b0a5` 已推送；Ubuntu 308／Windows 336 组与真实 137 例／52 候选全通过，四失败数组均空。独立终审与三个工程零警告编译通过，真实 CLR4 确认 limited 长度、NT 父 NUL／canonical 旧 NRE | 等严格提交 RequireEquivalent 与完整消费者结果后关闭；GetDir 仅服务 Normalize 后祖先遍历，短名时序与全部 raw BCL 输入不作完成承诺。测试专用历史 VB 不入产品，net48 实构建不冒称现代 WPF |
| C007c1 | 已划定两生产方法及真实 enum／CInt 桥，原 Root 和 SafeName 等其余方法不改；独立文件交付后统一编译／审查 | 待提交、Windows 原消费者与双平台检查，不能以字符串映射完成关闭整个 Store |

下次入口：核对 C007b2 严格提交的 RequireEquivalent／完整消费者结果；137／52 已真实零差异，不重复原 8＋2／9 修复或重建基线。然后收尾独立 C007c1，并按 Store 下一实际方法拆 C007c2。C007a、C006a／b／c、C005c1 已全通过；C005／C006 的界面母项仍 blocked。

当前仍需完成：剩余产品源码迁移、现代项目和第三方／子模块兼容实现、迁移后完整 GUI／真实账号／游戏启动与旧更新桥验收。初始与 C001 检查点均有独立通过证据，后续任务仍须对各自提交重新验证；N001 审计交付不代表这些运行门禁已经通过。
