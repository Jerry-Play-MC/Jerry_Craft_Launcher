using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace New_Launcher
{
    public partial class CreateRole : Form
    {
        public static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        public static string RolePath = Path.Combine(Path.Combine(LauncherPath, "Launcher Setting"), "Roles");
        public static bool IsFirstUse = false;
        private bool _closeByBackButton = false;

        public CreateRole(bool isFirstUse = false)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            IsFirstUse = isFirstUse;

            if (!Directory.Exists(RolePath)) Directory.CreateDirectory(RolePath);
        }

        private void CreateRole_Load(object sender, EventArgs e)
        {

        }

        private void button_Save_Click(object sender, EventArgs e)
        {
            _closeByBackButton = true;
            string UserID = this.textBox_RoleName.Text;
            if (UserID.Length > 3 && UserID.Length < 16 && !IfHaveChineseCode.HasChineseCode(UserID))
            {
                string uuid = GenerateOfflineUuid(UserID);

                // ★ 统一使用 "username" 字段（同时保留 "PlayerID" 兼容旧版本，但新文件只写 "username"）
                var roleData = new
                {
                    username = UserID,      // 主键
                    uuid = uuid
                    // 不再写 PlayerID，统一用 username
                };

                string jsonContent = JsonConvert.SerializeObject(roleData, Formatting.Indented);

                // ★ 使用 uuid 作为文件名，而不是用户名
                string roleFilePath = Path.Combine(RolePath, uuid + ".json");
                File.WriteAllText(roleFilePath, jsonContent);

                // 添加到 Settings.json
                MainForm.AddRole(uuid, "Offline", UserID);
                MainForm.SetDefaultRole(uuid);

                MessageBox.Show("角色创建成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine(GetTime.GetCurrentTimeString() + "角色创建成功，角色名：" + UserID);
                this.Close();
            }
            else
            {
                MessageBox.Show("角色名不符合要求，请检查是否有中文字符或长度是否在4-16个字符之间", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Skip_Click(object sender, EventArgs e)
        {
            _closeByBackButton = true;
            if (IsFirstUse)
            {
                MessageBox.Show("启动游戏必须创建角色，记得在启动游戏之前回来创建角色哦！", "跳过创建角色",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            this.Close();
        }

        public static string GenerateOfflineUuid(string username)
        {

            if (username == null)
                throw new ArgumentNullException(nameof(username));
            if (username.Length == 0)
                throw new ArgumentException("用户名不能为空", nameof(username));

            // 1. 拼接前缀和用户名
            string input = "OfflinePlayer:" + username;
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            // 2. 计算MD5
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(inputBytes);

                // 3. 设置版本（version 3）：将第7个字节（索引6）的高4位设为 0011 (0x3)
                hash[6] = (byte)((hash[6] & 0x0F) | 0x30);

                // 4. 设置变体（variant）：将第9个字节（索引8）的高2位设为 10 (0x80)
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

                // 5. 转换为标准UUID格式的十六进制字符串
                //    先转为连续32位小写十六进制
                string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                //    插入连字符：8-4-4-4-12
                return string.Format("{0}-{1}-{2}-{3}-{4}",
                    hex.Substring(0, 8),
                    hex.Substring(8, 4),
                    hex.Substring(12, 4),
                    hex.Substring(16, 4),
                    hex.Substring(20, 12)
                );
            }

        }

        private void CreateRole_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closeByBackButton)
            {
                Application.Exit();
            }
        }
    }
}