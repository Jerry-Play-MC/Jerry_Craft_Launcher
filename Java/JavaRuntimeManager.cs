using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using ICSharpCode.SharpZipLib.Zip;

namespace MinecraftJavaRuntime
{
    public class JavaRuntimeManager
    {
        private const string AllJsonUrl =
            "https://launchermeta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json";

        private const string VersionManifestUrl =
            "https://launchermeta.mojang.com/mc/game/version_manifest.json";

        private readonly string _runtimeBaseDir;

        public JavaRuntimeManager(string runtimeBaseDir = null)
        {
            if (string.IsNullOrEmpty(runtimeBaseDir))
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _runtimeBaseDir = Path.Combine(baseDir, "runtime");
            }
            else
            {
                _runtimeBaseDir = runtimeBaseDir;
            }
        }

        // ================== 对外公开方法 ==================
        public string EnsureJavaRuntime(string versionId, string minecraftRoot = null)
        {
            if (string.IsNullOrEmpty(versionId))
                throw new ArgumentException("版本 ID 不能为空");

            string mcVersion = ExtractMinecraftVersion(versionId, minecraftRoot);
            Console.WriteLine($"[JavaRuntime] 提取到的原版版本号: {mcVersion}");

            string javaComponent = GetJavaComponentForVersion(mcVersion);
            Console.WriteLine($"[JavaRuntime] 对应的 Java 组件: {javaComponent}");

            Dictionary<string, object> runtimeEntry = GetRuntimeEntry(javaComponent);
            if (runtimeEntry == null)
                throw new Exception($"未找到适用于平台的运行时组件: {javaComponent}");

            var versionDict = runtimeEntry["version"] as Dictionary<string, object>;
            if (versionDict == null || !versionDict.ContainsKey("name"))
                throw new Exception("无法解析版本名称");

            string versionName = versionDict["name"].ToString();
            string targetDir = Path.Combine(_runtimeBaseDir, versionName);
            string javaExecutable = GetJavaExecutablePath(targetDir);

            if (File.Exists(javaExecutable))
            {
                Console.WriteLine("[JavaRuntime] 缓存已存在: " + javaExecutable);
                return javaExecutable;
            }

            // 迁移旧格式目录
            string oldDir = FindOldFormatDirectory(_runtimeBaseDir, versionName);
            if (!string.IsNullOrEmpty(oldDir))
            {
                Console.WriteLine($"发现旧格式目录: {oldDir}，迁移到: {targetDir}");
                try
                {
                    Directory.Move(oldDir, targetDir);
                    if (File.Exists(javaExecutable))
                        return javaExecutable;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"迁移失败: {ex.Message}，重新下载");
                    if (Directory.Exists(oldDir))
                        Directory.Delete(oldDir, true);
                }
            }

            Console.WriteLine("[JavaRuntime] 开始下载运行时...");
            DownloadRuntime(runtimeEntry, targetDir);

            if (!File.Exists(javaExecutable))
                throw new Exception("下载完成后未找到 Java 可执行文件");

            Console.WriteLine("[JavaRuntime] 下载完成: " + javaExecutable);
            return javaExecutable;
        }

        // ================== 版本提取核心方法 ==================
        private string ExtractMinecraftVersion(string versionId, string minecraftRoot = null)
        {
            if (IsPureVersion(versionId))
                return versionId;

            // 策略1：从版本文件夹的 JSON 中读取 minecraftVersion
            string versionJsonPath = GetVersionJsonPath(versionId, minecraftRoot);
            if (!string.IsNullOrEmpty(versionJsonPath) && File.Exists(versionJsonPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(versionJsonPath);
                    var serializer = new JavaScriptSerializer();
                    var json = serializer.Deserialize<Dictionary<string, object>>(jsonContent);

                    if (json.ContainsKey("minecraftVersion"))
                        return json["minecraftVersion"].ToString();

                    if (json.ContainsKey("inheritsFrom"))
                    {
                        string parentId = json["inheritsFrom"].ToString();
                        return ExtractMinecraftVersion(parentId, minecraftRoot);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExtractMinecraftVersion] 读取 JSON 失败: {ex.Message}");
                }
            }

            // 策略2：按空格分割取第一个
            string[] parts = versionId.Split(' ');
            if (parts.Length > 0 && IsPureVersion(parts[0]))
                return parts[0];

            // 策略3：从 client.jar 读取
            string clientVersion = TryGetVersionFromClientJar(versionId, minecraftRoot);
            if (!string.IsNullOrEmpty(clientVersion))
                return clientVersion;

            // 策略4：正则匹配
            var match = System.Text.RegularExpressions.Regex.Match(versionId, @"(\d+\.\d+(\.\d+)?)");
            if (match.Success)
                return match.Groups[1].Value;

            throw new Exception($"无法从版本 ID 提取原版版本号: {versionId}");
        }

        private bool IsPureVersion(string version)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+(\.\d+)?$");
        }

        private string GetVersionJsonPath(string versionId, string minecraftRoot)
        {
            if (string.IsNullOrEmpty(minecraftRoot))
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                minecraftRoot = Path.Combine(baseDir, ".minecraft");
            }
            return Path.Combine(Path.Combine(Path.Combine(Path.Combine(minecraftRoot, "versions"), versionId + ".json"), versionId), versionId + ".json");
        }

        private string TryGetVersionFromClientJar(string versionId, string minecraftRoot)
        {
            if (string.IsNullOrEmpty(minecraftRoot))
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                minecraftRoot = Path.Combine(baseDir, ".minecraft");
            }

            string versionDir = Path.Combine(Path.Combine(minecraftRoot, "versions"), versionId);
            string clientJar = Path.Combine(versionDir, versionId + ".jar");

            if (!File.Exists(clientJar))
            {
                Console.WriteLine($"[TryGetVersionFromClientJar] 文件不存在: {clientJar}");
                return null;
            }

            try
            {
                using (var zip = new ZipFile(clientJar))
                {
                    ZipEntry entry = zip.GetEntry("version.json");
                    if (entry == null)
                    {
                        Console.WriteLine("[TryGetVersionFromClientJar] 未找到 version.json");
                        return null;
                    }

                    using (var stream = zip.GetInputStream(entry))
                    using (var reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();
                        var serializer = new JavaScriptSerializer();
                        var json = serializer.Deserialize<Dictionary<string, object>>(content);
                        if (json != null && json.ContainsKey("id"))
                        {
                            string version = json["id"].ToString();
                            Console.WriteLine($"[TryGetVersionFromClientJar] 读取到版本: {version}");
                            return version;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TryGetVersionFromClientJar] 读取失败: {ex.Message}");
                Console.WriteLine($"详细堆栈: {ex.ToString()}");
            }
            return null;
        }

        // ================== 获取 Java 组件名 ==================
        private string GetJavaComponentForVersion(string mcVersion)
        {
            string manifestJson = DownloadString(VersionManifestUrl);
            var serializer = new JavaScriptSerializer();
            var manifest = serializer.Deserialize<Dictionary<string, object>>(manifestJson);

            if (!manifest.ContainsKey("versions"))
                throw new Exception("版本清单缺少 versions 字段");

            var versions = manifest["versions"] as ArrayList;
            if (versions == null || versions.Count == 0)
                throw new Exception("版本清单为空");

            Dictionary<string, object> targetVersion = null;
            foreach (object item in versions)
            {
                var v = item as Dictionary<string, object>;
                if (v != null && v.ContainsKey("id") && v["id"].ToString() == mcVersion)
                {
                    targetVersion = v;
                    break;
                }
            }

            if (targetVersion == null)
                throw new Exception($"未找到游戏版本: {mcVersion}");

            if (!targetVersion.ContainsKey("url"))
                throw new Exception("版本条目缺少 url");

            string versionJsonUrl = targetVersion["url"].ToString();
            string versionJson = DownloadString(versionJsonUrl);
            var versionData = serializer.Deserialize<Dictionary<string, object>>(versionJson);

            if (versionData.ContainsKey("javaVersion"))
            {
                var javaVer = versionData["javaVersion"] as Dictionary<string, object>;
                if (javaVer != null && javaVer.ContainsKey("component"))
                    return javaVer["component"].ToString();
            }

            return "java-runtime-gamma";
        }

        // ================== 获取运行时条目 ==================
        public Dictionary<string, object> GetRuntimeEntry(string component)
        {
            string allJson = DownloadString(AllJsonUrl);
            var serializer = new JavaScriptSerializer();
            var allData = serializer.Deserialize<Dictionary<string, object>>(allJson);

            string platformKey = GetPlatformKey();

            if (!allData.ContainsKey(platformKey))
                throw new Exception($"未找到平台 {platformKey} 的运行时配置");

            var platform = allData[platformKey] as Dictionary<string, object>;
            if (platform == null || !platform.ContainsKey(component))
                throw new Exception($"平台 {platformKey} 下未找到组件 {component}");

            var entries = platform[component] as ArrayList;
            if (entries == null || entries.Count == 0)
                throw new Exception($"组件 {component} 下没有可用版本");

            return entries[0] as Dictionary<string, object>;
        }

        // ================== 下载运行时（完整实现） ==================
        private void DownloadRuntime(Dictionary<string, object> runtimeEntry, string targetDir)
        {
            if (!runtimeEntry.ContainsKey("manifest"))
                throw new Exception("运行时条目缺少 manifest");

            var manifestObj = runtimeEntry["manifest"] as Dictionary<string, object>;
            if (manifestObj == null || !manifestObj.ContainsKey("url"))
                throw new Exception("manifest 缺少 url");

            string manifestUrl = manifestObj["url"].ToString();
            Console.WriteLine("[JavaRuntime] 获取 manifest: " + manifestUrl);
            string manifestJson = DownloadString(manifestUrl);

            var serializer = new JavaScriptSerializer();
            var manifestData = serializer.Deserialize<Dictionary<string, object>>(manifestJson);

            if (!manifestData.ContainsKey("files"))
                throw new Exception("manifest 中缺少 files 字段");

            var files = manifestData["files"] as Dictionary<string, object>;
            if (files == null)
                throw new Exception("files 字段格式错误");

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            Console.WriteLine("[JavaRuntime] 需要处理 " + files.Count + " 个条目");

            int downloadedCount = 0;
            foreach (var kv in files)
            {
                string relativePath = kv.Key;
                var fileInfo = kv.Value as Dictionary<string, object>;
                if (fileInfo == null)
                    continue;

                // 从 downloads.raw 中提取下载信息
                Dictionary<string, object> downloadInfo = null;
                if (fileInfo.ContainsKey("downloads"))
                {
                    var downloads = fileInfo["downloads"] as Dictionary<string, object>;
                    if (downloads != null && downloads.ContainsKey("raw"))
                        downloadInfo = downloads["raw"] as Dictionary<string, object>;
                }
                if (downloadInfo == null)
                    continue; // 不是可下载文件，跳过

                if (!downloadInfo.ContainsKey("url") || !downloadInfo.ContainsKey("sha1") || !downloadInfo.ContainsKey("size"))
                {
                    Console.WriteLine("[JavaRuntime] 跳过缺少必要字段的文件: " + relativePath);
                    continue;
                }

                string fileUrl = downloadInfo["url"].ToString();
                string sha1 = downloadInfo["sha1"].ToString();
                long size = Convert.ToInt64(downloadInfo["size"]);

                string localPath = Path.Combine(targetDir, relativePath);
                string dir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // 检查本地是否有效
                if (File.Exists(localPath))
                {
                    try
                    {
                        if (VerifyFile(localPath, sha1, size))
                            continue;
                        else
                        {
                            Console.WriteLine("[JavaRuntime] 文件校验失败，重新下载: " + relativePath);
                            File.Delete(localPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[JavaRuntime] 校验异常，重新下载: " + relativePath + " - " + ex.Message);
                        File.Delete(localPath);
                    }
                }

                // 下载
                try
                {
                    Console.WriteLine("[JavaRuntime] 下载[" + (downloadedCount + 1) + "/" + files.Count + "]: " + relativePath);
                    DownloadFile(fileUrl, localPath);
                    if (!VerifyFile(localPath, sha1, size))
                        throw new Exception("下载后校验失败");
                    downloadedCount++;
                }
                catch (Exception ex)
                {
                    throw new Exception($"下载文件 {relativePath} 失败: {ex.Message}");
                }
            }

            Console.WriteLine("[JavaRuntime] 所有文件处理完成，实际下载了 " + downloadedCount + " 个文件");
        }

        // ================== 平台相关 ==================
        private string GetPlatformKey()
        {
            PlatformID pid = Environment.OSVersion.Platform;
            if (pid == PlatformID.Win32NT)
                return Is64BitOperatingSystem() ? "windows-x64" : "windows-x86";
            else if (pid == PlatformID.MacOSX)
                return "osx";
            else
                return "linux";
        }

        private bool Is64BitOperatingSystem()
        {
            string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            string arch6432 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
            if (!string.IsNullOrEmpty(arch6432))
                return true;
            if (arch == "AMD64" || arch == "IA64")
                return true;
            return IntPtr.Size == 8;
        }

        private string GetJavaExecutablePath(string runtimeDir)
        {
            string execName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "javaw.exe" : "java";
            string binDir = Path.Combine(runtimeDir, "bin");
            return Path.Combine(binDir, execName);
        }

        // ================== 网络与文件工具 ==================
        private string DownloadString(string url)
        {
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                return client.DownloadString(url);
            }
        }

        private void DownloadFile(string url, string localPath)
        {
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                client.DownloadFile(url, localPath);
            }
        }

        private bool VerifyFile(string path, string expectedSha1, long expectedSize)
        {
            if (!File.Exists(path)) return false;
            FileInfo fi = new FileInfo(path);
            if (fi.Length != expectedSize) return false;

            using (var stream = File.OpenRead(path))
            using (var sha1 = new SHA1Managed())
            {
                byte[] hash = sha1.ComputeHash(stream);
                string computed = BitConverter.ToString(hash).Replace("-", "").ToLower();
                return computed == expectedSha1;
            }
        }

        private string FindOldFormatDirectory(string baseDir, string versionName)
        {
            if (!Directory.Exists(baseDir)) return null;
            foreach (string dir in Directory.GetDirectories(baseDir))
            {
                string dirName = Path.GetFileName(dir);
                if (dirName.EndsWith("_" + versionName))
                    return dir;
            }
            return null;
        }
    }
}