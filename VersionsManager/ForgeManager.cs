using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml;

namespace New_Launcher
{
    public static class ForgeManager
    {
        public static class Installer
        {
            private static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            private static string SettingsDir = Path.Combine(LauncherPath, "Launcher Setting");
            private static string MinecraftDir = Path.Combine(LauncherPath, ".minecraft");

            // 镜像源顺序：优先 Minecraft 官方库，其次 BMCLAPI，然后 Forge Maven，最后 Maven Central
            private static readonly string[] MavenMirrors = new string[]
            {
                "https://libraries.minecraft.net/",
                "https://bmclapi2.bangbang93.com/maven/",
                "https://maven.minecraftforge.net/",
                "https://repo1.maven.org/maven2/"
            };
            private static readonly string OFFICIAL_MAVEN = "https://maven.minecraftforge.net/";

            private static readonly Version MinGameVersion = new Version(1, 1, 0);
            private static readonly Version MaxGameVersion = new Version(999, 999, 999);

            /// <summary>
            /// 可配置的 Java 可执行文件路径（默认为 "java"，即系统 PATH 中的 Java）
            /// </summary>
            public static string JavaExecutable { get; set; } = "java";

            /// <summary>
            /// 安装 Forge（主入口）
            /// </summary>
            public static string InstallForge(string gameVersion, string forgeVersion, bool isLaunch = false)
            {
                if (string.IsNullOrEmpty(forgeVersion))
                {
                    forgeVersion = ScanForgeVersionFromLibraries(gameVersion);
                    if (string.IsNullOrEmpty(forgeVersion))
                        throw new ArgumentException($"无法自动检测到 {gameVersion} 的 Forge 版本，请手动指定。");
                }

                if (!IsGameVersionSupported(gameVersion, out string err))
                    throw new ArgumentException(err);

                string minecraftDir = MinecraftCore.GetMinecraftDir();

                // ★★★ 启动模式：快速检查完整性，若完整则直接返回 ★★★
                if (isLaunch)
                {
                    // 尝试查找已安装的版本目录
                    string detected = DetectInstalledVersion(minecraftDir, gameVersion, forgeVersion);
                    if (!string.IsNullOrEmpty(detected) && CheckForgeIntegrity(detected))
                    {
                        Console.WriteLine("[启动] Forge 文件完整，跳过下载");
                        return detected;
                    }
                    Console.WriteLine("[启动] Forge 文件不完整，执行完整安装...");
                    // 若检查不通过，继续执行下面的下载安装流程
                }

                string installerCacheDir = GetInstallerCacheDir();
                string installerPath = Path.Combine(installerCacheDir, $"forge-{forgeVersion}-installer.jar");
                string officialBackupPath = installerPath + ".official";

                Console.WriteLine($"[Forge] 准备下载安装器（镜像优先，同时后台拉取官方源）...");

                // ---- 1. 并行下载：镜像源 + 官方源（后台） ----
                DownloadResult mirrorResult = null;
                Exception mirrorException = null;
                bool mirrorDone = false;
                bool officialDone = false;
                Exception officialException = null;
                ManualResetEvent mirrorEvent = new ManualResetEvent(false);
                ManualResetEvent officialEvent = new ManualResetEvent(false);

                // 线程1：镜像下载（优先）
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        mirrorResult = DownloadInstallerFromMirror(installerPath, gameVersion, forgeVersion);
                    }
                    catch (Exception ex)
                    {
                        mirrorException = ex;
                    }
                    finally
                    {
                        mirrorDone = true;
                        mirrorEvent.Set();
                    }
                });

                // 线程2：官方源下载（备份）
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        DownloadInstallerFromOfficial(officialBackupPath, forgeVersion);
                    }
                    catch (Exception ex)
                    {
                        officialException = ex;
                    }
                    finally
                    {
                        officialDone = true;
                        officialEvent.Set();
                    }
                });

                // 等待镜像下载完成（最多60秒）
                if (!mirrorEvent.WaitOne(60000))
                    throw new Exception("镜像下载超时，请检查网络。");

                // 若镜像失败，尝试使用官方备份
                if (mirrorException != null || mirrorResult == null || !mirrorResult.Success)
                {
                    Console.WriteLine("[Forge] 镜像下载失败，等待官方源备份...");
                    officialEvent.WaitOne();
                    if (officialDone && File.Exists(officialBackupPath) && new FileInfo(officialBackupPath).Length > 0)
                    {
                        File.Copy(officialBackupPath, installerPath, true);
                        Console.WriteLine("[Forge] 使用官方源备份文件进行安装");
                    }
                    else
                    {
                        throw new Exception($"无法从任何源下载 Forge {forgeVersion} 安装器。");
                    }
                }
                else
                {
                    Console.WriteLine("[Forge] 镜像下载成功，开始安装...");
                }

                // ========== 预下载所有库（并行多线程） ==========
                PreDownloadLibraries(installerPath, minecraftDir, gameVersion, forgeVersion);

                // ---- 2. 执行官方安装器 ----
                bool installSuccess = RunForgeInstaller(installerPath, minecraftDir);

                // 若安装失败且检测到 SHA1 校验错误，则尝试使用官方备份
                if (!installSuccess && IsSha1ErrorFromLastRun())
                {
                    Console.WriteLine("[Forge] 检测到 SHA1 校验失败，可能镜像文件过期，尝试使用官方备份...");
                    if (!officialDone)
                    {
                        Console.WriteLine("[Forge] 等待官方源备份下载完成...");
                        officialEvent.WaitOne();
                    }
                    if (File.Exists(officialBackupPath) && new FileInfo(officialBackupPath).Length > 0)
                    {
                        File.Copy(officialBackupPath, installerPath, true);
                        Console.WriteLine("[Forge] 已替换为官方源文件，重新安装...");
                        installSuccess = RunForgeInstaller(installerPath, minecraftDir);
                    }
                    else
                    {
                        throw new Exception("官方源备份文件不存在，无法重试。");
                    }
                }

                // ---- 3. 如果官方安装器仍然失败，则启用拆包安装作为最终后备 ----
                if (!installSuccess)
                {
                    Console.WriteLine("[Forge] 官方安装器安装失败，尝试拆包安装（Unpacked Install）...");
                    try
                    {
                        installSuccess = InstallForgeUnpacked(installerPath, minecraftDir, gameVersion, forgeVersion);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Forge] 拆包安装异常: {ex.Message}");
                        installSuccess = false;
                    }
                }

                if (!installSuccess)
                {
                    // 抛出异常，附带错误日志
                    string logFile = SaveLogToFile(_lastInstallLog);
                    string errorSummary = ExtractErrorSummary(_lastInstallLog);
                    throw new Exception($"Forge 安装失败。\n错误摘要：{errorSummary}\n完整日志已保存至：{logFile}");
                }

                // ---- 4. 检测版本目录，确保核心 jar 存在 ----
                string versionName = DetectInstalledVersion(minecraftDir, gameVersion, forgeVersion);
                Console.WriteLine($"[Forge] 检测到版本目录: {versionName}");

                string versionFolder = Path.Combine(Path.Combine(minecraftDir, "versions"), versionName);
                Directory.CreateDirectory(versionFolder);
                string targetJar = Path.Combine(versionFolder, $"{versionName}.jar");
                string versionJsonPath = Path.Combine(versionFolder, $"{versionName}.json");

                // ========== 核心 jar 查找 / 下载 ==========
                string sourceJar = null;

                // 1. 优先从版本目录中查找已有的 client.jar
                string clientJarPath = Path.Combine(versionFolder, $"{versionName}-client.jar");
                if (File.Exists(clientJarPath))
                    sourceJar = clientJarPath;
                else
                {
                    // 尝试版本目录中已有的其他 jar（排除目标 jar 本身）
                    var jarFiles = Directory.GetFiles(versionFolder, "*.jar");
                    foreach (var f in jarFiles)
                    {
                        if (!Path.GetFileName(f).Equals(Path.GetFileName(targetJar), StringComparison.OrdinalIgnoreCase))
                        {
                            sourceJar = f;
                            break;
                        }
                    }
                }

                // 2. 如果还没有，尝试从继承版本复制
                if (sourceJar == null && File.Exists(versionJsonPath))
                {
                    try
                    {
                        var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(versionJsonPath));
                        if (json.ContainsKey("inheritsFrom"))
                        {
                            string parentVersion = json["inheritsFrom"].ToString();
                            string parentFolder = Path.Combine(Path.Combine(minecraftDir, "versions"), parentVersion);
                            string parentJar = Path.Combine(parentFolder, $"{parentVersion}.jar");
                            if (File.Exists(parentJar))
                            {
                                sourceJar = parentJar;
                                Console.WriteLine($"[Forge] 将从继承版本 '{parentVersion}' 复制核心 JAR");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Forge] 读取版本 JSON 失败: {ex.Message}");
                    }
                }

                // 3. 新增：如果仍然没有，尝试从安装器解压的 maven 目录获取
                if (sourceJar == null && File.Exists(installerPath))
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), $"forge_core_{Guid.NewGuid():N}");
                    try
                    {
                        Directory.CreateDirectory(tempDir);
                        using (ZipFile zip = new ZipFile(installerPath))
                        {
                            foreach (ZipEntry entry in zip)
                            {
                                if (entry.IsDirectory) continue;
                                string target = Path.Combine(tempDir, entry.Name.Replace('/', Path.DirectorySeparatorChar));
                                string targetDir = Path.GetDirectoryName(target);
                                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                                using (Stream s = zip.GetInputStream(entry))
                                using (FileStream fs = File.Create(target))
                                {
                                    byte[] buffer = new byte[8192];
                                    int read;
                                    while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                                        fs.Write(buffer, 0, read);
                                }
                            }
                        }

                        // 在 maven/net/minecraftforge/forge/{forgeVersion}/ 下查找 universal 或 client jar
                        string forgeMavenPath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(
                            tempDir, "maven"), "net"), "minecraftforge"), "forge");
                        string forgeSubDir = Path.Combine(forgeMavenPath, forgeVersion);
                        if (Directory.Exists(forgeSubDir))
                        {
                            var jarCandidates = Directory.GetFiles(forgeSubDir, "*.jar")
                                .Where(f => f.Contains("universal") || f.Contains("client"))
                                .OrderByDescending(f => new FileInfo(f).Length)
                                .ToList();
                            if (jarCandidates.Count > 0)
                            {
                                sourceJar = jarCandidates.First();
                                Console.WriteLine($"[Forge] 从安装器 maven 目录找到核心 jar: {Path.GetFileName(sourceJar)}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Forge] 从安装器提取核心 jar 失败: {ex.Message}");
                    }
                    finally
                    {
                        try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
                    }
                }

                // 4. 新增：如果还是没有，尝试从网络下载 universal jar
                if (sourceJar == null)
                {
                    Console.WriteLine("[Forge] 尝试从 Maven 下载 universal jar...");
                    string universalFileName = $"forge-{forgeVersion}-universal.jar";
                    string downloadUrl = OFFICIAL_MAVEN + $"net/minecraftforge/forge/{forgeVersion}/{universalFileName}";
                    string tempUniversal = Path.Combine(Path.GetTempPath(), universalFileName);
                    try
                    {
                        if (TryDownloadFile(downloadUrl, tempUniversal, 60000))
                        {
                            sourceJar = tempUniversal;
                            Console.WriteLine($"[Forge] 成功下载 universal jar: {downloadUrl}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Forge] 下载 universal jar 失败: {ex.Message}");
                    }
                }

                // 最终判断
                if (sourceJar != null && File.Exists(sourceJar))
                {
                    File.Copy(sourceJar, targetJar, true);
                    Console.WriteLine($"[Forge] 已复制核心 jar: {targetJar}");
                }
                else
                {
                    throw new Exception($"无法获取 Forge 核心 jar，版本目录 {versionFolder} 中未找到可用的 jar，且无法从任何源下载。");
                }

                // ---- 合并 JSON，生成独立版本 ----
                if (File.Exists(versionJsonPath))
                {
                    VersionJsonMerger.MergeAndSaveIndependentJson(versionJsonPath, minecraftDir);
                }
                else
                {
                    Console.WriteLine($"[Forge] 警告：未找到版本 JSON 文件 {versionJsonPath}，跳过合并");
                }

                // 清理备份文件
                try
                {
                    if (File.Exists(officialBackupPath))
                        File.Delete(officialBackupPath);
                }
                catch (IOException)
                {
                    Console.WriteLine("[Forge] 无法删除备份文件，可能仍被占用，忽略。");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("[Forge] 无权删除备份文件，忽略。");
                }

                Console.WriteLine($"[Forge] 安装完成，版本目录: {versionName}");
                return versionName;
            }

            /// <summary>
            /// 从已下载的 libraries 目录中扫描最新 Forge 版本
            /// </summary>
            private static string ScanForgeVersionFromLibraries(string gameVersion)
            {
                string forgeDir = Path.Combine(Path.Combine(Path.Combine(Path.Combine(MinecraftDir, "libraries"), "net"), "minecraftforge"), "forge");
                if (!Directory.Exists(forgeDir)) return null;
                var dirs = Directory.GetDirectories(forgeDir);
                if (dirs.Length == 0) return null;

                // 安全排序，降序取最新
                Array.Sort(dirs, (a, b) =>
                {
                    string va = Path.GetFileName(a) ?? "";
                    string vb = Path.GetFileName(b) ?? "";
                    try
                    {
                        Version vA = new Version(va);
                        Version vB = new Version(vb);
                        return vB.CompareTo(vA);
                    }
                    catch
                    {
                        return string.Compare(vb, va, StringComparison.Ordinal);
                    }
                });

                return Path.GetFileName(dirs[0]);
            }

            // ==================== 预下载所有库（并行多线程） ====================
            private static void PreDownloadLibraries(string installerPath, string minecraftDir, string gameVersion, string forgeVersion)
            {
                string tempDir = Path.Combine(Path.GetTempPath(), $"forge_predownload_{Guid.NewGuid():N}");
                try
                {
                    Directory.CreateDirectory(tempDir);
                    Console.WriteLine("[Forge] 解压安装器以提取库列表...");

                    // 解压安装器
                    using (ZipFile zip = new ZipFile(installerPath))
                    {
                        foreach (ZipEntry entry in zip)
                        {
                            if (entry.IsDirectory) continue;
                            string target = Path.Combine(tempDir, entry.Name.Replace('/', Path.DirectorySeparatorChar));
                            string targetDir = Path.GetDirectoryName(target);
                            if (!Directory.Exists(targetDir))
                                Directory.CreateDirectory(targetDir);
                            using (Stream s = zip.GetInputStream(entry))
                            using (FileStream fs = File.Create(target))
                            {
                                byte[] buffer = new byte[8192];
                                int read;
                                while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                                    fs.Write(buffer, 0, read);
                            }
                        }
                    }

                    string profilePath = Path.Combine(tempDir, "install_profile.json");
                    if (!File.Exists(profilePath))
                    {
                        Console.WriteLine("[Forge] 未找到 install_profile.json，无法预下载库");
                        return;
                    }
                    var profile = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(profilePath));

                    Dictionary<string, Dictionary<string, object>> libMap = new Dictionary<string, Dictionary<string, object>>();

                    void AddLibraries(ArrayList list)
                    {
                        foreach (var item in list)
                        {
                            var dict = item as Dictionary<string, object>;
                            if (dict == null || !dict.ContainsKey("name")) continue;
                            string name = dict["name"].ToString();
                            if (!libMap.ContainsKey(name))
                                libMap[name] = new Dictionary<string, object>();
                            if (dict.ContainsKey("downloads"))
                                libMap[name]["downloads"] = dict["downloads"];
                            if (dict.ContainsKey("url"))
                                libMap[name]["url"] = dict["url"];
                        }
                    }

                    // ★★★ 先填充 libMap ★★★
                    if (profile.ContainsKey("libraries") && profile["libraries"] is ArrayList profileLibs)
                        AddLibraries(profileLibs);

                    string versionJsonPath = Path.Combine(tempDir, "version.json");
                    if (File.Exists(versionJsonPath))
                    {
                        var version = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(versionJsonPath));
                        if (version.ContainsKey("libraries") && version["libraries"] is ArrayList verLibs)
                            AddLibraries(verLibs);
                    }

                    // ★★★ 现在 libMap 已包含所有库，为 lzma 补充 downloads ★★★
                    if (libMap.ContainsKey("lzma:lzma:0.0.1"))
                    {
                        var lzmaInfo = libMap["lzma:lzma:0.0.1"];
                        if (!lzmaInfo.ContainsKey("downloads"))
                        {
                            var artifact = new Dictionary<string, object>();
                            artifact["url"] = "https://libraries.minecraft.net/lzma/lzma/0.0.1/lzma-0.0.1.jar";
                            artifact["path"] = "lzma/lzma/0.0.1/lzma-0.0.1.jar";
                            var downloads = new Dictionary<string, object>();
                            downloads["artifact"] = artifact;
                            lzmaInfo["downloads"] = downloads;
                            Console.WriteLine("[Forge] ✅ 为 lzma 补充下载信息");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[Forge] ⚠️ 未在 libMap 中找到 lzma，请检查 install_profile.json");
                    }

                    // 复制 maven 目录下的库（安装器自带的）
                    string mavenSrc = Path.Combine(tempDir, "maven");
                    if (Directory.Exists(mavenSrc))
                    {
                        string libDir = Path.Combine(minecraftDir, "libraries");
                        CopyMavenFiles(mavenSrc, libDir);
                    }

                    // ===================== 多线程并行下载 =====================
                    // 先收集需要下载的库列表
                    var needDownload = new List<KeyValuePair<string, Dictionary<string, object>>>();
                    foreach (var kv in libMap)
                    {
                        string name = kv.Key;
                        var libInfo = kv.Value;
                        var lib = new Library(name);
                        bool exists = LibraryExists(lib, minecraftDir);
                        Console.WriteLine($"[Forge] 检查库: {name} -> {(exists ? "已存在" : "缺失")}");
                        if (!exists)
                        {
                            needDownload.Add(kv);
                        }
                    }

                    if (needDownload.Count == 0)
                    {
                        Console.WriteLine("[Forge] 所有库已存在，无需下载");
                        return;
                    }

                    Console.WriteLine($"[Forge] 共 {needDownload.Count} 个缺失库，开始并行下载...");

                    // 控制并发数（避免同时开太多线程消耗网络资源，建议 8~10）
                    const int MAX_CONCURRENT = 10;
                    Semaphore semaphore = new Semaphore(MAX_CONCURRENT, MAX_CONCURRENT);

                    int totalCount = needDownload.Count;
                    int completedCount = 0;
                    int successCount = 0;
                    int failCount = 0;
                    object lockObj = new object();
                    List<WaitHandle> waitHandles = new List<WaitHandle>();

                    foreach (var kv in needDownload)
                    {
                        string name = kv.Key;
                        var libInfo = kv.Value;
                        var lib = new Library(name);

                        string downloadUrl = null;
                        if (libInfo.ContainsKey("downloads"))
                        {
                            var downloads = libInfo["downloads"] as Dictionary<string, object>;
                            if (downloads != null && downloads.ContainsKey("artifact"))
                            {
                                var artifact = downloads["artifact"] as Dictionary<string, object>;
                                if (artifact != null && artifact.ContainsKey("url"))
                                    downloadUrl = artifact["url"].ToString();
                            }
                        }
                        else if (libInfo.ContainsKey("url"))
                        {
                            downloadUrl = libInfo["url"].ToString();
                        }

                        // 创建等待句柄（用于主线程等待所有任务完成）
                        ManualResetEvent doneEvent = new ManualResetEvent(false);
                        waitHandles.Add(doneEvent);

                        // 排队到线程池
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                semaphore.WaitOne(); // 等待可用槽位
                                try
                                {
                                    DownloadLibrary(lib, minecraftDir, downloadUrl);
                                    lock (lockObj)
                                    {
                                        successCount++;
                                        Console.WriteLine($"[Forge] ✅ 下载成功 [{successCount}/{totalCount}]: {name}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    lock (lockObj)
                                    {
                                        failCount++;
                                        Console.WriteLine($"[Forge] ❌ 下载失败 [{failCount}/{totalCount}]: {name} - {ex.Message}");
                                    }
                                }
                                finally
                                {
                                    semaphore.Release(); // 释放槽位
                                }
                            }
                            finally
                            {
                                Interlocked.Increment(ref completedCount);
                                doneEvent.Set();
                            }
                        });
                    }

                    // 等待所有任务完成
                    Console.WriteLine("[Forge] 等待所有下载任务完成...");
                    foreach (var wh in waitHandles)
                    {
                        wh.WaitOne();
                        wh.Close(); // 释放句柄
                    }

                    Console.WriteLine($"[Forge] 下载完成！成功: {successCount}，失败: {failCount}");
                    if (failCount > 0)
                    {
                        Console.WriteLine("[Forge] 警告：部分库下载失败，游戏可能无法启动，请检查网络或手动补充缺失库。");
                    }
                }
                finally
                {
                    try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
                    catch { }
                }
            }

            // ------------------ 库下载辅助类与方法 ------------------
            private class Library
            {
                public string Group { get; }
                public string Artifact { get; }
                public string Version { get; }
                public string Classifier { get; }
                public string Name { get; }

                public Library(string name)
                {
                    Name = name;
                    var parts = name.Split(':');
                    if (parts.Length >= 3)
                    {
                        Group = parts[0];
                        Artifact = parts[1];
                        Version = parts[2];
                        Classifier = parts.Length > 3 ? parts[3] : null;
                    }
                }

                public string GetJarFileName()
                {
                    string baseName = $"{Artifact}-{Version}";
                    if (!string.IsNullOrEmpty(Classifier))
                        baseName += $"-{Classifier}";
                    return baseName + ".jar";
                }

                public string GetPathInLibraries()
                {
                    string groupPath = Group.Replace('.', Path.DirectorySeparatorChar);
                    return Path.Combine(Path.Combine(Path.Combine(groupPath, Artifact), Version), GetJarFileName());
                }
            }

            private static bool LibraryExists(Library lib, string minecraftDir)
            {
                string libPath = Path.Combine(Path.Combine(minecraftDir, "libraries"), lib.GetPathInLibraries());
                return File.Exists(libPath) && new FileInfo(libPath).Length > 0;
            }

            private static void DownloadLibrary(Library lib, string minecraftDir, string explicitUrl = null)
            {
                string relativePath = lib.GetPathInLibraries();
                string fullPath = Path.Combine(Path.Combine(minecraftDir, "libraries"), relativePath);
                string dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 0)
                    return;

                if (!string.IsNullOrEmpty(explicitUrl))
                {
                    if (TryDownloadFile(explicitUrl, fullPath, 60000))
                        return;
                    // 显式 URL 失败，继续尝试镜像，但不再静默
                }

                // 从镜像尝试下载
                string mavenPath = $"{lib.Group.Replace('.', '/')}/{lib.Artifact}/{lib.Version}/{lib.GetJarFileName()}";
                foreach (string mirror in MavenMirrors)
                {
                    string url = mirror + mavenPath;
                    if (TryDownloadFile(url, fullPath, 30000))
                        return;
                }

                // ★★★ 如果所有尝试失败，抛出异常 ★★★
                throw new Exception($"无法下载库 {lib.Name}，尝试过路径: {mavenPath}");
            }

            // ==================== 拆包安装 ====================
            private static bool InstallForgeUnpacked(string installerPath, string minecraftDir, string gameVersion, string forgeVersion)
            {
                string tempDir = Path.Combine(Path.GetTempPath(), $"forge_unpack_{Guid.NewGuid():N}");
                try
                {
                    Directory.CreateDirectory(tempDir);
                    Console.WriteLine($"[Forge Unpack] 解压安装器到: {tempDir}");

                    // ---- 1. 解压安装包 ----
                    using (ZipFile zipFile = new ZipFile(installerPath))
                    {
                        foreach (ZipEntry entry in zipFile)
                        {
                            if (entry.IsDirectory) continue;
                            string entryName = entry.Name.Replace('/', Path.DirectorySeparatorChar);
                            string targetPath = Path.Combine(tempDir, entryName);
                            string targetDir = Path.GetDirectoryName(targetPath);
                            if (!Directory.Exists(targetDir))
                                Directory.CreateDirectory(targetDir);

                            using (Stream zipStream = zipFile.GetInputStream(entry))
                            using (FileStream fileStream = File.Create(targetPath))
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                                    fileStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }

                    // ---- 2. 读取 install_profile.json ----
                    string profilePath = Path.Combine(tempDir, "install_profile.json");
                    if (!File.Exists(profilePath))
                    {
                        Console.WriteLine("[Forge Unpack] 未找到 install_profile.json，拆包失败");
                        return false;
                    }
                    var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(profilePath));
                    if (!json.ContainsKey("versionInfo"))
                    {
                        Console.WriteLine("[Forge Unpack] install_profile.json 缺少 versionInfo");
                        return false;
                    }
                    var versionInfo = json["versionInfo"] as Dictionary<string, object>;

                    // ---- 3. 确定版本名称 ----
                    string versionName = $"{gameVersion}-forge-{forgeVersion}";
                    string versionFolder = Path.Combine(Path.Combine(minecraftDir, "versions"), versionName);
                    Directory.CreateDirectory(versionFolder);

                    // ---- 4. 查找核心 jar（兼容老版本） ----
                    string sourceJar = null;
                    string mavenRoot = Path.Combine(tempDir, "maven");
                    if (Directory.Exists(mavenRoot))
                    {
                        // 4a. 尝试在 maven/net/minecraftforge/forge/{forgeVersion}/ 下查找
                        string forgeMavenPath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(
                            mavenRoot, "net"), "minecraftforge"), "forge"), forgeVersion);
                        if (Directory.Exists(forgeMavenPath))
                        {
                            var jarFiles = Directory.GetFiles(forgeMavenPath, "*.jar");
                            // 优先选择 universal 或 client 或 最大的 jar
                            var candidate = jarFiles.FirstOrDefault(f => f.Contains("universal")) ??
                                            jarFiles.FirstOrDefault(f => f.Contains("client")) ??
                                            jarFiles.OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();
                            if (candidate != null)
                            {
                                sourceJar = candidate;
                                Console.WriteLine($"[Forge Unpack] 在 maven 目录找到核心 jar: {Path.GetFileName(sourceJar)}");
                            }
                        }

                        // 4b. 如果没找到，递归搜索整个 maven 目录（兼容其他结构）
                        if (sourceJar == null)
                        {
                            var allJars = Directory.GetFiles(mavenRoot, "*.jar", SearchOption.AllDirectories);
                            // 排除 installer 和可能是依赖的 jar
                            var candidates = allJars.Where(f =>
                                !Path.GetFileName(f).Contains("installer") &&
                                !Path.GetFileName(f).Contains("maven-metadata") &&
                                !Path.GetFileName(f).Contains("source") &&
                                !Path.GetFileName(f).Contains("javadoc")
                            ).ToList();
                            if (candidates.Count > 0)
                            {
                                // 优先 universal，其次 client，最后最大的
                                sourceJar = candidates.FirstOrDefault(f => f.Contains("universal")) ??
                                            candidates.FirstOrDefault(f => f.Contains("client")) ??
                                            candidates.OrderByDescending(f => new FileInfo(f).Length).First();
                                Console.WriteLine($"[Forge Unpack] 在 maven 子目录找到核心 jar: {Path.GetFileName(sourceJar)}");
                            }
                        }
                    }

                    // 4c. 如果仍未找到，尝试从安装包根目录查找（某些老版本直接把 jar 放根目录）
                    if (sourceJar == null)
                    {
                        var rootJars = Directory.GetFiles(tempDir, "*.jar");
                        var candidates = rootJars.Where(f =>
                            !Path.GetFileName(f).Contains("installer") &&
                            !Path.GetFileName(f).Contains("maven-metadata") &&
                            !Path.GetFileName(f).Contains("source") &&
                            !Path.GetFileName(f).Contains("javadoc") &&
                            (Path.GetFileName(f).Contains("forge") || Path.GetFileName(f).Contains("minecraft"))
                        ).ToList();
                        if (candidates.Count > 0)
                        {
                            sourceJar = candidates.FirstOrDefault(f => f.Contains("universal")) ??
                                        candidates.FirstOrDefault(f => f.Contains("client")) ??
                                        candidates.OrderByDescending(f => new FileInfo(f).Length).First();
                            Console.WriteLine($"[Forge Unpack] 在安装包根目录找到核心 jar: {Path.GetFileName(sourceJar)}");
                        }
                    }

                    if (string.IsNullOrEmpty(sourceJar))
                    {
                        Console.WriteLine("[Forge Unpack] 未找到任何可用的核心 jar，拆包失败");
                        return false;
                    }

                    // ---- 5. 复制核心 jar 到版本目录 ----
                    string targetJar = Path.Combine(versionFolder, $"{versionName}.jar");
                    File.Copy(sourceJar, targetJar, true);
                    Console.WriteLine($"[Forge Unpack] 已复制核心 JAR: {targetJar}");

                    // ---- 6. 生成版本 JSON ----
                    string versionJsonPath = Path.Combine(versionFolder, $"{versionName}.json");
                    Dictionary<string, object> finalJson;

                    string installerVersionJson = Path.Combine(tempDir, "version.json");
                    if (File.Exists(installerVersionJson))
                    {
                        finalJson = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(installerVersionJson));
                    }
                    else
                    {
                        finalJson = new Dictionary<string, object>(versionInfo);
                    }

                    // 强制覆盖 id 和 inheritsFrom
                    finalJson["id"] = versionName;
                    finalJson["inheritsFrom"] = gameVersion;

                    // 确保 mainClass 存在
                    if (versionInfo.ContainsKey("mainClass"))
                        finalJson["mainClass"] = versionInfo["mainClass"];
                    else if (!finalJson.ContainsKey("mainClass"))
                        finalJson["mainClass"] = "net.minecraftforge.fml.loading.FMLClientLauncher";

                    // ★★★ 合并 logging 字段（如果存在）★★★
                    if (json.ContainsKey("logging") && !finalJson.ContainsKey("logging"))
                        finalJson["logging"] = json["logging"];

                    // ★★★ 合并 arguments（从 install_profile.json）★★★
                    if (json.ContainsKey("arguments"))
                    {
                        var installArgs = json["arguments"] as Dictionary<string, object>;
                        if (!finalJson.ContainsKey("arguments"))
                            finalJson["arguments"] = new Dictionary<string, object>();
                        var finalArgs = finalJson["arguments"] as Dictionary<string, object>;
                        if (installArgs != null && finalArgs != null)
                        {
                            foreach (var kv in installArgs)
                            {
                                if (!finalArgs.ContainsKey(kv.Key))
                                    finalArgs[kv.Key] = kv.Value;
                            }
                        }
                    }

                    // 合并 libraries（来自 install_profile.json）
                    if (json.ContainsKey("libraries"))
                    {
                        var installLibs = json["libraries"] as ArrayList;
                        if (installLibs != null)
                        {
                            if (!finalJson.ContainsKey("libraries"))
                                finalJson["libraries"] = new ArrayList();
                            var finalLibs = finalJson["libraries"] as ArrayList;
                            foreach (var lib in installLibs)
                            {
                                var libObj = lib as Dictionary<string, object>;
                                if (libObj != null && libObj.ContainsKey("name"))
                                {
                                    bool exists = false;
                                    foreach (var existing in finalLibs)
                                    {
                                        var existDict = existing as Dictionary<string, object>;
                                        if (existDict != null && existDict.ContainsKey("name") && existDict["name"].ToString() == libObj["name"].ToString())
                                        {
                                            exists = true;
                                            break;
                                        }
                                    }
                                    if (!exists)
                                        finalLibs.Add(lib);
                                }
                            }
                        }
                    }

                    // 合并 arguments
                    if (json.ContainsKey("arguments") && !finalJson.ContainsKey("arguments"))
                        finalJson["arguments"] = json["arguments"];

                    string jsonOutput = new JavaScriptSerializer().Serialize(finalJson);
                    File.WriteAllText(versionJsonPath, jsonOutput, Encoding.UTF8);
                    Console.WriteLine($"[Forge Unpack] 已生成版本 JSON: {versionJsonPath}");

                    // ---- 7. 复制 maven 库文件到 libraries 目录 ----
                    if (Directory.Exists(mavenRoot))
                    {
                        string librariesDir = Path.Combine(minecraftDir, "libraries");
                        CopyMavenFiles(mavenRoot, librariesDir);
                        Console.WriteLine("[Forge Unpack] 已复制库文件");
                    }

                    // ---- 8. 补充下载缺失的库（旧版 Forge 特有） ----
                    try
                    {
                        if (finalJson.ContainsKey("libraries"))
                        {
                            var finalLibs = finalJson["libraries"] as ArrayList;
                            if (finalLibs != null)
                            {
                                string libDir = Path.Combine(minecraftDir, "libraries");
                                foreach (var libObj in finalLibs)
                                {
                                    var libDict = libObj as Dictionary<string, object>;
                                    if (libDict == null || !libDict.ContainsKey("name"))
                                        continue;

                                    string name = libDict["name"].ToString();
                                    var parts = name.Split(':');
                                    if (parts.Length < 3) continue;

                                    string group = parts[0].Replace('.', '/');
                                    string artifact = parts[1];
                                    string version = parts[2];
                                    string classifier = parts.Length >= 4 ? parts[3] : null;

                                    // 跳过 natives
                                    if (classifier != null && classifier.StartsWith("natives-"))
                                        continue;

                                    // 跳过 Forge 自身核心库（已经复制了 universal.jar）
                                    if (name.StartsWith("net.minecraftforge:forge:"))
                                        continue;

                                    string jarName = $"{artifact}-{version}";
                                    if (!string.IsNullOrEmpty(classifier))
                                        jarName += $"-{classifier}";
                                    jarName += ".jar";

                                    string relPath = $"{group}/{artifact}/{version}/{jarName}";
                                    string fullPath = Path.Combine(libDir, relPath.Replace('/', Path.DirectorySeparatorChar));

                                    if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 0)
                                        continue;

                                    // 1. 如果 libDict 有顶层 url，则优先使用（旧版 Forge 常用）
                                    string downloadUrl = null;
                                    if (libDict.ContainsKey("url"))
                                    {
                                        string baseUrl = libDict["url"].ToString();
                                        if (!baseUrl.EndsWith("/"))
                                            baseUrl += "/";
                                        downloadUrl = baseUrl + relPath;
                                    }
                                    // 2. 如果有 downloads.artifact.url，则使用
                                    else if (libDict.ContainsKey("downloads"))
                                    {
                                        var downloads = libDict["downloads"] as Dictionary<string, object>;
                                        if (downloads != null && downloads.ContainsKey("artifact"))
                                        {
                                            var artifactDict = downloads["artifact"] as Dictionary<string, object>;
                                            if (artifactDict != null && artifactDict.ContainsKey("url"))
                                                downloadUrl = artifactDict["url"].ToString();
                                        }
                                    }

                                    // 3. 若仍无 URL，则从镜像列表尝试
                                    bool downloaded = false;
                                    if (!string.IsNullOrEmpty(downloadUrl))
                                    {
                                        try
                                        {
                                            Console.WriteLine($"[Forge Unpack] 下载缺失库: {relPath} (从 {downloadUrl})");
                                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                                            MinecraftCore.DownloadFile(downloadUrl, fullPath, null);
                                            downloaded = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[Forge Unpack] 从指定 URL 下载失败: {ex.Message}");
                                            // 继续尝试镜像
                                        }
                                    }

                                    if (!downloaded)
                                    {
                                        // 从 MavenMirrors 尝试
                                        foreach (string mirror in MavenMirrors)
                                        {
                                            string url = mirror + relPath;
                                            try
                                            {
                                                Console.WriteLine($"[Forge Unpack] 尝试镜像下载: {url}");
                                                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                                                MinecraftCore.DownloadFile(url, fullPath, null);
                                                downloaded = true;
                                                break;
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine($"[Forge Unpack] 镜像下载失败: {ex.Message}");
                                            }
                                        }
                                    }

                                    if (!downloaded)
                                    {
                                        Console.WriteLine($"[Forge Unpack] 所有来源均无法下载库: {relPath}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Forge Unpack] 补充下载库时出错: {ex.Message}");
                        // 不中断安装
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Forge Unpack] 拆包安装异常: {ex.Message}");
                    return false;
                }
                finally
                {
                    try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
                    catch { }
                }
            }

            // 复制 Maven 文件
            private static void CopyMavenFiles(string srcDir, string destLibDir)
            {
                foreach (string dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = GetRelativePath(srcDir, dir);
                    string targetDir = Path.Combine(destLibDir, relativePath);
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    foreach (string file in Directory.GetFiles(dir))
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(targetDir, fileName);
                        if (!File.Exists(destFile))
                        {
                            File.Copy(file, destFile, true);
                        }
                    }
                }
            }

            private static string GetRelativePath(string fromPath, string toPath)
            {
                if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
                if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

                Uri fromUri = new Uri(fromPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? fromPath : fromPath + Path.DirectorySeparatorChar);
                Uri toUri = new Uri(toPath);
                Uri relativeUri = fromUri.MakeRelativeUri(toUri);
                string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
                return relativePath.Replace('/', Path.DirectorySeparatorChar);
            }

            // ---- 下载方法 ----
            private static DownloadResult DownloadInstallerFromMirror(string savePath, string mcVersion, string forgeVersion)
            {
                string installerFileName = $"forge-{forgeVersion}-installer.jar";
                string[] possiblePaths = new string[]
                {
                    $"net/minecraftforge/forge/{forgeVersion}/{installerFileName}",
                    $"net/minecraftforge/forge/{mcVersion}-{forgeVersion}/{installerFileName}"
                };

                foreach (string baseUrl in MavenMirrors)
                {
                    foreach (string relPath in possiblePaths)
                    {
                        string fullUrl = baseUrl + relPath;
                        if (TryDownloadFile(fullUrl, savePath, 30000))
                            return new DownloadResult { Success = true, Url = fullUrl };
                    }
                }
                return new DownloadResult { Success = false };
            }

            private static void DownloadInstallerFromOfficial(string savePath, string forgeVersion)
            {
                string installerFileName = $"forge-{forgeVersion}-installer.jar";
                string fullUrl = OFFICIAL_MAVEN + $"net/minecraftforge/forge/{forgeVersion}/{installerFileName}";
                TryDownloadFile(fullUrl, savePath, 600000);
            }

            private static bool TryDownloadFile(string url, string localPath, int timeout)
            {
                string dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "GET";
                        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                        request.Timeout = timeout;
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            if (response.StatusCode != HttpStatusCode.OK)
                                return false;
                            using (Stream stream = response.GetResponseStream())
                            using (FileStream fs = new FileStream(localPath, FileMode.Create))
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
                        if (attempt == 2) throw;
                    }
                    catch
                    {
                        if (attempt == 2) throw;
                    }
                }
                return false;
            }

            // ---- 执行官方安装器（已移除 Java 查找，使用预设 JavaExecutable） ----
            private static bool RunForgeInstaller(string installerPath, string minecraftDir)
            {
                string profilePath = Path.Combine(minecraftDir, "launcher_profiles.json");
                if (!File.Exists(profilePath))
                {
                    try
                    {
                        string json = "{\"profiles\":{},\"settings\":{},\"launcherVersion\":{\"name\":\"1.0.0\",\"format\":1}}";
                        File.WriteAllText(profilePath, json, Encoding.UTF8);
                        Console.WriteLine("[Forge] 已创建 launcher_profiles.json");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Forge] 创建 launcher_profiles.json 失败: {ex.Message}");
                        return false;
                    }
                }

                string javaPath = JavaExecutable;
                if (string.IsNullOrEmpty(javaPath))
                {
                    Console.WriteLine("[Forge] 未设置 Java 可执行文件路径，请设置 Installer.JavaExecutable 属性。");
                    return false;
                }

                // ---- 定义要尝试的参数列表（按常用度排序） ----
                string[] argOptions = new string[]
                {
                    "--installClient",          // 新版 Forge 常用
                    "--install-client",         // 部分版本
                    "--installBoth",            // 某些安装器
                    "--installBothClient"       // 其他变种
                };

                bool anySuccess = false;
                StringBuilder fullOutput = new StringBuilder();
                StringBuilder fullError = new StringBuilder();

                foreach (string arg in argOptions)
                {
                    string args = $"-Djava.awt.headless=true -jar \"{installerPath}\" {arg}";
                    Console.WriteLine($"[Forge] 尝试参数: {arg}");

                    ProcessStartInfo psi = new ProcessStartInfo(javaPath, args)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = minecraftDir
                    };
                    psi.EnvironmentVariables["JAVA_AWT_HEADLESS"] = "true";

                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();

                    using (Process p = Process.Start(psi))
                    {
                        p.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                string line = e.Data;
                                output.AppendLine(line);
                                if (!ShouldFilterForgeLine(line))
                                    Console.WriteLine("[Forge] " + line);
                            }
                        };
                        p.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                string line = e.Data;
                                error.AppendLine(line);
                                if (!ShouldFilterForgeLine(line))
                                    Console.WriteLine("[Forge ERR] " + line);
                            }
                        };
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        p.WaitForExit();

                        fullOutput.Append(output);
                        fullError.Append(error);

                        if (p.ExitCode == 0)
                        {
                            Console.WriteLine($"[Forge] 参数 '{arg}' 安装成功（ExitCode=0）");
                            anySuccess = true;
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"[Forge] 参数 '{arg}' 失败，退出代码: {p.ExitCode}");
                        }
                    }
                }

                _lastInstallLog = fullOutput.ToString() + "\n" + fullError.ToString();

                if (anySuccess)
                {
                    // 清理安装器日志
                    string DownloadLogFile1 = Path.Combine(MinecraftDir, "installer.log");
                    string DownloadLogFile2 = Path.Combine(Path.Combine(SettingsDir, "Mode Loader Installer"), "installer.log");
                    if (File.Exists(DownloadLogFile1))
                    {
                        try { File.Delete(DownloadLogFile1); } catch { }
                    }
                    if (File.Exists(DownloadLogFile2))
                    {
                        try { File.Delete(DownloadLogFile2); } catch { }
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine("[Forge] 所有参数尝试均失败，安装失败");
                    string ErrorLogFile = Path.Combine(Path.Combine(SettingsDir, "Mode Loader Installer"), "installer.log");
                    Console.WriteLine("[Forge] 完整日志:" + ErrorLogFile);
                    return false;
                }
            }

            // ---- 错误检测与日志工具 ----
            private static string _lastInstallLog = "";

            private static bool IsSha1ErrorFromLastRun()
            {
                if (string.IsNullOrEmpty(_lastInstallLog)) return false;
                string lower = _lastInstallLog.ToLowerInvariant();
                return lower.Contains("invalid outputs") ||
                       lower.Contains("sha1") ||
                       lower.Contains("expected") ||
                       lower.Contains("actual") ||
                       lower.Contains("checksum") ||
                       lower.Contains("hash");
            }

            private static string SaveLogToFile(string logContent)
            {
                string dir = GetInstallerCacheDir();
                string fileName = $"forge_install_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                string filePath = Path.Combine(dir, fileName);
                File.WriteAllText(filePath, logContent, Encoding.UTF8);
                return filePath;
            }

            private static string ExtractErrorSummary(string log)
            {
                string[] lines = log.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (line.Contains("invalid outputs") || line.Contains("Processor failed") ||
                        line.Contains("Expected:") || line.Contains("Actual:"))
                        return line.Trim();
                }
                return "未知错误，请查看完整日志。";
            }

            // ---- 其他辅助方法 ----
            public static string DetectInstalledVersion(string minecraftDir, string gameVersion, string forgeVersion)
            {
                string versionsDir = Path.Combine(minecraftDir, "versions");
                if (!Directory.Exists(versionsDir))
                    return $"{gameVersion}-forge-{forgeVersion}";

                string[] possibleNames = new string[]
                {
                    $"{gameVersion}-forge-{forgeVersion}",
                    $"{gameVersion}-forge",
                    $"forge-{gameVersion}-{forgeVersion}",
                    $"forge-{gameVersion}"
                };

                foreach (var name in possibleNames)
                {
                    string fullPath = Path.Combine(versionsDir, name);
                    if (Directory.Exists(fullPath))
                        return name;
                }

                var dirs = Directory.GetDirectories(versionsDir)
                    .Where(d =>
                    {
                        string dirName = Path.GetFileName(d);
                        if (!dirName.StartsWith(gameVersion))
                            return false;
                        return dirName.Contains("forge") && !dirName.Contains("neoforge");
                    })
                    .OrderByDescending(d => Directory.GetCreationTime(d))
                    .ToArray();

                if (dirs.Length > 0)
                    return Path.GetFileName(dirs[0]);

                return $"{gameVersion}-forge-{forgeVersion}";
            }

            private static string GetInstallerCacheDir()
            {
                string launcherPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string cacheDir = Path.Combine(Path.Combine(launcherPath, "Launcher Setting"), "Mode Loader Installer");
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);
                return cacheDir;
            }

            // ---------- 版本支持检查 ----------
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

            private static string SanitizeVersion(string version)
            {
                if (string.IsNullOrEmpty(version)) return version;
                string cleaned = Regex.Replace(version, @"\u001B\[[;\\d]*m", "");
                cleaned = Regex.Replace(cleaned, @"[\x00-\x1F]", "");
                int dashIndex = cleaned.IndexOf('-');
                if (dashIndex >= 0) cleaned = cleaned.Substring(0, dashIndex);
                return cleaned.Trim();
            }

            // ---------- 获取 Forge 版本列表 ----------
            public static string[] GetForgeLoaderVersions(string gameVersion)
            {
                if (string.IsNullOrEmpty(gameVersion) || gameVersion.Contains("加载") || gameVersion.Contains("失败"))
                    return new string[] { "不选择" };

                string cleanGame = SanitizeVersion(gameVersion);
                if (!Regex.IsMatch(cleanGame, @"^\d+(\.\d+){1,2}$"))
                    return new string[] { "不选择" };

                List<string> versions = new List<string>();
                string cacheFile = Path.Combine(SettingsDir, "forge_versions.xml");

                // ---- 1. 尝试从缓存读取 ----
                if (File.Exists(cacheFile))
                {
                    try
                    {
                        string xml = File.ReadAllText(cacheFile);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xml);
                        XmlNodeList versionNodes = doc.SelectNodes("//metadata/versioning/versions/version");
                        if (versionNodes != null && versionNodes.Count > 0)
                        {
                            foreach (XmlNode node in versionNodes)
                            {
                                string ver = node.InnerText.Trim();
                                if (IsForgeVersionCompatible(ver, cleanGame))
                                    versions.Add(ver);
                            }
                            if (versions.Count > 0)
                            {
                                versions.Sort((a, b) =>
                                {
                                    Version va = TryParseForgeVersion(a);
                                    Version vb = TryParseForgeVersion(b);
                                    return vb.CompareTo(va);
                                });
                                versions.Insert(0, "不选择");
                                return versions.ToArray();
                            }
                        }
                    }
                    catch
                    {
                        // 缓存读取失败，继续网络请求
                    }
                }

                // ---- 2. 缓存不可用，网络请求 ----
                try
                {
                    string xml = MinecraftCore.DownloadString("https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml");
                    if (string.IsNullOrEmpty(xml))
                    {
                        return new string[] { "不选择" };
                    }
                    // 保存到缓存
                    File.WriteAllText(cacheFile, xml);

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);
                    XmlNodeList versionNodes = doc.SelectNodes("//metadata/versioning/versions/version");
                    if (versionNodes == null || versionNodes.Count == 0)
                    {
                        return new string[] { "不选择" };
                    }
                    foreach (XmlNode node in versionNodes)
                    {
                        string ver = node.InnerText.Trim();
                        if (IsForgeVersionCompatible(ver, cleanGame))
                            versions.Add(ver);
                    }
                }
                catch (Exception ex)
                {
                    return new string[] { "不选择" };
                }

                if (versions.Count == 0)
                {
                    return new string[] { "不选择" };
                }
                versions.Sort((a, b) =>
                {
                    Version va = TryParseForgeVersion(a);
                    Version vb = TryParseForgeVersion(b);
                    return vb.CompareTo(va);
                });
                versions.Insert(0, "不选择");
                return versions.ToArray();
            }

            private static bool IsForgeVersionCompatible(string forgeVer, string gameVer)
            {
                string extractedGameVer = ExtractGameVersionFromForgeVersion(forgeVer);
                if (string.IsNullOrEmpty(extractedGameVer))
                    return false;
                return IsVersionMatch(extractedGameVer, gameVer);
            }

            private static string ExtractGameVersionFromForgeVersion(string forgeVer)
            {
                if (string.IsNullOrEmpty(forgeVer)) return null;
                int dashIdx = forgeVer.IndexOf('-');
                if (dashIdx > 0)
                {
                    string candidate = forgeVer.Substring(0, dashIdx);
                    if (Regex.IsMatch(candidate, @"^\d+(\.\d+){1,2}$"))
                        return candidate;
                }
                var match = Regex.Match(forgeVer, @"^(\d+(\.\d+){1,2})");
                if (match.Success)
                    return match.Value;
                return null;
            }

            private static bool IsVersionMatch(string versionA, string versionB)
            {
                string[] partsA = versionA.Split('.');
                string[] partsB = versionB.Split('.');
                int len = Math.Min(partsA.Length, partsB.Length);
                for (int i = 0; i < len; i++)
                {
                    if (!int.TryParse(partsA[i], out int a) || !int.TryParse(partsB[i], out int b))
                        return false;
                    if (a != b)
                        return false;
                }
                return true;
            }

            private static Version TryParseForgeVersion(string ver)
            {
                int dashIdx = ver.IndexOf('-');
                string versionPart = dashIdx >= 0 ? ver.Substring(dashIdx + 1) : ver;
                var match = Regex.Match(versionPart, @"^(\d+(\.\d+)*)");
                if (match.Success)
                {
                    try { return new Version(match.Value); }
                    catch { }
                }
                match = Regex.Match(ver, @"^(\d+(\.\d+)*)");
                if (match.Success)
                {
                    try { return new Version(match.Value); }
                    catch { }
                }
                return new Version(0, 0);
            }

            // ---------- 过滤 Forge 日志 ----------
            private static bool ShouldFilterForgeLine(string line)
            {
                if (string.IsNullOrEmpty(line)) return false;
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

            /// <summary>
            /// 检查 Forge 版本文件是否完整（仅检查存在性，不计算 SHA1）
            /// </summary>
            public static bool CheckForgeIntegrity(string versionId)
            {
                string minecraftDir = MinecraftCore.GetMinecraftDir();
                string versionDir = Path.Combine(Path.Combine(minecraftDir, "versions"), versionId);
                string jsonPath = Path.Combine(versionDir, versionId + ".json");
                string jarPath = Path.Combine(versionDir, versionId + ".jar");

                // 1. 检查核心文件
                if (!File.Exists(jsonPath) || !File.Exists(jarPath) || new FileInfo(jarPath).Length == 0)
                    return false;

                // 2. 解析版本 JSON
                var root = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(jsonPath));
                string os = MinecraftCore.GetOsName();

                // 3. 递归收集所有库（包括父版本和 patches）
                var allLibraries = MinecraftCore.CollectLibrariesRecursivelyInternal(root, minecraftDir, os, new HashSet<string>());

                // 4. 检查每个库是否存在且非空
                foreach (var lib in allLibraries)
                {
                    if (!lib.ContainsKey("name")) continue;
                    string name = lib["name"].ToString();

                    // ★ 跳过 Forge 自身核心库（因为它在版本目录中，而非 libraries）
                    if (name.StartsWith("net.minecraftforge:forge:"))
                        continue;

                    // ★ 跳过只有 natives 分类器而没有 artifact 的库
                    bool hasArtifact = false;
                    if (lib.ContainsKey("downloads"))
                    {
                        var downloads = lib["downloads"] as Dictionary<string, object>;
                        if (downloads != null && downloads.ContainsKey("artifact"))
                            hasArtifact = true;
                    }
                    if (!hasArtifact && lib.ContainsKey("natives"))
                        continue;

                    string localPath = ResolveLibraryPath(name, minecraftDir);
                    if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath) || new FileInfo(localPath).Length == 0)
                    {
                        Console.WriteLine($"[CheckForgeIntegrity] 缺失库: {name} -> {localPath}");
                        return false;
                    }
                }

                // 5. 额外检查 Forge 自身核心库（universal 或 client）是否在 libraries 目录中存在
                //    有时版本 JSON 中的 libraries 可能不包含自身，但我们已经复制了 universal.jar 到版本目录，所以核心 jar 已检查。
                //    如果担心版本目录下的 jar 损坏，可以额外检查 libraries 中的对应文件，但非必需。

                return true;
            }

            /// <summary>
            /// 根据库名解析本地路径（Forge 使用 Maven 路径）
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

            private class DownloadResult
            {
                public bool Success { get; set; }
                public string Url { get; set; }
            }
        }

        // ==================== Launcher（启动游戏） ====================
        public class Launcher : BaseLauncher
        {

            protected override List<string> BuildCommand(LaunchContext context)
            {
                var cmd = new List<string>();
                cmd.Add(JavaResolver.GetJavaPath(context, context.MinecraftDir, context.VersionId));

                var root = LoadVersionJson(context.VersionId, context.MinecraftDir);
                string os = MinecraftCore.GetOsName();

                var libs = MinecraftCore.CollectLibrariesRecursively(root, context.MinecraftDir, os);
                libs.RemoveAll(l => l.ContainsKey("name") &&
                    (l["name"].ToString().Contains("neoforged") || l["name"].ToString().Contains("fabricmc")));

                string assetIndexId = GetAssetIndexId(root, context.MinecraftDir);
                string mainClass = root.ContainsKey("mainClass") ? root["mainClass"].ToString() : "net.minecraft.launchwrapper.Launch";

                string originalVersion = null;
                if (root.ContainsKey("minecraftVersion"))
                    originalVersion = root["minecraftVersion"].ToString();
                else if (root.ContainsKey("inheritsFrom"))
                    originalVersion = root["inheritsFrom"].ToString();

                string nativesDir = PrepareNatives(context, libs);
                string libraryDir = Path.Combine(context.MinecraftDir, "libraries");

                // JVM 参数
                cmd.Add($"-Xms{context.InitMemory}");
                cmd.Add($"-Xmx{context.MaxMemory}");
                cmd.Add($"-Djava.library.path=\"{nativesDir}\"");
                cmd.Add("-Dfml.environment=client");
                cmd.Add("-Dforge.logging.mojang.level=OFF");
                cmd.Add("-Dstdout.encoding=UTF-8");
                cmd.Add("-Dstderr.encoding=UTF-8");

                // 旧版 Forge 特殊参数
                string gameVersion = root.ContainsKey("inheritsFrom") ? root["inheritsFrom"].ToString() : context.VersionId;
                bool isOldForge = false;
                if (!string.IsNullOrEmpty(gameVersion))
                {
                    var parts = gameVersion.Split('.');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor))
                    {
                        if (major == 1 && minor <= 12)
                            isOldForge = true;
                    }
                }
                if (isOldForge)
                {
                    cmd.Add("-Dfml.ignoreInvalidMinecraftCertificates=true");
                    cmd.Add("-Dfml.ignorePatchDiscrepancies=true");
                }

                // ★ 添加 JVM 参数（包含模块路径 -p 等）
                AddJvmArgs(cmd, root, os, libraryDir, nativesDir, context);

                // ★ 不再手动添加模块路径，完全依赖 AddJvmArgs

                // ========== 构建 classpath ==========
                string classpath = BuildClasspath(context, libs, null);
                var entries = classpath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                // 1. 核心 jar（当前版本目录下的）
                string coreJar = Path.Combine(context.VersionDataPath, context.VersionId + ".jar");
                if (File.Exists(coreJar))
                {
                    entries.RemoveAll(e => string.Equals(e, coreJar, StringComparison.OrdinalIgnoreCase));
                    entries.Insert(0, coreJar);
                }

                // 2. 对于旧版 Forge，添加父版本 client jar
                if (mainClass == "net.minecraft.launchwrapper.Launch")
                {
                    string parentVersion = null;
                    if (root.ContainsKey("minecraftVersion"))
                        parentVersion = root["minecraftVersion"].ToString();
                    else if (root.ContainsKey("inheritsFrom"))
                        parentVersion = root["inheritsFrom"].ToString();

                    if (!string.IsNullOrEmpty(parentVersion))
                    {
                        string parentJar = Path.Combine(Path.Combine(Path.Combine(context.MinecraftDir, "versions"), parentVersion), parentVersion + ".jar");
                        if (File.Exists(parentJar))
                        {
                            if (!entries.Any(e => string.Equals(e, parentJar, StringComparison.OrdinalIgnoreCase)))
                            {
                                int insertIndex = entries.IndexOf(coreJar) + 1;
                                if (insertIndex <= 0) insertIndex = 0;
                                entries.Insert(insertIndex, parentJar);
                            }
                        }
                    }
                }

                classpath = string.Join(";", entries.Distinct().ToArray());

                cmd.Add("-cp");
                cmd.Add(classpath);

                // 仅当主类不是 LaunchWrapper 时才添加 -DignoreList
                if (mainClass != "net.minecraft.launchwrapper.Launch")
                {
                    string ignoreList = $"bootstraplauncher,securejarhandler,asm-commons,asm-util,asm-analysis,asm-tree,asm,JarJarFileSystems,client-extra,fmlcore,javafmllanguage,lowcodelanguage,mclanguage,forge-,{context.VersionId}.jar";
                    bool hasIgnore = false;
                    for (int i = 0; i < cmd.Count; i++)
                    {
                        if (cmd[i].StartsWith("-DignoreList="))
                        {
                            cmd[i] = "-DignoreList=" + ignoreList;
                            hasIgnore = true;
                            break;
                        }
                    }
                    if (!hasIgnore) cmd.Add("-DignoreList=" + ignoreList);
                }

                // 主类

                cmd.Add(mainClass);

                // 在 BuildCommand 中，在 cmd.Add(mainClass); 之后
                if (mainClass == "net.minecraft.launchwrapper.Launch")
                {
                    // 手动添加游戏参数（确保顺序正确）
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
                    cmd.Add("--tweakClass");
                    cmd.Add("net.minecraftforge.fml.common.launcher.FMLTweaker");
                    cmd.Add("--versionType");
                    cmd.Add("Forge");
                    // 宽度和高度可能不需要，但可以添加
                    cmd.Add("--width");
                    cmd.Add(context.Width.ToString());
                    cmd.Add("--height");
                    cmd.Add(context.Height.ToString());
                }
                else
                {
                    AddGameArgs(cmd, context, root, assetIndexId);
                }
                // 移除 --demo 使游戏不以试玩模式运行
                cmd.Remove("--demo");

                // 调试打印（建议保留）
                Console.WriteLine("[启动] 完整命令行: " + string.Join(" ", cmd.Select(arg => arg.Contains(" ") ? "\"" + arg + "\"" : arg).ToArray()));

                return cmd;
            }
        }
    }
}