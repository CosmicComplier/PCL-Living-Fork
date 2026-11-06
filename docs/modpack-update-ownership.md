# 整合包更新：身份、作者归属与预览

更新以旧作者声明、本地实际文件、新作者声明作三方判断，以新版作者整合包作为目标组合。平台项目 ID、平台文件／版本 ID、加载器自身 ID、提供别名、路径和内容哈希是不同证据，不能互相替代。

## 文件关联与操作

- 旧作者路径内容匹配原哈希：应用作者更新或移除。
- 路径改变而内容匹配旧作者哈希：关联实际本地路径。升级时先写新版，再处理真实旧路径，两步属于同一备份／回滚事务。
- 文件名变化的作者升级优先按平台项目关联；没有平台身份时，必须同时具备旧作者内容证明、旧／新作者声明和可靠自身 ID。旧 JAR 使用旧运行时解析，新 JAR 使用目标运行时解析。
- 个人修改的模组先按目标运行时及完整新版组合检查。声明检查通过时保留；已确认不兼容且存在可靠作者版本时使用作者新版。普通作者升级／移除不因“允许保留”被标成冲突，作者动作仍为默认。
- 个人新增同样检查新版兼容性。缺失前置先按精确平台证据准备兼容候选；补全不替换作者项目，实际候选还须满足本地版本约束。确定不兼容的个人模组在备份后禁用，保留文件用于恢复，不自动删除。未知声明明确提示，不当成已确认兼容。
- 原作者禁用模组有新版时也更新文件，在预览中独立确认“更新后继续保持禁用”或启用新版；默认保持禁用。启用目标占用或最终组合检查失败则拒绝提交。
- 本地新增或运行生成的内容不自动成为作者文件。个人文件占据作者目标路径时，未确认的归属不因同字节内容被自动收编。
- 同哈希多候选、一份本地文件对应多条作者记录、同项目多个版本候选：提示待确认，不自动选择、删除或替换。
- 重复自身 ID／有效提供别名与同一作者项目升级分别判断。预览前计算最终保留集合及新版文件之间的冲突；提交前按实际选择复核。

本地内容预览覆盖 `mods` 顶层文件，以及 `config`、`defaultconfigs`、`resourcepacks`、`shaderpacks`、`datapacks` 内容；不会把存档、运行库、账号数据或 PCL 日志目录扫描成个人模组。无法判断来源的新增配置会明确显示可能由运行生成。

## 作者记录 schema 2

`PCL/ModpackOrigin.json` 的 `files` 保存作者声明的完整预期内容，包括未安装、禁用、修改过的条目。`hash` 始终为原作者哈希，不用本地修改后的内容覆盖。`identity` 是平台项目；`fileIdentity` 是具体文件／版本。

`state` 与 `local` 分别记录实际状态、路径、观察哈希及归属标记；这些观察不是新的作者声明。`local.ownership=personal-added` 或 `ambiguous` 跨更新保留，防止第二次更新误认归属。新版清单之外保留的旧作者文件记录在 `retained`，不提升为新版作者文件。

schema 1 可读，但旧版首次记录可能裁掉缺失或已修改文件，必须从正确旧包重建完整声明。更新时，若记录保存了 Modrinth／CurseForge 的项目和当前文件／版本 ID，会自动定位并下载该原包。Modrinth 仅选唯一主文件，不把签名附件当成整合包；同时核对项目归属、精确版本、下载大小与哈希。

原包仍须匹配现有 MC、加载器、已知包名／版本及每条留存的作者哈希和平台项目。支持按记录的 MD5、SHA-1、SHA-256、SHA-512 校验。旧 Modrinth 下载链接的版本目录可能使用带点号、加号或转义的版本名，仍保留项目身份，无法确定的文件／版本身份留空。

只有原包完整声明用于生成 schema 2，再与本地实际文件做三方比较。补全只在内存和任务暂存目录中进行；玩家确认更新后，作者清单与文件变更在同一备份／回滚事务中提交。下载取消、校验失败、预览拒绝不提前改写作者清单；处理期间原清单发生变化则要求重新检查。

已有完整 schema 2 不重复下载原包。没有作者记录、缺少精确来源信息或自动校验失败时，保留选择原包的流程。仅 MC 相同不足以认定正确旧包；损坏结构、越界路径和未知哈希算法均拒绝。

个人依赖补全与个人模组禁用先在任务暂存目录准备，最终与作者变更在同一更新事务中提交。补充依赖不进入作者 files；作者已移除而个人保留或禁用的旧内容保留来源映射。失败回滚若发现目标已被外部修改，则保留外部内容、备份及待恢复日志，不宣告完整回滚。

## 预览布局与滚动

预览标题和分类保持可见，来源与兼容性详情默认折叠，与文件条目共用同一滚动区。展开长提示不会挤掉文件列表，也不嵌套独立的详情滚动条。分类优先使用一行，真正窄屏时换行；个人冲突的保留／作者选项并排并按需换行。冲突分类中的模组卡片在宽窗口使用两列，窄窗口恢复一列，每个模组的选择独立保留。内容占满弹窗可用宽度，底部更新与取消按钮保持可见。

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

### 启动检查与个人覆盖

启动检查从实例版本目录读取作者清单，从实际隔离游戏目录读取模组；二者可以位于不同目录。完整 schema 2 才能确认作者声明之外的个人新增。旧 schema、损坏记录或只有原包清单线索时仍可报告实际依赖问题，但不自动修改来源无法确认的组合。作者清单是来源证据，不会豁免已知依赖错误。

作者原组合本身不会仅因平台列出依赖而补装。个人新增文件先使用整合包已有、实际满足要求的前置，包括加载器选中的嵌套提供者；已经满足时不额外查询和下载顶层替代文件。确有未满足的个人必需依赖时，才规划补全，或在用户确认后启用已禁用的个人前置；作者禁用／未选组件不自动启用。

个人模组需要更换作者前置版本时，只有能唯一确认原作者文件和平台项目的情况才准备替换候选。界面明确显示原文件、候选及“验证并替换后启动”。确认后以整合包和个人模组的最终完整组合检查依赖、版本、加载器、自身 ID 与提供关系；有确定错误或仍无法确认的声明就不自动替换，也不在失败后静默换另一个作者版本。

已确认的替换、新前置、旧文件处理以及 `files[作者路径].local` 的个人覆盖记录共用备份／回滚事务。原作者 `hash`、`identity`、`fileIdentity` 不变，新增个人前置不进入作者清单。下次整包升级仍比较旧作者声明、本地覆盖和新作者声明；兼容的新版作者文件可以重新采用，保留覆盖则继续记录真实本地路径，不以本地内容冒充作者原版。

整包更新也检查最终选择形成的完整模组图。作者新版或移除操作会破坏个人模组依赖时，可以在预览保留当前版本，再重新检查组合。确定错误拒绝提交；无法确认的声明须另行明确确认，不能称为已经验证兼容。来源文件、模组内容与运行时变化会使旧接受记录或暂存方案失效，包括同大小、同修改时间的内容替换。

当前来源分析对无法按加载上下文完整归属的版本子目录采取只读策略，不将其额外文件自动认作个人新增。声明层面验证也不涵盖模组未声明的运行时冲突，实际整包仍需实机检查。

核心 fixture 使用真实生产引擎、有效 JAR 与有效整合包，只替换网络运输。运行：

```powershell
dotnet run --project tests/PCLLF.Migration.Tests -c Release -- --group pack-update --group mod-dependencies --group mod-dependency-online --group environment-history --group instance-clone
python scripts/tests/Test-MigrationSelection.py
```

Windows 产品构建后，实际 Main／WPF 预览与更新任务回归入口：

```powershell
dotnet run --project tests/PCLLF.Native.Facade.Tests -c Release --no-build -- --groups modpack-update,launch-dependencies
```

Windows 实际运行和玩家完整整包验收分别记账；Linux 交叉编译不作为 Windows 运行通过。玩家验收应检查 AQS 升级后的最终文件列表、Lithium 单份纯改名的升级结果、个人新增／禁用状态、选项与运行时告警，以及再次更新时的归属。没有取得玩家完整包时，不宣称已验证其实际游戏兼容性。

测试包通过 `scripts/Build-Release.ps1 -Slim` 构建，默认 `dev_edition`，需要 .NET 10 Desktop Runtime x64。不加 `-OfficialRelease`，不自动发布 Release。公开发布另遵循 [发布约定](PUBLISHING.md)。
