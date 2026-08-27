using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace New_Launcher
{
    public static class ModApiService
    {
        static ModApiService()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 20;
        }

        /// <summary>
        /// 下载 JSON 数据，包含详细的错误处理
        /// </summary>
        private static string DownloadJson(string url)
        {
            using (WebClient client = new WebClient())
            {
                // 设置 UTF-8 编码，防止中文乱码
                client.Encoding = System.Text.Encoding.UTF8;
                client.Headers.Add("User-Agent", "JerryStudioLauncher/1.0");
                // 如需代理，取消注释并设置正确的代理地址
                // var proxy = new WebProxy("http://127.0.0.1:10809", false);
                // client.Proxy = proxy;

                try
                {
                    string json = client.DownloadString(url);

                    // 清理非法控制字符（保留 \t, \n, \r）
                    // 移除 ASCII 0-31 中除 9(\t), 10(\n), 13(\r) 之外的字符
                    char[] chars = json.ToCharArray();
                    int writeIndex = 0;
                    for (int i = 0; i < chars.Length; i++)
                    {
                        char c = chars[i];
                        if (c == '\t' || c == '\n' || c == '\r' || c >= 32)
                        {
                            chars[writeIndex++] = c;
                        }
                    }
                    string cleanJson = new string(chars, 0, writeIndex);

                    return cleanJson;
                }
                catch (WebException ex)
                {
                    // 处理网络异常（保持不变）
                    string errorDetail = "";
                    int statusCode = 0;
                    if (ex.Response != null)
                    {
                        var httpResponse = (HttpWebResponse)ex.Response;
                        statusCode = (int)httpResponse.StatusCode;
                        using (var stream = ex.Response.GetResponseStream())
                        using (var reader = new StreamReader(stream))
                        {
                            errorDetail = reader.ReadToEnd();
                        }
                    }

                    string friendlyMessage;
                    if (statusCode == 429)
                        friendlyMessage = "请求过于频繁，请稍后再试。";
                    else if (statusCode == 400)
                        friendlyMessage = "请求参数错误，请检查搜索条件。";
                    else if (statusCode == 404)
                        friendlyMessage = "未找到请求的资源。";
                    else if (statusCode == 0)
                        friendlyMessage = "网络连接失败，请检查网络或代理设置。";
                    else
                        friendlyMessage = $"HTTP {statusCode} 错误";

                    throw new Exception(
                        $"{friendlyMessage}\n详细：{ex.Message}\n响应内容：{errorDetail}",
                        ex
                    );
                }
            }
        }

        public static List<ModrinthMod> SearchModrinth(string keyword, string mcVersion, int page, int limit, ProjectType type, out int totalHits)
        {
            totalHits = 0;
            int offset = (page - 1) * limit;

            // 构建分类过滤（使用 project_type 更准确）
            string categoryFacet;
            switch (type)
            {
                case ProjectType.Mod: categoryFacet = "project_type:mod"; break;
                case ProjectType.ResourcePack: categoryFacet = "project_type:resourcepack"; break;
                case ProjectType.Shader: categoryFacet = "project_type:shader"; break;
                case ProjectType.DataPack: categoryFacet = "project_type:datapack"; break;
                case ProjectType.Modpack: categoryFacet = "project_type:modpack"; break;
                default: categoryFacet = "project_type:mod"; break;
            }

            // 完全忽略版本过滤（mcVersion 不使用）
            string facets = "[[" + "\"" + categoryFacet + "\"" + "]]";

            // 构建 URL：根据 keyword 是否为空决定是否添加 query 参数
            string url;
            if (string.IsNullOrEmpty(keyword))
            {
                // 关键词为空：省略 query 参数，仅使用 facets 和 index=downloads
                url = "https://api.modrinth.com/v2/search?index=downloads&limit=" + limit
                    + "&offset=" + offset + "&facets=" + Uri.EscapeDataString(facets);
            }
            else
            {
                // 有关键词：正常添加 query 参数
                url = "https://api.modrinth.com/v2/search?query=" + Uri.EscapeDataString(keyword)
                    + "&index=downloads&limit=" + limit + "&offset=" + offset
                    + "&facets=" + Uri.EscapeDataString(facets);
            }

            Console.WriteLine("[ModApi] 请求 URL: " + url);

            string json = DownloadJson(url);
            JObject obj = JObject.Parse(json);
            totalHits = obj["total_hits"]?.Value<int>() ?? 0;
            JArray hits = obj["hits"] as JArray;

            List<ModrinthMod> results = new List<ModrinthMod>();
            if (hits == null) return results;

            foreach (JToken hit in hits)
            {
                results.Add(new ModrinthMod
                {
                    Id = hit["project_id"]?.ToString(),
                    Title = hit["title"]?.ToString(),
                    Description = hit["description"]?.ToString(),
                    IconUrl = hit["icon_url"]?.ToString(),
                    Downloads = hit["downloads"]?.Value<int>() ?? 0,
                    Author = hit["author"]?.ToString(),
                    ProjectType = hit["project_type"]?.ToString()
                });
            }
            return results;
        }

        public static List<ModVersion> GetModVersions(string modId)
        {
            if (string.IsNullOrEmpty(modId))
                throw new ArgumentException("Mod ID 不能为空", nameof(modId));

            modId = modId.Trim();
            string url = $"https://api.modrinth.com/v2/project/{modId}/version";
            System.Diagnostics.Debug.WriteLine($"[ModApi] 请求版本列表: {url}");

            try
            {
                string json = DownloadJson(url);
                JArray array = JArray.Parse(json);
                var versions = new List<ModVersion>();
                foreach (var item in array)
                {
                    var version = new ModVersion
                    {
                        Id = item["id"]?.ToString(),
                        ProjectId = item["project_id"]?.ToString(),
                        VersionNumber = item["version_number"]?.ToString(),
                        Changelog = item["changelog"]?.ToString(),
                        DatePublished = DateTime.Parse(item["date_published"]?.ToString() ?? DateTime.Now.ToString()),
                        Downloads = item["downloads"]?.Value<int>() ?? 0,
                        VersionType = item["version_type"]?.ToString(),
                        Featured = item["featured"]?.Value<bool>() ?? false,
                        GameVersions = new List<string>(),
                        Loaders = new List<string>(),
                        Files = new List<ModFile>()
                    };

                    var gameVersions = item["game_versions"] as JArray;
                    if (gameVersions != null)
                    {
                        foreach (var gv in gameVersions)
                            version.GameVersions.Add(gv.ToString());
                    }

                    var loaders = item["loaders"] as JArray;
                    if (loaders != null)
                    {
                        foreach (var loader in loaders)
                            version.Loaders.Add(loader.ToString());
                    }

                    var files = item["files"] as JArray;
                    if (files != null)
                    {
                        foreach (var file in files)
                        {
                            var modFile = new ModFile
                            {
                                Url = file["url"]?.ToString(),
                                Filename = file["filename"]?.ToString(),
                                Primary = file["primary"]?.Value<bool>() ?? false,
                                Size = file["size"]?.Value<int>() ?? 0,
                                Sha1 = file["hashes"]?["sha1"]?.ToString(),
                                Sha512 = file["hashes"]?["sha512"]?.ToString(),
                                GameVersions = new List<string>(),
                                Loaders = new List<string>()
                            };

                            var fileGameVersions = file["game_versions"] as JArray;
                            if (fileGameVersions != null)
                            {
                                foreach (var fgv in fileGameVersions)
                                    modFile.GameVersions.Add(fgv.ToString());
                            }

                            var fileLoaders = file["loaders"] as JArray;
                            if (fileLoaders != null)
                            {
                                foreach (var fl in fileLoaders)
                                    modFile.Loaders.Add(fl.ToString());
                            }

                            version.Files.Add(modFile);
                        }
                    }

                    versions.Add(version);
                }
                return versions;
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                    throw new Exception($"Mod ID '{modId}' 不存在或已被删除。");
                throw;
            }
        }
    }
}