# MMC 基本可用验收

本轮先验收导入、启动、专用补全及失败恢复；完整路线仍由
[全任务清单](mmc-completion-ledger.md)跟踪。性能工程后置，非 GTNH 实包游戏验收按用户决定交由用户在新构建上自行尝试。

| 项目 | 当前证据 | 状态 |
| --- | --- | --- |
| GTNH 2.9.0 Java 8／Java 21 | 两份独立实例完成主菜单、创建世界、正常退出、修复、重开世界、正常退出 | 已有实机证据，见 `mmc-gtnh-acceptance.md` |
| 普通 Forge／Fabric | 真实后端导入、依赖与库、离线修复、再次准备、世界／偏好保留 | 受控后端通过，不能称为实包游戏已测 |
| Main 补全入口 | 实际 Controller／Loader／RepairAsync、缺 JSON 与损坏 owned JAR、身份竞争、取消 | 已通过 |
| Main 导入恢复／重试 | 实际恢复按钮、目录、原记录校验、同 SHA 不同名称、取消排空、篡改拒绝 | 已通过 |
| 真实进程崩溃恢复 | 自有 child 在 Prepared／CommitDecided 信号后由核实身份的父进程结束，再调用真实恢复 | 进行中，不用异常模拟充数 |
| 自包含程序结构 | 官方构建包装器、真实 bundle 身份、六个主要程序集与 publish 输出逐字节 SHA256 一致、ZIP 内容与 EXE 摘要 | 已通过；未执行程序 |
| 程序冷启动 | 用户自行打开新构建并确认主界面／依赖情况 | 待用户结果 |

## 2026-11-09 开发构建

源码：`10543184`，`codex/mmc-compatibility`；Windows x64，`Snapshot`／`dev_edition`。
使用 `scripts/Build-ModernRelease.ps1 -NativePayload` 的原凭据生成、构建租约与 finally 清理，
输出通过真实 `ModernBundleIdentity` 只读检查。

保留目录：`artifacts/mmc-dev-build/20261109-10543184/`。

- 用户 EXE：`package/PCL-Living-Fork-dev_edition.exe`，188,798,148 字节。
- EXE SHA256：`61d7dff1afd1b9f4d156547cd867f68f694608765546bc80c70791e16f281cdb`。
- ZIP：`PCL-Living-Fork-dev_edition-win-x64.zip`，80,809,591 字节。
- ZIP SHA256：`3b7a98ba8578151fae2fb031f8144747b21d1d77fae8d87997ad84b75434130f`。
- 包含 Core 对应源代码 ZIP、完整 MPL-2.0 文本、宿主原许可及使用说明。
- `bundle-verification.json` 与 `package-verification.json` 记录结构检查；`ExecutedProduct=false`。

构建和结构检查不替代冷启动、账号登录、真实游戏或全部 MMC 特性的验收。
本次没有推送、合并 main 或上传 Release。
