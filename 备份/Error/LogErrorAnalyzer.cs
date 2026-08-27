using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace New_Launcher
{
    /// <summary>
    /// 日志错误分析器（增强版），支持 Forge/NeoForge/OptiFine/Quilt/Fabric 安装与游戏运行
    /// </summary>
    public static class LogErrorAnalyzer
    {
        // 错误模式列表
        private static readonly List<ErrorPattern> _errorPatterns = new List<ErrorPattern>();

        // 静态构造函数初始化
        static LogErrorAnalyzer()
        {
            // ========== 最高优先级：Quilt 文件系统兼容性 ==========
            _errorPatterns.Add(new ErrorPattern(
                @"QuiltZipPath.*toFile",
                "Quilt 加载器文件系统兼容性问题。",
                (match, fullLog) =>
                {
                    string modName = ExtractModIdFromQuiltZipPathError(fullLog);
                    if (!string.IsNullOrEmpty(modName))
                    {
                        return $"检测到模组 '{modName}' 尝试使用传统文件路径 API，但 Quilt 加载器不支持。\n" +
                               "该模组可能与 Quilt 加载器不兼容，请尝试：\n" +
                               "① 将模组更新到最新版本；\n" +
                               "② 联系模组作者反馈兼容性问题；\n" +
                               "③ 若无法解决，暂时禁用该模组。";
                    }
                    else
                    {
                        return "⚠️ Quilt 加载器报告了文件系统兼容性问题。\n" +
                               "某个模组尝试使用传统文件路径 API，但 Quilt 加载器不支持。\n\n" +
                               "请尝试以下步骤排查：\n" +
                               "① 查看上方的详细错误日志，找到类似 'at 某个模组包名.类名' 的行；\n" +
                               "② 将对应模组更新到最新版本；\n" +
                               "③ 如果仍无法解决，暂时禁用该模组。";
                    }
                }
            ));

            // ========== Quilt / Fabric 模组加载器相关 ==========
            _errorPatterns.Add(new ErrorPattern(
                @"Mod resolution encountered an error",
                "模组依赖解析失败。",
                "某个模组依赖的模组缺失或版本不匹配，请检查所有模组是否完整下载，并确认其兼容的游戏版本和加载器版本。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"A mod has been requested that doesn't exist",
                "模组依赖缺失。",
                (match) =>
                {
                    string modId = Regex.Match(match.Value, @"Requested by: ([^\s]+)").Groups[1].Value;
                    return $"模组 '{modId}' 依赖了某个不存在的模组。请检查该模组是否需要前置模组，并确保所有模组都已正确放入 mods 文件夹。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Missing or unsupported Java version",
                "Quilt/Fabric 加载器不支持当前 Java 版本。",
                "请使用 Java 17 或更高版本（推荐 Java 17/21），并在启动器设置中指定正确的 Java 路径。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"We were unable to find a valid version of Java",
                "系统未检测到有效的 Java 运行时。",
                "请安装 Java 17 或 21，并确保环境变量 'JAVA_HOME' 已正确设置。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Mixin.*Apply failed",
                "Mixin 注入失败。",
                (match) =>
                {
                    string target = Regex.Match(match.Value, @"target: ([^\s]+)").Groups[1].Value;
                    return $"Mixin 注入失败（目标类：{target}）。这通常由模组间冲突、模组版本不兼容，或模组加载器版本过旧引起。\n" +
                           $"尝试步骤：① 更新所有模组到最新版；\n" +
                           $"② 若使用混合加载器（同时安装 Fabric/Forge 模组），移除其中一组；\n" +
                           $"③ 更新模组加载器版本。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Mixin.*annotation.*could not be found",
                "Mixin 注解缺失。",
                "模组使用的 Mixin 版本与当前加载器不兼容。请更新该模组，或降级加载器到该模组支持的版本。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"java.lang.VerifyError",
                "字节码验证失败。",
                (match) =>
                {
                    string cls = Regex.Match(match.Value, @"class:\s*([^\s]+)").Groups[1].Value;
                    return $"字节码验证失败（类：{cls}）。通常由以下原因导致：\n" +
                           "① 模组被损坏或下载不完整 —— 重新下载该模组；\n" +
                           "② 模组与当前 Java 版本不兼容 —— 检查模组要求的 Java 版本；\n" +
                           "③ 多个模组互相覆盖了同一个类 —— 逐个禁用模组排查冲突。";
                }
            ));

            // ---------- 模组 ID / 版本冲突 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"Duplicate.*mod.*id",
                "模组 ID 重复。",
                (match) =>
                {
                    string modId = Regex.Match(match.Value, @"id:\s*['\""]?([^\s'\"",]+)['\""]?").Groups[1].Value;
                    return $"模组 ID '{modId}' 被多个模组同时使用。请检查 mods 文件夹中是否有同名或冲突的模组文件，移除其中一个。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"requires version.*but found",
                "模组依赖版本不匹配。",
                (match) =>
                {
                    string mod = Regex.Match(match.Value, @"([^\s]+)\s+requires").Groups[1].Value;
                    string required = Regex.Match(match.Value, @"requires version\s+([^\s]+)").Groups[1].Value;
                    string found = Regex.Match(match.Value, @"but found\s+([^\s]+)").Groups[1].Value;
                    return $"模组 '{mod}' 需要版本 '{required}'，但实际找到的是 '{found}'。\n" +
                           "请更新该模组或其依赖项，确保版本匹配。";
                }
            ));

            // ---------- 文件/网络/权限 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"FileNotFoundException\s*(?:.*\s*)?(?:at\s+)?([^:\r\n]+)",
                "文件访问失败。",
                (match) =>
                {
                    string path = match.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(path))
                        return "文件未找到。请检查文件是否存在或路径是否正确。";
                    else
                        return $"文件未找到：{path}。请确认该文件存在且未被占用，或以管理员身份运行启动器。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"远程服务器返回错误: \(404\) 未找到",
                "下载的版本在镜像源上不存在。",
                "可能是该版本已过时或被作者移除。请尝试更换版本，或切换至官方源（CurseForge/Modrinth）。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"文件大小不足，获取结果为 (\d+)，要求至少为 (\d+)",
                "下载的文件不完整（可能是错误页面或网络中断）。",
                (match) =>
                {
                    int got = int.Parse(match.Groups[1].Value);
                    int required = int.Parse(match.Groups[2].Value);
                    return $"文件大小不足（实际 {got} 字节，期望至少 {required} 字节）。请检查网络连接并重新下载。若多次失败，请切换下载源。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"ZipException.*error in opening zip file\s*:\s*([^\r\n]+)",
                "下载的 jar 包损坏或格式错误。",
                (match) =>
                {
                    string file = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(file))
                        return $"Jar 包损坏：{file}。请删除该文件后重新下载，或检查磁盘空间。";
                    else
                        return "Jar 包损坏，请删除后重新下载。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"javax\.net\.ssl\.SSLHandshakeException.*\s+URL\s*:\s*([^\s]+)",
                "SSL 证书验证失败。",
                (match) =>
                {
                    string url = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(url))
                        return $"连接到 {url} 时 SSL 握手失败。可能是系统时间不正确、网络代理干扰或镜像源证书问题。请校准时间，关闭 VPN/代理后重试。";
                    else
                        return "SSL 握手失败，请检查系统时间和网络设置。";
                }
            ));

            // ---------- Mod 兼容性 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"Reflector.*Field not found",
                "OptiFine 与当前 Forge/Fabric 版本不兼容。",
                "请降级 OptiFine 或升级 Forge 到对应的稳定组合。可参考 OptiFine 官网的兼容列表。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Class not present.*net\.minecraftforge",
                "Forge 核心类缺失，可能是 Forge 安装不完整或版本不匹配。",
                "请重新安装 Forge，或尝试更换 Forge 版本。若使用 NeoForge，请确保版本对应。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"java\.lang\.NoSuchMethodError",
                "方法调用冲突，通常由 Mod 与 Forge/NeoForge 版本不匹配引起。",
                "检查所有 Mod 是否支持当前游戏版本和加载器，尝试逐个移除 Mod 排查。"
            ));

            // ---------- 内存/性能 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"java\.lang\.OutOfMemoryError",
                "内存溢出。",
                "请增加启动器分配的内存（如 4096 MB 或更高）。若使用 32 位 Java，请改用 64 位。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"StackOverflowError",
                "堆栈溢出，通常由渲染距离过高或某些 Mod 的递归调用导致。",
                "降低渲染距离，或关闭部分 Mod（如光影包、小地图）进行测试。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Could not reserve enough space for (\d+)KB object heap",
                "JVM 无法分配足够内存。",
                (match) =>
                {
                    int kb = int.Parse(match.Groups[1].Value);
                    int mb = kb / 1024;
                    return $"无法分配 {mb} MB 内存。请减少分配的内存大小，或检查系统是否有足够的可用内存。32 位系统最大支持约 1.5GB。";
                }
            ));

            // ---------- 通用安装失败 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"Install profile not found",
                "Forge/NeoForge 安装配置文件缺失。",
                "请检查网络连接，或手动下载对应的 installer 文件并重试。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Failed to run process",
                "启动子进程失败（可能是 Java 路径无效或权限问题）。",
                "请检查 Java 安装是否正确，并确保启动器有权限执行外部程序。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"No suitable Java installation found",
                "系统中未找到可用的 Java 运行时。",
                "请安装 Java 17 或 21，并在启动器设置中指定 Java 路径。"
            ));

            // ---------- OptiFine 专用 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"OptiFine.*not installed",
                "OptiFine 未正确安装。",
                "若为独立安装，请运行 jar 安装包；若为 Mod 安装，请确认 jar 文件放在 mods 文件夹内，且 Forge/Fabric 已正确加载。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"OptiFine.*requires ([\w\s]+) version",
                "OptiFine 版本要求不匹配。",
                (match) =>
                {
                    string required = match.Groups[1].Value.Trim();
                    return $"OptiFine 需要 {required}，请检查当前游戏版本是否符合要求。";
                }
            ));

            // ---------- NeoForge 特有 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"NeoForge.*not compatible with this Minecraft version",
                "NeoForge 版本与游戏版本不兼容。",
                "请下载匹配当前游戏版本的 NeoForge 安装包（如 1.20.1 -> NeoForge 47.x）。"
            ));

            // ---------- 图形/驱动 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"GLFW error.*Unable to find (support|symbol)",
                "OpenGL 初始化失败，可能是显卡驱动过旧或不支持。",
                "请更新显卡驱动，并确保系统满足游戏的最低硬件要求。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Pixel format not accelerated",
                "显卡驱动不支持硬件加速。",
                "请更新显卡驱动，或尝试使用软件渲染（性能较低）。"
            ));

            // ---------- 游戏资源/渲染错误 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"java.lang.NullPointerException.*(?:model|texture|render)",
                "空指针异常——通常由纹理或模型加载失败引起。",
                "某个纹理或模型文件无法加载（可能损坏或缺失）。\n" +
                "尝试：① 关闭光影包或资源包；② 将渲染距离降低；③ 如果近期添加了新模组，尝试移除后重启。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Could not load texture",
                "纹理加载失败。",
                (match) =>
                {
                    string tex = Regex.Match(match.Value, @"texture:\s*([^\s]+)").Groups[1].Value;
                    return $"纹理文件加载失败：{tex}。\n" +
                           "可能是资源包损坏，或某个模组的纹理文件格式不正确。尝试禁用近期添加的资源包或模组。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Shader.*compilation failed",
                "光影包编译失败。",
                (match) =>
                {
                    string shader = Regex.Match(match.Value, @"shader:\s*([^\s]+)").Groups[1].Value;
                    return $"光影着色器编译失败：{shader}。\n" +
                           "尝试：① 更换或更新光影包；② 更新显卡驱动；③ 降低或关闭光影设置。";
                }
            ));

            // ---------- 配置文件 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"Cannot invoke.*getConfig.*because.*null",
                "模组配置文件读取失败。",
                "某个模组无法读取其配置文件，可能是配置文件损坏或格式错误。\n" +
                "尝试删除该模组的配置文件夹（位于 config/ 下），让其重新生成。"
            ));

            // ---------- 网络/更新 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"java.net.UnknownHostException",
                "域名解析失败，无法连接到服务器。",
                "网络连接问题：域名解析失败。\n" +
                "尝试：① 检查网络连接；② 关闭 VPN 或代理；③ 更换下载源/镜像源。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"java.net.SocketTimeoutException",
                "网络连接超时。",
                "网络请求超时，可能是服务器响应慢或网络不稳定。\n" +
                "尝试：① 检查网络连接；② 切换下载源；③ 稍后重试。"
            ));

            // ---------- 系统环境 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"Cannot run program.*CreateProcess error=5",
                "权限不足，无法执行子进程。",
                "启动器缺少执行外部程序的权限。\n" +
                "请尝试以管理员身份运行启动器，或检查杀毒软件是否拦截了 Java 进程。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"The specified executable is not a valid application",
                "Java 可执行文件无效。",
                "指定的 Java 路径不正确或文件已损坏。\n" +
                "请在启动器设置中重新选择正确的 javaw.exe 路径（Java 17/21 的 bin 目录下）。"
            ));

            // ---------- 游戏核心 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"Maybe you need to reinstall",
                "游戏核心文件损坏或版本不完整。",
                "游戏核心文件可能已损坏或下载不完整。\n" +
                "尝试在启动器中重新下载/修复该版本。"
            ));

            // ---------- Java 版本问题 ----------
            _errorPatterns.Add(new ErrorPattern(
                @"UnsupportedClassVersionError.*class file version (\d+)\.(\d+)",
                "Java 版本不兼容。",
                (match) =>
                {
                    int major = int.Parse(match.Groups[1].Value);
                    int minor = int.Parse(match.Groups[2].Value);
                    string required = GetJavaVersion(major);
                    return $"当前 Java 版本过旧（class file version {major}.{minor}），需要 {required} 或更高版本。请安装并切换到对应的 Java 运行时。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Unsupported major\.minor version (\d+)\.(\d+)",
                "Java 版本不匹配。",
                (match) =>
                {
                    int major = int.Parse(match.Groups[1].Value);
                    string required = GetJavaVersion(major);
                    return $"需要 {required}，当前使用的 Java 版本过低。请检查启动器中的 Java 路径设置。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"java\.lang\.ExceptionInInitializerError",
                "Java 初始化错误，通常由于 Java 版本不兼容或环境变量冲突。",
                "请确保使用正确的 Java 版本（建议 Java 17/21），并尝试重启启动器或计算机。"
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Unrecognized option: --([^\s]+)",
                "JVM 参数不兼容。",
                (match) =>
                {
                    string option = match.Groups[1].Value;
                    return $"当前 Java 版本不支持参数 '--{option}'。\n" +
                           $"这类参数通常需要较新版本的 Java 运行时（例如 Java 17 或更高）。\n" +
                           $"请尝试升级 Java 版本，或选择与当前 Java 版本兼容的 Minecraft/Forge 版本。";
                }
            ));

            _errorPatterns.Add(new ErrorPattern(
                @"Unrecognized VM option '([^']+)'",
                "Java 虚拟机选项不兼容。",
                (match) =>
                {
                    string option = match.Groups[1].Value;
                    return $"当前 Java 虚拟机不支持选项 '{option}'。\n" +
                           $"这通常是因为该选项需要较新版本的 Java（例如 Java 17 或更高）。\n" +
                           $"请安装并使用较新版本的 Java，或选择与当前 Java 兼容的 Minecraft/Forge 版本。";
                }
            ));
        }

        /// <summary>
        /// 根据 class file major version 返回对应的 Java 版本名称
        /// </summary>
        private static string GetJavaVersion(int major)
        {
            if (major >= 52)
            {
                int versionNum = major - 44;
                return $"Java {versionNum} (major {major})";
            }
            // 针对 45~51 的古老版本做特殊映射
            switch (major)
            {
                case 51: return "Java 7";
                case 50: return "Java 6";
                case 49: return "Java 5";
                case 48: return "Java 1.4";
                default: return $"未知 (major {major})";
            }
        }

        /// <summary>
        /// 分析日志文本，返回第一条匹配的错误信息及解决方案
        /// </summary>
        /// <param name="logText">完整的日志文本（可包含多行）</param>
        /// <returns>用户可读的报错信息；若无匹配则返回 null</returns>
        public static string Analyze(string logText)
        {
            if (string.IsNullOrEmpty(logText))
                return null;

            foreach (var pattern in _errorPatterns)
            {
                var match = pattern.Regex.Match(logText);
                if (match.Success)
                {
                    if (pattern.MessageBuilder != null)
                        return pattern.MessageBuilder(match, logText);
                    else
                        return pattern.FixedMessage;
                }
            }
            return null;
        }

        /// <summary>
        /// 分析日志，返回详细的错误报告（含原始匹配内容）
        /// </summary>
        public static ErrorReport AnalyzeDetailed(string logText)
        {
            if (string.IsNullOrEmpty(logText))
                return null;

            foreach (var pattern in _errorPatterns)
            {
                var match = pattern.Regex.Match(logText);
                if (match.Success)
                {
                    string message = pattern.MessageBuilder != null
                        ? pattern.MessageBuilder(match, logText)
                        : pattern.FixedMessage;

                    return new ErrorReport
                    {
                        MatchedPattern = pattern.Regex.ToString(),
                        MatchedText = match.Value,
                        ErrorMessage = message,
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// 将分析结果写入文件（纯文本，仅包含错误信息）
        /// </summary>
        /// <param name="logText">日志原文</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="append">是否追加到文件末尾（默认覆盖）</param>
        public static void WriteAnalysisResultToFile(string logText, string outputPath, bool append = false)
        {
            string result = Analyze(logText);
            if (string.IsNullOrEmpty(result))
                result = "未检测到已知错误。";

            if (append)
                File.AppendAllText(outputPath, result + Environment.NewLine, Encoding.UTF8);
            else
                File.WriteAllText(outputPath, result, Encoding.UTF8);
        }

        /// <summary>
        /// 将详细分析报告写入文件（包含匹配模式、原文和错误信息）
        /// </summary>
        public static void WriteDetailedReportToFile(string logText, string outputPath, bool append = false)
        {
            var report = AnalyzeDetailed(logText);
            string content;
            if (report == null)
                content = "未检测到已知错误。";
            else
            {
                content = $"匹配模式: {report.MatchedPattern}\n" +
                          $"匹配原文: {report.MatchedText}\n" +
                          $"错误信息: {report.ErrorMessage}";
            }

            if (append)
                File.AppendAllText(outputPath, content + Environment.NewLine + new string('-', 40) + Environment.NewLine, Encoding.UTF8);
            else
                File.WriteAllText(outputPath, content, Encoding.UTF8);
        }

        /// <summary>
        /// 直接从日志文件读取并分析，将结果写入另一个文件
        /// </summary>
        public static void AnalyzeLogFileAndWrite(string logFilePath, string outputFilePath, bool detailed = false, bool append = false)
        {
            if (!File.Exists(logFilePath))
            {
                string msg = "日志文件不存在。";
                if (append)
                    File.AppendAllText(outputFilePath, msg + Environment.NewLine);
                else
                    File.WriteAllText(outputFilePath, msg);
                return;
            }

            string logText = File.ReadAllText(logFilePath, Encoding.UTF8);
            if (detailed)
                WriteDetailedReportToFile(logText, outputFilePath, append);
            else
                WriteAnalysisResultToFile(logText, outputFilePath, append);
        }

        private static Dictionary<string, string> _packageToModIdCache = null;

        /// <summary>
        /// 从 QuiltZipPath 错误的堆栈中提取模组名
        /// </summary>
        private static string ExtractModIdFromQuiltZipPathError(string logText)
        {
            // 1. 先定位到 "UnsupportedOperationException: Only the default FileSystem supports 'Path.toFile()'"
            // 或直接定位到 "class org.quiltmc.loader.impl.filesystem.QuiltZipPath"
            // 这些行后面会跟着堆栈，其中第一个非框架的 at 行就是出错模组

            var exceptionMatch = Regex.Match(logText,
                @"UnsupportedOperationException: Only the default FileSystem supports 'Path\.toFile\(\)'[^\n]+\n((?:[^\n]*\n)*?)at\s+([a-zA-Z0-9_.]+)\.");

            if (!exceptionMatch.Success)
            {
                // 尝试备用匹配：直接找 QuiltZipPath 后面紧跟的 at 行
                var fallbackMatch = Regex.Match(logText,
                    @"QuiltZipPath[^\n]+\n((?:[^\n]*\n)*?)at\s+([a-zA-Z0-9_.]+)\.");
                if (fallbackMatch.Success)
                {
                    return ExtractModIdFromClass(fallbackMatch.Groups[2].Value);
                }
                return null;
            }

            // 获取第一个 at 行的类全名
            string fullClass = exceptionMatch.Groups[2].Value;
            return ExtractModIdFromClass(fullClass);
        }

        /// <summary>
        /// 从类全名中提取模组 ID（包名倒数第二段或第三段）
        /// </summary>
        private static string ExtractModIdFromClass(string fullClass)
        {
            if (string.IsNullOrEmpty(fullClass))
                return null;

            // 跳过框架类
            if (fullClass.StartsWith("org.quiltmc.loader") ||
                fullClass.StartsWith("net.fabricmc.loader") ||
                fullClass.StartsWith("org.spongepowered.asm.mixin") ||
                fullClass.StartsWith("java.") ||
                fullClass.StartsWith("sun.") ||
                fullClass.StartsWith("jdk.") ||
                fullClass.StartsWith("net.minecraft.") ||
                fullClass.StartsWith("com.mojang."))
                return null;

            string[] parts = fullClass.Split('.');
            if (parts.Length < 2)
                return null;

            // 优先取倒数第二段（通常是 modid）
            string candidate = parts[parts.Length - 2];
            if (!string.IsNullOrEmpty(candidate) &&
                candidate.Length > 2 &&
                candidate != "client" &&
                candidate != "main" &&
                candidate != "minecraft" &&
                candidate != "util" &&
                candidate != "impl" &&
                candidate != "core" &&
                candidate != "common" &&
                candidate != "api" &&
                candidate != "lib")
            {
                return candidate;
            }

            // 如果倒数第二段被过滤了，取倒数第三段
            if (parts.Length >= 3)
            {
                candidate = parts[parts.Length - 3];
                if (!string.IsNullOrEmpty(candidate) &&
                    candidate.Length > 2 &&
                    candidate != "minecraft")
                {
                    return candidate;
                }
            }

            // 如果都失败，返回最后一段（但要排除常见类名）
            string last = parts[parts.Length - 1];
            if (last.Length > 2 && last != "Minecraft" && last != "Client" && last != "Main")
                return last;

            return null;
        }

        // ---------- 内部类型 ----------
        private class ErrorPattern
        {
            public Regex Regex { get; }
            public string FixedMessage { get; }
            public Func<Match, string, string> MessageBuilder { get; }

            // 接受两个参数的 MessageBuilder
            public ErrorPattern(string pattern, string fixedMessage, Func<Match, string, string> messageBuilder)
            {
                Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                FixedMessage = fixedMessage;
                MessageBuilder = messageBuilder;
            }

            // 兼容旧的一个参数的 MessageBuilder
            public ErrorPattern(string pattern, string fixedMessage, Func<Match, string> messageBuilderOld)
            {
                Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                FixedMessage = fixedMessage;
                MessageBuilder = (match, _) => messageBuilderOld(match);
            }

            // 纯静态消息
            public ErrorPattern(string pattern, string fixedMessage, string staticSuggestion)
            {
                Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                FixedMessage = fixedMessage + " " + staticSuggestion;
                MessageBuilder = null;
            }
        }

        /// <summary>
        /// 详细错误报告
        /// </summary>
        public class ErrorReport
        {
            public string MatchedPattern { get; set; }
            public string MatchedText { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}