# MMC 隔离区永久清理候选

当前实现仍在验证，Main 没有永久清理入口，普通删除仍只移动到可恢复隔离区。尚不能把此候选报告为已完成的玩家数据清理功能。

逐条目 TxF 是为了解决 Windows 共享模式只保护单个数据流的问题。仅对默认流取得禁止写入的句柄，仍可能允许另一个程序新建 ADS；最后一次检查 ADS 后直接设置普通 delete-pending 也不能消除竞态。先设置普通 delete-pending、再检查并撤销的方案同样不成立：检查前被终止时，关闭句柄可能已经删除未确认的数据。

选定候选仅使用本地 NTFS 的短 KTM/TxF 事务：取得绑定事务的 writer 句柄后，在同一事务视图中核对文件身份、默认流摘要和完整有界数据流清单；目前只接受默认数据流。验证后暂存该条目的删除，关闭条目句柄，保留事务直至提交。提交前进程死亡应回滚本条目；提交后只允许删除已经验证的条目。外层 durable purge decision 仍负责记录剩余条目，并在恢复时继续前滚。目录和根目录也需要流清单和最终空目录检查。

这还需要真实小样本证明：外部 writer 明确使用 `FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE` 时，ADS 新建/更改在 writer 存活期间及条目句柄关闭、事务尚未提交期间都必须失败；提交前/后终止必须分别证明回滚/已批准删除。原语候选尚未接入 `PurgeForward`，旧源码的 ADS 风险尚未宣布关闭。

每个原生句柄均限制为本地 NTFS，并拒绝零或全一的 64 位文件 ID。ReFS、网络文件系统、TxF 缺失/禁用、EFS 或写入冲突均拒绝处理或保留剩余条目，禁止退回普通 disposition 删除。此可选功能不是导入、修复或启动的依赖，也不修改服务、ACL、卷设置或 TxRM 配置。

微软[说明 TxF 的数据流采用文件级锁，并让绑定事务的 disposition 参与事务](https://learn.microsoft.com/en-us/windows/win32/fileio/programming-considerations-for-transacted-fileio-)；[最后一个事务句柄在提交前关闭会回滚](https://learn.microsoft.com/en-us/windows/win32/api/ktmw32/nf-ktmw32-createtransaction)。微软也[不推荐新项目依赖 TxF，并提示未来 Windows 可能移除此 API](https://learn.microsoft.com/en-us/windows/win32/fileio/deprecation-of-txf)。因此运行时能力不足时只能明确拒绝永久清理，保留普通隔离/还原路径。

静态阶段只验证源码和差异格式；尚无编译、ADS 排他或真实终止测试通过结论。后续先由 Root 串行运行 `MmcPurgeTxFPrimitiveTests.SmokeCases()`，再决定是否扩展目录/根目录/终止场景和接入清理循环。
