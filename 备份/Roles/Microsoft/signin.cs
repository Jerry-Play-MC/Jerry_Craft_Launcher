using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Reflection; // 新增

namespace MinecraftLauncher
{
    public class MinecraftAuthenticator
    {
        static MinecraftAuthenticator()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        }

        // ========== 硬编码配置 ==========
        private const string DEFAULT_CLIENT_ID = "04bc9a34-3d65-4526-9201-28bca0c4bef7";
        private static readonly string[] DEFAULT_SCOPES = new[] { "XboxLive.signin", "offline_access" };

        private readonly string _clientId;
        private readonly string[] _scopes;
        private readonly string _clientToken;

        public Action<string, string> OnUserCodeReceived { get; set; }

        // 无参构造函数
        public MinecraftAuthenticator() : this(DEFAULT_CLIENT_ID, DEFAULT_SCOPES)
        {
        }

        public MinecraftAuthenticator(string clientId, string[] scopes = null)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            _clientId = clientId;
            _scopes = scopes ?? DEFAULT_SCOPES;
            _clientToken = Guid.NewGuid().ToString("N");
        }

        // ========== 公开方法 ==========

        /// <summary>
        /// 自动保存到默认路径：启动器目录\Launcher Setting\Roles\Microsoft\玩家名.json
        /// </summary>
        public string AuthenticateAndSave()
        {
            return AuthenticateAndSave(null);
        }

        /// <summary>
        /// 认证并保存账号信息。
        /// 若 filePath 为 null 或空，则执行完整登录并保存到默认路径；
        /// 若 filePath 非空，则按原逻辑（检查文件、刷新或完整登录）。
        /// </summary>
        public string AuthenticateAndSave(string filePath)
        {
            // ===== 情况1：未指定路径 → 直接完整登录（无刷新检测） =====
            if (string.IsNullOrEmpty(filePath))
            {
                DeviceAuthResult azureResult = GetAzureTokenByDeviceCode();
                MinecraftAuthResult mcResult = ExchangeToMinecraft(azureResult.access_token);
                string defaultPath = GetDefaultAccountPath(mcResult.Username);
                SaveAccount(defaultPath, mcResult, azureResult.refresh_token);
                return defaultPath;
            }

            // ===== 情况2：指定了路径 → 原逻辑（检查文件，刷新或登录） =====
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath, Encoding.UTF8);
                    var serializer = new JavaScriptSerializer();
                    var data = serializer.Deserialize<Dictionary<string, object>>(json);

                    string accessToken = data["accessToken"]?.ToString();
                    string refreshToken = data["refreshToken"]?.ToString();
                    // string clientToken = data["clientToken"]?.ToString();
                    // string uuid = data["uuid"]?.ToString();
                    // string username = data["username"]?.ToString();
                    // string xuid = data["xuid"]?.ToString();

                    if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
                    {
                        if (IsAccessTokenValid(accessToken))
                        {
                            return filePath; // 有效
                        }
                        else
                        {
                            try
                            {
                                RefreshAzureToken(refreshToken, out string newAccessToken, out string newRefreshToken);
                                var newMcResult = ExchangeToMinecraft(newAccessToken);
                                UpdateAccountFile(filePath, newMcResult, newRefreshToken);
                                return filePath;
                            }
                            catch
                            {
                                // 刷新失败，回退到完整登录
                            }
                        }
                    }
                }
                catch { /* 文件损坏，回退到完整登录 */ }
            }

            // 完整设备码登录
            DeviceAuthResult azureResultFull = GetAzureTokenByDeviceCode();
            MinecraftAuthResult mcResultFull = ExchangeToMinecraft(azureResultFull.access_token);
            SaveAccount(filePath, mcResultFull, azureResultFull.refresh_token);
            return filePath;
        }

        // ========== 私有辅助方法 ==========

        /// <summary>
        /// 构造默认账号文件路径：启动器目录\Launcher Setting\Roles\Microsoft\用户名.json
        /// </summary>
        private string GetDefaultAccountPath(string username)
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string folder = Path.Combine(Path.Combine(Path.Combine(exeDir, "Launcher Setting"), "Roles"), "Microsoft");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, username + ".json");
        }

        // ----- 以下方法保持不变（令牌验证、刷新、设备码、兑换、保存等） -----

        private bool IsAccessTokenValid(string accessToken)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://api.minecraftservices.com/minecraft/profile");
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + accessToken;
                request.Timeout = 5000;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        private void RefreshAzureToken(string refreshToken, out string accessToken, out string newRefreshToken)
        {
            const string tokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
            string postData = "client_id=" + Uri.EscapeDataString(_clientId) +
                              "&refresh_token=" + Uri.EscapeDataString(refreshToken) +
                              "&grant_type=refresh_token" +
                              "&scope=" + Uri.EscapeDataString(string.Join(" ", _scopes));

            string json = PostForm(tokenUrl, postData);
            var result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            if (result.ContainsKey("error"))
                throw new Exception("刷新失败: " + result["error"] + " - " + result["error_description"]);

            accessToken = result["access_token"].ToString();
            newRefreshToken = result["refresh_token"].ToString();
        }

        private DeviceAuthResult GetAzureTokenByDeviceCode()
        {
            const string deviceCodeUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/devicecode";
            const string tokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

            string deviceData = "client_id=" + Uri.EscapeDataString(_clientId) +
                                "&scope=" + Uri.EscapeDataString(string.Join(" ", _scopes));

            string deviceJson = PostForm(deviceCodeUrl, deviceData);
            var deviceInfo = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(deviceJson);

            string deviceCode = deviceInfo["device_code"].ToString();
            string userCode = deviceInfo["user_code"].ToString();
            string verificationUri = deviceInfo["verification_uri"].ToString();
            int interval = Convert.ToInt32(deviceInfo["interval"]);
            int expiresIn = Convert.ToInt32(deviceInfo["expires_in"]);

            try { System.Diagnostics.Process.Start(verificationUri); } catch { }

            if (OnUserCodeReceived != null)
                OnUserCodeReceived(userCode, verificationUri);
            else
                Console.WriteLine("请访问 " + verificationUri + " 并输入代码: " + userCode);

            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalSeconds < expiresIn)
            {
                string tokenData = "client_id=" + Uri.EscapeDataString(_clientId) +
                                   "&device_code=" + Uri.EscapeDataString(deviceCode) +
                                   "&grant_type=urn:ietf:params:oauth:grant-type:device_code";

                try
                {
                    string tokenJson = PostForm(tokenUrl, tokenData);
                    var result = new JavaScriptSerializer().Deserialize<DeviceAuthResult>(tokenJson);
                    if (!string.IsNullOrEmpty(result.access_token))
                        return result;
                }
                catch (WebException ex)
                {
                    using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        string errorJson = reader.ReadToEnd();
                        var errorDict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(errorJson);
                        if (errorDict.ContainsKey("error"))
                        {
                            string errorCode = errorDict["error"].ToString();
                            if (errorCode == "authorization_pending")
                            {
                                System.Threading.Thread.Sleep(interval * 1000);
                                continue;
                            }
                            else if (errorCode == "slow_down")
                            {
                                interval += 5;
                                System.Threading.Thread.Sleep(interval * 1000);
                                continue;
                            }
                            else
                            {
                                throw new Exception("服务器错误: " + errorCode + " - " + errorDict["error_description"]);
                            }
                        }
                        else throw;
                    }
                }
            }
            throw new TimeoutException("用户登录超时。");
        }

        private MinecraftAuthResult ExchangeToMinecraft(string azureAccessToken)
        {
            string xblToken = GetXboxLiveToken(azureAccessToken, out string xuid);
            string xstsToken = GetXSTSToken(xblToken);
            string mcToken = GetMinecraftToken(xstsToken);
            var profile = GetMinecraftProfile(mcToken);
            profile.Xuid = xuid;
            return profile;
        }

        private string GetXboxLiveToken(string azureToken, out string xuid)
        {
            xuid = null;

            var payload = new Dictionary<string, object>();
            var properties = new Dictionary<string, object>();
            properties.Add("AuthMethod", "RPS");
            properties.Add("SiteName", "user.auth.xboxlive.com");
            properties.Add("RpsTicket", "d=" + azureToken);
            payload.Add("Properties", properties);
            payload.Add("RelyingParty", "http://auth.xboxlive.com");
            payload.Add("TokenType", "JWT");

            string json = PostJson("https://user.auth.xboxlive.com/user/authenticate", payload);
            var result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);

            if (result.ContainsKey("DisplayClaims"))
            {
                var displayClaims = result["DisplayClaims"] as Dictionary<string, object>;
                if (displayClaims != null && displayClaims.ContainsKey("xui"))
                {
                    var xuiArray = displayClaims["xui"] as object[];
                    if (xuiArray != null && xuiArray.Length > 0)
                    {
                        var xui = xuiArray[0] as Dictionary<string, object>;
                        if (xui != null && xui.ContainsKey("xid"))
                        {
                            xuid = xui["xid"].ToString();
                        }
                    }
                }
            }

            return result["Token"].ToString();
        }

        private string GetXSTSToken(string xblToken)
        {
            var payload = new Dictionary<string, object>();
            var properties = new Dictionary<string, object>();
            properties.Add("SandboxId", "RETAIL");
            properties.Add("UserTokens", new string[] { xblToken });
            payload.Add("Properties", properties);
            payload.Add("RelyingParty", "rp://api.minecraftservices.com/");
            payload.Add("TokenType", "JWT");

            string json = PostJson("https://xsts.auth.xboxlive.com/xsts/authorize", payload);
            var result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);

            if (result.ContainsKey("XErr"))
            {
                string err = result["XErr"].ToString();
                if (err == "2148916233") throw new Exception("儿童账户需家长授权。");
                if (err == "2148916235") throw new Exception("该地区不被支持。");
                throw new Exception("XSTS错误: " + err);
            }
            return result["Token"].ToString();
        }

        private string GetMinecraftToken(string xstsToken)
        {
            var payload = new Dictionary<string, object>();
            payload.Add("identityToken", "XBL3.0 x=" + xstsToken);

            string json = PostJson("https://api.minecraftservices.com/authentication/login_with_xbox", payload);
            var result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            return result["access_token"].ToString();
        }

        private MinecraftAuthResult GetMinecraftProfile(string mcToken)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.minecraftservices.com/minecraft/profile");
            request.Method = "GET";
            request.Headers["Authorization"] = "Bearer " + mcToken;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                return new MinecraftAuthResult
                {
                    AccessToken = mcToken,
                    Uuid = data["id"].ToString(),
                    Username = data["name"].ToString()
                };
            }
        }

        private void SaveAccount(string filePath, MinecraftAuthResult mcResult, string refreshToken)
        {
            var accountObj = new
            {
                accessToken = mcResult.AccessToken,
                refreshToken = refreshToken,
                clientToken = _clientToken,
                uuid = mcResult.Uuid,
                username = mcResult.Username,
                xuid = mcResult.Xuid,
                userProperties = new { }
            };
            string json = new JavaScriptSerializer().Serialize(accountObj);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        private void UpdateAccountFile(string filePath, MinecraftAuthResult mcResult, string newRefreshToken)
        {
            string oldJson = File.ReadAllText(filePath, Encoding.UTF8);
            var oldData = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(oldJson);

            var accountObj = new
            {
                accessToken = mcResult.AccessToken,
                refreshToken = newRefreshToken,
                clientToken = oldData["clientToken"].ToString(),
                uuid = oldData["uuid"].ToString(),
                username = oldData["username"].ToString(),
                xuid = oldData["xuid"].ToString(),
                userProperties = new { }
            };
            string json = new JavaScriptSerializer().Serialize(accountObj);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        // ----- HTTP 辅助 -----
        private string PostForm(string url, string formData)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "MinecraftLauncher/1.0";
            byte[] data = Encoding.UTF8.GetBytes(formData);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream()) stream.Write(data, 0, data.Length);
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private string PostJson(string url, object payload)
        {
            string jsonData = new JavaScriptSerializer().Serialize(payload);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.UserAgent = "MinecraftLauncher/1.0";
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream()) stream.Write(data, 0, data.Length);
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        // ----- 内部类 -----
        private class DeviceAuthResult
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public string id_token { get; set; }
            public int expires_in { get; set; }
            public string scope { get; set; }
            public string token_type { get; set; }
        }

        private class MinecraftAuthResult
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public string Uuid { get; set; }
            public string Username { get; set; }
            public string Xuid { get; set; }
        }
    }
}