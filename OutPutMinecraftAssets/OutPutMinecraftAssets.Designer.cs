namespace New_Launcher.OutPutMinecraftAssets
{
    partial class OutPutMinecraftAssets
    {
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button button_Back;
        private System.Windows.Forms.Button btnExport;   // 新增导出按钮

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OutPutMinecraftAssets));
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnExport = new System.Windows.Forms.Button();
            this.button_Back = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnSearch = new System.Windows.Forms.Button();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.SystemColors.ControlLight;
            this.panelTop.Controls.Add(this.btnExport);
            this.panelTop.Controls.Add(this.button_Back);
            this.panelTop.Controls.Add(this.txtSearch);
            this.panelTop.Controls.Add(this.btnSearch);
            this.panelTop.Controls.Add(this.cmbType);
            this.panelTop.Controls.Add(this.lblStatus);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(5);
            this.panelTop.Size = new System.Drawing.Size(800, 50);
            this.panelTop.TabIndex = 1;
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(626, 8);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(80, 25);
            this.btnExport.TabIndex = 5;
            this.btnExport.Text = "导出";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // button_Back
            // 
            this.button_Back.Location = new System.Drawing.Point(712, 8);
            this.button_Back.Name = "button_Back";
            this.button_Back.Size = new System.Drawing.Size(80, 25);
            this.button_Back.TabIndex = 4;
            this.button_Back.Text = "返回";
            this.button_Back.UseVisualStyleBackColor = true;
            this.button_Back.Click += new System.EventHandler(this.button_Back_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(10, 12);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(300, 25);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.Text = "输入关键词过滤（如 menu）";
            this.txtSearch.Enter += new System.EventHandler(this.txtSearch_Enter);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(320, 10);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(80, 25);
            this.btnSearch.TabIndex = 1;
            this.btnSearch.Text = "搜索";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // cmbType
            // 
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.Items.AddRange(new object[] {
            "音效 (sounds)",
            "纹理 (textures)",
            "所有 (全部)"});
            this.cmbType.Location = new System.Drawing.Point(410, 12);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(120, 23);
            this.cmbType.TabIndex = 2;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblStatus.Location = new System.Drawing.Point(545, 15);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(37, 15);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "就绪";
            // 
            // dgvResults
            // 
            this.dgvResults.AllowUserToAddRows = false;
            this.dgvResults.AllowUserToDeleteRows = false;
            this.dgvResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvResults.Location = new System.Drawing.Point(0, 50);
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.ReadOnly = true;
            this.dgvResults.RowHeadersVisible = false;
            this.dgvResults.RowHeadersWidth = 51;
            this.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.Size = new System.Drawing.Size(800, 400);
            this.dgvResults.TabIndex = 0;
            // 
            // OutPutMinecraftAssets
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.dgvResults);
            this.Controls.Add(this.panelTop);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "OutPutMinecraftAssets";
            this.Text = "Minecraft资源提取器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OutPutMinecraftAssets_FormClosing);
            this.Load += new System.EventHandler(this.OutPutMinecraftAssets_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}