# MMC runtime smoke

独立 Windows / .NET 10 smoke 工程，ProjectReference 真实现代 PCLCS。它不属于 Migration.Tests 的默认 all，不扫描模组目录、不启动玩家游戏，也不执行下载的未知 JAR。测试只运行自制 Probe / TestLoader、原项目 JavaWrapper 和本轮自有 argv bridge。

完整验收必须显式提供三个实际 Java 可执行文件，不能把缺失 JDK 当成通过。Java 17 同目录还需 `javac`，可用 `PCLLF_MMC_TEST_JAVAC` 单独指定编译器。测试会核对实际 `java.specification.version`；装错 major 也会失败。

```powershell
git submodule update --init MeloongCore
$env:PCLLF_MMC_TEST_JAVA8 = 'C:\path\to\java8\bin\java.exe'
$env:PCLLF_MMC_TEST_JAVA17 = 'C:\path\to\java17\bin\java.exe'
$env:PCLLF_MMC_TEST_JAVA21 = 'C:\path\to\java21\bin\java.exe'
dotnet run --project tests/PCLLF.Mmc.Runtime.Tests --configuration Debug
```

`-- --group bridge` 仅核对自有 bridge 的源码与固定 bytecode/JAR 身份，不需要安装 Java；这不算真实进程验证。`argv` 与 `long-arguments` 使用 8/17/21，其他进程/类检查至少需要 8/17 及编译器。`session-budget` 只需 Java17 编译器，验证最终会话 nonce 的长度预留：runtime 参数加 nonce 恰好 32767 时允许，再增加一个字符明确拒绝，预留不会写入实际参数。可运行 `-- --list-groups` 查看七个分组。

CI 可在独立 Windows job 中按 [setup-java 官方输出协议](https://github.com/actions/setup-java/blob/main/README.md#outputs)，用 `actions/setup-java@v6` 分别安装 8、17、21，记录各步输出 `path`，再设置：

```yaml
- uses: actions/setup-java@v6
  id: java8
  with: { distribution: temurin, java-version: '8' }
- uses: actions/setup-java@v6
  id: java17
  with: { distribution: temurin, java-version: '17' }
- uses: actions/setup-java@v6
  id: java21
  with: { distribution: temurin, java-version: '21' }
- shell: pwsh
  env:
    PCLLF_MMC_TEST_JAVA8: ${{ steps.java8.outputs.path }}/bin/java.exe
    PCLLF_MMC_TEST_JAVA17: ${{ steps.java17.outputs.path }}/bin/java.exe
    PCLLF_MMC_TEST_JAVA21: ${{ steps.java21.outputs.path }}/bin/java.exe
  run: dotnet run --project tests/PCLLF.Mmc.Runtime.Tests --configuration Debug
```

上例是后续 CI 接线建议；本次没有修改工作流。该 job 仍需现有 .NET 10 / WindowsDesktop 工具链及固定 Shared 子模块。

Wrapper 本体没有修改或重打包。`MmcRuntimeBridgeTransport.JavaSource` 与固定 Java 8 bytecode 为自有宿主实现，适用根 LICENCE。VM classpath 先放已验证的 Wrapper / bridge，再放编译计划的完整游戏 classpath，主 JAR 仍在游戏清单末尾；先观察支持 JAR，阻止它们遮蔽必要游戏类。VM 和游戏参数的 CP 都相对已绑定 GameRoot 表示，文件观察和输入绑定继续使用绝对路径。`-Duser.dir` 绑定 GameRoot，包/用户试图切换到其它目录会明确拒绝。进程主类使用 Wrapper，应用参数保留原 Wrapper 的 `-jar <Wrapper> <bridge> <frame>` 标记协议；MMC3 封包包含解压长度与 zlib 压缩的 MMC2 长度帧，再用 Base64URL 传递。帧只留在内存，整个帧从日志/非敏感指纹中替换，解压受 1 MiB 内层预算限制。

原 Wrapper 会重新解析 Windows 原始命令行，遗漏空参数并折叠字面双反斜杠。bridge 恢复完整游戏 argv 与 `-D` 属性的末值；VM 在初始化 system loader 前已通过已绑定 `java.class.path` 属性取得完整 CP，从而避开原 Wrapper 对自定义 loader 的 BuiltinClassLoader 注入假定。

本次 bridge 由真实 `javac 17.0.21 --release 8 -encoding UTF-8 -g:none` 编译，class SHA-256 为 `dbcde6bc07b3981dc59bda69b21adad092bc335901f1fa0a5cd6953e01c1d94a`。进程测试会重新编译内嵌源码并核对该 class 身份，纯 `bridge` 组仍不依赖 JDK。

`long-arguments` 合成 92 个自有 JAR 与约 139 UTF-16 单元的深 GameRoot。自定义系统加载器在构造时逐一读取 92 个不同 JAR 的资源，证明 VM 初始化阶段已可解析完整 CP；运行 Probe 再对照实际文件、顺序、mainJar 尾位、游戏 CP 宏及空/Unicode/反斜杠/引号参数。测试同时记录旧绝对 CP 加原始 Base64 重复帧的长度下界与新命令行的精确长度（含 .NET Windows 参数引号及结尾 NUL）。自有子进程直接读取 Win32 `GetCommandLineW`，独立核对引号计数与恰好 32767 的实际启动边界；超限在创建进程前拒绝。Java 8/17/21 均覆盖 1 MiB 解压上限、越界、截断和尾随压缩数据，以及账号切换指纹稳定与整个敏感帧脱敏。该组不下载或执行 GTNH/Minecraft。

多版本 JAR 的观察按 manifest 与实际 Java major 选取真实 entry，并保留 MR entry 路径。当前 Core 结构证明对 MR class 仍可保守拒绝；观察通过不表示所有自定义 classloader 都按标准 JarFile 规则工作，也不表示实际 Minecraft 启动、世界、修复已验收。隐式 manifest Class-Path 与未建立执行输入绑定的 agent/argfile/替代模块模式明确拒绝。

窄审查回归还按 [Java 17 JarFile 实现属性](https://docs.oracle.com/en/java/javase/17/docs/api/java.base/java/util/jar/JarFile.html)核对最终生效的 `jdk.util.jar.enableMultiRelease` 与 `jdk.util.jar.version`：包/用户的 `-D` 顺序不改写，支持 JAR 与游戏 JAR 使用同一策略；`false` 禁用，`force` 不会给缺少 manifest 标记的 JAR 凭空开启 MR，版本上限按实际 Java 夹到 8～当前 major。Java 8 忽略这些 MR 属性。类观察保留每个 JAR 的显式存在/缺失，只有 classpath 最早实际命中的版本需要通过字节码兼容检查；后面的被遮蔽副本不阻止有效启动。`classpath` 组会与真实 Java 8/17 进程对照上述行为，并验证真正首个不兼容版本仍被拒绝。
