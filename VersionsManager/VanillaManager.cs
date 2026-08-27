using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace New_Launcher
{
    public static class VanillaManager
    {
        /// <summary>
        /// 下载原版文件
        /// </summary>
        // ==================== Installer（下载原版） ====================
        public static class Installer
        {
            public static void DownloadVanilla(string gameVersion, string minecraftDir, string versionPathName, bool isLaunch)
            {
                if (string.IsNullOrEmpty(versionPathName))
                    versionPathName = gameVersion;

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
                if (versionEntry == null)
                    throw new Exception("未找到版本 " + gameVersion);

                string versionJsonUrl = versionEntry["url"].ToString();
                string versionJson = DownloadStringWithRetry(versionJsonUrl);
                var versionRoot = DeserializeJson<Dictionary<string, object>>(versionJson);
                versionRoot["id"] = versionPathName;

                string versionFolder = Path.Combine(Path.Combine(minecraftDir, "versions"), versionPathName);
                Directory.CreateDirectory(versionFolder);
                string versionJsonPath = Path.Combine(versionFolder, versionPathName + ".json");
                File.WriteAllText(versionJsonPath, SerializeJson(versionRoot), Encoding.UTF8);

                // 构建下载项
                List<DownloadItem> downloadItems = new List<DownloadItem>();
                List<AssetItem> assetItems = new List<AssetItem>();

                // client jar
                if (versionRoot.ContainsKey("downloads"))
                {
                    var downloads = versionRoot["downloads"] as Dictionary<string, object>;
                    if (downloads != null && downloads.ContainsKey("client"))
                    {
                        var client = downloads["client"] as Dictionary<string, object>;
                        string url = client["url"].ToString();
                        string sha1 = client["sha1"].ToString();
                        string dest = Path.Combine(versionFolder, versionPathName + ".jar");
                        downloadItems.Add(new DownloadItem { Url = url, Dest = dest, Sha1 = sha1 });
                    }
                }

                // libraries
                var libraries = versionRoot["libraries"] as ArrayList;
                if (libraries != null)
                {
                    Console.WriteLine("[下载] 解析到 " + libraries.Count + " 个库");
                    int added = 0, nativeAdded = 0;
                    foreach (var libObj in libraries)
                    {
                        var lib = libObj as Dictionary<string, object>;
                        if (lib == null) continue;
                        if (!MinecraftCore.IsLibraryAllowed(lib, MinecraftCore.GetOsName())) continue;

                        var dlLib = lib["downloads"] as Dictionary<string, object>;
                        if (dlLib == null) continue;

                        if (dlLib.ContainsKey("artifact"))
                        {
                            var artifact = dlLib["artifact"] as Dictionary<string, object>;
                            string url = artifact["url"].ToString();
                            string path = artifact["path"].ToString();
                            string sha1 = artifact.ContainsKey("sha1") ? artifact["sha1"].ToString() : null;
                            string dest = Path.Combine(Path.Combine(minecraftDir, "libraries"), path);
                            downloadItems.Add(new DownloadItem { Url = url, Dest = dest, Sha1 = sha1 });
                            added++;
                        }

                        if (dlLib.ContainsKey("classifiers"))
                        {
                            var classifiers = dlLib["classifiers"] as Dictionary<string, object>;
                            foreach (var kv in classifiers)
                            {
                                string key = kv.Key;
                                if (key.IndexOf("windows", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    var nativeObj = kv.Value as Dictionary<string, object>;
                                    string url = nativeObj["url"].ToString();
                                    string path = nativeObj["path"].ToString();
                                    string sha1 = nativeObj.ContainsKey("sha1") ? nativeObj["sha1"].ToString() : null;
                                    string dest = Path.Combine(Path.Combine(minecraftDir, "libraries"), path);
                                    downloadItems.Add(new DownloadItem { Url = url, Dest = dest, Sha1 = sha1 });
                                    nativeAdded++;
                                }
                            }
                        }
                    }
                    Console.WriteLine("[下载] 添加了 " + added + " 个 artifact 任务，" + nativeAdded + " 个 native 任务");
                }

                // assets
                string assetIndexId = null;
                string assetIndexPath = null;
                if (versionRoot.ContainsKey("assetIndex"))
                {
                    var assetIdx = versionRoot["assetIndex"] as Dictionary<string, object>;
                    assetIndexId = assetIdx["id"].ToString();
                    string idxUrl = assetIdx["url"].ToString();
                    string idxSha1 = assetIdx.ContainsKey("sha1") ? assetIdx["sha1"].ToString() : null;
                    assetIndexPath = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "assets"), "indexes"), assetIndexId + ".json");
                    // 修改点 1：使用重试版本
                    DownloadFileWithRetry(idxUrl, assetIndexPath, idxSha1);

                    if (File.Exists(assetIndexPath))
                    {
                        var root = JObject.Parse(File.ReadAllText(assetIndexPath));
                        var objects = root["objects"] as JObject;
                        if (objects != null)
                        {
                            string objectsDir = Path.Combine(Path.Combine(minecraftDir, "assets"), "objects");
                            Directory.CreateDirectory(objectsDir);
                            foreach (var kv in objects)
                            {
                                string hash = kv.Value["hash"].ToString();
                                string sub = hash.Substring(0, 2);
                                string dest = Path.Combine(Path.Combine(objectsDir, sub), hash);
                                if (!File.Exists(dest))
                                {
                                    string url = "https://resources.download.minecraft.net/" + sub + "/" + hash;
                                    assetItems.Add(new AssetItem { Url = url, Dest = dest, Hash = hash });
                                }
                            }
                        }
                    }
                }

                // 并行下载
                List<DownloadTask> allTasks = new List<DownloadTask>();
                foreach (var item in downloadItems)
                    allTasks.Add(new DownloadTask { Url = item.Url, Dest = item.Dest, Sha1 = item.Sha1, IsAsset = false });
                foreach (var item in assetItems)
                    allTasks.Add(new DownloadTask { Url = item.Url, Dest = item.Dest, Sha1 = item.Hash, IsAsset = true });

                int totalFiles = allTasks.Count;
                if (totalFiles == 0)
                {
                    Console.WriteLine("[下载] 没有需要下载的文件");
                }
                else
                {
                    Console.WriteLine($"[下载] 共需下载 {totalFiles} 个文件 (库 + 资源)，开始并行下载...");

                    int completed = 0;
                    object lockObj = new object();
                    List<Exception> exceptions = new List<Exception>();
                    ManualResetEvent allDone = new ManualResetEvent(false);
                    int remainingTasks = allTasks.Count;

                    foreach (var task in allTasks)
                    {
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            bool hasCounted = false;
                            try
                            {
                                bool needDownload = true;

                                if (File.Exists(task.Dest))
                                {
                                    if (!string.IsNullOrEmpty(task.Sha1))
                                    {
                                        string actualSha1 = ComputeSha1(task.Dest);
                                        if (string.Equals(actualSha1, task.Sha1, StringComparison.OrdinalIgnoreCase))
                                        {
                                            needDownload = false;
                                            hasCounted = true;
                                            int newCompleted = Interlocked.Increment(ref completed);
                                            if (newCompleted % 10 == 0 || newCompleted == totalFiles)
                                            {
                                                lock (lockObj)
                                                    Console.WriteLine($"[下载] 已完成 {newCompleted}/{totalFiles} 个文件 (跳过已存在，校验通过)");
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[下载] 文件 {Path.GetFileName(task.Dest)} 校验失败，重新下载");
                                            File.Delete(task.Dest);
                                            needDownload = true;
                                        }
                                    }
                                    else
                                    {
                                        needDownload = false;
                                        hasCounted = true;
                                        int newCompleted = Interlocked.Increment(ref completed);
                                        if (newCompleted % 10 == 0 || newCompleted == totalFiles)
                                        {
                                            lock (lockObj)
                                                Console.WriteLine($"[下载] 已完成 {newCompleted}/{totalFiles} 个文件 (跳过已存在，无校验)");
                                        }
                                    }
                                }

                                if (needDownload)
                                {
                                    // 修改点 2：使用重试版本
                                    DownloadFileWithRetry(task.Url, task.Dest, task.Sha1);

                                    if (!string.IsNullOrEmpty(task.Sha1))
                                    {
                                        string actualSha1 = ComputeSha1(task.Dest);
                                        if (!string.Equals(actualSha1, task.Sha1, StringComparison.OrdinalIgnoreCase))
                                        {
                                            File.Delete(task.Dest);
                                            throw new Exception($"下载文件 {Path.GetFileName(task.Dest)} 校验失败，预期 {task.Sha1}，实际 {actualSha1}");
                                        }
                                    }

                                    hasCounted = true;
                                    int newCompleted = Interlocked.Increment(ref completed);
                                    if (newCompleted % 10 == 0 || newCompleted == totalFiles)
                                    {
                                        lock (lockObj)
                                            Console.WriteLine($"[下载] 已完成 {newCompleted}/{totalFiles} 个文件");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                lock (lockObj)
                                    exceptions.Add(ex);
                                if (!hasCounted)
                                {
                                    int newCompleted = Interlocked.Increment(ref completed);
                                    if (newCompleted % 10 == 0 || newCompleted == totalFiles)
                                    {
                                        lock (lockObj)
                                            Console.WriteLine($"[下载] 已完成 {newCompleted}/{totalFiles} 个文件 (含失败)");
                                    }
                                }
                            }
                            finally
                            {
                                if (Interlocked.Decrement(ref remainingTasks) == 0)
                                    allDone.Set();
                            }
                        }, null);
                    }

                    allDone.WaitOne();

                    if (exceptions.Count > 0)
                        throw new Exception($"下载过程中发生 {exceptions.Count} 个错误，第一个错误: {exceptions[0].Message}", exceptions[0]);
                }

                // 处理 legacy
                if (assetIndexId == "legacy")
                {
                    string zipUrl = "https://resources.download.minecraft.net/legacy/legacy.zip";
                    string zipPath = Path.Combine(Path.Combine(minecraftDir, "assets"), "legacy.zip");
                    string legacyDir = Path.Combine(Path.Combine(Path.Combine(minecraftDir, "assets"), "virtual"), "legacy");
                    if (Directory.Exists(legacyDir))
                        Directory.Delete(legacyDir, true);
                    Console.WriteLine("[下载] 下载 legacy 资源包...");
                    try
                    {
                        // 使用重试版本
                        DownloadFileWithRetry(zipUrl, zipPath, null);
                        new FastZip().ExtractZip(zipPath, legacyDir, null);

                        int attempts = 0;
                        while (attempts < 5)
                        {
                            try
                            {
                                File.Delete(zipPath);
                                break;
                            }
                            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                            {
                                attempts++;
                                if (attempts >= 5) throw;
                                Thread.Sleep(100);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[下载] legacy 资源下载失败: " + ex.Message);
                    }
                }
                Console.WriteLine("版本 " + gameVersion + " 处理完成，存储为 " + versionPathName);
            }

            // ---- 辅助方法 ----
            private static string ComputeSha1(string filePath)
            {
                using (FileStream fs = File.OpenRead(filePath))
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] hash = sha1.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }

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

            private static T DeserializeJson<T>(string json) { return new JavaScriptSerializer().Deserialize<T>(json); }
            private static string SerializeJson(object obj) { return new JavaScriptSerializer().Serialize(obj); }

            private class DownloadItem
            {
                public string Url { get; set; }
                public string Dest { get; set; }
                public string Sha1 { get; set; }
            }
            private class AssetItem
            {
                public string Url { get; set; }
                public string Dest { get; set; }
                public string Hash { get; set; }
            }
            private class DownloadTask
            {
                public string Url { get; set; }
                public string Dest { get; set; }
                public string Sha1 { get; set; }
                public bool IsAsset { get; set; }
            }
        }

        // ==================== VanillaManager.Launcher（修改为 ProcessBuilder 启动 + 调试 BAT） ====================
        public class Launcher : BaseLauncher
        {
            protected override void ValidateEnvironment(LaunchContext context)
            {
                string folder = Path.Combine(Path.Combine(context.MinecraftDir, "versions"), context.VersionId);
                if (!Directory.Exists(folder))
                    throw new Exception($"版本目录不存在: {folder}");
                if (!File.Exists(Path.Combine(folder, context.VersionId + ".json")))
                    throw new Exception($"版本 JSON 不存在: {context.VersionId}");
                if (!File.Exists(Path.Combine(folder, context.VersionId + ".jar")))
                    throw new Exception($"核心 Jar 不存在: {context.VersionId}");
                // Java 由 BuildCommand 中的 JavaResolver 处理
            }

            protected override List<string> BuildCommand(LaunchContext context)
            {
                var cmd = new List<string>();
                cmd.Add(JavaResolver.GetJavaPath(context, context.MinecraftDir, context.VersionId));

                var root = LoadVersionJson(context.VersionId, context.MinecraftDir);
                string os = MinecraftCore.GetOsName();

                var libs = GetFilteredLibraries(root, context.MinecraftDir, os);
                string nativesDir = PrepareNatives(context, libs);
                string classpath = BuildClasspath(context, libs);

                // ★ 添加调试日志
                Console.WriteLine($"[Vanilla] classpath 长度: {classpath.Length}");
                Console.WriteLine($"[Vanilla] classpath 前100字符: {classpath.Substring(0, Math.Min(classpath.Length, 100))}");

                // JVM 参数
                cmd.Add($"-Xms{context.InitMemory}");
                cmd.Add($"-Xmx{context.MaxMemory}");
                cmd.Add($"-Djava.library.path=\"{nativesDir}\"");
                AddJvmArgs(cmd, root, os, Path.Combine(context.MinecraftDir, "libraries"), nativesDir, context);

                // ★ 添加 -cp 和 classpath
                cmd.Add("-cp");
                cmd.Add(classpath);   // 确保这里没有硬编码 "${classpath}"

                // 主类
                cmd.Add(GetMainClass(root));

                string assetIndexId = GetAssetIndexId(root, context.MinecraftDir);

                int cpIdxBefore = cmd.IndexOf("-cp");
                if (cpIdxBefore >= 0 && cpIdxBefore + 1 < cmd.Count)
                {
                    Console.WriteLine($"[Vanilla] AddGameArgs 之前 -cp 后面的值: {cmd[cpIdxBefore + 1]}");
                }
                else
                {
                    Console.WriteLine("[Vanilla] AddGameArgs 之前未找到 -cp");
                }

                AddGameArgs(cmd, context, root, assetIndexId);

                // ★ 调试：打印 cmd 中 -cp 后面的值
                int cpIdx = cmd.IndexOf("-cp");
                if (cpIdx >= 0 && cpIdx + 1 < cmd.Count)
                {
                    Console.WriteLine($"[Vanilla] cmd 中 -cp 后面的值: {cmd[cpIdx + 1]}");
                }

                return cmd;
            }
        }

        /// <summary>
        /// 专门用于启动远古版 Minecraft（rd-*, inf-* 等）的启动器。
        /// 优先从启动器根目录的 Java7.zip 解压 Java 7，若没有则回退到 Java 8。
        /// 始终正确设置 -Djava.library.path 指向 natives 目录。
        /// </summary>
        public class AncientLauncher : BaseLauncher
        {
            protected override List<string> BuildCommand(LaunchContext context)
            {
                // 在 MainForm 或启动逻辑中调用
                bool? hasDedicated = HardwareInfo.HasDedicatedGpu();
                if (hasDedicated == false)
                {
                    DialogResult result = MessageBox.Show(
                        "您的电脑中没有独立显卡，远古版平均帧率可能低至1~2帧，是否执意运行？", "硬件不支持",MessageBoxButtons.YesNo,MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes)
                    {
                        throw new LauncherException("用户取消了启动，因为硬件不支持。");
                    }
                }
                else if (hasDedicated == true)
                {
                    Console.WriteLine("[提示] 检测到独立显卡，远古版可以流畅运行。");
                }
                else
                {
                    Console.WriteLine("[提示] 无法检测显卡类型，若出现卡顿请检查显卡驱动。");
                }

                var cmd = new List<string>();

                string javaPath = JavaResolver.GetJavaPath(context, context.MinecraftDir, context.VersionId);
                cmd.Add(javaPath);

                // ---------- JVM 参数 ----------
                cmd.Add("-Xmx256M");
                cmd.Add("-Xms256M");
                cmd.Add("-Dsun.java2d.d3d=true");
                cmd.Add("-Dsun.java2d.opengl=false");
                cmd.Add("-Dawt.toolkit=sun.awt.windows.WToolkit");
                cmd.Add("-Dminecraft.fps=60");
                cmd.Add("-Dfps=60");
                cmd.Add("-XX:+UseSerialGC");

                // Java 8 兼容远古版字节码
                cmd.Add("-Djava.util.Arrays.useLegacyMergeSort=true");

                // ---------- 构建完整的 classpath（包含所有 libraries） ----------
                string versionFolder = Path.Combine(Path.Combine(context.MinecraftDir, "versions"), context.VersionId);
                string versionJsonPath = Path.Combine(versionFolder, context.VersionId + ".json");
                if (!File.Exists(versionJsonPath))
                    throw new LauncherException($"版本 JSON 不存在: {versionJsonPath}");

                var root = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(versionJsonPath));
                string os = MinecraftCore.GetOsName();

                // 收集所有 libraries（包含父版本和 patches）
                var allLibs = MinecraftCore.CollectLibrariesRecursively(root, context.MinecraftDir, os);

                // 使用基类的统一 natives 管理
                string nativesDir = PrepareNatives(context, allLibs);

                // 关键修复：将 natives 目录添加到 java.library.path
                cmd.Add($"-Djava.library.path=\"{nativesDir}\"");

                // 构建 classpath（包含核心 jar 和所有库）
                string classpath = MinecraftCore.BuildClasspath(allLibs, context.MinecraftDir, context.VersionId);
                cmd.Add("-cp");
                cmd.Add(classpath);

                // ---------- 主类 ----------
                string mainClass = DetermineMainClass(context.VersionId);
                cmd.Add(mainClass);

                // ---------- 游戏参数 ----------
                cmd.Add("--gameDir");
                cmd.Add(context.MinecraftDir);
                cmd.Add("--username");
                cmd.Add(context.Username);

                return cmd;
            }

            // ==================== 辅助方法 ====================

            private string DetermineMainClass(string versionId)
            {
                if (versionId.StartsWith("rd-"))
                    return "com.mojang.rubydung.RubyDung";
                if (versionId.StartsWith("inf-"))
                    return "com.mojang.minecraft.Minecraft";
                // 更多远古版可扩展
                return "com.mojang.rubydung.RubyDung";
            }
        }
    }
}