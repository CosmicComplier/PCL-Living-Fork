# 微软登录存储隔离

微软刷新令牌绑定用户与客户端，不能把不同 Client ID 的启动器缓存当作可互换数据。依据：[Microsoft identity platform refresh tokens](https://learn.microsoft.com/en-us/entra/identity-platform/refresh-tokens)。

内部设置名继续使用 `LoginMsJson` 和 `CacheMs*`，注册表存储统一由 `RegistryValueName` 映射为带 `_LF` 后缀的键，位置仍为 `HKCU\Software\PCL`。共覆盖 13 个账号、令牌、资料、过期时间及升级标记，例如 `LoginMsJson_LF`、`CacheMsV2OAuthRefresh_LF`、`CacheMsV2Migrated_LF`。读取、保存、刷新、退出、重置与账号格式迁移均使用该映射。

不读取、覆盖或删除无后缀的共享微软凭据；旧私有存储中没有客户端来源标记的微软凭据也保留原样，不猜测导入。首次升级可能需要在 LF 重新登录一次。已有 `_LF` 缓存及明确的退出登录状态优先，不使用旧缓存复活账号。同一命名空间内的旧版缓存仍可按原有规则升级为 V2 格式。

其他账号与普通设置保留现有共享规则，游戏文件夹列表、`Identify`、登录类型等键不加后缀。账号写入继续使用设备加密与事务回滚，凭据不写入日志。该隔离防止本分支误用其他分支缓存，不改变微软服务本身的令牌过期与撤销规则。

`Test-MicrosoftRegistryIsolation.ps1` 和 `Test-Release.ps1` 仅在 GUID 注册表测试分支使用虚构密文，验证共享微软键不变、LF 保存/退出/重置、旧缓存排除、迁移幂等及失败回滚，不读取真实账号或请求微软服务。
