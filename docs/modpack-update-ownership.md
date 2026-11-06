# 整合包更新：身份、作者归属与预览

更新以旧作者声明、本地实际文件、新作者声明作三方判断。平台项目 ID、平台文件／版本 ID、加载器自身 ID、提供别名、路径和内容哈希是不同证据，不能互相替代。

## 文件关联与操作

- 旧作者路径内容匹配原哈希：应用作者更新或移除。
- 路径改变而内容匹配旧作者哈希：关联实际本地路径。升级时先写新版，再处理真实旧路径，两步属于同一备份／回滚事务。
- 文件名变化的作者升级优先按平台项目关联；没有平台身份时，必须同时具备旧作者内容证明、旧／新作者声明和可靠自身 ID。旧 JAR 使用旧运行时解析，新 JAR 使用目标运行时解析。
- 内容或版本被替换：默认保留，可逐项选择作者版本。禁用文件即使选择作者升级也保持 `.disabled`，不会自动启用。
- 本地新增或运行生成的内容：预览展示并保留，不进入删除事务，不自动成为作者文件。个人文件占据作者目标路径时也不能被后续更新收编。
- 同哈希多候选、一份本地文件对应多条作者记录、同项目多个版本候选：提示待确认，不自动选择、删除或替换。
- 重复自身 ID／有效提供别名与同一作者项目升级分别判断。预览前计算最终保留集合及新版文件之间的冲突；提交前按实际选择复核。

本地内容预览覆盖 `mods` 顶层文件，以及 `config`、`defaultconfigs`、`resourcepacks`、`shaderpacks`、`datapacks` 内容；不会把存档、运行库、账号数据或 PCL 日志目录扫描成个人模组。无法判断来源的新增配置会明确显示可能由运行生成。

## 作者记录 schema 2

`PCL/ModpackOrigin.json` 的 `files` 保存作者声明的完整预期内容，包括未安装、禁用、修改过的条目。`hash` 始终为原作者哈希，不用本地修改后的内容覆盖。`identity` 是平台项目；`fileIdentity` 是具体文件／版本。

`state` 与 `local` 分别记录实际状态、路径、观察哈希及归属标记；这些观察不是新的作者声明。`local.ownership=personal-added` 或 `ambiguous` 跨更新保留，防止第二次更新误认归属。新版清单之外保留的旧作者文件记录在 `retained`，不提升为新版作者文件。

schema 1 可读，但旧版首次记录可能裁掉缺失或已修改文件，必须用正确旧包补录。补录核对现有 MC、加载器及已知包名／版本、作者哈希和平台项目。无法核对时明确报错；仅 MC 相同不足以认定正确旧包。损坏的结构、越界路径或未知哈希算法均拒绝。支持按记录的 MD5、SHA-1、SHA-256、SHA-512 校验。

## 加载身份与兼容性

更新复用 `ModDependencyInspection` 的加载器上下文解析器。单 JAR 入口不扫描目录、不写索引或旧模组缓存：

- Forge／NeoForge 仅 `[[mods]]` 建立自身声明；`[[dependencies.*]]` 属于依赖。多模组、注释及多行字符串有 fixture。错误表形状、损坏或当前 TOML 子集不支持的语法标为未知，不用弱匹配判重。
- Fabric 根 ID、`provides`、`depends`、嵌套 `jars` 分开。Quilt 字符串／对象别名分别读取。多加载器元数据按实际上下文选择，不无差别合并。
- 旧 Forge 的 `mcmod.info` 为描述信息，结合实际 class `@Mod` 声明确认身份；metadata-only 文件不冒充可靠加载身份。
- 嵌套库候选独立保留，不当作外层文件自身身份。未知或损坏的元数据会在预览中提示。

预览显示 MC／加载器的前后版本及保留模组的声明约束风险。没有发现声明冲突不代表实机兼容；缺失或无法完整判断的约束明确标为未知。预览之后仍检查本地文件、模组目录及版本 JSON／JAR／配置是否变化，变动则要求重新检查，不覆盖外部修改。

固定审计参照（核对于 2026-11-06；不代表玩家实际使用这些加载器版本）：

- [NeoForge 1.21.1](https://docs.neoforged.net/docs/1.21.1/gettingstarted/modfiles/)、[Forge 1.20.x](https://docs.minecraftforge.net/en/1.20.x/gettingstarted/modfiles/)。语言加载器 `loaderVersion` 不直接当成 Forge／NeoForge 产品版本。
- [Fabric 文档](https://docs.fabricmc.net/develop/loader/fabric-mod-json)，[Loader 0.16.14 解析器](https://github.com/FabricMC/fabric-loader/blob/075dbfa476b1259f1d04a96c2b9afc6bd8f06c4e/src/main/java/net/fabricmc/loader/impl/metadata/V1ModMetadataParser.java)。
- [Quilt Loader 0.29.2 解析器](https://github.com/QuiltMC/quilt-loader/blob/0d9e1f2bcfad433651ec194f5bd2b58c7caf56ee/src/main/java/org/quiltmc/loader/impl/metadata/qmj/V1ModMetadataReader.java)。
- [旧 Forge MetadataCollection](https://github.com/MinecraftForge/MinecraftForge/blob/3effde4f1fc9d14d6ed1dbf6bebc39c2b18780e1/src/main/java/net/minecraftforge/fml/common/MetadataCollection.java)、[FMLModContainer](https://github.com/MinecraftForge/MinecraftForge/blob/3effde4f1fc9d14d6ed1dbf6bebc39c2b18780e1/src/main/java/net/minecraftforge/fml/common/FMLModContainer.java)。

实现以当前 LF 的 C# 生产代码与同源历史为基础，复用现有解析器；本修复未复制 CE 组件或更换工程底座。

## 验证与玩家验收

核心 fixture 使用真实生产引擎、有效 JAR 与有效整合包，只替换网络运输。运行：

```powershell
dotnet run --project tests/PCLLF.Migration.Tests -c Release -- --group pack-update --group mod-dependencies --group environment-history --group instance-clone
python scripts/tests/Test-MigrationSelection.py
```

Windows 产品构建后，实际 Main／WPF 预览与更新任务回归入口：

```powershell
dotnet run --project tests/PCLLF.Native.Facade.Tests -c Release --no-build -- --groups modpack-update
```

Windows 实际运行和玩家完整整包验收分别记账；Linux 交叉编译不作为 Windows 运行通过。玩家验收应检查 AQS 升级后的最终文件列表、Lithium 单份纯改名的升级结果、个人新增／禁用状态、选项与运行时告警，以及再次更新时的归属。没有取得玩家完整包时，不宣称已验证其实际游戏兼容性。

测试包通过 `scripts/Build-Release.ps1 -Slim` 构建，默认 `dev_edition`，需要 .NET 10 Desktop Runtime x64。不加 `-OfficialRelease`，不自动发布 Release。公开发布另遵循 [发布约定](PUBLISHING.md)。
