# Windows 资源路径对照记录

本记录属于 C007b 的等价迁移，不新增路径能力或扩大链接保护策略。C007b1 的原控制流迁移与诊断工具已运行；下列现代差异仍由 C007b2 处理，不能将 Diagnostic 成功解释为路径等价。

## 固定来源与实际运行

- LF 原方法：`f4aa0fae62c3ee8af24c81e61844c8fdd13c8983` 的 `InstanceResourceStore.vb`，由 Git 原始字节唯一提取 Normalize／EnsureWithin；原文件 SHA256 为 `97bb35f2f561de41cb985176822601dc34d7f90e5eae9429080acba1a92d3f70`。
- 真实前缀处理：MeloongCore `c774ae4c85fd94e0eaa171db21e78a65870df19b`；在独立临时目录编译原 Shared，不使用替代的前缀算法。
- 生产与对照提交：`b12b6cfee2e6a877a65a50485908ca21943b2145`。
- [Migration validation 41730760349](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41730760349)，Windows job `126247144983`。独立 CLR4 和 .NET 10 apphost 使用原 LF config／有效根 manifest、同一夹具与工作目录。
- 原进程：CLR `4.0.30319.42000`、Domain 与程序集 `.NETFramework,Version=v4.8`；现代进程：`.NET 10.0.14`。两者均 64 位、`en-US`；UseLegacyPathHandling／BlockLongPaths 均实际为 `present=false,value=false`。未显式定义不是错误，也未为满足对照人工设置开关。
- 75 个实际样本、5 个真实符号链接；基线人工契约失败 0、路径差异 8、Unicode 差异 2。`RequireEquivalent` 对两类差异都失败；本次明确使用 Diagnostic。
- [完整 JSON artifact 13276625509](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41730760349/artifacts/13276625509)，名称 `migration-resource-path-comparison-windows`，ZIP SHA256 `fb1b683a6ee1b39689d994176e048c40ca007c6c6d7c4f0703bfa472b3dd5a87`。原报告含请求、上下文、原方法／配置／清单／依赖哈希、双方全部结果；续跑可从 Actions artifact 获取，不依赖本地 scratch。

下表用 `<root>` 代替临时夹具路径，避免将机器私有路径写入仓库。系统异常仅比较类型，LF 自有两条错误比较原文；不宣称复制全部 BCL 本地化消息。

## C007b1 的实测差异

| 样本 ID | 输入或含义 | 原 CLR4 | 当前 .NET 10 原控制流 |
| --- | --- | --- | --- |
| normalize.unicode-space | 仅 U+00A0 | ArgumentException | 作为当前目录下文件名接受 |
| normalize.illegal-kept | 文件名为 bad＋U+007C＋name | ArgumentException | 接受 |
| normalize.question-kept | `<root>\bad?name` | ArgumentException | 接受 |
| normalize.star-kept | `<root>\bad*name` | ArgumentException | 接受 |
| normalize.colon | `<root>\bad:name` | NotSupportedException | 接受 |
| normalize.trailing-tab | `<root>\tail` 后 U+0009 | 去除尾 tab | 保留尾 tab |
| normalize.long-component | 单组件 270 个 ASCII 字符 | PathTooLongException | Normalize 接受 |
| normalize.device-nt-nul | 单起始反斜线 `\??\` 前缀的夹具路径含 NUL | 原串返回 | ArgumentException |
| ordinal.sigma | Σ 与 ς，OrdinalIgnoreCase | false | true |
| ordinal.deseret | U+10400 与 U+10428，OrdinalIgnoreCase | false | true |

## 已确认且必须保留的边界

- `illegal-collapsed`、`question-collapsed`、`star-collapsed`：相应非法／通配符段被 `..` 消除后，原 Normalize 成功。不能在原串上套 C006a 的 GetFileName 检查来代替 GetFullPath。
- 多个短组件组成的超过 260 字符路径在该 LF-default／长路径 manifest 下成功；单组件长度失败不能误改为所有长路径失败。
- `guard.drive-root-descendant`：本次原进程对夹具所在卷根及夹具内缺失 leaf 实际抛 NullReferenceException。原 TrimEnd 将卷根变为 `C:` 形状，祖先 walk 不等于该 parent；本轮不能顺手修正。
- AllowRoot 必须在两次 Normalize 后判断；规范化后等于根且允许时提前返回，拒绝模式仍按原越界错误处理。
- Root 是可信锚点。内部文件／目录链接及内部链接祖先按原规则拒绝；Root 自身或其上方链接仍按原规则信任。缺失后代允许，不创建目录，也不新增 root 必须存在或必须为目录的要求。
- 固定 Meloong 前缀处理按 Ordinal 区分大小写，AfterLast 取最后完整标记；小写／混合 `unc`、重复标记等独立金值通过。假 UNC 仅做字符串规范化或提前返回分支，不对假服务器后代执行 Exists。

## C007b2 适配与扩展验证中

生产路径现经 `InstanceResourcePathCompatibility`／`InstanceResourcePathText`。net48 继续使用原 BCL；现代 Windows 在系统归约后补旧非法字符、组件／总长度、精确八尾字符与通配符／冒号异常顺序，extended 原串仍先返回。文本比较经显式 UTF16 长度的 Windows `CompareStringOrdinal`，保留原实例 receiver 的 NullReferenceException；非 Windows 继续使用平台 API。这是等价适配，不新增原生模块或性能改造。

oracle v2 保留原 75 例和期望，新增真实 Unicode guard、八 trim、组件 254／255／256、总 UTF16 长度 32766／32767、混合错误、leading-space、NT／dot-device 根与 GetDirectoryName 边界，总计 124 例。13 组文本以整数 UTF16 单元传输，真实 Framework String 的双向 Equals／StartsWith 共 52 项独立核对 Win32 候选；候选抛错不覆盖 String 基线，候选差异在 Diagnostic 也失败。旧 private trim 字段只在历史测试驱动反射记录，生产无此依赖。假 UNC 仍只做 Normalize，不对服务器执行文件探测。

本地实际 Linux 测试 308／308、net48 库与现代 host 零警告／错误；原方法注入 VB 驱动已编译，尚未在本批 Windows 执行。`GetDirectoryName` 此时仍直接转发 BCL，设备根／受限归约与短名处理仍未闭合；待扩展报告确定必要的局部适配，不能将既有 8＋2 差异或新增边界写为已消除。

## 下一步

C007b2 以实测差异为依据，分别处理现代 Windows 的路径 API 和旧 OrdinalIgnoreCase 语义。生产 equality／prefix 和 oracle 必须调用同一实际适配入口；旧基线继续使用固定原 VB／真实 Framework，不更改期望掩盖差异。适配后改为 RequireEquivalent，并运行完整 Windows 资源、备份、更新、克隆、取消与恢复消费者。

源码审计参考固定 [Framework path.cs](https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/mscorlib/system/io/path.cs)、[FileIOPermission](https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/mscorlib/system/security/permissions/fileiopermission.cs) 与 [runtime 10.0.0 Path.Windows.cs](https://github.com/dotnet/runtime/blob/60629d14374c56f1cb51819049ad1fa529307f8d/src/libraries/System.Private.CoreLib/src/System/IO/Path.Windows.cs)。尚未执行的适配和未覆盖边界仍保持显式待验证；75 个样本不代表所有 BCL 输入或完整现代 WPF 已验收。
