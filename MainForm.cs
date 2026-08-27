using MinecraftLauncher;
using New_Launcher.Error;
using New_Launcher.Roles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using static New_Launcher.BaseLauncher;
using AssetsForm = New_Launcher.OutPutMinecraftAssets.OutPutMinecraftAssets;

namespace New_Launcher
{
    public partial class MainForm : Form
    {
        // 路径常量
        private static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string SettingsDir = Path.Combine(LauncherPath, "Launcher Setting");
        private static string MinecraftDir = Path.Combine(LauncherPath, ".minecraft");
        private static string javaDir = Path.Combine(SettingsDir, "Java");
        private static string RolesDir = Path.Combine(SettingsDir, "Roles");
        private bool _isLoading = false;

        // 用户自定义 Java 路径（null 表示自动选择）
        private string customJavaPath = null;

        // 当前玩家信息
        private string CurrentPlayerName;
        private string CurrentUUID;

        // 在 MainForm 类内部添加（位置随意，建议放在字段声明区域）
        public class RoleEntry
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
        }

        public MainForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;

            this.FormClosed += MainForm_FormClosed;

            // 绑定版本选择变更事件
            this.comboBox_Version.SelectedIndexChanged += comboBox_Version_SelectedIndexChanged;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 加载自定义 Java 路径（从 Settings.json 读取）
            string settingsDir = Path.Combine(LauncherPath, "Launcher Setting");
            string settingsFile = Path.Combine(settingsDir, "Settings.json");
            string customJavaPath = null;

            if (File.Exists(settingsFile))
            {
                try
                {
                    string jsonContent = File.ReadAllText(settingsFile);
                    JObject settingsJson = JObject.Parse(jsonContent);
                    // 提取 GameSettings.Java 字段
                    var javaToken = settingsJson.SelectToken("GameSettings.Java");
                    if (javaToken != null)
                    {
                        customJavaPath = javaToken.Value<string>();
                        if (string.IsNullOrEmpty(customJavaPath) && !File.Exists(customJavaPath))
                        {
                            customJavaPath = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MainForm] 读取 Settings.json 失败: {ex.Message}");
                    customJavaPath = null;
                }
            }

            // 加载自定义 Java 路径
            LoadCustomJavaPath();

            // 1. 加载默认角色
            LoadDefaultRole();

            // 2. 加载已安装版本列表
            LoadVersionList();
        }

        private void LoadCustomJavaPath()
        {
            string settingsFile = Path.Combine(SettingsDir, "Settings.json");
            if (File.Exists(settingsFile))
            {
                try
                {
                    string jsonContent = File.ReadAllText(settingsFile);
                    JObject settingsJson = JObject.Parse(jsonContent);
                    var javaToken = settingsJson.SelectToken("GameSettings.Java");
                    if (javaToken != null)
                    {
                        string folder = javaToken.Value<string>();
                        if (!string.IsNullOrEmpty(folder))
                        {
                            string fullPath = Path.Combine(Path.Combine(Path.Combine(javaDir, folder), "bin"), "javaw.exe");
                            if (File.Exists(fullPath))
                            {
                                customJavaPath = fullPath;
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MainForm] 读取 Settings.json 失败: {ex.Message}");
                }
            }
            customJavaPath = null;
        }

        /// <summary>
        /// 加载默认角色信息
        /// </summary>
        private void LoadDefaultRole()
        {
            try
            {
                // 1. 从 Settings.json 读取默认角色 ID
                string defaultId = GetDefaultRoleId();
                if (string.IsNullOrEmpty(defaultId))
                {
                    // 尝试从旧文件迁移（兼容）
                    string oldTacitPath = Path.Combine(RolesDir, "TacitlyApproveRole.txt");
                    if (File.Exists(oldTacitPath))
                    {
                        string oldName = File.ReadAllText(oldTacitPath).Trim();
                        if (!string.IsNullOrEmpty(oldName))
                        {
                            string oldUuid = null;
                            string oldJsonPath = Path.Combine(RolesDir, oldName + ".json");
                            if (File.Exists(oldJsonPath))
                            {
                                try
                                {
                                    string content = File.ReadAllText(oldJsonPath);
                                    var obj = JObject.Parse(content);
                                    oldUuid = obj["uuid"]?.Value<string>();
                                }
                                catch { }
                            }
                            if (string.IsNullOrEmpty(oldUuid))
                                oldUuid = Guid.NewGuid().ToString();
                            AddRole(oldUuid, "Offline", oldName);
                            SetDefaultRole(oldUuid);
                            File.Delete(oldTacitPath);
                            defaultId = oldUuid;
                        }
                    }
                }

                if (string.IsNullOrEmpty(defaultId))
                {
                    MessageBox.Show("未找到默认角色，请先创建角色。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 2. 从角色列表中查找该 ID
                var roles = GetRoleList();
                var target = roles.Find(r => r.Id == defaultId);
                if (target == null)
                {
                    MessageBox.Show("默认角色已不存在，请重新设置。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string roleType = target.Type;
                string username = target.Name;

                // 3. 加载对应的 JSON 文件
                string roleFile = null;
                if (roleType == "Offline")
                {
                    // ★ 统一用 Id (UUID) 做文件名 ★
                    roleFile = Path.Combine(RolesDir, defaultId + ".json");
                    // 如果按 UUID 找不到，尝试按用户名找（兼容旧版本）
                    if (!File.Exists(roleFile))
                    {
                        string fallbackFile = Path.Combine(RolesDir, username + ".json");
                        if (File.Exists(fallbackFile))
                            roleFile = fallbackFile;
                    }
                }
                else if (roleType == "Microsoft")
                {
                    // ★ 用 UUID 做文件名 ★
                    roleFile = Path.Combine(Path.Combine(RolesDir, "Microsoft"), defaultId + ".json");
                    // 如果按 UUID 找不到，尝试按用户名找（兼容旧版本）
                    if (!File.Exists(roleFile))
                    {
                        string fallbackFile = Path.Combine(Path.Combine(RolesDir, "Microsoft"), username + ".json");
                        if (File.Exists(fallbackFile))
                            roleFile = fallbackFile;
                    }
                }

                if (string.IsNullOrEmpty(roleFile) || !File.Exists(roleFile))
                {
                    MessageBox.Show($"角色档案不存在，请重新创建。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string jsonContent = File.ReadAllText(roleFile);
                JObject roleObj = JObject.Parse(jsonContent);

                // 读取基本信息
                string playerId = roleObj["username"]?.Value<string>();
                // ★ 如果找不到 username，尝试兼容旧格式的 PlayerID
                if (string.IsNullOrEmpty(playerId))
                {
                    playerId = roleObj["PlayerID"]?.Value<string>();
                }
                string uuid = roleObj["uuid"]?.Value<string>();

                if (!string.IsNullOrEmpty(playerId))
                {
                    CurrentPlayerName = playerId.Trim();
                    label_PlayerID.Text = "玩家ID：" + CurrentPlayerName + (roleType == "Microsoft" ? " (M)" : "");
                }
                else
                {
                    MessageBox.Show("角色档案格式不正确，未找到玩家名称。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (!string.IsNullOrEmpty(uuid))
                {
                    CurrentUUID = uuid.Trim();
                    label_uuid.Text = "UUID：" + CurrentUUID;
                }
                else
                {
                    MessageBox.Show("角色档案格式不正确，未找到 UUID。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // ★ 新增：如果是 Microsoft 账号，读取并验证 Token ★
                if (roleType == "Microsoft")
                {
                    string accessToken = roleObj["accessToken"]?.Value<string>();
                    string refreshToken = roleObj["refreshToken"]?.Value<string>();

                    if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    {
                        MessageBox.Show("Microsoft 账号令牌不完整，请重新登录。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 验证 accessToken 是否有效
                    if (!IsAccessTokenValid(accessToken))
                    {
                        // Token 无效，尝试刷新
                        try
                        {
                            var authenticator = new MinecraftAuthenticator();
                            authenticator.RefreshAzureToken(refreshToken, out string newToken, out string newRefreshToken);
                            UpdateTokenInFile(roleFile, newToken, newRefreshToken);
                            MessageBox.Show("正版令牌已自动刷新。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"令牌刷新失败，请重新登录。\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载角色信息失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"[MainForm] 加载角色异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载已安装的版本列表
        /// </summary>
        private void LoadVersionList()
        {
            try
            {
                string versionsDir = Path.Combine(MinecraftDir, "versions");
                if (!Directory.Exists(versionsDir))
                {
                    Directory.CreateDirectory(versionsDir);
                    return;
                }

                var versionFolders = Directory.GetDirectories(versionsDir);
                var versionNames = new List<string>();
                foreach (var folder in versionFolders)
                {
                    string versionName = Path.GetFileName(folder);
                    string jsonPath = Path.Combine(folder, versionName + ".json");
                    if (File.Exists(jsonPath))
                        versionNames.Add(versionName);
                }

                // ★ 打印所有版本名称，查看是否有异常字符或空值
                Console.WriteLine("版本列表：");
                foreach (var v in versionNames)
                    Console.WriteLine($"  '{v}'");

                // ★ 安全排序
                versionNames.Sort((a, b) =>
                {
                    string sa = a ?? "";
                    string sb = b ?? "";
                    return string.Compare(sa, sb, StringComparison.Ordinal);
                });

                comboBox_Version.DataSource = versionNames;
                if (comboBox_Version.Items.Count > 0)
                    comboBox_Version.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[LoadVersionList] 异常：" + ex.ToString());
                MessageBox.Show($"加载版本列表失败：{ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---- 版本选择变更事件 ----
        private void comboBox_Version_SelectedIndexChanged(object sender, EventArgs e)
        {
            string versionId = comboBox_Version.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(versionId)) return;

            string loaderType = MinecraftCore.GetLoaderType(versionId);
            string gameVersion = ExtractGameVersion(versionId);
            Console.WriteLine($"[版本选择] 版本: {versionId}，加载器类型: {loaderType}，游戏版本: {gameVersion}");
        }

        /// <summary>
        /// 从版本 ID 中提取实际 Minecraft 版本号
        /// </summary>
        private string ExtractGameVersion(string versionId)
        {
            // 处理 NeoForge
            if (versionId.StartsWith("neoforge-"))
            {
                var json = LoadVersionJson(versionId);
                if (json != null && json.ContainsKey("inheritsFrom"))
                    return json["inheritsFrom"].ToString();
                string neoVer = versionId.Substring(9);
                string[] parts = neoVer.Split('.');
                if (parts.Length >= 2)
                    return "1." + parts[0] + "." + parts[1];
                else
                    return "1." + parts[0];
            }

            // 处理 Fabric / Quilt
            if (versionId.StartsWith("fabric-loader-") || versionId.StartsWith("quilt-loader-"))
            {
                int lastDash = versionId.LastIndexOf('-');
                if (lastDash > 0)
                    return versionId.Substring(lastDash + 1);
            }

            // 处理 Forge（兼容两种格式）
            if (versionId.EndsWith("-forge") || versionId.Contains("-forge-"))
            {
                int idx = versionId.LastIndexOf("-forge");
                if (idx > 0)
                    return versionId.Substring(0, idx);
            }

            // 处理 NeoForge 的另一种格式
            if (versionId.EndsWith("-neoforge") || versionId.Contains("-neoforge-"))
            {
                int idx = versionId.LastIndexOf("-neoforge");
                if (idx > 0)
                    return versionId.Substring(0, idx);
            }

            return versionId;
        }

        // ---- 启动游戏 ----
        private void button_StartGame_Click(object sender, EventArgs e)
        {
            string selectedVersion = comboBox_Version.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(CurrentPlayerName) || string.IsNullOrEmpty(CurrentUUID))
            {
                MessageBox.Show("玩家信息不完整，请检查角色配置。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ★ 新增：获取当前角色的类型和 Token ★
            string defaultId = GetDefaultRoleId();
            var roles = GetRoleList();
            var target = roles.Find(r => r.Id == defaultId);
            bool isMicrosoft = target?.Type == "Microsoft";

            string accessToken = "0"; // 离线模式默认值

            if (isMicrosoft)
            {
                // 从 Microsoft 角色文件中读取 accessToken
                string roleFile = Path.Combine(Path.Combine(RolesDir, "Microsoft"), defaultId + ".json");
                if (!File.Exists(roleFile))
                {
                    // 尝试用用户名查找（兼容旧版本）
                    roleFile = Path.Combine(Path.Combine(RolesDir, "Microsoft"), CurrentPlayerName + ".json");
                }

                if (File.Exists(roleFile))
                {
                    try
                    {
                        string json = File.ReadAllText(roleFile);
                        JObject obj = JObject.Parse(json);
                        string token = obj["accessToken"]?.Value<string>();
                        string refreshToken = obj["refreshToken"]?.Value<string>();

                        if (!string.IsNullOrEmpty(token))
                        {
                            // 验证 token 是否有效
                            if (IsAccessTokenValid(token))
                            {
                                accessToken = token;
                            }
                            else if (!string.IsNullOrEmpty(refreshToken))
                            {
                                // Token 无效，尝试刷新
                                try
                                {
                                    var authenticator = new MinecraftAuthenticator();
                                    authenticator.RefreshAzureToken(refreshToken, out string newToken, out string newRefreshToken);
                                    accessToken = newToken;
                                    UpdateTokenInFile(roleFile, newToken, newRefreshToken);
                                    MessageBox.Show("正版令牌已自动刷新。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                catch
                                {
                                    MessageBox.Show("正版登录已过期，请重新登录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                            else
                            {
                                MessageBox.Show("正版登录已过期，请重新登录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("正版登录信息不完整，请重新登录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"读取正版令牌失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("正版角色文件不存在，请重新登录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            button_StartGame.Enabled = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, args) =>
            {
                try
                {
                    // 1. 解析版本信息（使用自己的识别逻辑）
                    string loaderType, gameVersion, loaderVersion;
                    ParseLoaderInfo(selectedVersion, out loaderType, out gameVersion, out loaderVersion);

                    string actualVersionId = selectedVersion;

                    try
                    {
                        VersionCompleter.Complete(actualVersionId, MinecraftDir, parallel: true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("补全失败: " + ex.Message);
                    }

                    // 4. 读取设置文件的版本隔离开关
                    bool IsVersionIsolated;
                    string settingsFilePath = Path.Combine(Path.Combine(LauncherPath, "Launcher Setting"), "Settings.json");
                    if (!File.Exists(settingsFilePath))
                        IsVersionIsolated = true;
                    else
                    {
                        string jsonContent = File.ReadAllText(settingsFilePath);
                        JObject settingsJson = JObject.Parse(jsonContent);
                        IsVersionIsolated = settingsJson.SelectToken("GameSettings.VersionIsolated")?.Value<bool>() ?? true;
                    }
                    Console.WriteLine($"[启动] 版本隔离开关状态: {IsVersionIsolated}");

                    // 5. 构造启动上下文
                    var context = new LaunchContext
                    {
                        VersionId = actualVersionId,
                        GameVersion = gameVersion,
                        MinecraftDir = MinecraftDir,
                        VersionDataPath = Path.Combine(Path.Combine(MinecraftDir, "versions"), actualVersionId),
                        JavaPath = customJavaPath,
                        Username = CurrentPlayerName,
                        Uuid = CurrentUUID,
                        AccessToken = accessToken,  // ★ 使用获取到的 Token
                        UserType = isMicrosoft ? "msa" : "mojang",
                        MaxMemory = "2G",
                        InitMemory = "2G",
                        Width = 854,
                        Height = 480,
                        IsVersionIsolated = IsVersionIsolated
                    };

                    // 6. 根据 loaderType 选择 Launcher
                    ILauncher launcher;
                    switch (loaderType)
                    {
                        case "Vanilla":
                            launcher = new VanillaManager.Launcher();
                            break;
                        case "Forge":
                            launcher = new ForgeManager.Launcher();
                            break;
                        case "NeoForge":
                            launcher = new NeoForgeManager.Launcher();
                            break;
                        case "Fabric":
                            launcher = new FabricManager.Launcher();
                            break;
                        case "Quilt":
                            launcher = new QuiltManager.Launcher();
                            break;
                        default:
                            throw new Exception("未知加载器类型: " + loaderType);
                    }

                    launcher.Launch(context);
                }
                catch (Exception ex)
                {
                    args.Result = ex;
                }
            };

            worker.RunWorkerCompleted += (s, args) =>
            {
                button_StartGame.Enabled = true;
                string currentVersion = comboBox_Version.SelectedItem?.ToString() ?? "unknown";
                bool isIsolated = GetIsolatedStatus();

                if (args.Result is Exception ex)
                {
                    if (ex is GameLaunchException gameEx)
                    {
                        using (var errorForm = new ErrorForm(gameEx.AnalyzedMessage, gameEx.LogFilePath, MinecraftDir, currentVersion, isIsolated))
                            errorForm.ShowDialog();
                    }
                    else
                    {
                        using (var errorForm = new ErrorForm($"启动器内部错误：\n{ex.Message}", null, MinecraftDir, currentVersion, isIsolated))
                            errorForm.ShowDialog();
                    }
                }
            };

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// 解析版本 ID，提取加载器类型、基础游戏版本和加载器版本
        /// </summary>
        private void ParseLoaderInfo(string versionId, out string loaderType, out string gameVersion, out string loaderVersion)
        {
            loaderType = MinecraftCore.GetLoaderType(versionId);
            loaderVersion = null;

            var json = LoadVersionJson(versionId);
            if (json != null && json.ContainsKey("inheritsFrom"))
                gameVersion = json["inheritsFrom"].ToString();
            else
                gameVersion = ExtractGameVersion(versionId);

            if (loaderType == "Fabric")
            {
                string prefix = "fabric-loader-";
                if (versionId.StartsWith(prefix))
                {
                    string rest = versionId.Substring(prefix.Length);
                    int lastDash = rest.LastIndexOf('-');
                    if (lastDash > 0)
                        loaderVersion = rest.Substring(0, lastDash);
                }
            }
            else if (loaderType == "Quilt")
            {
                string prefix = "quilt-loader-";
                if (versionId.StartsWith(prefix))
                {
                    string rest = versionId.Substring(prefix.Length);
                    int lastDash = rest.LastIndexOf('-');
                    if (lastDash > 0)
                        loaderVersion = rest.Substring(0, lastDash);
                }
            }
            else if (loaderType == "NeoForge")
            {
                if (json != null)
                    loaderVersion = ExtractNeoForgeVersionFromJson(json);
                if (string.IsNullOrEmpty(loaderVersion))
                    loaderVersion = ScanNeoForgeVersionFromLibraries(gameVersion);
            }
            else if (loaderType == "Forge")
            {
                if (json != null)
                    loaderVersion = ExtractForgeVersionFromJson(json);
                if (string.IsNullOrEmpty(loaderVersion))
                    loaderVersion = ScanForgeVersionFromLibraries(gameVersion);
            }
        }

        /// <summary>
        /// 加载版本 JSON 文件
        /// </summary>
        private Dictionary<string, object> LoadVersionJson(string versionId)
        {
            string jsonPath = Path.Combine(Path.Combine(Path.Combine(MinecraftDir, "versions"), versionId), versionId + ".json");
            if (!File.Exists(jsonPath)) return null;
            try
            {
                string json = File.ReadAllText(jsonPath);
                return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            }
            catch
            {
                return null;
            }
        }

        private string ExtractNeoForgeVersionFromJson(Dictionary<string, object> root)
        {
            if (root == null) return null;
            if (!root.ContainsKey("libraries")) return null;
            var libraries = root["libraries"] as ArrayList;
            if (libraries == null) return null;

            foreach (var libObj in libraries)
            {
                var lib = libObj as Dictionary<string, object>;
                if (lib != null && lib.ContainsKey("name"))
                {
                    string name = lib["name"].ToString();
                    if (name.StartsWith("net.neoforged:neoforge:"))
                    {
                        string[] parts = name.Split(':');
                        if (parts.Length >= 3)
                            return parts[2];
                    }
                }
            }
            return null;
        }

        private string ScanNeoForgeVersionFromLibraries(string gameVersion)
        {
            string neoForgeDir = Path.Combine(Path.Combine(Path.Combine(Path.Combine(MinecraftDir, "libraries"), "net"), "neoforged"), "neoforge");
            if (!Directory.Exists(neoForgeDir)) return null;

            var dirs = Directory.GetDirectories(neoForgeDir);
            if (dirs.Length == 0) return null;
            Array.Sort(dirs, (a, b) => string.Compare(Path.GetFileName(b), Path.GetFileName(a), StringComparison.Ordinal));
            return Path.GetFileName(dirs[0]);
        }

        private string ExtractForgeVersionFromJson(Dictionary<string, object> root)
        {
            if (root == null) return null;
            if (!root.ContainsKey("libraries")) return null;
            var libraries = root["libraries"] as ArrayList;
            if (libraries == null) return null;
            foreach (var libObj in libraries)
            {
                var lib = libObj as Dictionary<string, object>;
                if (lib != null && lib.ContainsKey("name"))
                {
                    string name = lib["name"].ToString();
                    if (name.StartsWith("net.minecraftforge:forge:"))
                    {
                        string[] parts = name.Split(':');
                        if (parts.Length >= 3)
                            return parts[2];
                    }
                }
            }
            // 回退：从版本 ID 提取
            string id = root.ContainsKey("id") ? root["id"].ToString() : "";
            int idx = id.LastIndexOf("-forge-");
            if (idx > 0)
                return id.Substring(idx + 7);
            return null;
        }

        private string ScanForgeVersionFromLibraries(string gameVersion)
        {
            try
            {
                string forgeDir = Path.Combine(Path.Combine(Path.Combine(Path.Combine(MinecraftDir, "libraries"), "net"), "minecraftforge"), "forge");
                if (!Directory.Exists(forgeDir)) return null;
                var dirs = Directory.GetDirectories(forgeDir);
                if (dirs.Length == 0) return null;

                // ★ 安全排序：捕获 Version 解析异常，回退到字符串比较 ★
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
            catch (Exception ex)
            {
                Console.WriteLine($"[ScanForgeVersionFromLibraries] 异常: {ex.Message}");
                return null;
            }
        }

        // ---- 其他按钮事件 ----
        private void button_Settings_Click(object sender, EventArgs e)
        {
            this.Hide();
            Settings settings = new Settings();
            settings.ShowDialog();
            this.Show();

            // 重新加载自定义 Java 路径（可能被修改）
            LoadCustomJavaPath();

            // 重新加载版本列表（可能添加/重命名了版本）
            LoadVersionList();
        }

        private void button_DownloadVersions_Click(object sender, EventArgs e)
        {
            this.Hide();
            ChooseVersions chooseForm = new ChooseVersions();
            chooseForm.ShowDialog();
            this.Show();
            LoadVersionList();
        }

        private void button_DownloadMod_Click(object sender, EventArgs e)
        {
            this.Hide();
            string selectedVersion = comboBox_Version.SelectedItem?.ToString();
            bool isIsolated = GetIsolatedStatus();  // 已有方法
            ModManagerForm modForm = new ModManagerForm(selectedVersion, isIsolated);
            modForm.ShowDialog();
            this.Show();
            LoadVersionList();
        }

        private void button_Roles_Click(object sender, EventArgs e)
        {
            this.Hide();
            using (var manage = new ManageRoles())
            {
                manage.ShowDialog(this);
            }
            this.Show();
            // 刷新主界面显示（重新加载默认角色）
            LoadDefaultRole();
        }

        // ---- 窗体关闭事件 ----
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            MinecraftCore.KillGameProcess();
        }

        // ---- 辅助类：控制台重定向 ----
        public class ConsoleRedirector : TextWriter
        {
            private RichTextBox _textBox;
            public TextWriter OriginalOut { get; }

            public ConsoleRedirector(RichTextBox textBox)
            {
                _textBox = textBox;
                OriginalOut = Console.Out;
            }

            public override void Write(string value)
            {
                if (_textBox.InvokeRequired)
                    _textBox.Invoke(new Action(() => AppendText(value)));
                else
                    AppendText(value);
            }

            public override void WriteLine(string value)
            {
                Write(value + Environment.NewLine);
            }

            private void AppendText(string text)
            {
                _textBox.AppendText(text);
                _textBox.ScrollToCaret();
            }

            public override Encoding Encoding => Encoding.UTF8;
        }

        private bool GetIsolatedStatus()
        {
            string settingsFilePath = Path.Combine(Path.Combine(LauncherPath, "Launcher Setting"), "Settings.json");
            if (!File.Exists(settingsFilePath))
                return true; // 默认开启版本隔离

            try
            {
                string jsonContent = File.ReadAllText(settingsFilePath);
                JObject settingsJson = JObject.Parse(jsonContent);
                return settingsJson.SelectToken("GameSettings.VersionIsolated")?.Value<bool>() ?? true;
            }
            catch
            {
                return true;
            }
        }

        // 在 MainForm 类中添加
        public static string SettingsFilePath => Path.Combine(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Launcher Setting"), "Settings.json");

        /// <summary>
        /// 确保 Settings.json 存在并包含 Roles 结构
        /// </summary>
        private static void EnsureSettingsRoles()
        {
            string dir = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            JObject root;
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
                root = JObject.Parse(json);
            }
            else
            {
                root = new JObject();
            }

            if (root["Roles"] == null)
            {
                root["Roles"] = new JObject
                {
                    ["DefaultRoleId"] = null,
                    ["List"] = new JArray()
                };
                File.WriteAllText(SettingsFilePath, root.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 获取所有角色列表
        /// </summary>
        public static List<RoleEntry> GetRoleList()
        {
            EnsureSettingsRoles();
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            var root = JObject.Parse(json);
            var list = root["Roles"]["List"] as JArray;
            var result = new List<RoleEntry>();
            if (list != null)
            {
                foreach (var item in list)
                {
                    string id = item["Id"]?.Value<string>();
                    string type = item["Type"]?.Value<string>();
                    string name = item["Name"]?.Value<string>();
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(name))
                        result.Add(new RoleEntry { Id = id, Type = type, Name = name });
                }
            }
            return result;
        }

        /// <summary>
        /// 获取默认角色 UUID（若未设置则返回 null）
        /// </summary>
        public static string GetDefaultRoleId()
        {
            EnsureSettingsRoles();
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            var root = JObject.Parse(json);
            return root["Roles"]["DefaultRoleId"]?.Value<string>();
        }

        /// <summary>
        /// 添加一个新角色到 Settings.json（如果已存在则忽略）
        /// </summary>
        public static void AddRole(string id, string type, string name)
        {
            EnsureSettingsRoles();
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            var root = JObject.Parse(json);
            var list = root["Roles"]["List"] as JArray;

            // 检查是否已存在
            bool exists = false;
            foreach (var item in list)
            {
                if (item["Id"]?.Value<string>() == id)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                list.Add(new JObject
                {
                    ["Id"] = id,
                    ["Type"] = type,
                    ["Name"] = name
                });
                File.WriteAllText(SettingsFilePath, root.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
            }
        }

        /// <summary>
        /// 设置默认角色（通过 UUID）
        /// </summary>
        public static void SetDefaultRole(string id)
        {
            EnsureSettingsRoles();
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            var root = JObject.Parse(json);
            root["Roles"]["DefaultRoleId"] = id;
            File.WriteAllText(SettingsFilePath, root.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
        }

        /// <summary>
        /// 从 Settings.json 中移除角色（按 UUID）
        /// </summary>
        public static bool RemoveRole(string id)
        {
            EnsureSettingsRoles();
            string json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            var root = JObject.Parse(json);
            var list = root["Roles"]["List"] as JArray;
            if (list == null) return false;

            JObject toRemove = null;
            foreach (JObject item in list)
            {
                if (item["Id"]?.Value<string>() == id)
                {
                    toRemove = item;
                    break;
                }
            }
            if (toRemove == null) return false;

            list.Remove(toRemove);
            File.WriteAllText(SettingsFilePath, root.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
            return true;
        }

        // ---- 清除未使用的事件（若有） ----
        private void comboBox_Java_Click(object sender, EventArgs e)
        {
            // 该事件已由 PopulateJavaComboBox 处理，保留空方法以防绑定
        }

        private void button_OutPutMinecarftAsset_Click(object sender, EventArgs e)
        {
            var outPutMinecraftAssets = new AssetsForm();
            this.Hide();
            outPutMinecraftAssets.ShowDialog();
            this.Show();
        }

        /// <summary>
        /// 验证 Minecraft AccessToken 是否有效
        /// </summary>
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

        /// <summary>
        /// 更新角色文件中的 Token
        /// </summary>
        private void UpdateTokenInFile(string filePath, string newAccessToken, string newRefreshToken)
        {
            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                JObject obj = JObject.Parse(json);
                obj["accessToken"] = newAccessToken;
                obj["refreshToken"] = newRefreshToken;
                File.WriteAllText(filePath, obj.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
                Console.WriteLine($"[Token] 已更新: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Token] 更新失败: {ex.Message}");
                throw;
            }
        }
    }
}