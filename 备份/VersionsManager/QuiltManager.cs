using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace New_Launcher
{
    public static class QuiltManager
    {
        private const string QUILT_META_BASE = "https://meta.quiltmc.org/v3";
        private const string QUILT_MAVEN = "https://maven.quiltmc.org/repository/release/";

        // ==================== Installer（下载 Quilt 加载器） ====================
        public static class Installer
        {
            // ---------- 版本限制 ----------
            private static readonly Version MinGameVersion = new Version(1, 14, 0);
            private static readonly Version MaxGameVersion = new Version(999, 999, 999);

            public static bool IsGameVersionSupported(string gameVersion, out string errorMessage)
            {
                errorMessage = null;
                string clean = SanitizeVersion(gameVersion);
                Version ver;
                if (!TryParseVersion(clean, out ver))
                {
                    errorMessage = $"无法解析游戏版本 '{gameVersion}'";
                    return false;
                }
                if (ver < MinGameVersion)
                {
                    errorMessage = $"游戏版本 {clean} 过低，最低支持 {MinGameVersion}";
                    return false;
                }
                if (ver > MaxGameVersion)
                {
                    errorMessage = $"游戏版本 {clean} 过高，最高支持 {MaxGameVersion}";
                    return false;
                }
                return true;
            }

            private static bool TryParseVersion(string input, out Version version)
            {
                version = null;
                if (string.IsNullOrEmpty(input)) return false;
                string[] parts = input.Split('.');
                if (parts.Length < 2 || parts.Length > 4) return false;
                int[] nums = new int[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    if (!int.TryParse(parts[i], out nums[i])) return false;
                if (nums.Length == 2)
                    version = new Version(nums[0], nums[1]);
                else if (nums.Length == 3)
                    version = new Version(nums[0], nums[1], nums[2]);
                else if (nums.Length == 4)
                    version = new Version(nums[0], nums[1], nums[2], nums[3]);
                else
                    return false;
                return true;
            }

            public static string SanitizeVersion(string version)
            {
                if (string.IsNullOrEmpty(version)) return version;
                string cleaned = Regex.Replace(version, @"\u001B\[[;\\d]*m", "");
                cleaned = Regex.Replace(cleaned, @"[\x00-\x1F]", "");
                int dashIndex = cleaned.IndexOf('-');
                if (dashIndex >= 0) cleaned = cleaned.Substring(0, dashIndex);
                return cleaned.Trim();
            }

            public static string InstallQuilt(string gameVersion, string loaderVersion, bool isLaunch = false)
            {
                if (!IsGameVersionSupported(gameVersion, out string err))
                    throw new ArgumentException(err);

                // 如果未指定加载器版本，自动获取最新版
                if (string.IsNullOrEmpty(loaderVersion) || loaderVersion == "不选择")
                    loaderVersion = GetLatestQuiltLoaderVersion(gameVersion);

                string cleanGame = SanitizeVersion(gameVersion);
                string cleanLoader = SanitizeVersion(loaderVersion);
                if (string.IsNullOrEmpty(cleanGame) || cleanGame == "不选择")
                    throw new ArgumentException("游戏版本无效");
                if (string.IsNullOrEmpty(cleanLoader) || cleanLoader == "不选择")
                    throw new ArgumentException("加载器版本无效");

                string encodedGame = Uri.EscapeUriString(cleanGame);
                string encodedLoader = Uri.EscapeUriString(cleanLoader);

                string versionId = $"quilt-loader-{encodedLoader}-{encodedGame}";

                // ★ 启动模式：先检查完整性，若完整则直接返回
                if (isLaunch)
                {
                    if (CheckQuiltIntegrity(versionId))
                    {
                        Console.WriteLine("[启动] Quilt 文件完整，跳过下载");
                        return versionId;
                    }
                    Console.WriteLine("[启动] Quilt 文件不完整，执行完整安装...");
                    // 若检查不通过，继续执行下面的下载安装流程（此时虽为启动模式，但需下载）
                }

                string metaUrl = $"{QUILT_META_BASE}/versions/loader/{encodedGame}/{encodedLoader}/profile/json";

                InstallLoader(metaUrl, QUILT_MAVEN, versionId, isLaunch, "Quilt", cleanGame);
                return versionId;
            }

            private static void InstallLoader(string metaUrl, string defaultMaven, string versionId, bool isLaunch, string loaderType, string gameVersion)
            {
                string prefix = isLaunch ? "[Launcher]" : "[Downloader]";
                Console.WriteLine($"{prefix} 获取 {loaderType} 配置...");

                // 修改点 1：使用重试版本
                string jsonText = DownloadStringWithRetry(metaUrl);
                JObject profile = JObject.Parse(jsonText);

                string versionDir = Path.Combine(Path.Combine(MinecraftCore.GetMinecraftDir(), "versions"), versionId);
                Directory.CreateDirectory(versionDir);

                string jsonPath = Path.Combine(versionDir, versionId + ".json");
                File.WriteAllText(jsonPath, jsonText);
                Console.WriteLine($"{prefix} 已保存 JSON: {jsonPath}");

                // 补全 assetIndex（如果缺失）
                try
                {
                    var profileObj = JObject.Parse(jsonText);
                    if (profileObj["assetIndex"] == null)
                    {
                        Console.WriteLine($"{prefix} 版本 JSON 缺少 assetIndex，尝试从原版 Manifest 补全...");
                        var manifest = MinecraftCore.GetVersionManifest();
                        var versions = manifest["versions"] as ArrayList;
                        Dictionary<string, object> versionEntry = null;
                        foreach (var v in versions)
                        {
                            var entry = v as Dictionary<string, object>;
                            if (entry != null && entry["id"].ToString() == gameVersion)
                            {
                                versionEntry = entry;
                                break;
                            }
                        }
                        if (versionEntry != null)
                        {
                            string versionJsonUrl = versionEntry["url"].ToString();
                            // 修改点 2：使用重试版本
                            string versionJson = DownloadStringWithRetry(versionJsonUrl);
                            var originalRoot = JObject.Parse(versionJson);
                            if (originalRoot["assetIndex"] != null)
                            {
                                profileObj["assetIndex"] = originalRoot["assetIndex"];
                                string updatedJson = profileObj.ToString(Newtonsoft.Json.Formatting.Indented);
                                File.WriteAllText(jsonPath, updatedJson);
                                Console.WriteLine($"{prefix} assetIndex 已补全，值为: {profileObj["assetIndex"]["id"]}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{prefix} 补全 assetIndex 时出错: {ex.Message}");
                }

                // 下载 client.jar
                string clientPath = Path.Combine(versionDir, versionId + ".jar");
                if (!File.Exists(clientPath))
                {
                    string clientUrl = null, clientSha1 = null;
                    var downloads = profile["downloads"] as JObject;
                    if (downloads != null && downloads["client"] != null)
                    {
                        var client = downloads["client"] as JObject;
                        clientUrl = client["url"].ToString();
                        clientSha1 = client["sha1"]?.ToString();
                        Console.WriteLine($"{prefix} 从 profile 获取 client.jar: {clientUrl}");
                    }
                    else
                    {
                        Console.WriteLine($"{prefix} profile 中没有 downloads.client，尝试从原版 manifest 获取...");
                        try
                        {
                            string url, sha1;
                            MinecraftCore.GetClientInfo(gameVersion, out url, out sha1);
                            clientUrl = url;
                            clientSha1 = sha1;
                            Console.WriteLine($"{prefix} 从原版 manifest 获取 client.jar: {clientUrl}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{prefix} 从原版 manifest 获取 client 失败: {ex.Message}");
                        }
                    }
                    if (!string.IsNullOrEmpty(clientUrl))
                    {
                        Console.WriteLine($"{prefix} 下载 client.jar: {clientUrl}");
                        // 修改点 3：使用重试版本
                        DownloadFileWithRetry(clientUrl, clientPath, clientSha1);
                    }
                }

                // 下载加载器库
                JArray libraries = profile["libraries"] as JArray;
                if (libraries != null)
                {
                    int total = libraries.Count;
                    int downloadedCount = 0;
                    Console.WriteLine($"{prefix} 需下载加载器库 {total} 个");
                    foreach (JToken lib in libraries)
                    {
                        ResolveLibrary(lib, defaultMaven, out string localPath, out string downloadUrl);
                        if (!File.Exists(localPath))
                        {
                            downloadedCount++;
                            Console.WriteLine($"{prefix} 下载 [{downloadedCount}/{total}]: {downloadUrl}");
                            try
                            {
                                // 修改点 4：使用重试版本
                                DownloadFileWithRetry(downloadUrl, localPath, null);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"{prefix} 库下载失败: {ex.Message}");
                            }
                        }
                    }
                }

                try
                {
                    if (File.Exists(jsonPath))
                    {
                        var root = JObject.Parse(File.ReadAllText(jsonPath));
                        if (root["inheritsFrom"] != null)
                        {
                            Console.WriteLine($"{prefix} 检测到继承，正在合并为独立 JSON...");
                            VersionJsonMerger.MergeAndSaveIndependentJson(jsonPath, MinecraftCore.GetMinecraftDir());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{prefix} 合并 JSON 时出错: {ex.Message}");
                }

                Console.WriteLine($"{prefix} {loaderType} 安装完成");
            }

            private static void ResolveLibrary(JToken lib, string defaultMaven, out string localPath, out string downloadUrl)
            {
                string name = lib["name"].ToString();
                string url = lib["url"]?.ToString();
                string[] parts = name.Split(':');
                string group = parts[0].Replace('.', '/');
                string artifact = parts[1];
                string version = parts[2];
                string fileName = $"{artifact}-{version}.jar";
                string relativePath = $"{group}/{artifact}/{version}/{fileName}";

                string minecraftDir = MinecraftCore.GetMinecraftDir();
                localPath = Path.Combine(Path.Combine(minecraftDir, "libraries"), relativePath);
                downloadUrl = (url ?? defaultMaven) + relativePath;
            }

            private static string GetLatestQuiltLoaderVersion(string gameVersion)
            {
                string url = $"{QUILT_META_BASE}/versions/loader/{gameVersion}";
                // 修改点 5：使用重试版本
                string json = DownloadStringWithRetry(url);
                JArray array = JArray.Parse(json);
                if (array.Count == 0)
                    throw new Exception($"未找到适用于 {gameVersion} 的 Quilt 加载器");
                var latest = array[0] as JObject;
                if (latest == null) throw new Exception("解析失败");
                string version = latest["version"]?.ToString();
                if (string.IsNullOrEmpty(version))
                    throw new Exception("无法获取加载器版本");
                return version;
            }

            // 额外提供获取版本列表的方法（供 UI 调用）
            public static string[] GetQuiltLoaderVersions(string gameVersion)
            {
                if (string.IsNullOrEmpty(gameVersion) || gameVersion.Contains("加载") || gameVersion.Contains("失败"))
                    return new string[] { "不选择" };

                string cleanGame = SanitizeVersion(gameVersion);
                if (!Regex.IsMatch(cleanGame, @"^\d+(\.\d+){1,2}$"))
                    return new string[] { "不选择" };

                string encodedGame = Uri.EscapeUriString(cleanGame);
                string url = $"{QUILT_META_BASE}/versions/loader/{encodedGame}";
                try
                {
                    // 修改点 6：使用重试版本
                    string json = DownloadStringWithRetry(url);
                    JArray arr = JArray.Parse(json);
                    List<string> versions = new List<string> { "不选择" };
                    foreach (JToken item in arr)
                        versions.Add(item["loader"]["version"].ToString());

                    // 排序（去掉 "不选择"，排序后再加回去）
                    if (versions.Count > 1)
                    {
                        List<string> toSort = versions.GetRange(1, versions.Count - 1);
                        toSort.Sort(new VersionComparer());
                        toSort.Reverse();
                        versions = new List<string> { "不选择" };
                        versions.AddRange(toSort);
                    }
                    return versions.ToArray();
                }
                catch
                {
                    return new string[] { "不选择" };
                }
            }

            // ---- 新增：带重试的下载辅助方法 ----
            private static void DownloadFileWithRetry(string url, string dest, string sha1)
            {
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    try
                    {
                        MinecraftCore.DownloadFile(url, dest, sha1);
                        return;
                    }
                    catch
                    {
                        if (attempt == 2) throw;
                    }
                }
            }

            private static string DownloadStringWithRetry(string url)
            {
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    try
                    {
                        return MinecraftCore.DownloadString(url);
                    }
                    catch
                    {
                        if (attempt == 2) throw;
                    }
                }
                return null;
            }

            // ---- 自定义版本比较器 ----
            private class VersionComparer : IComparer<string>
            {
                public int Compare(string x, string y)
                {
                    Version vx = ParseVersionPart(x);
                    Version vy = ParseVersionPart(y);
                    int cmp = vx.CompareTo(vy);
                    if (cmp != 0) return cmp;
                    return ComparePreRelease(x, y);
                }

                private Version ParseVersionPart(string version)
                {
                    var match = Regex.Match(version, @"^(\d+(\.\d+){1,2})");
                    if (match.Success)
                    {
                        try { return new Version(match.Value); }
                        catch { }
                    }
                    return new Version(0, 0);
                }

                private int ComparePreRelease(string x, string y)
                {
                    string preX = GetPreRelease(x);
                    string preY = GetPreRelease(y);
                    if (string.IsNullOrEmpty(preX) && !string.IsNullOrEmpty(preY)) return 1;
                    if (!string.IsNullOrEmpty(preX) && string.IsNullOrEmpty(preY)) return -1;
                    return string.Compare(preX, preY, StringComparison.Ordinal);
                }

                private string GetPreRelease(string version)
                {
                    int idx = version.IndexOf('-');
                    if (idx >= 0) return version.Substring(idx + 1);
                    return string.Empty;
                }
            }

            /// <summary>
            /// 检查 Quilt 版本文件是否完整（仅检查存在性，不计算 SHA1）
            /// </summary>
            public static bool CheckQuiltIntegrity(string versionId)
            {
                string minecraftDir = MinecraftCore.GetMinecraftDir();
                string versionDir = Path.Combine(Path.Combine(minecraftDir, "versions"), versionId);
                string jsonPath = Path.Combine(versionDir, versionId + ".json");
                string jarPath = Path.Combine(versionDir, versionId + ".jar");

                // 检查核心文件
                if (!File.Exists(jsonPath) || !File.Exists(jarPath))
                    return false;

                // 读取 version.json，检查所有 libraries
                var root = JObject.Parse(File.ReadAllText(jsonPath));
                var libraries = root["libraries"] as JArray;
                if (libraries != null)
                {
                    foreach (var lib in libraries)
                    {
                        string name = lib["name"]?.ToString();
                        if (string.IsNullOrEmpty(name)) continue;

                        // 解析库路径（与 ResolveLibrary 类似）
                        string localPath = ResolveLibraryPath(name, minecraftDir);
                        if (!File.Exists(localPath) || new FileInfo(localPath).Length == 0)
                            return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// 根据库名解析本地路径（与 Installer 中的 ResolveLibrary 逻辑一致）
            /// </summary>
            private static string ResolveLibraryPath(string name, string minecraftDir)
            {
                var parts = name.Split(':');
                if (parts.Length < 3) return null;
                string group = parts[0].Replace('.', '/');
                string artifact = parts[1];
                string version = parts[2];
                string classifier = parts.Length > 3 ? parts[3] : null;
                string fileName = $"{artifact}-{version}";
                if (!string.IsNullOrEmpty(classifier)) fileName += $"-{classifier}";
                fileName += ".jar";
                string relativePath = Path.Combine(Path.Combine(Path.Combine(group, artifact), version), fileName);
                return Path.Combine(Path.Combine(minecraftDir, "libraries"), relativePath);
            }
        }

        // ==================== Launcher（实现 ILauncher，用于启动 Quilt） ====================
        public class Launcher : BaseLauncher
        {
            protected override List<string> BuildCommand(LaunchContext context)
            {
                var cmd = new List<string>();
                cmd.Add(JavaResolver.GetJavaPath(context, context.MinecraftDir, context.VersionId));

                var root = LoadVersionJson(context.VersionId, context.MinecraftDir);
                string os = MinecraftCore.GetOsName();

                var libs = GetFilteredLibraries(root, context.MinecraftDir, os);
                string nativesDir = PrepareNatives(context, libs);
                string classpath = BuildClasspath(context, libs);

                // JVM 参数
                cmd.Add($"-Xms{context.InitMemory}");
                cmd.Add($"-Xmx{context.MaxMemory}");
                cmd.Add($"-Djava.library.path=\"{nativesDir}\"");

                // Quilt 特定优化（减少日志输出，避免不必要的映射加载）
                cmd.Add("-Dquilt.loader.debug=false");
                // 如果启动慢，可以尝试禁用延迟类加载（可能改善启动时间）
                // cmd.Add("-Dquilt.loader.disable_lazy_loading=true");

                AddJvmArgs(cmd, root, os, Path.Combine(context.MinecraftDir, "libraries"), nativesDir, context);

                cmd.Add("-cp");
                cmd.Add(classpath);

                // 主类统一从 JSON 读取
                cmd.Add(GetMainClass(root));

                // 游戏参数
                string assetIndexId = GetAssetIndexId(root, context.MinecraftDir);
                AddGameArgs(cmd, context, root, assetIndexId);

                return cmd;
            }
        }
    }
}