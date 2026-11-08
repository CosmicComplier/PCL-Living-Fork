# PCL Living Fork 来源与版权声明

项目标识：PCL-Living-Fork。程序名称：PCL Living Fork（原 LivingPCL）。内部程序集及既有数据标识保留兼容。

本项目是基于 Plain Craft Launcher（PCL）原项目的第三方修改版本，不是龙腾猫跃、成都瓜皮龙科技有限公司或原 PCL 团队发布的官方版本，也不表示已获得其背书。

## 原作与来源

- 原作：Plain Craft Launcher（PCL）。
- 原作作者：龙腾猫跃及 PCL 原项目贡献者；具体贡献记录见原仓库 Git 历史与关于页面。
- 原作版权声明：Copyright © 成都瓜皮龙科技有限公司。
- 上游原仓库：https://github.com/Meloong-Git/PCL 。
- 本分支仓库：https://github.com/DDZmumo/PCL-Living-Fork ，由本次克隆来源 `DDZmumo/PCL` 经 `LivingPCL2-Re` 更名而来；旧仓库保留为兼容更新入口。
- 本分支代码基线：`5ef81e490ae46b1100d61e33d0183094c7a80ca8`，PCL 2.14.0.0 正式版。

## 源码基线与逆向参考

当前工程基于上述源码继续修改；部分功能结合对原版发行程序的分析实现。维护者补充的逆向参考来源包含赞助快照版。源码基线、参考发行版和本项目的最终构建属于不同层次，不将整个项目笼统称为官方开源版或官方赞助快照版。

对发行程序的分析不改变原作、源码与资产的版权归属；本项目保持独立第三方身份，不主张获得原作权利人背书。
- 原作者赞助链接：https://meloong.com/afd/a/LTCat 。

## 许可及第三方组件

本项目保留原仓库的《PCL 分发有限许可》链接与《PCL 存储库合理使用指南》，见 [LICENCE](LICENCE)。更名、重新编译或添加本地修改不改变原作与第三方资产的版权归属，也不授予原许可未授予的权利。本项目修改部分的权利归相应贡献者所有，不主张对原作拥有版权。

MeloongCore、第三方库、图标、字体、帮助文件及其他资源分别遵循其自身的许可与版权声明；保留关于页的原作者、协力开发者、资源贡献者和第三方许可列表。MeloongCore 的声明见 [MeloongCore/LICENSE.txt](MeloongCore/LICENSE.txt)。

以下为本项目实际使用或移植内容的现有许可入口，具体文件与固定来源见 [源码来源记录](modern/Main/SOURCE_PROVENANCE.md)：

- MeloongCore Shared 及选择性移植的 WPF 辅助代码：Apache-2.0；WPF 移植副本的完整文本见 [MeloongCore-Wpf/LICENSE.txt](modern/ThirdParty/MeloongCore-Wpf/LICENSE.txt)，原文件署名和修改说明保留在对应实现中。
- .NET bundle 身份读取器使用的格式实现：MIT，Copyright (c) .NET Foundation and Contributors；见 [完整许可](modern/ThirdParty/dotnet-runtime-bundle/LICENSE.TXT)及[固定源文件清单](modern/ThirdParty/dotnet-runtime-bundle/source-manifest.json)。既有 CoreCLR 数值解析算法移植的 MIT 许可与署名完整保留在[对应源码文件头](PCLCS/Minecraft/LegacyPackMetadataNumberParser.cs)。
- Markdig 1.6.0：BSD 2-Clause，Copyright (c) 2016-2026, Alexandre Mutel；见[完整许可](modern/ThirdParty/Markdig/LICENSE.txt)。该文本同时作为应用资源供关于页展示。

上述 MeloongCore、.NET 和 Markdig 完整许可均作为应用资源保留，可在关于页的第三方许可区域展开阅读；单文件打包沿用这些资源。

新原创 [PCLLF.MMCC.Core](PCLLF.MMCC.Core/README.md) 使用自身的 [MPL-2.0 许可](PCLLF.MMCC.Core/LICENSE)；其许可范围不替换宿主的根许可、既有第三方声明或输入整合包的许可。

Minecraft、Mojang 和 Microsoft 的名称与相关资产归相应权利人所有。本项目不是 Minecraft 官方产品，未经 Mojang 或 Microsoft 批准，也不与其关联。

## 本地修改范围

本分支使用 Minecraft 下载限制引入前的源码基线，禁用官方启动器更新，使用 PCL Living Fork 名称，并补充独立第三方身份、来源与版权说明。原仓库附带的旧版发行 ZIP 和历史更新补丁已从工作树移除，原始记录仍可通过 Git 历史追溯。
