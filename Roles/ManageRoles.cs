using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace New_Launcher.Roles
{
    public partial class ManageRoles : Form
    {
        public ManageRoles()
        {
            InitializeComponent();
        }

        private void ManageRoles_Load(object sender, EventArgs e)
        {
            RefreshRoleList();
        }

        private void RefreshRoleList()
        {
            listViewRoles.Items.Clear();
            var roles = MainForm.GetRoleList();
            string defaultId = MainForm.GetDefaultRoleId();
            foreach (var role in roles)
            {
                var item = new ListViewItem(role.Name);
                item.SubItems.Add(role.Type);
                item.SubItems.Add(role.Id == defaultId ? "✓" : "");
                item.Tag = role;
                listViewRoles.Items.Add(item);
            }
        }

        private MainForm.RoleEntry GetSelectedRole()
        {
            if (listViewRoles.SelectedItems.Count == 0)
                return null;
            return listViewRoles.SelectedItems[0].Tag as MainForm.RoleEntry;
        }

        // ---- 添加角色 ----
        private void btnAdd_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("选择要创建的角色类型：\n点击“是”创建离线角色，点击“否”登录 Microsoft 账户。",
                                         "添加角色",
                                         MessageBoxButtons.YesNoCancel,
                                         MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                using (var createRole = new CreateRole())
                {
                    createRole.ShowDialog(this);
                }
                RefreshRoleList();
            }
            else if (result == DialogResult.No)
            {
                using (var msRole = new CreateMicrosoftRoles())
                {
                    msRole.ShowDialog(this);
                }
                RefreshRoleList();
            }
        }

        // ---- 删除角色 ----
        private void btnDelete_Click(object sender, EventArgs e)
        {
            var role = GetSelectedRole();
            if (role == null)
            {
                MessageBox.Show("请先选择一个角色。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string msg = $"确定要删除角色 \"{role.Name}\" 吗？";
            if (role.Type == "Microsoft")
            {
                msg += "\n\n注意：此账号为 Microsoft 账户，删除后需要重新登录才能使用。";
            }
            if (MessageBox.Show(msg, "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                return;

            bool deleted = MainForm.RemoveRole(role.Id);
            if (deleted)
            {
                // 如果删除的是默认角色，清空默认设置
                if (MainForm.GetDefaultRoleId() == role.Id)
                {
                    MainForm.SetDefaultRole(null);
                }

                // 删除角色文件
                string launcherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string rolesDir = Path.Combine(Path.Combine(launcherPath, "Launcher Setting"), "Roles");
                string filePath = null;
                if (role.Type == "Offline")
                    filePath = Path.Combine(rolesDir, role.Name + ".json");
                else if (role.Type == "Microsoft")
                    filePath = Path.Combine(Path.Combine(rolesDir, "Microsoft"), role.Name + ".json");

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }

                MessageBox.Show("角色删除成功。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshRoleList();
            }
            else
            {
                MessageBox.Show("删除角色失败，请检查设置。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---- 设为默认 ----
        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            var role = GetSelectedRole();
            if (role == null)
            {
                MessageBox.Show("请先选择一个角色。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MainForm.SetDefaultRole(role.Id);
            RefreshRoleList();
            MessageBox.Show($"已将 \"{role.Name}\" 设为默认角色。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---- 双击设为默认 ----
        private void listViewRoles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            btnSetDefault_Click(sender, e);
        }

        // ---- 关闭 ----
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}