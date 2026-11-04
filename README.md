# PCL Living Fork

**PCL Living Fork**（原 LivingPCL）是基于 [Plain Craft Launcher（PCL）](https://github.com/Meloong-Git/PCL) 原项目的第三方 Minecraft 启动器，由 DDZmumo 维护，非 PCL 官方版本。

[下载 1.3.4](https://github.com/DDZmumo/PCL-Living-Fork/releases/tag/v1.3.4) · [预发布 1.3.5-pre-2](https://github.com/DDZmumo/PCL-Living-Fork/releases/tag/v1.3.5-pre-2) · [更新说明](https://github.com/DDZmumo/PCL-Living-Fork/releases/tag/v1.3.5-pre-2) · [问题反馈](https://github.com/DDZmumo/PCL-Living-Fork/issues)

## 功能

- **游戏资源管理**：在启动器内管理存档、截图、模组、资源包、光影包和投影/原理图，使用 PCL 原生列表、多选与动效。资源回收站支持还原和彻底删除；资源包和光影包支持在线更新，CurseForge 地图下载后自动解压安装。
- **个人游戏习惯**：按类别同步按键、鼠标、视野、字幕等设置，每个版本可单独例外；按键管理支持搜索、同键占用检查和重新绑定。
- **继续上次的世界**：首页和存档管理可直接进入支持 Quick Play 的 Java 版存档，首页卡片可在“个性化 → 主页”关闭。
- **整合包更新**：从本地文件或 CurseForge / Modrinth 更新已识别的整合包，按正式版、测试版选择版本；预览按新增、更新、移除和保留个人修改分类，下载时同步实际进度。取消后会等待文件处理和必要的回滚结束，再解除版本占用。
- **还原点**：手动或在更新前保存 Minecraft、加载器、模组与配置，支持整套恢复、删除和恢复前留档，保留当前存档进度。可选开启自动还原点数量上限，手动还原点不计入，也不会自动清理。
- **存档备份**：正常退出后备份世界，更新 Minecraft 前额外备份；在存档下展开查看、恢复和删除备份，并设置自动备份保留份数。手动备份单独保留，不计入自动清理上限。
- **模组依赖检查**：用带圆角图标的关系图查看平台声明的前置、被依赖关系与冲突；网络超时或未查到精确平台声明时，改以本地声明作为主要参考。首次打开或检查后保存依赖树，模组未变化时直接读取；手动更新索引会分批获取全部模组的平台声明。同一已安装项目合并展示，平台指定其他版本时单独提示核对；未确认声明不会直接标为缺失。
- **加载器匹配**：识别 Fabric、Forge、NeoForge 及旧版 Forge 的实际加载声明，减少旧整合包中残留模板造成的误报。与实例加载器不匹配的启用模组在列表中标红，并给出处理建议；加载器内置组件归组展示。
- **启动前依赖管理**：主页“上次游玩”上方可开关依赖检查和自动补全，首次使用默认勾选，之后保留玩家选择。必需前置先列出并确认，已禁用且兼容的文件可直接启用；缺少文件时通过原生下载任务补全，复查后启动。内嵌组件按所属 JAR 归组；依赖齐全时不弹补全提示，相同环境中已确认继续启动的问题在本次运行期间不重复询问。
- **测试副本与崩溃对照**：克隆当前版本进行测试，在版本选择中按来源折叠显示；崩溃时对照上次正常运行后的环境变化，并连接到还原点。
- **整合包导出与模式**：可附带启动器、选择该版本的版本隔离，并使用仅保留必要入口的整合包界面。

## 使用

需要 **Windows 64 位系统和 .NET Framework 4.8**。从 [Releases](https://github.com/DDZmumo/PCL-Living-Fork/releases) 下载 EXE，或解压 ZIP 后运行 `PCL-Living-Fork-1.3.4.exe`。

升级时保留原有 `PCL`、`.minecraft` 和游戏目录。程序内更新保留原文件名。旧 `LivingPCL2-Re` 仓库继续提供兼容更新入口。

可与官方 PCL 共用兼容的账号和设置；同目录运行时共用 `PCL\Setup.ini`，修改共享配置也会影响另一边。保留默认强调色，支持浅色、深色外观，可在“设置 → 个性化 → 背景图片”切换随主题的纯色背景与现有背景；自定义及其他颜色主题请[赞助原作者](https://meloong.com/afd/a/LTCat)，获取官方快照版。

地图 ZIP 默认保存到当前版本的 `saves\PCLLFWorldDownload`，文件夹不存在时自动创建；存档解压到 `saves`，保留原压缩包并避免覆盖同名存档。

**Minecraft API 与 CurseForge 第三方 API 均已获批，发行版使用本项目自有 Client ID 和正式 API Key。** 接口可用性以后续平台授权及服务政策为准。

## 自行编译

需要 Visual Studio 2022 的“.NET 桌面开发”工作负载、.NET Framework 4.8 Targeting Pack 和 .NET 9 SDK。在 VS Developer PowerShell 中运行：

```powershell
git submodule update --init --recursive
dotnet restore .\PCLCS\PCLCS.csproj
.\scripts\Build-Release.ps1
```

输出文件按版本号命名，例如 `Plain Craft Launcher 2\bin\PCL-Living-Fork-1.3.5-pre-2.exe`。源码不包含实际 Client ID 或 API Key；请通过 `PCL_MS_CLIENT_ID`、`PCL_CURSEFORGE_API_KEY` 环境变量提供自己的配置，构建时会加密嵌入。

## 国内发布同步

国内下载入口：[CNB Releases](https://cnb.cool/ddzmumo/PCL-Living-Fork/-/releases)。后续包含同步工作流的标签发布或编辑正式版、预发布版 Release 后，Actions 立即触发 CNB 同步并等待结果；CNB 每 30 分钟的定时检查作为正式版补偿。

启动器更新默认使用 CNB，可在设置中保存 GitHub 偏好；更新提示中也可临时切换本次下载源。更新下载使用 PCL 原生任务界面。CNB 同步预发布附件并标记为预发布版，独立清单保存在 `updates` 分支的 `releases/<tag>.json`；正式版的 `latest.json` 不会被预发布版覆盖。

更新通道默认“仅接收正式发布版更新”。切换到“接收正式发布版和预发布版更新”时，确认警告后才保存。可选通道使用独立的 `latest-preview.json`，接收其中较新的正式版或预发布版，包含预发布序号与正式版本的正确排序。

在“设置 → 个性化 → 基础”可切换浅色、深色外观，即时生效并保留选择。深色配色覆盖顶栏、启动器卡片、菜单、输入与选择控件、滑块气泡、悬停提示和弹窗。内置角色背景随主题切换白天与夜晚图，夜间版使用不同动作；自定义背景保留并按主题调暗。游戏图标、皮肤和截图保留原始颜色。

“窗口不透明度”与“面板不透明度”分别控制整体窗口和侧栏、卡片底色；背景图片沿用独立的不透明度设置。调整面板不会淡化文字和图标。从实例的模组页进入下载时，下载目标沿用该实例的 `mods` 目录，优先于上次手动选择的文件夹。

同步按版本、说明和附件内容指纹去重。已完成且内容相同时直接跳过，不启动 CNB 任务，也不重复上传附件。旧标签若不含该工作流，可用手动补同步。

同步仅包含版本说明、EXE、ZIP、校验文件及公开更新清单。CNB 的版本标签基于自身最小 `main` 分支创建，不镜像 GitHub 源码。手动补同步可在 Actions 中运行 `Sync release to CNB`，可指定版本标签。
## 来源与许可

原作作者为龙腾猫跃及 PCL 原项目贡献者，原作版权归成都瓜皮龙科技有限公司。基线为上游 2.14.0.0，保留原作与第三方组件的署名和许可。

- [上游 PCL](https://github.com/Meloong-Git/PCL)
- [来源与版权声明](NOTICE.md)
- [许可与使用指南](LICENCE)
- [支持原作者](https://meloong.com/afd/a/LTCat)
