# Windows 资源路径对照记录

本记录属于 C007b 的等价迁移，不新增路径能力或扩大链接保护策略。`348b0a5` 已实际对照 137 例／52 项候选，原 8＋2 与后续 9 项差异全部消除；CI 改为 RequireEquivalent，仍待严格模式新提交实跑。下列历史差异保留作为实现依据，不把 Diagnostic 成功本身解释为路径等价。

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

## C007b2 适配与扩展验证

生产路径现经 `InstanceResourcePathCompatibility`／`InstanceResourcePathText`。net48 继续使用原 BCL；现代 Windows 在系统归约后补旧非法字符、组件／总长度、精确八尾字符与通配符／冒号异常顺序，extended 原串仍先返回。文本比较经显式 UTF16 长度的 Windows `CompareStringOrdinal`，保留原实例 receiver 的 NullReferenceException；非 Windows 继续使用平台 API。这是等价适配，不新增原生模块或性能改造。

oracle v2 保留原 75 例和期望，新增真实 Unicode guard、八 trim、组件 254／255／256、总 UTF16 长度 32766／32767、混合错误、leading-space、NT／dot-device 根与 GetDirectoryName 边界，总计 124 例。13 组文本以整数 UTF16 单元传输，真实 Framework String 的双向 Equals／StartsWith 共 52 项独立核对 Win32 候选；候选抛错不覆盖 String 基线，候选差异在 Diagnostic 也失败。旧 private trim 字段只在历史测试驱动反射记录，生产无此依赖。假 UNC 仍只做 Normalize，不对服务器执行文件探测。

提交 `8c64fcc52f6468ea254f73f8368d81b39f428538` 的 [Migration validation 41736377415](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41736377415)：Ubuntu job `126266516885` 308／308、Windows job `126266516510` 331／331，均 success。真实独立 CLR4 与现代 apphost 的 124 例及 52 候选比较已执行；契约／候选／文本差异全 0，原 8 个路径差异全消除，新增 9 个路径差异。原 CLR4 两处 trim 字段实际均为 `[9,10,11,12,13,32,133,160]`。

[完整 v2 JSON artifact 13280010226](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41736377415/artifacts/13280010226)，ZIP SHA256 `aceddb30263718b24039c6f731460c68e871532fb4b11d6c7104baac4f89712f`；固定源／config／manifest／Shared／Json 和现代两个 helper 哈希在报告中。`GetDirectoryName` 此提交仍直接转发 BCL；以下 9 项全部是新增样本，旧 75 项未删改期望。

| 新差异 ID | 原 CLR4 | 当前现代适配 |
| --- | --- | --- |
| guard.device-nt-descendant／guard.device-dot-descendant | 成功，祖先 walk 能到达可信设备根 | NullReferenceException |
| normalize.unc-no-share／normalize.unc-no-share-slash | ArgumentException | 接受仅 server 的 UNC |
| normalize.leading-space-nt | ArgumentException | 原串返回 |
| normalize.leading-space-extended | 折叠 sub／.. | 未折叠 |
| dirname.nt-repeated-separators | 截取原串，保留重复分隔符 | 压缩重复分隔符 |
| dirname.nt-root／dirname.dot-root | 截掉卷根最后反斜线 | null |

继续依据这些真实差异做最小设备路径／UNC 适配。普通 FullPath／GetDirectoryName 与限定归约不可混用：GetDirectoryName 不做完整路径归约、八字符 trim 或 FullPath 的 wildcard／ADS 拒绝；短名展开时序等未覆盖输入仍需保留限制，不声称此小批复现全部 BCL。

后继工作树已加入 NT 原串父目录截取、dot-device 盘符根末分隔符修正、UNC 缺 share 拒绝；只有前导空格移走后才出现 extended 的形状交给 `GetFullPathNameW`，避免现代 API 二次提前返回。GetDir 仅为 Normalize 后祖先遍历补旧 limited 长度，不作为所有 raw BCL 输入的通用替代；先归一分隔符再按旧跨度检查，保留中间 255 与末尾 255 的区别。canonical extended 盘符根与 dot-device 的旧根长度不同，不能套用同一裁剪。

为这些后继改动增加 5 组 Windows 生产回归，oracle 保留原 124 例再增 13 例（中间 254／255、末尾 255、总长 32766／32767、真实 guard／canonical 根、NT 父目录保 NUL、非法原串优先与空白），新总计 137／52。GetDir 的原串非法字符检查先于 limited 长度，不能套用 FullPath 归约后检查顺序。末次小修后三个工程零警告编译、本地 308／308 与独立终审通过。

`348b0a516b2395d1559135cf0a0e9fb36bdec270` 的 [Migration validation 41737707302](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41737707302)：Ubuntu job `126271080274` 308／308、Windows job `126271080675` 336／336；真实 137 例／52 项比较全无差异，四个失败数组均空，`equivalent=true`。新增长度合同已由实际 CLR4 确认，NT 父目录返回值确实保留 NUL，canonical leading-space 根仍抛原 NullReferenceException；不能把 dot-device 修复扩大到它。

[完整 JSON artifact 13280505603](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41737707302/artifacts/13280505603)，ZIP SHA256 `3e32a4f3a0f67f193f77bf9e324a64e7817360b4a3efecce77ff428c47a1b6e7`，已下载逐项核对，不依赖本地 scratch。原进程仍 CLR4.0.30319.42000、现代 .NET 10.0.14；本次为 Diagnostic 模式的零差异实测，严格模式下一提交实际运行后再关闭门禁。

## 下一步

CI 与脚本默认改为 RequireEquivalent，等待该提交的真实执行；任何 path／text／候选／基线差异均阻止通过。生产 equality／prefix 和 oracle 调同一实际适配入口；旧基线继续用固定原 VB／真实 Framework，不改期望掩盖差异。完整 Windows 资源、备份、更新、克隆、取消与恢复消费者同样持续验证。

源码审计参考固定 [Framework path.cs](https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/mscorlib/system/io/path.cs)、[FileIOPermission](https://github.com/microsoft/referencesource/blob/ec9fa9ae770d522a5b5f0607898044b7478574a3/mscorlib/system/security/permissions/fileiopermission.cs) 与 [runtime 10.0.0 Path.Windows.cs](https://github.com/dotnet/runtime/blob/60629d14374c56f1cb51819049ad1fa529307f8d/src/libraries/System.Private.CoreLib/src/System/IO/Path.Windows.cs)。尚未执行的适配和未覆盖边界仍保持显式待验证；75 个样本不代表所有 BCL 输入或完整现代 WPF 已验收。
