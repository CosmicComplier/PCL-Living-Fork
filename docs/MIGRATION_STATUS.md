# 迁移状态与续跑入口

目标见 [MIGRATION_GOAL.md](MIGRATION_GOAL.md)，完整背景见 [MIGRATION.md](MIGRATION.md)，模块对应关系见 [MODULE_MAP.md](migration/MODULE_MAP.md)。本文件是持续任务的进度账本；每次执行后在原条目上更新，不重复建立迁移计划。

## 当前状态

- 目标状态：`in_progress`。资源整页／回收／在线更新／存档备份、按键整页与近期世界Quick Play链均已按各自实际Windows证据验收；LF全C#库.NET10/WPF组件已双配置运行通过。C011环境历史／分析／还原点整页已在原Windows链验收；当前C026原生动画、C027a控件族与C012克隆整链集成，主程序与全产品现代化未完成。
- 集成分支：`migration/csharp-modern-dotnet`。工作起点为 `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`；`main` 不参与提交或合并。
- 最新已验收检查点 `3f04b63346a23dfeee4a9b301cc0778d7aec07fa`：Modern41804541772 Debug126488198216／Release126488197906各11步成功，真.NET10／Shared、四精确资源bytes、缩略图／按键保存／decoder运行通过；旧Build41804541653 Debug126488274095／Release126488274011各18成功23跳过，只按键／截图＋常驻门禁复验。C006近期动画／Quick Play使用同产品树b7493b8已成功的前序步骤，未把整个失败job写成功；先前完整388／439与严格oracle见8c706ee。
- 当前交付批次：C014在02ba2b2通过实际Windows原消费者、411／462纯样本及现代JSON严格v7；b7c8172现代两配置实际前台线程child通过。六子代理的大模块代码已统一接入：C015／C016／C017a-b／C018／C019a／C022／C024a／C025／C026b／C027b，随后C020a-b两个完整模块组；两个Core与两套Presentation及现代WPF测试图均Release警告视错编译通过，原产品Windows整体验证与新行为样本尚待本批。
- 用户补充验收要求：完整功能链迁移与LF兼容必须同时满足。恢复时先按可运行闭环梳理C007剩余工作及必要宿主前置；C007d2是内部实施起点，单个方法／规则和局部测试通过不代表整条资源功能链完成。兼容矩阵、真实Windows回归及未验证边界随交付同步记录；目标已按用户指令恢复。
- 用户补充测试要求：按受影响模块／下游／功能链执行；跨链高影响或范围不明的公共契约／框架变更及发布前才全量，纯文档不跑产品测试。G005原分组已验，当前17组（另增设置／校验Loader／Java独立组），resources始终只4组；精确源只选实际下游，不拉无关Store大集。C026公共动画与C012新增Shared测试依赖／路由合并一个必要完整检查点，之后局部修复定向。
- 执行路线已按用户要求调整：以完整可运行功能链＋LF兼容通过交付，同类源码批量转换，优先适配固定CE现成成果，集中解决编译／集成差异；实际宿主／控件／现代工程阻塞纳入本轮前置，不长期绕开。小提交保留审查价值，减少重复扫描、零碎研究、重复全测和说明性提交。
- 恢复原则：复用既有清单与证据，只整理当前整链范围和必要依赖；先小范围完成G005，再从C007d2推进资源链及所需宿主前置，按需更新相关任务依赖并保持无环，不重新规划全仓库。旧子任务号作为内部步骤，已完成项和真实失败证据均保留；目标与自动续跑按用户指令恢复。

## 证据与基线

| 项目 | 实际结果与证据 | 适用范围及限制 |
| --- | --- | --- |
| LF／PCL 来源 | LF `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`；PCL `5ef81e490ae46b1100d61e33d0183094c7a80ca8`；`git merge-base --is-ancestor` 返回 0 | PCL 基线在 LF 可达历史中，完整历史 checkout 可重复清单；不读取工作树改动 |
| CE 来源 | `v2.15.1` / `1a7653127d578efdb2a9388999a3fdc67d9e49d5` | 同源参考；具体复用仍逐文件记录 |
| 初始源码清单 | [source-inventory.baseline.json](migration/source-inventory.baseline.json)：249 个源文件、62,474 行；VB 168／55,900 行，C# 10／1,034 行，XAML 71／5,540 行 | Git 固定树统计；其中产品 248 文件，更新桥工具 1 文件。不是迁移完成度百分比 |
| 相对 PCL 同路径变化 | 新增 66，修改 67，未变 116；另有 2 个上游源路径在 LF 中移除 | 按 Git blob／mode 比较，不推测重命名；详细路径见 JSON |
| 项目声明 | 原 VB 项目显式项、PCLCS SDK 默认项；4 个 LegacyLink 条件源保留；唯一无项目声明源为 `scripts/update-bridge/Program.cs` | XML 源码声明，不是 MSBuild 求值；生成凭据项保留为未求值表达式；更新桥由构建脚本动态生成项目 |
| 初始 Windows 自动化 | [Build run 41656492901](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41656492901)，Release job `125988076604`、Debug job `125988077228` 均 success | 外部 GitHub Actions 证据，每 job 40 步成功、含 25 个 Verify 步骤及 legacy bridge；不是当前 Linux 本地运行，也不证明迁移后 GUI 或账号端到端已验证 |
| 清单工具重复运行 | 固定 LF `--ref` 输出成功；CI 基线核查批次独立重复运行并与提交候选基线逐文本一致 | Python 标准库和 Git；无时间、机器路径、凭据或子模块内容；新批次 `--ref HEAD` 需在提交后另产当前清单 |
| 首批 C# 解析器测试 | `dotnet build tests/PCLLF.Migration.Tests/PCLLF.Migration.Tests.csproj --configuration Release --no-restore -warnaserror` 零警告／零错误；`dotnet run --project tests/PCLLF.Migration.Tests/PCLLF.Migration.Tests.csproj --configuration Release --no-build` 27／27 组通过 | Linux .NET SDK 10.0.403，编译链接真实生产解析器；覆盖现代／旧世界、16 字段、signed 值、UTF-8、未知类型、截断、短路、64 层与 16 MiB 边界及流所有权；不代替 Windows 外壳与 GUI 回归 |
| 现有辅助库编译 | `dotnet build PCLCS/PCLCS.csproj --configuration Release -p:EnableWindowsTargeting=true` 成功，零警告／零错误 | Linux 对现有 net48 的 PCLCS／MeloongCore 编译验证；不是 WPF 主程序运行结果。修正资源 Include 的磁盘大小写，原 LogicalName 保持不变 |
| 首批独立自审 | NBT 旧语义、PCL.WorldMetadata 16 字段／类型及 Read／ReadDetails／Describe 反射入口保持；所有字段映射完整，Describe 文案未变 | 源码逐段对照与独立复核；后续 C002／C003 实现也已通过下列各自 Windows／迁移矩阵，不能将这一结论外推到尚未实施的模块 |
| C002／C003 当前清单 | `--ref cfd4cbf23bb276fba800e930f8cb67c39b11c426`：255 个源文件，其中产品 251、测试 3、工具 1；产品 VB 仍为 168 文件／55,692 行 | 当前清单在忽略的 artifacts 下重新生成；初始基线不改写。兼容门面仍是产品 VB，不能按 C# 增量或总行数声称完成度 |
| C004 当前清单 | `--ref 1c1841f00298579fca865e703af04b8bc08fc521`：261 个源文件，其中产品 254、测试 6、工具 1；产品 VB 仍为 168 文件／55,540 行 | 已解耦的核心不代表 VB 适配、页面、控件或现代主工程已转换；最终源码归零仍由 R001 实际核查 |
| C005c1 当前清单 | `--ref 2f60f73afb085912708789551923d3de988f1751`：267 个源文件，其中产品 257、测试 9、工具 1；产品 VB 仍为 168 文件／55,371 行 | 仅以提交树核实当前范围；页面其余后台尚未迁移，不据总行数或 C# 增量计算完成率 |
| C006c 当前清单 | `--ref 6023f3dc3c0c544a8d7c12d189d676e90fb3377e`：273 个源文件，其中产品 260、测试 12、工具 1；产品 VB 仍为 168 文件／55,284 行 | 固定初始清单不改写；适配门面、控件和主工程仍是待迁产品源码，不据本表计算完成率 |
| C007a 当前清单 | `--ref 459bb0a8f8b1112187e884f1a9985014e58dc970`：275 个源文件，其中产品 261、测试 13、工具 1；产品 VB 仍为 168 文件／55,270 行 | 八数据字段和一个校验核心已迁，Icon／全局路径／Store 其余及主工程仍待；不是完成率 |
| C007b2 首批当前清单 | `--ref 8c64fcc52f6468ea254f73f8368d81b39f428538`：283 个源文件，其中产品 264、测试 18、工具 1；产品 VB 仍为 168 文件／55,263 行 | 总 VB 169 包括测试专用历史驱动 1 个；不算产品迁移增减，固定基线不改写，非完成率 |
| C007c1 当前清单 | `--ref 8734df5707aed5e30944a4609ef516ffaac5c6c2`：285 个源文件，其中产品 265、测试 19、工具 1；产品 VB 仍为 168 文件／55,261 行 | 历史驱动仍只计测试；枚举与门面未删除，固定初始清单不改写，不据此计算完成率 |
| C007c2 当前清单 | `--ref 2d111e141fec34d6f284c1060884bf1ac02290dc`：287 个源文件，其中产品 266、测试 20、工具 1；产品 VB 仍为 168 文件／55,258 行 | 原门面和绝大部分业务仍待转换；新增测试／C# 行数不算完成率，原固定基线保留 |
| C007c3 当前清单 | `--ref 70f254528e70f4b62aeb4c1514861b20fb458665`：290 个源文件，其中产品 268、测试 21、工具 1；产品 VB 仍为 168 文件／55,248 行 | 只迁资源筛选及共享已验证的原字符检查；所有门面仍计产品，固定基线不变，非完成率 |
| C007c4 当前清单 | `--ref a89bc041412b4382eb68e1ea4053127ec973874a`：292 个源文件，其中产品 269、测试 22、工具 1；产品 VB 仍为 168 文件／55,245 行 | 原历史测试驱动只计测试，C#／测试增量不作为完成率；主工程／界面与其余业务仍待转换 |
| C007d1a 当前清单 | `--ref 745f0b845139bad953a03eed70288506c5bcc1c3`：294 个源文件，其中产品 270、测试 23、工具 1；产品 VB 仍为 168 文件／55,242 行 | 私有门面仍计产品；源码基线与真实 GUI／现代主工程待办不变，非完成率 |
| C007d1b 首批当前清单 | `--ref 4eb50fbbc884fb284aac7377a897aaef4176f8d2`：295 个源文件，其中产品270、测试24、工具1；产品VB仍168文件／55,236行 | 原控制流提取与新测试不作为完成率；原固定基线不改，真实现代主工程／UI仍待 |
| C007d1b 已验当前清单 | `--ref 476eb42bb00e85b7cf8d4f800d1264740b8373c6`：299源文件，产品273／测试25／工具1；产品VB仍168文件／55,236行 | 历史驱动只计测试；局部numeric适配与测试增量不是完成率，固定基线不改 |

当前资源批次实跑：`0bee590a0bc1c11da5cced77dc065eb0cfe321bb` 的Migration validation `41790862804`成功（Ubuntu job126443749905／Windows126443749914），v7实际397例／56候选／14双向ordering样本严格无差异，artifact13303231765已下载核对，ZIP SHA256 `ff1d711542b849fc860035f8aaa5072685a88a49017794db19ed8e4d10718d3f`。Build `41790862956` Debug126443726271失败、Release126443726299 cancelled：原资源／地图回收／更新／备份／Keybinding／路径等消费者以及修复后的G005实际coverage检查均已通过，最后截图验证的Add-Type缺System.Xaml引用而失败。补的是测试编译引用，生产未改；补齐引用的b1357f7实际Build41791636627成功：Debug126446183687／Release126446183725各18步成功、23步跳过，真实异步截图WPF检查通过；Migration41791636538验证job按路由skipped。仅截图检查＋常驻构建／凭据／旧桥门禁重跑，其余以0bee590对应已运行步骤为证。

资源整页／C008首轮 `271d0589273596f45959670b9fa8eccb91a006b4`：Migration [41794871750](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41794871750) 成功，Ubuntu126456557787 **381／381**、Windows126456557846 **425／425**，严格v7仍397例无差异；六个新增Windows回收／地图真实文件组已执行。Build [41794871590](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41794871590) 的Debug126456567159／Release126456567314完整产品编译成功，资源／地图回收／更新等前序检查成功；Release在Test-SaveBackupIntegration失败、Debug同一步被取消，原因是测试hook仍假定WorldBackupPoints等三项在页面上有转发属性，实际已整体进入Controller。修复范围限定测试读取真实Controller状态和BackupGroup.Element，不增加仅供测试的生产私有属性、不删行为断言；后续未运行步骤不记通过。固定提交当前清单311源文件，产品283／测试27／工具1，产品VB仍168文件／54,335行，非完成率。

`0df889ab3d6aa1695981855ebf6d5e8ca5df8c58` 修正后的完整Windows [Build41795970888](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41795970888)成功：Debug126460111396／Release126460111508各41步成功、0跳过，真实Save/backup integration、回收站工具栏／选择／还原、原Screenshot gallery＋C#异步控制器及旧桥均通过。Migration [41795970903](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41795970903) Windows126460120303／Ubuntu126460120400成功，严格oracle通过；本提交无产品修改，生产同271d058。C007／C008业务与当前原控件适配闭环可关闭，最终VB源码清零／现代WPF／真实游戏和账号验收不在该结论内。

C009／C010联合验收：`8c706ee11d705006f6ce9055532214c6da5aef85` 的 [Build41798578440](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41798578440) Debug126468571779／Release126468571785各41步成功、无跳过，实际Test-ResourceUpdates（原LF模型）、WorldBackups产品＋EngineOnly各自完整故障fixture、SaveBackupIntegration、截图与旧桥均通过。[Migration41798578476](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41798578476) Ubuntu126468575296为388／388，Windows126468575178为439／439、严格v7 397例零差异；Ubuntu只按OS跳过3个Windows专属工作流步骤，不记成运行。当前固定树318源文件，产品VB仍168／53,623行，不据行数或测试数计算完成度。

C005c2首轮 `ebdb594dc8e6cd29fea654a8a4689eb9e5ff6090`：Build41799421556 Debug126471315206失败、Release126471315247取消；完整产品编译／LocalGameFeatures／GameFeatureRegressions与原native分组检查通过，新C#5 verifier Run因KeyEventArgs构造参数inputSource为null抛ArgumentNullException，未进入期望ESC断言。不是生产行为改动；修测试为using隐藏HwndSource提供实际PresentationSource并展开C#栈，原assert保留，随后随现代共享源复验。Migration41799421563定向按键组成功，其余未选不记通过。

现代首轮 `b7493b80dfd40be856be65f4e13507dd8713f506`：Modern WPF41803410215 Debug126484450166／Release126484450642实际build成功并通过真.NET10／Shared加载、四反斜线资源名／字节和共享缩略图异步验收，之后按键fixture在设置Handled时因未指定RoutedEvent失败；图片decode最终段未执行，不记通过。旧Build41803410094 Debug126484522335失败、Release126484522290取消，产品编译及此前资源／备份／近期卡片等通过，同一共享fixture触发失败。Migration41803410117成功。补测试真实PreviewKeyDownEvent（原assert不减），截图Run也展开C#底层异常供以后定位；只选按键／截图两旧消费者＋现代双配置，不重复全量。

`3f04b63346a23dfeee4a9b301cc0778d7aec07fa` 实际复验：[Modern41804541772](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41804541772) Release126488197906／Debug126488198216各11步成功，全部Core／Shared／4资源／缩略图／按键options／image decoder检查通过。[旧Build41804541653](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41804541653) Release126488274011／Debug126488274095各18成功23跳过，只key／screenshot与常驻编译／凭据／旧桥，未跑其余组不记通过。Migration41804541868选择126488197829成功，验证126488318743按范围skipped。近期世界链使用b7493b8中两配置已成功的原Quick Play／recent-world animation步骤（后续因独立Key fixture导致job失败），git产品源码与3f无差异；C005／C006／C029a／N002a的当前边界可以关闭，完整主产品仍未现代化。

C011／控件批次实际发布采用GitHub API正常fast-forward：Git smart push三次500，API逐树核验源码哈希不变后发布17270c60e88b9b47d8590030289c285c05d3b818（原未发布34db0f3，tree ec0895f13d94eeee4d24dca9f2382235c6930348）及ee5290299e3938d2ae61fbc1d0038af7d84da86e（原未发布1ca333b，tree 8b4f1adcb0c0b8ae1c6ba412e38fb6262b378746），详细消息原样；仅API作者／时间元数据不同。旧已发布历史不改、无force，local通过同树keep对齐且原未发布对象有本地bundle备份。ee52902 Modern41810651886两配置成功，Migration41810651783双平台成功；旧Build41810651741产品编译成功，cold子进程已实际通过新DLL嵌入SHA／空Location／identity／MVID，随后Add-Type把EXE路径当程序集名而失败（Debug126508834614／Release126508833994取消）。控件6组及此后消费者未执行，不记通过；下一批修compiler-only引用并完整重验未完成的公共控件／环境链。

ee52902原生控件cold资源已PASS，Add-Type引用主EXE路径报0x80131047（非生产DLL加载失败）；修为cold成功后将已加载EXE同字节另存compiler/<真实AssemblyName>.dll供编译，并核元数据MVID及verifier仍绑定原EXE实例。六组断言未改。新控件gate移到原所有消费者／旧桥之后，两配置不因另一cfg夹具失败取消，保完整独立证据；步骤命令／条件逐对象不变。这次涉及公共新控件未完成回归及C011b真实新页面，执行一个必要完整检查点，后续局部修复仍定向。

cbf8063完整复验：Modern41812928568与Migration41812928614成功；旧Build41812928561两配置编译后在原InstanceResources PageGet真实创建MyCheckBox时XamlParseException（Debug126516483009／Release126516478251），非测试编译引用问题。新增族gate在末端因默认success未运行，仍无完整ctor内层原因。下一诊断保原assert，完整输出资源页异常链，并在已成功编译且未取消时让末端族gate即使前序消费者失败也执行，以取得独立cold构造／事件原始异常。不得把公共控件批次写成通过。

aa72efd实际诊断（Build41813603772 Debug126518768470／Release126518767208）：原资源消费者及末端cold族Run均显示FileNotFoundException PCLLF.Presentation.resources→ResourceManager／ResourcePart→新view InitializeComponent。现原StartsWith白名单误把optional satellite当neutral DLL返回，阻断neutral BAML回退；修仅新库精确.resources simpleName／displayName返回Nothing，旧所有前缀／缓存／其它satellite策略不变。目前尚未到达完整构造／六族行为验收，后续仍须查实际新异常；不提前据框架源码推测通过或修改视觉。


5b5dda6实际复验：[Build41814330441](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41814330441) Debug126521255158／Release126521203195仍失败，Migration41814330469成功；satellite修复后已进入BAML加载，原资源页MyCheckBox与cold族MyButton均报实际派生类型没有对应资源URI。WPF公开Application.LoadComponent(object, Uri)要求BAML所属程序集等于component.GetType().Assembly，旧VB壳继承新C#view触发该限制。新增仅跨程序集构造使用的CompiledControlLoader，通过公开Baml2006Reader／XamlObjectWriter把同一编译BAML装入同一旧壳实例，保ConnectionId／原事件／资源URI／NameScope；现代同程序集构造仍走原InitializeComponent，不改XAML视觉或降低六组断言。两真实WPF项目Release已在Linux SDK10.0.403以--no-restore -warnaserror编译，均0警告／0错误（该本地源码图还含下一批已冻结C026动画源，隔离提交树以远端CI为准）；真实Windows页面／cold六组仍待本次修复验证，不据编译推断运行通过。


C012a后端成组迁移：原InstanceCloner／InstanceCloneGroups／ExportedVersionIsolation的实际业务移入六份C#引擎／Workflow／契约，三原VB只保真实类型与LF事务／INI／缓存／Worker／锁的typed平台接线。来源为LF f4…且分别核对8d61586e4627102ac2e360ccc7d4e092d8797e2f／202aa3b4e7703b61865c875c9e6d98d993e7f18c／f1b16ce83b7e272a6d34220566e43d89a67afd09；固定CE稳定参考没有这一LF专属等价链，未复制CE实现。新增六组真实生产测试，不模拟原事务回滚算法；实际Release -warnaserror 0警告／0错误，--group instance-clone --group environment-history定向11／11，四种无效CLI选择均exit2且无PASS。根测试显式排除Shared传递JSON资产并保留原LF HintPath，实测ReferencePath／CopyLocal皆唯一LF DLL、逐字节SHA E1E27AF7B07EEEDF5CE71A9255F0422816A6FC5849A483C6714E1B472044FA9D；Shared固定提交与自身包构建不变。9精确生产路径选择第12组与7真实克隆／隔离／历史下游消费者，原43脚本与资源4组不变；路由自身／公共项目仍full。实际net48 Core Rebuild首次CS8620（Tuple.Create可空泛型推断）已以显式三泛型参数修复，随后Core与纯测试Release -warnaserror均0警告／0错误，改后克隆6／6重新通过；不改元组值或事务顺序。两页面由C012b继续，Windows真实迁回、回滚、锁与设置缓存待CI。


98f81fe实际[Build41816180616](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41816180616) Debug126527445833／Release126527445668：产品编译、原资源页／备份／历史／克隆／账号相关旧消费者及旧桥均成功，仅末端新增控件族StaticApi断言失败“Original InlineCollection forwarding changed”。这次已实际通过compiled BAML构造、原XAML属性、所有原页面消费者；冷EXE仍通过嵌入identity／MVID／SHA且无sidecar。原LF Inlines getter就是每次返回LabText.Inlines，WPF返回包装集合，ReferenceEquals包装器不是原行为契约；新断言改为三个真实控件各自双向Add／Contains／Count＋清理恢复，验证同一视觉内容而不缓存包装器改产品。其余六组断言保留，未到达的事件／逐帧组仍待验。Modern41816180601 Debug126527386734／Release126527388787成功（C026尚未在该树），Migration41816180595双平台成功。


C026a完整动画成组迁移：八份Animation契约／数据／工厂／缓动／Engine／ThreadDriver／NativeHost／NativeRuntime与原ModAnimation公开API壳。实际固定LF与PCL的原ModAnimation逐行diff为空；CE稳定1a765312…的Plain Craft Launcher 2/Modules/Base/ModAnimation.cs:1200-1467（CE LICENSE及PCL LICENCE）仅同源C#／缓动参照，保LF原Dictionary、StartTick与AniLastTick、Uuid同名重建、After原位重试、结构体写回、signed颜色余数及完成SetResourceReference、三毫秒gate／double delta／前台线程／Sleep(1)／绝对AniControlEnabled setter。Shared Lerp保七位，MyColor通道Lerp保六位，CompareTo clamp保NaN原行为；NativeHost保实际WPF／时钟／VB Conversions／窄late fallback，GB18030只局部请求provider不改全局编码，null color保白色。旧私有_AniControlEnabled的两脚本三处仅改公开property hook，不减断言。两呈现项目及完整现代test图Release --no-restore -warnaserror均0警告错误，作者＋独立审未见行为回归。新五组真实NativeHost／Runtime／Factories／Engine与三控件中帧／终态、取消、After／延迟／重建、颜色资源／余数、回调故障恢复已接入，未启动原无限前台线程；Linux只编译，Windows实际运行与原产品消费者待同一完整检查点。


859fd4f集成首轮：Modern[41817913425](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41817913425) Debug126533323044／Release126533323134各11步success，新增5／5实际NativeHost／Runtime动画组（含三真实C#控件中帧／终态／取消／After／颜色余数／资源／异常）及全部原现代组件断言实际通过。Migration[41817913498](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41817913498) Ubuntu126533400340／Windows126533400410在编译前报CS0246 MeloongCore：纯runner新Shared ProjectReference先于原仅Windows oracle初始化step，子模块未checkout。两OScase未执行，oracle随后未运行，不记通过；保固定Shared哈希检查，将同一初始化命令移到纯测试之前，条件覆盖任意非空分组或Windows oracle，不改测试／产品逻辑。旧[Build41817913454](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41817913454) Debug126533411185／Release126533413538均42步success、无skip／failure，原克隆／真实事务回滚／stale、来源分组懒页／新增真实克隆页、导出隔离／缓存、环境还原／失败／取消及旧桥成功。末端standalone EXE无sidecar身份／MVID／嵌入SHA与六组原控件／主题／真实AniTimer均实际通过（Debug DLL SHA 2c5595123a0e117b5b04d25e5ba6fca671c1ec5cc6067bcd66631ebfad233a7e；Release b022de114f5bada125f6158fab0ff81a699863521f7bad042b13c31554942760）。手工hover／焦点／视觉手感和完整主产品现代化仍待后续，不能以自动化外推完成。


b84e6d2实际手动复验全成功（e6e0470与b84普通push收到远端但未产生Actions运行，gh CLI dispatch返回JSON解析错误，改用明确JSON HTTP API发送得到204并实际生成以下run，不据请求成功代替结果）：[Migration41819921743](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41819921743) Ubuntu126540340319为405／405、Windows126540340579为456／456，13canonical组全部运行。v7 RequireEquivalent：397case／5link／56native／14ordering、equivalent true，contract／native／diff／info全空；artifact13317725089 ZIP SHA b112b3ef5b0ff15835e00c8aeefa5c6cee7e569020042e0974627a76f1581688。Modern[41820061072](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41820061072) Debug126540707392／Release126540708289各11步success，全部资源／thumbnail／按键／decoder与真实动画5组通过。Legacy[41820172267](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41820172267) Debug126541161573／Release126541161273各42步success无skip，实际Inspector93／Online72原断言、Graph100节点与引用／icon／dialog、LaunchDeps修复／校验／取消／stale／回滚、Activation启用复查／歧义／混合回滚及六cold控件／旧桥均通过。两noEXE fallback模式未被workflow调用，单独模式实跑仍后验；C026真实无界worker当时也未启动，不以5组手动帧外推。C012／C013在已述生产模块范围验收，整产品主窗／账号／游戏及最终VB清零仍未完成。


C014本地集成：四C#完整pipeline／hash／匹配／HTTP与原50行typed VB壳保旧Options16字段、缓存单份、private HttpFailure及原签头ByRef语义。net48／modern Core＋原Online完整stub的实际Strict桥编译0警告错误，独立终审未见规则差异。新六组真实C13报告／文件／平台响应样本首轮出现5失败、总7／12（本地Inspector6全过）：FirstChance实证是测试加载原Framework Newtonsoft后首次JArray.FromObject/ToObject缺System.Security.Permissions，原Enrich正确降级致后续阶段未执行，不是生产匹配规则错误。只预加载固定NuGet13.0.3/net6.0资产的同一测试DLL六组原断言全部成功，未改golds或业务catch。正式.NET10纯测试改与modern/PCLCS同PackageReference13.0.3，独立CLR4 oracle继续使用固定LF DLL；现代oracle也改用实际现代包并记录程序集framework／version／SHA，旧LF源码／config／manifest／DLL与所有397case不改。完整14组正式运行411／411，Release0警告错误；现代测试与oracle实际copy SHA均22c649f75fce5be7c7ccda8880473b634ef69ecf33f5d1ab8ad892caf47d5a07，旧LF仍e1e27af7…44fa9d。该全量纯test是明确共享序列化目标变化的必要复验；Windows现代JSON与CLR4严格比较、原在线／Graph／启动消费者及新增实际thread-driver子进程待本批CI，不能将编译或孤立预加载算成已通过。现6子代理分别准备C015、C016整条UI、C018资源、C017 Java、C024校验与C026图像颜色几何，完整稿互斥、主执行者统一build／测试／提交，准备稿尚不记完成。


02ba2b2实际自动CI：Migration41825028416 Windows126557566519为462／462、Ubuntu126557566806为411／411，14canonical组均实际执行；现代JSON包资产下v7严格397case零baseline／native／path／text差异（实际modern端已与产品同13.0.3包，原LF CLR4 DLL不变）。旧Build41825028388 Debug126557526963／Release126557527028全success，原在线／图／启动消费者、真实克隆页、六cold控件及旧桥通过。Modern41825028405 Debug126557455671／Release126557455884：全部原组件及动画1～5实际通过，新worker child已真启动并验证线程／STA，随后终值断言失败，Release实际0.9999979999999995。原LF每帧七位四舍五入累积不保证增量1最后恰为1；保持全部断言／Near阈值和生产算法，仅把worker样本增量改1.25，使用原[0,1]clamp让期望终值1成为真实LF约定，仍要求中帧／callback／foreground／priority／UI。该fixture修复仅现代测试定向复验，不重跑已通过业务全量。新并行C015／C016／C017／C018／C024／C026代码另批集成，未把未提交源码混计到本次运行。

本轮已推送的实现与验证检查点：

| 检查点 | 修改及实际验证 |
| --- | --- |
| `c3a2838` | 固定 goal、提交／PR 策略、LF／PCL／CE 模块映射、可重复源码清单和进度账本；后续规则按用户补充继续完善 |
| `e47fc55` | 修正 OptiFine 哈希资源磁盘路径大小写，保持 LogicalName；现有 net48 辅助库在 Linux 编译通过 |
| `bdad3a7` | 将 NBT 引擎迁入 `PCLCS.WorldMetadataReader`，保留 PCL.WorldMetadata 字段和生产入口；新增真实 NBT 测试 |
| `d27f394` | 新增限定迁移分支的 .NET 10／源码清单 CI；以下两次远端运行均针对该完整提交 |
| `c7cb287` | 固定详细 commit／PR 正文要求、模型边界和 .NET 10 兼容审计；现代运行门禁仍保留 |
| `0388bd6` | C002a 文件／gzip／详情文案迁入 C#；15 组真实文件样本加入既有 27 组测试；Windows 原详情脚本增强全部字段、精确格式、共享／只读及真实符号链接检查 |
| `cfd4cbf` | C003 持久化／原子写入／进程身份迁入 `LocalGameDataStore`；保留原 Gate／动态 TestRoot／所有入口。新增 20 组跨平台实际样本与 2 组 Windows 文件语义；EngineOnly 夹具及其专门 CI 接通 |
| `8a9cd4f` | 固定 C002／C003 已通过的完整远端证据，关闭对应核心与适配验收；拆出 C004a／b／c，原产品仍为 net48 |
| `758a3c2` | C004a 个人选项纯规则迁入 `PersonalOptionsRules`，保留原动态设置；窄适配 .NET 10 的 existing FOV 非有限解析变化，新增 28 组规则测试和 42 条 CLR4 原始解析 oracle |
| `35114dd` | C004b options.txt 原始读取和合并写回进入 `PersonalOptionsFiles`；18 组跨平台／20 组 Windows 文件样本，保留 no-op／BOM／hash／备份及成功后 cache 顺序 |
| `1c1841f` | C004c 导入／准备／跟踪／捕获／恢复进入 `PersonalOptionsFlow`，惰性实例适配、原 Store／Gate 和派生字段 DTO 接通 23 个原入口；27 组新增真实流程样本及 Windows 原会话字段／对象状态断言 |
| `f36c6e6` | 关闭 C004 三批核心／适配验收，记录完整证据与当前清单，拆出 C005a／b／c 及原按键／标题换行约束；现代主工程和最终适配删除仍未完成 |
| `a726586` | C005a Save 迁入 `InstanceKeybindingFiles`，21 组跨平台／23 组 Windows 文件样本，原 VB 门面接通；原脚本验证 same-value backup 轮换，模型／读取／默认表未改 |
| `33a1c6a` | C005b 模型／默认／读取归属进入 `InstanceKeybindingReader`，33 组 Linux／32 组 Windows 读取 ZIP 样本；补 Framework 压缩引用，原模型字段与实际 Meloong 委托验证接通，并拆开 WPF 宿主前置与最终汇总依赖 |
| `2f60f73` | C005c1 原页面 DisplayKey／RenderList 的显示、搜索、计数与显式分组规则进入 `InstanceKeybindingView`，21 组金值验证精确比较、全局总数、原对象及输入不变；其余页面后台、控件、捕获与动画保留 |
| `8a5e707` | 关闭 C005c1 的真实 Windows 与矩阵证据，母项因整页前置 blocked；固定当前清单并拆出 C006a／b／c／d，长期目标仍在进行 |
| `046ed2d` | C006a 两路径／参数叶子进入 `QuickPlayWorldRules`，16 组跨平台／另 1 组 Windows 生产样本；窄补现代 Windows 原 ArgumentException 顺序，增强真实 Prepare 参数与 CLR4 路径 oracle，不改版本／日志／会话 |
| `f1fa26b` | C006b 历史／日志与双 DTO 进入 `QuickPlayHistory`，原四入口和派生旧类型接通；26 组真实文件样本、原 40／5 限额／两次读取／捕获边界、UTF16 BOM、mtime 完整性及 Windows 4＋5 字段／ID／UTC 消费者验证 |
| `257e98e` | C006c 原七入口进入 `QuickPlayFlow`／8 惰性 getter，31 组真实进程／文件／gzip／NBT／异常边界样本；Windows 脚本验证 live／dead／disabled probe、坏 JSON finally 和坏 gzip 原异常／字节，旧 Start／UI 回调保留 |
| `6023f3d` | 仅修正 Windows PrepareIdentity 金值的原内部斜杠写法并记录初次失败；生产与脚本树对 `257e98e` 无差异，保精确字符串断言；重跑双平台全通过 |
| `c3271f5` | 固定 C006 核心三批完整证据、保留初次失败与修正，母项待 WPF；C021 使用 C006c 核心依赖，开始 C007a／b 拆分 |
| `459bb0a` | C007a 原八资源字段进入纯 DTO，原 sealed VB 类／Icon 保留，ValidateEntry 接通原消费者；13＋1 平台样本与真实 Scan 继承字段／冻结 Icon、资源操作回归通过 |
| `028b9ed` | 关闭 C007a 的完整证据／清单，进入固定旧路径契约与真实 Windows oracle 审计 |
| `b12b6cf` | C007b1 路径两方法原控制流迁 C#，12 新组、独立固定原 VB／CLR4 与真实 .NET 10 对照及 Diagnostic CI；实际测出 8 路径＋2 Unicode 差异，母项保持未等价，见独立报告摘要 |
| `8c64fcc` | C007b2 首批路径／旧 Ordinal helper、16 Windows／8 Linux 新组与 oracle v2 已推送；实际原 8＋2 差异消除、52 Win32 候选通过，新增 9 路径边界仍待后继适配，不据 Diagnostic 关闭母项 |
| `348b0a5` | C007b2 后继设备根／UNC／leading-space 系统归约、GetDir limited 长度及原串错误顺序已推送；5 新 Windows 组与 137／52 对照全通过，新旧差异全部归零；后继启用严格 CI，不以本次 Diagnostic 模式代替门禁实跑 |
| `21675ee` | CI 及脚本默认切到 RequireEquivalent，保留全部 137 例／52 候选及原基线／差异历史；真实严格对照、308／336 单元及 Windows 各 41 步全通过，C007b2 完成 |
| `8734df5` | C007c1 两原入口与 C# 映射、三组金值及真实原 enum／Root／精确异常顺序消费者已推送；311／339 单元、严格 137／52 与 Windows 各 41 步全通过，C007c1 完成 |
| `2d111e1` | C007c2 SafeName 单函数／原门面、8 组平台规则及真实旧入口三文案／41 字符／22 保留名／120 UTF16 验证已推送；319／347 单元、严格 137／52、Windows 各 41 步全通过，C007c2 完成 |
| `70f2545` | C007c3 原私有筛选／CInt 桥与 9 白名单、原 C6 字符共享、8／9 平台组及真实旧入口样本已推送；327／356、严格 137／52 和 Windows 各 41 步全通过，C007c3 完成 |
| `dfb81b8` | C007c4 原包校验／Using／两消息、真实 ZIP 测试与 v3 150／56 对照已推送；Linux 339／Windows 371 单元通过，严格对照实测 1 项未置 EFS 解码差异、完整 Build 原消费者失败，c4 不提前关闭 |
| `a89bc04` | 依据真实 v3 报告恢复现代 Win 未置 EFS 的系统代码页解码，且 PS 只拆调用包装器保留业务异常；后继 339／371、严格 150／56、Windows 各 41 步及原 package／lazy-directory／handle 消费者全通过，C007c4 完成 |
| `745f0b8` | C007d1a 原大小显示／当前culture／GB上限与原private入口、6组金值和v4三原方法注入已推送；345／377、严格190／56和Windows各41步全通过，d1a完成，JSON规则仍由d1b继续 |
| `4eb50fb` | C007d1b 原 JSON 末段／ref 桥、13组测试和原片段v5已推送；Ubuntu358／Windows390及Windows各41步全通过，严格280／56实际18个metadata数值差异，母项不关闭；见下列真实失败与artifact证据 |
| `93c51e2` | 初次数值适配／8组回归／291例已推送；366／398单元、Windows各41步成功，原18个差异归零，但新增epsilon／normal边界／R／G15中点4差异使严格检查继续失败；不隐去首次失败或提前关闭d1b |
| `476eb42` | C007d1b局部MIT旧数值转换／独立15／17格式／旧R尾零缓冲与v6已推送；367／399、严格397／56、Windows各41步全通过。原两轮失败保留，母项d1纯显示范围完成，IO／Icon／Scan待后继 |
| [C007d1b v6 Migration validation 41754149284](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41754149284) | `476eb42`：Ubuntu job `126326151285` 367／367、Windows job `126326151418` 399／399均success，严格397／56（metadata101、parse34、format72）四失败数组空、equivalent=true；artifact `13286793149` 已下载核对，ZIP SHA256 `a17d0bcd04bae63a819debe49a224400472c9cdf99ef62899d82dd8b2461b7f5` |
| [C007d1b v6 Windows Build 41754149252](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41754149252) | `476eb42`：Release job `126326151610`、Debug job `126326151708`各41步success，实际原pack-description／ref／UTF16／error和完整资源／事务／旧桥通过；原net48产品，不替代现代WPF／全域CRT验收 |
| [C007d1b 第二次 Migration validation 41751706901](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41751706901) | `93c51e2`：Ubuntu job `126318081169` 366／366 success；Windows job `126318080583` 398／398单元通过，严格291／56仍4差异，contract／native／text失败全0。artifact `13286565797` 已下载校验，ZIP SHA256 `205d99158a44c148bb37eb29b95d7cd16ed1c73797e5cecf74e21574a7855ffc` |
| [C007d1b 第二次 Windows Build 41751706759](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41751706759) | `93c51e2`：Debug job `126318080530`、Release job `126318080805`各41步success；原net48消费链继续通过，不抵销现代数值差异 |
| [C007d1b 首次 Migration validation 41750296482](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41750296482) | `4eb50fb`：Ubuntu job `126313430696` 358／358 success；Windows job `126313431272` 390／390单元通过，但RequireEquivalent的280／56有18个path差异（均pack-metadata），contract／native／text失败均0，严格退出2。artifact `13285062521` 已下载校验，ZIP SHA256 `347ec6f0351850429fc10d3a6b0f1eaabad55a012c3c7dfe787d7f746f8f9bbd` |
| [C007d1b 首次 Windows Build 41750296507](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41750296507) | `4eb50fb`：Release job `126313431139`、Debug job `126313431176` 各41步success，实际原pack-description／ref／UTF16／error与既有资源／事务／旧桥消费者全通过；仍为net48产品，不抵销现代数值差异 |
| [C007d1a Migration validation 41747607034](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41747607034) | `745f0b8`：Ubuntu job `126304430224` 345／345、Windows job `126304430601` 377／377，均 success；严格190／56／40大小探针四失败数组全空、equivalent=true。artifact `13284023876` 已下载校验，ZIP SHA256 `dd36a4d1cbb431aa2be4c514cf8fb771438116d546ed60d3944dca880ef93798` |
| [C007d1a Windows Build 41747606964](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41747606964) | `745f0b8`：Release job `126304429811`、Debug job `126304430181` 各41步success；实际原size/current-culture/long参数及资源／事务／更新／旧桥全通过，不外推未实施的JSON或扫描 |
| [C007c4 首次 Migration validation 41743920173](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41743920173) | Ubuntu job `126292044914` success、Windows job `126292045184` failure：339／371 单元均通过，150 例／56 候选实跑，contract／native／text 失败全 0，仅 zip.utf8-no-efs-long-s 一项名字解码不同，严格退出 2；artifact `13282837230` 及哈希见对照记录 |
| [C007c4 首次 Windows Build 41743920174](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41743920174) | Release job `126292045363` failure，原 PS 资源消费者在期待 InvalidData 时失败；Debug job `126292044964` 因矩阵 fail-fast cancelled，不能写为通过。检查发现测试 helper 递归拆所有 InnerException 会丢失业务异常顶层，后继只拆调用包装器并增加 observed 类型诊断 |
| [C007c4 修复后 Migration validation 41745161924](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41745161924) | `a89bc04`：Ubuntu job `126296201993` 339／339、Windows job `126296202455` 371／371，均 success；严格 150／56／12 ZIP API 请求四失败数组均空、equivalent=true。下载核对 artifact `13283825558`，ZIP SHA256 `ec55afa9a51c681a6ede106be2c0dc3e073b2416e33c8293a250fc85ad027c78`；Frame default1252／modern default65001／modern ZIP1252已实证 |
| [C007c4 修复后 Windows Build 41745161847](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41745161847) | `a89bc04`：Debug job `126296202166`、Release job `126296202385` 各 41 步 success；原 package／lazy-directory／handle、两消息、既有资源／更新／取消恢复／旧桥消费者通过。源码及数据格式未更换为CE，原捕获／Using／事务范围不变 |
| [C007b2 首批 Windows Build 41736377697](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41736377697) | 提交 `8c64fcc`：Debug job `126266518133`、Release job `126266518659` 各 41 步 success；原资源／备份还原／更新／取消恢复、账号合成迁移、旧更新桥及发布产物检查通过，仍是旧 net48 产品，非现代 WPF 运行证据 |
| [C007b2 首批 Migration validation 41736377415](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41736377415) | Ubuntu job `126266516885` 308／308、Windows job `126266516510` 331／331，均 success；124-case 独立 CLR4／现代对照、52 项候选实跑，基线／候选失败 0、文本差异 0、新路径差异 9。Diagnostic 成功保留残差，不代表 RequireEquivalent，通过报告与 artifact 摘要可续接 |
| [C007b2 后继 Migration validation 41737707302](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41737707302) | Ubuntu job `126271080274` 308／308、Windows job `126271080675` 336／336，均 success；137-case／52 候选真实实跑，基线／候选／path／text 失败均 0、equivalent=true。新增长度、NT 父 NUL、canonical 根旧 NRE 均由真实 CLR4 证实；artifact `13280505603` 及哈希见摘要 |
| [C007b2 后继 Windows Build 41737707325](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41737707325) | `348b0a5`：Release job `126271080762`、Debug job `126271081115` 各 41 步 success；原资源、备份／还原、取消／更新／克隆及旧更新桥完整自动化通过，现代 WPF／真实账号／游戏验收仍待后续 |
| [C007b2 严格 Migration validation 41738157780](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41738157780) | `21675ee`：Windows job `126272610400` 336／336、Ubuntu job `126272610683` 308／308，均 success；日志明确 Mode=RequireEquivalent，137／52 零差异。artifact `13280565854` 已下载核对严格模式／equivalent=true 和空失败数组，ZIP SHA256 `d28ff017b1a99ead6c5020b452da27993bc7c0a6b2a2e17549d1edb0b16e1afc` |
| [C007b2 严格 Windows Build 41738157829](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41738157829) | `21675ee`：Debug job `126272610758`、Release job `126272611340` 各 41 步 success；完整现有消费者、文件事务、旧更新桥／产物检查通过。与 348b0a5 的生产代码树一致，严格门禁／资源路径子任务关闭，完整 Store／现代主工程仍未完成 |
| [C007c1 Migration validation 41738281554](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41738281554) | `8734df5`：Windows job `126273028908` 339／339、Ubuntu job `126273029104` 311／311，均 success；三组新映射／非法值金值及严格 137／52 零差异通过 |
| [C007c1 Windows Build 41738281516](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41738281516) | `8734df5`：Release job `126273029398`、Debug job `126273029740` 各 41 步 success；真实原 enum 名／Int32／五值、原入口十映射／Root Combine／null-无效值精确异常顺序、完整资源／事务／旧更新桥通过 |
| [C007c2 Migration validation 41739195319](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41739195319) | `2d111e1`：Windows job `126276117223` 347／347、Ubuntu job `126276117677` 319／319，均 success；8 组新平台规则与严格 137／52 零差异通过，测试只针对原平台差异，不声称 LF 主产品已支持 Linux |
| [C007c2 Windows Build 41739195359](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41739195359) | `2d111e1`：Debug job `126276117985`、Release job `126276118427` 各 41 步 success；日志确认原 SafeName／UTF16／三消息及 enum 契约、全部既有资源／取消恢复／备份／旧更新桥通过 |
| [C007c3 Migration validation 41740416793](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41740416793) | `70f2545`：Windows job `126280223145` 356／356、Ubuntu job `126280223447` 327／327，均 success；原 Quick Play 和新增筛选直接生产样本通过，137／52 在 RequireEquivalent 下零差异 |
| [C007c3 Windows Build 41740416675](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41740416675) | `70f2545`：Release job `126280222951`、Debug job `126280223303` 各 41 步 success；日志明确原 extension-filter／null／device、SafeName／enum 及五页资源、既有事务／更新／旧桥全通过，未把 PowerShell 域当 LF-default 对照 |
| [Windows Build 41696728260](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260) | [Debug job 126129118107](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260/job/126129118107) 与 [Release job 126129118529](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728260/job/126129118529) 各 40 步 success，包含原有自动化回归、凭据保护、旧更新桥和产物上传；主程序仍为 net48，不能据此关闭 N003 或 R003 |
| [Migration validation 41696728492](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41696728492) | Ubuntu job `126129118456`、Windows job `126129119218` 均 success：固定清单一致，.NET 10 真实生产解析器测试通过；这只验证已提取的解析器范围 |
| [C002 Windows Build 41705399746](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41705399746) | 提交 `0388bd6f6f99ac142a3a6d88032a70ba727fe6f3`：Debug job `126159247355`、Release job `126159247567` 各 40 步 success；增强后的真实 WorldMetadata 门面、文件链接校验、只读／共享、精确文案及旧回归均运行通过 |
| [C002 Migration validation 41705399845](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41705399845) | Ubuntu job `126159247961`、Windows job `126159248400` 均 success，42／42 组生产解析器／文件／文案测试及固定源码基线核查通过 |
| [C003 Windows Build 41705751541](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41705751541) | 提交 `cfd4cbf23bb276fba800e930f8cb67c39b11c426`：Debug job `126160481124`、Release job `126160481435` 各 41 步 success；含原完整回归、两种方式的真实备份引擎、个人习惯／Quick Play、还原／克隆／取消恢复、凭据保护、旧更新桥和产物上传 |
| [C003 Migration validation 41705751480](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41705751480) | Ubuntu job `126160481337` 62／62、Windows job `126160481670` 64／64 组通过，固定清单一致；日志确认两个符号链接用例均实际执行，Windows 共享锁和只读拒绝两组也通过，没有 SKIP。这些是功能样本，不代替完整 GUI／真实账号／游戏运行验收 |
| [C004a Windows Build 41708039034](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41708039034) | 提交 `758a3c2d1631cd30f4619e6a5fc9a520f776c763`：Debug job `126168470323`、Release job `126168469671` 均 success，原完整功能链及独立备份引擎通过。日志中的 42 条 CLR4 oracle 确认 `1e400`、特殊字面大小写／符号与空白的旧接受语义，包含 profile／会话和按键实际消费者回归 |
| [C004a Migration validation 41708039126](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41708039126) | Ubuntu job `126168470971` 90／90、Windows job `126168471703` 92／92 组通过；含 28 组规则样本、原文件语义与固定源码清单。现代输入收窄行为与上述真实旧运行时结果对应；不声称复现全部有限值极端精度 |
| [C004b Windows Build 41709819895](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41709819895) | 提交 `35114dd4af209eda009b47e027f9ce19525b8994`：Debug job `126174639661`、Release job `126174640224` 各 41 步 success；原完整消费者、独立备份引擎、CLR4 oracle 与旧更新桥通过 |
| [C004b Migration validation 41709819781](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41709819781) | Ubuntu job `126174639198` 108／108、Windows job `126174639572` 112／112 组通过，固定清单一致；Windows 日志确认原读取共享行为和只读写失败两组实际通过，无链接 SKIP |
| [C004c Windows Build 41712282261](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41712282261) | 提交 `1c1841f00298579fca865e703af04b8bc08fc521`：Debug job `126183120514`、Release job `126183119989` 各 41 步 success；原 VB 会话类型、六继承字段、原对象 Tracked 更新、个人设置／按键／日志恢复、独立备份引擎及旧更新桥等完整回归通过 |
| [C004c Migration validation 41712282087](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41712282087) | Ubuntu job `126183119631` 135／135、Windows job `126183120114` 139／139 组通过；含 27 组真实流程、六字段派生 DTO／factory 身份、Monitor 重入、失败后日志保留等样本，固定源码清单一致，无 SKIP |
| [C005a Windows Build 41713871974](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41713871974) | 提交 `a726586a6e9b21b4930f6e595017218c2bdc5076`：Debug job `126188672461`、Release job `126188672911` 各 41 步 success；原按键消费者、same-value backup 哨兵、stale 编辑、完整原回归及旧更新桥通过 |
| [C005a Migration validation 41713872243](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41713872243) | Ubuntu job `126188673706` 156／156、Windows job `126188673254` 162／162 组通过；新共享写失败／只读失败两组实际执行，链接无 SKIP，固定清单一致 |
| [C005b Windows Build 41716125675](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41716125675) | 提交 `33a1c6aebfac08088de06a3d0d06c7aa959e47e9`：Debug job `126196457564`、Release job `126196457417` 各 41 步 success；实际 VB 类型／七继承字段／31 默认、Meloong CRLF／VT 标题委托、原归属／冲突／保存／页面消费者及旧更新桥通过 |
| [C005b Migration validation 41716125643](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41716125643) | Ubuntu job `126196457531` 189／189、Windows job `126196458304` 194／194 组通过，固定清单一致；读取新增 33／32 组差异是 Windows 不创建仅大小写不同文件的单项 fixture，非异常／取消能力被跳过 |
| [C005c1 Windows Build 41717722719](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41717722719) | 提交 `2f60f73afb085912708789551923d3de988f1751`：Debug job `126202044008`、Release job `126202043635` 各 41 步 success；真实 KeybindingGroups 页面消费者、个人习惯／按键／Quick Play、独立备份引擎、凭据保护及旧更新桥通过 |
| [C005c1 Migration validation 41717722638](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41717722638) | Ubuntu job `126202044078` 210／210、Windows job `126202044337` 215／215 组通过，固定清单一致；新增 21 组精确显示／搜索／计数／分组金值均运行，无 SKIP；不是整页 C# 转换或完整 GUI 实机验收 |
| [C006a Windows Build 41719762439](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41719762439) | 提交 `046ed2dd90427537e48012bbab537cc32e80d587`：Debug job `126209114550`、Release job `126209113916` 各 41 步 success；真实 CLR4 GetFileName oracle 遍历 41 字符通过，原 Prepare 精确参数、缺失世界、个人习惯／日志／按键及旧更新桥等回归通过 |
| [C006a Migration validation 41719762399](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41719762399) | Ubuntu job `126209113899` 226／226、Windows job `126209114524` 232／232 组通过，固定清单一致；Windows-only 旧路径类型／顺序／device／显式开关样本实际通过，无 SKIP |
| [C006b Windows Build 41721885199](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41721885199) | 提交 `f1fa26b3705a002c237fba190c4c01bfe84a7442`：Debug job `126216509011`、Release job `126216508717` 各 41 步 success；实际旧 VB 会话／最近世界类型、4＋5 继承字段、GUID-N 和 Mojang ISO 日志 UTC 断言、Quick Play／个人习惯／按键／旧更新桥等完整回归通过 |
| [C006b Migration validation 41721885208](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41721885208) | Ubuntu job `126216508313` 252／252、Windows job `126216508853` 258／258 组通过，固定清单一致；26 组新历史／日志／JSON／锁样本含真 UTF16LE BOM 和逐项 mtime 完整性均实际运行，无 SKIP |
| [C006c 初次 Migration validation 41724453187](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41724453187) | 提交 `257e98ea14e2cd93d2c9a906c69c62d9709ad2d3`：Ubuntu job `126225449726` success，Windows job `126225449293` 为 288／289；唯一失败为 PrepareIdentity 金值将原 Address 的内部 `/` 规范化为 `\`。生产保留 LF 原相对名称是正确行为；只修正测试为原两参数 Combine 形状，随后重跑，不将此初次运行写为通过 |
| [C006c Windows Build 41724453366](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41724453366) | 生产提交 `257e98e`：Debug job `126225450403`、Release job `126225449900` 各 41 步 success；新流程原入口、继承字段、Mojang UTC、真实进程 live／dead 恢复、禁用下状态探测、精确坏 JSON finally、坏 gzip InvalidData／字节、原近期卡片动画 ticks／顺序及旧更新桥通过。卡片脚本没有启动应用窗口或 Minecraft，不替代 R003 |
| [C006c 修正后 Migration validation 41724887383](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41724887383) | 提交 `6023f3d`：Ubuntu job `126226953558` 283／283、Windows job `126226953958` 289／289 组通过，固定清单一致；原 raw path 精确金值和有效 NBT 只读／字节样本通过，无 SKIP。`git diff 257e98e 6023f3d -- PCLCS "Plain Craft Launcher 2" scripts` 为空，Windows 生产证据适用于同一代码树 |
| [C007a Windows Build 41726305503](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41726305503) | 提交 `459bb0a`：Debug job `126231854074`、Release job `126231854550` 各 41 步 success；真实 Scan 原 sealed VB 类／基类八字段／本类 ImageSource 与冻结图标、五资源页、Rename／Export／回收／更新、取消／陈旧保护及旧更新桥等完整回归通过，源图标加载仍在原 VB |
| [C007a Migration validation 41726305734](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41726305734) | Ubuntu job `126231855656` 296／296、Windows job `126231856008` 303／303 组通过，固定清单一致；新增 13 跨平台＋1 Windows 样本含真实只读／payload 独占句柄、原目录内容变化规则，Windows-only 实际通过；不声称并发竞态或完整现代 WPF 验收 |
| [C007b1 Windows Build 41730760410](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41730760410) | 提交 `b12b6cf`：Debug job `126247143719`、Release job `126247144148` 各 41 步 success；原资源／更新／备份／克隆／取消恢复及旧更新桥等真实消费者通过。原 SDK9／VS Build 工作流未修改，产品路径实现仍运行 Framework API |
| [C007b1 Migration validation 41730760349](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41730760349) | Ubuntu job `126247143804` 300／300、Windows job `126247144983` 315／315 单元组通过；Windows 独立 CLR4 v4.8 与 .NET 10.0.14 的 75-case／5 真实链接 oracle 成功运行，基线契约失败 0、路径差异 8、Unicode 差异 2，**Diagnostic 不是等价通过**。artifact `13276625509` 与全部差异／环境／下一步见 [摘要](migration/RESOURCE_PATH_ORACLE.md) |

清单重新生成：

```bash
python3 scripts/Inventory-Migration.py --ref f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 --output docs/migration/source-inventory.baseline.json
python3 scripts/Inventory-Migration.py --ref HEAD --output artifacts/migration/source-inventory.current.json
```

初始基线固定不随迁移提交变化；当前清单用于与初始清单对照。工具只读指定 Git 提交树，未提交的转换不会反映在 `HEAD` 清单里。JSON 的项目条件、属性、导入和 Remove／Update 不执行求值；以实际 MSBuild 和 Windows 构建作为编译范围的最终证据。

## 当前批次文件与协作归属

六个生产子代理已冻结各自模块，主执行者统一集成、编译、自审、提交；当前继续补该批必要行为样本，不并行改同一源或固定输出。

| 范围 | 执行归属／实际状态 |
| --- | --- |
| C015／C019a | resource_ui_chain：依赖补全与下载目录/源/客户端整模块，主执行者集成原模型/Loader平台边界 |
| C016／C027b | g005_runner：依赖四视图及六通用控件；共享动画工厂/项目Page接线由主执行者合并 |
| C017a-b | g005_ci：Java/实例完整后端和目录loader；原FolderScope所有GUID fixture/断言/cleanup逐字保留，新wrapper引用实际Core/Shared/JSON并Strict编译通过 |
| C018 | clone_review：五原资源模块成组迁移，原字段/ByRef/Optional/WithEvents及典型真实下游隔离编译通过；许可证按CE路径校准 |
| C024a／C025 | validate_chain：完整验证器/Loader族；nullable仅编译注解保持Nothing及原转换异常，补可持续真实payload故障链 |
| C026b／C022 | bitmap_chain：图像/颜色/矩形及完整设置/账号/偏好/Binding；原Imazen/native加载保留，新测试只用GUID存储，不触真实账号 |
| C020a-b 导入/更新/整合包模式 | resource_ui_chain／clone_review：第二批17文件已按原repo/draft SHA双guard集成；两真实Core已编译，既有安装/模式/事务/恢复/下载进度消费者随本次必要完整Windows检查点验，不据草稿完成整链 |
| 项目／提交／Windows | 主执行者：两个Core、两个Presentation与modern test完整图Release警告视错通过。Linux旧vbproj直接Compile缺WPF生成target/BAML及netstandard而失败，不能据此判原产品通过或回归；实际原Windows构建/运行待本批CI |

首轮d7a830b检查：Migration41837988181 Ubuntu126601919611实际435／435、Windows126601920011实际486／486及17组均成功；严格v7仍397例、contract/native/path/text差异全零（artifact13327667429）。Modern41837988171两配置在build阶段因冗余显式System.Drawing.Common触NU1510失败（该程序集已由WindowsDesktop framework提供）；原Build41837988131两配置在新控件local-type BAML触发临时程序集时缺Presentation NuGet props/targets/dgspec而失败，原工作流只restore Core。两者尚未进入运行，不能记Windows通过。修复为移除冗余Drawing包引用和Build-Release使用MSBuild `-restore`恢复完整实际项目图，保凭据/锁/产物/测试语义；本地完整restore graph已确认包含Presentation、Core及Shared/Wpf/原VB五项目；移除包后实际restore＋现代test完整图警告视错构建0警告错误。等待新Windows检查点。

45ef882实际Windows检查：Build41838845476两配置已完成原产品构建，并通过凭据失败清理/锁、合成账号迁移/DES/回滚、旧更新器、扫描取消、审计/设置/证书、激活、工具、资源整页、地图回收、背景设置、在线更新、个人习惯/QuickPlay、整合包更新事务和六cold控件。Release126604871752／Debug126604871929在ModpackUpdateUx辅助C#编译缺System.Xaml停住，之后消费者未执行；仅补该脚本及相同Loader消费者ReviewFixes的精确引用，保所有原断言。Modern41838845600两配置实际完成资源/已有controller/动画五组/真实ThreadDriver/Color与图像转换，首轮在旋转DPI测试假定横纵轴不交换处失败，后续未跑；生产原顺序仍为SetResolution后RotateFlip。新增固定f4原VB摘录/源SHA并由独立CLR4进程实测crop/rotate，两目标严格比较尺寸、每像素和双DPI轴；不改生产像素/DPI算法或阈值。新图像子组和六控件各自清理并聚合失败继续独立检查，避免一个故障遮住同批其它样本；失败仍使进程非零。完整现代测试图和原VB摘录编译已通过，Windows新baseline/运行待验证。

b7c8172已验：Modern [41831770480](https://github.com/DDZmumo/PCL-Living-Fork/actions/runs/41831770480)两配置各11步成功，实际原ThreadDriver在隔离子进程启动。Build41831770664／Migration41831770692仅选择job通过，产品/纯测试job按范围skipped；复用02ba2b2实际全纯测试/原Windows结果，不把跳过写成功。

本批实际本地验证：SDK10.0.403，`dotnet build tests/PCLLF.Migration.Tests/PCLLF.Migration.Tests.csproj -c Release --no-restore -warnaserror`零警告错误；仓库runner定向`--group settings --group validation-loaders`实际18／18、`--group legacy-java`实际6／6，未选旧14组未重复计通过。设置只在具体`RegistryValueKind`元数据语句局部抑制CA1416，不掩盖真实Registry API；Java六child首触Configs前设置GUID真实provider，异常/超时退出均向父传递，未触用户目录。新增真实现代图像五组与六控件交互/渲染样本已编译接线，Windows运行待CI。

完整旧VB调用图补充编译：临时SDK net48 WPF工程使用实际原167个生效VB、61Page+Application、原Imports/RootNamespace/MyType/条件与实际四库产物，产生62份真实BAML与EXE。Linux仅在临时resx修正19个FileRef路径的磁盘大小写，仓库resx/资源bytes未改；最终0错误／1条原Newtonsoft6→13绑定warning。这是辅助编译证据，不能替代原Windows工程、正式产物或GUI运行；不依赖该临时工程续跑。

## 任务依赖与验收

状态只有 `todo`、`in_progress`、`blocked`、`done`、`deferred`。`done` 必须关联实际改动或证据。以下顺序用于选择最早可执行任务；每项仍须按功能链拆成可审查的小批次，拆分结果写在原编号下。

| 编号 | 状态 | 依赖 | 工作与完成证据 |
| --- | --- | --- | --- |
| G001 | done | 无 | 固定 LF／PCL 源清单工具与初始 JSON；可重复输出、条件源和项目声明已记录，见基线表 |
| G002 | done | G001 | LF／PCL／CE 模块映射、生产入口、依赖和逐组迁移边界；核对源清单覆盖，并记录 CE 参考位置及未查证处 |
| G003 | done | 固定 LF 来源 | 初始 Windows Debug／Release 构建与既有自动化回归；证据为 run `41656492901`，GUI 对照仍归 R003 |
| G004 | done | G001 | 迁移 CI 已在双平台通过：固定源码清单重现和 27 组纯 C# 生产解析器测试；见 run 41696728492 |
| G005 | done | G004 | `093ba7d`选择执行与双平台实际成功；旧Test-ReviewFixes字面断言失败保留，`0bee590`改成真实glob／路由检查并Windows通过。`b1357f7` Build41791636627 Debug126446183687／Release126446183725成功（各18步成功／23跳过），Migration41791636538只选择job成功、验证job按范围skipped；9组、42原脚本与常驻发布门禁保持 |
| B001 | done | G002 | 模块映射已固定实例／组件／依赖／启动／文件计划与适配边界，存档流接口已有生产调用与样本验证；其余模型生产实现随各功能任务完成，不声称整个 PCLCS 已无 WPF |
| C001 | done | G001、G003 | WorldMetadata NBT 引擎已进入 C# 生产类，保留 VB 兼容外壳；27 组真实样本与边界通过，Windows Debug／Release 原消费者完整回归通过 |
| C002 | done | B001、C001 | 文件／gzip／详情文案已迁入 C#，原三个消费者经兼容入口接通；42 组双平台样本与 Windows Debug／Release 完整回归通过。最终删除类型映射／路径日志 VB 门面仍由 C006／C010／C029／C031、R001 验收，不声称页面后台已转换 |
| C003 | done | B001 | `LocalGameDataStore` 已接通所有原持久化入口，保留 Gate、动态 TestRoot、旧 JSON／编码、摘要检查、备份与失败清理；Windows 两种备份引擎与完整旧回归通过，.NET 10 Ubuntu 20／Windows 22 组对应样本通过。剩余 VB 转发门面归最终主工程验收 |
| C004 | done | C003 | 规则／文件／会话三批均已接通真实消费者并通过各自 Windows Debug／Release、双平台矩阵和必要 CLR4 oracle；原数据、Gate、会话字段／类型适配保留。VB 兼容门面与设置／实例平台适配的最终 C# 替换仍归后续主工程／R001，不据此关闭整项迁移 |
| C005 | done | C004、C029a | a／b／c1已有证据，c2按键整页typed Host／Controller已实际Frame／NET10验收，源与真实options保存／冲突／profile告警／失败通过；最终VB partial／控件移除仍归C027／C029／R001，不代表全主产品现代化 |
| C006 | done | C006c、C029a | a／b／c核心与d近期页面／动画编排／Start交接已C#接通；b7493b8两配置Quick Play／近期卡片逐帧消费者成功，3f产品源码未变。实际ModLaunch／账号／主窗宿主仍后续C021／C023／C031，真实游戏由R003验 |
| C007 | done | B001 | `0bee590`后端／截图与`271d058`整页主／Recycle／Backups控制器已C#接通，`0df889a` Windows各41步＋381／425及严格v7通过；原XAML、Loader、自定义控件和typed平台适配保留，最终VB门面／控件归C027／C029／R001，非现代主程序已完成 |
| C008 | done | C007d2、C007e、C007u2 | `271d058` ResourceRecycleFiles／MapDownloadFiles接原入口，保PathUtils.IsSame、记录／链接／全预检／取消／同名／双暂存清理；425组含六新增Windows真实文件链，`0df889a`两配置原地图回收与整页消费者通过 |
| C009 | done | C007 | `2304fc5`完整OnlineFiles／Info／typed Host接原四入口，原平台模型与注入保持；`8c706ee` Windows各41步含真实LF ResourceVersion／NetFile消费者通过，439组含8新样本，批次独立终审通过；无live HTTP／游戏，最终VB适配移除归R001 |
| C010 | done | C002、C003、C007 | `8c706ee`完整备份／校验／保留／会话／恢复事务与UI命令接原入口，同一Gate／Busy／TestRoot／Changed；Windows各41步含原完整产品和EngineOnly真实门面／全部故障样本通过，388／439含5跨平台＋1Windows锁新增组；最终原控件／VB宿主移除与真实游戏仍归后续 |
| C011 | done | C003、C007、C010 | a完整历史／分析／Workflow及b还原点整页／row已C#接通，98f81fe Build41816180616两配置所有原历史／保留／覆盖／克隆／更新消费者及旧桥成功；末端新增控件静态误判不涉及本链。原真实事务／平台typed壳最终清理由C020／C025／C031／R001验，非全产品现代化完成 |
| C011a | done | C003、C007、C010 | 五C#＋两typed VB接原Gate／Mutex／Store／coverage／Capture／Retain／Restore／Recent差异与UI命令；测试保真实提交前边界、不自造VB事务，独立审通过、5／5及两目标0警告错误；原Windows消费者已在98f81fe Build41816180616两配置成功，真实事务／回滚／保留及旧桥通过 |
| C011b | done | C011a、C029a | 完整RestorePoints页面与共享row仅RestorePoint显示业务已迁typed C# controller／presenter，原XAML与非还原点row区段不变；net48／现代Core编译、独立源审通过，原BackupRetention仅三state写入支持同名property，Windows原页面／恢复链与保留检查已在98f81fe两配置通过 |
| C012 | done | C003、C007 | a完整克隆／迁回／分组／导出隔离引擎及真实事务typed适配已迁，b两页面及选择器来源分组呈现正在迁；859fd4f两配置真实原链及页面通过；b84手动矩阵405／456与13组、严格v7全通过，Shared初始化遗漏修复已实证。最终VB平台壳仍后续主工程任务 |
| C012a | done | C003、C007、C011a | 六C#生产源＋三VB typed壳，保原Gate／128KiB复制／Flush(true)／stale双快照／MC变化备份／Capture／原ApplyTransaction及双版本锁；独立源码审无回归。Release纯测试0警告错误，克隆6＋历史5定向11／11；真实McVersion／McComponents／ModLoaders与固定Shared接线，JSON引用／CopyLocal／字节保持LF DLL（SHA E1E27AF7…44FA9D），未以替身模型过关。第12组instance-clone与9精确生产路径／7原Windows消费者路由，resources仍固定4组。859fd4f两配置Windows事务／回滚／原隔离与历史消费者成功；b84两OS新组及完整矩阵已实际通过 |
| C012b | done | C012a、C029a | 完整克隆页面与版本选择来源分组呈现成组迁移，保原XAML／MyCard懒构建／事件／展开折叠与其它选择器区段；两C# controller／presenter与两typed VB页面已接通，原分类／空页／行点击及删除区段逐字保留，独立终审无行为差异；net48 Core与完整现代test图Release -warnaserror均0警告／0错误。原Groups脚本保全部断言并新增真实PageInstanceClone构造／validator／原生选项／来源卡片／同owner和换owner命名／invalid与stale guard／Changed attach-detach样本，StartupUri清除且owner finally恢复，无成功克隆／迁回命令或游戏启动。859fd4f两配置原真实MyCard／lazy行／选择器及新页Windows样本成功，b84两OS矩阵及v7亦成功 |
| C013 | done | B001 | 原完整Inspector解析／版本三态／图／可靠度／移除影响／说明迁四份C#及166行原DTO／Context typed VB壳；原10个嵌套类型闭包、enum字段／List／引用身份／缓存JSON ABI保持。net48／现代Core及原facade＋Online strict警告作错编译成功，独立终审无回归；Release纯test六组真实JAR／TOML／嵌套／预算／取消6／6。第13组及5精确生产路径→5实际Windows消费者，b84实际legacy两配置各42步成功，原Inspector93／Online72断言、Graph／启动修复／Activation及6控件／旧桥通过；pure405／456、modern双11与v7空差异成功。noEXE两fallback新增真Core构建／引用／加载但该可选测试模式未Windows实跑，明确后验，不冒充已跑 |
| C014 | done | C013 | 02ba2b2完整在线匹配／哈希／CF指纹／Session预算／缓存／候选／关系映射／HTTP接四C#与原typed壳；实际Windows消费者两配置成功、Ubuntu411／Windows462及14组通过，实际现代JSON13.0.3 v7严格397例无差异。原Framework DLL保持独立CLR4基线；无live平台HTTP，最终VB壳清理由R001验 |
| C015 | in_progress | C013、C014 | 完整AutoFill/Activation/Install迁四C#、原LaunchDependencyCheck及ModDependencyAutoFill薄桥；16候选重试、64轮启用闭包、会话stamp、真实C013复查、stage/hash/下载/回滚/finally保持。net48／NET10 Core警告视错编译通过；原/新真实薄桥vbc零诊断，83项原public/Friend metadata全一致；原Windows启动补全／混合启用与取消故障消费者待本批验收 |
| C016 | in_progress | C013、C015 | 七C#承载关系图layout/render/icon并发／检查视图／影响确认，四原名VB壳保GraphCanvas/Groups/Details/Cards及真实node Tag。两Core已编译；原100节点／图标／对话框与启动消费者Windows运行待验 |
| C017 | in_progress | B001 | a四C#完整Java需求／选择／下载计划及原WithEvents桥；b八C#完整实例/目录/继承/隔离/skins/libs/assets与原字段桥，另提取同一真实FolderLoader便于原fixture执行。LF新增目录作用域/图标/近期世界/取消安全保留；两Core与Strict桥编译通过，Windows消费者与Java配置隔离样本待验 |
| C017a | in_progress | B001 | 完整Java原需求/选择/旧设置迁移/下载计划已C#接线，原Java.cs缓存/扫描模型和RefreshWorker保留；仓库runner六条真实Config provider/Java模型/Worker链全部通过（逐条隔离child），Windows消费者待验，不新增Java规则 |
| C017b | in_progress | B001 | 完整Minecraft目录/实例/继承/隔离/缓存/skins/libs/assets接线；全路径构造惰性初始化，原_McInstanceSelected单份字段、NextUuid Integer及ShouldIgnoreFileCheck Object保持；原FolderScope fixture逐字保留且真实wrapper Strict编译通过，Windows运行待验 |
| C018 | in_progress | B001 | 十五C#完整资源项目/版本/cache/wiki/title/tag/列表/下载追踪、双语分页搜索与真实本地文件hash/INI/JSON/更新MR-CF workers；五VB原类型薄桥保唯一字段/列表/缓存/事件。原枚举/ByRef/Optional/WithEvents真调用者Strict编译通过；LF Map两处扩展保留，现代Core已编译，Windows整链待本批 |
| C019 | in_progress | C017、C018 | a完整ModDownload目录/源切换/六加载器版本解析/client Jar/libs/assets工作体移五C#，原六结果结构与20 LoaderTask字段保留，两Core编译通过；117项原public成员/参数名/Optional metadata零差异；原版和各loader安装及取消/失败Windows验收待后续，不能以目录后端完成关闭安装整链 |
| C019a | in_progress | C017a、C017b、C018 | ModDownload整个模块C#后端/源任务/目录/客户端补全及原结构体/loader字段桥已接通；安装其它模块/页面仍属母项，实际Windows消费待验 |
| C020 | in_progress | C007、C011、C019 | a完整导入/模式、b完整更新事务/流程已成组C#接原typed入口；保现有MMC/CF/MR/HMCL/MCBBS行为及LF个人修改/隔离/回滚。两Core编译0警告错误，原页面/真实下载/取消/恢复/模式Windows待本批；页面最终C#汇总仍后续，不提前关闭母项 |
| C020a | in_progress | C007、C019a、C017b、C018 | 四Install与两Mode C#接ModModpack/Mode两原名薄桥，原Source/RecordInstalled/导出隔离、root marker优先级、覆盖目录、worker负载与revision可视决策保留。原38接口/参数/Optional metadata零差异，两Core0警告错误，全实际VB/WPF探针通过；现有格式/模式Windows验收待本批，无新增MMC策略 |
| C020b | in_progress | C007、C011a、C019a、C018、C025 | 七C#完整Package/Plan/Transaction/Recovery/Flow/在线选择，原单份DTO/Active字段/反射hook、ByRef Source及Stopped后锁释放保持；保LF历史修复07dd3503b38b39095c8b92b40ae0454907387f9f。两Core编译0警告错误，原源本地事务/故障/worker/Source与预取消smoke已验；真实旧Windows消费者和loopback进度待本批 |
| C021 | todo | C004、C006c、C010、C015、C017 | 迁移 `ModLaunch`／`ModWatcher`／`ModCrash` 的生命周期与适配；Quick Play 所需核心已由 C006c提供，页面剩余归 C006d，不用 UI 母项阻塞核心；按预检、进程、监控拆子任务，启动、退出和崩溃对照回归 |
| C022 | in_progress | B001 | 六C# Settings后端+真实Registry/Binding两平台类接原四VB；205定义/40回调、同一Entries/Source/ValueCache、旧DES wire、账号组事务/marker-last/回滚与偏好显式空值保留。两Core编译成功，新增10条真实store故障/独立DES固定密文样本已由仓库runner全部通过；Windows临时Registry/INI消费者待本批，未读写真实用户账号 |
| C023 | todo | C017、C021、C022 | 迁移登录页、皮肤和主页的生产调用；脱敏账号旧数据、取消、错误、刷新及启动交接验证，实际账号行为单独记录 |
| C024 | in_progress | B001 | a完整14校验器与有序pipeline迁LegacyValidationRules、保原typed VB ctor/属性及5行OnErrorResumeNext primitive；Object父目录选项/延迟枚举/Err91保留。两Core编译与同NET10原源差分1303输入+7pipeline通过；原Windows消费者待验，网络/日志完整模块仍后续，母项不关闭 |
| C024a | in_progress | B001 | 完整校验族及原入口已迁，真实Core双目标零警告错误；与C025合并八条真实文件/延迟枚举/异常短路/取消payload样本已由仓库runner全部通过，原Windows工具/克隆校验待验 |
| C025 | in_progress | C024a | 五C#完整Loader Base/Task/Worker/Combo/Taskbar/目录时间戳/FileOperation接原VB，保真实Shared IWorker/RedoableWorker、事件/Thread identity、中止/实际结束与文件guard语义。两Core警告视错编译通过；同NET10差分与真实payload取消已有隔离结果，仓库八条集中真实链已全部通过、零skip；原Windows锁/工作流故障回归待本批 |
| C026 | in_progress | C026a、C025 | a完整动画引擎与真实.NET10宿主已成组迁移，窗口／位图／Loader其它平台适配及人工手感仍后续；不以局部自动化关闭母项 |
| C026a | done | B001、C029a | 完整八C#动画引擎/NativeHost/NativeRuntime及原AniData/AniGroups/API typed壳已接通；859fd4f五native组及legacy中帧成功。b7c8172 Modern41831770480 Release126580471404／Debug126580472047各11步成功，含隔离子进程实际前台ThreadDriver；fixture增量1.25触原[0,1]clamp，未改断言/阈值/生产帧舍入。手工手感与主宿主仍C026/R003 |
| C026b | in_progress | B001、C029a | Bitmap/Color/Rect全组C#及原操作符/单份Pic/ARGB、LF pack URI/cache、原Imazen与native WebP、短读/stream政策保留；固定Drawing10.0.14及canonical G15接现代图，两Core/Presentation/modern test编译通过。真实像素/PNG-BMP/锁/pack/native WebP Windows样本待验；未改decoder/性能策略 |
| C027 | in_progress | C029a | a先迁3常用控件完整视图／业务与原typed动画Host，原名壳保现XAML接线；其余控件成族推进。现代实际动画引擎由C026a承载且待本批Windows，最终薄壳／事件类型清理由C031／R001验，不用空host关闭母项 |
| C027a | done | B001、C029a | 三完整C#view／原LF视觉XAML／Services与真实AniData Host／两WPF项目／原名VB壳／设置事件／单exe嵌入接线完成；net48与NET10实际XAML编译0警告错误，独立终审通过；859fd4f实际legacy两配置全部42步与无sidecar冷进程／BAML／原页面／六族事件设置主题AniTimer成功；Modern41817913425两配置真实动画五组与三C#view成功。其余控件、主现代主题／窗口及人工手感不在a完成范围 |
| C027b | in_progress | C026a、C029a | 六完整C#控件Card/DropShadow/Hint/IconButton/ListItem/VirtualizingElement及三实际BAML接原六薄壳；原ContentHandler字段/事件/DP/懒加载/阴影缓存/布局/Height marker保留，真实动画工厂扩充；两Presentation完整图与六Strict桥编译通过，新增真实交互/渲染及原消费者Windows待验 |
| C028 | todo | C018、C019、C027 | 完成下载页面的 C# 后台与 XAML 调用，确认没有残留 VB 功能实现和重复入口 |
| C029a | done | C005b、C007u2 | 资源与按键整页已实证LF C# WPF标准视图／惰性typed Host单向边界，无main反引；3f在Frame实际native壳及NET10同源live控制器双配置通过，足以解除独立页面人工阻塞。完整原生控件／主C#工程与最终VB清理仍归C027／N003／R001 |
| C029 | todo | C005c2、C008、C010、C011、C012、C016、C020、C027 | 完成实例页面和资源页面 C# 后台／XAML 的最终汇总，已有先行页面只验收集成，不与 C029a 宿主前置相互等待 |
| C030 | todo | C022、C027 | 完成设置、个性化、工具、帮助、关于及现有可选联机页面的迁移；默认关闭项单独验收，不静默删除 |
| C031 | todo | C023、C025、C026、C027、C028、C029、C030 | 成组完成 `Application`、`FormMain`、`ModMain`、事件／音乐／维护等剩余入口；窗口、路由、单实例和关闭生命周期回归 |
| N001 | done | G002、G003 | [兼容审计](migration/DOTNET10_COMPATIBILITY.md) 已完成工程／依赖清单、12 项门禁与小批次建议；仅表示审计交付，实际现代运行验证仍由 N002–N005／R002 关闭 |
| N002 | in_progress | N001 | N002a先落实LF当前真实全C#库／WPF组件的独立现代工程、完整依赖图与Windows运行，不等全部Base／Loader先迁；剩余C024／C025及后续新增源的现代兼容仍随整链验证，不能以本批关闭整个现代迁移 |
| N002a | done | B001、G003 | b7493b8真实52源／4资源＋3f输入fixture修复，Modern41804541772两配置各11步通过：真实NET10／Shared加载、精确反斜线资源与bytes、图片冻结／流释放、同源缩略图与按键实际保存；旧Frame两消费者复验通过。只关闭当前Core/WPF组件，不关闭整个N002／主产品 |
| N003 | todo | N002、C031 | 主程序切换 C# SDK WPF／.NET 10，处理 My／VB 生成代码、XAML、资源名和程序启动；Windows Debug／Release 实构建与运行 |
| N004 | todo | N003、C022 | 验证旧账号和配置数据、共享目录、版本隔离、备份／还原点与旧格式写回，保留脱敏样本和实际结果 |
| N005 | todo | N003、N004 | 完成凭据生成、资源依赖、发布脚本、产物命名、单实例与旧版更新桥；测试发布产物和旧更新入口交接 |
| R001 | todo | N003、N005 | 逐项处理初始清单中所有 VB／生成／条件源码，检查当前产品 VB 数为零、项目声明完整及许可来源齐全 |
| R002 | todo | R001 | 迁移后的完整 Windows Debug／Release 自动化回归、现有 MMC 和普通整合包样本；每项保存真实命令、提交和 CI 结果 |
| R003 | todo | R002 | 实际 Windows 运行与新旧 UI 对照；账号、游戏启动／退出、文件中断恢复、按钮、滚动、页面及动画行为验收 |
| R004 | todo | R003 | 汇总全部完成证据和剩余限制，自审最终差异与远端状态；仅在目标全部验收后标 `completed` 并暂停续跑 |
| D001 | deferred | 当前目标完成后另立目标 | 新 MMC／Prism 组件规则、补丁语义、Java 交集、LWJGL 与 GTNH 增强 |
| D002 | deferred | 当前目标完成后另立目标 | 性能优化；当前只保留必要基线与等价回归 |
| D003 | deferred | 当前目标完成后另立目标 | 新 Rust／C++ 模块或其他原生化改造 |

任务存在交叉文件时，以最先完成的功能链为准更新后续任务的剩余范围，不重复转换。例如 C010 已完成备份页面后台，则 C029 只验收该入口集成并处理尚余页面。较大的 C017、C019、C020、C021、C027、C031 在执行前按可运行入口拆出子编号；子任务完成不会隐式关闭母项的其他验收。

C004 按实际生产入口拆分如下，不提前改变原会话与文件事务：

| 子任务 | 状态 | 依赖 | 实现与验收边界 |
| --- | --- | --- | --- |
| C004a | done | C003 | `758a3c2`：白名单、解析、键码／FOV／修饰符、严格 UTF-8 解码与合并已进入 C#；保留原动态 Settings 和所有入口，28 组新增生产样本与完整 Windows 功能链通过，42 条 CLR4 oracle 验证必要的现代运行时适配 |
| C004b | done | C004a | `35114dd`：原始读取／写回已接入 `PersonalOptionsFiles`，复用 C003 原子写入；18 组 Linux／20 组 Windows 对应文件样本及完整 Windows Debug／Release 消费者通过 |
| C004c | done | C004b | `1c1841f`：Flow／惰性接口／兼容 DTO 接通原 23 入口，新增 27 组流程样本与实际 VB 反射断言；Windows Debug／Release 各 41 步与 Ubuntu 135／Windows 139 组总矩阵通过 |

C005 先按实际生产入口拆分，Save 与个人习惯合并写回的语义不同：

| 子任务 | 状态 | 依赖 | 实现与验收边界 |
| --- | --- | --- | --- |
| C005a | done | C004 | `a726586`：Save 核心和原四参数入口已接通，Windows Debug／Release 各 41 步、Ubuntu 156／Windows 162 组总矩阵与原 same-value backup 消费者断言通过；读／默认表／模型未在此批改动 |
| C005b | done | C005a | `33a1c6a`：模型／31 默认／读取归属及原泛型／Save 保持，Windows 各 41 步与 Ubuntu 189／Windows 194 组总矩阵通过；七继承字段、原类型与实际标题委托已验证 |
| C005c1 | done | C005b | `2f60f73`：View 已接通原两方法，21 组新金值令 Ubuntu 210／Windows 215 组总矩阵通过；net48 编译／VB 索引桥／独立终审、Windows 各 41 步及真实 KeybindingGroups 均通过 |
| C005c2 | done | C005c1、C007u2 | ebdb594整页＋3f修两次真实WPF输入fixture后，Frame native分组／同源C#真实保存及NET10同样live verifier均通过；保原Sender／Handled／modal后重读Owner／options事务与profile警告；鼠标hover及真人交互仍留R003 |

页面前置以实际项目为证：`PCL-Living-Fork.vbproj` 显式编译旧后台及 XAML，主程序已经引用 PCLCS；页面继承主程序集的 `MyPageRight`，还依赖同程序集的自定义控件、`LocalResourceUi`、Loader、动画、实例与 `FormMain`。因此 `C029a` 是独立前置子任务，不继承 C029 的汇总依赖；`C005c2→C029a→C027` 与 `C029→C005c2` 无环。不得把 `C005c2` 依赖改成整个 C029／N003，否则会经 C031 回到自身。c2 阻塞期间母项不能标 done，按 goal 继续 C006 等最早 ready 核心任务；这不是需要用户处理的持久阻塞。

C006 按实际行为拆分；日志／历史与会话的清理顺序不同于 C004，不能直接套用个人习惯流程：

| 子任务 | 状态 | 依赖 | 实现与验收边界 |
| --- | --- | --- | --- |
| C006a | done | C002、C003 | `046ed2d`：原两个入口、16 组跨平台／另 1 组仅 Windows 生产样本；Ubuntu 226／Windows 232 组总矩阵、net48 编译、独立终审、Windows 各 41 步与实际 41 字符 CLR4 oracle 全通过；现代 Windows 窄补边界见兼容审计 |
| C006b | done | C006a | `f1fa26b`：History／双 DTO／原四入口及 26 新组，Ubuntu 252／Windows 258 组矩阵、net48／测试零警告编译、独立生产／测试终审、Windows 各 41 步与实际旧类型／字段／UTC 消费者全通过 |
| C006c | done | C006b | `257e98e`／纯测试修正 `6023f3d`：原七入口、8 惰性 getter／双 factory 和 31 新组已接通，Ubuntu 283／Windows 289 组、Windows 各 41 步／原流程／卡片消费者、独立终审与零警告编译全通过；初次矩阵失败及修正已保留证据 |
| C006d | done | C006c、C007u2 | 1af62f6近期UI／原AniData动画计划与Start校验已接，b7493b8 Debug126484522335／Release126484522290的Quick Play／近期动画步骤均success；同产品树3f只修测试fixture。原逐帧／Tag／拒绝恢复／卸载取消断言未改，非近期主页字节不动 |

C006 的启动生命周期依赖明确落到已完成 C006c，C021 不再等待 UI 母项；原 Start／OnEnabledChanged 与近期卡片后台的最终迁移仍归 C006d／C023。母项 blocked 仅指尚未转换的真实界面前置，不是需要用户处理的持久阻塞。

C007 当前只拆已勘查的实际入口，后续导入／导出／扫描／图片在执行前按生产依赖继续拆分，不以模型完成关闭整个 Store：

| 子任务 | 状态 | 依赖 | 实现与验收边界 |
| --- | --- | --- | --- |
| C007a | done | B001 | `459bb0a`：原八字段／Validator／消费者、13 跨平台＋1 Windows 组已接通，Ubuntu 296／Windows 303 组、Windows 各 41 步与实际 sealed／8 字段／冻结 Icon 和资源链、独立终审及零警告编译全通过 |
| C007b | done | C007a | b1 原控制流＋独立诊断与 b2 有证据现代适配完成；137 例／52 候选严格等价、原消费者／各 41 步和双平台矩阵通过；局部生产边界与短名／全部 raw BCL 输入限制明确记录，不把本项外推至全部现代路径 API |
| C007b1 | done | C007a | `b12b6cf`：原 Core／VB 真委托／12 新组、原消费者 Windows 各 41 步和 Ubuntu 300／Windows 315 组通过；独立 CLR4 与现代 oracle 实际运行并保留 10 差异。done 仅指本批控制流／诊断交付，母项未等价 |
| C007b2 | done | C007b1 | `348b0a5` 适配／`21675ee` 严格门禁：137 例／52 候选、308／336 单元、Windows 各 41 步与消费者全通过；保原 root／link／drive-root NRE，不泛称全部 BCL 或短名组合已覆盖 |
| C007c1 | done | C007a | `8734df5`：两原入口／CInt 桥／原 enum／Root 保持，三组新金值，Ubuntu 311／Windows 339、严格 137／52、Windows 各 41 步及实际类型／入口／Root 异常顺序全通过 |
| C007c2 | done | C007c1 | `2d111e1`：SafeName 原顺序／三文案／平台规则，8 组新增令 Ubuntu 319／Windows 347、严格 137／52、Windows 各 41 步及原消费者全通过；独立终审／零警告编译通过，不扩大保留名策略 |
| C007c3 | done | C007c2 | `70f2545`：原 AcceptFile／9 白名单／null 先后、原 C6 字符共享与消费者接通，8 Unix／9 Windows 组令 327／356、严格 137／52、Windows 各 41 步全通过；独立终审／零警告编译通过，ZIP 内容校验未在本批迁 |
| C007c4 | done | C007c3、C007b2 | `dfb81b8`／`a89bc04`：原包校验／Using／原两文案与 Shader 前缀、真实 ZIP／惰性／句柄／共享只读及原消费者接通；339／371、严格 150／56、Windows 各 41 步全通过；首次编码与测试解包失败保留，不扩大扫描／导入策略 |
| C007d1 | done | C007c4 | a大小／b JSON纯显示已真实验收：保CurrentCulture／GB上限／ref部分更新／两次Meloong调用，v6严格397／56与Windows原消费者通过；done仅指纯显示，IO／Icon／catch由d2后继，不关闭整个Store |
| C007d1a | done | C007c4 | `745f0b8`：原 FormatSize／私有桥／6组金值、20 long×两culture的原方法对照；345／377、严格190／56、Windows各41步与原消费者全通过，保CurrentCulture／GB cap／原舍入，不外推d1b |
| C007d1b | done | C007d1a | `4eb50fb`／`93c51e2`／`476eb42`：两轮差异及修复保留，局部旧数字转换／15／17格式／R尾零与原ref桥完成；367／399、严格397／56和Windows各41步通过，实际源片段与隔离数值API验证，不外推所有CRT／现代WPF |
| C007d2 | done | C007d1b | `0bee590` Catalog／Windows decoder接原Scan／ReadPackInfo／LoadImage，保1／4／64MiB、share／catch／ref／取消／80次预览；Ubuntu379／Windows417组及实际消费者通过，v7严格397／56／14排序无差异 |
| C007e | done | C007b2、C007c4 | `0bee590` Transactions接原导入／导出／重命名／回收恢复，保CreateNew／Flush／取消／finally／全预验证及TEntry物化；6个Windows文件链组及原消费者实际通过；现代HashSet全Unicode域仍归N002 |
| C007u1 | done | C007a、C007d2 | `0bee590`实际截图后台迁ThumbnailController，保XAML／Entry字段／Sender／动画host；`b1357f7` Windows Debug／Release真实WPF verifier通过2并发／640／revision／viewport／卸载／错误dispatch，测试System.Xaml引用首轮失败保留；不据此关闭整页 |
| C007u2 | done | C007d2、C007e、C007u1 | `271d058`主／Recycle／Backups业务进typed Controller与Host／View，`0df889a` Windows Debug／Release各41步通过（原回收工具栏、备份分组／选择／孤立存档、截图异步等），短路／快照／事件身份交叉审；测试读取真正Controller三状态与adapter.Element修正保留，不新增测试用生产属性 |

## 当前批次与下一步

| 批次 | 当前进度 | 仍需完成 |
| --- | --- | --- |
| G001 | 已随 `c3a2838` 推送，双平台 CI 固定清单重现一致 | 当前固定基线不随后续迁移改写；需要时为新提交生成独立当前清单 |
| G002 | 模块映射完成：14 条功能链、固定源码清单全部 249 文件归组；53 个链接目标、46 个脚本名核实 | 已完成映射交付，后续按实际迁移更新对应行，不代表功能已迁移 |
| G004 | 已随 `d27f394` 推送，Ubuntu／Windows 迁移矩阵通过 | 后续生产改动继续使用该工作流，并按实际依赖扩展测试 |
| C001 | C# 解析器、原 VB 适配和 27 组测试已提交；独立自审、net48 辅助库编译、完整 Windows Debug／Release 及双平台迁移矩阵通过 | 本项完成。文件／gzip／格式与消费者的进一步 C# 迁移属于 C002，VB 兼容外壳仍纳入最终源码清单 |
| C002a | 已随 `0388bd6` 推送；42／42 双平台与 Windows Debug／Release 完整回归均通过，真实路径适配／文件链接、共享／只读、16 字段、精确 CRLF 和句柄释放已实测；独立源码审查通过 | 本批完成。尚未转换详情页／Quick Play／备份 UI 后台及最终门面，不据此关闭 C006／C010／C029／C031、R001 或整项迁移 |
| C003a | 已随 `cfd4cbf` 推送；完整 Windows Debug／Release 和 EngineOnly 通过；Ubuntu 62／Windows 64 组总矩阵通过，其中本机资料 20／22 组，链接用例无跳过。独立源码复核与 net48 编译通过 | 本批完成。陈旧摘要测试是顺序模拟外部编辑，进程权限不明分支只有源码核对，不声称多线程竞争／真实进程权限错误已实测；VB 转发门面仍在最终源码清单中 |
| C004a | 已随 `758a3c2` 推送，Ubuntu 90／Windows 92 组总矩阵与完整 Windows Debug／Release 均通过；真实 CLR4 oracle 42 条通过，独立源码审查和 net48 编译通过 | 本批完成；固定微软来源与有限值极端精度限制见兼容审计，C004b／c 不因此完成 |
| C004b | 已随 `35114dd` 推送，Ubuntu 108／Windows 112 组总矩阵与完整 Windows Debug／Release 各 41 步通过；独立审查与 net48 编译通过，新增共享读取／只读语义两组和链接实际运行 | 本批完成；cache 回调异常仍是写入完成后抛错，不新增回滚或多线程竞态保证 |
| C004c | 已随 `1c1841f` 推送，Linux／Ubuntu 135、Windows 139 组及完整 Windows Debug／Release 各 41 步通过；原 VB 类型／六字段／Tracked 实测通过，独立生产／测试／PS 审查和 net48 编译通过 | 本批及 C004 核心验收完成；枚举外层异常位置由源码复核，权限未知和真实 Minecraft／多线程竞态不在本批实测范围，最后适配门面仍纳入 R001 |
| C005a | 已随 `a726586` 推送；Ubuntu 156／Windows 162 组与完整 Windows Debug／Release 各 41 步通过，same-value backup 原消费者断言、独立终审和 net48 编译通过 | 本批完成；不声称真实多线程竞态或游戏运行已实测，页面与读取后继继续 C005b／c |
| C005b | 已随 `33a1c6a` 推送，Linux／Ubuntu 189、Windows 194 组及完整 Windows 各 41 步成功；原 VB 型／七字段／31 默认／实际 Meloong 标题和原消费者通过，独立终审及 net48 编译通过 | 本批完成；一项仅大小写不同文件 fixture 仅Linux执行，不声称所有文件系统一致或实际游戏／目录权限故障已验 |
| C005c1 | 已随 `2f60f73` 推送；Ubuntu 210／Windows 215 组、完整 Windows Debug／Release 各 41 步与实际 KeybindingGroups 通过；net48 PCLCS／VB 泛型桥零警告／错误，独立终审通过，页面 BOM 与其余后台逐文本未变 | 本批完成；控件／Tag／Margin／文案和数值文化仍由原页面执行，不动捕获／Loader／KeyboardName，不替代 c2 整页迁移 |
| C006a | 已随 `046ed2d` 推送；Ubuntu 226／Windows 232 组、完整 Windows Debug／Release 各 41 步、原 Prepare 和 41 字符 CLR4 oracle 通过；net48 PCLCS 零警告／错误、独立生产／测试／PS 终审通过 | 本批完成。不改变支持版本／日志／会话／首页；现代只保留 BCL 异常类型／顺序，不复制系统所有本地化文案 |
| C006b | 已随 `f1fa26b` 推送；Ubuntu 252／Windows 258 组、Windows Debug／Release 各 41 步与实际旧型／4＋5 字段／GUID-N／UTC 消费者全通过；生产／测试独立终审、net48／测试零警告编译通过，非本批方法逐字不变 | 本批完成。Root 每 Address 仍读两次；fresh 样本的 read/read/write 为 Address 回调计数，不冒称 Root 只调用三次。不是多线程竞态或真实 Minecraft 验证 |
| C006c | 已随 `257e98e`／`6023f3d` 推送，Ubuntu 283／Windows 289 组、Windows Debug／Release 各 41 步及真实新流程／旧卡片消费者通过；有效 NBT 只读／字节、原坏 gzip、精确坏 JSON finally 都有实际样本 | 本批完成，C006d／母项仍等待 WPF；未知进程权限与枚举器失败位置仅原契约／源码审查，不伪称竞态或实游戏验收 |
| C007a | 已随 `459bb0a` 推送；Ubuntu 296／Windows 303 组、Windows 各 41 步与原类型／继承字段／Icon 和完整资源消费者通过，独立终审／零警告编译通过；其余 Store／路径逐字未改，脚本 BOM 保留 | 本批完成，不新增 hash／类型检查或文件并发保证；全局路径与其余 Store 继续 C007b 及后续实际拆分 |
| C007b1 | 已随 `b12b6cf` 推送；Windows 各 41 步、Ubuntu 300／Windows 315 单元组和独立 CLR4／现代 75-case 对照运行成功，Diagnostic 明确输出 8＋2 差异，报告已从 artifact核实并摘要入库 | 本批完成，母项仍未等价。历史 LegacyDriver.vb 只用于原源注入的测试，不在产品编译链，不用于降低产品 VB 统计 |
| C007b2 | `348b0a5`／`21675ee` 已推送；Ubuntu 308／Windows 336、真实严格 137／52 和完整 Windows 各 41 步全通过，四失败数组均空；独立终审／零警告编译通过 | 本批完成，GetDir 仅服务 Normalize 后祖先遍历，短名时序与全部 raw BCL 输入不作完成承诺。后续现代工程仍须验证实际调用链；net48 实构建不冒称现代 WPF |
| C007c1 | `8734df5` 已推送，Ubuntu 311／Windows 339、严格 137／52、Windows 各 41 步及真实 VB enum／入口／Root 原异常顺序全部通过；独立终审／零警告编译通过，脚本 BOM 保留 | 本批完成，不以字符串映射完成关闭整个 Store；Root 仍原 BCL Combine，更多现代 API 边界按实际消费另验 |
| C007c2 | `2d111e1` 已推送；319／347 单元、严格 137／52、Windows 各 41 步／真实旧入口与原资源回归全通过，独立终审与零警告编译通过 | 本批完成，所有写入事务和原取消仍由既有流程执行；不以单规则关闭 Store |
| C007c3 | `70f2545` 已推送；327／356、严格 137／52、Windows 各 41 步与原私有桥／null-device／资源和 Quick Play 消费者全通过；独立终审／零警告编译通过 | 本批完成，筛选不做 SafeName／完整路径归约；更多 ZIP 和扫描语义仍属后继 |
| C007c4 | `dfb81b8`／`a89bc04` 已推送；首次失败及修复记录齐全，339／371、严格 150／56／12 ZIP API、Windows 各 41 步和真实原消费者全通过；独立终审／零警告编译通过，未全局注册编码 | 本批完成，OpenRead 快照与未知 kind 业务惰性分别验收，不外推所有 raw BCL I/O 或现代 WPF；包名/条件/数据/事务范围保留 |
| C007d1a | `745f0b8` 已推送；345／377、严格190／56／40大小探针、Windows各41步与原私有入口全通过；独立终审／零警告编译通过，实际Frame确认1280为1.3 KB、fr-FR为1,3 KB，long.Max仍8589934592 GB | 本批完成；JSON／IO／Icon未在此批迁，不以a完成关闭d1母项 |
| C007d1b | 三批实现已推送且真实397／56、367／399、Windows各41步通过；独立交叉审查、原R尾零缓冲回归与固定MIT来源／artifact证据齐全，前两次严格失败保留 | 本批纯JSON→Detail完成；不是整个Store／全域数字／现代WPF完成，原IO／Icon／catch继续d2 |

下次入口：已验C005～C012原产品链、C027a三控件与C026a真实动画不重复无关全测；当前先复验C013完整本地检查后端＋e6固定Shared初始化的实际双平台／Windows矩阵，具体失败按真实影响修复。原noEXE脚本模式补引用后实际Windows尚未运行，不能冒称该模式通过。之后接C014在线匹配与C015启动补全整链（准备稿不是实现），C016依赖UI同步按链补齐；原VB平台薄壳／全主产品现代化及账号／游戏／发布最终仍后续。

当前仍需完成：剩余产品源码迁移、现代项目和第三方／子模块兼容实现、迁移后完整 GUI／真实账号／游戏启动与旧更新桥验收。初始与 C001 检查点均有独立通过证据，后续任务仍须对各自提交重新验证；N001 审计交付不代表这些运行门禁已经通过。
