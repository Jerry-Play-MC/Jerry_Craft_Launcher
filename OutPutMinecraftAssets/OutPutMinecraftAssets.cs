using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace New_Launcher.OutPutMinecraftAssets
{
    public partial class OutPutMinecraftAssets : Form
    {
        private bool _closeByBackButton = false;
        private string currentPrefix = "minecraft/sounds/";
        private DataTable resultTable;

        public OutPutMinecraftAssets()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            InitializeDataGrid();
            // 设置下拉框默认选中“音效”
            cmbType.SelectedIndex = 0;
        }

        private void InitializeDataGrid()
        {
            resultTable = new DataTable();
            resultTable.Columns.Add("资源名称", typeof(string));
            resultTable.Columns.Add("哈希值", typeof(string));
            resultTable.Columns.Add("文件路径", typeof(string));
            resultTable.Columns.Add("文件大小(KB)", typeof(string));
            resultTable.Columns.Add("是否存在", typeof(string));
            dgvResults.DataSource = resultTable;

            // 隐藏不需要显示的列（仅保留“资源名称”）
            dgvResults.Columns["哈希值"].Visible = false;
            dgvResults.Columns["文件路径"].Visible = false;
            dgvResults.Columns["文件大小(KB)"].Visible = false;
            dgvResults.Columns["是否存在"].Visible = false;

            // 可选：设置“资源名称”列自动填充宽度
            dgvResults.Columns["资源名称"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            // 确定类型
            switch (cmbType.SelectedIndex)
            {
                case 0: currentPrefix = "minecraft/sounds/"; break;
                case 1: currentPrefix = "minecraft/textures/"; break;
                case 2: currentPrefix = ""; break;
                default: currentPrefix = "minecraft/sounds/"; break;
            }

            string keyword = txtSearch.Text.Trim();
            if (keyword == "输入关键词过滤（如 menu）") keyword = "";
            keyword = keyword.Replace(' ', '_');

            resultTable.Rows.Clear();

            string launcherDir = Application.StartupPath;
            string indexesDir = Path.Combine(Path.Combine(Path.Combine(launcherDir, ".minecraft"), "assets"), "indexes");
            if (!Directory.Exists(indexesDir))
            {
                MessageBox.Show($"未找到索引目录：{indexesDir}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var jsonFiles = Directory.GetFiles(indexesDir, "*.json");
            if (jsonFiles.Length == 0)
            {
                MessageBox.Show("索引目录中没有 .json 文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<DataRow> recordsMatches = new List<DataRow>();
            List<DataRow> musicMatches = new List<DataRow>();
            List<DataRow> otherMatches = new List<DataRow>();

            resultTable.BeginLoadData();

            foreach (var jsonPath in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    JObject root = JObject.Parse(jsonContent);
                    JObject objects = root["objects"] as JObject;
                    if (objects == null) continue;

                    foreach (var prop in objects.Properties())
                    {
                        string assetKey = prop.Name;

                        // 仅处理音效类型
                        if (cmbType.SelectedIndex == 0 && !assetKey.StartsWith("minecraft/sounds/"))
                            continue;
                        if (cmbType.SelectedIndex == 1 && !assetKey.StartsWith("minecraft/textures/"))
                            continue;
                        // 全部则不过滤前缀

                        // 如果是音效类型，强制 .ogg 后缀
                        if (cmbType.SelectedIndex == 0 && !assetKey.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // ====== 关键修改：关键词必须以 /关键词.ogg 结尾 ======
                        if (!string.IsNullOrEmpty(keyword))
                        {
                            string expectedEnding = "/" + keyword + ".ogg";
                            if (!assetKey.EndsWith(expectedEnding, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        // 提取元数据
                        JObject meta = prop.Value as JObject;
                        if (meta == null) continue;
                        JToken hashToken = meta["hash"];
                        if (hashToken == null) continue;
                        string hash = hashToken.ToString();
                        long size = meta["size"] != null ? Convert.ToInt64(meta["size"]) : 0;
                        string hashPrefix = hash.Substring(0, 2);
                        string objectPath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(launcherDir, ".minecraft"), "assets"), "objects"), hashPrefix), hash);
                        bool exists = File.Exists(objectPath);
                        string sizeKB = size > 0 ? (size / 1024.0).ToString("F2") : "未知";

                        DataRow row = resultTable.NewRow();
                        row["资源名称"] = assetKey;
                        row["哈希值"] = hash;
                        row["文件路径"] = objectPath;
                        row["文件大小(KB)"] = sizeKB;
                        row["是否存在"] = exists ? "是" : "否";

                        // 按优先级分类
                        if (cmbType.SelectedIndex == 0)
                        {
                            if (assetKey.StartsWith("minecraft/sounds/records/", StringComparison.OrdinalIgnoreCase))
                                recordsMatches.Add(row);
                            else if (assetKey.StartsWith("minecraft/sounds/music/game/", StringComparison.OrdinalIgnoreCase))
                                musicMatches.Add(row);
                            else
                                otherMatches.Add(row);
                        }
                        else
                        {
                            otherMatches.Add(row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"解析索引文件失败：{jsonPath}\n错误：{ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // 按优先级决定最终显示列表
            List<DataRow> finalRows;
            if (recordsMatches.Count > 0)
            {
                finalRows = recordsMatches;
                lblStatus.Text = $"在 records 目录中找到 {recordsMatches.Count} 个匹配资源";
            }
            else if (musicMatches.Count > 0)
            {
                finalRows = musicMatches;
                lblStatus.Text = $"在 music/game 目录中找到 {musicMatches.Count} 个匹配资源";
            }
            else
            {
                finalRows = otherMatches;
                lblStatus.Text = $"找到 {otherMatches.Count} 个匹配资源";
            }

            foreach (var row in finalRows)
                resultTable.Rows.Add(row);

            resultTable.EndLoadData();

            if (finalRows.Count == 0)
                lblStatus.Text = "未找到匹配资源";
        }

        private void txtSearch_Enter(object sender, EventArgs e)
        {
            if (txtSearch.Text == "输入关键词过滤（如 menu）")
                txtSearch.Text = "";
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearch.Text.Trim()))
                txtSearch.Text = "输入关键词过滤（如 menu）";
        }

        private void dgvResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string filePath = dgvResults.Rows[e.RowIndex].Cells["文件路径"].Value.ToString();
                if (File.Exists(filePath))
                {
                    MessageBox.Show($"文件存在：{filePath}", "提示");
                }
                else
                {
                    MessageBox.Show("文件不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // ---------- 导出 MP3 功能 ----------
        private void btnExport_Click(object sender, EventArgs e)
        {
            if (dgvResults.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先在表格中选择要导出的音效（可按住 Ctrl 或 Shift 多选）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "选择保存 MP3 的文件夹";
                folderDialog.ShowNewFolderButton = true;
                if (folderDialog.ShowDialog() != DialogResult.OK)
                    return;

                string saveFolder = folderDialog.SelectedPath;
                int successCount = 0;
                int failCount = 0;
                string errorDetails = "";

                foreach (DataGridViewRow row in dgvResults.SelectedRows)
                {
                    string filePath = row.Cells["文件路径"].Value.ToString();
                    if (!File.Exists(filePath))
                    {
                        failCount++;
                        errorDetails += $"文件不存在：{filePath}\n";
                        continue;
                    }

                    // 从资源名称提取文件名并首字母大写
                    string assetKey = row.Cells["资源名称"].Value.ToString();
                    string fileName = Path.GetFileNameWithoutExtension(assetKey); // 如 "sweden"
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        fileName = char.ToUpper(fileName[0]) + fileName.Substring(1); // "Sweden"
                    }
                    fileName += ".mp3";
                    string outputPath = Path.Combine(saveFolder, fileName);

                    try
                    {
                        OGGToMP3.ConvertOggToMp3(filePath, outputPath, 192);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errorDetails += $"转换失败 [{Path.GetFileName(filePath)}]：{ex.Message}\n";
                        System.Diagnostics.Debug.WriteLine($"转换失败: {filePath} -> {ex.Message}");
                    }
                }

                string msg = $"导出完成！\n成功：{successCount} 个\n失败：{failCount} 个";
                if (failCount > 0)
                {
                    msg += $"\n\n错误详情：\n{errorDetails}";
                }
                MessageBox.Show(msg, "结果", MessageBoxButtons.OK, failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                lblStatus.Text = $"导出完成：成功 {successCount}，失败 {failCount}";
            }
        }

        // ---------- 窗体事件 ----------
        private void OutPutMinecraftAssets_Load(object sender, EventArgs e)
        {
            // 可以留空
        }

        private void button_Back_Click(object sender, EventArgs e)
        {
            _closeByBackButton = true;
            this.Close();
        }

        private void OutPutMinecraftAssets_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closeByBackButton)
            {
                Application.Exit();
            }
        }
    }
}