# GTNH 显式实包导入验收

此入口需要本机原包、真实 Java 与外网，**不属于默认 facade 分组或 CI 门禁**。它测试真实 Main 的 managed MMC 导入、最终配置和制品布局；导入完成不能当作游戏启动、世界或修复验收通过。

在测试程序 `Main` 的默认分组解析之前显式接入：

```csharp
if (MmcGtnhAcceptance.TryRun(args, out int gtnhExit)) return gtnhExit;
```

构建完整真实 Main/facade 产物后手动执行：

```powershell
dotnet tests/PCLLF.Native.Facade.Tests/bin/Debug/net10.0-windows/PCLLF.Native.Facade.Tests.dll --mmc-gtnh-acceptance
```

可用 `--gtnh-case java8` 或 `--gtnh-case java21` 单独运行一份；默认分别运行两份。入口固定只读使用：

- `D:\User\Downloads\GT_New_Horizons_2.9.0_Java_8.zip`。
- `D:\User\Downloads\GT_New_Horizons_2.9.0_Java_17-26.zip`。

原包 SHA256 与 [P0 实测基线](mmc-samples.md)逐字节核对；不重新下载发行 ZIP，也不读取玩家实例、存档或账号。fixture 的 Java 列表只含真实安装的 Java 8 与 Java 21，仍由实际 Main Java 探测和包策略过滤选择；入口记录探测路径、版本与最终编译环境。

每次运行在仓库已忽略的 `artifacts/mmc-gtnh-acceptance/<时间-GUID>/` 下创建独立 case、游戏根、缓存和私有 profile。另一个 `PclNativeFacade-<GUID>` 临时根保存原隔离 fixture 身份与完整编译产物；两处均写所有权记录并永久保留。父进程核对 nonce/PID/根路径后只结束自己创建的子进程，不递归删除验收数据。后续实机启动可继续使用报告中的实际实例根。

后续重跑可显式传入 `--gtnh-reuse-cache-run <旧验收 run 根>`。仅接受本仓库验收目录的直接子 run，核对 run/case 的所有权标记并拒绝重解析点祖先；每个新实例和 profile 仍使用全新目录。它只复用旧 run 对应 case 的 metadata、libraries、assets 和 source-cache，继续执行原有来源/摘要校验，不把旧的半成品实例或事务目录直接发布。旧日志、原包和实例目录保留。

新版 runner 等待真实 receipt/子进程退出，不按总耗时判定失败；UI 泵使用有限等待片段，未达到 idle 只结束本片段、继续等待实际结果；父进程周期输出存活提示。相同 NetFile 计数不会重复宣称有新进展。

`stdout.log`、`stderr.log` 和 `import-result.json` 记录每份结果，`source-and-java.json` 保存原包/实际 Main 身份、Java 探测和摘要。BYTES 日志来自原 ZIP 实际读取、后端真实解包/物化回调与真实 NetFile 计数；总量未知时写 `unknown`，不据此编造完成百分比。最终核对已发布定义/兼容投影、Java 策略、入口、参数、库顺序、实际文件 SHA256、owned 运行库路径和 native/asset 派生记录。

初版为每个 case 使用独立 Minecraft 根；它证明逐包独立导入，不覆盖“同一游戏根混合普通实例与两类 MMC”的验收。该项需要随后在保留的目录上通过受支持的移动/克隆与实例管理入口补做，不通过手改定义或删除缓存来冒充混合回归。真实游戏、世界、修复和再次启动继续单独记录。
