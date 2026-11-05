# PCLLF 迁移与 MMC 兼容工程

本工程从现有 PCL Living Fork（PCLLF）继续演进，以 PCL 原版公开源码为基础来源，以 PCL Community Edition（CE）为同源迁移参考。最终目标是完成产品源码的 C# 迁移、升级现代 .NET，并完善 MMC／MultiMC／Prism 整合包的导入与启动兼容，保留 LF 现有功能、用户数据和操作手感。

| 来源 | 在迁移工程中的作用 | 初始对照基线 |
| --- | --- | --- |
| PCLLF | 工程起点、开发主线和功能验收主体 | `main` 的 `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`，产品版本 1.3.4 |
| PCL 原版 | 基础源码、原有行为和上游变更的参照 | LF 声明的 PCL 2.14.0.0 基线 `5ef81e490ae46b1100d61e33d0183094c7a80ca8` |
| PCL CE | C# 转换、运行时适配、问题修复、测试和独立组件的参考及复用来源 | 稳定版 `v2.15.1`，提交 `1a7653127d578efdb2a9388999a3fdc67d9e49d5` |

迁移分支为 `migration/csharp-modern-dotnet`。工程继承 LF 的仓库历史、产品标识、功能约定和用户数据兼容要求。CE 的实现按模块选择和适配，每次引入记录对应提交、文件、署名、许可及 LF 的修改；引用开发分支成果时同样固定提交。

迁移目标平台为 C#、.NET 10 和 WPF。语言转换、运行时升级、结构调整及兼容增强分别形成可对照的改动。net48 可以作为阶段性验证环境，保留范围由实际依赖和构建结果确定。

当前长程任务执行下列第 1 至 4 阶段及其必要的等价回归。新增 MMC／Prism／GTNH 兼容规则、性能优化和 Rust／C++ 扩展属于后续阶段；现有 MMC 行为在本轮保持并回归。自主提交、PR 审查与合并规则见 [任务目标](MIGRATION_GOAL.md)，续接任务和验证记录见 [迁移进度](MIGRATION_STATUS.md)。所有实现与集成均限定在迁移分支。

LF 现有功能按以下范围纳入迁移和回归：

| 功能范围 | 当前主要实现 |
| --- | --- |
| 基础框架、界面和动画 | `Modules/Base`、`Controls`、`FormMain`、`Pages` |
| Java、账号、版本、游戏启动和崩溃分析 | `ModJava`、`ModMinecraft`、`ModLaunch`、`ModWatcher`、`ModCrash`、`PCLCS` |
| 在线资源搜索、下载与安装 | `Modules/Resource`、`ModDownload`、`Pages/PageDownload` |
| 本地资源、回收站、地图安装和资源更新 | `InstanceResourceStore`、`ResourceRecycleStore`、`MapDownloadInstaller`、`InstanceResourceUpdates` |
| 整合包导入、导出、更新和整合包模式 | `ModModpack`、`ModpackUpdate`、`ModpackUpdateFlow`、`ExportedVersionIsolation`、`ModModpackMode` |
| 存档备份、保留策略和环境还原点 | `WorldBackups`、`EnvironmentHistory`、`WorldMetadata` |
| 个人习惯、按键和最近世界 | `PersonalGameSettings`、`InstanceKeybindings`、`QuickPlayWorlds` |
| 模组关系、依赖检查和启动前补全 | `ModDependencyInspector`、`ModDependencyOnline`、`ModDependencyAutoFill`、`LaunchDependencyCheck` 及对应控件 |
| 测试副本、来源分组和环境变化对照 | `InstanceCloner`、`InstanceCloneGroups`、`RecentEnvironmentChanges` |
| 配置兼容、启动器维护、工具和发布 | `Settings.*`、单实例与窗口唤醒、自更新、更新桥、工具箱、`scripts` 和 Actions |

实施顺序如下：

1. 建立 LF／PCL／CE 模块映射，保存 Windows 构建、回归结果、典型交互和性能基线，明确各模块的行为约束与依赖。
2. 整理实例、组件、依赖、启动计划和文件变更计划等模型，将纯业务规则与 WPF、网络、存储、Windows 和 Java 进程适配分离。
3. 按完整功能链推进 C# 迁移，同步处理调用入口和测试；高耦合的主窗口、设置、加载器和启动生命周期成组适配。现有 PCLCS 与 MeloongCore 按实际职责继续使用。
4. 完成 LF 自有工程的现代 .NET 构建、资源与依赖加载、账号设置兼容、发布和旧版更新桥验证。
5. 结合 CE 的适用实现完善 MMC 兼容，验证从整合包解析、文件安装到 Java 选择、启动和退出的完整流程。
6. 根据基准测量优化缓存、扫描、后台任务和文件处理；对确有收益的独立模块评估原生库或 Rust／C++ 实现，并继续使用必要的 Java 辅助组件。

完整迁移按以下结果验收：

- 产品中的 VB 业务实现、控件和页面后台完成 C# 替换，生成代码与构建链同步调整；源码清单逐项记录迁移结果，覆盖现有功能与可选功能的处置。
- 主程序及实际依赖在现代 .NET 上完成正式构建、发布和运行验证。
- LF 的账号、设置、游戏目录、备份、还原点、个人修改和更新通道保持兼容。
- 按钮、滚动、页面加载及动画中断和叠加行为完成新旧对照验证。
- 文件操作在取消或失败后等待实际负载、提交或回滚收尾，再释放版本占用；中断恢复通过临时数据和故障场景验证。
- 迁移前后的启动、扫描、安装、备份和后台资源占用具有可比较的测量记录。

MMC 兼容按明确的格式能力和样本矩阵验收，覆盖：

- `mmc-pack.json`、`instance.cfg`、归档根目录和游戏目录布局，以及缺失可选配置时的处理。
- 组件引用、补丁应用顺序、依赖与冲突、主类、游戏参数、JVM 参数和 Tweaker 等启动语义。
- 库列表的合并与替换、Maven 坐标、下载地址、本地库、平台规则、原生库和 classpath 顺序。
- Java 兼容主版本集合及其交集、组件明确要求与原版默认推断之间的处理。
- LWJGL2／LWJGL3 和替换引导组件等场景，包括固定版本的 GTNH 现代 Java 整合包。
- 未实现的必要组件或补丁能力有明确诊断，避免生成缺失关键启动信息的安装结果。
- 普通 Java 8 实例、常见加载器实例、含自定义补丁的 MMC／Prism 实例与 GTNH 样本完成端到端验证，并回归 LF 原有导入、更新、备份和还原流程。

样本记录整合包版本与摘要、Java 版本、操作系统、预期库列表和参数，以及导入、启动、退出和恢复结果。启动器效率与游戏运行性能分别测量，性能收益以实际结果说明。

本说明固定整体工程范围与验收目标；当前长程任务的完成情况以 [迁移进度](MIGRATION_STATUS.md) 中的实际构建、测试与运行记录为准。

来源与版权说明继续见 [NOTICE.md](../NOTICE.md) 和 [LICENCE](../LICENCE)。CE 初始参考版本见 [v2.15.1](https://github.com/PCL-Community/PCL-CE/releases/tag/v2.15.1)。
