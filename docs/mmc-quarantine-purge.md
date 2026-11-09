# MMC 隔离区永久清理候选

当前实现仍在验证，Main 没有永久清理入口，普通删除仍只移动到可恢复隔离区。尚不能把此候选报告为已完成的玩家数据清理功能。

逐条目 TxF 是为了解决 Windows 共享模式只保护单个数据流的问题。仅对默认流取得禁止写入的句柄，仍可能允许另一个程序新建 ADS；最后一次检查 ADS 后直接设置普通 delete-pending 也不能消除竞态。先设置普通 delete-pending、再检查并撤销的方案同样不成立：检查前被终止时，关闭句柄可能已经删除未确认的数据。

选定候选仅使用本地 NTFS 的短 KTM/TxF 事务：取得绑定事务的 writer 句柄后，在同一事务视图中核对文件身份、默认流摘要和完整有界数据流清单；目前只接受默认数据流。验证后暂存该条目的删除，关闭条目句柄，保留事务直至提交。提交前进程死亡应回滚本条目；提交后只允许删除已经验证的条目。外层 durable purge decision 仍负责记录剩余条目，并在恢复时继续前滚。目录和根目录也需要流清单和最终空目录检查。

原语的真实小样本验证见下文：外部 writer 明确使用 `FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE` 时，ADS 新建/更改在 writer 存活期间及条目句柄关闭、事务尚未提交期间均被拒绝，提交前/后终止也分别证明回滚/已批准删除。

之后已静态接入 `PurgeForward`：文件、目录、根目录和未决准备记录的撤回均通过短 TxF，不存在普通 disposition 删除后备路径。预览和执行扫描均拒绝已有 ADS/EFS；不可逆外层决定前还会用主定义执行无修改的 transaction-writer 能力检查。每条目在 writer 上重验身份/默认流清单/摘要，暂存删除并关闭条目句柄后才提交；`EntryStaged` 断点保留事务，用于确认该间隙的排他和真实终止回滚。根目录最终删除同样使用该原语并检查为空。已适配的实例所有权许可可验证当前事务 marker writer，避免为了读取自身 marker 而放松其它 pending/session 守卫。

本轮接入后的完整后端测试尚未运行，Main 入口仍关闭，不能据原语测试宣布整个永久清理功能验收通过。

每个原生句柄均限制为本地 NTFS，并拒绝零或全一的 64 位文件 ID。ReFS、网络文件系统、TxF 缺失/禁用、EFS 或写入冲突均拒绝处理或保留剩余条目，禁止退回普通 disposition 删除。此可选功能不是导入、修复或启动的依赖，也不修改服务、ACL、卷设置或 TxRM 配置。

微软[说明 TxF 的数据流采用文件级锁，并让绑定事务的 disposition 参与事务](https://learn.microsoft.com/en-us/windows/win32/fileio/programming-considerations-for-transacted-fileio-)；[最后一个事务句柄在提交前关闭会回滚](https://learn.microsoft.com/en-us/windows/win32/api/ktmw32/nf-ktmw32-createtransaction)。微软也[不推荐新项目依赖 TxF，并提示未来 Windows 可能移除此 API](https://learn.microsoft.com/en-us/windows/win32/fileio/deprecation-of-txf)。因此运行时能力不足时只能明确拒绝永久清理，保留普通隔离/还原路径。

2026-11-09 首轮只编译 Migration 项目并运行 `MmcPurgeTxFPrimitiveTests.SmokeCases()` 的一个文件原语用例：固定 SDK 10/c774，编译 0 警告/0 错误；已有 ADS 修改和新建请求在 writer 存活时以错误 32 被拒，关闭条目句柄、事务仍活动时的新 ADS 请求以 6800 被拒，随后提交删除已验证文件。此结果尚不覆盖目录、根目录、终止恢复或完整实例清理。原始日志位于工作树已忽略目录 `artifacts/mmc-purge-txf-proof/20261109-035916-e3a5b423f5cd47f095c61f60cc602b79/`。

随后单节点二编同样为 0 警告/0 错误，4 个 `BoundaryCases()` 分进程通过：文件/目录/根目录已有 ADS 被拒且原字节保留；目录/根目录的 stage 后关闭句柄间隙仍以 6800 拒绝 ADS，外部新子目录不能加入；已有 share-delete ADS writer 阻止事务 writer；3 个 parent 创建并核对 GUID/nonce/ready PID 的子进程分别在 writer、stage 后/commit 前、commit 后被终止，前两者回滚并保留主体和未批准流，后者只删除已验证主体。原始日志位于 `artifacts/mmc-purge-txf-proof/20261109-040105-boundaries-b56ff3d5f5df4644bc3440d2aa675619/`。这些是原语证据，尚不是完整实例永久清理验收。
