# Jerry Craft Launcher

> 一个基于 C# .NET Framework 3.5 的 超轻量Minecraft启动器

## ✨ 功能特点
- 完美兼容 Windows 7
- 下载原版Minecraft
- 管理你的Minecraft实例
- 支持版本隔离、不隔离
- 目前支持Forge、NeoForge、Fabric、Quilt加载器（OptiFine目前正在加紧做）
- 支持修改存档NBT数据救急已死亡的极限模式存档
- 支持向官方启动器导入目前.minecraft文件夹
- 支持较为简单的报错分析（可能不完善）
- 接入Modrinth的MODAPI，下载您心仪的Mod、整合包、数据包、资源包、光影包

## 🚀 如何使用

### 方式一：直接运行（推荐普通用户）
1. 前往右侧 **Releases**（发布页）下载最新的 `JerryCraftLauncher.exe`。
2. 双击运行即可（无需安装）。
3. 首次打开时，程序会自动引导您选择 `.minecraft` 文件夹位置（支持选择官方启动器的目录）。
4. 登录正版账号或选择离线模式，点击启动即可游玩。

> 💡 如果杀毒软件报错，请添加信任区，因为本启动器使用了代码压缩（Costura.Fody）且未购买数字签名。

### 方式二：自行编译运行（面向开发者）
1. 使用 `git clone` 将本仓库克隆到本地。
2. 使用 **Visual Studio 2022**（或更高版本）打开解决方案文件（`.sln`）。
3. 右键解决方案，选择 **还原 NuGet 包**（确保 Fody、fNbt 等依赖下载完整）。
4. 将生成配置切换为 `Release`，目标平台选 `x86`。
5. 点击 **生成** -> **生成解决方案**，生成的 `.exe` 文件位于 `bin\x86\Release` 目录下。

## ⚠️ 当前状态与注意事项
> **开发背景**：本项目是作者在暑期两个月内独立完成的个人练习作品，旨在学习 C# 和 .NET 开发。由于作者目前仍在学习阶段，代码风格和架构设计上可能存在不足之处，**请谨慎用于生产环境或重要存档**。

> **已知问题**：较老版本的 NeoForge（如1.21.1 NeoForge 21.1.230）可能无法正常启动，目前正在修复中。

> **正版登录合规说明**：本启动器仅在用户主动授权的情况下使用 Microsoft OAuth 登录，不存储用户密码，不涉及任何盗号或黑卡行为。

## 🛠️ 技术栈与依赖
- 目标框架：.NET Framework 3.5
- 开发环境：Visual Studio 2026
- 核心库：
    Fody 2.0.0
    Costura.Fody 1.6.2
    fNbt 0.6.4
    Imazen.WebP 3.4.1
    NAudio 1.10.0
    NAudio.Lame 1.0.5
    NVorbis 0.8.6

## 🙏 致谢
本项目的学习和开发过程中参考了以下优秀开源项目：
- **Plain Craft Launcher 2**：https://github.com/Hex-Dragon/PCL2
- **PCL Community Edition**：https://github.com/PCL-Community/PCL-CE
- 特别感谢 **DeepSeek** 提供的编程指导与建议。