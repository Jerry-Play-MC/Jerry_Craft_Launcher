using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace New_Launcher
{
    public partial class InputOfficialLauncher : Form
    {
        private static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string MinecraftDir = Path.Combine(LauncherPath, ".minecraft");
        private BackgroundWorker worker;
        private bool isIsolated;

        public InputOfficialLauncher()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void InputOfficialLauncher_Load(object sender, EventArgs e)
        {
            // 禁用关闭按钮（可选）
            this.ControlBox = false;

            // 1. 读取版本隔离状态
            isIsolated = GetVersionIsolated();

            // 2. 启动后台线程
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = false;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// 读取 Settings.json 中的 VersionIsolated 值
        /// </summary>
        private bool GetVersionIsolated()
        {
            try
            {
                string settingsPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Launcher Setting"), "Settings.json");
                if (!File.Exists(settingsPath))
                    return true; // 默认启用

                string json = File.ReadAllText(settingsPath);
                JObject root = JObject.Parse(json);
                return root.SelectToken("GameSettings.VersionIsolated")?.Value<bool>() ?? true;
            }
            catch
            {
                return true; // 出错时默认启用
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string officialDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            worker.ReportProgress(0, "准备复制...");

            if (isIsolated)
            {
                // 启用版本隔离：复制 assets、libraries 和 versions 下每个子文件夹
                worker.ReportProgress(5, "复制 assets...");
                CopyDirectoryMerge(Path.Combine(MinecraftDir, "assets"), Path.Combine(officialDir, "assets"), "assets");

                worker.ReportProgress(20, "复制 libraries...");
                CopyDirectoryMerge(Path.Combine(MinecraftDir, "libraries"), Path.Combine(officialDir, "libraries"), "libraries");

                // 处理 versions 下的每个版本子文件夹
                string srcVersions = Path.Combine(MinecraftDir, "versions");
                if (Directory.Exists(srcVersions))
                {
                    string[] versionFolders = Directory.GetDirectories(srcVersions);
                    int total = versionFolders.Length;
                    int done = 0;
                    foreach (string folder in versionFolders)
                    {
                        string versionName = Path.GetFileName(folder);
                        string dstVersionFolder = Path.Combine(Path.Combine(officialDir, "versions"), versionName);
                        worker.ReportProgress(20 + (int)((done / (double)total) * 60), $"复制版本：{versionName}");
                        CopyDirectoryMerge(folder, dstVersionFolder, versionName);
                        done++;
                    }
                }
            }
            else
            {
                // 未启用版本隔离：复制整个 .minecraft
                worker.ReportProgress(5, "复制整个 .minecraft...");
                CopyDirectoryMerge(MinecraftDir, officialDir, "整个 .minecraft");
            }

            // 最后：为每个复制的版本自动添加启动配置
            worker.ReportProgress(90, "更新启动配置...");
            string dstVersions = Path.Combine(officialDir, "versions");
            if (Directory.Exists(dstVersions))
            {
                foreach (string folder in Directory.GetDirectories(dstVersions))
                {
                    string versionName = Path.GetFileName(folder);
                    string jsonPath = Path.Combine(folder, versionName + ".json");
                    if (File.Exists(jsonPath))
                    {
                        AddProfileToOfficialLauncher(versionName, officialDir);
                    }
                }
            }

            worker.ReportProgress(100, "完成！");
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            label.Text = "当前任务：" + e.UserState?.ToString();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show($"导入出错：{e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.DialogResult = DialogResult.OK;
            Thread.Sleep(1000);
            this.Close();
        }

        /// <summary>
        /// 递归复制目录，同名文件跳过，同名文件夹合并，并更新进度（每复制一个文件更新一次）
        /// </summary>
        private void CopyDirectoryMerge(string sourceDir, string destDir, string taskName)
        {
            if (!Directory.Exists(sourceDir))
                return;

            Directory.CreateDirectory(destDir);

            // 1. 复制文件（跳过已存在）
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFile = Path.Combine(destDir, fileName);
                if (!File.Exists(destFile))
                {
                    File.Copy(filePath, destFile);
                    // 每复制一个文件，进度增加一点（这里简单用百分比基数，但实际频率高，可用增量）
                    // 由于无法预知总文件数，我们只显示任务描述，进度条由外部控制
                }
            }

            // 2. 递归子目录
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryMerge(subDir, destSubDir, taskName);
            }
        }

        /// <summary>
        /// 为指定版本在官方启动器的配置文件中添加一个启动配置（如果尚无）
        /// </summary>
        private void AddProfileToOfficialLauncher(string versionId, string officialDir)
        {
            string profilePath = Path.Combine(officialDir, "launcher_profiles.json");
            string accountsPath = Path.Combine(officialDir, "launcher_accounts_microsoft_store.json");

            JObject root;
            if (File.Exists(profilePath))
                root = JObject.Parse(File.ReadAllText(profilePath));
            else
            {
                root = new JObject();
                root["profiles"] = new JObject();
                root["settings"] = new JObject();
                root["version"] = 6;
            }

            // ===== 1. 同步账户信息（如果账户文件存在） =====
            if (File.Exists(accountsPath))
            {
                var accountsRoot = JObject.Parse(File.ReadAllText(accountsPath));
                var activeId = accountsRoot["activeAccountLocalId"]?.ToString();
                if (!string.IsNullOrEmpty(activeId) && accountsRoot["accounts"]?[activeId] != null)
                {
                    var account = accountsRoot["accounts"][activeId] as JObject;
                    var authDb = new JObject();
                    var authEntry = new JObject();
                    authEntry["accessToken"] = account["accessToken"]?.ToString() ?? "";
                    authEntry["username"] = account["username"]?.ToString() ?? "";
                    var profile = account["minecraftProfile"] as JObject;
                    if (profile != null)
                    {
                        var profilesObj = new JObject();
                        var profileEntry = new JObject();
                        profileEntry["displayName"] = profile["name"]?.ToString() ?? "";
                        profilesObj[profile["id"]?.ToString() ?? ""] = profileEntry;
                        authEntry["profiles"] = profilesObj;
                    }
                    authDb[activeId] = authEntry;
                    root["authenticationDatabase"] = authDb;

                    var selectedUser = new JObject();
                    selectedUser["account"] = activeId;
                    if (profile != null)
                        selectedUser["profile"] = profile["id"]?.ToString() ?? "";
                    root["selectedUser"] = selectedUser;
                }
            }

            // ===== 2. 确保 profiles 对象存在 =====
            if (root["profiles"] == null)
                root["profiles"] = new JObject();
            JObject profiles = (JObject)root["profiles"];

            // ===== 3. 检查是否已存在指向该版本的配置 =====
            bool existing = false;
            foreach (var prop in profiles.Properties())
            {
                var profile = prop.Value as JObject;
                if (profile != null && profile["lastVersionId"]?.ToString() == versionId)
                {
                    existing = true;
                    break;
                }
            }

            // ===== 4. 如果不存在，则创建新配置 =====
            if (!existing)
            {
                string profileId = Guid.NewGuid().ToString("N");
                JObject newProfile = new JObject();
                newProfile["name"] = versionId;
                newProfile["type"] = "custom";
                newProfile["created"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                newProfile["lastUsed"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                newProfile["lastVersionId"] = versionId;
                newProfile["icon"] = "Grass";
                profiles[profileId] = newProfile;

                // 写入文件
                File.WriteAllText(profilePath, root.ToString(Newtonsoft.Json.Formatting.Indented));
            }
        }
    }
}