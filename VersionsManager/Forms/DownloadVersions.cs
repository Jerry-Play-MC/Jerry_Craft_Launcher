using MinecraftJavaRuntime;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static New_Launcher.MainForm;

namespace New_Launcher
{
    public partial class DownloadVersions : Form
    {
        private static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string MinecraftDir = Path.Combine(LauncherPath, ".minecraft");
        private string _targetGameVersion;
        private string _currentRequestedGameVersion;
        private ConsoleRedirector _consoleRedirector;
        private bool _closeByBackButton = false;
        private bool _isClosing = false;
        private int _requestId = 0;
        private readonly object _requestLock = new object();
        private bool _isProgrammaticUpdate = false; // 防止程序更新触发事件

        private string _selectedLoaderType = null;
        private readonly object _mutexLock = new object();

        private BackgroundWorker downloadWorker;

        public DownloadVersions()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;

            downloadWorker = new BackgroundWorker();
            downloadWorker.WorkerSupportsCancellation = true;
            downloadWorker.DoWork += DownloadWorker_DoWork;
            downloadWorker.RunWorkerCompleted += DownloadWorker_RunWorkerCompleted;

            _consoleRedirector = new ConsoleRedirector(richTextBox_Log);
            Console.SetOut(_consoleRedirector);

            this.FormClosed += DownloadVersions_FormClosed;

            comboBox_Forge.Enabled = false;
            comboBox_NeoForge.Enabled = false;
            comboBox_Fabric.Enabled = false;
            comboBox_Quilt.Enabled = false;
            comboBox_Forge.DataSource = new string[] { "未选择版本" };
            comboBox_Forge.SelectedIndex = 0;
            comboBox_NeoForge.DataSource = new string[] { "未选择版本" };
            comboBox_NeoForge.SelectedIndex = 0;
            comboBox_Fabric.DataSource = new string[] { "未选择版本" };
            comboBox_Fabric.SelectedIndex = 0;
            comboBox_Quilt.DataSource = new string[] { "未选择版本" };
            comboBox_Quilt.SelectedIndex = 0;

            comboBox_Forge.SelectedIndexChanged += comboBox_Forge_SelectedIndexChanged;
            comboBox_NeoForge.SelectedIndexChanged += comboBox_NeoForge_SelectedIndexChanged;
            comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;
            comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;
        }

        public void SetTargetVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                MessageBox.Show("传入的版本号无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _targetGameVersion = version;
            _currentRequestedGameVersion = version;

            int currentRequestId;
            lock (_requestLock) { currentRequestId = ++_requestId; }

            lock (_mutexLock) { _selectedLoaderType = null; }

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

            ThreadPool.QueueUserWorkItem(state => LoadForgeVersions(new object[] { version, currentRequestId }));
            ThreadPool.QueueUserWorkItem(state => LoadNeoForgeVersions(new object[] { version, currentRequestId }));
            ThreadPool.QueueUserWorkItem(state => LoadFabricVersions(new object[] { version, currentRequestId }));
            ThreadPool.QueueUserWorkItem(state => LoadQuiltVersions(new object[] { version, currentRequestId }));
        }

        private bool ShouldUpdateLoader(string loaderType)
        {
            lock (_mutexLock)
            {
                if (!string.IsNullOrEmpty(_selectedLoaderType) && _selectedLoaderType != loaderType)
                    return false;
                return true;
            }
        }

        private void SetSelectedLoader(string loaderType, string gameVersion)
        {
            lock (_mutexLock) { _selectedLoaderType = loaderType; }
            DisableOtherLoaders(loaderType);
        }

        private void DisableOtherLoaders(string activeLoaderType)
        {
            string incompatibleText = $"与 {activeLoaderType} 不兼容";

            if (activeLoaderType != "Forge")
            {
                _isProgrammaticUpdate = true;
                comboBox_Forge.SelectedIndexChanged -= comboBox_Forge_SelectedIndexChanged;
                comboBox_Forge.DataSource = new string[] { incompatibleText };
                comboBox_Forge.SelectedIndex = 0;
                comboBox_Forge.Enabled = false;
                comboBox_Forge.SelectedIndexChanged += comboBox_Forge_SelectedIndexChanged;
                _isProgrammaticUpdate = false;
            }

            if (activeLoaderType != "NeoForge")
            {
                _isProgrammaticUpdate = true;
                comboBox_NeoForge.SelectedIndexChanged -= comboBox_NeoForge_SelectedIndexChanged;
                comboBox_NeoForge.DataSource = new string[] { incompatibleText };
                comboBox_NeoForge.SelectedIndex = 0;
                comboBox_NeoForge.Enabled = false;
                comboBox_NeoForge.SelectedIndexChanged += comboBox_NeoForge_SelectedIndexChanged;
                _isProgrammaticUpdate = false;
            }

            if (activeLoaderType != "Fabric")
            {
                _isProgrammaticUpdate = true;
                comboBox_Fabric.SelectedIndexChanged -= comboBox_Fabric_SelectedIndexChanged;
                comboBox_Fabric.DataSource = new string[] { incompatibleText };
                comboBox_Fabric.SelectedIndex = 0;
                comboBox_Fabric.Enabled = false;
                comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;
                _isProgrammaticUpdate = false;
            }

            if (activeLoaderType != "Quilt")
            {
                _isProgrammaticUpdate = true;
                comboBox_Quilt.SelectedIndexChanged -= comboBox_Quilt_SelectedIndexChanged;
                comboBox_Quilt.DataSource = new string[] { incompatibleText };
                comboBox_Quilt.SelectedIndex = 0;
                comboBox_Quilt.Enabled = false;
                comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;
                _isProgrammaticUpdate = false;
            }
        }

        private void RefreshAllLoaders(string gameVersion)
        {
            lock (_mutexLock) { _selectedLoaderType = null; }

            int currentRequestId;
            lock (_requestLock) { currentRequestId = ++_requestId; }
            _currentRequestedGameVersion = gameVersion;

            _isProgrammaticUpdate = true;

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

            _isProgrammaticUpdate = false;

            ThreadPool.QueueUserWorkItem(state => LoadForgeVersions(new object[] { gameVersion, currentRequestId }));
            ThreadPool.QueueUserWorkItem(state => LoadNeoForgeVersions(new object[] { gameVersion, currentRequestId }));
            ThreadPool.QueueUserWorkItem(state => LoadFabricVersions(new object[] { gameVersion, currentRequestId }));
            ThreadPool.QueueUserWorkItem(state => LoadQuiltVersions(new object[] { gameVersion, currentRequestId }));
        }

        // ======================================== 加载器异步加载（带重试） ========================================

        private void LoadForgeVersions(object state)
        {
            object[] args = (object[])state;
            string gameVersion = (string)args[0];
            int requestId = (int)args[1];

            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelayMs = 500;

            while (retryCount < maxRetries)
            {
                if (_requestId != requestId) break;

                try
                {
                    string[] versions = ForgeManager.Installer.GetForgeLoaderVersions(gameVersion);
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                            // 不再检查 _requestId，直接更新
                            _isProgrammaticUpdate = true;
                            comboBox_Forge.SelectedIndexChanged -= comboBox_Forge_SelectedIndexChanged;
                            comboBox_Forge.DataSource = versions;
                            comboBox_Forge.SelectedIndex = 0;
                            if (_selectedLoaderType == null)
                                comboBox_Forge.Enabled = true;
                            comboBox_Forge.SelectedIndexChanged += comboBox_Forge_SelectedIndexChanged;
                            _isProgrammaticUpdate = false;
                        }));
                    }
                    break;
                }
                catch
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                                _isProgrammaticUpdate = true;
                                comboBox_Forge.SelectedIndexChanged -= comboBox_Forge_SelectedIndexChanged;
                                comboBox_Forge.DataSource = new string[] { "不选择" };
                                comboBox_Forge.SelectedIndex = 0;
                                if (_selectedLoaderType == null)
                                    comboBox_Forge.Enabled = true;
                                comboBox_Forge.SelectedIndexChanged += comboBox_Forge_SelectedIndexChanged;
                                _isProgrammaticUpdate = false;
                            }));
                        }
                        break;
                    }
                    else
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                                _isProgrammaticUpdate = true;
                                comboBox_Forge.SelectedIndexChanged -= comboBox_Forge_SelectedIndexChanged;
                                comboBox_Forge.DataSource = new string[] { $"重试中... ({retryCount}/{maxRetries})" };
                                comboBox_Forge.SelectedIndex = 0;
                                comboBox_Forge.Enabled = false;
                                comboBox_Forge.SelectedIndexChanged += comboBox_Forge_SelectedIndexChanged;
                                _isProgrammaticUpdate = false;
                            }));
                        }
                        Thread.Sleep(retryDelayMs);
                    }
                }
            }
        }

        // 以下三个方法结构与 LoadForgeVersions 完全相同，仅替换类名和控件名
        private void LoadNeoForgeVersions(object state)
        {
            object[] args = (object[])state;
            string gameVersion = (string)args[0];
            int requestId = (int)args[1];

            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelayMs = 500;

            while (retryCount < maxRetries)
            {
                if (_requestId != requestId) break;

                try
                {
                    string[] versions = NeoForgeManager.Installer.GetNeoForgeLoaderVersions(gameVersion);
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                            _isProgrammaticUpdate = true;
                            comboBox_NeoForge.SelectedIndexChanged -= comboBox_NeoForge_SelectedIndexChanged;
                            comboBox_NeoForge.DataSource = versions;
                            comboBox_NeoForge.SelectedIndex = 0;
                            if (_selectedLoaderType == null)
                                comboBox_NeoForge.Enabled = true;
                            comboBox_NeoForge.SelectedIndexChanged += comboBox_NeoForge_SelectedIndexChanged;
                            _isProgrammaticUpdate = false;
                        }));
                    }
                    break;
                }
                catch
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                                _isProgrammaticUpdate = true;
                                comboBox_NeoForge.SelectedIndexChanged -= comboBox_NeoForge_SelectedIndexChanged;
                                comboBox_NeoForge.DataSource = new string[] { "不选择" };
                                comboBox_NeoForge.SelectedIndex = 0;
                                if (_selectedLoaderType == null)
                                    comboBox_NeoForge.Enabled = true;
                                comboBox_NeoForge.SelectedIndexChanged += comboBox_NeoForge_SelectedIndexChanged;
                                _isProgrammaticUpdate = false;
                            }));
                        }
                        break;
                    }
                    else
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                                _isProgrammaticUpdate = true;
                                comboBox_NeoForge.SelectedIndexChanged -= comboBox_NeoForge_SelectedIndexChanged;
                                comboBox_NeoForge.DataSource = new string[] { $"重试中... ({retryCount}/{maxRetries})" };
                                comboBox_NeoForge.SelectedIndex = 0;
                                comboBox_NeoForge.Enabled = false;
                                comboBox_NeoForge.SelectedIndexChanged += comboBox_NeoForge_SelectedIndexChanged;
                                _isProgrammaticUpdate = false;
                            }));
                        }
                        Thread.Sleep(retryDelayMs);
                    }
                }
            }
        }

        private void LoadFabricVersions(object state)
        {
            object[] args = (object[])state;
            string gameVersion = (string)args[0];
            int requestId = (int)args[1];

            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelayMs = 500;

            while (retryCount < maxRetries)
            {
                if (_requestId != requestId) break;

                try
                {
                    string[] versions = FabricManager.Installer.GetFabricLoaderVersions(gameVersion);
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                            _isProgrammaticUpdate = true;
                            comboBox_Fabric.SelectedIndexChanged -= comboBox_Fabric_SelectedIndexChanged;
                            comboBox_Fabric.DataSource = versions;
                            comboBox_Fabric.SelectedIndex = 0;
                            if (_selectedLoaderType == null)
                                comboBox_Fabric.Enabled = true;
                            comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;
                            _isProgrammaticUpdate = false;
                        }));
                    }
                    break;
                }
                catch
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                                _isProgrammaticUpdate = true;
                                comboBox_Fabric.SelectedIndexChanged -= comboBox_Fabric_SelectedIndexChanged;
                                comboBox_Fabric.DataSource = new string[] { "不选择" };
                                comboBox_Fabric.SelectedIndex = 0;
                                if (_selectedLoaderType == null)
                                    comboBox_Fabric.Enabled = true;
                                comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;
                                _isProgrammaticUpdate = false;
                            }));
                        }
                        break;
                    }
                    else
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                                _isProgrammaticUpdate = true;
                                comboBox_Fabric.SelectedIndexChanged -= comboBox_Fabric_SelectedIndexChanged;
                                comboBox_Fabric.DataSource = new string[] { $"重试中... ({retryCount}/{maxRetries})" };
                                comboBox_Fabric.SelectedIndex = 0;
                                comboBox_Fabric.Enabled = false;
                                comboBox_Fabric.SelectedIndexChanged += comboBox_Fabric_SelectedIndexChanged;
                                _isProgrammaticUpdate = false;
                            }));
                        }
                        Thread.Sleep(retryDelayMs);
                    }
                }
            }
        }

        private void LoadQuiltVersions(object state)
        {
            object[] args = (object[])state;
            string gameVersion = (string)args[0];
            int requestId = (int)args[1];

            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelayMs = 500;

            while (retryCount < maxRetries)
            {
                if (_requestId != requestId) break;

                try
                {
                    string[] versions = QuiltManager.Installer.GetQuiltLoaderVersions(gameVersion);
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                            _isProgrammaticUpdate = true;
                            comboBox_Quilt.SelectedIndexChanged -= comboBox_Quilt_SelectedIndexChanged;
                            comboBox_Quilt.DataSource = versions;
                            comboBox_Quilt.SelectedIndex = 0;
                            if (_selectedLoaderType == null)
                                comboBox_Quilt.Enabled = true;
                            comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;
                            _isProgrammaticUpdate = false;
                        }));
                    }
                    break;
                }
                catch
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                                _isProgrammaticUpdate = true;
                                comboBox_Quilt.SelectedIndexChanged -= comboBox_Quilt_SelectedIndexChanged;
                                comboBox_Quilt.DataSource = new string[] { "不选择" };
                                comboBox_Quilt.SelectedIndex = 0;
                                if (_selectedLoaderType == null)
                                    comboBox_Quilt.Enabled = true;
                                comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;
                                _isProgrammaticUpdate = false;
                            }));
                        }
                        break;
                    }
                    else
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                if (_isClosing || this.IsDisposed || !this.IsHandleCreated) return;
                                _isProgrammaticUpdate = true;
                                comboBox_Quilt.SelectedIndexChanged -= comboBox_Quilt_SelectedIndexChanged;
                                comboBox_Quilt.DataSource = new string[] { $"重试中... ({retryCount}/{maxRetries})" };
                                comboBox_Quilt.SelectedIndex = 0;
                                comboBox_Quilt.Enabled = false;
                                comboBox_Quilt.SelectedIndexChanged += comboBox_Quilt_SelectedIndexChanged;
                                _isProgrammaticUpdate = false;
                            }));
                        }
                        Thread.Sleep(retryDelayMs);
                    }
                }
            }
        }

        // ======================================== 加载器互斥事件 ========================================

        private void comboBox_Forge_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate) return;

            string selected = comboBox_Forge.SelectedItem?.ToString();
            string gameVersion = _targetGameVersion;

            if (selected == "不选择")
            {
                RefreshAllLoaders(gameVersion);
            }
            else if (selected != "加载中..." && selected != "重试中..." && selected != "未选择版本" && selected != "与 Forge 不兼容")
            {
                SetSelectedLoader("Forge", gameVersion);
            }
        }

        private void comboBox_NeoForge_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate) return;

            string selected = comboBox_NeoForge.SelectedItem?.ToString();
            string gameVersion = _targetGameVersion;

            if (selected == "不选择")
            {
                RefreshAllLoaders(gameVersion);
            }
            else if (selected != "加载中..." && selected != "重试中..." && selected != "未选择版本" && selected != "与 NeoForge 不兼容")
            {
                SetSelectedLoader("NeoForge", gameVersion);
            }
        }

        private void comboBox_Fabric_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate) return;

            string selected = comboBox_Fabric.SelectedItem?.ToString();
            string gameVersion = _targetGameVersion;

            if (selected == "不选择")
            {
                RefreshAllLoaders(gameVersion);
            }
            else if (selected != "加载中..." && selected != "重试中..." && selected != "未选择版本" && selected != "与 Fabric 不兼容")
            {
                SetSelectedLoader("Fabric", gameVersion);
            }
        }

        private void comboBox_Quilt_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate) return;

            string selected = comboBox_Quilt.SelectedItem?.ToString();
            string gameVersion = _targetGameVersion;

            if (selected == "不选择")
            {
                RefreshAllLoaders(gameVersion);
            }
            else if (selected != "加载中..." && selected != "重试中..." && selected != "未选择版本" && selected != "与 Quilt 不兼容")
            {
                SetSelectedLoader("Quilt", gameVersion);
            }
        }

        // ======================================== 下载按钮与后台任务 ========================================

        private void button_Download_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_targetGameVersion))
            {
                MessageBox.Show("请先设置要下载的游戏版本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string forgeVer = comboBox_Forge.SelectedItem?.ToString();
            string neoforgeVer = comboBox_NeoForge.SelectedItem?.ToString();
            string fabricVer = comboBox_Fabric.SelectedItem?.ToString();
            string quiltVer = comboBox_Quilt.SelectedItem?.ToString();

            string loaderType = null;
            string loaderVersion = null;

            if (!string.IsNullOrEmpty(forgeVer) && forgeVer != "不选择" && forgeVer != "加载中..." && !forgeVer.Contains("兼容") && !forgeVer.Contains("重试中"))
            {
                loaderType = "Forge";
                loaderVersion = forgeVer;
            }
            else if (!string.IsNullOrEmpty(neoforgeVer) && neoforgeVer != "不选择" && neoforgeVer != "加载中..." && !neoforgeVer.Contains("兼容") && !neoforgeVer.Contains("重试中"))
            {
                loaderType = "NeoForge";
                loaderVersion = neoforgeVer;
            }
            else if (!string.IsNullOrEmpty(fabricVer) && fabricVer != "不选择" && fabricVer != "加载中..." && !fabricVer.Contains("兼容") && !fabricVer.Contains("重试中"))
            {
                loaderType = "Fabric";
                loaderVersion = fabricVer;
            }
            else if (!string.IsNullOrEmpty(quiltVer) && quiltVer != "不选择" && quiltVer != "加载中..." && !quiltVer.Contains("兼容") && !quiltVer.Contains("重试中"))
            {
                loaderType = "Quilt";
                loaderVersion = quiltVer;
            }

            string customName = GenerateCustomVersionName(_targetGameVersion, loaderType, loaderVersion);
            if (string.IsNullOrEmpty(customName))
            {
                MessageBox.Show("生成版本名失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            textBox_VersionsName.Text = customName;
            textBox_VersionsName.ReadOnly = true;

            string versionsDir = Path.Combine(MinecraftDir, "versions");
            List<string> beforeDirs = new List<string>();
            if (Directory.Exists(versionsDir))
            {
                beforeDirs.AddRange(Directory.GetDirectories(versionsDir).Select(Path.GetFileName));
            }
            else
            {
                Directory.CreateDirectory(versionsDir);
            }

            button_Download.Enabled = false;
            button_Download.Text = "下载中...";
            richTextBox_Log.Clear();

            var args = new object[] { _targetGameVersion, customName, loaderType, loaderVersion, beforeDirs };
            downloadWorker.RunWorkerAsync(args);
        }

        private string GenerateCustomVersionName(string gameVersion, string loaderType, string loaderVersion)
        {
            string baseName = gameVersion;
            if (!string.IsNullOrEmpty(loaderType) && !string.IsNullOrEmpty(loaderVersion))
            {
                baseName = $"{gameVersion}-{loaderType}_{loaderVersion}";
            }
            string candidate = baseName + "-1";
            int suffix = 1;
            while (Directory.Exists(Path.Combine(Path.Combine(MinecraftDir, "versions"), candidate)))
            {
                suffix++;
                candidate = baseName + "-" + suffix;
            }
            return candidate;
        }

        private void DownloadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] args = (object[])e.Argument;
            string gameVersion = (string)args[0];
            string customName = (string)args[1];
            string loaderType = (string)args[2];
            string loaderVersion = (string)args[3];
            List<string> beforeDirs = (List<string>)args[4];

            string minecraftDir = Path.Combine(LauncherPath, ".minecraft");

            try
            {
                Console.WriteLine($"[下载] 开始下载原版 {gameVersion}");
                VanillaManager.Installer.DownloadVanilla(gameVersion, minecraftDir, gameVersion, false);
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

                string versionsDir = Path.Combine(minecraftDir, "versions");
                List<string> afterDirs = new List<string>();
                if (Directory.Exists(versionsDir))
                {
                    afterDirs.AddRange(Directory.GetDirectories(versionsDir).Select(Path.GetFileName));
                }

                List<string> added = afterDirs.Except(beforeDirs).ToList();

                e.Result = new object[] { added, customName };
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void DownloadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                return;

            button_Download.Enabled = true;
            button_Download.Text = "下载";
            textBox_VersionsName.ReadOnly = false;

            if (e.Error != null)
            {
                MessageBox.Show($"下载失败: {e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (e.Result is Exception ex)
            {
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            object[] resultArray = e.Result as object[];
            if (resultArray == null || resultArray.Length != 2)
            {
                MessageBox.Show("未知的下载结果", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<string> added = resultArray[0] as List<string>;
            string customName = resultArray[1] as string;
            if (added == null || customName == null)
            {
                MessageBox.Show("下载结果数据格式错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (added.Count == 0)
            {
                MessageBox.Show("下载后没有新增版本文件夹，可能下载失败或未生成。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (added.Count == 1)
            {
                string oldName = added[0];
                if (RenameVersion(oldName, customName))
                {
                    MessageBox.Show($"版本重命名成功：{oldName} -> {customName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (added.Count == 2)
            {
                string gameVersion = _targetGameVersion;
                string vanillaFolder = added.FirstOrDefault(f => f == gameVersion);
                string loaderFolder = added.FirstOrDefault(f => f != gameVersion);

                if (string.IsNullOrEmpty(vanillaFolder) || string.IsNullOrEmpty(loaderFolder))
                {
                    MessageBox.Show("无法识别原版和加载器文件夹，请手动处理。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    string vanillaPath = Path.Combine(Path.Combine(MinecraftDir, "versions"), vanillaFolder);
                    Directory.Delete(vanillaPath, true);
                    Console.WriteLine($"已删除原版文件夹: {vanillaFolder}");
                }
                catch (Exception delEx)
                {
                    MessageBox.Show($"删除原版文件夹失败：{delEx.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (RenameVersion(loaderFolder, customName))
                {
                    MessageBox.Show($"版本重命名成功：{loaderFolder} -> {customName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show($"新增了 {added.Count} 个文件夹，无法自动处理，请手动管理。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ---- 窗体加载 ----
        private void DownloadVersions_Load(object sender, EventArgs e)
        {
            // 若有目标版本，则立即开始加载；否则等待调用 SetTargetVersion
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

        // ---- 重命名版本（您已有的代码） ----
        private bool RenameVersion(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("原版本名或新版本名为空。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (oldName == newName)
            {
                MessageBox.Show("新旧版本名相同，无需重命名。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (newName.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show($"新版本名包含非法字符（如 \\ / : * ? \" < > |）", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string versionsDir = Path.Combine(MinecraftDir, "versions");
            string oldFolder = Path.Combine(versionsDir, oldName);
            string newFolder = Path.Combine(versionsDir, newName);

            if (!Directory.Exists(oldFolder))
            {
                MessageBox.Show($"原版本文件夹不存在：{oldFolder}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (Directory.Exists(newFolder))
            {
                MessageBox.Show($"目标版本名 {newName} 已存在，无法重命名。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string oldJsonPath = Path.Combine(oldFolder, oldName + ".json");
            if (!File.Exists(oldJsonPath))
            {
                MessageBox.Show($"找不到版本 JSON 文件：{oldJsonPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            JObject jsonObj;
            try
            {
                string jsonText = File.ReadAllText(oldJsonPath);
                jsonObj = JObject.Parse(jsonText);
                jsonObj["id"] = newName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析或修改 JSON 文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                string tempJsonPath = Path.Combine(oldFolder, "temp_" + newName + ".json");
                File.WriteAllText(tempJsonPath, jsonObj.ToString(Newtonsoft.Json.Formatting.Indented));

                Directory.Move(oldFolder, newFolder);

                string tempJsonInNew = Path.Combine(newFolder, "temp_" + newName + ".json");
                string oldJsonInNew = Path.Combine(newFolder, oldName + ".json");
                string newJsonInNew = Path.Combine(newFolder, newName + ".json");

                if (File.Exists(oldJsonInNew))
                {
                    File.Delete(oldJsonInNew);
                    File.Move(tempJsonInNew, newJsonInNew);
                }
                else
                {
                    File.Move(tempJsonInNew, newJsonInNew);
                }

                string oldJarPath = Path.Combine(newFolder, oldName + ".jar");
                string newJarPath = Path.Combine(newFolder, newName + ".jar");
                if (File.Exists(oldJarPath))
                {
                    File.Move(oldJarPath, newJarPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名过程中发生错误：{ex.Message}\n请检查文件夹权限或手动恢复。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ---- 提取资源（可能用不到，但保留） ----
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
                if (_textBox == null || _textBox.IsDisposed)
                    return;

                try
                {
                    if (_textBox.InvokeRequired)
                        _textBox.Invoke(new Action(() => AppendText(value)));
                    else
                        AppendText(value);
                }
                catch (ObjectDisposedException) { }
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
                catch (ObjectDisposedException) { }
            }

            public override Encoding Encoding => Encoding.UTF8;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _textBox = null;
                }
                base.Dispose(disposing);
            }
        }

        private void DownloadVersions_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            if (downloadWorker != null && downloadWorker.IsBusy)
                downloadWorker.CancelAsync();
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
                Application.Exit();
        }
    }
}