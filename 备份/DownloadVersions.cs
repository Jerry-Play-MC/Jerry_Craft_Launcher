using ICSharpCode.SharpZipLib.Zip;
using MinecraftJavaRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace New_Launcher
{
    public partial class DownloadVersions : Form
    {
        private static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private string _selectedVersion;
        private string _currentRequestedGameVersion; // 用于丢弃过期结果
        private ConsoleRedirector _consoleRedirector;
        private bool _closeByBackButton = false;
        private bool _isClosing = false;  // 窗体正在关闭标志

        // ---- 互斥状态记录 ----
        private string _selectedLoaderType = null;   // 当前选中的加载器类型，null 表示未选中任何加载器
        private readonly object _mutexLock = new object();

        // 后台任务
        private BackgroundWorker downloadWorker;
        private BackgroundWorker loadListWorker;

        public DownloadVersions()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;

            // 初始化下载 Worker
            downloadWorker = new BackgroundWorker();
            downloadWorker.WorkerSupportsCancellation = true;
            downloadWorker.DoWork += DownloadWorker_DoWork;
            downloadWorker.RunWorkerCompleted += DownloadWorker_RunWorkerCompleted;

            // 初始化版本列表加载
            loadListWorker = new BackgroundWorker();
            loadListWorker.WorkerSupportsCancellation = true;
            loadListWorker.DoWork += LoadListWorker_DoWork;
            loadListWorker.RunWorkerCompleted += LoadListWorker_RunWorkerCompleted;

            // 控制台重定向到 RichTextBox
            _consoleRedirector = new ConsoleRedirector(richTextBox_Log);
            Console.SetOut(_consoleRedirector);

            this.FormClosed += DownloadVersions_FormClosed;

            // 初始 UI 状态
            comboBox_Versions.Items.Clear();
            comboBox_Versions.Items.Add("正在加载版本列表...");
            comboBox_Versions.SelectedIndex = 0;
            comboBox_Versions.Enabled = false;

            comboBox_Fabric.DataSource = new string[] { "不选择" };
            comboBox_Fabric.SelectedIndex = 0;
            comboBox_Fabric.Enabled = false;

            comboBox_Quilt.DataSource = new string[] { "不选择" };
            comboBox_Quilt.SelectedIndex = 0;
            comboBox_Quilt.Enabled = false;

            // 绑定事件
            comboBox_Versions.SelectedIndexChanged += comboBox_Versions_SelectedIndexChanged;
            comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;
            comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;

            // 异步加载原版版本列表
            loadListWorker.RunWorkerAsync();
        }

        // ---- 异步加载原版版本列表 ----
        private void LoadListWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var manifest = MinecraftCore.GetVersionManifest();
                var versions = manifest["versions"] as ArrayList;
                List<string> versionIds = new List<string>();
                foreach (var v in versions)
                {
                    var entry = v as Dictionary<string, object>;
                    if (entry != null)
                        versionIds.Add(entry["id"].ToString());
                }
                e.Result = versionIds;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void LoadListWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // 如果任务被取消，直接返回，不更新 UI
            if (e.Cancelled)
                return;

            comboBox_Versions.Enabled = true;
            if (e.Result is Exception ex)
            {
                MessageBox.Show($"加载版本列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_Versions.Items.Clear();
                comboBox_Versions.Items.Add("加载失败，请重试");
                comboBox_Versions.SelectedIndex = 0;
                return;
            }

            var versionIds = e.Result as List<string>;
            if (versionIds == null || versionIds.Count == 0)
            {
                comboBox_Versions.Items.Clear();
                comboBox_Versions.Items.Add("没有可用的版本");
                comboBox_Versions.SelectedIndex = 0;
                return;
            }

            comboBox_Versions.Items.Clear();
            foreach (var id in versionIds)
                comboBox_Versions.Items.Add(id);
            comboBox_Versions.SelectedIndex = 0;
        }

        // ---- 原版版本选择变更（添加句柄检查） ----
        private void comboBox_Versions_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 防止在句柄未创建时触发后台线程
            if (!this.IsHandleCreated)
                return;

            string gameVersion = comboBox_Versions.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(gameVersion) ||
                gameVersion.Contains("加载") ||
                gameVersion.Contains("失败") ||
                gameVersion.Contains("没有可用的版本"))
                return;

            // 清除互斥状态
            lock (_mutexLock)
            {
                _selectedLoaderType = null;
            }

            // 显示加载中
            comboBox_Forge.Enabled = false;
            comboBox_NeoForge.Enabled = false;
            comboBox_Fabric.Enabled = false;
            comboBox_Quilt.Enabled = false;
            comboBox_Forge.DataSource = new string[] { "加载中..." };
            comboBox_Forge.SelectedIndex = 0;
            comboBox_NeoForge.DataSource = new string[] { "加载中..." };
            comboBox_NeoForge.SelectedIndex = 0;
            comboBox_Fabric.DataSource = new string[] { "加载中..." };
            comboBox_Fabric.SelectedIndex = 0;
            comboBox_Quilt.DataSource = new string[] { "加载中..." };
            comboBox_Quilt.SelectedIndex = 0;

            _currentRequestedGameVersion = gameVersion;

            // 异步获取各加载器版本
            ThreadPool.QueueUserWorkItem(LoadForgeVersions, gameVersion);
            ThreadPool.QueueUserWorkItem(LoadNeoForgeVersions, gameVersion);
            ThreadPool.QueueUserWorkItem(LoadFabricVersions, gameVersion);
            ThreadPool.QueueUserWorkItem(LoadQuiltVersions, gameVersion);
        }

        // ======================================== 异步加载Mod加载器版本（互斥感知） ========================================

        // ---- 辅助：检查是否应该更新该加载器 ----
        private bool ShouldUpdateLoader(string loaderType)
        {
            lock (_mutexLock)
            {
                // 如果当前有选中的加载器，且不是自己，则不更新
                if (!string.IsNullOrEmpty(_selectedLoaderType) && _selectedLoaderType != loaderType)
                    return false;
                return true;
            }
        }

        // ---- 辅助：设置互斥状态并禁用其他加载器 ----
        private void SetSelectedLoader(string loaderType, string gameVersion)
        {
            lock (_mutexLock)
            {
                _selectedLoaderType = loaderType;
            }

            // 禁用其他加载器
            DisableOtherLoaders(loaderType);
        }

        private void DisableOtherLoaders(string activeLoaderType)
        {
            // 构建“不兼容”提示文字
            string incompatibleText = $"与 {activeLoaderType} 不兼容";

            // 禁用 Forge
            if (activeLoaderType != "Forge")
            {
                comboBox_Forge.SelectedIndexChanged -= comboBox_Forge_SelectedIndexChanged;
                comboBox_Forge.DataSource = new string[] { incompatibleText };
                comboBox_Forge.SelectedIndex = 0;
                comboBox_Forge.Enabled = false;
                comboBox_Forge.SelectedIndexChanged += comboBox_Forge_SelectedIndexChanged;
            }

            // 禁用 NeoForge
            if (activeLoaderType != "NeoForge")
            {
                comboBox_NeoForge.SelectedIndexChanged -= comboBox_NeoForge_SelectedIndexChanged;
                comboBox_NeoForge.DataSource = new string[] { incompatibleText };
                comboBox_NeoForge.SelectedIndex = 0;
                comboBox_NeoForge.Enabled = false;
                comboBox_NeoForge.SelectedIndexChanged += comboBox_NeoForge_SelectedIndexChanged;
            }

            // 禁用 Fabric
            if (activeLoaderType != "Fabric")
            {
                comboBox_Fabric.SelectedIndexChanged -= comboBox_Fabric_SelectedIndexChanged;
                comboBox_Fabric.DataSource = new string[] { incompatibleText };
                comboBox_Fabric.SelectedIndex = 0;
                comboBox_Fabric.Enabled = false;
                comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;
            }

            // 禁用 Quilt
            if (activeLoaderType != "Quilt")
            {
                comboBox_Quilt.SelectedIndexChanged -= comboBox_Quilt_SelectedIndexChanged;
                comboBox_Quilt.DataSource = new string[] { incompatibleText };
                comboBox_Quilt.SelectedIndex = 0;
                comboBox_Quilt.Enabled = false;
                comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;
            }
        }

        // ---- 恢复所有加载器（当取消选中时） ----
        private void RefreshAllLoaders(string gameVersion)
        {
            // 清除互斥状态
            lock (_mutexLock)
            {
                _selectedLoaderType = null;
            }

            // 恢复所有加载器为“加载中...”并启动后台加载
            comboBox_Forge.SelectedIndexChanged -= comboBox_Forge_SelectedIndexChanged;
            comboBox_Forge.DataSource = new string[] { "加载中..." };
            comboBox_Forge.SelectedIndex = 0;
            comboBox_Forge.Enabled = false;
            comboBox_Forge.SelectedIndexChanged += comboBox_Forge_SelectedIndexChanged;

            comboBox_NeoForge.SelectedIndexChanged -= comboBox_NeoForge_SelectedIndexChanged;
            comboBox_NeoForge.DataSource = new string[] { "加载中..." };
            comboBox_NeoForge.SelectedIndex = 0;
            comboBox_NeoForge.Enabled = false;
            comboBox_NeoForge.SelectedIndexChanged += comboBox_NeoForge_SelectedIndexChanged;

            comboBox_Fabric.SelectedIndexChanged -= comboBox_Fabric_SelectedIndexChanged;
            comboBox_Fabric.DataSource = new string[] { "加载中..." };
            comboBox_Fabric.SelectedIndex = 0;
            comboBox_Fabric.Enabled = false;
            comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;

            comboBox_Quilt.SelectedIndexChanged -= comboBox_Quilt_SelectedIndexChanged;
            comboBox_Quilt.DataSource = new string[] { "加载中..." };
            comboBox_Quilt.SelectedIndex = 0;
            comboBox_Quilt.Enabled = false;
            comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;

            // 重新启动加载线程
            _currentRequestedGameVersion = gameVersion;
            ThreadPool.QueueUserWorkItem(LoadForgeVersions, gameVersion);
            ThreadPool.QueueUserWorkItem(LoadNeoForgeVersions, gameVersion);
            ThreadPool.QueueUserWorkItem(LoadFabricVersions, gameVersion);
            ThreadPool.QueueUserWorkItem(LoadQuiltVersions, gameVersion);
        }

        // ---- 各加载器异步加载（带互斥检查和句柄保护） ----
        private void LoadForgeVersions(object state)
        {
            string gameVersion = (string)state;
            try
            {
                string[] versions = ForgeManager.Installer.GetForgeLoaderVersions(gameVersion);
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        // ★ 关键检查：如果窗体正在关闭或已释放，则不再更新 UI
                        if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                            return;

                        if (_currentRequestedGameVersion != gameVersion) return;
                        if (!ShouldUpdateLoader("Forge")) return;

                        comboBox_Forge.SelectedIndexChanged -= comboBox_Forge_SelectedIndexChanged;
                        comboBox_Forge.DataSource = versions;
                        comboBox_Forge.SelectedIndex = 0;
                        if (_selectedLoaderType == null)
                            comboBox_Forge.Enabled = true;
                        comboBox_Forge.SelectedIndexChanged += comboBox_Forge_SelectedIndexChanged;
                    }));
                }
            }
            catch (Exception)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        // ★ 同样检查
                        if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                            return;

                        if (_currentRequestedGameVersion != gameVersion) return;
                        if (!ShouldUpdateLoader("Forge")) return;
                        comboBox_Forge.DataSource = new string[] { "不选择" };
                        comboBox_Forge.SelectedIndex = 0;
                        if (_selectedLoaderType == null)
                            comboBox_Forge.Enabled = true;
                    }));
                }
            }
        }

        private void LoadNeoForgeVersions(object state)
        {
            string gameVersion = (string)state;
            try
            {
                string[] versions = NeoForgeManager.Installer.GetNeoForgeLoaderVersions(gameVersion);
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                            return;

                        if (_currentRequestedGameVersion != gameVersion) return;
                        if (!ShouldUpdateLoader("NeoForge")) return;

                        comboBox_NeoForge.SelectedIndexChanged -= comboBox_NeoForge_SelectedIndexChanged;
                        comboBox_NeoForge.DataSource = versions;
                        comboBox_NeoForge.SelectedIndex = 0;
                        if (_selectedLoaderType == null)
                            comboBox_NeoForge.Enabled = true;
                        comboBox_NeoForge.SelectedIndexChanged += comboBox_NeoForge_SelectedIndexChanged;
                    }));
                }
            }
            catch (Exception)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                            return;

                        if (_currentRequestedGameVersion != gameVersion) return;
                        if (!ShouldUpdateLoader("NeoForge")) return;
                        comboBox_NeoForge.DataSource = new string[] { "不选择" };
                        comboBox_NeoForge.SelectedIndex = 0;
                        if (_selectedLoaderType == null)
                            comboBox_NeoForge.Enabled = true;
                    }));
                }
            }
        }

        private void LoadFabricVersions(object state)
        {
            string gameVersion = (string)state;
            try
            {
                string[] versions = FabricManager.Installer.GetFabricLoaderVersions(gameVersion);
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                            return;

                        if (_currentRequestedGameVersion != gameVersion) return;
                        if (!ShouldUpdateLoader("Fabric")) return;

                        comboBox_Fabric.SelectedIndexChanged -= comboBox_Fabric_SelectedIndexChanged;
                        comboBox_Fabric.DataSource = versions;
                        comboBox_Fabric.SelectedIndex = 0;
                        if (_selectedLoaderType == null)
                            comboBox_Fabric.Enabled = true;
                        comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;
                    }));
                }
            }
            catch (Exception)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                            return;

                        if (_currentRequestedGameVersion != gameVersion) return;
                        if (!ShouldUpdateLoader("Fabric")) return;
                        comboBox_Fabric.DataSource = new string[] { "不选择" };
                        comboBox_Fabric.SelectedIndex = 0;
                        if (_selectedLoaderType == null)
                            comboBox_Fabric.Enabled = true;
                    }));
                }
            }
        }

        private void LoadQuiltVersions(object state)
        {
            string gameVersion = (string)state;
            try
            {
                string[] versions = QuiltManager.Installer.GetQuiltLoaderVersions(gameVersion);
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                            return;

                        if (_currentRequestedGameVersion != gameVersion) return;
                        if (!ShouldUpdateLoader("Quilt")) return;

                        comboBox_Quilt.SelectedIndexChanged -= comboBox_Quilt_SelectedIndexChanged;
                        comboBox_Quilt.DataSource = versions;
                        comboBox_Quilt.SelectedIndex = 0;
                        if (_selectedLoaderType == null)
                            comboBox_Quilt.Enabled = true;
                        comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;
                    }));
                }
            }
            catch (Exception)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        if (_isClosing || this.IsDisposed || !this.IsHandleCreated)
                            return;

                        if (_currentRequestedGameVersion != gameVersion) return;
                        if (!ShouldUpdateLoader("Quilt")) return;
                        comboBox_Quilt.DataSource = new string[] { "不选择" };
                        comboBox_Quilt.SelectedIndex = 0;
                        if (_selectedLoaderType == null)
                            comboBox_Quilt.Enabled = true;
                    }));
                }
            }
        }

        // ======================================== 加载器互斥事件 ========================================

        private void comboBox_Forge_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox_Forge.SelectedItem?.ToString();
            string gameVersion = comboBox_Versions.SelectedItem?.ToString();

            if (selected == "不选择")
            {
                RefreshAllLoaders(gameVersion);
            }
            else if (selected != "加载中..." && selected != "与 Forge 不兼容")
            {
                SetSelectedLoader("Forge", gameVersion);
            }
        }

        private void comboBox_NeoForge_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox_NeoForge.SelectedItem?.ToString();
            string gameVersion = comboBox_Versions.SelectedItem?.ToString();

            if (selected == "不选择")
            {
                RefreshAllLoaders(gameVersion);
            }
            else if (selected != "加载中..." && selected != "与 NeoForge 不兼容")
            {
                SetSelectedLoader("NeoForge", gameVersion);
            }
        }

        private void comboBox_Fabric_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox_Fabric.SelectedItem?.ToString();
            string gameVersion = comboBox_Versions.SelectedItem?.ToString();

            if (selected == "不选择")
            {
                RefreshAllLoaders(gameVersion);
            }
            else if (selected != "加载中..." && selected != "与 Fabric 不兼容")
            {
                SetSelectedLoader("Fabric", gameVersion);
            }
        }

        private void comboBox_Quilt_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox_Quilt.SelectedItem?.ToString();
            string gameVersion = comboBox_Versions.SelectedItem?.ToString();

            if (selected == "不选择")
            {
                RefreshAllLoaders(gameVersion);
            }
            else if (selected != "加载中..." && selected != "与 Quilt 不兼容")
            {
                SetSelectedLoader("Quilt", gameVersion);
            }
        }

        // ---- 下载按钮 ----
        private void button_Download_Click(object sender, EventArgs e)
        {
            string selectedVersion = comboBox_Versions.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedVersion) || selectedVersion.Contains("加载") || selectedVersion.Contains("失败"))
            {
                MessageBox.Show("请选择一个有效的游戏版本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string versionName = textBox_VersionsName.Text.Trim();
            if (string.IsNullOrEmpty(versionName))
                versionName = selectedVersion;

            string forgeVer = comboBox_Forge.SelectedItem?.ToString();
            string neoforgeVer = comboBox_NeoForge.SelectedItem?.ToString();
            string fabricVer = comboBox_Fabric.SelectedItem?.ToString();
            string quiltVer = comboBox_Quilt.SelectedItem?.ToString();

            string loaderType = null;
            string loaderVersion = null;

            if (!string.IsNullOrEmpty(forgeVer) && forgeVer != "不选择" && forgeVer != "加载中..." && !forgeVer.Contains("兼容"))
            {
                loaderType = "Forge";
                loaderVersion = forgeVer;
            }
            else if (!string.IsNullOrEmpty(neoforgeVer) && neoforgeVer != "不选择" && neoforgeVer != "加载中..." && !neoforgeVer.Contains("兼容"))
            {
                loaderType = "NeoForge";
                loaderVersion = neoforgeVer;
            }
            else if (!string.IsNullOrEmpty(fabricVer) && fabricVer != "不选择" && fabricVer != "加载中..." && !fabricVer.Contains("兼容"))
            {
                loaderType = "Fabric";
                loaderVersion = fabricVer;
            }
            else if (!string.IsNullOrEmpty(quiltVer) && quiltVer != "不选择" && quiltVer != "加载中..." && !quiltVer.Contains("兼容"))
            {
                loaderType = "Quilt";
                loaderVersion = quiltVer;
            }

            button_Download.Enabled = false;
            button_Download.Text = "下载中...";
            richTextBox_Log.Clear();

            var args = new object[] { selectedVersion, versionName, loaderType, loaderVersion };
            downloadWorker.RunWorkerAsync(args);
        }

        // ---- 下载后台执行 ----
        private void DownloadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] args = (object[])e.Argument;
            string gameVersion = (string)args[0];
            string versionName = (string)args[1];
            string loaderType = (string)args[2];
            string loaderVersion = (string)args[3];

            string minecraftDir = Path.Combine(LauncherPath, ".minecraft");

            try
            {
                Console.WriteLine($"[下载] 开始下载原版 {gameVersion}");
                VanillaManager.Installer.DownloadVanilla(gameVersion, minecraftDir, versionName, false);
                Console.WriteLine($"[下载] 原版下载完成");

                var manager = new JavaRuntimeManager(Path.Combine(Path.Combine(LauncherPath, "Launcher Setting"), "Java"));
                try
                {
                    string javaPath = manager.EnsureJavaRuntime(gameVersion);
                    Console.WriteLine("Java 可执行文件位于: " + javaPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("错误: " + ex.Message);
                }

                if (loaderType == "Fabric")
                {
                    Console.WriteLine($"[下载] 开始安装 Fabric {loaderVersion} 到版本 {gameVersion}");
                    string actualVersionId = FabricManager.Installer.InstallFabric(gameVersion, loaderVersion, false);
                    Console.WriteLine($"[下载] Fabric 安装完成，版本目录: {actualVersionId}");
                }
                else if (loaderType == "Quilt")
                {
                    Console.WriteLine($"[下载] 开始安装 Quilt {loaderVersion} 到版本 {gameVersion}");
                    string actualVersionId = QuiltManager.Installer.InstallQuilt(gameVersion, loaderVersion, false);
                    Console.WriteLine($"[下载] Quilt 安装完成，版本目录: {actualVersionId}");
                }
                else if (loaderType == "NeoForge")
                {
                    Console.WriteLine($"[下载] 开始安装 NeoForge {loaderVersion} 到版本 {gameVersion}");
                    string actualVersionId = NeoForgeManager.Installer.InstallNeoForge(gameVersion, loaderVersion, false);
                    Console.WriteLine($"[下载] NeoForge 安装完成，版本目录: {actualVersionId}");
                }
                else if (loaderType == "Forge")
                {
                    Console.WriteLine($"[下载] 开始安装 Forge {loaderVersion} 到版本 {gameVersion}");
                    string actualVersionId = ForgeManager.Installer.InstallForge(gameVersion, loaderVersion, false);
                    Console.WriteLine($"[下载] Forge 安装完成，版本目录: {actualVersionId}");
                }
            }
            catch (Exception ex)
            {
                e.Result = ex;
                return;
            }

            e.Result = "下载完成";
        }

        private void DownloadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // 如果任务被取消，直接返回
            if (e.Cancelled)
                return;

            button_Download.Enabled = true;
            button_Download.Text = "下载";

            if (e.Error != null)
            {
                MessageBox.Show($"下载失败: {e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(e.Result?.ToString() ?? "下载完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ---- 返回按钮 ----
        private void button_Back_Click(object sender, EventArgs e)
        {
            _closeByBackButton = true;
            if (_consoleRedirector != null)
            {
                try
                {
                    Console.SetOut(_consoleRedirector.OriginalOut);
                    _consoleRedirector.Dispose();
                }
                catch { }
            }
            // 关闭当前窗体，主窗体将自动重新显示
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ---- 窗体关闭 ----
        private void DownloadVersions_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_consoleRedirector != null)
            {
                try
                {
                    Console.SetOut(_consoleRedirector.OriginalOut);
                    _consoleRedirector.Dispose();
                }
                catch { }
            }
        }

        private void DownloadVersions_Load(object sender, EventArgs e) { }

        /// <summary>
        /// 将嵌入的程序集资源提取到指定目录
        /// </summary>
        public static void CopyFileInEXE(string resourceName, string destinationPath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    throw new Exception($"未找到嵌入的资源: {resourceName}");

                string destDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = resourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }

        // ---- 内部类：控制台重定向 ----
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
                // ★ 关键检查：如果文本框已被释放，则直接返回
                if (_textBox == null || _textBox.IsDisposed)
                    return;

                try
                {
                    if (_textBox.InvokeRequired)
                        _textBox.Invoke(new Action(() => AppendText(value)));
                    else
                        AppendText(value);
                }
                catch (ObjectDisposedException)
                {
                    // 忽略，控件已释放
                }
            }

            public override void WriteLine(string value)
            {
                Write(value + Environment.NewLine);
            }

            private void AppendText(string text)
            {
                try
                {
                    _textBox.AppendText(text);
                    _textBox.ScrollToCaret();
                }
                catch (ObjectDisposedException)
                {
                    // 忽略
                }
            }

            public override Encoding Encoding => Encoding.UTF8;

            // ★ 添加 Dispose 方法，断开引用
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _textBox = null;   // 解除引用，帮助 GC
                }
                base.Dispose(disposing);
            }
        }

        private void DownloadVersions_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;

            // 取消正在运行的 BackgroundWorker
            if (downloadWorker != null && downloadWorker.IsBusy)
            {
                downloadWorker.CancelAsync();
                // 等待一小段时间，让 Worker 有机会响应取消（可选）
                // System.Threading.Thread.Sleep(100);
            }

            if (loadListWorker != null && loadListWorker.IsBusy)
            {
                loadListWorker.CancelAsync();
            }

            // 释放 ConsoleRedirector
            if (_consoleRedirector != null)
            {
                try
                {
                    Console.SetOut(_consoleRedirector.OriginalOut);
                    _consoleRedirector.Dispose();
                }
                catch { }
            }

            if (!_closeByBackButton)
            {
                Application.Exit();
            }
        }
    }
}