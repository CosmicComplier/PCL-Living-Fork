# 现代 C# 产品开发入口

在仓库根目录打开 [PCL-Living-Fork.sln](../PCL-Living-Fork.sln) 或 [PCL-Living-Fork.Modern.slnx](../PCL-Living-Fork.Modern.slnx)。两个入口都包含同一组真实 C#/.NET 10 LF 主程序、现代 Core/Presentation、固定的 Shared，以及现有 Native/组件/纯 C# 验收项目；默认 solution 不再构建 VB 产品。迁移验收与旧比较源码的退休门禁见 [MIGRATION_STATUS.md](MIGRATION_STATUS.md)。

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

默认发布入口现在生成真实 Native 完整包。在仓库根目录指定一个新目录：

```powershell
$releaseOutput = Join-Path $PWD ('artifacts/native-release/' + [Guid]::NewGuid().ToString('N'))
pwsh -NoProfile -File scripts/Build-Release.ps1 -Configuration Release -OutputPath $releaseOutput
```

脚本复用 C# 凭据生成器的 GUID 临时目录与清理逻辑，执行真正的 `win-x64` 自包含单文件发布，并创建原更新协议需要的兼容桥。其发布调用已关闭构建服务器/节点复用并顺序执行。默认源码构建可使用空内嵌凭据。该命令只生成文件。未传 `-OutputPath` 时输出到 `artifacts/release/{Configuration}`；已存在的发布资产不会被覆盖，重复发布请选新目录或先自行处理旧资产。可用 `-EnableLegacyLink` 将原可选联机代码编译进真实产品，构建脚本不会启动它。`Build-NativeRelease.ps1` 和 `Build-ModernRelease.ps1` 保持独立底层入口，调用链为默认入口→Native 打包→Modern C# 发布，不循环调用。

以实际 `NativeRelease.Artifacts.json` 中的角色、版本与 SHA256 为准：

| 角色 | 文件与用途 |
| --- | --- |
| `native-exe` | `LivingPCL.Native-{version}-win-x64.exe`，真正的自包含单文件启动器 |
| `native-zip` | `PCL-Living-Fork-{version}-win-x64.zip`，内含 `PCL-Living-Fork-{version}.exe`，其字节为原生产品 |
| `legacy-bridge` | 顶层 `PCL-Living-Fork-{version}.exe`，供旧客户端自动更新使用的托管兼容桥 |
| `checksums` | `SHA256SUMS.txt`，覆盖以上三个实际资产 |

`NativeRelease.Artifacts.json` 是构建索引。跨目录或跨 CI job 下载后，按角色的 `fileName` 在下载目录定位并复验 hash，索引里的原生产目录绝对 `path` 需要重新定位。完整功能兼容与 Windows 实际结果继续按迁移进度记录验收。

旧 `Test-BuildCredentials.ps1` 的 CLR4 源码差分及失败/并发样本继续使用固定 LF Git 对象和 `scripts/tests/Build-HistoricalCredentialFixture.ps1`。该 helper 必须使用原 GUID 临时目录与明确的 `FailBuild.exe`，正常工作树/MSBuild/成功产品构建均被拒绝；它不是默认发布路径。真实 Native 凭据验收必须引用对应发布的完整 `LivingPCL.dll` 或经过有界读取识别的真正单文件入口，不能反射顶层旧更新兼容桥来冒充 Main。
