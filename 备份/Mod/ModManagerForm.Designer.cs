using System.Windows.Forms;

namespace New_Launcher
{
    partial class ModManagerForm : Form
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModManagerForm));
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnSearch = new System.Windows.Forms.Button();
            this.flpMods = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPrevPage = new System.Windows.Forms.Button();
            this.btnNextPage = new System.Windows.Forms.Button();
            this.lblPageInfo = new System.Windows.Forms.Label();
            this.comboBox_Type = new System.Windows.Forms.ComboBox();
            this.button_Back = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(13, 12);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(539, 25);
            this.txtSearch.TabIndex = 0;
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(560, 9);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(100, 27);
            this.btnSearch.TabIndex = 1;
            this.btnSearch.Text = "搜索";
            this.btnSearch.UseVisualStyleBackColor = true;
            // 
            // flpMods
            // 
            this.flpMods.AutoScroll = true;
            this.flpMods.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpMods.Location = new System.Drawing.Point(13, 43);
            this.flpMods.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.flpMods.Name = "flpMods";
            this.flpMods.Size = new System.Drawing.Size(774, 362);
            this.flpMods.TabIndex = 2;
            this.flpMods.WrapContents = false;
            // 
            // btnPrevPage
            // 
            this.btnPrevPage.Location = new System.Drawing.Point(13, 411);
            this.btnPrevPage.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnPrevPage.Name = "btnPrevPage";
            this.btnPrevPage.Size = new System.Drawing.Size(100, 27);
            this.btnPrevPage.TabIndex = 3;
            this.btnPrevPage.Text = "上一页";
            this.btnPrevPage.UseVisualStyleBackColor = true;
            // 
            // btnNextPage
            // 
            this.btnNextPage.Location = new System.Drawing.Point(580, 411);
            this.btnNextPage.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnNextPage.Name = "btnNextPage";
            this.btnNextPage.Size = new System.Drawing.Size(100, 27);
            this.btnNextPage.TabIndex = 4;
            this.btnNextPage.Text = "下一页";
            this.btnNextPage.UseVisualStyleBackColor = true;
            // 
            // lblPageInfo
            // 
            this.lblPageInfo.AutoSize = true;
            this.lblPageInfo.Location = new System.Drawing.Point(121, 417);
            this.lblPageInfo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPageInfo.Name = "lblPageInfo";
            this.lblPageInfo.Size = new System.Drawing.Size(93, 15);
            this.lblPageInfo.TabIndex = 5;
            this.lblPageInfo.Text = "第 1 / 1 页";
            // 
            // comboBox_Type
            // 
            this.comboBox_Type.FormattingEnabled = true;
            this.comboBox_Type.Items.AddRange(new object[] {
            "MOD",
            "整合包",
            "数据包",
            "资源包",
            "光影包"});
            this.comboBox_Type.Location = new System.Drawing.Point(667, 12);
            this.comboBox_Type.Name = "comboBox_Type";
            this.comboBox_Type.Size = new System.Drawing.Size(121, 23);
            this.comboBox_Type.TabIndex = 6;
            // 
            // button_Back
            // 
            this.button_Back.Location = new System.Drawing.Point(688, 411);
            this.button_Back.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.button_Back.Name = "button_Back";
            this.button_Back.Size = new System.Drawing.Size(100, 27);
            this.button_Back.TabIndex = 7;
            this.button_Back.Text = "返回";
            this.button_Back.UseVisualStyleBackColor = true;
            this.button_Back.Click += new System.EventHandler(this.button_Back_Click);
            // 
            // ModManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button_Back);
            this.Controls.Add(this.comboBox_Type);
            this.Controls.Add(this.lblPageInfo);
            this.Controls.Add(this.btnNextPage);
            this.Controls.Add(this.btnPrevPage);
            this.Controls.Add(this.flpMods);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.txtSearch);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "ModManagerForm";
            this.Text = "Mod 管理器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ModManagerForm_FormClosing);
            this.Load += new System.EventHandler(this.ModManagerForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.FlowLayoutPanel flpMods;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
        private System.Windows.Forms.Label lblPageInfo;
        private ComboBox comboBox_Type;
        private Button button_Back;
    }
}