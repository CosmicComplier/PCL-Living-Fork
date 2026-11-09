# MMC 导入任务与暂存目录

2026-11-09 用户报告：真实导入长期显示 15%，任务卡只有一行；取消后错误版本列表仍显示
`.pcllf-mmc-import-*`，普通版本删除入口因 MMC 门禁拒绝。随后又要求解压显示真实读写。

## 实现

导入与原包重试保持一个真实 producer。任务页的七行是该 producer 的只读阶段投影：
读取校验、组件解析、Java 选择、解压文件、准备运行环境、安装文件、保存实例。
状态来自真实 phase 和成功读写事件；未知总量显示进行中，不使用定时器推进百分比。
全缓存／空文件路径也由明确边界切换阶段。重试的第二次原包核对会重新显示读取状态，
不借用前一次已完成的 100%，总体权重不回退。

任务卡使用操作对象身份，cfg 名称替代 ZIP 标题不会再生成第二张旧卡。
取消后保留任务和卡片，显示正在取消，直到实际文件操作返回；提交决定后的取消继续完成结果。
producer 已完成但 Loader 尚在 PreviewFinish 的窗口也不能把成功发布改报 Canceled。
普通 CurseForge／Modrinth 及其他任务沿用原显示／关闭路径。

读写计数进入既有 `FileTaskProgress`／`IoRateSampler`：原包 SHA256、文件哈希、复制、解压与
结构文件写入均从实际成功的 Read／Write 计数。多遍校验分别计入真实读取；解压统计为逻辑流字节，
不是磁盘硬件吞吐。下载速率仍来自现有 NetFile，不把网络与文件 I/O 混为一项。

## 列表与恢复

私有暂存目录仅在规范路径、两份精确 marker 和独立 reservation owner 都匹配时从版本列表隐藏。
前缀相同、伪造 marker、损坏／超界 schema 均不能隐藏普通或未知实例。活动生产者不被列表读取抢占。
缓存与实时扫描都过滤已不存在的目录；成功导入继续刷新列表，取消现场的独立导入恢复入口保留。
此过滤没有删除或改写玩家目录，不把恢复数据交给普通版本删除流程。

用户当时的两个暂存目录在只读复核时已经不在磁盘上，外部 reservation 记录仍存在；
旧缓存仍把缺失路径加载为错误版本。该观察只说明当时状态，不推断由哪个进程删除了目录。

## 验证范围

最小实际 Main 用例使用 LF 自造 tiny ZIP、真实 backend、缓存命中和原生离屏控件，覆盖阶段、
实际字节、动态标题、同 SHA 重试、真实取消按钮、held 解压／事务与普通任务原行为。
`mmc-import-private-list` 另核对真实 reservation、缓存缺失项、损坏／伪造 marker 与恢复可达性。
完成边界、读写计数的最新断言已经在对应真实 Main 下执行：上述三个分组和
`mmc-import-completion-boundary` 四组均通过，Native 构建 0 警告／0 错误，Main 为 78 个既有警告／0 错误。

完整 GTNH 导入正在独立保留实例中复跑，使用用户原 ZIP 和旧验收缓存，不写用户 MCPACK 目录，
不启动游戏。日志显示真实解压持续推进后进入运行环境与文件安装；最终结果单独记录，不把这些中间
字节进度当作发布成功。首轮 private-Main 测试图缺失 `System.Runtime.Caching` runtime target，
发生在导入前；已在 prebuilt facade 模式补齐与产品相同的依赖，原失败日志保留。

2026-11-09 本轮完整 Java 8 原包复跑已取得 `Status=Succeeded` 的实际导入 receipt；
现代包继续独立执行。它们均不包含游戏启动，不能用中间进度或首包结果替代另一包结果。
保留 run 为 `C:/Users/16892/.codex/worktrees/mmc-sample-baseline/PCL2-MMC-Compatibility/artifacts/mmc-gtnh-acceptance/20261109-070501-bc01acf3d40a4978a658cd99211c80ef`。

当前进度／IO／取消修正的开发包源码为 `81796b2c908b8742a3043dfaa08fda4224cbdc5c`，
路径 `artifacts/mmc-dev-build/20261109-81796b2c/package/PCL-Living-Fork-dev_edition.exe`。
EXE 189076676 字节，SHA256 `3ff675ed5e70f11426e68a2cf01f4848fd38c89210c04e9abba41f6f96e92c50`；
ZIP 78599313 字节，SHA256 `0f12404470842ad6e6787f1bc2ad10fe6590d4cadad1fabace872d93f2270f68`。
六个发布模块、外 ZIP 五个条目及 Core 37 个 Git blob／CRC 均通过；没有执行该产品包做冷启动。
此包尚未包含用户随后授权的重复路径校验消重优化；该优化在独立分支开发和验证。
