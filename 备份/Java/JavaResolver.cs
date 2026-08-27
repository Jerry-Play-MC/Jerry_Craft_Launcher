using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace New_Launcher
{
    /// <summary>
    /// 启动时专用：自动解析并获取 Java 可执行文件路径（用户指定优先，否则自动下载）
    /// </summary>
    public static class JavaResolver
    {
        public static string GetJavaPath(LaunchContext context, string minecraftDir, string versionId)
        {
            // 1. 用户手动指定的 Java 优先，且必须有效
            if (!string.IsNullOrEmpty(context.JavaPath) && File.Exists(context.JavaPath))
            {
                Console.WriteLine("[Java] 使用用户指定的 Java: " + context.JavaPath);
                return context.JavaPath;
            }

            if (!string.IsNullOrEmpty(context.JavaPath))
            {
                throw new Exception($"用户指定的 Java 路径无效：{context.JavaPath}，请检查设置。");
            }

            // 2. 从版本 JSON 中提取真实游戏版本号
            string versionFolder = Path.Combine(Path.Combine(minecraftDir, "versions"), versionId);
            string versionJsonPath = Path.Combine(versionFolder, versionId + ".json");

            if (!File.Exists(versionJsonPath))
                throw new Exception("未找到版本 JSON: " + versionJsonPath);

            string jsonText = File.ReadAllText(versionJsonPath);
            var serializer = new JavaScriptSerializer();
            var root = serializer.Deserialize<Dictionary<string, object>>(jsonText);

            string gameVersion = null;
            // ★★★ 使用 TryGetValue 代替直接索引 ★★★
            if (root.TryGetValue("minecraftVersion", out object mcVerObj))
                gameVersion = mcVerObj?.ToString();
            else if (root.TryGetValue("inheritsFrom", out object inheritObj))
                gameVersion = inheritObj?.ToString();
            else if (root.TryGetValue("id", out object idObj))
                gameVersion = ExtractGameVersionFromId(idObj?.ToString());

            if (string.IsNullOrEmpty(gameVersion))
                throw new Exception("无法从版本 JSON 中提取 Minecraft 游戏版本");

            Console.WriteLine("[Java] 检测到游戏版本: " + gameVersion + "，确保运行时存在...");

            // 3. 调用 JavaRuntimeManager
            var manager = new MinecraftJavaRuntime.JavaRuntimeManager(Path.Combine(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Launcher Setting"), "Java"));
            string resolvedPath = manager.EnsureJavaRuntime(gameVersion);

            Console.WriteLine("[Java] 已就绪: " + resolvedPath);
            return resolvedPath;
        }

        private static string ExtractGameVersionFromId(string versionId)
        {
            var match = Regex.Match(versionId, @"neoforge-(\d+)\.(\d+)\.\d+");
            if (match.Success)
                return $"1.{match.Groups[1].Value}.{match.Groups[2].Value}";
            // 可扩展其他加载器格式
            return versionId; // 保底
        }
    }
}