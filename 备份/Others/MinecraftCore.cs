using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using static New_Launcher.VanillaManager;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace New_Launcher
{
    public static class MinecraftCore
    {
        private static Process _gameProcess;
        private const string MANIFEST_URL = "https://launchermeta.mojang.com/mc/game/version_manifest.json";

        static MinecraftCore()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.Expect100Continue = false;
            WebRequest.DefaultWebProxy = null;
            ServicePointManager.DefaultConnectionLimit = 20;
            ServicePointManager.MaxServicePointIdleTime = 1000;
        }

        // ========== 公共工具方法 ==========

        public static bool IsRulesAllowed(ArrayList rules, string osName)
        {
            if (rules == null || rules.Count == 0) return true;
            bool allow = false;
            foreach (var ruleObj in rules)
            {
                var rule = ruleObj as Dictionary<string, object>;
                if (rule == null) continue;
                string action = rule.ContainsKey("action") ? rule["action"].ToString() : "allow";
                bool match = true;
                if (rule.ContainsKey("os"))
                {
                    var os = rule["os"] as Dictionary<string, object>;
                    if (os != null && os.ContainsKey("name"))
                    {
                        string osNameRule = os["name"].ToString();
                        if (osNameRule != osName)
                            match = false;
                    }
                }
                if (match)
                {
                    if (action == "allow")
                        allow = true;
                    else if (action == "disallow")
                        allow = false;
                }
            }
            return allow;
        }

        public static Dictionary<string, object> GetVersionManifest()
        {
            return DeserializeJson<Dictionary<string, object>>(DownloadString(MANIFEST_URL));
        }

        public static string DownloadString(string url)
        {
            for (int i = 1; i <= 3; i++)
            {
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0");
                        wc.Proxy = null;
                        return wc.DownloadString(url);
                    }
                }
                catch
                {
                    if (i == 3) throw;
                    Thread.Sleep(1000 * i);
                }
            }
            return null;
        }

        public static void DownloadFile(string url, string dest, string expectedSha1)
        {
            for (int i = 1; i <= 3; i++)
            {
                try
                {
                    string dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    if (File.Exists(dest) && !string.IsNullOrEmpty(expectedSha1))
                    {
                        using (var fs = File.OpenRead(dest))
                        {
                            var sha1 = System.Security.Cryptography.SHA1.Create();
                            byte[] hash = sha1.ComputeHash(fs);
                            string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            if (hex == expectedSha1)
                                return;
                        }
                        File.Delete(dest);
                    }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UserAgent = "Mozilla/5.0";
                    request.Proxy = null;
                    request.Timeout = 120000;
                    request.ReadWriteTimeout = 60000;
                    request.KeepAlive = false;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (FileStream fs = File.Create(dest))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            fs.Write(buffer, 0, bytesRead);
                    }

                    if (!string.IsNullOrEmpty(expectedSha1))
                    {
                        using (var fs = File.OpenRead(dest))
                        {
                            var sha1 = System.Security.Cryptography.SHA1.Create();
                            byte[] hash = sha1.ComputeHash(fs);
                            string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            if (hex != expectedSha1)
                                throw new Exception("SHA1 校验失败，期望 " + expectedSha1 + "，实际 " + hex);
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    if (File.Exists(dest)) File.Delete(dest);
                    Console.WriteLine("[下载] 下载失败 (尝试 " + i + "/3): " + url + " - " + ex.Message);
                    if (i == 3) throw new Exception("下载失败: " + url + "\n" + ex.Message);
                    Thread.Sleep(2000 * i);
                }
            }
        }

        public static string GetOsName()
        {
            PlatformID pid = Environment.OSVersion.Platform;
            if (pid == PlatformID.Win32NT || pid == PlatformID.Win32Windows)
                return "windows";
            else
                return "linux";
        }

        public static bool IsLibraryAllowed(Dictionary<string, object> lib, string osName)
        {
            if (!lib.TryGetValue("rules", out object rulesObj))
                return true;
            var rules = rulesObj as ArrayList;
            if (rules == null) return true;

            bool? allowed = null;
            bool hasMatchedRule = false;

            foreach (var ruleObj in rules)
            {
                var rule = ruleObj as Dictionary<string, object>;
                if (rule == null) continue;

                if (!rule.TryGetValue("action", out object actionObj)) continue;
                string action = actionObj.ToString();

                bool matchesOs = true;
                if (rule.TryGetValue("os", out object osObj))
                {
                    var osDict = osObj as Dictionary<string, object>;
                    if (osDict != null && osDict.TryGetValue("name", out object osNameObj))
                    {
                        string ruleOs = osNameObj.ToString();
                        if (!string.IsNullOrEmpty(ruleOs) && !ruleOs.Equals(osName, StringComparison.OrdinalIgnoreCase))
                            matchesOs = false;
                    }
                }

                if (matchesOs)
                {
                    hasMatchedRule = true;
                    allowed = (action == "allow");
                }
            }

            if (!hasMatchedRule)
                return true; // 没有匹配任何规则，默认允许
            return allowed ?? true;
        }

        public static void GetClientInfo(string versionId, out string url, out string sha1)
        {
            url = null;
            sha1 = null;
            var manifest = GetVersionManifest();
            var versions = manifest["versions"] as ArrayList;
            Dictionary<string, object> versionEntry = null;
            foreach (var v in versions)
            {
                var entry = v as Dictionary<string, object>;
                if (entry != null && entry["id"].ToString() == versionId)
                {
                    versionEntry = entry;
                    break;
                }
            }
            if (versionEntry == null)
                throw new Exception("未找到版本 " + versionId);

            string versionJsonUrl = versionEntry["url"].ToString();
            string versionJson = DownloadString(versionJsonUrl);
            var versionRoot = DeserializeJson<Dictionary<string, object>>(versionJson);
            var downloads = versionRoot["downloads"] as Dictionary<string, object>;
            if (downloads != null && downloads.ContainsKey("client"))
            {
                var client = downloads["client"] as Dictionary<string, object>;
                url = client["url"].ToString();
                sha1 = client["sha1"].ToString();
                return;
            }
            throw new Exception("未找到 client 信息");
        }

        public static void KillGameProcess()
        {
            if (_gameProcess != null && !_gameProcess.HasExited)
            {
                try
                {
                    _gameProcess.Kill();
                    _gameProcess.WaitForExit(5000);
                }
                catch { }
                _gameProcess.Dispose();
                _gameProcess = null;
            }
        }

        public static string GetMinecraftDir()
        {
            return Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), ".minecraft");
        }

        // ========== 原生库解压 ==========

        public static void ExtractNatives(List<Dictionary<string, object>> libraries, string minecraftDir, string nativesDir)
        {
            Console.WriteLine("[ExtractNatives] 开始解压原生库");
            int extracted = 0;
            foreach (var lib in libraries)
            {
                if (!lib.ContainsKey("downloads")) continue;
                var downloads = lib["downloads"] as Dictionary<string, object>;
                if (downloads == null || !downloads.ContainsKey("classifiers")) continue;
                var classifiers = downloads["classifiers"] as Dictionary<string, object>;
                string osKey = "natives-windows";
                foreach (var kv in classifiers)
                {
                    string key = kv.Key;
                    if (!key.StartsWith("natives-")) continue;
                    if (!key.Equals(osKey, StringComparison.OrdinalIgnoreCase) && !key.Contains("windows")) continue;
                    var nativeObj = kv.Value as Dictionary<string, object>;
                    if (nativeObj == null) continue;
                    string path = nativeObj["path"].ToString();
                    string fullPath = Path.Combine(Path.Combine(minecraftDir, "libraries"), path);
                    if (!File.Exists(fullPath))
                    {
                        // Console.WriteLine("[ExtractNatives] 警告: " + fullPath + " 不存在");
                        continue;
                    }
                    Console.WriteLine("[ExtractNatives] 解压 " + Path.GetFileName(fullPath));
                    using (var fs = File.OpenRead(fullPath))
                    using (var zip = new ZipFile(fs))
                    {
                        foreach (ZipEntry entry in zip)
                        {
                            if (entry.IsDirectory) continue;
                            string name = entry.Name.Replace('/', Path.DirectorySeparatorChar);
                            if (name.StartsWith("META-INF" + Path.DirectorySeparatorChar)) continue;
                            string dest = Path.Combine(nativesDir, name);
                            string destDir = Path.GetDirectoryName(dest);
                            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                            using (var stream = zip.GetInputStream(entry))
                            using (var outStream = File.Create(dest))
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                    outStream.Write(buffer, 0, bytesRead);
                            }
                            extracted++;
                        }
                    }
                }
            }
            Console.WriteLine("[ExtractNatives] 解压完成，共 " + extracted + " 个文件");
        }

        // ========== 工厂方法 ==========

        public static ILauncher GetLauncher(string versionId)
        {
            // 远古版：rd-*, inf-*, 以及可能的 a-* 等
            if (versionId.StartsWith("rd-") || versionId.StartsWith("inf-") || versionId.StartsWith("a-"))
                return new AncientLauncher();

            string loaderType = GetLoaderType(versionId);
            switch (loaderType)
            {
                case "Vanilla": return new VanillaManager.Launcher();
                case "Forge": return new ForgeManager.Launcher();
                case "NeoForge": return new NeoForgeManager.Launcher();
                case "Fabric": return new FabricManager.Launcher();
                case "Quilt": return new QuiltManager.Launcher();
                default: throw new Exception("未知加载器类型: " + loaderType);
            }
        }

        public static string GetLoaderType(string versionId)
        {
            string minecraftDir = GetMinecraftDir();
            string versionJsonPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), versionId), versionId + ".json");
            if (!File.Exists(versionJsonPath))
                return "Vanilla";

            var root = DeserializeJson<Dictionary<string, object>>(File.ReadAllText(versionJsonPath));
            string os = GetOsName();

            // ---- 1. 优先检查 arguments.game 是否包含 --fml.neoForgeVersion ----
            if (root.ContainsKey("arguments"))
            {
                var argsObj = root["arguments"] as Dictionary<string, object>;
                if (argsObj != null && argsObj.ContainsKey("game"))
                {
                    var gameList = argsObj["game"] as ArrayList;
                    if (gameList != null && gameList.Contains("--fml.neoForgeVersion"))
                        return "NeoForge";
                }
            }

            // ---- 2. 递归收集所有库（包括继承和 patches） ----
            var allLibraries = CollectLibrariesRecursivelyInternal(root, minecraftDir, os, new HashSet<string>());

            // ---- 3. 检查特征库（NeoForge 必须排在 Forge 前面） ----
            foreach (var lib in allLibraries)
            {
                if (lib.ContainsKey("name"))
                {
                    string name = lib["name"].ToString();
                    if (name.StartsWith("net.neoforged:neoforge:")) return "NeoForge";
                    if (name.StartsWith("net.minecraftforge:forge:")) return "Forge";
                    if (name.StartsWith("net.fabricmc:fabric-loader:")) return "Fabric";
                    if (name.StartsWith("org.quiltmc:quilt-loader:")) return "Quilt";
                }
            }

            // ---- 4. 检查 mainClass（但排除已被识别为 NeoForge 的情况，前面已做） ----
            if (root.ContainsKey("mainClass"))
            {
                string mainClass = root["mainClass"].ToString();
                // 仅当不包含 NeoForge 特征时才视为 Forge
                if (mainClass.Contains("BootstrapLauncher") ||
                    mainClass.Contains("FMLClientLaunchWrapper") ||
                    mainClass.Contains("net.minecraftforge.fml"))
                {
                    return "Forge";
                }
            }

            return "Vanilla";
        }

        // ========== 新增公共辅助方法（供所有 Launcher 使用） ==========

        /// <summary>
        /// 递归收集 libraries，包括父版本和 patches
        /// </summary>
        public static List<Dictionary<string, object>> CollectLibrariesRecursively(Dictionary<string, object> root, string minecraftDir, string os)
        {
            var result = new List<Dictionary<string, object>>();
            // 1. 添加当前版本的 libraries
            if (root.ContainsKey("libraries"))
            {
                var libs = root["libraries"] as ArrayList;
                if (libs != null)
                {
                    foreach (var lib in libs)
                    {
                        var libObj = lib as Dictionary<string, object>;
                        if (libObj != null && libObj.ContainsKey("name"))
                            result.Add(libObj);
                    }
                }
            }
            // 2. 如果有 inheritsFrom，递归添加父版本的 libraries（去重）
            if (root.ContainsKey("inheritsFrom"))
            {
                string parent = root["inheritsFrom"].ToString();
                string parentJsonPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), parent), parent + ".json");
                if (File.Exists(parentJsonPath))
                {
                    var parentRoot = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(parentJsonPath));
                    var parentLibs = CollectLibrariesRecursively(parentRoot, minecraftDir, os);
                    // 合并去重（按 name）
                    foreach (var parentLib in parentLibs)
                    {
                        if (parentLib.ContainsKey("name"))
                        {
                            bool exists = result.Any(l => l.ContainsKey("name") && l["name"].ToString() == parentLib["name"].ToString());
                            if (!exists)
                                result.Add(parentLib);
                        }
                    }
                }
            }
            return result;
        }

        public static List<Dictionary<string, object>> CollectLibrariesRecursivelyInternal(
    Dictionary<string, object> root, string minecraftDir, string osName, HashSet<string> visited)
        {
            // 使用 TryGetValue 获取 id
            string currentId = null;
            if (root.TryGetValue("id", out object idObj))
                currentId = idObj?.ToString();

            if (currentId != null && visited.Contains(currentId))
                return new List<Dictionary<string, object>>();
            if (currentId != null)
                visited.Add(currentId);

            var result = new List<Dictionary<string, object>>();

            // 父版本
            if (root.TryGetValue("inheritsFrom", out object parentObj))
            {
                string parentVersion = parentObj.ToString();
                string parentJsonPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), parentVersion), parentVersion + ".json");
                if (File.Exists(parentJsonPath))
                {
                    var parentRoot = DeserializeJson<Dictionary<string, object>>(File.ReadAllText(parentJsonPath));
                    var parentLibs = CollectLibrariesRecursivelyInternal(parentRoot, minecraftDir, osName, visited);
                    result.AddRange(parentLibs);
                }
            }

            // 当前 libraries
            if (root.TryGetValue("libraries", out object libsObj))
            {
                var libs = libsObj as ArrayList;
                if (libs != null)
                {
                    foreach (var libObj in libs)
                    {
                        var lib = libObj as Dictionary<string, object>;
                        if (lib != null && IsLibraryAllowed(lib, osName))
                            result.Add(lib);
                    }
                }
            }

            // patches
            if (root.TryGetValue("patches", out object patchesObj))
            {
                var patches = patchesObj as ArrayList;
                if (patches != null)
                {
                    foreach (var patchObj in patches)
                    {
                        var patch = patchObj as Dictionary<string, object>;
                        if (patch != null && patch.TryGetValue("libraries", out object plibsObj))
                        {
                            var plibs = plibsObj as ArrayList;
                            if (plibs != null)
                            {
                                foreach (var libObj in plibs)
                                {
                                    var lib = libObj as Dictionary<string, object>;
                                    if (lib != null && IsLibraryAllowed(lib, osName))
                                        result.Add(lib);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 构建完整的 classpath 字符串（包含所有库及版本核心 jar）
        /// </summary>
        public static string BuildClasspath(List<Dictionary<string, object>> libraries, string minecraftDir, string versionId)
        {
            var entries = new List<string>();
            string os = GetOsName();

            foreach (var lib in libraries)
            {
                // 跳过不符合当前操作系统的库（根据 rules 过滤）
                if (!IsLibraryAllowed(lib, os))
                    continue;

                string localPath = null;

                // 1. 优先从 downloads.artifact 中获取路径（官方 JSON 标准）
                if (lib.TryGetValue("downloads", out object downloadsObj))
                {
                    var downloads = downloadsObj as Dictionary<string, object>;
                    if (downloads != null && downloads.TryGetValue("artifact", out object artObj))
                    {
                        var artifact = artObj as Dictionary<string, object>;
                        if (artifact != null && artifact.TryGetValue("path", out object pathObj))
                        {
                            localPath = pathObj.ToString();
                        }
                    }
                }

                // 2. 如果没有 downloads 字段，手动从 name 解析
                if (string.IsNullOrEmpty(localPath) && lib.TryGetValue("name", out object nameObj))
                {
                    string name = nameObj.ToString();
                    localPath = ResolveLibraryPathFromName(name); // 使用安全的解析方法
                }

                // 3. 如果仍为空，跳过
                if (string.IsNullOrEmpty(localPath))
                    continue;

                string fullPath = Path.Combine(Path.Combine(minecraftDir, "libraries"), localPath);
                entries.Add(fullPath);
            }

            // 添加版本核心 jar（位于 versions/<versionId>/<versionId>.jar）
            string coreJar = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), versionId), versionId + ".jar");
            if (File.Exists(coreJar))
                entries.Insert(0, coreJar); // 放在首位

            return string.Join(";", entries.ToArray());
        }

        /// <summary>
        /// 从 Maven 坐标解析库文件的相对路径（相对于 libraries 目录）
        /// </summary>
        private static string ResolveLibraryPathFromName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var parts = name.Split(':');
            if (parts.Length < 3)
                return null;

            string group = parts[0].Replace('.', '/');   // 组名：ca.weblite → ca/weblite
            string artifact = parts[1];
            string version = parts[2];                    // 版本号：1.1 → 原样保留
            string classifier = parts.Length > 3 ? parts[3] : null;

            string fileName = $"{artifact}-{version}";
            if (!string.IsNullOrEmpty(classifier))
                fileName += $"-{classifier}";
            fileName += ".jar";

            return $"{group}/{artifact}/{version}/{fileName}";
        }

        // ---- 序列化辅助 ----
        private static T DeserializeJson<T>(string json) => new JavaScriptSerializer().Deserialize<T>(json);
    }

    public static class VersionJsonMerger
    {
        /// <summary>
        /// 将版本 JSON 与父版本完全合并，生成独立的 JSON 文件（移除 inheritsFrom）
        /// </summary>
        /// <param name="versionJsonPath">当前版本 JSON 的文件路径</param>
        /// <param name="minecraftDir">.minecraft 目录</param>
        public static void MergeAndSaveIndependentJson(string versionJsonPath, string minecraftDir)
        {
            if (!File.Exists(versionJsonPath))
            {
                Console.WriteLine("[Merge] 文件不存在，跳过: " + versionJsonPath);
                return;
            }

            JObject root = JObject.Parse(File.ReadAllText(versionJsonPath));

            if (root["inheritsFrom"] == null)
            {
                Console.WriteLine("[Merge] JSON 没有 inheritsFrom，已经是独立版本");
                return;
            }

            string parentId = root["inheritsFrom"].ToString();
            Console.WriteLine($"[Merge] 检测到继承自 {parentId}，正在合并...");

            JObject parentRoot = GetVersionJson(parentId, minecraftDir);
            if (parentRoot == null)
            {
                Console.WriteLine($"[Merge] 警告：无法获取父版本 {parentId} 的 JSON，跳过合并");
                return;
            }

            // ---- 合并各个字段 ----

            // 3.1 合并 libraries（去重，基于 name）
            JArray currentLibraries = root["libraries"] as JArray ?? new JArray();
            JArray parentLibraries = parentRoot["libraries"] as JArray ?? new JArray();
            var existingNames = new HashSet<string>();
            foreach (var lib in currentLibraries)
            {
                string name = lib["name"]?.ToString();
                if (!string.IsNullOrEmpty(name))
                    existingNames.Add(name);
            }
            foreach (var lib in parentLibraries)
            {
                string name = lib["name"]?.ToString();
                if (!string.IsNullOrEmpty(name) && !existingNames.Contains(name))
                {
                    currentLibraries.Add(lib);
                    existingNames.Add(name);
                }
            }
            root["libraries"] = currentLibraries;
            Console.WriteLine($"[Merge] 合并 libraries 完成，当前总数: {currentLibraries.Count}");

            // 3.2 合并 assetIndex（如果当前没有）
            if (root["assetIndex"] == null && parentRoot["assetIndex"] != null)
                root["assetIndex"] = parentRoot["assetIndex"];

            // 3.3 合并 downloads（如果当前没有）
            if (root["downloads"] == null && parentRoot["downloads"] != null)
                root["downloads"] = parentRoot["downloads"];

            // 3.4 合并 logging（如果当前没有）
            if (root["logging"] == null && parentRoot["logging"] != null)
                root["logging"] = parentRoot["logging"];

            // 3.5 合并 javaVersion（如果当前没有）
            if (root["javaVersion"] == null && parentRoot["javaVersion"] != null)
                root["javaVersion"] = parentRoot["javaVersion"];

            // 3.6 合并 arguments
            if (parentRoot["arguments"] != null)
            {
                JObject currentArgs = root["arguments"] as JObject ?? new JObject();
                JObject parentArgs = parentRoot["arguments"] as JObject;

                // 合并 game 参数
                JArray currentGame = currentArgs["game"] as JArray ?? new JArray();
                JArray parentGame = parentArgs["game"] as JArray;
                if (parentGame != null)
                {
                    foreach (var item in parentGame)
                    {
                        if (item.Type == JTokenType.String)
                        {
                            string val = item.ToString();
                            if (!currentGame.Any(x => x.Type == JTokenType.String && x.ToString() == val))
                                currentGame.Add(item);
                        }
                        else
                        {
                            currentGame.Add(item);
                        }
                    }
                    currentArgs["game"] = currentGame;
                }

                // 合并 jvm 参数
                JArray currentJvm = currentArgs["jvm"] as JArray ?? new JArray();
                JArray parentJvm = parentArgs["jvm"] as JArray;
                if (parentJvm != null)
                {
                    foreach (var item in parentJvm)
                    {
                        if (item.Type == JTokenType.String)
                        {
                            string val = item.ToString();
                            if (!currentJvm.Any(x => x.Type == JTokenType.String && x.ToString() == val))
                                currentJvm.Add(item);
                        }
                        else
                        {
                            currentJvm.Add(item);
                        }
                    }
                    currentArgs["jvm"] = currentJvm;
                }

                root["arguments"] = currentArgs;
                Console.WriteLine("[Merge] 已合并 arguments");
            }

            // 在 VersionJsonMerger.cs 的 MergeAndSaveIndependentJson 方法末尾，写回文件之前添加：

            // 如果是 NeoForge（根据 libraries 特征判断），强制设置为 Bootstrap 模式
            bool isNeoForge = false;
            foreach (var lib in currentLibraries)
            {
                string name = lib["name"]?.ToString();
                if (!string.IsNullOrEmpty(name) && name.StartsWith("net.neoforged:neoforge:"))
                {
                    isNeoForge = true;
                    break;
                }
            }

            if (isNeoForge)
            {
                // 强制使用 BootstrapLauncher
                root["mainClass"] = "cpw.mods.bootstraplauncher.BootstrapLauncher";

                // 确保 arguments.game 中包含 --launchTarget forgeclient 和 --fml.* 参数
                JObject argsObj = root["arguments"] as JObject ?? new JObject();
                JArray gameArray = argsObj["game"] as JArray ?? new JArray();

                // 检查是否已有 --fml.neoForgeVersion，没有则添加
                bool hasFmlNeoForge = false;
                foreach (var item in gameArray)
                {
                    if (item.Type == JTokenType.String && item.ToString() == "--fml.neoForgeVersion")
                    {
                        hasFmlNeoForge = true;
                        break;
                    }
                }
                if (!hasFmlNeoForge)
                {
                    // 提取 neoForgeVersion
                    string neoVersion = null;
                    foreach (var lib in currentLibraries)
                    {
                        string name = lib["name"]?.ToString();
                        if (!string.IsNullOrEmpty(name) && name.StartsWith("net.neoforged:neoforge:"))
                        {
                            var parts = name.Split(':');
                            if (parts.Length >= 3) neoVersion = parts[2];
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(neoVersion))
                    {
                        // 将 --fml.* 参数放在最前面
                        JArray newGameArray = new JArray();
                        newGameArray.Add("--launchTarget");
                        newGameArray.Add("forgeclient");
                        newGameArray.Add("--fml.neoForgeVersion");
                        newGameArray.Add(neoVersion);
                        newGameArray.Add("--fml.mcVersion");
                        newGameArray.Add("1.21.11"); // 从 parentId 获取，这里简化
                                                     // 添加原有参数
                        foreach (var item in gameArray)
                            newGameArray.Add(item);
                        argsObj["game"] = newGameArray;
                    }
                }
                root["arguments"] = argsObj;
            }

            // 保存 Minecraft 版本
            if (parentRoot["id"] != null)
            {
                root["minecraftVersion"] = parentRoot["id"];
                Console.WriteLine($"[Merge] 已保存 minecraftVersion: {parentRoot["id"]}");
            }

            // 移除继承
            root.Remove("inheritsFrom");
            Console.WriteLine("[Merge] 已移除 inheritsFrom");

            // 写回文件
            File.WriteAllText(versionJsonPath, root.ToString(Newtonsoft.Json.Formatting.Indented));
            Console.WriteLine("[Merge] ✅ 已生成独立 JSON");
        }

        /// <summary>
        /// 获取指定版本的 JSON（优先本地，否则从网络下载）
        /// </summary>
        private static JObject GetVersionJson(string versionId, string minecraftDir)
        {
            string localPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), versionId), versionId + ".json");
            if (File.Exists(localPath))
            {
                try { return JObject.Parse(File.ReadAllText(localPath)); }
                catch { }
            }

            // 从网络获取（利用原版 manifest）
            try
            {
                var manifest = MinecraftCore.GetVersionManifest();
                var versions = manifest["versions"] as ArrayList;
                Dictionary<string, object> versionEntry = null;
                foreach (var v in versions)
                {
                    var entry = v as Dictionary<string, object>;
                    if (entry != null && entry["id"].ToString() == versionId)
                    {
                        versionEntry = entry;
                        break;
                    }
                }
                if (versionEntry != null)
                {
                    string url = versionEntry["url"].ToString();
                    string jsonText = MinecraftCore.DownloadString(url);
                    JObject root = JObject.Parse(jsonText);
                    string dir = Path.GetDirectoryName(localPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(localPath, jsonText);
                    return root;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetVersionJson] 下载失败: {ex.Message}");
            }
            return null;
        }
    }
}