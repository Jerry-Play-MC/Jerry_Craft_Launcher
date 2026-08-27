using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace New_Launcher
{
    public partial class ModManagerForm : Form
    {
        private string _currentVersion;      // 主窗体选中的版本 ID（如 fabric-loader-0.15.0-1.20.4）
        private bool _isVersionIsolated;
        private int currentPage = 1;
        private const int pageSize = 20;
        private int totalHits = 0;
        private string currentKeyword = "";
        private ProjectType currentType = ProjectType.Mod;
        private readonly BackgroundWorker loadWorker;
        private bool _closeByBackButton = false;

        public ModManagerForm(string currentVersion, bool isIsolated)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;

            _currentVersion = currentVersion;
            _isVersionIsolated = isIsolated;

            comboBox_Type.SelectedIndex = 0;

            this.btnSearch.Click += BtnSearch_Click;
            this.btnPrevPage.Click += BtnPrevPage_Click;
            this.btnNextPage.Click += BtnNextPage_Click;
            this.FormClosing += ModManagerForm_FormClosing;
            this.comboBox_Type.SelectedIndexChanged += ComboBox_Type_SelectedIndexChanged;

            loadWorker = new BackgroundWorker();
            loadWorker.WorkerSupportsCancellation = true;   // <-- 关键修复
            loadWorker.DoWork += LoadWorker_DoWork;
            loadWorker.RunWorkerCompleted += LoadWorker_RunWorkerCompleted;

            currentKeyword = "";
            LoadMods();
        }

        private void ComboBox_Type_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 切换类型时重置页码并搜索
            currentPage = 1;
            LoadMods();
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            currentKeyword = txtSearch.Text.Trim();
            currentPage = 1;
            LoadMods();
        }

        private void BtnPrevPage_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadMods();
            }
        }

        private void BtnNextPage_Click(object sender, EventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)totalHits / pageSize);
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadMods();
            }
        }

        private void LoadMods()
        {
            // 获取当前选中的类型
            currentType = (ProjectType)comboBox_Type.SelectedIndex;

            btnSearch.Enabled = false;
            btnPrevPage.Enabled = false;
            btnNextPage.Enabled = false;
            flpMods.Controls.Clear();
            flpMods.Controls.Add(new Label { Text = "加载中...", AutoSize = true });

            if (!loadWorker.IsBusy)
                loadWorker.RunWorkerAsync(new object[] { currentKeyword, currentPage, pageSize, currentType });
        }

        private void LoadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as object[];
            string keyword = (string)args[0];
            int page = (int)args[1];
            int limit = (int)args[2];
            ProjectType type = (ProjectType)args[3];

            // 此处 mcVersion 应从设置中读取，暂硬编码
            string mcVersion = "";

            int total;
            var mods = ModApiService.SearchModrinth(keyword, mcVersion, page, limit, type, out total);
            e.Result = new object[] { mods, total, type };
        }

        private void LoadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnSearch.Enabled = true;
            if (e.Error != null)
            {
                flpMods.Controls.Clear();
                string fullError = GetFullExceptionMessage(e.Error);
                flpMods.Controls.Add(new Label
                {
                    Text = $"加载失败：{fullError}",
                    ForeColor = System.Drawing.Color.Red,
                    AutoSize = true,
                    MaximumSize = new System.Drawing.Size(flpMods.Width - 20, 0)
                });
                UpdatePagination(0);
                return;
            }

            var result = e.Result as object[];
            var mods = result[0] as List<ModrinthMod>;
            totalHits = (int)result[1];
            ProjectType type = (ProjectType)result[2];

            flpMods.Controls.Clear();
            if (mods == null || mods.Count == 0)
            {
                flpMods.Controls.Add(new Label { Text = "未找到任何项目" });
            }
            else
            {
                foreach (var mod in mods)
                {
                    var card = new ModCardControl();
                    card.SetData(mod);
                    card.ModClicked += (s, args) =>
                    {
                        // 将当前类型、版本信息和隔离状态传递给版本窗体
                        var versionsForm = new ModVersionsForm(args.ModId, type, _currentVersion, _isVersionIsolated);
                        versionsForm.ShowDialog(this);
                    };
                    flpMods.Controls.Add(card);
                }
            }
            UpdatePagination(totalHits);
        }

        private void UpdatePagination(int total)
        {
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            if (totalPages == 0) totalPages = 1;
            lblPageInfo.Text = $"第 {currentPage} / {totalPages} 页";

            btnPrevPage.Enabled = currentPage > 1;
            btnNextPage.Enabled = currentPage < totalPages;
        }

        private void ModManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Control ctrl in flpMods.Controls)
            {
                if (ctrl is ModCardControl card)
                    card.CancelAsyncLoad();
            }
            if (loadWorker.IsBusy)
                loadWorker.CancelAsync();

            if (!_closeByBackButton)
            {
                Application.Exit();
            }
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            if (ex == null) return "未知错误";
            string result = ex.Message;
            if (ex.InnerException != null)
            {
                result += "\n内部错误：" + GetFullExceptionMessage(ex.InnerException);
            }
            return result;
        }

        private void ModManagerForm_Load(object sender, EventArgs e) { }

        private void button_Back_Click(object sender, EventArgs e)
        {
            _closeByBackButton = true;
            this.Close();
        }
    }
}