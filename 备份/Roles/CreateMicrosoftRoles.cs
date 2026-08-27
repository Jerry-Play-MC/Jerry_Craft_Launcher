using MinecraftLauncher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace New_Launcher.Roles
{
    public partial class CreateMicrosoftRoles : Form
    {
        private readonly MinecraftAuthenticator _authenticator;
        private BackgroundWorker _bgWorker;
        public string AccountFilePath { get; private set; }

        public CreateMicrosoftRoles()
        {
            InitializeComponent();
            _authenticator = new MinecraftAuthenticator();
            _authenticator.OnUserCodeReceived += ShowUserCode;
        }

        // ---------- 事件处理 ----------
        private void ShowUserCode(string userCode, string verificationUri)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, string>(ShowUserCode), userCode, verificationUri);
                return;
            }
            UpdateStatus($"请访问 {verificationUri} 并输入代码: {userCode}", true);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            btnLogin.Text = "登录中...";
            UpdateStatus("正在准备登录，请稍候...", false);
            progressBar.Visible = true;

            _bgWorker = new BackgroundWorker();
            _bgWorker.DoWork += BgWorker_DoWork;
            _bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
            _bgWorker.RunWorkerAsync();
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                string savedPath = _authenticator.AuthenticateAndSave();
                e.Result = savedPath;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visible = false;
            btnLogin.Enabled = true;
            btnLogin.Text = "开始登录";

            if (e.Result is Exception ex)
            {
                UpdateStatus($"登录失败: {ex.Message}", false);
                MessageBox.Show($"登录过程中出现错误：\n{ex.Message}", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (e.Result is string filePath)
            {
                AccountFilePath = filePath;
                // 读取账号文件获取用户名和 UUID
                string json = File.ReadAllText(filePath);
                var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                string username = data["username"]?.ToString();
                string uuid = data["uuid"]?.ToString();

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(uuid))
                {
                    MainForm.AddRole(uuid, "Microsoft", username);
                    MainForm.SetDefaultRole(uuid);
                }

                UpdateStatus($"登录成功！账号已保存至：{filePath}", true);
                MessageBox.Show($"登录成功！\n账号文件已保存。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void UpdateStatus(string text, bool isSuccess)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, bool>(UpdateStatus), text, isSuccess);
                return;
            }
            lblStatus.Text = text;
            lblStatus.ForeColor = isSuccess ? Color.Green : Color.Gray;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_bgWorker != null && _bgWorker.IsBusy)
            {
                if (MessageBox.Show("正在登录中，确定要退出吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }

        private void CreateMicrosoftRoles_Load(object sender, EventArgs e)
        {

        }
    }
}