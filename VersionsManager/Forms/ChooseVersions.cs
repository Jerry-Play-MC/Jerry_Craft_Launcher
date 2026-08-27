using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace New_Launcher
{
    public partial class ChooseVersions : Form
    {
        private static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string ManifestPath = Path.Combine(Path.Combine(LauncherPath, "Launcher Setting"), "version_manifest.json");
        private string _selectedVersionId = null;  // 当前选中的版本ID（通过下拉框选择）

        private List<string> _releaseVersions = new List<string>();
        private List<string> _snapshotVersions = new List<string>();
        private List<string> _oldAlphaVersions = new List<string>();
        private List<string> _aprilFoolVersions = new List<string>();

        private string _latestRelease;
        private string _latestSnapshot;

        // 互斥状态
        private string _selectedType = null;
        private readonly object _mutexLock = new object();
        private bool _isLoading = false;
        private bool _isLoaded = false;

        public ChooseVersions()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            comboBox_releaseVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_snapshotVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_old_alphaVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_AprilFool_sDayVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;

            SetLoadingState();
        }

        /// <summary>
        /// 非4月1日发布但社区公认的愚人节版本ID列表（覆盖特殊版本）
        /// </summary>
        private static readonly HashSet<string> _knownAprilFoolIds = new HashSet<string>
        {
            "1.RV-Pre1"
        };

        private void SetLoadingState()
        {
            comboBox_releaseVersion.Items.Clear();
            comboBox_releaseVersion.Items.Add("加载中...");
            comboBox_releaseVersion.SelectedIndex = 0;
            comboBox_releaseVersion.Enabled = false;

            comboBox_snapshotVersion.Items.Clear();
            comboBox_snapshotVersion.Items.Add("加载中...");
            comboBox_snapshotVersion.SelectedIndex = 0;
            comboBox_snapshotVersion.Enabled = false;

            comboBox_old_alphaVersion.Items.Clear();
            comboBox_old_alphaVersion.Items.Add("加载中...");
            comboBox_old_alphaVersion.SelectedIndex = 0;
            comboBox_old_alphaVersion.Enabled = false;

            comboBox_AprilFool_sDayVersion.Items.Clear();
            comboBox_AprilFool_sDayVersion.Items.Add("加载中...");
            comboBox_AprilFool_sDayVersion.SelectedIndex = 0;
            comboBox_AprilFool_sDayVersion.Enabled = false;

            button_latestreleaseVersion.Enabled = false;
            button_latestsnapshotVersion.Enabled = false;
        }

        private void ChooseVersions_Load(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(LoadVersionManifest);
        }

        private void LoadVersionManifest(object state)
        {
            try
            {
                if (!File.Exists(ManifestPath))
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("版本清单文件不存在，请确保启动器已更新。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SetLoadingState();
                    });
                    return;
                }

                string json = File.ReadAllText(ManifestPath);
                JObject manifest;
                using (var reader = new JsonTextReader(new StringReader(json)))
                {
                    reader.DateParseHandling = DateParseHandling.None;
                    manifest = JObject.Load(reader);
                }
                JArray versions = (JArray)manifest["versions"];
                JObject latest = (JObject)manifest["latest"];

                _latestRelease = latest["release"]?.ToString();
                _latestSnapshot = latest["snapshot"]?.ToString();

                _releaseVersions.Clear();
                _snapshotVersions.Clear();
                _oldAlphaVersions.Clear();
                _aprilFoolVersions.Clear();

                foreach (JObject version in versions)
                {
                    string id = version["id"].ToString();
                    string type = version["type"].ToString();
                    string releaseTime = version["releaseTime"].Value<string>();

                    bool isAprilFool = false;

                    // 1. 先检查内置列表（覆盖特殊版本）
                    if (_knownAprilFoolIds.Contains(id) && type == "snapshot")
                    {
                        isAprilFool = true;
                    }
                    // 2. 否则按常规规则：仅限快照且 releaseTime 为 4 月 1 日，防止26.1.1正式版被误判为愚人节版
                    else if (type == "snapshot" && !string.IsNullOrEmpty(releaseTime) && releaseTime.Length >= 10)
                    {
                        string datePart = releaseTime.Substring(4, 6);
                        isAprilFool = (datePart == "-04-01");
                    }

                    if (isAprilFool)
                        _aprilFoolVersions.Add(id);
                    else
                    {
                        switch (type)
                        {
                            case "release": _releaseVersions.Add(id); break;
                            case "snapshot": _snapshotVersions.Add(id); break;
                            case "old_alpha": _oldAlphaVersions.Add(id); break;
                        }
                    }
                }

                this.BeginInvoke((MethodInvoker)delegate
                {
                    _isLoading = true;
                    BindComboBox(comboBox_releaseVersion, _releaseVersions);
                    BindComboBox(comboBox_snapshotVersion, _snapshotVersions);
                    BindComboBox(comboBox_old_alphaVersion, _oldAlphaVersions);
                    BindComboBox(comboBox_AprilFool_sDayVersion, _aprilFoolVersions);

                    comboBox_releaseVersion.SelectedIndex = 0;
                    comboBox_snapshotVersion.SelectedIndex = 0;
                    comboBox_old_alphaVersion.SelectedIndex = 0;
                    comboBox_AprilFool_sDayVersion.SelectedIndex = 0;

                    comboBox_releaseVersion.Enabled = true;
                    comboBox_snapshotVersion.Enabled = true;
                    comboBox_old_alphaVersion.Enabled = true;
                    comboBox_AprilFool_sDayVersion.Enabled = true;

                    button_latestreleaseVersion.Text = string.IsNullOrEmpty(_latestRelease)
                        ? "最新正式版"
                        : $"最新正式版：{_latestRelease}";
                    button_latestsnapshotVersion.Text = string.IsNullOrEmpty(_latestSnapshot)
                        ? "最新快照版"
                        : $"最新快照版：{_latestSnapshot}";
                    button_latestreleaseVersion.Enabled = true;
                    button_latestsnapshotVersion.Enabled = true;

                    _isLoading = false;
                    _isLoaded = true;
                });
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    MessageBox.Show($"读取版本清单失败：\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetLoadingState();
                });
            }
        }

        private void BindComboBox(ComboBox cb, List<string> items)
        {
            cb.Items.Clear();
            cb.Items.Add("不选择");
            foreach (var item in items)
                cb.Items.Add(item);
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        // ---- 以下互斥和选择逻辑与原代码完全相同 ----
        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoading || !_isLoaded) return;

            ComboBox cb = sender as ComboBox;
            if (cb == null) return;
            string selected = cb.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            string type = null;
            if (cb == comboBox_releaseVersion) type = "release";
            else if (cb == comboBox_snapshotVersion) type = "snapshot";
            else if (cb == comboBox_old_alphaVersion) type = "old_alpha";
            else if (cb == comboBox_AprilFool_sDayVersion) type = "aprilfool";
            else return;

            if (selected == "不选择")
            {
                lock (_mutexLock) { _selectedType = null; }
                _selectedVersionId = null;
                RestoreAllComboBoxes();
            }
            else
            {
                lock (_mutexLock) { _selectedType = type; }
                _selectedVersionId = selected;
                DisableOtherComboBoxes(type);
            }
        }

        private void RestoreAllComboBoxes()
        {
            comboBox_releaseVersion.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
            comboBox_snapshotVersion.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
            comboBox_old_alphaVersion.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
            comboBox_AprilFool_sDayVersion.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;

            BindComboBox(comboBox_releaseVersion, _releaseVersions);
            BindComboBox(comboBox_snapshotVersion, _snapshotVersions);
            BindComboBox(comboBox_old_alphaVersion, _oldAlphaVersions);
            BindComboBox(comboBox_AprilFool_sDayVersion, _aprilFoolVersions);

            comboBox_releaseVersion.Enabled = true;
            comboBox_snapshotVersion.Enabled = true;
            comboBox_old_alphaVersion.Enabled = true;
            comboBox_AprilFool_sDayVersion.Enabled = true;

            comboBox_releaseVersion.SelectedIndex = 0;
            comboBox_snapshotVersion.SelectedIndex = 0;
            comboBox_old_alphaVersion.SelectedIndex = 0;
            comboBox_AprilFool_sDayVersion.SelectedIndex = 0;

            comboBox_releaseVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_snapshotVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_old_alphaVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_AprilFool_sDayVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
        }

        private void DisableOtherComboBoxes(string activeType)
        {
            string incompatibleText = $"与 {activeType} 不兼容";

            comboBox_releaseVersion.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
            comboBox_snapshotVersion.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
            comboBox_old_alphaVersion.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
            comboBox_AprilFool_sDayVersion.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;

            if (activeType != "release")
            {
                comboBox_releaseVersion.Items.Clear();
                comboBox_releaseVersion.Items.Add(incompatibleText);
                comboBox_releaseVersion.SelectedIndex = 0;
                comboBox_releaseVersion.Enabled = false;
            }
            if (activeType != "snapshot")
            {
                comboBox_snapshotVersion.Items.Clear();
                comboBox_snapshotVersion.Items.Add(incompatibleText);
                comboBox_snapshotVersion.SelectedIndex = 0;
                comboBox_snapshotVersion.Enabled = false;
            }
            if (activeType != "old_alpha")
            {
                comboBox_old_alphaVersion.Items.Clear();
                comboBox_old_alphaVersion.Items.Add(incompatibleText);
                comboBox_old_alphaVersion.SelectedIndex = 0;
                comboBox_old_alphaVersion.Enabled = false;
            }
            if (activeType != "aprilfool")
            {
                comboBox_AprilFool_sDayVersion.Items.Clear();
                comboBox_AprilFool_sDayVersion.Items.Add(incompatibleText);
                comboBox_AprilFool_sDayVersion.SelectedIndex = 0;
                comboBox_AprilFool_sDayVersion.Enabled = false;
            }

            comboBox_releaseVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_snapshotVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_old_alphaVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox_AprilFool_sDayVersion.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
        }

        private void SelectVersion(string versionId)
        {
            if (string.IsNullOrEmpty(versionId)) return;

            DownloadVersions downloadForm = new DownloadVersions();
            downloadForm.SetTargetVersion(versionId);
            this.Hide();
            downloadForm.ShowDialog();
            this.Close();
        }

        private void button_latestreleaseVersion_Click(object sender, EventArgs e)
        {
            if (!_isLoaded)
            {
                MessageBox.Show("版本列表正在加载中，请稍候...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!string.IsNullOrEmpty(_latestRelease))
                SelectVersion(_latestRelease);
            else
                MessageBox.Show("无法获取最新正式版", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void button_latestsnapshotVersion_Click(object sender, EventArgs e)
        {
            if (!_isLoaded)
            {
                MessageBox.Show("版本列表正在加载中，请稍候...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!string.IsNullOrEmpty(_latestSnapshot))
                SelectVersion(_latestSnapshot);
            else
                MessageBox.Show("无法获取最新快照版", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void button__NextStep_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedVersionId))
            {
                MessageBox.Show("请先选择一个版本。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SelectVersion(_selectedVersionId);
        }
    }
}