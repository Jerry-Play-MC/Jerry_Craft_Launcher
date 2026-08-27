using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace New_Launcher
{
    /// <summary>
    /// 终极启动基类，处理所有底层进程启动、日志、崩溃报告、参数转义
    /// </summary>
    public abstract class BaseLauncher : ILauncher
    {
        // 子类只需实现：构建命令列表（不含启动逻辑）
        protected abstract List<string> BuildCommand(LaunchContext context);

        // 可选重写：启动前额外校验（如检查特定库文件）
        protected virtual void ValidateEnvironment(LaunchContext context) { }

        public void Launch(LaunchContext context)
        {
            string logFilePath = null;
            string classpathFile = null;
            try
            {
                ValidateEnvironment(context);

                List<string> command;
                try
                {
                    command = BuildCommand(context);
                }
                catch (Exception ex)
                {
                    // 不再写文件，直接重新抛出
                    Console.WriteLine($"[启动] BuildCommand 异常: {ex.Message}");
                    throw;
                }

                // ---- 处理超长 classpath ----
                int cpIndex = command.IndexOf("-cp");
                if (cpIndex >= 0 && cpIndex + 1 < command.Count)
                {
                    string cpValue = command[cpIndex + 1];
                }

                // 生成调试 BAT
                WriteDebugBat(command, context.VersionDataPath);

                // ---- 确定日志目录 ----
                string logDir;
                if (context.IsVersionIsolated)
                    logDir = Path.Combine(context.VersionDataPath, "logs");
                else
                    logDir = Path.Combine(context.MinecraftDir, "logs");
                Directory.CreateDirectory(logDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string safeVersion = context.VersionId.Replace('\\', '_').Replace('/', '_');
                logFilePath = Path.Combine(logDir, $"{timestamp}_{safeVersion}.log");

                // 打印 command 内容，查看 -cp 后的值
                int idx = command.IndexOf("-cp");
                if (idx >= 0 && idx + 1 < command.Count)
                {
                    Console.WriteLine($"[启动] -cp 参数值: {command[idx + 1]}");
                }

                // ---- 启动进程 ----
                ProcessStartInfo psi = new ProcessStartInfo();
                string argsStr = BuildArgumentString(command, skipFirst: true);
                Console.WriteLine($"[启动] 最终启动参数: {argsStr}");
                psi.Arguments = argsStr;
                psi.FileName = command[0];
                psi.Arguments = BuildArgumentString(command, skipFirst: true);
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.WorkingDirectory = context.VersionDataPath;
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;

                // 进程退出码
                int exitCode = -1;
                string fullLogPath = logFilePath;

                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                        throw new Exception("进程启动失败，Process.Start 返回 null。");

                    // 7. 绑定日志管理器（自动过滤颜色码，实时写入）
                    using (var logManager = new GameLogManager(process, logFilePath))
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        // 8. 等待退出
                        process.WaitForExit();
                        exitCode = process.ExitCode;

                        // 9. 处理退出（修改此处）
                        if (exitCode != 0)
                        {
                            logManager.AppendCrashReport(context.MinecraftDir);
                            Console.WriteLine($"[启动] 异常退出 (Code {exitCode})，日志: {logFilePath}");
                        }
                        else
                        {
                            Console.WriteLine($"[启动] 正常退出，日志: {logFilePath}");
                        }
                    } // logManager 在此释放，日志文件流已关闭
                } // process 在此释放

                // ===== 现在日志文件已关闭，可以安全读取 =====
                if (exitCode != 0)
                {
                    string analyzedError = null;
                    try
                    {
                        if (File.Exists(logFilePath))
                        {
                            string logContent = File.ReadAllText(logFilePath, Encoding.UTF8);
                            analyzedError = LogErrorAnalyzer.Analyze(logContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[启动] 日志分析失败: {ex.Message}");
                    }

                    if (string.IsNullOrEmpty(analyzedError))
                    {
                        analyzedError = $"游戏进程异常退出，退出码: {exitCode}。\n" +
                                        $"请查看日志文件：{logFilePath}\n" +
                                        "你也可以点击下方按钮导出完整错误报告。";
                    }

                    // 抛出包含详细信息的自定义异常
                    throw new GameLaunchException(analyzedError, exitCode, logFilePath);
                }

                // 10. 清理临时 classpath 文件
                try
                {
                    if (!string.IsNullOrEmpty(classpathFile) && File.Exists(classpathFile))
                        File.Delete(classpathFile);
                }
                catch { }
            }
            catch (Exception ex)
            {
                // 不再写文件，直接抛出包装异常，包含原始信息
                Console.WriteLine($"[启动错误] {ex.Message}");
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    try { File.AppendAllText(logFilePath, $"[FATAL] {ex}{Environment.NewLine}"); }
                    catch { }
                }
                // 如果已经是 GameLaunchException，直接重新抛出，否则包装
                if (ex is GameLaunchException)
                    throw;
                else
                    throw new LauncherException($"启动失败: {ex.Message}", ex);
            }
        }

        // ---- 辅助方法 ----

        private string BuildArgumentString(List<string> command, bool skipFirst)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var arg in command)
            {
                if (skipFirst && first) { first = false; continue; }
                if (!first) sb.Append(' ');
                first = false;
                string a = arg;
                // 如果参数以 @ 开头（classpath 文件），不加引号
                if (!a.StartsWith("@") && a.Contains(" ") && !a.StartsWith("\"") && !a.EndsWith("\""))
                    a = $"\"{a}\"";
                sb.Append(a);
            }
            return sb.ToString();
        }

        private void WriteDebugBat(List<string> command, string workingDir)
        {
            try
            {
                string launcherPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string batDir = Path.Combine(launcherPath, "Launcher Setting");
                Directory.CreateDirectory(batDir);
                string batPath = Path.Combine(batDir, "LastLauncher.bat");

                var batContent = new StringBuilder();
                batContent.AppendLine("@echo off");
                batContent.AppendLine($"cd /d \"{workingDir}\"");
                batContent.AppendLine($"\"{command[0]}\" -version");
                var cmdLine = new StringBuilder();
                cmdLine.Append($"\"{command[0]}\"");
                for (int i = 1; i < command.Count; i++)
                {
                    string arg = command[i];
                    if (arg.Contains(" ") && !arg.StartsWith("\"") && !arg.EndsWith("\""))
                        arg = $"\"{arg}\"";
                    cmdLine.Append($" {arg}");
                }
                batContent.AppendLine(cmdLine.ToString());
                File.WriteAllText(batPath, batContent.ToString(), Encoding.Default);
            }
            catch (Exception ex) { Console.WriteLine($"[调试] BAT 调试脚本生成失败: {ex.Message}"); }
        }

        /// <summary>
        /// 判断当前操作系统是否满足 JVM 参数条目的规则。
        /// </summary>
        /// <param name="arg">参数条目（可能是字符串或字典）</param>
        /// <param name="osName">当前操作系统名称（小写，如 "windows", "macos", "linux"）</param>
        /// <returns>true 表示应该包含该参数，false 表示跳过</returns>
        protected static bool ShouldIncludeJvmArg(object arg, string osName)
        {
            if (arg is string)
                return true; // 无规则的字符串参数直接保留

            if (arg is Dictionary<string, object> dict)
            {
                // 如果没有 rules，默认保留
                if (!dict.ContainsKey("rules"))
                    return true;

                var rules = dict["rules"] as ArrayList;
                if (rules == null)
                    return true;

                bool allowed = false;
                foreach (var ruleObj in rules)
                {
                    var rule = ruleObj as Dictionary<string, object>;
                    if (rule == null) continue;

                    string action = rule.ContainsKey("action") ? rule["action"].ToString() : "allow";
                    bool matchesOs = true;

                    if (rule.ContainsKey("os"))
                    {
                        var osRule = rule["os"] as Dictionary<string, object>;
                        if (osRule != null && osRule.ContainsKey("name"))
                        {
                            string ruleOs = osRule["name"].ToString().ToLowerInvariant();
                            // 匹配规则（macos 可能写作 "osx" 或 "mac"）
                            bool isMatch = false;
                            if (ruleOs == "windows" && osName == "windows")
                                isMatch = true;
                            else if ((ruleOs == "mac" || ruleOs == "osx") && (osName == "macos" || osName == "osx"))
                                isMatch = true;
                            else if (ruleOs == "linux" && osName == "linux")
                                isMatch = true;
                            // 若还有 "arch" 等其他条件，按需扩展
                            matchesOs = isMatch;
                        }
                    }

                    // 若匹配操作系统，则根据 action 决定是否允许
                    if (matchesOs)
                    {
                        if (action == "allow")
                            allowed = true;
                        else if (action == "disallow")
                            allowed = false;
                    }
                }
                return allowed;
            }
            return true;
        }

        // ---- 游戏参数通用辅助方法（供子类新版分支使用） ----

        /// <summary>
        /// 替换 Minecraft 游戏参数中的所有占位符
        /// </summary>
        /// <summary>
        /// 替换 Minecraft 游戏参数中的所有占位符
        /// </summary>
        protected string ReplaceGamePlaceholders(string input, LaunchContext context, string assetIndexId)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input
                .Replace("${auth_player_name}", context.Username)
                .Replace("${version_name}", context.VersionId)
                .Replace("${game_directory}", context.IsVersionIsolated ? context.VersionDataPath : context.MinecraftDir)
                .Replace("${assets_root}", Path.Combine(context.MinecraftDir, "assets"))
                .Replace("${assets_index_name}", assetIndexId)
                .Replace("${auth_uuid}", context.Uuid)
                .Replace("${auth_access_token}", context.AccessToken)
                .Replace("${user_type}", context.UserType)
                .Replace("${auth_session}", context.AccessToken)
                .Replace("${user_properties}", "{}")
                .Replace("${launcher_name}", "minecraft-launcher")
                .Replace("${launcher_version}", "1.0.0")
                .Replace("${clientid}", "0")
                .Replace("${auth_xuid}", "")
                .Replace("${version_type}", "release")
                .Replace("${resolution_width}", context.Width.ToString())
                .Replace("${resolution_height}", context.Height.ToString())
                .Replace("${quickPlayPath}", "")
                .Replace("${quickPlaySingleplayer}", "")
                .Replace("${quickPlayMultiplayer}", "")
                .Replace("${quickPlayRealms}", "")
                .Replace("${natives_directory}", context.IsVersionIsolated ? Path.Combine(context.VersionDataPath, "natives") : Path.Combine(context.MinecraftDir, "natives"))
                .Replace("${library_directory}", Path.Combine(context.MinecraftDir, "libraries"))
                .Replace("${classpath}", "");
        }

        /// <summary>
        /// 从 arguments.game 列表中解析参数并添加到 cmd（新版专用）
        /// </summary>
        /// <summary>
        /// 从 arguments.game 列表中解析参数并添加到 cmd（新版专用）
        /// </summary>
        protected void AddGameArgsFromList(List<string> cmd, ArrayList gameList, LaunchContext context, string assetIndexId)
        {
            foreach (var item in gameList)
            {
                if (item is string s)
                {
                    if (s.Trim() == "--demo")
                        return;
                    string replaced = ReplaceGamePlaceholders(s, context, assetIndexId);
                    cmd.Add(replaced);
                    continue;
                }
                else if (item is Dictionary<string, object> dict && dict.ContainsKey("value"))
                {
                    object val = dict["value"];
                    if (val is string vs)
                    {
                        if (vs.Trim() == "--demo")
                            return;
                        string replaced = ReplaceGamePlaceholders(vs, context, assetIndexId);
                        cmd.Add(replaced);
                    }
                    else if (val is ArrayList list)
                    {
                        foreach (var sub in list)
                            if (sub is string subStr)
                            {
                                if (subStr.Trim() == "--demo")
                                    return;
                                string replaced = ReplaceGamePlaceholders(subStr, context, assetIndexId);
                                cmd.Add(replaced);
                            }
                    }
                }
            }
        }

        /// <summary>
        /// 兜底手动添加基础参数（当 arguments.game 不存在时）
        /// </summary>
        protected void AddFallbackGameArgs(List<string> cmd, LaunchContext context, string assetIndexId)
        {
            cmd.Add("--username");
            cmd.Add(context.Username);
            cmd.Add("--version");
            cmd.Add(context.VersionId);
            cmd.Add("--gameDir");
            cmd.Add(context.IsVersionIsolated ? context.VersionDataPath : context.MinecraftDir);
            cmd.Add("--assetsDir");
            cmd.Add(Path.Combine(context.MinecraftDir, "assets"));
            cmd.Add("--assetIndex");
            cmd.Add(assetIndexId);
            cmd.Add("--uuid");
            cmd.Add(context.Uuid);
            cmd.Add("--accessToken");
            cmd.Add(context.AccessToken);
            cmd.Add("--userType");
            cmd.Add(context.UserType);
            cmd.Add("--width");
            cmd.Add(context.Width.ToString());
            cmd.Add("--height");
            cmd.Add(context.Height.ToString());
        }

        /// <summary>
        /// 游戏进程启动后异常退出时抛出的异常（包含分析结果和日志路径）
        /// </summary>
        public class GameLaunchException : Exception
        {
            public int ExitCode { get; }
            public string LogFilePath { get; }
            public string AnalyzedMessage { get; }

            public GameLaunchException(string message, int exitCode, string logFilePath, Exception innerException = null)
                : base(message, innerException)
            {
                ExitCode = exitCode;
                LogFilePath = logFilePath;
                AnalyzedMessage = message;
            }
        }


        /// <summary>加载版本 JSON</summary>
        protected Dictionary<string, object> LoadVersionJson(string versionId, string minecraftDir)
        {
            string jsonPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), versionId), versionId + ".json");
            if (!File.Exists(jsonPath))
                throw new Exception($"版本 JSON 不存在: {jsonPath}");
            string json = File.ReadAllText(jsonPath);
            return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
        }

        /// <summary>获取经过规则过滤的 libraries 列表（含继承和 patches）</summary>
        protected List<Dictionary<string, object>> GetFilteredLibraries(
                Dictionary<string, object> root, string minecraftDir, string os)
        {
            return MinecraftCore.CollectLibrariesRecursivelyInternal(root, minecraftDir, os, new HashSet<string>());
        }

        protected string PrepareNatives(LaunchContext context, List<Dictionary<string, object>> libraries)
        {
            string nativesDir = context.IsVersionIsolated
                ? Path.Combine(context.VersionDataPath, "natives")
                : Path.Combine(context.MinecraftDir, "natives");

            // 每次启动都清理并重建 natives 目录
            if (Directory.Exists(nativesDir))
                Directory.Delete(nativesDir, true);
            Directory.CreateDirectory(nativesDir);

            // 调用 ExtractNatives（它已经按照 libraries 中的 classifiers 精确解压）
            MinecraftCore.ExtractNatives(libraries, context.MinecraftDir, nativesDir);

            return nativesDir;
        }

        /// <summary>构建 classpath 字符串（可额外添加自定义 jar）</summary>
        protected string BuildClasspath(LaunchContext context, List<Dictionary<string, object>> libraries,
                                        IEnumerable<string> extraJars = null)
        {
            string cp = MinecraftCore.BuildClasspath(libraries, context.MinecraftDir, context.VersionId);
            if (extraJars != null)
            {
                foreach (var jar in extraJars)
                    if (File.Exists(jar))
                        cp = jar + ";" + cp;
            }
            return cp;
        }

        /// <summary>获取 assetIndex ID</summary>
        protected string GetAssetIndexId(Dictionary<string, object> root, string minecraftDir)
        {
            if (root.TryGetValue("assetIndex", out object assetIdxObj))
            {
                var assetIdx = assetIdxObj as Dictionary<string, object>;
                if (assetIdx != null && assetIdx.TryGetValue("id", out object idObj))
                    return idObj.ToString();
            }
            else if (root.TryGetValue("inheritsFrom", out object parentObj))
            {
                string parent = parentObj.ToString();
                string parentJson = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), parent), parent + ".json");
                if (File.Exists(parentJson))
                {
                    var parentRoot = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(parentJson));
                    if (parentRoot.TryGetValue("assetIndex", out object pAssetIdxObj))
                    {
                        var pAssetIdx = pAssetIdxObj as Dictionary<string, object>;
                        if (pAssetIdx != null && pAssetIdx.TryGetValue("id", out object pIdObj))
                            return pIdObj.ToString();
                    }
                }
            }
            return "legacy";
        }

        protected void AddJvmArgs(List<string> cmd, Dictionary<string, object> root, string os, string libraryDir, string nativesDir, LaunchContext context)
        {
            // 获取 Java 主版本号
            int javaMajor = 0;
            if (!string.IsNullOrEmpty(context.JavaPath) && File.Exists(context.JavaPath))
                javaMajor = GetJavaMajorVersion(context.JavaPath);
            else if (cmd.Count > 0)
                javaMajor = GetJavaMajorVersion(cmd[0]);
            Console.WriteLine($"[JVM] 检测到 Java 主版本号: {javaMajor}");

            // ---- 硬编码参数 ----
            cmd.Add($"-Djna.tmpdir=\"{nativesDir}\"");
            cmd.Add($"-Dorg.lwjgl.system.SharedLibraryExtractPath=\"{nativesDir}\"");
            cmd.Add($"-Dio.netty.native.workdir=\"{nativesDir}\"");
            cmd.Add("-Dminecraft.launcher.brand=Jerry_Studio_Minecraft_Launcher");
            cmd.Add("-Dminecraft.launcher.version=1.0.0");

            if (javaMajor > 0 && javaMajor <= 8)
                cmd.Add("-Xverify:none");
            else
                Console.WriteLine($"[JVM] Java 版本 {javaMajor}，跳过 -Xverify:none。");

            cmd.Add("-XX:+UseG1GC");
            cmd.Add("-XX:-UseAdaptiveSizePolicy");
            cmd.Add("-XX:+DisableExplicitGC");

            // ---- 从 JSON 读取 arguments.jvm ----
            if (root.ContainsKey("arguments"))
            {
                var argsObj = root["arguments"] as Dictionary<string, object>;
                if (argsObj != null && argsObj.ContainsKey("jvm"))
                {
                    var jvmList = argsObj["jvm"] as ArrayList;
                    if (jvmList != null)
                    {
                        for (int i = 0; i < jvmList.Count; i++)
                        {
                            var item = jvmList[i];
                            if (!ShouldIncludeJvmArg(item, os))
                                continue;

                            if (item is string arg)
                            {
                                // 跳过 -cp 及其值（避免重复）
                                if (arg == "-cp" || arg == "-classpath" || arg == "--class-path")
                                {
                                    if (i + 1 < jvmList.Count && jvmList[i + 1] is string nextArg && !nextArg.StartsWith("-"))
                                        i++; // 跳过值
                                    continue;
                                }

                                arg = arg.Replace("${library_directory}", libraryDir)
                                         .Replace("${natives_directory}", nativesDir)
                                         .Replace("${launcher_name}", "Jerry_Studio_Minecraft_Launcher")
                                         .Replace("${launcher_version}", "1.0.0")
                                         .Replace("${classpath_separator}", ";");
                                cmd.Add(arg);
                            }
                            else if (item is Dictionary<string, object> dictItem)
                            {
                                if (dictItem.TryGetValue("value", out object val))
                                {
                                    if (val is string s)
                                    {
                                        if (s == "-cp" || s == "-classpath" || s == "--class-path")
                                            continue;
                                        s = s.Replace("${library_directory}", libraryDir)
                                             .Replace("${natives_directory}", nativesDir)
                                             .Replace("${launcher_name}", "Jerry_Studio_Minecraft_Launcher")
                                             .Replace("${launcher_version}", "1.0.0")
                                             .Replace("${classpath_separator}", ";");
                                        cmd.Add(s);
                                    }
                                    else if (val is ArrayList list)
                                    {
                                        foreach (var sub in list)
                                        {
                                            if (sub is string subStr)
                                            {
                                                if (subStr == "-cp" || subStr == "-classpath" || subStr == "--class-path")
                                                    continue;
                                                subStr = subStr.Replace("${library_directory}", libraryDir)
                                                               .Replace("${natives_directory}", nativesDir)
                                                               .Replace("${launcher_name}", "Jerry_Studio_Minecraft_Launcher")
                                                               .Replace("${launcher_version}", "1.0.0")
                                                               .Replace("${classpath_separator}", ";");
                                                cmd.Add(subStr);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 获取主类
            string mainClass = root.ContainsKey("mainClass") ? root["mainClass"].ToString() : "";

            // ---- 模块开放参数（仅当 Java 9+ 且游戏版本 ≥1.19） ----
            if (javaMajor >= 9)
            {
                string gameVersion = root.ContainsKey("inheritsFrom") ? root["inheritsFrom"].ToString() :
                                     (root.ContainsKey("id") ? root["id"].ToString() : null);
                bool needsOpenArgs = false;
                if (!string.IsNullOrEmpty(gameVersion))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(gameVersion, @"^(\d+)\.(\d+)");
                    if (match.Success)
                    {
                        int major = int.Parse(match.Groups[1].Value);
                        int minor = int.Parse(match.Groups[2].Value);
                        if (major > 1 || (major == 1 && minor >= 19))
                            needsOpenArgs = true;
                    }
                }

                if (needsOpenArgs && !cmd.Exists(arg => arg.Contains("--add-opens")))
                {
                    // 添加 java.base 的开放参数（必需）
                    string[] javaBasePkgs = {
                "java.base/java.lang",
                "java.base/java.lang.invoke",
                "java.base/java.lang.reflect",
                "java.base/java.util",
                "java.base/java.util.concurrent",
                "java.base/java.util.concurrent.atomic",
                "java.base/java.util.concurrent.locks",
                "java.base/java.util.function",
                "java.base/java.util.stream",
                "java.base/java.util.regex",
                "java.base/java.nio",
                "java.base/java.nio.charset",
                "java.base/sun.security.util"
            };
                    foreach (var pkg in javaBasePkgs)
                    {
                        cmd.Add("--add-opens");
                        cmd.Add(pkg + "=ALL-UNNAMED");
                    }
                    cmd.Add("--add-exports");
                    cmd.Add("jdk.naming.dns/com.sun.jndi.dns=ALL-UNNAMED");

                    // ★★★ 关键修改：仅对 NeoForge 添加 minecraft 模块开放参数 ★★★
                    bool isNeoForge = false;
                    if (root.ContainsKey("libraries"))
                    {
                        var libs = root["libraries"] as ArrayList;
                        if (libs != null)
                        {
                            foreach (var libObj in libs)
                            {
                                var lib = libObj as Dictionary<string, object>;
                                if (lib != null && lib.ContainsKey("name"))
                                {
                                    string name = lib["name"].ToString();
                                    if (name.StartsWith("net.neoforged:neoforge:"))
                                    {
                                        isNeoForge = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (isNeoForge)  // 只有 NeoForge 才添加
                    {
                        string target = "ALL-UNNAMED,net.minecraftforge.forge,net.neoforged.neoforge";
                        string[] minecraftPkgs = {
                    "minecraft/net.minecraft.core",
                    "minecraft/net.minecraft.world.level",
                    "minecraft/net.minecraft.world.level.block",
                    "minecraft/net.minecraft.world.level.block.entity",
                    "minecraft/net.minecraft.world.level.chunk",
                    "minecraft/net.minecraft.world.level.material",
                    "minecraft/net.minecraft.world.entity",
                    "minecraft/net.minecraft.server",
                    "minecraft/net.minecraft.server.level",
                    "minecraft/net.minecraft.network",
                    "minecraft/net.minecraft.client",
                    "minecraft/net.minecraft.client.gui",
                    "minecraft/net.minecraft.client.renderer",
                    "minecraft/net.minecraft.resources",
                    "minecraft/net.minecraft.commands",
                    "minecraft/net.minecraft.util",
                    "minecraft/net.minecraft.core.registries"
                };
                        foreach (var pkg in minecraftPkgs)
                        {
                            cmd.Add("--add-opens");
                            cmd.Add(pkg + "=" + target);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加游戏参数（支持 minecraftArguments 和 arguments.game）
        /// </summary>
        protected void AddGameArgs(List<string> cmd, LaunchContext context, Dictionary<string, object> root, string assetIndexId)
        {
            try
            {
                // ★ 调试：打印 cmd 中 -cp 的值
                int cpIdx = cmd.IndexOf("-cp");
                if (cpIdx >= 0 && cpIdx + 1 < cmd.Count)
                {
                    Console.WriteLine($"[AddGameArgs] 进入时 -cp 后面的值: {cmd[cpIdx + 1]}");
                }

                if (root == null)
                {
                    AddFallbackGameArgs(cmd, context, assetIndexId);
                    return;
                }

                // 尝试获取 arguments.game 列表
                ArrayList gameList = null;
                if (root.TryGetValue("arguments", out object argsObjRaw))
                {
                    var argsObj = argsObjRaw as Dictionary<string, object>;
                    if (argsObj != null && argsObj.TryGetValue("game", out object gameObj))
                    {
                        gameList = gameObj as ArrayList;
                    }
                }

                // 如果 gameList 为空，尝试兼容旧版 minecraftArguments
                if (gameList == null && root.TryGetValue("minecraftArguments", out object mcArgs))
                {
                    string argsTemplate = mcArgs as string;
                    if (!string.IsNullOrEmpty(argsTemplate))
                    {
                        argsTemplate = ReplaceGamePlaceholders(argsTemplate, context, assetIndexId);
                        if (!argsTemplate.Contains("--width"))
                            argsTemplate += $" --width {context.Width} --height {context.Height}";
                        cmd.AddRange(SplitCommandLine(argsTemplate));
                        // 补全可能缺失的 --version, --assetsDir, --assetIndex
                        if (!cmd.Contains("--version"))
                        {
                            cmd.Add("--version");
                            cmd.Add(context.VersionId);
                        }
                        if (!cmd.Contains("--assetsDir"))
                        {
                            cmd.Add("--assetsDir");
                            cmd.Add(Path.Combine(context.MinecraftDir, "assets"));
                        }
                        if (!cmd.Contains("--assetIndex"))
                        {
                            cmd.Add("--assetIndex");
                            cmd.Add(assetIndexId);
                        }
                        return;
                    }
                }

                if (gameList != null)
                {
                    foreach (var item in gameList)
                    {
                        if (item is string s)
                        {
                            string replaced = ReplaceGamePlaceholders(s, context, assetIndexId);
                            cmd.Add(replaced);
                        }
                        else if (item is Dictionary<string, object> dict)
                        {
                            // 跳过带 rules 的条件参数（如快速启动）
                            if (dict.ContainsKey("rules"))
                                continue;

                            if (dict.TryGetValue("value", out object val))
                            {
                                if (val is string vs)
                                {
                                    string replaced = ReplaceGamePlaceholders(vs, context, assetIndexId);
                                    cmd.Add(replaced);
                                }
                                else if (val is ArrayList list)
                                {
                                    foreach (var sub in list)
                                        if (sub is string subStr)
                                        {
                                            string replaced = ReplaceGamePlaceholders(subStr, context, assetIndexId);
                                            cmd.Add(replaced);
                                        }
                                }
                            }
                        }
                    }

                    // 补全可能缺失的必备参数
                    if (!cmd.Contains("--version"))
                    {
                        cmd.Add("--version");
                        cmd.Add(context.VersionId);
                    }
                    if (!cmd.Contains("--assetsDir"))
                    {
                        cmd.Add("--assetsDir");
                        cmd.Add(Path.Combine(context.MinecraftDir, "assets"));
                    }
                    if (!cmd.Contains("--assetIndex"))
                    {
                        cmd.Add("--assetIndex");
                        cmd.Add(assetIndexId);
                    }
                }
                else
                {
                    // 兜底
                    AddFallbackGameArgs(cmd, context, assetIndexId);
                }

                // ★ 调试：打印 cmd 中 -cp 的值
                cpIdx = cmd.IndexOf("-cp");
                if (cpIdx >= 0 && cpIdx + 1 < cmd.Count)
                {
                    Console.WriteLine($"[AddGameArgs] 退出时 -cp 后面的值: {cmd[cpIdx + 1]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddGameArgs] 异常: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>拆分命令行（辅助）</summary>
        public string[] SplitCommandLine(string commandLine)
        {
            var args = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();
            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Length = 0;
                    }
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0)
                args.Add(current.ToString());
            return args.ToArray();
        }

        /// <summary>
        /// 从版本 JSON 中读取主类（必须存在，否则抛出异常）
        /// </summary>
        protected string GetMainClass(Dictionary<string, object> root)
        {
            if (root.ContainsKey("mainClass"))
                return root["mainClass"].ToString();
            else
                throw new Exception("版本 JSON 中缺少 mainClass 字段，该版本不合法。");
        }

        /// <summary>
        /// 获取指定 Java 可执行文件的主版本号
        /// </summary>
        private int GetJavaMajorVersion(string javaPath)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(javaPath, "-version");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    // 匹配如 "version \"21.0.7\"" 或 "version \"1.8.0_51\""
                    var match = System.Text.RegularExpressions.Regex.Match(output, @"version ""(\d+)\.\d+\.\d+");
                    if (match.Success)
                    {
                        int major = int.Parse(match.Groups[1].Value);
                        if (major == 1) // Java 8 及以前，格式为 "1.8.0"
                        {
                            var m2 = System.Text.RegularExpressions.Regex.Match(output, @"version ""1\.(\d+)");
                            if (m2.Success)
                                return int.Parse(m2.Groups[1].Value);
                        }
                        return major;
                    }
                }
            }
            catch { }
            return 0; // 无法检测，视为旧版
        }

        public class LauncherException : Exception
        {
            public LauncherException(string msg) : base(msg) { }
            public LauncherException(string msg, Exception inner) : base(msg, inner) { }
        }
    }
}