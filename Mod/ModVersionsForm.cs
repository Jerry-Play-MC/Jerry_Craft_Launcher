using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace New_Launcher
{
    public partial class ModVersionsForm : Form
    {
        private string _modId;
        private ProjectType _projectType;
        private string _currentVersion;
        private bool _isVersionIsolated;
        private BackgroundWorker _worker;
        private List<ModVersion> _allVersions;
        private List<ModVersion> _filteredVersions;
        private bool _closeByBackButton = false;
        private BackgroundWorker _filterWorker;
        private WebClient _downloadClient; // 用于取消下载
        private bool _isExitingApp = false; // 标记是否正在退出程序
        private string _minecraftDir;
        private string _tempModpackFile = null; // 存储整合包临时文件路径
        private BackgroundWorker _modpackWorker;
        private WebClient _modpackWebClient; // 用于取消下载
        private string _modpackExtractTarget = null;
        private string _modpackExtractDir;

        public ModVersionsForm(string modId, ProjectType type, string currentVersion, bool isIsolated)
        {
            InitializeComponent();
            _modId = modId;
            _projectType = type;
            _currentVersion = currentVersion;
            _isVersionIsolated = isIsolated;
            this.StartPosition = FormStartPosition.CenterParent;

            _minecraftDir = MinecraftCore.GetMinecraftDir();

            // 初始化筛选工作器并启用取消
            _filterWorker = new BackgroundWorker();
            _filterWorker.WorkerSupportsCancellation = true;   // 必须设置为 true
            _filterWorker.DoWork += FilterWorker_DoWork;
            _filterWorker.RunWorkerCompleted += FilterWorker_RunWorkerCompleted;

            // 订阅事件
            this.btnDownload.Click += BtnDownload_Click;

            // 筛选/排序/搜索控件值变化触发刷新
            this.txtSearch.TextChanged += OnFilterChanged;
            this.cmbSort.SelectedIndexChanged += OnFilterChanged;
            this.cmbSortDirection.SelectedIndexChanged += OnFilterChanged;
            this.cmbGameVersion.SelectedIndexChanged += OnFilterChanged;
            this.cmbLoader.SelectedIndexChanged += OnFilterChanged;

            this.Shown += (s, e) => LoadVersions();

            
        }

        private void DownloadFileAsync(string url, string savePath, string versionNumber, Action<bool, string> onCompleted, int retryCount = 0)
        {
            string downloadUrl = GetMirrorUrl(url);
            if (retryCount > 0) // 如果重试，直接使用原始 URL
                downloadUrl = url;

            _downloadClient = new WebClient();
            _downloadClient.Headers.Add("User-Agent", "JerryStudioLauncher/1.0");
            _downloadClient.Headers.Add("Accept", "application/octet-stream");

            long lastBytes = 0;
            DateTime lastTime = DateTime.Now;

            _downloadClient.DownloadProgressChanged += (s, e) =>
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    DateTime now = DateTime.Now;
                    double deltaTime = (now - lastTime).TotalSeconds;
                    if (deltaTime > 0)
                    {
                        long deltaBytes = e.BytesReceived - lastBytes;
                        double speedKB = deltaBytes / 1024.0 / deltaTime;
                        lastBytes = e.BytesReceived;
                        lastTime = now;
                        lblStatus.Text = $"正在下载 {versionNumber}: {e.ProgressPercentage}% ({e.BytesReceived / 1024}KB / {e.TotalBytesToReceive / 1024}KB) {speedKB:F1} KB/s";
                    }
                    else
                    {
                        lblStatus.Text = $"正在下载 {versionNumber}: {e.ProgressPercentage}% ({e.BytesReceived / 1024}KB / {e.TotalBytesToReceive / 1024}KB)";
                    }
                }));
            };

            _downloadClient.DownloadFileCompleted += (s, e) =>
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    btnDownload.Enabled = true;
                    if (e.Error != null)
                    {
                        // 如果是 WebException 且状态码为 404，并且还没有重试过（即 retryCount == 0）
                        WebException webEx = e.Error as WebException;
                        if (retryCount == 0 && webEx != null)
                        {
                            HttpWebResponse response = webEx.Response as HttpWebResponse;
                            if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                            {
                                lblStatus.Text = "镜像文件不存在，尝试使用原始地址...";
                                // 关闭当前 client，避免事件重复触发
                                _downloadClient.Dispose();
                                _downloadClient = null;
                                // 用原始 URL 重试一次（retryCount = 1）
                                DownloadFileAsync(url, savePath, versionNumber, onCompleted, 1);
                                return;
                            }
                        }

                        // 其他错误或重试后仍然失败
                        MessageBox.Show($"下载失败：{e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblStatus.Text = $"下载失败：{e.Error.Message}";
                        onCompleted?.Invoke(false, null);
                        _downloadClient.Dispose();
                        _downloadClient = null;
                    }
                    else if (e.Cancelled)
                    {
                        lblStatus.Text = "下载已取消";
                        onCompleted?.Invoke(false, null);
                        _downloadClient.Dispose();
                        _downloadClient = null;
                    }
                    else
                    {
                        lblStatus.Text = $"下载完成！已保存至：{savePath}";
                        onCompleted?.Invoke(true, savePath);
                        _downloadClient.Dispose();
                        _downloadClient = null;
                    }
                }));
            };

            try
            {
                _downloadClient.DownloadFileAsync(new Uri(downloadUrl), savePath);
                lblStatus.Text = retryCount > 0 ? $"重试下载 (原始源) {versionNumber}..." : $"开始下载 {versionNumber}...";
            }
            catch (Exception ex)
            {
                btnDownload.Enabled = true;
                MessageBox.Show($"启动下载失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "下载失败";
                _downloadClient.Dispose();
                _downloadClient = null;
                onCompleted?.Invoke(false, null);
            }
        }

        private void LoadVersions()
        {
            listVersions.Items.Clear();
            listVersions.Enabled = false;
            btnDownload.Enabled = false;
            SetFilterEnabled(false);
            lblStatus.Text = "正在加载版本...";

            _worker = new BackgroundWorker();
            _worker.DoWork += (s, e) =>
            {
                try
                {
                    var versions = ModApiService.GetModVersions(_modId);
                    e.Result = versions;
                }
                catch (Exception ex)
                {
                    e.Result = ex;
                }
            };
            _worker.RunWorkerCompleted += (s, e) =>
            {
                listVersions.Enabled = true;
                btnDownload.Enabled = true;
                SetFilterEnabled(true);

                if (e.Result is Exception ex)
                {
                    MessageBox.Show($"加载版本失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    listVersions.Items.Add("加载失败，请重试");
                    lblStatus.Text = "加载失败";
                    return;
                }

                _allVersions = e.Result as List<ModVersion>;
                if (_allVersions == null || _allVersions.Count == 0)
                {
                    listVersions.Items.Add("没有找到版本");
                    lblStatus.Text = "共 0 个版本";
                    return;
                }

                FillFilterComboBoxes();
                ApplyFilter();
            };
            _worker.RunWorkerAsync();
        }

        private void SetFilterEnabled(bool enabled)
        {
            txtSearch.Enabled = enabled;
            cmbSort.Enabled = enabled;
            cmbSortDirection.Enabled = enabled;
            cmbGameVersion.Enabled = enabled;
            cmbLoader.Enabled = enabled;
        }

        private void FillFilterComboBoxes()
        {
            var gameVersions = new HashSet<string>();
            var loaders = new HashSet<string>();

            foreach (var v in _allVersions)
            {
                if (v.GameVersions != null)
                {
                    foreach (var gv in v.GameVersions)
                        gameVersions.Add(gv);
                }
                if (v.Loaders != null)
                {
                    foreach (var loader in v.Loaders)
                        loaders.Add(loader);
                }
            }

            var sortedGameVersions = gameVersions.ToList();
            sortedGameVersions.Sort((a, b) => CompareVersions(a, b));
            var sortedLoaders = loaders.ToList();
            sortedLoaders.Sort();

            string currentGame = cmbGameVersion.SelectedItem?.ToString() ?? "全部";
            string currentLoader = cmbLoader.SelectedItem?.ToString() ?? "全部";

            // ⬇️ 禁止重绘
            cmbGameVersion.BeginUpdate();
            cmbLoader.BeginUpdate();
            try
            {
                cmbGameVersion.Items.Clear();
                cmbGameVersion.Items.Add("全部");
                cmbGameVersion.Items.AddRange(sortedGameVersions.ToArray());
                cmbGameVersion.SelectedItem = cmbGameVersion.Items.Contains(currentGame) ? currentGame : "全部";

                cmbLoader.Items.Clear();
                cmbLoader.Items.Add("全部");
                cmbLoader.Items.AddRange(sortedLoaders.ToArray());
                cmbLoader.SelectedItem = cmbLoader.Items.Contains(currentLoader) ? currentLoader : "全部";
            }
            finally
            {
                cmbGameVersion.EndUpdate();
                cmbLoader.EndUpdate();
            }
        }

        private void ApplyFilter()
        {
            // 如果当前有筛选任务正在运行，取消它
            if (_filterWorker.IsBusy)
            {
                _filterWorker.CancelAsync();
                // 等待取消完成（最多 500ms，避免阻塞 UI）
                int wait = 0;
                while (_filterWorker.IsBusy && wait < 50)
                {
                    System.Threading.Thread.Sleep(10);
                    wait++;
                }
                // 如果仍然忙，直接返回，不启动新任务（用户会触发下一次筛选）
                if (_filterWorker.IsBusy)
                    return;
            }

            // 显示加载状态
            lblStatus.Text = "正在筛选...";
            listVersions.Enabled = false;

            // 准备参数
            string searchText = txtSearch.Text.Trim().ToLowerInvariant();
            string selectedGame = cmbGameVersion.SelectedItem?.ToString() ?? "全部";
            string selectedLoader = cmbLoader.SelectedItem?.ToString() ?? "全部";
            string sortKey = cmbSort.SelectedItem?.ToString() ?? "发布日期";
            bool isDescending = cmbSortDirection.SelectedItem?.ToString() == "降序(最新)";

            var args = new object[] { _allVersions, searchText, selectedGame, selectedLoader, sortKey, isDescending };
            _filterWorker.RunWorkerAsync(args);
        }

        private void EnableDoubleBuffering()
        {
            typeof(ListView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(listVersions, true, null);
        }

        private void UpdateListView(List<ModVersion> versions)
        {
            listVersions.BeginUpdate();
            try
            {
                listVersions.Items.Clear();

                if (versions == null || versions.Count == 0)
                {
                    if (_allVersions != null && _allVersions.Count > 0)
                        listVersions.Items.Add("没有符合条件的版本");
                    else
                        listVersions.Items.Add("没有找到版本");
                    lblStatus.Text = $"共 {_allVersions?.Count ?? 0} 个版本，显示 0 个";
                    return;
                }

                // ⬇️ 构建 ListViewItem 数组
                var items = new ListViewItem[versions.Count];
                for (int i = 0; i < versions.Count; i++)
                {
                    var v = versions[i];
                    var item = new ListViewItem(v.VersionNumber);
                    item.SubItems.Add(string.Join(", ", (v.GameVersions ?? new List<string>()).ToArray()));
                    item.SubItems.Add(string.Join(", ", (v.Loaders ?? new List<string>()).ToArray()));
                    item.SubItems.Add(v.DatePublished.ToString("yyyy-MM-dd"));
                    item.SubItems.Add(v.Downloads.ToString("N0"));
                    item.Tag = v;
                    items[i] = item;
                }

                // ⬇️ 一次性添加
                listVersions.Items.AddRange(items);

                lblStatus.Text = $"共 {_allVersions.Count} 个版本，显示 {versions.Count} 个";
            }
            finally
            {
                listVersions.EndUpdate();
            }

            listVersions.Enabled = true;
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            if (cmbGameVersion.Items.Count == 0 || cmbLoader.Items.Count == 0)
                return;
            ApplyFilter();
        }

        private void BtnDownload_Click(object sender, EventArgs e)
        {
            if (listVersions.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择一个版本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = listVersions.SelectedItems[0];
            var version = selected.Tag as ModVersion;
            if (version == null)
            {
                MessageBox.Show("数据异常，请重新选择", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string downloadUrl = version.Files != null && version.Files.Count > 0 ? version.Files[0].Url : null;
            if (string.IsNullOrEmpty(downloadUrl))
            {
                MessageBox.Show("该版本没有可下载的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 确定目标目录和初始文件名
            string targetDir = null;
            string defaultFileName = version.Files?[0]?.Filename ?? $"{_modId}-{version.VersionNumber}.jar";

            switch (_projectType)
            {
                case ProjectType.Mod:
                    targetDir = GetModTargetDirectory(version);  // 传入 version 进行智能匹配
                    break;
                case ProjectType.ResourcePack:
                    targetDir = GetResourcePackTargetDirectory();
                    break;
                case ProjectType.Shader:
                    targetDir = GetShaderTargetDirectory();
                    break;
                case ProjectType.DataPack:
                    HandleDataPackDownload(version, defaultFileName);
                    return;
                case ProjectType.Modpack:
                    HandleModpackDownload(version);
                    return;
                default:
                    targetDir = Path.Combine(_minecraftDir, "mods");
                    break;
            }

            if (string.IsNullOrEmpty(targetDir))
                return; // 已经弹窗提示

            // 确保目录存在
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            string savePath = Path.Combine(targetDir, defaultFileName);

            // 弹出保存对话框（可以改为直接下载，但为了友好，保留对话框）
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "保存文件";
                dialog.FileName = defaultFileName;
                dialog.Filter = _projectType == ProjectType.Mod ? "Mod 文件 (*.jar)|*.jar|所有文件 (*.*)|*.*"
                             : "所有文件 (*.*)|*.*";
                dialog.InitialDirectory = targetDir;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    savePath = dialog.FileName;
                    btnDownload.Enabled = false;
                    DownloadFileAsync(downloadUrl, savePath, version.VersionNumber);
                }
            }
        }

        private void DownloadFileWithMirror(string url, string savePath, string versionNumber, Action<bool, string> onCompleted, bool useMirror)
        {
            string downloadUrl = useMirror ? GetMirrorUrl(url) : url;
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "JerryStudioLauncher/1.0");
            client.Headers.Add("Accept", "application/octet-stream");

            long lastBytes = 0;
            DateTime lastTime = DateTime.Now;

            client.DownloadProgressChanged += (s, e) =>
            {
                // 进度更新（同前）
            };

            client.DownloadFileCompleted += (s, e) =>
            {
                client.Dispose();
                if (e.Error != null)
                {
                    // 如果使用了镜像且错误是404或任何WebException，则尝试原始URL
                    if (useMirror && e.Error is WebException wex)
                    {
                        // 检查状态码是否为404，或者任何错误（因为镜像可能根本不可达）
                        HttpWebResponse resp = wex.Response as HttpWebResponse;
                        if (resp == null || resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            // 尝试原始URL
                            this.Invoke((MethodInvoker)(() => lblStatus.Text = "镜像源不可用，切换到原始源..."));
                            DownloadFileWithMirror(url, savePath, versionNumber, onCompleted, false);
                            return;
                        }
                    }
                    // 其他错误
                    this.Invoke((MethodInvoker)(() =>
                    {
                        btnDownload.Enabled = true;
                        MessageBox.Show($"下载失败：{e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblStatus.Text = $"下载失败：{e.Error.Message}";
                        onCompleted?.Invoke(false, null);
                    }));
                    return;
                }
                // 成功
                this.Invoke((MethodInvoker)(() =>
                {
                    btnDownload.Enabled = true;
                    lblStatus.Text = $"下载完成！已保存至：{savePath}";
                    onCompleted?.Invoke(true, savePath);
                }));
            };

            try
            {
                client.DownloadFileAsync(new Uri(downloadUrl), savePath);
                this.Invoke((MethodInvoker)(() => lblStatus.Text = $"开始下载 {versionNumber}..."));
            }
            catch (Exception ex)
            {
                client.Dispose();
                this.Invoke((MethodInvoker)(() =>
                {
                    btnDownload.Enabled = true;
                    MessageBox.Show($"启动下载失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "下载失败";
                    onCompleted?.Invoke(false, null);
                }));
            }
        }

        private int CompareVersions(string a, string b)
        {
            try
            {
                var partsA = a.Split('.').Select(int.Parse).ToArray();
                var partsB = b.Split('.').Select(int.Parse).ToArray();
                int maxLen = Math.Max(partsA.Length, partsB.Length);
                for (int i = 0; i < maxLen; i++)
                {
                    int valA = i < partsA.Length ? partsA[i] : 0;
                    int valB = i < partsB.Length ? partsB[i] : 0;
                    if (valA != valB)
                        return valA.CompareTo(valB);
                }
                return 0;
            }
            catch
            {
                return string.Compare(a, b, StringComparison.Ordinal);
            }
        }

        private class VersionComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                try
                {
                    var partsX = x.Split('.').Select(int.Parse).ToArray();
                    var partsY = y.Split('.').Select(int.Parse).ToArray();
                    int maxLen = Math.Max(partsX.Length, partsY.Length);
                    for (int i = 0; i < maxLen; i++)
                    {
                        int valX = i < partsX.Length ? partsX[i] : 0;
                        int valY = i < partsY.Length ? partsY[i] : 0;
                        if (valX != valY)
                            return valX.CompareTo(valY);
                    }
                    return 0;
                }
                catch
                {
                    return string.Compare(x, y, StringComparison.Ordinal);
                }
            }
        }

        private void ModVersionsForm_Load(object sender, EventArgs e) 
        {
            EnableDoubleBuffering();
        }

        private void panelFilter_Paint(object sender, PaintEventArgs e) { }

        private void ModVersionsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 取消整合包下载
            if (_modpackWorker != null && _modpackWorker.IsBusy)
            {
                _modpackWorker.CancelAsync();
                if (_modpackWebClient != null && _modpackWebClient.IsBusy)
                {
                    _modpackWebClient.CancelAsync();
                }
                // 等待取消完成
                int wait = 0;
                while (_modpackWorker.IsBusy && wait < 50)
                {
                    System.Threading.Thread.Sleep(100);
                    wait++;
                }
            }

            // 删除临时整合包文件
            if (!string.IsNullOrEmpty(_tempModpackFile) && File.Exists(_tempModpackFile))
            {
                try { File.Delete(_tempModpackFile); }
                catch { }
            }

            // 原有的关闭逻辑...
            if (_closeByBackButton)
                return;
            Environment.Exit(0);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _closeByBackButton = true;
            this.Close();
        }

        /// <summary>
        /// 从版本 ID 中提取实际游戏版本（如 1.20.4）
        /// </summary>
        private string ExtractGameVersion(string versionId)
        {
            if (string.IsNullOrEmpty(versionId)) return null;

            // 处理 Fabric / Quilt
            if (versionId.StartsWith("fabric-loader-") || versionId.StartsWith("quilt-loader-"))
            {
                int lastDash = versionId.LastIndexOf('-');
                if (lastDash > 0)
                    return versionId.Substring(lastDash + 1);
            }

            // 处理 Forge / NeoForge
            if (versionId.Contains("-forge") || versionId.Contains("-neoforge"))
            {
                int idx = versionId.LastIndexOf("-forge");
                if (idx < 0) idx = versionId.LastIndexOf("-neoforge");
                if (idx > 0)
                    return versionId.Substring(0, idx);
            }

            // 如果是纯原版（如 1.20.4）
            return versionId;
        }

        private string GetModTargetDirectory(ModVersion version)
        {
            if (!_isVersionIsolated)
                return Path.Combine(_minecraftDir, "mods");

            string versionFolder = ResolveVersionByMod(version);
            if (versionFolder == null)
            {
                MessageBox.Show("匹配失败，您没有安装任何兼容此 Mod 的游戏版本！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return Path.Combine(Path.Combine(Path.Combine(_minecraftDir, "versions"), versionFolder), "mods");
        }

        private string GetResourcePackTargetDirectory()
        {
            if (!_isVersionIsolated)
                return Path.Combine(_minecraftDir, "resourcepacks");

            string versionFolder = ResolveVersionDirectory();
            if (versionFolder == null)
            {
                MessageBox.Show("匹配失败，您没有下载对应的游戏版本和加载器版本！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return Path.Combine(Path.Combine(Path.Combine(_minecraftDir, "versions"), versionFolder), "resourcepacks");
        }

        private string GetShaderTargetDirectory()
        {
            if (!_isVersionIsolated)
                return Path.Combine(_minecraftDir, "shaderpacks");

            string versionFolder = ResolveVersionDirectory();
            if (versionFolder == null)
            {
                MessageBox.Show("匹配失败，您没有下载对应的游戏版本和加载器版本！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            string shaderDir = Path.Combine(Path.Combine(Path.Combine(_minecraftDir, "versions"), versionFolder), "shaderpacks");
            if (!Directory.Exists(shaderDir))
            {
                MessageBox.Show("您未下载用于加载光影的 Mod（如 OptiFine 或 Iris），请先安装后再下载光影包。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return shaderDir;
        }

        private void HandleDataPackDownload(ModVersion version, string defaultFileName)
        {
            string minecraftRoot = AppDomain.CurrentDomain.BaseDirectory;
            string savesDir = Path.Combine(minecraftRoot, "saves");
            if (!Directory.Exists(savesDir))
            {
                MessageBox.Show("未找到 saves 文件夹，请先创建世界。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var worlds = Directory.GetDirectories(savesDir);
            if (worlds.Length == 0)
            {
                MessageBox.Show("未找到任何存档，请先创建世界。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 简单起见，取第一个世界（实际可弹出选择列表）
            string worldPath = worlds[0];
            string datapackDir = Path.Combine(worldPath, "datapacks");
            if (!Directory.Exists(datapackDir))
                Directory.CreateDirectory(datapackDir);

            string savePath = Path.Combine(datapackDir, defaultFileName);
            // 直接下载，不弹窗（或弹窗确认）
            var result = MessageBox.Show($"即将下载数据包到：{savePath}\n是否继续？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                string downloadUrl = version.Files?[0]?.Url;
                if (!string.IsNullOrEmpty(downloadUrl))
                    DownloadFileAsync(downloadUrl, savePath, version.VersionNumber, null);
                else
                    MessageBox.Show("该版本没有可下载的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void HandleModpackDownload(ModVersion version)
        {
            string downloadUrl = version.Files?[0]?.Url;
            if (string.IsNullOrEmpty(downloadUrl))
            {
                MessageBox.Show("该版本没有可下载的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 获取文件名（用于缓存）
            string defaultFileName = version.Files?[0]?.Filename ?? $"{_modId}-{version.VersionNumber}.zip";
            string cacheDir = Path.Combine(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Launcher Setting"), "ModPack");
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);
            string cachedFilePath = Path.Combine(cacheDir, defaultFileName);

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择整合包解压目标目录（建议选择 .minecraft\\versions）";
                string defaultPath = Path.Combine(_minecraftDir, "versions");
                if (Directory.Exists(defaultPath))
                    dialog.SelectedPath = defaultPath;
                else
                    dialog.SelectedPath = _minecraftDir;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _modpackExtractDir = dialog.SelectedPath;
                    btnDownload.Enabled = false;

                    // 检查缓存
                    if (File.Exists(cachedFilePath))
                    {
                        lblStatus.Text = $"使用缓存整合包：{defaultFileName}";
                        ProcessModpack(cachedFilePath, version);
                        btnDownload.Enabled = true;
                        return;
                    }

                    // 下载到缓存目录
                    lblStatus.Text = "正在下载整合包...";
                    DownloadFileAsync(downloadUrl, cachedFilePath, version.VersionNumber, (success, downloadedPath) =>
                    {
                        if (!success)
                        {
                            btnDownload.Enabled = true;
                            return;
                        }
                        ProcessModpack(downloadedPath, version);
                        btnDownload.Enabled = true;
                    });
                }
            }
        }

        private void ProcessModpack(string filePath, ModVersion version)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    string extractTemp = Path.Combine(Path.GetTempPath(), "modpack_extract_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(extractTemp);

                    // ★ 统一定义 overridesDir（只在开头定义一次）
                    string overridesDir = Path.Combine(extractTemp, "overrides");

                    var fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();
                    fastZip.ExtractZip(filePath, extractTemp, null);

                    string indexJsonPath = Path.Combine(extractTemp, "modrinth.index.json");
                    if (File.Exists(indexJsonPath))
                    {
                        string json = File.ReadAllText(indexJsonPath);
                        var index = JObject.Parse(json);
                        string mcVersion = index["dependencies"]?["minecraft"]?.ToString();

                        // ---- 1. 检测加载器类型和版本 ----
                        string loaderType = "vanilla";
                        string loaderVersion = null;

                        // 优先级1：从 dependencies 读取（含版本号）
                        if (index["dependencies"]?["neoforge"] != null)
                        {
                            loaderType = "neoforge";
                            loaderVersion = index["dependencies"]["neoforge"].ToString();
                        }
                        else if (index["dependencies"]?["forge"] != null)
                        {
                            loaderType = "forge";
                            loaderVersion = index["dependencies"]["forge"].ToString();
                            if (!loaderVersion.Contains("-"))
                                loaderVersion = $"{mcVersion}-{loaderVersion}";
                        }
                        else if (index["dependencies"]?["fabric-loader"] != null)
                        {
                            loaderType = "fabric";
                            loaderVersion = index["dependencies"]["fabric-loader"].ToString();
                        }
                        else if (index["dependencies"]?["quilt-loader"] != null)
                        {
                            loaderType = "quilt";
                            loaderVersion = index["dependencies"]["quilt-loader"].ToString();
                        }

                        // 优先级2：如果 dependencies 中没有，使用 API 返回的加载器类型
                        if (loaderType == "vanilla" && version.Loaders != null && version.Loaders.Count > 0)
                        {
                            string apiLoader = version.Loaders.FirstOrDefault(l =>
                                l.Equals("fabric", StringComparison.OrdinalIgnoreCase) ||
                                l.Equals("forge", StringComparison.OrdinalIgnoreCase) ||
                                l.Equals("quilt", StringComparison.OrdinalIgnoreCase) ||
                                l.Equals("neoforge", StringComparison.OrdinalIgnoreCase));
                            if (!string.IsNullOrEmpty(apiLoader))
                            {
                                loaderType = apiLoader.ToLowerInvariant();
                                loaderVersion = "latest"; // API 没有版本信息，标记为 latest
                                Console.WriteLine($"[整合包] 从 API (version.Loaders) 获取加载器类型: {loaderType}");
                            }
                        }

                        // 优先级3：从 mods 目录推断（仅当上面都未识别时）
                        string modsDirForDetection = Path.Combine(overridesDir, "mods");
                        if (loaderType == "vanilla" && Directory.Exists(modsDirForDetection))
                        {
                            var jarFiles = Directory.GetFiles(modsDirForDetection, "*.jar");
                            foreach (var jar in jarFiles)
                            {
                                string fileName = Path.GetFileName(jar).ToLowerInvariant();
                                if (fileName.Contains("fabric") || fileName.Contains("fabric-api") ||
                                    fileName.Contains("sodium") || fileName.Contains("iris"))
                                {
                                    loaderType = "fabric";
                                    loaderVersion = "latest";
                                    Console.WriteLine($"[整合包] 从 mods 目录推断加载器类型为: fabric");
                                    break;
                                }
                                else if (fileName.Contains("quilt"))
                                {
                                    loaderType = "quilt";
                                    loaderVersion = "latest";
                                    Console.WriteLine($"[整合包] 从 mods 目录推断加载器类型为: quilt");
                                    break;
                                }
                                else if (fileName.Contains("forge"))
                                {
                                    loaderType = "forge";
                                    loaderVersion = "latest";
                                    Console.WriteLine($"[整合包] 从 mods 目录推断加载器类型为: forge");
                                    break;
                                }
                                else if (fileName.Contains("neoforge"))
                                {
                                    loaderType = "neoforge";
                                    loaderVersion = "latest";
                                    Console.WriteLine($"[整合包] 从 mods 目录推断加载器类型为: neoforge");
                                    break;
                                }
                            }
                        }

                        // 最后保底：如果还是 vanilla 但有 mods 目录，推测为 fabric
                        if (loaderType == "vanilla" && Directory.Exists(modsDirForDetection))
                        {
                            bool hasForgeMod = Directory.GetFiles(modsDirForDetection, "*.jar")
                                .Any(f => Path.GetFileName(f).ToLowerInvariant().Contains("forge"));
                            loaderType = hasForgeMod ? "forge" : "fabric";
                            loaderVersion = "latest";
                            Console.WriteLine($"[整合包] 无法从 dependencies 和文件名推断，默认使用: {loaderType}");
                        }

                        if (string.IsNullOrEmpty(mcVersion))
                            throw new Exception("无法从 modrinth.index.json 中解析 Minecraft 版本号");

                        // ---- 2. 安装加载器（如果有） ----
                        string versionId = null;
                        string originalLoaderType = loaderType;

                        if (loaderVersion == "latest")
                        {
                            Console.WriteLine($"[整合包] 未指定 {loaderType} 版本号，正在获取最新稳定版...");

                            // 根据加载器类型获取最新版本
                            string latestVersion = GetLatestLoaderVersion(loaderType, mcVersion);

                            if (!string.IsNullOrEmpty(latestVersion))
                            {
                                loaderVersion = latestVersion;
                                Console.WriteLine($"[整合包] 获取到最新 {loaderType} 版本: {loaderVersion}");
                            }
                            else
                            {
                                Console.WriteLine($"[整合包] 无法获取 {loaderType} 最新版本，跳过加载器安装，使用原版核心");
                                versionId = mcVersion;
                                VanillaManager.Installer.DownloadVanilla(mcVersion, _minecraftDir, mcVersion, false);
                                // 注意：这里不改变 originalLoaderType，依赖补全仍使用推断类型
                            }
                        }

                        // 如果 loaderVersion 现在不是 "latest" 且不为空，则执行安装
                        if (loaderVersion != "latest" && !string.IsNullOrEmpty(loaderVersion) && loaderType != "vanilla")
                        {
                            this.Invoke((MethodInvoker)(() => lblStatus.Text = $"正在安装 {loaderType} {loaderVersion}..."));

                            switch (loaderType)
                            {
                                case "forge":
                                    versionId = ForgeManager.Installer.InstallForge(mcVersion, loaderVersion, false);
                                    break;
                                case "fabric":
                                    versionId = FabricManager.Installer.InstallFabric(mcVersion, loaderVersion, false);
                                    break;
                                case "quilt":
                                    versionId = QuiltManager.Installer.InstallQuilt(mcVersion, loaderVersion, false);
                                    break;
                                case "neoforge":
                                    versionId = NeoForgeManager.Installer.InstallNeoForge(mcVersion, loaderVersion, false);
                                    break;
                                case "vanilla":
                                    versionId = mcVersion;
                                    VanillaManager.Installer.DownloadVanilla(mcVersion, _minecraftDir, mcVersion, false);
                                    break;
                            }

                            if (string.IsNullOrEmpty(versionId))
                                throw new Exception($"安装 {loaderType} 加载器失败，返回的版本 ID 为空");
                        }
                        else if (string.IsNullOrEmpty(versionId) && loaderType != "vanilla")
                        {
                            // 如果上面没有设置 versionId（例如获取最新版本失败），则使用原版
                            versionId = mcVersion;
                            VanillaManager.Installer.DownloadVanilla(mcVersion, _minecraftDir, mcVersion, false);
                        }

                        // ---- 3. 创建整合包版本目录 ----
                        string packName = index["name"]?.ToString() ?? "Modpack";
                        foreach (char c in Path.GetInvalidFileNameChars())
                            packName = packName.Replace(c, '_');
                        if (string.IsNullOrEmpty(packName) || packName.Trim().Length == 0)
                            packName = "Modpack_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                        string packDir = Path.Combine(_modpackExtractDir, packName);
                        Directory.CreateDirectory(packDir);

                        // ---- 4. 复制加载器的 version.json 和核心 jar ----
                        if (versionId != null)
                        {
                            string loaderVersionDir = Path.Combine(Path.Combine(_minecraftDir, "versions"), versionId);
                            string loaderJsonPath = Path.Combine(loaderVersionDir, versionId + ".json");
                            if (File.Exists(loaderJsonPath))
                            {
                                string loaderJson = File.ReadAllText(loaderJsonPath);
                                var jsonObj = JObject.Parse(loaderJson);
                                jsonObj["id"] = packName;
                                if (jsonObj["inheritsFrom"] == null)
                                    jsonObj["inheritsFrom"] = mcVersion;
                                string newJsonPath = Path.Combine(packDir, packName + ".json");
                                File.WriteAllText(newJsonPath, jsonObj.ToString(Newtonsoft.Json.Formatting.Indented));
                            }

                            string loaderJarPath = Path.Combine(loaderVersionDir, versionId + ".jar");
                            string targetJarPath = Path.Combine(packDir, packName + ".jar");
                            if (File.Exists(loaderJarPath) && !File.Exists(targetJarPath))
                                File.Copy(loaderJarPath, targetJarPath, true);
                        }

                        // ---- 5. 复制 overrides ----
                        if (Directory.Exists(overridesDir))
                            CopyDirectory(overridesDir, packDir, true);
                        else
                        {
                            foreach (string dir in Directory.GetDirectories(extractTemp))
                            {
                                string dirName = Path.GetFileName(dir);
                                if (dirName != "overrides" && dirName != "META-INF")
                                    CopyDirectory(dir, packDir, true);
                            }
                            foreach (string file in Directory.GetFiles(extractTemp))
                            {
                                string fileName = Path.GetFileName(file);
                                if (fileName != "modrinth.index.json")
                                    File.Copy(file, Path.Combine(packDir, fileName), true);
                            }
                        }

                        // ---- 6. 清理 overrides 中可能损坏的模组 ----
                        string modsDir = Path.Combine(packDir, "mods");
                        CleanCorruptedFiles(modsDir);

                        // ---- 7. 处理 modrinth.index.json 中的 files 列表 ----
                        List<DownloadTaskWithSha> downloadTasks = new List<DownloadTaskWithSha>();
                        if (index["files"] is JArray filesArray)
                        {
                            foreach (var fileEntry in filesArray)
                            {
                                string path = fileEntry["path"]?.ToString();
                                var downloads = fileEntry["downloads"] as JArray;
                                string downloadUrl = downloads?.FirstOrDefault()?.ToString();
                                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(downloadUrl))
                                    continue;

                                string expectedSha1 = fileEntry["hashes"]?["sha1"]?.ToString();
                                if (string.IsNullOrEmpty(expectedSha1))
                                    expectedSha1 = fileEntry["sha1"]?.ToString();

                                string targetPath = Path.Combine(packDir, path.Replace('/', Path.DirectorySeparatorChar));
                                string targetDir = Path.GetDirectoryName(targetPath);
                                if (!Directory.Exists(targetDir))
                                    Directory.CreateDirectory(targetDir);

                                if (File.Exists(targetPath) && IsValidJarFile(targetPath, expectedSha1))
                                    continue;

                                downloadTasks.Add(new DownloadTaskWithSha
                                {
                                    Url = downloadUrl,
                                    SavePath = targetPath,
                                    FileName = Path.GetFileName(path),
                                    ExpectedSha1 = expectedSha1
                                });
                            }
                        }

                        // ---- 8. 第一轮下载 ----
                        List<string> failedFiles = new List<string>();
                        object lockFailed = new object();
                        int total = downloadTasks.Count;
                        int completed = 0;
                        int failedCount = 0;

                        if (total > 0)
                        {
                            this.Invoke((MethodInvoker)(() => lblStatus.Text = $"正在下载文件: 0/{total}"));
                            var waitHandles = new List<ManualResetEvent>();
                            int maxConcurrent = Math.Min(10, total);

                            using (var semaphore = new Semaphore(maxConcurrent, maxConcurrent))
                            {
                                foreach (var task in downloadTasks)
                                {
                                    ManualResetEvent doneEvent = new ManualResetEvent(false);
                                    waitHandles.Add(doneEvent);

                                    ThreadPool.QueueUserWorkItem(stateObj =>
                                    {
                                        try
                                        {
                                            semaphore.WaitOne();
                                            bool success = DownloadFileWithRetry(
                                                task.Url,
                                                task.SavePath,
                                                task.ExpectedSha1,
                                                3
                                            );

                                            if (success)
                                            {
                                                Interlocked.Increment(ref completed);
                                                int currentCompleted = completed;
                                                this.Invoke((MethodInvoker)(() =>
                                                {
                                                    lblStatus.Text = $"正在下载文件[{currentCompleted}/{total}]: {task.FileName}";
                                                }));
                                            }
                                            else
                                            {
                                                lock (lockFailed) failedFiles.Add(task.FileName);
                                                Interlocked.Increment(ref failedCount);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            lock (lockFailed) failedFiles.Add(task.FileName);
                                            Interlocked.Increment(ref failedCount);
                                            Console.WriteLine($"[整合包] 下载异常: {task.FileName} - {ex.Message}");
                                        }
                                        finally
                                        {
                                            semaphore.Release();
                                            doneEvent.Set();
                                        }
                                    });
                                }

                                foreach (var wh in waitHandles)
                                {
                                    wh.WaitOne();
                                    wh.Close();
                                }
                            }

                            this.Invoke((MethodInvoker)(() =>
                            {
                                lblStatus.Text = $"下载完成，成功 {completed} 个，失败 {failedCount} 个";
                            }));
                        }

                        // ---- 9. 第二轮重试 ----
                        if (failedFiles.Count > 0)
                        {
                            Console.WriteLine($"[整合包] 开始第二轮重试，共 {failedFiles.Count} 个文件");
                            List<string> finalFailed = new List<string>();
                            int retryCompleted = 0;

                            var retryTasks = downloadTasks.Where(t => failedFiles.Contains(t.FileName)).ToList();
                            foreach (var task in retryTasks)
                            {
                                bool success = DownloadFileWithRetry(
                                    task.Url,
                                    task.SavePath,
                                    task.ExpectedSha1,
                                    2
                                );

                                if (success)
                                {
                                    retryCompleted++;
                                    Console.WriteLine($"[整合包] 重试成功: {task.FileName}");
                                }
                                else
                                {
                                    finalFailed.Add(task.FileName);
                                    Console.WriteLine($"[整合包] 重试失败: {task.FileName}");
                                }
                            }

                            Console.WriteLine($"[整合包] 第二轮重试完成，成功 {retryCompleted} 个，仍失败 {finalFailed.Count} 个");

                            if (finalFailed.Count > 0)
                            {
                                this.Invoke((MethodInvoker)(() =>
                                {
                                    string message = "以下整合包文件在多次重试后仍然下载失败，请检查网络或手动下载：\n" +
                                                     string.Join("\n", finalFailed.ToArray()) +
                                                     "\n\n部分模组缺失可能导致游戏功能不完整。";
                                    MessageBox.Show(message, "整合包下载不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }));
                            }
                        }

                        // ---- 10. 最终清理 ----
                        CleanCorruptedFiles(modsDir);

                        // ---- 11. 依赖补全 ----
                        // ★ 使用原始加载器类型（即使跳过安装也保留）
                        DownloadMissingDependencies(packDir, mcVersion, originalLoaderType);

                        // ---- 12. 清理临时目录 ----
                        try { Directory.Delete(extractTemp, true); } catch { }

                        this.Invoke((MethodInvoker)(() =>
                        {
                            lblStatus.Text = $"整合包解压完成：{packDir}";
                            MessageBox.Show($"整合包已安装到：{packDir}\n\n现在可以在启动器中选中 '{packName}' 版本启动游戏。", "安装成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    }
                    else
                    {
                        // ---- 没有 index：直接复制所有内容 ----
                        foreach (string dir in Directory.GetDirectories(extractTemp))
                        {
                            string dirName = Path.GetFileName(dir);
                            if (dirName != "META-INF")
                                CopyDirectory(dir, _modpackExtractDir, true);
                        }
                        foreach (string file in Directory.GetFiles(extractTemp))
                        {
                            string fileName = Path.GetFileName(file);
                            if (fileName != "modrinth.index.json")
                                File.Copy(file, Path.Combine(_modpackExtractDir, fileName), true);
                        }

                        try { Directory.Delete(extractTemp, true); } catch { }

                        this.Invoke((MethodInvoker)(() =>
                        {
                            lblStatus.Text = $"整合包已解压到：{_modpackExtractDir}（缺少 modrinth.index.json，请手动设置版本）";
                            MessageBox.Show($"整合包已解压到：{_modpackExtractDir}\n但缺少 modrinth.index.json，可能无法直接启动。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        MessageBox.Show($"处理整合包失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        /// <summary>
        /// 扫描整合包 mods 目录，解析依赖并下载缺失的模组
        /// </summary>
        /// <param name="packDir">整合包版本目录（如 .minecraft/versions/MyModpack）</param>
        /// <param name="mcVersion">Minecraft 游戏版本（如 1.19.2）</param>
        /// <param name="loaderType">加载器类型（从 modrinth.index.json 中解析，如 "forge"、"fabric"、"quilt"）</param>
        private void DownloadMissingDependencies(string packDir, string mcVersion, string loaderType)
        {
            Console.WriteLine($"[依赖补全] 开始检测依赖，packDir={packDir}, mcVersion={mcVersion}, loaderType={loaderType}");

            string modsDir = Path.Combine(packDir, "mods");
            if (!Directory.Exists(modsDir))
            {
                Console.WriteLine("[依赖补全] mods 目录不存在，跳过");
                return;
            }

            // 先清理已存在的损坏文件
            CleanCorruptedFiles(modsDir);

            // 1. 获取所有已安装模组的 ID 集合
            var installedMods = new HashSet<string>();
            foreach (var file in Directory.GetFiles(modsDir, "*.jar"))
            {
                string modId = ExtractModIdFromJar(file);
                if (!string.IsNullOrEmpty(modId))
                {
                    installedMods.Add(modId);
                    Console.WriteLine($"[依赖补全] 已安装: {modId} ({Path.GetFileName(file)})");
                }
            }
            Console.WriteLine($"[依赖补全] 共 {installedMods.Count} 个已安装模组");

            // 2. 解析每个模组的依赖
            var missingDeps = new HashSet<string>();
            foreach (var file in Directory.GetFiles(modsDir, "*.jar"))
            {
                var deps = ExtractDependenciesFromJar(file);
                foreach (var dep in deps)
                {
                    if (dep == "minecraft" || dep == "java" || dep == "fabricloader" || dep == "forge")
                        continue;

                    if (!installedMods.Contains(dep))
                        missingDeps.Add(dep);
                }
            }

            // ★ 检查是否需要自动下载 Fabric API
            if (loaderType.Equals("fabric", StringComparison.OrdinalIgnoreCase))
            {
                // 检查缺失依赖中是否有 Fabric API 的内部模块
                bool hasFabricApiModules = missingDeps.Any(d =>
                    d.StartsWith("fabric-") &&
                    !d.Equals("fabricloader", StringComparison.OrdinalIgnoreCase) &&
                    !d.Equals("fabric-carpet", StringComparison.OrdinalIgnoreCase) &&
                    !d.Equals("fabric-language-kotlin", StringComparison.OrdinalIgnoreCase));

                if (hasFabricApiModules && !installedMods.Contains("fabric-api"))
                {
                    Console.WriteLine("[依赖补全] 检测到 Fabric API 内部模块依赖，自动下载 Fabric API 最新稳定版...");
                    string fabricApiUrl = SearchAndGetDownloadUrl("fabric-api", mcVersion, loaderType);
                    if (!string.IsNullOrEmpty(fabricApiUrl))
                    {
                        string savePath = Path.Combine(modsDir, "fabric-api-latest.jar");
                        bool success = DownloadFileWithRetry(fabricApiUrl, savePath, null, 3);
                        if (success)
                        {
                            Console.WriteLine("[依赖补全] Fabric API 下载成功。");
                            installedMods.Add("fabric-api"); // 标记已安装，避免循环
                                                             // 从 missingDeps 中移除所有 Fabric API 内部模块，因为它们将由 fabric-api 提供
                            missingDeps.RemoveWhere(d =>
                                d.StartsWith("fabric-") &&
                                !d.Equals("fabricloader", StringComparison.OrdinalIgnoreCase) &&
                                !d.Equals("fabric-carpet", StringComparison.OrdinalIgnoreCase) &&
                                !d.Equals("fabric-language-kotlin", StringComparison.OrdinalIgnoreCase));
                            Console.WriteLine("[依赖补全] 已移除 Fabric API 内部模块依赖，它们将由 fabric-api 提供。");
                        }
                        else
                        {
                            Console.WriteLine("[依赖补全] Fabric API 下载失败，将尝试单独搜索各个模块。");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[依赖补全] 未找到 Fabric API 的下载链接，将尝试单独搜索各个模块。");
                    }
                }
            }

            if (missingDeps.Count == 0)
            {
                Console.WriteLine("[依赖补全] 所有依赖均已满足，无需下载");
                return;
            }

            Console.WriteLine($"[依赖补全] 发现 {missingDeps.Count} 个缺失依赖: {string.Join(", ", missingDeps.ToArray())}");

            // 3. 下载缺失的模组
            int successCount = 0;
            int failCount = 0;
            foreach (var modId in missingDeps)
            {
                try
                {
                    Console.WriteLine($"[依赖补全] 正在搜索 {modId} (加载器: {loaderType}, 游戏版本: {mcVersion})...");
                    string downloadUrl = SearchAndGetDownloadUrl(modId, mcVersion, loaderType);

                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        Console.WriteLine($"[依赖补全] ❌ 未找到 {modId} 的下载链接，跳过");
                        failCount++;
                        continue;
                    }

                    // 生成文件名
                    string fileName = $"{modId}-{mcVersion}.jar";
                    string savePath = Path.Combine(modsDir, fileName);

                    bool success = DownloadFileWithRetry(downloadUrl, savePath, null, 3);

                    if (success)
                    {
                        Console.WriteLine($"[依赖补全] ✅ 下载完成: {savePath}");
                        successCount++;
                    }
                    else
                    {
                        Console.WriteLine($"[依赖补全] ❌ 下载失败: {modId}");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[依赖补全] ❌ 下载 {modId} 失败: {ex.Message}");
                    failCount++;
                }
            }

            // 最终清理
            CleanCorruptedFiles(modsDir);

            Console.WriteLine($"[依赖补全] 完成！成功 {successCount} 个，失败 {failCount} 个");
            if (failCount > 0)
            {
                Console.WriteLine("[依赖补全] 部分模组未能自动下载，可能需要手动安装。");
            }
        }

        private string ExtractModIdFromJar(string jarPath)
        {
            try
            {
                using (var fs = File.OpenRead(jarPath))
                using (var zip = new ZipFile(fs))
                {
                    // Fabric / Quilt
                    ZipEntry fabricEntry = zip.GetEntry("fabric.mod.json");
                    if (fabricEntry != null)
                    {
                        using (var stream = zip.GetInputStream(fabricEntry))
                        using (var reader = new StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            var obj = JObject.Parse(json);
                            return obj["id"]?.ToString();
                        }
                    }

                    // Forge / NeoForge
                    ZipEntry forgeEntry = zip.GetEntry("META-INF/mods.toml");
                    if (forgeEntry != null)
                    {
                        using (var stream = zip.GetInputStream(forgeEntry))
                        using (var reader = new StreamReader(stream))
                        {
                            string toml = reader.ReadToEnd();
                            var match = Regex.Match(toml, @"modId\s*=\s*""([^""]+)""");
                            if (match.Success)
                                return match.Groups[1].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[依赖补全] 提取 {jarPath} 的 modId 失败: {ex.Message}");
            }
            return null;
        }

        private List<string> ExtractDependenciesFromJar(string jarPath)
        {
            var deps = new List<string>();
            try
            {
                using (var fs = File.OpenRead(jarPath))
                using (var zip = new ZipFile(fs))
                {
                    // Forge mods.toml
                    ZipEntry forgeEntry = zip.GetEntry("META-INF/mods.toml");
                    if (forgeEntry != null)
                    {
                        using (var stream = zip.GetInputStream(forgeEntry))
                        using (var reader = new StreamReader(stream))
                        {
                            string toml = reader.ReadToEnd();
                            // 匹配 [[dependencies.xxx]] 行
                            var matches = Regex.Matches(toml, @"^\[+dependencies\.([\w.]+)\]+\s*$", RegexOptions.Multiline);
                            foreach (Match m in matches)
                            {
                                if (m.Groups.Count > 1)
                                    deps.Add(m.Groups[1].Value);
                            }
                        }
                    }

                    // Fabric / Quilt fabric.mod.json
                    ZipEntry fabricEntry = zip.GetEntry("fabric.mod.json");
                    if (fabricEntry != null)
                    {
                        using (var stream = zip.GetInputStream(fabricEntry))
                        using (var reader = new StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            var obj = JObject.Parse(json);
                            var depends = obj["depends"] as JObject;
                            if (depends != null)
                            {
                                foreach (var prop in depends.Properties())
                                {
                                    // 只添加模组依赖，跳过 Minecraft、Java、Fabric Loader
                                    if (prop.Name != "minecraft" && prop.Name != "java" && prop.Name != "fabricloader")
                                        deps.Add(prop.Name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[依赖补全] 提取 {jarPath} 的依赖失败: {ex.Message}");
            }
            return deps;
        }

        /// <summary>
        /// 搜索指定模组的特定版本
        /// </summary>
        /// <param name="modId">模组 ID</param>
        /// <param name="mcVersion">Minecraft 版本</param>
        /// <param name="loaderType">加载器类型</param>
        /// <param name="expectedVersion">期望的版本号</param>
        /// <returns>匹配该版本号的下载 URL，找不到则返回 null</returns>
        private string SearchAndGetDownloadUrl(string modId, string mcVersion, string loaderType, string expectedVersion = null)
        {
            string searchUrl = $"https://api.modrinth.com/v2/search?query={Uri.EscapeDataString(modId)}&limit=1";
            try
            {
                using (var client = new WebClient())
                {
                    string json = client.DownloadString(searchUrl);
                    var obj = JObject.Parse(json);
                    var hits = obj["hits"] as JArray;
                    if (hits == null || hits.Count == 0)
                        return null;

                    string projectId = hits[0]["project_id"]?.ToString();
                    if (string.IsNullOrEmpty(projectId))
                        return null;

                    string versionsUrl = $"https://api.modrinth.com/v2/project/{projectId}/version";
                    string versionsJson = client.DownloadString(versionsUrl);
                    var versions = JArray.Parse(versionsJson);

                    foreach (var version in versions)
                    {
                        // 首先检查版本号是否匹配
                        string versionNumber = version["version_number"]?.ToString();
                        if (string.IsNullOrEmpty(versionNumber) || versionNumber != expectedVersion)
                            continue;

                        var gameVersions = version["game_versions"] as JArray;
                        var loaders = version["loaders"] as JArray;
                        if (gameVersions != null && gameVersions.Contains(mcVersion) &&
                            loaders != null && loaders.Contains(loaderType.ToLowerInvariant()))
                        {
                            var files = version["files"] as JArray;
                            if (files != null && files.Count > 0)
                            {
                                foreach (var file in files)
                                {
                                    if (file["primary"]?.Value<bool>() == true)
                                        return file["url"]?.ToString();
                                }
                                return files[0]["url"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[搜索] 搜索 {modId} 版本 {expectedVersion} 失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 解析最匹配的版本文件夹名称
        /// </summary>
        /// <param name="minecraftRoot">.minecraft 目录</param>
        /// <returns>版本文件夹名，若找不到则返回 null</returns>
        /// <summary>
        /// 解析最匹配的版本文件夹名称，优先读取 JSON 元数据
        /// </summary>
        // 删除带参的 ResolveVersionDirectory，只保留无参版本
        private string ResolveVersionDirectory()
        {
            if (string.IsNullOrEmpty(_currentVersion))
            {
                Console.WriteLine("[Resolve] _currentVersion 为空");
                return null;
            }

            string versionsDir = Path.Combine(_minecraftDir, "versions");
            if (!Directory.Exists(versionsDir))
            {
                Console.WriteLine("[Resolve] versions 目录不存在: " + versionsDir);
                return null;
            }

            string currentGameVersion = ExtractGameVersion(_currentVersion);
            string currentLoaderType = MinecraftCore.GetLoaderType(_currentVersion);
            Console.WriteLine($"[Resolve] 当前游戏版本: {currentGameVersion}, 加载器类型: {currentLoaderType}");

            // 1. 直接匹配完整 ID
            string fullPath = Path.Combine(versionsDir, _currentVersion);
            if (Directory.Exists(fullPath))
            {
                Console.WriteLine($"[Resolve] 直接匹配成功: {_currentVersion}");
                return _currentVersion;
            }

            // 2. 扫描 JSON
            var allDirs = Directory.GetDirectories(versionsDir);
            foreach (string dir in allDirs)
            {
                string name = Path.GetFileName(dir);
                string jsonPath = Path.Combine(dir, name + ".json");
                if (!File.Exists(jsonPath))
                    continue;

                try
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(jsonPath));
                    string id = json["id"]?.ToString();
                    string inheritsFrom = json["inheritsFrom"]?.ToString();

                    Console.WriteLine($"[Resolve] 检查目录 {name}: id={id}, inheritsFrom={inheritsFrom}");

                    if (!string.IsNullOrEmpty(id) && id.Equals(_currentVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[Resolve] 通过 id 匹配成功: {name}");
                        return name;
                    }

                    if (!string.IsNullOrEmpty(inheritsFrom) && inheritsFrom == currentGameVersion)
                    {
                        string dirLoader = MinecraftCore.GetLoaderType(name);
                        if (dirLoader == currentLoaderType)
                        {
                            Console.WriteLine($"[Resolve] 通过 inheritsFrom + 加载器匹配成功: {name}");
                            return name;
                        }
                    }

                    if (!string.IsNullOrEmpty(id) && id.Contains(currentGameVersion))
                    {
                        Console.WriteLine($"[Resolve] 通过 id 包含游戏版本匹配成功: {name}");
                        return name;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Resolve] 解析 {jsonPath} 失败: {ex.Message}");
                }
            }

            // 3. 后备
            if (currentLoaderType == "Vanilla" && !string.IsNullOrEmpty(currentGameVersion))
            {
                foreach (string dir in allDirs)
                {
                    string name = Path.GetFileName(dir);
                    if (name.Contains(currentGameVersion))
                    {
                        Console.WriteLine($"[Resolve] Vanilla 后备匹配成功: {name}");
                        return name;
                    }
                }
            }

            Console.WriteLine("[Resolve] 所有匹配方式均失败");
            return null;
        }

        private void FilterWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as object[];
            var allVersions = args[0] as List<ModVersion>;
            string searchText = args[1] as string;
            string selectedGame = args[2] as string;
            string selectedLoader = args[3] as string;
            string sortKey = args[4] as string;
            bool isDescending = (bool)args[5];

            var worker = sender as BackgroundWorker;
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            // 执行筛选（注意：如果数据量大，可穿插检查取消）
            var filtered = allVersions;

            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.FindAll(v =>
                    v.VersionNumber != null && v.VersionNumber.ToLowerInvariant().Contains(searchText));
                if (worker.CancellationPending) { e.Cancel = true; return; }
            }

            if (selectedGame != "全部")
            {
                filtered = filtered.FindAll(v =>
                    v.GameVersions != null && v.GameVersions.Contains(selectedGame));
                if (worker.CancellationPending) { e.Cancel = true; return; }
            }

            if (selectedLoader != "全部")
            {
                filtered = filtered.FindAll(v =>
                    v.Loaders != null && v.Loaders.Contains(selectedLoader));
                if (worker.CancellationPending) { e.Cancel = true; return; }
            }

            switch (sortKey)
            {
                case "发布日期":
                    filtered = isDescending
                        ? filtered.OrderByDescending(v => v.DatePublished).ToList()
                        : filtered.OrderBy(v => v.DatePublished).ToList();
                    break;
                case "下载次数":
                    filtered = isDescending
                        ? filtered.OrderByDescending(v => v.Downloads).ToList()
                        : filtered.OrderBy(v => v.Downloads).ToList();
                    break;
                case "版本号":
                    filtered = isDescending
                        ? filtered.OrderByDescending(v => v.VersionNumber, new VersionComparer()).ToList()
                        : filtered.OrderBy(v => v.VersionNumber, new VersionComparer()).ToList();
                    break;
                default:
                    filtered = filtered.OrderByDescending(v => v.DatePublished).ToList();
                    break;
            }
            if (worker.CancellationPending) { e.Cancel = true; return; }

            e.Result = filtered;
        }

        private void FilterWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                return;

            if (e.Error != null)
            {
                MessageBox.Show($"筛选失败：{e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var filtered = e.Result as List<ModVersion>;
            _filteredVersions = filtered;
            UpdateListView(filtered);
            listVersions.Enabled = true;
        }

        /// <summary>
        /// 根据 Mod 支持的 Minecraft 版本和加载器类型，匹配已安装的版本目录
        /// </summary>
        private string ResolveVersionByMod(ModVersion version)
        {
            if (version == null || version.GameVersions == null || version.GameVersions.Count == 0)
            {
                // 如果 Mod 没有指明支持版本，则降级为使用主窗体版本
                Console.WriteLine("[Resolve] Mod 没有指定支持的版本，使用主窗体版本");
                return ResolveVersionDirectory(); // 原有的基于 _currentVersion 的匹配
            }

            string versionsDir = Path.Combine(_minecraftDir, "versions");
            if (!Directory.Exists(versionsDir))
            {
                Console.WriteLine("[Resolve] versions 目录不存在");
                return null;
            }

            // 获取所有已安装版本信息
            var installedVersions = new List<InstalledVersion>();
            var allDirs = Directory.GetDirectories(versionsDir);
            foreach (string dir in allDirs)
            {
                string name = Path.GetFileName(dir);
                string jsonPath = Path.Combine(dir, name + ".json");
                if (!File.Exists(jsonPath)) continue;

                try
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(jsonPath));
                    string id = json["id"]?.ToString();
                    string inheritsFrom = json["inheritsFrom"]?.ToString();
                    string gameVersion = inheritsFrom ?? id; // 如果 inheritsFrom 存在，则它是真实游戏版本
                    if (string.IsNullOrEmpty(gameVersion)) continue;

                    // 获取加载器类型
                    string loaderType = MinecraftCore.GetLoaderType(name);
                    installedVersions.Add(new InstalledVersion
                    {
                        FolderName = name,
                        GameVersion = gameVersion,
                        LoaderType = loaderType,
                        Id = id
                    });
                }
                catch { /* 忽略无效 JSON */ }
            }

            if (installedVersions.Count == 0)
                return null;

            // 获取 Mod 支持的加载器（例如 fabric, forge 等）
            var modLoaders = version.Loaders ?? new List<string>();

            // 优先匹配：游戏版本一致 + 加载器类型匹配
            var candidates = installedVersions
                .Where(iv => version.GameVersions.Contains(iv.GameVersion))
                .ToList();

            // 如果候选有多个，再按加载器过滤
            if (candidates.Count > 1 && modLoaders.Count > 0)
            {
                candidates = candidates
                    .Where(iv => modLoaders.Any(ml => iv.LoaderType.Equals(ml, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // 如果匹配到唯一一个，直接返回
            if (candidates.Count == 1)
            {
                Console.WriteLine($"[Resolve] 匹配到版本: {candidates[0].FolderName}");
                return candidates[0].FolderName;
            }
            else if (candidates.Count > 1)
            {
                // 多个匹配，让用户选择
                return ShowVersionSelectionDialog(candidates);
            }

            // 没有匹配到：列出所有已安装版本让用户选择
            var result = ShowVersionSelectionDialog(installedVersions);
            if (result == null)
            {
                MessageBox.Show("没有安装任何兼容版本，请在主窗体中切换或安装对应游戏版本。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return result;
        }

        /// <summary>
        /// 显示版本选择对话框
        /// </summary>
        private string ShowVersionSelectionDialog(List<InstalledVersion> versions)
        {
            if (versions.Count == 0) return null;

            using (var form = new Form())
            {
                form.Text = "选择安装目标版本";
                form.Width = 450;
                form.Height = 350;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var listBox = new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 250,
                    DisplayMember = "Display",
                    ValueMember = "FolderName"
                };

                // 构建显示项列表
                var items = versions.Select(v => new ItemDisplay
                {
                    Display = $"{v.GameVersion} ({v.LoaderType}) - {v.FolderName}",
                    FolderName = v.FolderName
                }).ToList();

                listBox.DataSource = items;

                var btnOk = new Button
                {
                    Text = "确定",
                    DialogResult = DialogResult.OK,
                    Top = 270,
                    Left = 150,
                    Width = 75
                };
                var btnCancel = new Button
                {
                    Text = "取消",
                    DialogResult = DialogResult.Cancel,
                    Top = 270,
                    Left = 230,
                    Width = 75
                };
                form.Controls.Add(listBox);
                form.Controls.Add(btnOk);
                form.Controls.Add(btnCancel);

                if (form.ShowDialog() == DialogResult.OK && listBox.SelectedItem != null)
                {
                    var selected = listBox.SelectedItem as ItemDisplay;
                    return selected?.FolderName;
                }
                return null;
            }
        }

        private void CopyDirectory(string sourceDir, string targetDir, bool overwrite = true)
        {
            if (!Directory.Exists(sourceDir)) return;
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, dest, overwrite);
            }
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(dir));
                CopyDirectory(dir, dest, overwrite);
            }
        }

        private void DownloadFileAsync(string url, string savePath, string versionNumber)
        {
            DownloadFileAsync(url, savePath, versionNumber, null);
        }

        private string GetMirrorUrl(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl)) return originalUrl;
            if (originalUrl.Contains("cdn.modrinth.com"))
            {
                try
                {
                    var uri = new Uri(originalUrl);
                    string pathAndQuery = uri.PathAndQuery;
                    return "https://bmclapi2.bangbang93.com" + pathAndQuery;
                }
                catch { return originalUrl; }
            }
            return originalUrl;
        }

        /// <summary>
        /// 校验文件是否为有效的 JAR/ZIP 格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="expectedSha1">期望的 SHA1（可选）</param>
        /// <returns>有效返回 true，否则 false</returns>
        private bool IsValidJarFile(string filePath, string expectedSha1 = null)
        {
            try
            {
                // 1. 检查文件是否存在
                if (!File.Exists(filePath))
                    return false;

                var info = new FileInfo(filePath);

                // 2. 如果提供了 SHA1，进行校验
                if (!string.IsNullOrEmpty(expectedSha1))
                {
                    using (var fs = File.OpenRead(filePath))
                    using (var sha1 = System.Security.Cryptography.SHA1.Create())
                    {
                        byte[] hash = sha1.ComputeHash(fs);
                        string actualSha1 = BitConverter.ToString(hash).Replace("-", "").ToLower();
                        if (!actualSha1.Equals(expectedSha1, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[校验] SHA1 不匹配: {Path.GetFileName(filePath)}");
                            return false;
                        }
                    }
                }

                // 3. 尝试读取 ZIP 结构（验证是否为有效的 JAR）
                using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(filePath))
                {
                    // 能正常打开说明是有效的 ZIP 格式
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[校验] 文件损坏: {Path.GetFileName(filePath)} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理指定目录中所有损坏的 JAR 文件
        /// </summary>
        /// <param name="directory">要清理的目录</param>
        /// <param name="expectedSha1Map">可选：文件名到 SHA1 的映射，用于精确校验</param>
        private void CleanCorruptedFiles(string directory, Dictionary<string, string> expectedSha1Map = null)
        {
            if (!Directory.Exists(directory)) return;

            foreach (var file in Directory.GetFiles(directory, "*.jar"))
            {
                string fileName = Path.GetFileName(file);
                string expectedSha1 = null;

                // 如果有 SHA1 映射，尝试获取
                if (expectedSha1Map != null && expectedSha1Map.ContainsKey(fileName))
                    expectedSha1 = expectedSha1Map[fileName];

                if (!IsValidJarFile(file, expectedSha1))
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine($"[清理] 删除损坏文件: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[清理] 删除失败: {fileName} - {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 带重试、校验和镜像切换的文件下载
        /// </summary>
        /// <param name="url">原始下载 URL</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="expectedSha1">期望的 SHA1（可选）</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <returns>下载成功返回 true，否则 false</returns>
        private bool DownloadFileWithRetry(string url, string savePath, string expectedSha1 = null, int maxRetries = 3)
        {
            // 确保目录存在
            string dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 如果文件已存在且校验通过，直接返回成功
            if (File.Exists(savePath) && IsValidJarFile(savePath, expectedSha1))
                return true;

            // 如果文件已存在但校验失败，删除它
            if (File.Exists(savePath))
            {
                try { File.Delete(savePath); }
                catch { }
            }

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // 第一次尝试使用镜像，后续尝试使用原始 URL
                    string downloadUrl = (attempt == 0) ? GetMirrorUrl(url) : url;
                    if (attempt > 0)
                        Console.WriteLine($"[下载] 重试 {attempt + 1}/{maxRetries}: {Path.GetFileName(savePath)} (使用原始源)");

                    using (var client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "JerryStudioLauncher/1.0");
                        client.DownloadFile(downloadUrl, savePath);
                    }

                    // 下载完成后校验
                    if (IsValidJarFile(savePath, expectedSha1))
                    {
                        Console.WriteLine($"[下载] 成功: {Path.GetFileName(savePath)}");
                        return true;
                    }

                    // 校验失败，删除损坏文件
                    try { File.Delete(savePath); }
                    catch { }

                    Console.WriteLine($"[下载] 校验失败，准备重试: {Path.GetFileName(savePath)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[下载] 失败 (尝试 {attempt + 1}/{maxRetries}): {Path.GetFileName(savePath)} - {ex.Message}");
                    try { if (File.Exists(savePath)) File.Delete(savePath); }
                    catch { }
                }

                // 重试前等待（指数退避）
                if (attempt < maxRetries - 1)
                    System.Threading.Thread.Sleep(2000 * (attempt + 1));
            }

            Console.WriteLine($"[下载] 所有重试均失败: {Path.GetFileName(savePath)}");
            return false;
        }

        /// <summary>
        /// 获取指定加载器类型的最新稳定版本
        /// </summary>
        private string GetLatestLoaderVersion(string loaderType, string mcVersion)
        {
            try
            {
                string[] versions = null;
                switch (loaderType.ToLowerInvariant())
                {
                    case "forge":
                        versions = ForgeManager.Installer.GetForgeLoaderVersions(mcVersion);
                        break;
                    case "fabric":
                        versions = FabricManager.Installer.GetFabricLoaderVersions(mcVersion);
                        break;
                    case "quilt":
                        versions = QuiltManager.Installer.GetQuiltLoaderVersions(mcVersion);
                        if (versions != null)
                        {
                            // 过滤掉 beta 版本（包含 -beta）和占位项 "不选择"
                            versions = versions
                                .Where(v => v != "不选择" && !v.Contains("-beta"))
                                .ToArray();
                        }
                        break;
                    case "neoforge":
                        versions = NeoForgeManager.Installer.GetNeoForgeLoaderVersions(mcVersion);
                        break;
                    default:
                        return null;
                }

                if (versions == null || versions.Length == 0)
                    return null;

                // 取第一个（假设列表已经是降序排列）
                string latest = versions[0];
                return latest;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[获取版本] 获取 {loaderType} 最新版本失败: {ex.Message}");
                return null;
            }
        }

        // 辅助类
        private class InstalledVersion
        {
            public string FolderName { get; set; }
            public string GameVersion { get; set; }
            public string LoaderType { get; set; }
            public string Id { get; set; }
        }

        private class ItemDisplay
        {
            public string Display { get; set; }
            public string FolderName { get; set; }
        }

        private class DownloadTask
        {
            public string Url { get; set; }
            public string SavePath { get; set; }
            public string FileName { get; set; }
        }

        private class DownloadTaskWithSha
        {
            public string Url { get; set; }
            public string SavePath { get; set; }
            public string FileName { get; set; }
            public string ExpectedSha1 { get; set; }
        }
    }
}