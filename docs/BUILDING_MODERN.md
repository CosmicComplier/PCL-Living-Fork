# 现代 C# 产品开发入口

在仓库根目录打开 [PCL-Living-Fork.sln](../PCL-Living-Fork.sln) 或 [PCL-Living-Fork.Modern.slnx](../PCL-Living-Fork.Modern.slnx)。两个入口都包含同一组真实 C#/.NET 10 LF 主程序、现代 Core/Presentation、固定的 Shared，以及现有 Native/组件/纯 C# 验收项目；默认 solution 不再构建 VB 产品。原178个业务VB/旧vbproj与临时比较builder已退役，两个独立测试VB oracle及固定历史源码故障样本保留；迁移验收和退休后Windows结果见 [MIGRATION_STATUS.md](MIGRATION_STATUS.md)。

主程序是 [modern/Main/PCL-Living-Fork.Modern.csproj](../modern/Main/PCL-Living-Fork.Modern.csproj)，`WinExe` 的入口为 `PCL.Application`。它是 solution 唯一根项目。启动项目选择属于 IDE 用户设置，`.slnx` 没有可移植的默认启动项目声明；首次打开时将此项目设为启动项目。命令行启动使用下面明确的 `--project` 路径。

## Windows 工具链与依赖

使用 Windows x64 和 .NET 10 SDK，包含 Windows Desktop/WPF/WinForms 的目标与运行组件。IDE 需要支持 .NET 10 与 `.slnx`。先在仓库根目录初始化固定依赖：

```powershell
git submodule update --init MeloongCore
git -C MeloongCore rev-parse HEAD
```

当前 Shared 固定为 `c774ae4c85fd94e0eaa171db21e78a65870df19b`，保持原 `netstandard2.0` 项目。现代 solution 的七个项目都为 C#；BuildEvents 与资源生成工具由 Main 的实际构建 targets 管理，solution 的 `Build inputs` 提供它们的文件入口。

创建完整发布包中的旧版更新桥，还需要 Visual Studio/Build Tools 的 MSBuild 组件及 .NET Framework 4.8 编译/运行环境。发布脚本用 `vswhere` 定位其 Roslyn `csc.exe`。Windows CI 镜像提供这套旧桥工具链；首次安装用的原生自包含产品携带 .NET 10 运行时。

## 构建、启动和定向验收

在同一工作目录顺序执行构建与发布。`-nr:false` 使 MSBuild 节点在命令结束后退出，便于释放 BuildEvents 的任务 DLL；Shared 的原输出目录也由这条构建链管理。

```powershell
dotnet restore PCL-Living-Fork.Modern.slnx
dotnet build PCL-Living-Fork.Modern.slnx --configuration Debug --no-restore -nr:false
```

solution 构建会编译列出的验收程序。日常只改产品时，可直接构建真实 Main；随后通过同一项目启动正常 LF：

```powershell
dotnet build modern/Main/PCL-Living-Fork.Modern.csproj --configuration Debug --no-restore -nr:false
dotnet run --project modern/Main/PCL-Living-Fork.Modern.csproj --configuration Debug --no-build
```

当前 Main 的已知转换警告保留在构建日志中；Core、Presentation、构建工具与测试的严格检查由现有 CI 记录。原可选 LegacyLink 模式的独立编译入口：

```powershell
dotnet build modern/Main/PCL-Living-Fork.Modern.csproj --configuration Debug --no-restore -p:EnableLegacyLink=true -nr:false
```

测试使用各项目现有的自运行入口，按受影响链选择。例如，编译 solution 后检查纯 C# 存档备份规则：

```powershell
dotnet run --project tests/PCLLF.Migration.Tests/PCLLF.Migration.Tests.csproj --configuration Debug --no-build -- --group world-backups
```

真实 WPF 组件与 Native facade 验收在 Windows 执行；参数分别见 [组件测试](../tests/PCLLF.Modern.Wpf.Tests/PCLLF.Modern.Wpf.Tests.csproj) 和 [Native facade 说明](../tests/PCLLF.Native.Facade.Tests/README.md)。正式冷启动、正常关闭、重开及旧版升级由隔离的 Windows CI 脚本验证。普通开发启动使用 LF 的正常用户数据路径。

## Release 发布包

默认本机构建（包括 Debug、Release 和精简包）使用 `dev_edition` 作为显示版本和产品版本。`ModBase.ReleaseBaselineVersion` 保留最后同步的已发布版本，仅供更新比较：开发构建不被同基线或更旧发布覆盖，更新通道仍决定是否接收更高版本的预发布；开发构建在版本列表中按内部基线参与排序。开发标识不是公共 Release 标签。

设置的“更新”页面按页读取所选 CNB 或 GitHub 源的公开发布目录。搜索栏复用模组管理的 `MySearchBox`，下方列表显示全部匹配结果，不限于最新发布或第一页。正式版、预发布版和本机开发版统一使用 PCLLF 图标，并在版本名后以文字与颜色标签区分。浏览预发布版不改变自动更新通道；缺少完整自动更新附件的历史发布仍可查看说明，下载与替换继续执行原有附件及校验检查。

检查更新期间隐藏版本列表、结果数量和提示条，显示加载状态；请求成功或失败后恢复相应内容。全部版本按版本号降序排列；当前已安装版本使用绿色边框和“当前版本”标记，搜索不匹配时隐藏。

准备真正的正式版或预发布版时，先更新源码中的发布基线与数字程序集版本，再明确传入 `-OfficialRelease`，例如 `scripts/Build-Release.ps1 -OfficialRelease -OutputPath <全新目录>`。直接 `dotnet build` / IDE 启动默认仍为开发版；只有显式的 `LFOfficialRelease=true` 编译属性才使用发布基线作为产品版本。内部数字程序集版本继续用于 CLR 与旧更新桥身份兼容。

需要精简包时，可运行 `scripts/Build-Release.ps1 -Slim -OutputPath <全新目录>`。该选项生成依赖已安装的 **.NET 10 Windows Desktop Runtime x64** 的单文件 EXE、ZIP、校验文件和 `SlimRelease.Artifacts.json`，不捆绑运行时，也不生成旧版自动更新桥。当前精简资产使用独立的 `LivingPCL.Slim-*` 名称，供手动安装和实机测试；不要直接替换现有正式更新源的兼容资产。默认自包含发布流程保持原有角色与更新协议。

默认发布入口现在生成真实 Native 完整包。在仓库根目录指定一个新目录：

```powershell
$releaseOutput = Join-Path $PWD ('artifacts/native-release/' + [Guid]::NewGuid().ToString('N'))
pwsh -NoProfile -File scripts/Build-Release.ps1 -Configuration Release -OutputPath $releaseOutput
```

脚本复用 C# 凭据生成器的 GUID 临时目录与清理逻辑，执行真正的 `win-x64` 自包含单文件发布，并创建原更新协议需要的兼容桥。其发布调用已关闭构建服务器/节点复用并顺序执行。默认源码构建可使用空内嵌凭据。`Build` 工作流现在统一调用 Native reusable；保留旧流水线的 `CLIENT_ID`、`CURSEFORGE_API_KEY` 两个可选 secret，仅在打包及同一实际 Main/bundle 凭据检查步骤映射为 `PCL_MS_CLIENT_ID`、`PCL_CURSEFORGE_API_KEY`。缺失时仍允许公开空值构建；本地可经相同环境变量提供输入。值不进入命令行、日志或验收回执，ce65最终统一Build中真实打包/同Main-bundle C7、双配置28族和同run旧桥均已通过，不以空值或配置来源校验证明真实服务授权。该命令只生成文件。未传 `-OutputPath` 时输出到 `artifacts/release/{Configuration}`；已存在的发布资产不会被覆盖，重复发布请选新目录或先自行处理旧资产。可用 `-EnableLegacyLink` 将原可选联机代码编译进真实产品，构建脚本不会启动它。`Build-NativeRelease.ps1` 和 `Build-ModernRelease.ps1` 保持独立底层入口，调用链为默认入口→Native 打包→Modern C# 发布，不循环调用。

以实际 `NativeRelease.Artifacts.json` 中的角色、版本与 SHA256 为准：

| 角色 | 文件与用途 |
| --- | --- |
| `native-exe` | `LivingPCL.Native-{version}-win-x64.exe`，真正的自包含单文件启动器 |
| `native-zip` | `PCL-Living-Fork-{version}-win-x64.zip`，内含 `PCL-Living-Fork-{version}.exe`，其字节为原生产品 |
| `legacy-bridge` | 顶层 `PCL-Living-Fork-{version}.exe`，供旧客户端自动更新使用的托管兼容桥 |
| `checksums` | `SHA256SUMS.txt`，覆盖以上三个实际资产 |

`NativeRelease.Artifacts.json` 是构建索引。跨目录或跨 CI job 下载后，按角色的 `fileName` 在下载目录定位并复验 hash，索引里的原生产目录绝对 `path` 需要重新定位。完整功能兼容与 Windows 实际结果继续按迁移进度记录验收。

独立历史凭据检查运行 `powershell -NoProfile -File scripts/native-smoke/Test-HistoricalCredentialFailures.ps1`，需要真正 Windows PowerShell 5 Desktop/CLR4。它从固定 LF Git 对象导出凭据基础源与许可，编译自有 GUID 内不含 Main/WPF 的 credential-only DLL，以独立 WinPS 子进程复用原完整 `Test-BuildCredentials.ps1`，保留源码差分、失败/非法输入/并发锁样本；编译与子进程临时文件限制在同一 GUID，环境在 finally 恢复。此独立入口已在20e/585的真实Windows Release通过。失败构建由 `scripts/tests/Build-HistoricalCredentialFixture.ps1` 承载。该 helper 必须使用原 GUID 临时目录与明确的 `FailBuild.exe`，正常工作树/MSBuild/成功产品构建均被拒绝；它不是默认发布路径。真实 Native 凭据验收必须引用对应发布的完整 `LivingPCL.dll` 或经过有界读取识别的真正单文件入口，不能反射顶层旧更新兼容桥来冒充 Main。

历史交接和验收记录见 [迁移账本交接段](MIGRATION_STATUS.md#migration-handoff)。当前主分支门禁以本页说明及实际 workflow 为准。本机请走正常产品 UI，不伪造 CI fresh-host 守卫环境。

## 主分支 CI 门禁

`main` 的推送、面向 `main` 的 PR、合并队列组合提交，以及手动检查均执行全量产品、迁移和 WPF 验证。`migration/csharp-modern-dotnet` 的普通推送/PR 保留按改动选择检查；无法识别的事件和差异回退为全量。自动触发不使用路径过滤，文档变更也会报告必需检查状态。

汇总检查 `Native product gate`、`Migration validation gate`、`Modern WPF gate` 使用 `always()` 检查真实前置 job 结果：已选验证必须成功，失败、取消或意外跳过均失败；只有选择器明确判定不受影响的迁移分支检查才允许跳过。它们可作为分支保护的稳定检查名称，CNB 工具检查仍仅运行离线测试，不触发发布。

本地先运行以下路由和结果门禁回归（Python 环境需有 PyYAML 6.0.2）：

```text
python scripts/tests/Test-MainCiGates.py
python scripts/tests/Test-MigrationSelection.py
python scripts/tests/Test-NativeUpgradeQualification.py --powershell <PowerShell 7>
```

本地干净输出目录的构建、迁移回归和隔离 facade 不能替代最终提交的远端干净 CI。真实冷启动/关闭/重开、旧版到 Native 升级及异常恢复仍使用隔离的 Windows CI 环境验证。

## 源码职责与发布体积

| 路径 | 职责 |
| --- | --- |
| `modern/Main` | WPF 应用入口、页面、原生控件适配与模块调用面 |
| `PCLCS` | 唯一的业务与平台实现 |
| `PCLCS/Platform/Windows/{Events,Downloads,Instances,ModDependencies,Modpacks,Resources,Settings,Imaging,Toolbox}` | Windows 控制器、契约与服务按业务域归类；命名空间保持兼容 |
| `presentation` | 按 Controls、Dialogs、Pages、Shell 等职责组织的 WPF 表现层 |
| `modern/BuildEvents` | VB WithEvents 的显式事件绑定生成与校验 |
| `modern/BuildResources` | 五个必要数据/原生资源及 WPF pack 资源清单 |

托管依赖随正常 .NET 依赖图/单文件包提供，不再在主程序集资源中嵌入第二份副本。精简发布保留兼容的单文件布局、完整程序集与资源；.NET SDK 不支持依赖运行时的单文件压缩。产品身份、模块 SHA、凭据检查与冷启动读取继续使用有界、只读的原生 bundle 读取器。

版本说明使用原生 WPF Markdown 视图，支持标题、强调、列表、引用、代码、表格、任务列表和可点击链接。Markdig 许可在关于页和仓库第三方目录保留。
