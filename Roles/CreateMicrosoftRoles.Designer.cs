namespace New_Launcher.Roles
{
    partial class CreateMicrosoftRoles
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
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
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateMicrosoftRoles));
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblInstruction = new System.Windows.Forms.Label();
            this.btnLogin = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblTitle.Location = new System.Drawing.Point(40, 30);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(263, 36);
            this.lblTitle.TabIndex = 5;
            this.lblTitle.Text = "Microsoft 账户登录";
            // 
            // lblInstruction
            // 
            this.lblInstruction.AutoSize = true;
            this.lblInstruction.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.lblInstruction.Location = new System.Drawing.Point(40, 80);
            this.lblInstruction.MaximumSize = new System.Drawing.Size(480, 0);
            this.lblInstruction.Name = "lblInstruction";
            this.lblInstruction.Size = new System.Drawing.Size(473, 46);
            this.lblInstruction.TabIndex = 4;
            this.lblInstruction.Text = "点击下方按钮，您的默认浏览器将打开 Microsoft 登录页面。\r\n请按提示输入显示的验证码以完成授权。";
            this.lblInstruction.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnLogin
            // 
            this.btnLogin.BackColor = System.Drawing.Color.LightSkyBlue;
            this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogin.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.btnLogin.Location = new System.Drawing.Point(608, 398);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(180, 40);
            this.btnLogin.TabIndex = 2;
            this.btnLogin.Text = "登录";
            this.btnLogin.UseVisualStyleBackColor = false;
            this.btnLogin.Click += new System.EventHandler(this.BtnLogin_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(40, 340);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(39, 20);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "就绪";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(40, 370);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(400, 20);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 0;
            this.progressBar.Visible = false;
            // 
            // CreateMicrosoftRoles
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.lblInstruction);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CreateMicrosoftRoles";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "登录到您的 Microsoft 账户";
            this.Load += new System.EventHandler(this.CreateMicrosoftRoles_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        // 控件字段声明（由设计器自动生成，但为了清晰列出）
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblInstruction;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}