namespace New_Launcher
{
    partial class MainForm
    {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.button_StartGame = new System.Windows.Forms.Button();
            this.button_Roles = new System.Windows.Forms.Button();
            this.button_DownloadVersions = new System.Windows.Forms.Button();
            this.button_Settings = new System.Windows.Forms.Button();
            this.label_VersionSetting = new System.Windows.Forms.Label();
            this.comboBox_Version = new System.Windows.Forms.ComboBox();
            this.label_PlayerID = new System.Windows.Forms.Label();
            this.label_uuid = new System.Windows.Forms.Label();
            this.button_DownloadMod = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button_StartGame
            // 
            this.button_StartGame.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_StartGame.Location = new System.Drawing.Point(691, 399);
            this.button_StartGame.Name = "button_StartGame";
            this.button_StartGame.Size = new System.Drawing.Size(97, 39);
            this.button_StartGame.TabIndex = 0;
            this.button_StartGame.Text = "启动游戏";
            this.button_StartGame.UseVisualStyleBackColor = true;
            this.button_StartGame.Click += new System.EventHandler(this.button_StartGame_Click);
            // 
            // button_Roles
            // 
            this.button_Roles.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_Roles.Location = new System.Drawing.Point(71, 97);
            this.button_Roles.Name = "button_Roles";
            this.button_Roles.Size = new System.Drawing.Size(75, 75);
            this.button_Roles.TabIndex = 1;
            this.button_Roles.Text = "角色";
            this.button_Roles.UseVisualStyleBackColor = true;
            this.button_Roles.Click += new System.EventHandler(this.button_Roles_Click);
            // 
            // button_DownloadVersions
            // 
            this.button_DownloadVersions.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_DownloadVersions.Location = new System.Drawing.Point(12, 399);
            this.button_DownloadVersions.Name = "button_DownloadVersions";
            this.button_DownloadVersions.Size = new System.Drawing.Size(97, 39);
            this.button_DownloadVersions.TabIndex = 2;
            this.button_DownloadVersions.Text = "下载版本";
            this.button_DownloadVersions.UseVisualStyleBackColor = true;
            this.button_DownloadVersions.Click += new System.EventHandler(this.button_DownloadVersions_Click);
            // 
            // button_Settings
            // 
            this.button_Settings.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_Settings.Location = new System.Drawing.Point(115, 399);
            this.button_Settings.Name = "button_Settings";
            this.button_Settings.Size = new System.Drawing.Size(97, 39);
            this.button_Settings.TabIndex = 3;
            this.button_Settings.Text = "设置";
            this.button_Settings.UseVisualStyleBackColor = true;
            this.button_Settings.Click += new System.EventHandler(this.button_Settings_Click);
            // 
            // label_VersionSetting
            // 
            this.label_VersionSetting.AutoSize = true;
            this.label_VersionSetting.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_VersionSetting.Location = new System.Drawing.Point(302, 9);
            this.label_VersionSetting.Name = "label_VersionSetting";
            this.label_VersionSetting.Size = new System.Drawing.Size(99, 20);
            this.label_VersionSetting.TabIndex = 5;
            this.label_VersionSetting.Text = "已选中版本：";
            // 
            // comboBox_Version
            // 
            this.comboBox_Version.FormattingEnabled = true;
            this.comboBox_Version.Location = new System.Drawing.Point(407, 9);
            this.comboBox_Version.Name = "comboBox_Version";
            this.comboBox_Version.Size = new System.Drawing.Size(381, 23);
            this.comboBox_Version.TabIndex = 6;
            // 
            // label_PlayerID
            // 
            this.label_PlayerID.AutoSize = true;
            this.label_PlayerID.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_PlayerID.Location = new System.Drawing.Point(12, 212);
            this.label_PlayerID.Name = "label_PlayerID";
            this.label_PlayerID.Size = new System.Drawing.Size(69, 20);
            this.label_PlayerID.TabIndex = 7;
            this.label_PlayerID.Text = "玩家ID：";
            // 
            // label_uuid
            // 
            this.label_uuid.AutoSize = true;
            this.label_uuid.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_uuid.Location = new System.Drawing.Point(12, 232);
            this.label_uuid.Name = "label_uuid";
            this.label_uuid.Size = new System.Drawing.Size(86, 20);
            this.label_uuid.TabIndex = 13;
            this.label_uuid.Text = "玩家uuid：";
            // 
            // button_DownloadMod
            // 
            this.button_DownloadMod.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_DownloadMod.Location = new System.Drawing.Point(12, 354);
            this.button_DownloadMod.Name = "button_DownloadMod";
            this.button_DownloadMod.Size = new System.Drawing.Size(97, 39);
            this.button_DownloadMod.TabIndex = 14;
            this.button_DownloadMod.Text = "下载资源";
            this.button_DownloadMod.UseVisualStyleBackColor = true;
            this.button_DownloadMod.Click += new System.EventHandler(this.button_DownloadMod_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button_DownloadMod);
            this.Controls.Add(this.label_uuid);
            this.Controls.Add(this.label_PlayerID);
            this.Controls.Add(this.comboBox_Version);
            this.Controls.Add(this.label_VersionSetting);
            this.Controls.Add(this.button_Settings);
            this.Controls.Add(this.button_DownloadVersions);
            this.Controls.Add(this.button_Roles);
            this.Controls.Add(this.button_StartGame);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Jerry Studio Minecraft Launcher";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_StartGame;
        private System.Windows.Forms.Button button_Roles;
        private System.Windows.Forms.Button button_DownloadVersions;
        private System.Windows.Forms.Button button_Settings;
        private System.Windows.Forms.Label label_VersionSetting;
        private System.Windows.Forms.ComboBox comboBox_Version;
        private System.Windows.Forms.Label label_PlayerID;
        private System.Windows.Forms.Label label_uuid;
        private System.Windows.Forms.Button button_DownloadMod;
    }
}