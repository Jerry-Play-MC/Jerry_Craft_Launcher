namespace New_Launcher
{
    partial class DownloadVersions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DownloadVersions));
            this.textBox_VersionsName = new System.Windows.Forms.TextBox();
            this.label_VersionsName = new System.Windows.Forms.Label();
            this.comboBox_Fabric = new System.Windows.Forms.ComboBox();
            this.label_Fabric = new System.Windows.Forms.Label();
            this.comboBox_Quilt = new System.Windows.Forms.ComboBox();
            this.label_Quilt = new System.Windows.Forms.Label();
            this.button_Download = new System.Windows.Forms.Button();
            this.button_Back = new System.Windows.Forms.Button();
            this.richTextBox_Log = new System.Windows.Forms.RichTextBox();
            this.label_DownloadLog = new System.Windows.Forms.Label();
            this.label_NeoForge = new System.Windows.Forms.Label();
            this.comboBox_NeoForge = new System.Windows.Forms.ComboBox();
            this.label_Forge = new System.Windows.Forms.Label();
            this.comboBox_Forge = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // textBox_VersionsName
            // 
            this.textBox_VersionsName.Location = new System.Drawing.Point(100, 12);
            this.textBox_VersionsName.Name = "textBox_VersionsName";
            this.textBox_VersionsName.Size = new System.Drawing.Size(688, 25);
            this.textBox_VersionsName.TabIndex = 2;
            // 
            // label_VersionsName
            // 
            this.label_VersionsName.AutoSize = true;
            this.label_VersionsName.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_VersionsName.Location = new System.Drawing.Point(10, 12);
            this.label_VersionsName.Name = "label_VersionsName";
            this.label_VersionsName.Size = new System.Drawing.Size(84, 20);
            this.label_VersionsName.TabIndex = 3;
            this.label_VersionsName.Text = "版本名称：";
            // 
            // comboBox_Fabric
            // 
            this.comboBox_Fabric.FormattingEnabled = true;
            this.comboBox_Fabric.Location = new System.Drawing.Point(100, 101);
            this.comboBox_Fabric.Name = "comboBox_Fabric";
            this.comboBox_Fabric.Size = new System.Drawing.Size(688, 23);
            this.comboBox_Fabric.TabIndex = 4;
            // 
            // label_Fabric
            // 
            this.label_Fabric.AutoSize = true;
            this.label_Fabric.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_Fabric.Location = new System.Drawing.Point(12, 101);
            this.label_Fabric.Name = "label_Fabric";
            this.label_Fabric.Size = new System.Drawing.Size(68, 20);
            this.label_Fabric.TabIndex = 5;
            this.label_Fabric.Text = "Fabric：";
            // 
            // comboBox_Quilt
            // 
            this.comboBox_Quilt.FormattingEnabled = true;
            this.comboBox_Quilt.Location = new System.Drawing.Point(100, 130);
            this.comboBox_Quilt.Name = "comboBox_Quilt";
            this.comboBox_Quilt.Size = new System.Drawing.Size(688, 23);
            this.comboBox_Quilt.TabIndex = 6;
            // 
            // label_Quilt
            // 
            this.label_Quilt.AutoSize = true;
            this.label_Quilt.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_Quilt.Location = new System.Drawing.Point(12, 130);
            this.label_Quilt.Name = "label_Quilt";
            this.label_Quilt.Size = new System.Drawing.Size(59, 20);
            this.label_Quilt.TabIndex = 7;
            this.label_Quilt.Text = "Quilt：";
            // 
            // button_Download
            // 
            this.button_Download.Location = new System.Drawing.Point(713, 408);
            this.button_Download.Name = "button_Download";
            this.button_Download.Size = new System.Drawing.Size(75, 30);
            this.button_Download.TabIndex = 8;
            this.button_Download.Text = "下载";
            this.button_Download.UseVisualStyleBackColor = true;
            this.button_Download.Click += new System.EventHandler(this.button_Download_Click);
            // 
            // button_Back
            // 
            this.button_Back.Location = new System.Drawing.Point(632, 408);
            this.button_Back.Name = "button_Back";
            this.button_Back.Size = new System.Drawing.Size(75, 30);
            this.button_Back.TabIndex = 9;
            this.button_Back.Text = "返回";
            this.button_Back.UseVisualStyleBackColor = true;
            this.button_Back.Click += new System.EventHandler(this.button_Back_Click);
            // 
            // richTextBox_Log
            // 
            this.richTextBox_Log.Location = new System.Drawing.Point(100, 159);
            this.richTextBox_Log.Name = "richTextBox_Log";
            this.richTextBox_Log.ReadOnly = true;
            this.richTextBox_Log.Size = new System.Drawing.Size(688, 243);
            this.richTextBox_Log.TabIndex = 10;
            this.richTextBox_Log.Text = "";
            // 
            // label_DownloadLog
            // 
            this.label_DownloadLog.AutoSize = true;
            this.label_DownloadLog.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_DownloadLog.Location = new System.Drawing.Point(10, 159);
            this.label_DownloadLog.Name = "label_DownloadLog";
            this.label_DownloadLog.Size = new System.Drawing.Size(84, 20);
            this.label_DownloadLog.TabIndex = 11;
            this.label_DownloadLog.Text = "下载日志：";
            // 
            // label_NeoForge
            // 
            this.label_NeoForge.AutoSize = true;
            this.label_NeoForge.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_NeoForge.Location = new System.Drawing.Point(12, 72);
            this.label_NeoForge.Name = "label_NeoForge";
            this.label_NeoForge.Size = new System.Drawing.Size(98, 20);
            this.label_NeoForge.TabIndex = 12;
            this.label_NeoForge.Text = "NeoForge：";
            // 
            // comboBox_NeoForge
            // 
            this.comboBox_NeoForge.FormattingEnabled = true;
            this.comboBox_NeoForge.Location = new System.Drawing.Point(100, 72);
            this.comboBox_NeoForge.Name = "comboBox_NeoForge";
            this.comboBox_NeoForge.Size = new System.Drawing.Size(688, 23);
            this.comboBox_NeoForge.TabIndex = 13;
            this.comboBox_NeoForge.SelectedIndexChanged += new System.EventHandler(this.comboBox_NeoForge_SelectedIndexChanged);
            // 
            // label_Forge
            // 
            this.label_Forge.AutoSize = true;
            this.label_Forge.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_Forge.Location = new System.Drawing.Point(12, 43);
            this.label_Forge.Name = "label_Forge";
            this.label_Forge.Size = new System.Drawing.Size(67, 20);
            this.label_Forge.TabIndex = 14;
            this.label_Forge.Text = "Forge：";
            // 
            // comboBox_Forge
            // 
            this.comboBox_Forge.FormattingEnabled = true;
            this.comboBox_Forge.Location = new System.Drawing.Point(100, 43);
            this.comboBox_Forge.Name = "comboBox_Forge";
            this.comboBox_Forge.Size = new System.Drawing.Size(688, 23);
            this.comboBox_Forge.TabIndex = 15;
            this.comboBox_Forge.SelectedIndexChanged += new System.EventHandler(this.comboBox_Forge_SelectedIndexChanged);
            // 
            // DownloadVersions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.comboBox_Forge);
            this.Controls.Add(this.label_Forge);
            this.Controls.Add(this.comboBox_NeoForge);
            this.Controls.Add(this.label_NeoForge);
            this.Controls.Add(this.label_DownloadLog);
            this.Controls.Add(this.richTextBox_Log);
            this.Controls.Add(this.button_Back);
            this.Controls.Add(this.button_Download);
            this.Controls.Add(this.label_Quilt);
            this.Controls.Add(this.comboBox_Quilt);
            this.Controls.Add(this.label_Fabric);
            this.Controls.Add(this.comboBox_Fabric);
            this.Controls.Add(this.label_VersionsName);
            this.Controls.Add(this.textBox_VersionsName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DownloadVersions";
            this.Text = "下载版本";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DownloadVersions_FormClosing);
            this.Load += new System.EventHandler(this.DownloadVersions_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_VersionsName;
        private System.Windows.Forms.Label label_VersionsName;
        private System.Windows.Forms.ComboBox comboBox_Fabric;
        private System.Windows.Forms.Label label_Fabric;
        private System.Windows.Forms.ComboBox comboBox_Quilt;
        private System.Windows.Forms.Label label_Quilt;
        private System.Windows.Forms.Button button_Download;
        private System.Windows.Forms.Button button_Back;
        private System.Windows.Forms.RichTextBox richTextBox_Log;
        private System.Windows.Forms.Label label_DownloadLog;
        private System.Windows.Forms.Label label_NeoForge;
        private System.Windows.Forms.ComboBox comboBox_NeoForge;
        private System.Windows.Forms.Label label_Forge;
        private System.Windows.Forms.ComboBox comboBox_Forge;
    }
}