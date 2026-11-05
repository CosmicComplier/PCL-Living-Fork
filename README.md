# PCL Living Fork

**PCL Living Fork**（原 LivingPCL）是基于 [Plain Craft Launcher（PCL）](https://github.com/Meloong-Git/PCL) 原项目的第三方 Minecraft 启动器，由 DDZmumo 维护，非 PCL 官方版本。

[下载 1.3.4](https://github.com/DDZmumo/PCL-Living-Fork/releases/tag/v1.3.4) · [更新说明](https://github.com/DDZmumo/PCL-Living-Fork/releases/tag/v1.3.4) · [问题反馈](https://github.com/DDZmumo/PCL-Living-Fork/issues)

## 功能

- **游戏资源管理**：在启动器内管理存档、截图、模组、资源包、光影包和投影/原理图，使用 PCL 原生列表、多选与动效。资源回收站支持还原和彻底删除；资源包和光影包支持在线更新，CurseForge 地图下载后自动解压安装。
- **个人游戏习惯**：按类别同步按键、鼠标、视野、字幕等设置，每个版本可单独例外；按键管理支持搜索、同键占用检查和重新绑定。
- **继续上次的世界**：首页和存档管理可直接进入支持 Quick Play 的 Java 版存档，首页卡片可在“个性化 → 主页”关闭。
- **整合包更新**：从本地文件或 CurseForge / Modrinth 更新已识别的整合包，按正式版、测试版选择版本；预览按新增、更新、移除和保留个人修改分类，下载时同步实际进度。取消后会等待文件处理和必要的回滚结束，再解除版本占用。
- **还原点**：手动或在更新前保存 Minecraft、加载器、模组与配置，支持整套恢复、删除和恢复前留档，保留当前存档进度。可选开启自动还原点数量上限，手动还原点不计入，也不会自动清理。
- **存档备份**：正常退出后备份世界，更新 Minecraft 前额外备份；在存档下展开查看、恢复和删除备份，并设置自动备份保留份数。手动备份单独保留，不计入自动清理上限。
- **模组依赖检查**：用带圆角图标的关系图查看平台声明的前置、被依赖关系与冲突；网络超时或未查到精确平台声明时，改以本地声明作为主要参考。未确认或已禁用的前置合并后用红框标注，删除、禁用的影响确认使用两列红框。启动检查保留本地可靠声明。
- **启动前依赖管理**：主页“上次游玩”上方可开关依赖检查和自动补全，首次使用默认勾选，之后保留玩家选择。必需前置先列出并确认，已禁用且兼容的文件可直接启用；缺少文件时通过原生下载任务补全，复查后启动。内嵌组件按所属 JAR 归组；依赖齐全时不弹补全提示，相同环境中已确认继续启动的问题在本次运行期间不重复询问。
- **测试副本与崩溃对照**：克隆当前版本进行测试，在版本选择中按来源折叠显示；崩溃时对照上次正常运行后的环境变化，并连接到还原点。
- **整合包导出与模式**：可附带启动器、选择该版本的版本隔离，并使用仅保留必要入口的整合包界面。

## 使用

已发布的旧版 1.3.4 使用 **Windows 64 位系统和 .NET Framework 4.8**，从 [Releases](https://github.com/DDZmumo/PCL-Living-Fork/releases) 下载后按该版本说明运行。本迁移分支构建的 C#/.NET 10 原生产品面向 Windows x64，包含运行时；迁移构建与产物角色见 [现代构建说明](docs/BUILDING_MODERN.md)。迁移分支尚未发布为正式版本。

升级时保留原有 `PCL`、`.minecraft` 和游戏目录。程序内更新保留原文件名。旧 `LivingPCL2-Re` 仓库继续提供兼容更新入口。

可与官方 PCL 共用兼容的账号和设置；同目录运行时共用 `PCL\Setup.ini`，修改共享配置也会影响另一边。保留固定默认配色，可在“设置 → 个性化 → 背景图片”切换原生白底与现有背景；自定义及其他颜色主题请[赞助原作者](https://meloong.com/afd/a/LTCat)，获取官方快照版。

地图 ZIP 默认保存到当前版本的 `saves\PCLLFWorldDownload`，文件夹不存在时自动创建；存档解压到 `saves`，保留原压缩包并避免覆盖同名存档。

**Minecraft API 与 CurseForge 第三方 API 均已获批，发行版使用本项目自有 Client ID 和正式 API Key。** 接口可用性以后续平台授权及服务政策为准。

## 自行编译

迁移分支使用 .NET 10 SDK 与 Windows Desktop/WPF 工具链；完整发布包中的旧版兼容桥还需要 Visual Studio/Build Tools 的 MSBuild 和 .NET Framework 4.8 编译工具。根 solution 与默认发布脚本均指向完整 C# 产品，详细工具链和定向检查见 [现代构建说明](docs/BUILDING_MODERN.md)。在 Windows PowerShell 中运行：

```powershell
git submodule update --init --recursive
dotnet restore .\PCL-Living-Fork.Modern.slnx
.\scripts\Build-Release.ps1
```

默认输出到 `artifacts/release/Release`，包含原生 EXE/ZIP、供旧客户端更新的托管兼容桥、SHA256 校验和及构建索引。各文件的角色由 `NativeRelease.Artifacts.json` 标明，首次安装使用原生 EXE/ZIP。源码不包含实际 Client ID 或 API Key；请通过 `PCL_MS_CLIENT_ID`、`PCL_CURSEFORGE_API_KEY` 环境变量提供自己的配置，构建时会加密嵌入。

## 迁移工程

迁移分支的目标、实施顺序和 MMC 兼容验收要求见 [迁移工程说明](docs/MIGRATION.md)。工程从现有 LF 继续演进，以 PCL 原版源码为基础来源，以 CE 为同源迁移参考。

## 国内发布同步

国内下载入口：[CNB Releases](https://cnb.cool/ddzmumo/PCL-Living-Fork/-/releases)。后续包含同步工作流的标签发布或编辑正式 Release 后，Actions 立即触发 CNB 同步并等待结果；CNB 每 30 分钟的定时检查作为补偿。

同步按版本、说明和附件内容指纹去重。已完成且内容相同时直接跳过，不启动 CNB 任务，也不重复上传附件。旧标签若不含该工作流，可用手动补同步。

同步仅包含版本说明、EXE、ZIP、校验文件及公开更新清单。CNB 的版本标签基于自身最小 `main` 分支创建，不镜像 GitHub 源码。手动补同步可在 Actions 中运行 `Sync release to CNB`，可指定版本标签。
## 来源与许可

原作作者为龙腾猫跃及 PCL 原项目贡献者，原作版权归成都瓜皮龙科技有限公司。基线为上游 2.14.0.0，保留原作与第三方组件的署名和许可。

- [上游 PCL](https://github.com/Meloong-Git/PCL)
- [来源与版权声明](NOTICE.md)
- [许可与使用指南](LICENCE)
- [支持原作者](https://meloong.com/afd/a/LTCat)
