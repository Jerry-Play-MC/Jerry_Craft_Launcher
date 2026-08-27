using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace New_Launcher
{
    public static class NeoForgeManager
    {
        private const string NEOFORGE_MAVEN = "https://maven.neoforged.net/releases/";
        private static readonly string[] MavenMirrors = new string[]
        {
            "https://bmclapi2.bangbang93.com/maven/",   // 国内镜像
            "https://repo1.maven.org/maven2/",          // 中央仓库
            "https://maven.minecraftforge.net/",        // Forge 镜像
            "https://maven.neoforged.net/releases/"     // NeoForge 官方
        };

        static NeoForgeManager()
        {
            // 为 .NET 3.5 强制启用 TLS 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 20;
            // 如果需要忽略证书错误（仅调试），可添加：
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }
        public static class Installer
        {
            private static readonly Version MinGameVersion = new Version(1, 20, 1);
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
                    version = new Version(nums[0], nums[1], 0);
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

            public static string[] GetNeoForgeLoaderVersions(string gameVersion)
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.DefaultConnectionLimit = 20;

                if (string.IsNullOrEmpty(gameVersion) || gameVersion.Contains("加载") || gameVersion.Contains("失败"))
                    return new string[] { "不选择" };

                string cleanGame = SanitizeVersion(gameVersion);
                if (!Regex.IsMatch(cleanGame, @"^\d+(\.\d+){1,2}$"))
                    return new string[] { "不选择" };

                List<string> versions = new List<string>();
                versions.Add("不选择");

                string[] metaUrls = new string[]
                {
                    $"{NEOFORGE_MAVEN}net/neoforged/neoforge/maven-metadata.xml",
                    $"{NEOFORGE_MAVEN}net/neoforged/forge/maven-metadata.xml"
                };

                foreach (string metaUrl in metaUrls)
                {
                    try
                    {
                        string xmlContent = DownloadString(metaUrl);
                        if (string.IsNullOrEmpty(xmlContent)) continue;

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlContent);

                        XmlNode versioningNode = doc.SelectSingleNode("//metadata/versioning");
                        if (versioningNode == null) continue;

                        XmlNode versionsNode = versioningNode.SelectSingleNode("versions");
                        if (versionsNode == null) continue;

                        foreach (XmlNode versionNode in versionsNode.ChildNodes)
                        {
                            if (versionNode.Name == "version")
                            {
                                string ver = versionNode.InnerText.Trim();
                                if (!string.IsNullOrEmpty(ver) && !versions.Contains(ver))
                                {
                                    if (IsVersionCompatible(ver, cleanGame))
                                        versions.Add(ver);
                                }
                            }
                        }

                        if (versions.Count > 1)
                            break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NeoForge] 获取版本列表失败 (URL: {metaUrl}): {ex.Message}");
                    }
                }

                if (versions.Count > 1)
                {
                    List<string> toSort = versions.GetRange(1, versions.Count - 1);
                    toSort.Sort((a, b) =>
                    {
                        Version va = TryParseVersionPart(a);
                        Version vb = TryParseVersionPart(b);
                        return va.CompareTo(vb);
                    });
                    toSort.Reverse();
                    versions = new List<string> { "不选择" };
                    versions.AddRange(toSort);
                }

                if (versions.Count == 1)
                    Console.WriteLine("[NeoForge] 未找到任何版本，返回 '不选择'。");

                return versions.ToArray();
            }

            private static Version TryParseVersionPart(string version)
            {
                var match = Regex.Match(version, @"^(\d+(\.\d+){1,2})");
                if (match.Success)
                {
                    try { return new Version(match.Value); }
                    catch { }
                }
                return new Version(0, 0);
            }

            private static bool IsVersionCompatible(string neoVersion, string gameVersion)
            {
                string[] neoParts = neoVersion.Split('.');
                if (neoParts.Length >= 2)
                {
                    string neoMajorMinor = $"{neoParts[0]}.{neoParts[1]}";
                    string gameShort = gameVersion;
                    if (gameVersion.StartsWith("1."))
                        gameShort = gameVersion.Substring(2);

                    if (neoMajorMinor == gameShort)
                        return true;

                    if (neoParts.Length >= 3)
                    {
                        string neoMajorMinorPatch = $"{neoParts[0]}.{neoParts[1]}.{neoParts[2]}";
                        if (neoMajorMinorPatch == gameVersion)
                            return true;
                    }
                }

                if (Regex.IsMatch(neoVersion, @"^\d{2}w\d{2}[a-z]?$"))
                    return true;

                return false;
            }

            private static bool DownloadWithMirrors(string relativePath, string localPath, int maxRetries = 3)
            {
                foreach (string baseUrl in MavenMirrors)
                {
                    string fullUrl = baseUrl + relativePath;
                    Console.WriteLine($"[Download] 尝试从镜像 {baseUrl} 下载 {relativePath}");
                    if (TryDownloadFile(fullUrl, localPath, maxRetries))
                        return true;
                }
                return false;
            }

            private static string DownloadString(string url)
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.DefaultConnectionLimit = 20;

                for (int i = 1; i <= 3; i++)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
                        request.Timeout = 15000;
                        request.Proxy = null;
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response is HttpWebResponse err && err.StatusCode == HttpStatusCode.NotFound)
                            return null;
                        if (i == 3) throw;
                        Thread.Sleep(1000 * i);
                    }
                    catch
                    {
                        if (i == 3) throw;
                        Thread.Sleep(1000 * i);
                    }
                }
                return null;
            }

            public static string InstallNeoForge(string gameVersion, string neoVersion, bool isLaunch = false)
            {
                if (!IsGameVersionSupported(gameVersion, out string err))
                    throw new ArgumentException(err);

                string cleanGame = SanitizeVersion(gameVersion);
                string cleanNeo = SanitizeVersion(neoVersion);
                if (string.IsNullOrEmpty(cleanGame) || cleanGame == "不选择")
                    throw new ArgumentException("游戏版本无效");
                if (string.IsNullOrEmpty(cleanNeo) || cleanNeo == "不选择")
                    throw new ArgumentException("NeoForge 版本无效");

                string minecraftDir = MinecraftCore.GetMinecraftDir();
                string prefix = isLaunch ? "[启动]" : "[下载]";

                string defaultVersionId = $"neoforge-{cleanNeo}";

                // ========== 启动分支（如果 isLaunch == true） ==========
                if (isLaunch)
                {
                    string actualId = FindInstalledNeoForgeVersionId(minecraftDir, cleanNeo);
                    if (string.IsNullOrEmpty(actualId))
                        actualId = GetInstalledVersionIdFromProfiles(minecraftDir, cleanNeo);
                    if (string.IsNullOrEmpty(actualId))
                        actualId = defaultVersionId;

                    Console.WriteLine($"[启动] 使用版本 ID: {actualId}");

                    if (CheckNeoForgeIntegrity(actualId))
                    {
                        Console.WriteLine("[启动] NeoForge 文件完整，跳过下载");
                        string jsonPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), actualId), actualId + ".json");
                        // ★ 强制合并 JSON，确保成为独立版本 ★
                        VersionJsonMerger.MergeAndSaveIndependentJson(jsonPath, minecraftDir);
                        FixJsonLibraries(jsonPath, minecraftDir, cleanNeo);
                        return actualId;
                    }

                    Console.WriteLine("[启动] NeoForge 文件不完整，执行完整安装...");
                }

                // ========== 安装流程（首次安装或文件不完整时执行） ==========
                Console.WriteLine("[安装] 开始安装 NeoForge...");

                // 下载 installer（如果不存在）
                string installerPath = GetInstallerPath(cleanNeo);
                if (!File.Exists(installerPath))
                {
                    if (!DownloadInstaller(installerPath, minecraftDir, cleanGame, cleanNeo))
                        throw new Exception("下载安装器失败");
                }

                // 解析 install_profile.json
                JObject profile = GetInstallProfile(installerPath);
                JArray libraries = profile["libraries"] as JArray;
                JArray processors = profile["processors"] as JArray;
                JObject data = profile["data"] as JObject;

                // 下载所有库（包括 processor 依赖）
                if (libraries != null)
                    DownloadLibrariesFromProfile(libraries, minecraftDir);

                // 执行 processors
                if (processors != null)
                    ExecuteProcessors(processors, data, minecraftDir, cleanGame, cleanNeo);

                // 从 installer jar 中提取 version.json
                string versionJsonContent = null;
                using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(installerPath))
                {
                    var entry = zip.GetEntry("version.json");
                    if (entry != null)
                    {
                        using (var stream = zip.GetInputStream(entry))
                        using (var reader = new StreamReader(stream))
                        {
                            versionJsonContent = reader.ReadToEnd();
                        }
                    }
                }
                if (!string.IsNullOrEmpty(versionJsonContent))
                {
                    // 确定版本 ID（通常从 version.json 中的 id 字段读取，或使用你已有的 defaultVersionId）
                    string versionId = defaultVersionId; // 或从 json 中解析
                    string versionDir = Path.Combine(Path.Combine(minecraftDir, "versions"), versionId);
                    Directory.CreateDirectory(versionDir);
                    string jsonPath = Path.Combine(versionDir, versionId + ".json");
                    File.WriteAllText(jsonPath, versionJsonContent);
                }

                // 安装后获取真实版本 ID
                string newVersionId = FindInstalledNeoForgeVersionId(minecraftDir, cleanNeo);
                if (string.IsNullOrEmpty(newVersionId))
                {
                    newVersionId = GetInstalledVersionIdFromProfiles(minecraftDir, cleanNeo);
                    if (string.IsNullOrEmpty(newVersionId))
                        newVersionId = defaultVersionId;
                }
                Console.WriteLine($"[安装] 安装完成，实际版本 ID: {newVersionId}");

                // 确保版本目录存在
                string newVersionDir = Path.Combine(Path.Combine(minecraftDir, "versions"), newVersionId);
                if (!Directory.Exists(newVersionDir))
                    Directory.CreateDirectory(newVersionDir);
                string newJsonPath = Path.Combine(newVersionDir, newVersionId + ".json");

                if (!File.Exists(newJsonPath))
                    throw new Exception($"安装后未生成 JSON: {newJsonPath}");

                // ---- 新增：合并父版本 libraries ----
                JObject root = JObject.Parse(File.ReadAllText(newJsonPath));
                if (root["inheritsFrom"] != null)
                {
                    string parentId = root["inheritsFrom"].ToString();
                    Console.WriteLine($"[安装] 检测到继承自 {parentId}，正在合并 libraries...");

                    JObject parentRoot = GetVersionJson(parentId, minecraftDir);
                    if (parentRoot != null)
                    {
                        JArray parentLibraries = parentRoot["libraries"] as JArray;
                        if (parentLibraries != null)
                        {
                            JArray currentLibraries = root["libraries"] as JArray ?? new JArray();
                            // 去重合并（基于 name）
                            var existingNames = new HashSet<string>();
                            foreach (var lib in currentLibraries)
                            {
                                string name = lib["name"]?.ToString();
                                if (!string.IsNullOrEmpty(name))
                                    existingNames.Add(name);
                            }
                            int added = 0;
                            foreach (var parentLib in parentLibraries)
                            {
                                string name = parentLib["name"]?.ToString();
                                if (!string.IsNullOrEmpty(name) && !existingNames.Contains(name))
                                {
                                    currentLibraries.Add(parentLib);
                                    existingNames.Add(name);
                                    added++;
                                }
                            }
                            root["libraries"] = currentLibraries;
                            File.WriteAllText(newJsonPath, root.ToString(Newtonsoft.Json.Formatting.Indented));
                            Console.WriteLine($"[安装] 合并完成，新增 {added} 个父版本库，当前总库数: {currentLibraries.Count}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[安装] 警告：无法获取父版本 {parentId} 的 JSON，跳过合并");
                    }
                }

                // ---- 新增：下载所有缺失的库（包括父版本的） ----
                DownloadMissingLibraries(root, minecraftDir, NEOFORGE_MAVEN);

                // 提取 NeoForge 版本号
                var match2 = Regex.Match(newVersionId, @"(\d+\.\d+\.\d+)");
                if (!match2.Success) match2 = Regex.Match(cleanNeo, @"(\d+\.\d+\.\d+)");
                if (!match2.Success) throw new Exception($"无法提取 NeoForge 版本号: {newVersionId}");
                string neoVer2 = match2.Value;

                string uniJar = GetUniversalJarPath(minecraftDir, neoVer2);
                string patJar = GetPatchedJarPath(minecraftDir, neoVer2);
                bool isNew2 = IsNewNeoForgeVersion(neoVer2);

                // 复制核心 jar 到版本目录
                string newJarPath = Path.Combine(newVersionDir, newVersionId + ".jar");
                bool copied = false;

                // 先尝试 patched jar
                if (File.Exists(patJar))
                {
                    File.Copy(patJar, newJarPath, true);
                    copied = true;
                    // 如果是新版本（即 patched jar 存在），修改 MANIFEST
                    AddMinecraftDistsToManifest(newJarPath, neoVer2);
                    Console.WriteLine($"[安装] 使用 patched jar 作为核心: {patJar}");
                }
                else if (File.Exists(uniJar))
                {
                    File.Copy(uniJar, newJarPath, true);
                    copied = true;
                    // 对于旧版本，不需要修改 MANIFEST（或跳过）
                    Console.WriteLine($"[安装] 使用 universal jar 作为核心: {uniJar}");
                }
                if (!copied)
                    throw new Exception("未找到核心 jar，安装失败");

                VersionJsonMerger.MergeAndSaveIndependentJson(newJsonPath, minecraftDir);

                // 修复 JSON（可选，但保留）
                FixJsonLibraries(newJsonPath, minecraftDir, neoVer2);

                Console.WriteLine($"{prefix} NeoForge 安装完成，版本 ID: {newVersionId}");
                return newVersionId;
            }

            /// <summary>
            /// 获取指定版本的 JSON（优先本地，否则从网络下载）
            /// </summary>
            private static JObject GetVersionJson(string versionId, string minecraftDir)
            {
                // 先尝试本地
                string localPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), versionId), versionId + ".json");
                if (File.Exists(localPath))
                {
                    try { return JObject.Parse(File.ReadAllText(localPath)); }
                    catch { }
                }

                // 本地不存在，从网络获取（利用原版 manifest）
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
                        // 保存到本地以便下次使用
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

            /// <summary>
            /// 修改：使用镜像优先下载缺失的库（从 version.json 的 libraries）
            /// </summary>
            private static void DownloadMissingLibraries(JObject root, string minecraftDir, string defaultMaven)
            {
                JArray libraries = root["libraries"] as JArray;
                if (libraries == null) return;

                int total = libraries.Count;
                int downloaded = 0;
                int failed = 0;
                Console.WriteLine($"[安装] 检查 {total} 个库，开始下载缺失的...");

                string os = MinecraftCore.GetOsName();

                foreach (JToken lib in libraries)
                {
                    // 规则检查（保持不变）
                    bool shouldInclude = true;
                    var rules = lib["rules"] as JArray;
                    if (rules != null)
                    {
                        shouldInclude = false;
                        foreach (var rule in rules)
                        {
                            string action = rule["action"]?.ToString();
                            var osRule = rule["os"] as JObject;
                            if (osRule != null)
                            {
                                string osName = osRule["name"]?.ToString();
                                if (osName == os)
                                {
                                    shouldInclude = (action == "allow");
                                    break;
                                }
                            }
                            else
                            {
                                shouldInclude = (action == "allow");
                            }
                        }
                    }
                    if (!shouldInclude) continue;

                    string name = lib["name"]?.ToString();
                    if (string.IsNullOrEmpty(name)) continue;

                    // 仅获取 localPath 和 relativePath
                    ResolveLibrary(lib, defaultMaven, out string localPath, out string relativePath);
                    if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(relativePath))
                    {
                        Console.WriteLine($"[安装] 无法解析库: {name}");
                        failed++;
                        continue;
                    }

                    if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
                        continue;

                    Console.WriteLine($"[安装] 下载库: {name}");
                    if (!DownloadWithMirrors(relativePath, localPath))
                    {
                        Console.WriteLine($"[安装] 下载库 {name} 失败");
                        failed++;
                    }
                    else
                    {
                        downloaded++;
                    }
                }

                Console.WriteLine($"[安装] 库检查完成，新下载 {downloaded} 个文件，失败 {failed} 个。");
                if (failed > 0)
                    throw new Exception($"部分库下载失败 ({failed} 个)，请检查网络后重试。");
            }

            /// <summary>
            /// 根据 JSON 中的库条目解析本地路径和下载 URL，正确处理分类器（如 :api, :universal）
            /// </summary>
            private static void ResolveLibrary(JToken lib, string defaultMaven, out string localPath, out string relativePath)
            {
                localPath = null;
                relativePath = null;
                string mcDir = MinecraftCore.GetMinecraftDir();

                // 1. 优先使用 downloads 字段
                var downloads = lib["downloads"];
                if (downloads != null)
                {
                    var artifactToken = downloads["artifact"];
                    if (artifactToken != null)
                    {
                        string path = artifactToken["path"]?.ToString();
                        string url = artifactToken["url"]?.ToString();
                        if (!string.IsNullOrEmpty(path))
                        {
                            localPath = Path.Combine(Path.Combine(mcDir, "libraries"), path);
                            relativePath = path;  // 直接使用 Maven 路径
                            return;
                        }
                    }
                }

                // 2. 手动解析 Maven 坐标
                string name = lib["name"]?.ToString();
                if (string.IsNullOrEmpty(name)) return;
                string[] parts = name.Split(':');
                if (parts.Length < 3) return;

                string group = parts[0].Replace('.', '/');
                string artifactId = parts[1];
                string version = parts[2];
                string classifier = parts.Length > 3 ? parts[3] : null;

                string fileName = $"{artifactId}-{version}";
                string ext = ".jar";

                if (!string.IsNullOrEmpty(classifier))
                {
                    int atIndex = classifier.IndexOf('@');
                    if (atIndex >= 0)
                    {
                        string baseName = classifier.Substring(0, atIndex);
                        string extension = classifier.Substring(atIndex + 1);
                        fileName += $"-{baseName}";
                        ext = "." + extension;
                    }
                    else
                    {
                        fileName += $"-{classifier}";
                        ext = ".jar";
                    }
                }

                string relPath = $"{group}/{artifactId}/{version}/{fileName}{ext}";
                // 替换 @ 为 .（某些分类器可能包含 @）
                relPath = relPath.Replace("@", ".");

                localPath = Path.Combine(Path.Combine(mcDir, "libraries"), relPath);
                relativePath = relPath;
            }

            // 辅助方法：扫描已安装版本（与之前相同）
            private static string FindInstalledNeoForgeVersionId(string minecraftDir, string neoVersion)
            {
                string versionsDir = Path.Combine(minecraftDir, "versions");
                if (!Directory.Exists(versionsDir)) return null;

                foreach (string dir in Directory.GetDirectories(versionsDir))
                {
                    string dirName = Path.GetFileName(dir);
                    if (dirName.IndexOf(neoVersion, StringComparison.OrdinalIgnoreCase) >= 0 &&
                        (dirName.IndexOf("neoforge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         dirName.IndexOf("forge", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return dirName;
                    }
                }
                return null;
            }

            public static bool CheckNeoForgeIntegrity(string versionId)
            {
                if (string.IsNullOrEmpty(versionId)) return false;

                string minecraftDir = MinecraftCore.GetMinecraftDir();
                string versionDir = Path.Combine(Path.Combine(minecraftDir, "versions"), versionId);
                string jsonPath = Path.Combine(versionDir, versionId + ".json");
                string jarPath = Path.Combine(versionDir, versionId + ".jar");

                // 版本 JSON 和 JAR 必须存在
                if (!File.Exists(jsonPath) || !File.Exists(jarPath) || new FileInfo(jarPath).Length == 0)
                    return false;

                // 提取 NeoForge 版本号
                var match = Regex.Match(versionId, @"(\d+\.\d+\.\d+)");
                if (!match.Success) return false;
                string neoVersion = match.Value;

                // universal 是必需的
                string universalJar = GetUniversalJarPath(minecraftDir, neoVersion);
                if (!File.Exists(universalJar) || new FileInfo(universalJar).Length == 0)
                    return false;

                // 不再强制检查 patched，因为某些版本可能没有
                return true;
            }

            public static bool CheckFileResourceIntegrity(string versionId, string minecraftDir)
            {
                // 直接复用 CheckNeoForgeIntegrity，因为逻辑一致
                return CheckNeoForgeIntegrity(versionId);
            }

            private static bool DownloadInstaller(string savePath, string minecraftDir, string mcVersion, string neoVersion)
            {
                string dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string installerFileName = $"neoforge-{neoVersion}-installer.jar";
                string[] possiblePaths = new string[]
                {
                    $"net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-installer.jar",
                    $"net/neoforged/forge/{neoVersion}/{installerFileName}",
                    $"net/neoforged/forge/{mcVersion}-{neoVersion}/{installerFileName}"
                };

                foreach (string baseUrl in MavenMirrors)
                {
                    foreach (string relPath in possiblePaths)
                    {
                        string fullUrl = baseUrl + relPath;
                        try
                        {
                            if (TryDownloadFile(fullUrl, savePath))
                                return true;
                        }
                        catch { }
                    }
                }
                return false;
            }

            private static bool TryDownloadFile(string url, string localPath, int maxRetries = 5)
            {
                // 确保目标目录存在
                string dir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "GET";
                        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";
                        request.Accept = "*/*";
                        request.Timeout = 30000;
                        request.ReadWriteTimeout = 30000;
                        request.KeepAlive = false;
                        request.Proxy = null;
                        request.AllowAutoRedirect = true;
                        // 添加额外头
                        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                        request.Headers.Add("Accept-Encoding", "gzip, deflate, br");

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            if (response.StatusCode != HttpStatusCode.OK)
                                return false;
                            using (Stream stream = response.GetResponseStream())
                            using (FileStream fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                    fs.Write(buffer, 0, bytesRead);
                            }
                            return true;
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response is HttpWebResponse err && err.StatusCode == HttpStatusCode.NotFound)
                            return false;
                        Console.WriteLine($"[TryDownload] 尝试 {attempt}/{maxRetries} 失败: {ex.Message}");
                        if (attempt == maxRetries)
                            return false;
                        int delay = (int)Math.Pow(2, attempt) * 1000;
                        Thread.Sleep(delay);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TryDownload] 尝试 {attempt}/{maxRetries} 异常: {ex.Message}");
                        if (attempt == maxRetries)
                            return false;
                        int delay = (int)Math.Pow(2, attempt) * 1000;
                        Thread.Sleep(delay);
                    }
                }
                return false;
            }

            private static string FindJavaExecutable(int? minMajorVersion = null)
            {
                string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (!string.IsNullOrEmpty(javaHome))
                {
                    string exe = Path.Combine(Path.Combine(javaHome, "bin"), "java.exe");
                    if (File.Exists(exe))
                    {
                        int version = GetJavaMajorVersion(exe);
                        if (!minMajorVersion.HasValue || version >= minMajorVersion.Value)
                            return exe;
                    }
                }

                string pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathEnv))
                {
                    foreach (string dir in pathEnv.Split(';'))
                    {
                        string exe = Path.Combine(dir, "java.exe");
                        if (File.Exists(exe))
                        {
                            int version = GetJavaMajorVersion(exe);
                            if (!minMajorVersion.HasValue || version >= minMajorVersion.Value)
                                return exe;
                        }
                    }
                }

                string[] commonRoots = new string[]
                {
                    @"C:\Program Files\Java",
                    @"C:\Program Files (x86)\Java"
                };
                foreach (string root in commonRoots)
                {
                    if (Directory.Exists(root))
                    {
                        foreach (string subDir in Directory.GetDirectories(root))
                        {
                            string exe = Path.Combine(Path.Combine(subDir, "bin"), "java.exe");
                            if (File.Exists(exe))
                            {
                                int version = GetJavaMajorVersion(exe);
                                if (!minMajorVersion.HasValue || version >= minMajorVersion.Value)
                                    return exe;
                            }
                        }
                    }
                }

                string launcherJava = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
                    "Launcher Setting"), "Java"), "8"), "bin"), "java.exe");
                if (File.Exists(launcherJava))
                {
                    int version = GetJavaMajorVersion(launcherJava);
                    if (!minMajorVersion.HasValue || version >= minMajorVersion.Value)
                        return launcherJava;
                }

                return null;
            }

            private static int GetJavaMajorVersion(string javaPath)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo(javaPath, "-version")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    };
                    using (Process p = Process.Start(psi))
                    {
                        string output = p.StandardError.ReadToEnd();
                        p.WaitForExit();
                        var match = Regex.Match(output, @"version ""(\d+)\.\d+\.\d+");
                        if (match.Success)
                        {
                            int major = int.Parse(match.Groups[1].Value);
                            return major;
                        }
                    }
                }
                catch { }
                return 0;
            }

            // 新增：获取安装包缓存目录
            private static string GetInstallerCacheDir()
            {
                string launcherPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                string cacheDir = Path.Combine(Path.Combine(launcherPath, "Launcher Setting"), "Mode Loader Installer");
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);
                return cacheDir;
            }

            private static void AddMinecraftDistsToManifest(string jarPath, string neoVersion = null)
            {
                if (!File.Exists(jarPath)) return;
                if (string.IsNullOrEmpty(neoVersion) || !IsNewNeoForgeVersion(neoVersion))
                {
                    Console.WriteLine($"[NeoForge] 老版本，跳过 MANIFEST 修改: {jarPath}");
                    return;
                }

                Console.WriteLine($"[NeoForge] 修补 MANIFEST (新版本): {jarPath}");
                string tempDir = Path.Combine(Path.GetTempPath(), "neoforge_manifest_patch_" + Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(tempDir);

                    // 解压 jar 到临时目录
                    var fastZip = new FastZip();
                    fastZip.ExtractZip(jarPath, tempDir, null);

                    // 修改 MANIFEST.MF
                    string manifestPath = Path.Combine(Path.Combine(tempDir, "META-INF"), "MANIFEST.MF");
                    if (File.Exists(manifestPath))
                    {
                        string content = File.ReadAllText(manifestPath);
                        if (!content.Contains("Minecraft-Dists"))
                        {
                            content = content.TrimEnd() + "\r\nMinecraft-Dists: client\r\n";
                            // 可选：修改 Main-Class（通常不改）
                            File.WriteAllText(manifestPath, content);
                            Console.WriteLine("[NeoForge] MANIFEST 已更新，添加了 Minecraft-Dists");
                        }
                        else
                        {
                            Console.WriteLine("[NeoForge] MANIFEST 已包含 Minecraft-Dists，无需修改");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[NeoForge] 警告：未找到 MANIFEST.MF，跳过修改");
                    }

                    // 重新打包为 jar
                    var fastZip2 = new FastZip();
                    fastZip2.CreateZip(jarPath, tempDir, true, null);
                    Console.WriteLine($"[NeoForge] MANIFEST 修补完成: {jarPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NeoForge] 修补 MANIFEST 失败: {ex.Message}");
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                        try { Directory.Delete(tempDir, true); } catch { }
                }
            }

            /// <summary>
            /// 启动时修复 NeoForge JSON，补全缺失的库条目和字段。
            /// 确保 minecraft-client-patched 和 neoforge-universal 都有完整 artifact 信息。
            /// </summary>
            public static void FixNeoForgeJsonOnLaunch(string versionId, string minecraftDir)
            {
                Console.WriteLine($"[Fix] 开始修复 NeoForge JSON，versionId='{versionId}'");

                if (string.IsNullOrEmpty(versionId) || string.IsNullOrEmpty(minecraftDir))
                {
                    Console.WriteLine("[Fix] versionId 或 minecraftDir 为空");
                    return;
                }

                // 只处理 NeoForge 版本
                string lowerId = versionId.ToLower();
                if (!lowerId.Contains("neoforge") && !lowerId.Contains("neo forge"))
                {
                    Console.WriteLine($"[Fix] 不是 NeoForge 版本: {versionId}");
                    return;
                }

                // 提取 NeoForge 版本号 (形如 21.11.45)
                var match = Regex.Match(versionId, @"(\d+\.\d+\.\d+)");
                if (!match.Success)
                {
                    Console.WriteLine($"[Fix] 无法从版本名 '{versionId}' 提取 NeoForge 版本号");
                    return;
                }
                string neoVersion = match.Value;
                Console.WriteLine($"[Fix] 提取到 NeoForge 版本号: {neoVersion}");

                // 构建可能的版本文件夹名（尝试多种命名）
                string[] possibleNames = new string[]
                {
                    versionId,
                    $"Neoforge {neoVersion}",
                    $"neoforge-{neoVersion}",
                    $"NeoForge {neoVersion}",
                    $"NeoForge-{neoVersion}",
                    $"neoforge_{neoVersion}"
                };
                possibleNames = possibleNames.Distinct().ToArray();

                foreach (string name in possibleNames)
                {
                    string versionJsonPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), name), name + ".json");
                    Console.WriteLine($"[Fix] 尝试路径: {versionJsonPath}");
                    if (File.Exists(versionJsonPath))
                    {
                        Console.WriteLine($"[Fix] 找到 JSON 文件: {versionJsonPath}");
                        FixJsonInternal(versionJsonPath, minecraftDir, neoVersion);
                        return;
                    }
                }

                Console.WriteLine($"[Fix] 未找到版本 JSON 文件，尝试过的名称: {string.Join(", ", possibleNames)}");
            }

            /// <summary>
            /// 核心修复逻辑：根据 NeoForge 版本（新/老）补全或移除 libraries 条目。
            /// </summary>
            /// <param name="libraries">JArray 引用，直接修改</param>
            /// <param name="minecraftDir">.minecraft 目录</param>
            /// <param name="neoVersion">NeoForge 版本号，如 "21.11.45"</param>
            /// <param name="modified">输出是否发生了修改</param>
            private static void FixLibrariesCore(JArray libraries, string minecraftDir, string neoVersion, out bool modified)
            {
                modified = false;
                bool isNew = IsNewNeoForgeVersion(neoVersion);

                // ========== 1. 处理 minecraft-client-patched ==========
                JObject patchedLib = null;
                bool patchedFound = false;
                foreach (JObject lib in libraries.OfType<JObject>())
                {
                    string name = lib["name"]?.ToString();
                    if (!string.IsNullOrEmpty(name) && name.StartsWith("net.neoforged:minecraft-client-patched:"))
                    {
                        patchedFound = true;
                        patchedLib = lib;
                        break;
                    }
                }

                string patchedJarPath = GetPatchedJarPath(minecraftDir, neoVersion);
                bool patchedFileExists = File.Exists(patchedJarPath);

                if (patchedFileExists)
                {
                    // 文件存在：确保条目存在且完整
                    if (!patchedFound)
                    {
                        patchedLib = new JObject();
                        patchedLib["name"] = $"net.neoforged:minecraft-client-patched:{neoVersion}";
                        var downloads = new JObject();
                        var artifact = new JObject();
                        downloads["artifact"] = artifact;
                        patchedLib["downloads"] = downloads;
                        libraries.Add(patchedLib);
                        modified = true;
                        Console.WriteLine($"[Fix] 添加 minecraft-client-patched 条目 (文件存在)");
                    }
                    // 补全 artifact 信息（如果有必要）
                    var patchedArtifact = (patchedLib["downloads"] as JObject)?["artifact"] as JObject;
                    if (patchedArtifact != null)
                    {
                        // ... 补全 sha1, size, url, path （从文件读取）...
                    }
                }
                else
                {
                    // 文件不存在：如果已有条目，保留它（不删除），但可以输出警告
                    if (patchedFound)
                    {
                        Console.WriteLine($"[Fix] 警告：minecraft-client-patched 文件不存在，但保留条目（请手动下载）");
                    }
                    else
                    {
                        // 不创建条目，因为文件不存在
                        Console.WriteLine($"[Fix] minecraft-client-patched 文件不存在，跳过添加");
                    }
                }
                // 注意：不要主动删除 patched 条目！

                // ========== 2. 处理 neoforge-universal (所有版本都必须存在且完整) ==========
                JObject universalLib = null;
                JObject universalArtifact = null;
                bool universalFound = false;

                foreach (JObject lib in libraries.OfType<JObject>())
                {
                    string name = lib["name"]?.ToString();
                    if (!string.IsNullOrEmpty(name) && name.StartsWith("net.neoforged:neoforge:") && name.EndsWith(":universal"))
                    {
                        universalFound = true;
                        universalLib = lib;
                        universalArtifact = lib["downloads"]?["artifact"] as JObject;
                        break;
                    }
                }

                string universalJarPath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(
                    minecraftDir, "libraries"), "net"), "neoforged"), "neoforge"),
                    neoVersion), $"neoforge-{neoVersion}-universal.jar");

                bool universalFileExists = File.Exists(universalJarPath);

                if (!universalFound)
                {
                    // 没有 universal 条目，创建
                    universalLib = new JObject();
                    universalLib["name"] = $"net.neoforged:neoforge:{neoVersion}:universal";
                    var downloads = new JObject();
                    universalArtifact = new JObject();
                    downloads["artifact"] = universalArtifact;
                    universalLib["downloads"] = downloads;
                    libraries.Add(universalLib);
                    modified = true;
                    Console.WriteLine($"[Fix] 添加 neoforge-universal 条目");
                }

                // 补全 universal artifact 信息（始终需要，即使文件不存在也尽量补全）
                if (universalArtifact != null)
                {
                    bool needSha1 = universalArtifact["sha1"] == null;
                    bool needSize = universalArtifact["size"] == null;
                    bool needUrl = universalArtifact["url"] == null;
                    bool needPath = universalArtifact["path"] == null;

                    if (needSha1 || needSize || needUrl || needPath)
                    {
                        if (universalFileExists)
                        {
                            try
                            {
                                using (var fs = new FileStream(universalJarPath, FileMode.Open, FileAccess.Read))
                                using (var sha = System.Security.Cryptography.SHA1.Create())
                                {
                                    byte[] hash = sha.ComputeHash(fs);
                                    string sha1 = BitConverter.ToString(hash).Replace("-", "").ToLower();
                                    long size = fs.Length;

                                    if (needSha1) universalArtifact["sha1"] = sha1;
                                    if (needSize) universalArtifact["size"] = size;
                                    if (needUrl) universalArtifact["url"] = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-universal.jar";
                                    if (needPath) universalArtifact["path"] = $"net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-universal.jar";
                                    modified = true;
                                    Console.WriteLine($"[Fix] 补全 neoforge-universal 信息成功");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Fix] 补全 neoforge-universal 失败: {ex.Message}");
                            }
                        }
                        else
                        {
                            // 文件不存在，但仍尽量补全 URL 和 path（无 hash/size）
                            if (needUrl) universalArtifact["url"] = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-universal.jar";
                            if (needPath) universalArtifact["path"] = $"net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-universal.jar";
                            if (needSha1) universalArtifact["sha1"] = "0000000000000000000000000000000000000000"; // 占位
                            if (needSize) universalArtifact["size"] = 0;
                            modified = true;
                            Console.WriteLine($"[Fix] 补全 neoforge-universal 基本信息 (文件缺失)");
                        }
                    }
                }
            }

            private static string GetInstallerPath(string neoVersion)
            {
                string cacheDir = GetInstallerCacheDir();
                return Path.Combine(cacheDir, $"neoforge-{neoVersion}-installer.jar");
            }

            public static string GetPatchedJarPath(string minecraftDir, string neoVersion)
            {
                return Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(
                    minecraftDir, "libraries"), "net"), "neoforged"), "minecraft-client-patched"),
                    neoVersion), $"minecraft-client-patched-{neoVersion}.jar");
            }

            public static string GetUniversalJarPath(string minecraftDir, string neoVersion)
            {
                return Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(
                    minecraftDir, "libraries"), "net"), "neoforged"), "neoforge"),
                    neoVersion), $"neoforge-{neoVersion}-universal.jar");
            }

            private static void FixJsonInternal(string versionJsonPath, string minecraftDir, string neoVersion)
            {
                if (!File.Exists(versionJsonPath))
                {
                    Console.WriteLine($"[Fix] 文件不存在: {versionJsonPath}");
                    return;
                }

                JObject root;
                try
                {
                    root = JObject.Parse(File.ReadAllText(versionJsonPath));
                    Console.WriteLine($"[Fix] JSON 解析成功: {versionJsonPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Fix] 解析 JSON 失败: {ex.Message}");
                    return;
                }

                JArray libraries = root["libraries"] as JArray;
                if (libraries == null)
                {
                    Console.WriteLine("[Fix] JSON 中没有 libraries 数组");
                    return;
                }

                FixLibrariesCore(libraries, minecraftDir, neoVersion, out bool modified);

                if (modified)
                {
                    try
                    {
                        File.WriteAllText(versionJsonPath, root.ToString(Newtonsoft.Json.Formatting.Indented));
                        Console.WriteLine($"[Fix] ✅ 已保存修复后的 JSON: {versionJsonPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Fix] 保存 JSON 失败: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("[Fix] ℹ️ JSON 无需修复");
                }
            }

            private static void FixJsonLibraries(string jsonPath, string minecraftDir, string neoVersion)
            {
                if (!File.Exists(jsonPath))
                {
                    Console.WriteLine($"[Fix] 文件不存在: {jsonPath}");
                    return;
                }

                JObject root;
                try
                {
                    root = JObject.Parse(File.ReadAllText(jsonPath));
                    Console.WriteLine($"[Fix] JSON 解析成功: {jsonPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Fix] 解析 JSON 失败: {ex.Message}");
                    return;
                }

                JArray libraries = root["libraries"] as JArray;
                if (libraries == null)
                {
                    Console.WriteLine("[Fix] JSON 中没有 libraries 数组");
                    return;
                }

                FixLibrariesCore(libraries, minecraftDir, neoVersion, out bool modified);

                if (modified)
                {
                    try
                    {
                        File.WriteAllText(jsonPath, root.ToString(Newtonsoft.Json.Formatting.Indented));
                        Console.WriteLine($"[Fix] ✅ 已保存修复后的 JSON: {jsonPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Fix] 保存 JSON 失败: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("[Fix] ℹ️ JSON 无需修复");
                }
            }

            public static bool IsNewNeoForgeVersion(string neoVersion)
            {
                if (string.IsNullOrEmpty(neoVersion)) return true;
                var parts = neoVersion.Split('.');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int major))
                    return major >= 21; // 21 及以上需要 patched，可调整为 22 等
                return true;
            }
            private static string GetInstalledVersionIdFromProfiles(string minecraftDir, string neoVersion)
            {
                string profilePath = Path.Combine(minecraftDir, "launcher_profiles.json");
                if (!File.Exists(profilePath)) return null;
                try
                {
                    var json = JObject.Parse(File.ReadAllText(profilePath));
                    var profiles = json["profiles"] as JObject;
                    if (profiles == null) return null;
                    string latestId = null;
                    DateTime latestTime = DateTime.MinValue;
                    foreach (var prop in profiles.Properties())
                    {
                        string id = prop.Name;
                        var profile = prop.Value as JObject;
                        if (profile == null) continue;
                        string lastUsed = profile["lastUsed"]?.ToString();
                        if (!string.IsNullOrEmpty(lastUsed) && DateTime.TryParse(lastUsed, out DateTime used))
                        {
                            if (id.Contains(neoVersion) || id.Contains("neoforge") || id.Contains("NeoForge"))
                            {
                                if (used > latestTime)
                                {
                                    latestTime = used;
                                    latestId = id;
                                }
                            }
                        }
                    }
                    return latestId;
                }
                catch { return null; }
            }

            /// <summary>
            /// 从版本 ID 中提取 NeoForge 版本号（如 "21.1.230"）
            /// </summary>
            public static string ExtractNeoVersion(string versionId)
            {
                var match = Regex.Match(versionId, @"(\d+\.\d+\.\d+)");
                return match.Success ? match.Value : null;
            }

            /// <summary>
            /// 修改：调整 URL 顺序，优先使用镜像
            /// </summary>
            public static bool TryDownloadPatchedJar(string minecraftDir, string neoVersion)
            {
                string localPath = GetPatchedJarPath(minecraftDir, neoVersion);
                if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
                    return true;

                // 构建相对路径
                string relativePath = $"net/neoforged/minecraft-client-patched/{neoVersion}/minecraft-client-patched-{neoVersion}.jar";
                string dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // 使用镜像优先下载
                return DownloadWithMirrors(relativePath, localPath);
            }

            /// <summary>
            /// 确保 JSON 的 libraries 中已包含 patched 条目（不下载，仅检查/补全）
            /// 注意：root 是 Dictionary<string, object> 类型（来自 LoadVersionJson）
            /// </summary>
            public static bool EnsurePatchedLibraryExists(Dictionary<string, object> root, string minecraftDir, string neoVersion)
            {
                if (root == null || !root.ContainsKey("libraries")) return false;

                // 获取 libraries 列表（可能是 ArrayList 或 List<object>）
                var libraries = root["libraries"] as IList;
                if (libraries == null) return false;

                // 1. 检查是否已有 patched 条目
                bool found = false;
                foreach (var item in libraries)
                {
                    var lib = item as Dictionary<string, object>;
                    if (lib != null && lib.ContainsKey("name"))
                    {
                        string name = lib["name"]?.ToString();
                        if (!string.IsNullOrEmpty(name) && name.StartsWith("net.neoforged:minecraft-client-patched:"))
                        {
                            found = true;
                            break;
                        }
                    }
                }
                if (found) return true;

                // 2. 若文件存在则补全条目
                string patchedJarPath = GetPatchedJarPath(minecraftDir, neoVersion);
                if (!File.Exists(patchedJarPath)) return false;

                // 3. 创建新条目（Dictionary 格式）
                var newLib = new Dictionary<string, object>
                {
                    ["name"] = $"net.neoforged:minecraft-client-patched:{neoVersion}"
                };
                var downloads = new Dictionary<string, object>();
                var artifact = new Dictionary<string, object>();
                downloads["artifact"] = artifact;
                newLib["downloads"] = downloads;

                // 填充 artifact（从文件计算 SHA1 和大小）
                try
                {
                    using (var fs = new FileStream(patchedJarPath, FileMode.Open, FileAccess.Read))
                    using (var sha = System.Security.Cryptography.SHA1.Create())
                    {
                        byte[] hash = sha.ComputeHash(fs);
                        string sha1 = BitConverter.ToString(hash).Replace("-", "").ToLower();
                        long size = fs.Length;
                        artifact["sha1"] = sha1;
                        artifact["size"] = size;
                        artifact["url"] = $"https://maven.neoforged.net/releases/net/neoforged/minecraft-client-patched/{neoVersion}/minecraft-client-patched-{neoVersion}.jar";
                        artifact["path"] = $"net/neoforged/minecraft-client-patched/{neoVersion}/minecraft-client-patched-{neoVersion}.jar";
                    }
                }
                catch
                {
                    // 无法读取文件，填写占位
                    artifact["sha1"] = "0000000000000000000000000000000000000000";
                    artifact["size"] = 0;
                    artifact["url"] = $"https://maven.neoforged.net/releases/net/neoforged/minecraft-client-patched/{neoVersion}/minecraft-client-patched-{neoVersion}.jar";
                    artifact["path"] = $"net/neoforged/minecraft-client-patched/{neoVersion}/minecraft-client-patched-{neoVersion}.jar";
                }

                libraries.Add(newLib);
                return true;
            }

            private static JObject GetInstallProfile(string installerPath)
            {
                using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(installerPath))
                {
                    var entry = zip.GetEntry("install_profile.json");
                    if (entry == null) throw new Exception("install_profile.json not found in installer.");
                    using (var stream = zip.GetInputStream(entry))
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        return JObject.Parse(json);
                    }
                }
            }

            /// <summary>
            /// 修改：使用镜像优先下载所有库（从 install_profile.json）
            /// </summary>
            private static void DownloadLibrariesFromProfile(JArray libs, string minecraftDir)
            {
                bool anyFailed = false;

                foreach (JToken lib in libs)
                {
                    string name = lib["name"]?.ToString();
                    if (string.IsNullOrEmpty(name)) continue;

                    // 仅获取 localPath 和 relativePath，不再生成官方 URL
                    ResolveLibrary(lib, NEOFORGE_MAVEN, out string localPath, out string relativePath);
                    if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(relativePath))
                    {
                        Console.WriteLine($"[NeoForge] 无法解析库: {name}");
                        continue;
                    }

                    // 如果文件已存在且大小 > 0，跳过
                    if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
                        continue;

                    Console.WriteLine($"[NeoForge] 开始下载库: {name}");
                    if (!DownloadWithMirrors(relativePath, localPath))
                    {
                        Console.WriteLine($"[NeoForge] 警告：下载库 {name} 失败");
                        anyFailed = true;
                    }
                }

                if (anyFailed)
                    throw new Exception("部分库下载失败，无法继续安装。请检查网络。");
            }

            private static void ExecuteProcessors(JArray processors, JObject dataMap, string minecraftDir, string mcVersion, string neoVersion)
            {
                foreach (JObject processor in processors.OfType<JObject>())
                {
                    // 检查 sides（仅客户端）
                    var sides = processor["sides"] as JArray;
                    if (sides != null && !sides.Any(s => s.ToString() == "client"))
                    {
                        Console.WriteLine($"[NeoForge] 跳过 processor（仅服务端）");
                        continue;
                    }

                    string jar = processor["jar"]?.ToString();
                    if (string.IsNullOrEmpty(jar)) continue;

                    // 将 jar 坐标转换为相对路径
                    string relJar = jar.Contains(':') ? MavenCoordsToRelativePath(jar) : jar;
                    if (string.IsNullOrEmpty(relJar))
                    {
                        Console.WriteLine($"[NeoForge] 无法解析 jar: {jar}");
                        continue;
                    }
                    string mainJar = Path.Combine(Path.Combine(minecraftDir, "libraries"), relJar);
                    if (!File.Exists(mainJar))
                    {
                        Console.WriteLine($"[NeoForge] 警告: 主 jar 不存在: {mainJar}");
                        continue;
                    }

                    // 构建 classpath（去重）
                    var cpEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    cpEntries.Add(mainJar);

                    var classpath = processor["classpath"] as JArray;
                    if (classpath != null)
                    {
                        foreach (string cp in classpath.OfType<JValue>().Select(v => v.ToString()))
                        {
                            string cpRel = cp.Contains(':') ? MavenCoordsToRelativePath(cp) : cp;
                            if (!string.IsNullOrEmpty(cpRel))
                            {
                                string cpPath = Path.Combine(Path.Combine(minecraftDir, "libraries"), cpRel);
                                if (File.Exists(cpPath))
                                    cpEntries.Add(cpPath);
                                else
                                    Console.WriteLine($"[NeoForge] 警告: classpath 库不存在: {cpPath}");
                            }
                        }
                    }

                    // 构建参数列表（原始）
                    var rawArgs = new List<string>();
                    var argsTokens = processor["args"] as JArray;
                    if (argsTokens != null)
                    {
                        foreach (var token in argsTokens)
                        {
                            string arg = token.ToString();
                            arg = ReplaceVariables(arg, dataMap, minecraftDir, mcVersion, neoVersion);
                            rawArgs.Add(arg);
                        }
                    }

                    // ★★★ 去重逻辑：检测到第二个 --task 则截断 ★★★
                    List<string> argsList = new List<string>(rawArgs);
                    if (argsList.Count > 0)
                    {
                        int firstTaskIndex = argsList.IndexOf("--task");
                        if (firstTaskIndex >= 0)
                        {
                            int secondTaskIndex = -1;
                            for (int i = firstTaskIndex + 1; i < argsList.Count; i++)
                            {
                                if (argsList[i] == "--task")
                                {
                                    secondTaskIndex = i;
                                    break;
                                }
                            }
                            if (secondTaskIndex >= 0)
                            {
                                argsList = argsList.GetRange(0, secondTaskIndex);
                                Console.WriteLine("[NeoForge] 检测到重复参数，已自动去重，保留第一次出现的参数序列。");
                            }
                        }
                    }

                    // 在 argsList 构建完成后，处理 /data/client.lzma
                    for (int i = 0; i < argsList.Count; i++)
                    {
                        string arg = argsList[i];
                        if ((arg == "--apply-patches" || arg == "--apply") && i + 1 < argsList.Count)
                        {
                            string patchValue = argsList[i + 1];
                            if (patchValue.StartsWith("/data/"))
                            {
                                // 提取补丁文件（与原有逻辑完全相同）
                                string installerPath = GetInstallerPath(neoVersion);
                                string tempDir = Path.Combine(Path.GetTempPath(), "neoforge_patches_" + Guid.NewGuid().ToString("N"));
                                Directory.CreateDirectory(tempDir);
                                string extractedPath = Path.Combine(tempDir, Path.GetFileName(patchValue));
                                try
                                {
                                    using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(installerPath))
                                    {
                                        var entry = zip.GetEntry(patchValue.TrimStart('/'));
                                        if (entry != null)
                                        {
                                            using (var stream = zip.GetInputStream(entry))
                                            using (var fs = new FileStream(extractedPath, FileMode.Create))
                                            {
                                                byte[] buffer = new byte[8192];
                                                int bytesRead;
                                                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                                    fs.Write(buffer, 0, bytesRead);
                                            }
                                            argsList[i + 1] = extractedPath;
                                            Console.WriteLine($"[NeoForge] 已提取补丁文件到: {extractedPath}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[NeoForge] 警告：在 installer 中未找到 {patchValue}，保留原值");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[NeoForge] 提取补丁文件失败: {ex.Message}");
                                }
                            }
                        }
                    }

                    // 在 argsList 构建完成后（替换变量后）：
                    for (int i = 0; i < argsList.Count; i++)
                    {
                        if (argsList[i] == "--neoform-data" && i + 1 < argsList.Count)
                        {
                            string neoformPath = argsList[i + 1];
                            // 构建完整路径（相对于 .minecraft/libraries）
                            string fullPath = Path.Combine(minecraftDir, neoformPath);
                            if (!File.Exists(fullPath))
                            {
                                // 移除 --neoform-data 及其值
                                argsList.RemoveAt(i); // 移除 --neoform-data
                                argsList.RemoveAt(i); // 移除后面的值（索引已变）
                                Console.WriteLine("[NeoForge] 警告：neoform-data 文件不存在，已移除该参数，继续安装");
                                break; // 只处理一次
                            }
                        }
                    }

                    // 在 argsList 构建完成后，添加日志
                    Console.WriteLine("[ExecuteProcessors] 最终参数列表:");
                    foreach (var arg in argsList)
                    {
                        Console.WriteLine($"  {arg}");
                    }

                    // 确定主类
                    string mainClass = processor["mainClass"]?.ToString();
                    if (string.IsNullOrEmpty(mainClass))
                    {
                        mainClass = GetMainClassFromJar(mainJar);
                        if (string.IsNullOrEmpty(mainClass))
                            throw new Exception($"无法确定 Processor 的主类（jar: {jar}）");
                        Console.WriteLine($"[NeoForge] 从 MANIFEST 读取 mainClass: {mainClass}");
                    }
                    else
                    {
                        Console.WriteLine($"[NeoForge] 使用指定 mainClass: {mainClass}");
                    }

                    // 查找 Java
                    bool isNew = IsNewNeoForgeVersion(neoVersion);
                    int minJavaMajor = isNew ? 17 : 8;
                    string javaPath = FindJavaExecutable(minJavaMajor);
                    if (string.IsNullOrEmpty(javaPath))
                        javaPath = FindJavaExecutable(null);
                    if (string.IsNullOrEmpty(javaPath))
                        throw new Exception("未找到 Java 运行时");

                    // 构建 classpath 字符串
                    string cpString = string.Join(";", cpEntries.ToArray());

                    // 构建命令行参数（每个参数单独处理引号）
                    List<string> allArgs = new List<string>();
                    allArgs.Add("-Djava.net.preferIPv4Stack=true");
                    allArgs.Add("-cp");
                    if (cpString.Contains(" "))
                        allArgs.Add("\"" + cpString + "\"");
                    else
                        allArgs.Add(cpString);
                    allArgs.Add(mainClass);
                    // 添加去重后的参数，且每个参数如果包含空格则加引号
                    foreach (string arg in argsList)
                    {
                        if (arg.Contains(" ") && !arg.StartsWith("\""))
                            allArgs.Add("\"" + arg + "\"");
                        else
                            allArgs.Add(arg);
                    }

                    // 启动进程
                    ProcessStartInfo psi = new ProcessStartInfo(javaPath)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = minecraftDir,
                        Arguments = string.Join(" ", allArgs.ToArray())
                    };

                    Console.WriteLine($"[NeoForge] 执行 Processor: {jar}");
                    Console.WriteLine($"[NeoForge] 命令行: {javaPath} {psi.Arguments}");

                    using (Process p = Process.Start(psi))
                    {
                        p.OutputDataReceived += (sender, e) => {
                            if (e.Data != null && !ShouldFilterProcessorLine(e.Data))
                                Console.WriteLine("[Processor] " + e.Data);
                        };
                        p.ErrorDataReceived += (sender, e) => {
                            if (e.Data != null && !ShouldFilterProcessorLine(e.Data))
                                Console.WriteLine("[Processor ERR] " + e.Data);
                        };
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        p.WaitForExit();
                        if (p.ExitCode != 0)
                            throw new Exception($"Processor 执行失败，退出码: {p.ExitCode}");
                    }
                }
            }

            // 优化日志输出，去除无用行
            private static bool ShouldFilterProcessorLine(string line)
            {
                if (string.IsNullOrEmpty(line)) return true;
                string trimmed = line.TrimStart();
                return trimmed.StartsWith("asset", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("com", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("data", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("net", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("Can't Find Class", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("Cant Find Class", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("Copying", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("Patching", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("Reading patch", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("Slim", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("Checksum", StringComparison.OrdinalIgnoreCase);
            }

            // 变量替换辅助函数
            private static string ReplaceVariables(string input, JObject dataMap, string minecraftDir, string mcVersion, string neoVersion)
            {
                if (string.IsNullOrEmpty(input)) return input;
                string result = input;

                // 先替换标准变量
                result = result.Replace("{ROOT}", minecraftDir);
                result = result.Replace("{SIDE}", "client");
                string mcJar = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "versions"), mcVersion), mcVersion + ".jar");
                result = result.Replace("{MINECRAFT_JAR}", mcJar);
                result = result.Replace("{LIBRARY_DIR}", Path.Combine(minecraftDir, "libraries"));

                // 替换 dataMap 中的变量
                if (dataMap != null)
                {
                    foreach (var prop in dataMap.Properties())
                    {
                        string key = "{" + prop.Name + "}";
                        JToken value = prop.Value;
                        string replacement = null;
                        if (value is JObject obj)
                        {
                            var clientVal = obj["client"];
                            if (clientVal != null)
                                replacement = clientVal.ToString();
                            else
                            {
                                var first = obj.Properties().FirstOrDefault();
                                if (first != null)
                                    replacement = first.Value.ToString();
                            }
                        }
                        else
                        {
                            replacement = value.ToString();
                        }

                        if (!string.IsNullOrEmpty(replacement))
                        {
                            // 去除可能的方括号
                            string trimmed = replacement.Trim();
                            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                                trimmed = trimmed.Substring(1, trimmed.Length - 2);

                            // 如果包含 ':' 且包含 '@'，说明是带扩展名的 Maven 坐标
                            if (trimmed.Contains(':') && trimmed.Contains('@'))
                            {
                                string[] parts = trimmed.Split(':');
                                if (parts.Length >= 4)
                                {
                                    string group = parts[0].Replace('.', '/');
                                    string artifact = parts[1];
                                    string version = parts[2];
                                    string rest = parts[3]; // 如 "mappings@tsrg.lzma"
                                                            // 解析 rest 中的 @
                                    int atIdx = rest.IndexOf('@');
                                    string classifier = atIdx >= 0 ? rest.Substring(0, atIdx) : rest;
                                    string ext = atIdx >= 0 ? "." + rest.Substring(atIdx + 1) : ".jar";
                                    string fileName = $"{artifact}-{version}-{classifier}{ext}";
                                    string path = $"libraries/{group}/{artifact}/{version}/{fileName}";
                                    // 确保目录存在
                                    string fullDir = Path.Combine(Path.Combine(minecraftDir, "libraries"), Path.GetDirectoryName(path));
                                    if (!Directory.Exists(fullDir))
                                        Directory.CreateDirectory(fullDir);
                                    replacement = path;
                                }
                            }
                            else if (trimmed.Contains(':'))
                            {
                                // 普通 Maven 坐标（无 @）
                                string[] parts = trimmed.Split(':');
                                if (parts.Length >= 3)
                                {
                                    string group = parts[0].Replace('.', '/');
                                    string artifact = parts[1];
                                    string version = parts[2];
                                    string classifier = parts.Length > 3 ? parts[3] : null;
                                    string fileName = $"{artifact}-{version}";
                                    if (!string.IsNullOrEmpty(classifier))
                                        fileName += $"-{classifier}";
                                    fileName += ".jar";
                                    string path = $"libraries/{group}/{artifact}/{version}/{fileName}";
                                    replacement = path;
                                }
                            }
                            // 执行替换
                            result = result.Replace(key, replacement);
                        }
                    }
                }

                // ★★★ 通用转换：如果结果包含 ':' 且未以 "libraries/" 开头，则视为 Maven 坐标，转换为路径 ★★★
                if (result.Contains(':') && !result.StartsWith("libraries/"))
                {
                    string coord = result.Trim('[', ']');
                    if (coord.Contains(':'))
                    {
                        try
                        {
                            string[] parts = coord.Split(':');
                            if (parts.Length >= 3)
                            {
                                string group = parts[0].Replace('.', '/');
                                string artifact = parts[1];
                                string versionPart = parts[2];
                                string classifier = parts.Length > 3 ? parts[3] : null;

                                // ----- 处理版本号中的 @（如 1.21.1-20240808.144430@zip）-----
                                string version = versionPart;
                                string ext = ".jar"; // 默认
                                if (versionPart.Contains('@'))
                                {
                                    int idx = versionPart.IndexOf('@');
                                    version = versionPart.Substring(0, idx);
                                    string extPart = versionPart.Substring(idx + 1);
                                    ext = "." + extPart; // 例如 .zip, .lzma
                                }

                                // ----- 构建文件名 -----
                                string fileName = $"{artifact}-{version}";
                                if (!string.IsNullOrEmpty(classifier))
                                {
                                    // 处理分类器中的 @（如 mappings@tsrg.lzma）
                                    int atIndex = classifier.IndexOf('@');
                                    if (atIndex >= 0)
                                    {
                                        string baseName = classifier.Substring(0, atIndex);
                                        string extension = classifier.Substring(atIndex + 1);
                                        fileName += $"-{baseName}";
                                        ext = "." + extension; // 覆盖扩展名
                                    }
                                    else
                                    {
                                        fileName += $"-{classifier}";
                                        // 扩展名保持原有（已由版本或分类器设定）
                                    }
                                }

                                string path = $"libraries/{group}/{artifact}/{version}/{fileName}{ext}";
                                // 确保目录存在
                                string fullDir = Path.Combine(Path.Combine(minecraftDir, "libraries"), Path.GetDirectoryName(path));
                                if (!Directory.Exists(fullDir))
                                    Directory.CreateDirectory(fullDir);
                                result = path;
                                Console.WriteLine($"[ReplaceVariables] 通用转换: {coord} -> {path}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ReplaceVariables] 通用转换失败: {ex.Message}");
                        }
                    }
                }

                // 如果仍有 @ 且不是路径的一部分，替换为 .（但通常已被处理）
                if (result.Contains('@') && !result.StartsWith("libraries/"))
                    result = result.Replace("@", ".");

                return result;
            }

            private static string MavenCoordsToRelativePath(string coords)
            {
                if (string.IsNullOrEmpty(coords)) return null;
                string[] parts = coords.Split(':');
                if (parts.Length < 3) return null;
                string group = parts[0].Replace('.', '/');
                string artifact = parts[1];
                string version = parts[2];
                string classifier = parts.Length > 3 ? parts[3] : null;
                string fileName = $"{artifact}-{version}";
                if (!string.IsNullOrEmpty(classifier))
                    fileName += $"-{classifier}";
                // 如果分类器不包含 '.'，则添加 .jar（否则保留原扩展名）
                if (string.IsNullOrEmpty(classifier) || !classifier.Contains('.'))
                    fileName += ".jar";
                return $"{group}/{artifact}/{version}/{fileName}";
            }

            private static string GetMainClassFromJar(string jarPath)
            {
                try
                {
                    using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(jarPath))
                    {
                        var entry = zip.GetEntry("META-INF/MANIFEST.MF");
                        if (entry != null)
                        {
                            using (var stream = zip.GetInputStream(entry))
                            using (var reader = new StreamReader(stream))
                            {
                                string content = reader.ReadToEnd();
                                var match = Regex.Match(content, @"Main-Class:\s*(\S+)");
                                if (match.Success)
                                    return match.Groups[1].Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NeoForge] 读取 MANIFEST 失败: {ex.Message}");
                }
                return null;
            }
        }

        public class Launcher : BaseLauncher
        {
            protected override List<string> BuildCommand(LaunchContext context)
            {
                // 启动前修复 JSON
                NeoForgeManager.Installer.FixNeoForgeJsonOnLaunch(context.VersionId, context.MinecraftDir);

                var cmd = new List<string>();
                cmd.Add(JavaResolver.GetJavaPath(context, context.MinecraftDir, context.VersionId));

                var root = LoadVersionJsonWithInheritance(context.VersionId, context.MinecraftDir);
                if (root == null)
                    throw new Exception("无法加载版本 JSON");

                // 确保 arguments 存在
                if (!root.ContainsKey("arguments"))
                    root["arguments"] = new JObject();
                var args = root["arguments"] as JObject;
                if (args != null)
                {
                    if (args["game"] == null) args["game"] = new JArray();
                    if (args["jvm"] == null) args["jvm"] = new JArray();
                }

                string os = MinecraftCore.GetOsName();
                var libs = GetFilteredLibraries(root, context.MinecraftDir, os);
                string nativesDir = PrepareNatives(context, libs);

                // ---- 确定主类 ----
                string mainClass = null;
                if (root["mainClass"] != null)
                    mainClass = root["mainClass"].ToString();

                bool isNeoForge = context.VersionId.IndexOf("neoforge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  context.VersionId.IndexOf("neo forge", StringComparison.OrdinalIgnoreCase) >= 0;
                string neoVer = null;
                bool isNew = false;
                if (isNeoForge)
                {
                    neoVer = Installer.ExtractNeoVersion(context.VersionId);
                    isNew = Installer.IsNewNeoForgeVersion(neoVer);
                    if (string.IsNullOrEmpty(mainClass))
                    {
                        mainClass = isNew ? "net.neoforged.fml.startup.Client" : "cpw.mods.modlauncher.Launcher";
                    }
                    Console.WriteLine($"[启动] 使用主类: {mainClass}");
                }
                else
                {
                    if (string.IsNullOrEmpty(mainClass))
                        mainClass = "net.minecraft.client.main.Main";
                }

                // ---- 通用 JVM 参数 ----
                cmd.Add($"-Xms{context.InitMemory}");
                cmd.Add($"-Xmx{context.MaxMemory}");
                cmd.Add($"-Djava.library.path=\"{nativesDir}\"");
                cmd.Add("-Dfml.environment=client");
                cmd.Add($"-DlibraryDirectory=\"{Path.Combine(context.MinecraftDir, "libraries")}\"");
                cmd.Add("-Dneoforge.logging.mojang.level=OFF");
                cmd.Add("-Dfml.ignorePatchDiscrepancies=true");
                cmd.Add("-Dfml.ignoreInvalidMinecraftCertificates=true");
                cmd.Add("-Djava.net.preferIPv6Addresses=system");
                cmd.Add($"-DignoreList=client-extra,{context.VersionId}.jar");
                cmd.Add($"-DlibraryDirectory=\"{Path.Combine(context.MinecraftDir, "libraries")}\"");
                string libraryDir = Path.Combine(context.MinecraftDir, "libraries");
                AddJvmArgs(cmd, root, os, libraryDir, nativesDir, context);

                // ---- 构建 classpath ----
                string classpath = BuildClasspath(context, libs, null);
                var entries = classpath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (isNeoForge && !isNew)
                {
                    // 从 classpath 中移除 universal.jar，避免模块重复
                    entries.RemoveAll(entry => entry.Contains("libraries") && entry.Contains("neoforge") && entry.Contains("universal.jar"));
                }

                // 获取原版 Minecraft 版本 ID
                string mcVersion = null;
                if (root["minecraftVersion"] != null)
                    mcVersion = root["minecraftVersion"].ToString();
                if (string.IsNullOrEmpty(mcVersion))
                {
                    var match = Regex.Match(context.VersionId, @"(\d+\.\d+\.\d+)");
                    if (match.Success)
                        mcVersion = match.Value;
                    else
                        mcVersion = "1.20.1";
                }
                string originalClientJar = Path.Combine(Path.Combine(Path.Combine(context.MinecraftDir, "versions"), mcVersion), mcVersion + ".jar");

                if (isNew)
                {
                    int removed = entries.RemoveAll(entry => string.Equals(entry.Trim(), originalClientJar, StringComparison.OrdinalIgnoreCase));
                    if (removed > 0)
                        Console.WriteLine($"[启动] 已从 classpath 移除原版 client.jar (新版本)");
                }
                else
                {
                    Console.WriteLine("[启动] 旧版本，保留原版 client.jar");
                }

                // 添加核心 jar（版本目录下的）
                string coreJar = Path.Combine(context.VersionDataPath, context.VersionId + ".jar");
                if (File.Exists(coreJar))
                {
                    entries.RemoveAll(entry => string.Equals(entry.Trim(), coreJar, StringComparison.OrdinalIgnoreCase));
                    entries.Insert(0, coreJar);
                    Console.WriteLine($"[启动] 核心 jar 已置于 classpath 首位: {coreJar}");
                }

                classpath = string.Join(";", entries.ToArray());

                cmd.Add("-cp");
                cmd.Add(classpath);

                cmd.Add(mainClass);

                string assetIndexId = GetAssetIndexId(root, context.MinecraftDir);
                AddGameArgs(cmd, context, root, assetIndexId);

                // 保存修改后的 JSON（可选）
                string versionJsonPath = Path.Combine(context.VersionDataPath, context.VersionId + ".json");
                string jsonOutput = Newtonsoft.Json.JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(versionJsonPath, jsonOutput);

                Console.WriteLine("[启动] 最终 classpath (共 " + entries.Count + " 项):");
                foreach (var entry in entries)
                    Console.WriteLine("  " + entry);
                Console.WriteLine("[启动] 主类: " + mainClass);
                Console.WriteLine("[启动] 完整命令行参数:");
                Console.WriteLine(string.Join(" ", cmd.Select(arg => arg.Contains(" ") ? "\"" + arg + "\"" : arg).ToArray()));

                return cmd;
            }

            private Dictionary<string, object> LoadVersionJsonWithInheritance(string versionId, string minecraftDir)
            {
                var root = LoadVersionJson(versionId, minecraftDir);
                if (root == null) return null;

                if (root.ContainsKey("inheritsFrom"))
                {
                    string parentId = root["inheritsFrom"].ToString();
                    var parentRoot = LoadVersionJsonWithInheritance(parentId, minecraftDir);
                    if (parentRoot != null)
                    {
                        var libraries = root["libraries"] as IList ?? new List<object>();
                        var parentLibraries = parentRoot["libraries"] as IList ?? new List<object>();
                        foreach (var lib in parentLibraries) 
                        {
                            // 去重添加
                            if (!libraries.Contains(lib))
                                libraries.Add(lib);
                        }
                        root["libraries"] = libraries;
                    }
                }
                return root;
            }
        }
    }
}