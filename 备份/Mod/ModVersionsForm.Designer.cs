namespace New_Launcher
{
    partial class ModVersionsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModVersionsForm));
            this.listVersions = new System.Windows.Forms.ListView();
            this.colVersion = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colGameVersions = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colLoaders = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDownloads = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnDownload = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelTop = new System.Windows.Forms.Panel();
            this.panelFilter = new System.Windows.Forms.Panel();
            this.lblSearch = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSort = new System.Windows.Forms.Label();
            this.cmbSort = new System.Windows.Forms.ComboBox();
            this.lblSortDir = new System.Windows.Forms.Label();
            this.cmbSortDirection = new System.Windows.Forms.ComboBox();
            this.lblGameVersion = new System.Windows.Forms.Label();
            this.cmbGameVersion = new System.Windows.Forms.ComboBox();
            this.lblLoader = new System.Windows.Forms.Label();
            this.cmbLoader = new System.Windows.Forms.ComboBox();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.panelList = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.panelTop.SuspendLayout();
            this.panelFilter.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.panelList.SuspendLayout();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // listVersions
            // 
            this.listVersions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colVersion,
            this.colGameVersions,
            this.colLoaders,
            this.colDate,
            this.colDownloads});
            this.listVersions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listVersions.FullRowSelect = true;
            this.listVersions.GridLines = true;
            this.listVersions.HideSelection = false;
            this.listVersions.Location = new System.Drawing.Point(0, 0);
            this.listVersions.MultiSelect = false;
            this.listVersions.Name = "listVersions";
            this.listVersions.Size = new System.Drawing.Size(800, 336);
            this.listVersions.TabIndex = 0;
            this.listVersions.UseCompatibleStateImageBehavior = false;
            this.listVersions.View = System.Windows.Forms.View.Details;
            // 
            // colVersion
            // 
            this.colVersion.Text = "版本号";
            this.colVersion.Width = 180;
            // 
            // colGameVersions
            // 
            this.colGameVersions.Text = "支持的游戏版本";
            this.colGameVersions.Width = 180;
            // 
            // colLoaders
            // 
            this.colLoaders.Text = "加载器";
            this.colLoaders.Width = 120;
            // 
            // colDate
            // 
            this.colDate.Text = "发布日期";
            this.colDate.Width = 140;
            // 
            // colDownloads
            // 
            this.colDownloads.Text = "下载次数";
            this.colDownloads.Width = 100;
            // 
            // btnDownload
            // 
            this.btnDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDownload.Location = new System.Drawing.Point(583, 3);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(124, 30);
            this.btnDownload.TabIndex = 1;
            this.btnDownload.Text = "📥 下载选中版本";
            this.btnDownload.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(713, 3);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 30);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(3, 6);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(92, 27);
            this.lblTitle.TabIndex = 3;
            this.lblTitle.Text = "版本列表";
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Controls.Add(this.btnClose);
            this.panelTop.Controls.Add(this.btnDownload);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Font = new System.Drawing.Font("宋体", 9F);
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(800, 39);
            this.panelTop.TabIndex = 4;
            // 
            // panelFilter
            // 
            this.panelFilter.Controls.Add(this.lblSearch);
            this.panelFilter.Controls.Add(this.txtSearch);
            this.panelFilter.Controls.Add(this.lblSort);
            this.panelFilter.Controls.Add(this.cmbSort);
            this.panelFilter.Controls.Add(this.lblSortDir);
            this.panelFilter.Controls.Add(this.cmbSortDirection);
            this.panelFilter.Controls.Add(this.lblGameVersion);
            this.panelFilter.Controls.Add(this.cmbGameVersion);
            this.panelFilter.Controls.Add(this.lblLoader);
            this.panelFilter.Controls.Add(this.cmbLoader);
            this.panelFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilter.Location = new System.Drawing.Point(0, 0);
            this.panelFilter.Name = "panelFilter";
            this.panelFilter.Size = new System.Drawing.Size(800, 75);
            this.panelFilter.TabIndex = 0;
            this.panelFilter.Paint += new System.Windows.Forms.PaintEventHandler(this.panelFilter_Paint);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(8, 10);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(45, 15);
            this.lblSearch.TabIndex = 0;
            this.lblSearch.Text = "搜索:";
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(58, 7);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(180, 25);
            this.txtSearch.TabIndex = 1;
            // 
            // lblSort
            // 
            this.lblSort.AutoSize = true;
            this.lblSort.Location = new System.Drawing.Point(255, 10);
            this.lblSort.Name = "lblSort";
            this.lblSort.Size = new System.Drawing.Size(45, 15);
            this.lblSort.TabIndex = 2;
            this.lblSort.Text = "排序:";
            // 
            // cmbSort
            // 
            this.cmbSort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSort.Items.AddRange(new object[] {
            "发布日期",
            "下载次数",
            "版本号"});
            this.cmbSort.Location = new System.Drawing.Point(305, 7);
            this.cmbSort.Name = "cmbSort";
            this.cmbSort.Size = new System.Drawing.Size(100, 23);
            this.cmbSort.TabIndex = 3;
            // 
            // lblSortDir
            // 
            this.lblSortDir.AutoSize = true;
            this.lblSortDir.Location = new System.Drawing.Point(418, 10);
            this.lblSortDir.Name = "lblSortDir";
            this.lblSortDir.Size = new System.Drawing.Size(45, 15);
            this.lblSortDir.TabIndex = 4;
            this.lblSortDir.Text = "方向:";
            // 
            // cmbSortDirection
            // 
            this.cmbSortDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSortDirection.Items.AddRange(new object[] {
            "降序(最新)",
            "升序(最早)"});
            this.cmbSortDirection.Location = new System.Drawing.Point(468, 7);
            this.cmbSortDirection.Name = "cmbSortDirection";
            this.cmbSortDirection.Size = new System.Drawing.Size(100, 23);
            this.cmbSortDirection.TabIndex = 5;
            // 
            // lblGameVersion
            // 
            this.lblGameVersion.AutoSize = true;
            this.lblGameVersion.Location = new System.Drawing.Point(8, 45);
            this.lblGameVersion.Name = "lblGameVersion";
            this.lblGameVersion.Size = new System.Drawing.Size(75, 15);
            this.lblGameVersion.TabIndex = 6;
            this.lblGameVersion.Text = "游戏版本:";
            // 
            // cmbGameVersion
            // 
            this.cmbGameVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGameVersion.Items.AddRange(new object[] {
            "全部"});
            this.cmbGameVersion.Location = new System.Drawing.Point(94, 42);
            this.cmbGameVersion.Name = "cmbGameVersion";
            this.cmbGameVersion.Size = new System.Drawing.Size(140, 23);
            this.cmbGameVersion.TabIndex = 7;
            // 
            // lblLoader
            // 
            this.lblLoader.AutoSize = true;
            this.lblLoader.Location = new System.Drawing.Point(255, 45);
            this.lblLoader.Name = "lblLoader";
            this.lblLoader.Size = new System.Drawing.Size(84, 15);
            this.lblLoader.TabIndex = 8;
            this.lblLoader.Text = "Mod加载器:";
            // 
            // cmbLoader
            // 
            this.cmbLoader.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLoader.Items.AddRange(new object[] {
            "全部"});
            this.cmbLoader.Location = new System.Drawing.Point(341, 42);
            this.cmbLoader.Name = "cmbLoader";
            this.cmbLoader.Size = new System.Drawing.Size(140, 23);
            this.cmbLoader.TabIndex = 9;
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.panelList);
            this.panelBottom.Controls.Add(this.panelFilter);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBottom.Location = new System.Drawing.Point(0, 39);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(800, 411);
            this.panelBottom.TabIndex = 5;
            // 
            // panelList
            // 
            this.panelList.Controls.Add(this.listVersions);
            this.panelList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelList.Location = new System.Drawing.Point(0, 75);
            this.panelList.Name = "panelList";
            this.panelList.Size = new System.Drawing.Size(800, 336);
            this.panelList.TabIndex = 1;
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Location = new System.Drawing.Point(0, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(800, 25);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "共 0 个版本";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelStatus
            // 
            this.panelStatus.Controls.Add(this.lblStatus);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatus.Location = new System.Drawing.Point(0, 425);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Size = new System.Drawing.Size(800, 25);
            this.panelStatus.TabIndex = 6;
            // 
            // ModVersionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "ModVersionsForm";
            this.Text = "Mod 版本列表";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ModVersionsForm_FormClosing);
            this.Load += new System.EventHandler(this.ModVersionsForm_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelFilter.ResumeLayout(false);
            this.panelFilter.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelList.ResumeLayout(false);
            this.panelStatus.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.ListView listVersions;
        private System.Windows.Forms.ColumnHeader colVersion;
        private System.Windows.Forms.ColumnHeader colGameVersions;
        private System.Windows.Forms.ColumnHeader colLoaders;
        private System.Windows.Forms.ColumnHeader colDate;
        private System.Windows.Forms.ColumnHeader colDownloads;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Panel panelFilter;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.ComboBox cmbSort;
        private System.Windows.Forms.Label lblSort;
        private System.Windows.Forms.ComboBox cmbSortDirection;
        private System.Windows.Forms.Label lblSortDir;
        private System.Windows.Forms.Label lblGameVersion;
        private System.Windows.Forms.ComboBox cmbGameVersion;
        private System.Windows.Forms.Label lblLoader;
        private System.Windows.Forms.ComboBox cmbLoader;
        private System.Windows.Forms.Panel panelList;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panelStatus;
    }
}