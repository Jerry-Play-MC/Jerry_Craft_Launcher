# Jerry Craft Launcher

> A lightweight Minecraft launcher based on C# .NET Framework 3.5

## ✨ Features
- Perfectly compatible with Windows 7
- Download vanilla Minecraft
- Manage your Minecraft instances
- Support version isolation and non-isolation
- Currently supports Forge, NeoForge, Fabric, Quilt loaders (OptiFine is in active development)
- Support modifying NBT data of saved games to rescue dead hardcore mode worlds
- Support importing/exporting the `.minecraft` folder to/from the official launcher
- Support basic error analysis (may not be comprehensive)
- Integrated with Modrinth's MOD API, download your favorite mods, modpacks, datapacks, resource packs, and shader packs

## 🚀 How to Use

### Method 1: Direct Run (Recommended for Regular Users)
1. Go to the **Releases** section on the right side and download the latest `JerryCraftLauncher.exe`.
2. Double-click to run (no installation required).
3. On first launch, the program will guide you to select your `.minecraft` folder location (supports selecting the official launcher's directory).
4. Log in with a Microsoft account or choose offline mode, then click Launch to play.

> 💡 If your antivirus software flags the launcher, please add it to the exclusion list. This is because the launcher uses code compression (Costura.Fody) and does not have a digital signature.

### Method 2: Build from Source (For Developers)
1. Use `git clone` to clone this repository to your local machine.
2. Open the solution file (`.sln`) with **Visual Studio 2022** (or later).
3. Right-click the solution and select **Restore NuGet Packages** (ensure Fody, fNbt, and other dependencies are fully downloaded).
4. Switch the build configuration to `Release` and set the target platform to `x86`.
5. Click **Build** -> **Build Solution**. The generated `.exe` file will be located in the `bin\x86\Release` directory.

## ⚠️ Current Status and Notes
> **Development Background**: This project is a personal practice work independently completed by the author over two months during summer break, aimed at learning C# and .NET development. Since the author is still in the learning phase, there may be shortcomings in code style and architecture design. **Please use with caution in production environments or with important game saves.**

> **Known Issues**: Older versions of NeoForge (e.g., 1.21.1 NeoForge 21.1.230) may fail to launch. This is currently being fixed.

> **Microsoft Authentication Compliance**: This launcher only uses Microsoft OAuth login with explicit user consent. It does not store user passwords and is not involved in any account theft or black-market activities.

## 🛠️ Tech Stack & Dependencies
- Target Framework: .NET Framework 3.5
- Development Environment: Visual Studio 2026
- Core Libraries:
    Fody 2.0.0
    Costura.Fody 1.6.2
    fNbt 0.6.4
    Imazen.WebP 3.4.1
    NAudio 1.10.0
    NAudio.Lame 1.0.5
    NVorbis 0.8.6

## 🙏 Acknowledgements
The learning and development of this project referenced the following excellent open-source projects:
- **Plain Craft Launcher 2**: https://github.com/Hex-Dragon/PCL2
- **PCL Community Edition**: https://github.com/PCL-Community/PCL-CE
- Special thanks to **DeepSeek** for programming guidance and suggestions.