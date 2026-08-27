using New_Launcher.SavesManager;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace New_Launcher
{
    public partial class Settings : Form
    {
        private static string LauncherPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string MinecraftDir = Path.Combine(LauncherPath, ".minecraft");
        private static string SettingsDir = Path.Combine(LauncherPath, "Launcher Setting");
        private static string javaDir = Path.Combine(SettingsDir, "Java");
        private string _originalVersionName = "";
        private string _pendingNewName = null;
        private string customJavaFolder = null;
        private bool _isLoading = false;
        private string customJavaPath = null;
        private bool _closeByBackButton = false;

        private static string SettingsFilePath
        {
            get
            {
                string launcherPath = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(Path.Combine(launcherPath, "Launcher Setting"), "Settings.json");
            }
        }

        private bool currentIsolatedValue;

        // 用于存储外部选择的存档路径（非列表中的）
        private string _currentWorldPath = null;

        public Settings()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Load += Settings_Load;
            this.comboBox_Java.SelectedIndexChanged += comboBox_Java_SelectedIndexChanged;

            // 绑定事件（设计器未绑定的）
            this.comboBox_SavesList.SelectedIndexChanged += ComboBox_SavesList_SelectedIndexChanged;
            this.button_Fresh.Click += Button_Fresh_Click;
            this.btn_ApplyAll.Click += Btn_ApplyAll_Click;
            this.btn_Resurrect.Click += Btn_Resurrect_Click;
            this.btn_ReloadFromSave.Click += Btn_ReloadFromSave_Click;
        }

        #region 窗体加载

        private void Settings_Load(object sender, EventArgs e)
        {
            // 先加载自定义 Java 路径
            LoadCustomJavaPath();

            // 填充 Java 下拉框（此方法内部会取消/恢复事件绑定，避免循环触发）
            PopulateJavaComboBox();

            // 其他初始化
            comboBox_GameSettings_VersionIsolated.Items.Clear();
            comboBox_GameSettings_VersionIsolated.Items.Add("启用版本隔离");
            comboBox_GameSettings_VersionIsolated.Items.Add("禁用版本隔离");

            LoadSettings();  // 此方法只读取 VersionIsolated，不影响 Java
            LoadVersionList();
            LoadSavesList();
        }

        private void LoadCustomJavaPath()
        {
            string settingsFile = Path.Combine(SettingsDir, "Settings.json");
            if (File.Exists(settingsFile))
            {
                try
                {
                    string jsonContent = File.ReadAllText(settingsFile);
                    JObject settingsJson = JObject.Parse(jsonContent);
                    var javaToken = settingsJson.SelectToken("GameSettings.Java");
                    if (javaToken != null)
                    {
                        string folderName = javaToken.Value<string>();
                        // 只要文件夹名不是 null（即 JSON 中明确写了值），就保留它
                        if (folderName != null)
                        {
                            customJavaFolder = folderName; // 保留文件夹名，即使文件不存在
                            string fullPath = Path.Combine(Path.Combine(Path.Combine(javaDir, folderName), "bin"), "java.exe");
                            if (File.Exists(fullPath))
                            {
                                customJavaPath = fullPath;
                                Console.WriteLine($"[Settings] 已加载自定义 Java: 文件夹={folderName}, 路径={fullPath}");
                            }
                            else
                            {
                                customJavaPath = null; // 路径无效，但文件夹名保留
                                Console.WriteLine($"[Settings] 警告：文件夹 {folderName} 中的 java.exe 不存在，但已保留选择。");
                            }
                            return;
                        }
                        else
                        {
                            // 值为 null，表示智能选择
                            customJavaFolder = null;
                            customJavaPath = null;
                            Console.WriteLine("[Settings] GameSettings.Java 为 null，将使用自动选择。");
                        }
                    }
                    else
                    {
                        // 键不存在，视为未设置
                        Console.WriteLine("[Settings] GameSettings.Java 键不存在，将使用自动选择。");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Settings] 读取 Settings.json 失败: {ex.Message}");
                }
            }
            // 文件不存在或键不存在，置 null
            customJavaFolder = null;
            customJavaPath = null;
            Console.WriteLine("[Settings] 未找到有效自定义 Java，将使用自动选择。");
        }

        #endregion

        #region 全局设置

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    currentIsolatedValue = true;
                }
                else
                {
                    string jsonContent = File.ReadAllText(SettingsFilePath);
                    JObject settingsJson = JObject.Parse(jsonContent);
                    currentIsolatedValue = settingsJson.SelectToken("GameSettings.VersionIsolated")?.Value<bool>() ?? true;
                }
                comboBox_GameSettings_VersionIsolated.SelectedIndex = currentIsolatedValue ? 0 : 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取设置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_GameSettings_VersionIsolated.SelectedIndex = 0;
                currentIsolatedValue = true;
            }
        }

        private void SaveSettings()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                JObject settingsJson;
                if (File.Exists(SettingsFilePath))
                {
                    string jsonContent = File.ReadAllText(SettingsFilePath);
                    settingsJson = JObject.Parse(jsonContent);
                }
                else
                {
                    settingsJson = new JObject();
                }

                if (settingsJson["GameSettings"] == null)
                    settingsJson["GameSettings"] = new JObject();

                settingsJson["GameSettings"]["VersionIsolated"] = currentIsolatedValue;
                File.WriteAllText(SettingsFilePath, settingsJson.ToString(Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_pendingNewName) && _pendingNewName != _originalVersionName)
            {
                if (RenameVersion(_originalVersionName, _pendingNewName))
                {
                    LoadVersionList();
                    comboBox_Versions.SelectedItem = _pendingNewName;
                    _originalVersionName = _pendingNewName;
                    _pendingNewName = null;
                    MessageBox.Show($"版本已成功重命名为：{_originalVersionName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _pendingNewName = null;
                }
            }
        }

        private void PopulateJavaComboBox()
        {
            _isLoading = true; // 开始加载

            // 暂时移除事件
            this.comboBox_Java.SelectedIndexChanged -= comboBox_Java_SelectedIndexChanged;

            var items = new List<string>();
            items.Add("根据版本号智能选择");

            if (Directory.Exists(javaDir))
            {
                var versionDirs = Directory.GetDirectories(javaDir);
                foreach (var dir in versionDirs)
                {
                    string folderName = Path.GetFileName(dir);
                    string javaExe = Path.Combine(Path.Combine(dir, "bin"), "java.exe");
                    if (File.Exists(javaExe))
                        items.Add(folderName);
                }
            }

            if (items.Count == 1)
                items.Add("未检测到任何安装");

            comboBox_Java.DataSource = items;

            int selectedIndex = 0; // 默认为智能选择
            if (!string.IsNullOrEmpty(customJavaFolder))
            {
                for (int i = 0; i < comboBox_Java.Items.Count; i++)
                {
                    if (string.Equals(comboBox_Java.Items[i].ToString(), customJavaFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            comboBox_Java.SelectedIndex = selectedIndex;

            // 重新添加事件
            this.comboBox_Java.SelectedIndexChanged += comboBox_Java_SelectedIndexChanged;

            _isLoading = false; // 加载完成
        }
        #endregion

        #region 版本管理

        private void LoadVersionList()
        {
            try
            {
                string versionsDir = Path.Combine(MinecraftDir, "versions");
                if (!Directory.Exists(versionsDir))
                {
                    Directory.CreateDirectory(versionsDir);
                    return;
                }

                var versionFolders = Directory.GetDirectories(versionsDir);
                var versionNames = new List<string>();
                foreach (var folder in versionFolders)
                {
                    string versionName = Path.GetFileName(folder);
                    string jsonPath = Path.Combine(folder, versionName + ".json");
                    if (File.Exists(jsonPath))
                        versionNames.Add(versionName);
                }

                versionNames.Sort();
                comboBox_Versions.DataSource = versionNames;
                if (comboBox_Versions.Items.Count > 0)
                    comboBox_Versions.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Settings] 加载版本列表失败: {ex.Message}");
            }
        }

        private bool RenameVersion(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("原版本名或新版本名为空。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (oldName == newName)
            {
                MessageBox.Show("新旧版本名相同，无需重命名。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (newName.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show($"新版本名包含非法字符（如 \\ / : * ? \" < > |）", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string versionsDir = Path.Combine(MinecraftDir, "versions");
            string oldFolder = Path.Combine(versionsDir, oldName);
            string newFolder = Path.Combine(versionsDir, newName);

            if (!Directory.Exists(oldFolder))
            {
                MessageBox.Show($"原版本文件夹不存在：{oldFolder}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (Directory.Exists(newFolder))
            {
                MessageBox.Show($"目标版本名 {newName} 已存在，无法重命名。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string oldJsonPath = Path.Combine(oldFolder, oldName + ".json");
            if (!File.Exists(oldJsonPath))
            {
                MessageBox.Show($"找不到版本 JSON 文件：{oldJsonPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            JObject jsonObj;
            try
            {
                string jsonText = File.ReadAllText(oldJsonPath);
                jsonObj = JObject.Parse(jsonText);
                jsonObj["id"] = newName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析或修改 JSON 文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                string tempJsonPath = Path.Combine(oldFolder, "temp_" + newName + ".json");
                File.WriteAllText(tempJsonPath, jsonObj.ToString(Newtonsoft.Json.Formatting.Indented));

                Directory.Move(oldFolder, newFolder);

                string tempJsonInNew = Path.Combine(newFolder, "temp_" + newName + ".json");
                string oldJsonInNew = Path.Combine(newFolder, oldName + ".json");
                string newJsonInNew = Path.Combine(newFolder, newName + ".json");

                if (File.Exists(oldJsonInNew))
                {
                    File.Delete(oldJsonInNew);
                    File.Move(tempJsonInNew, newJsonInNew);
                }
                else
                {
                    File.Move(tempJsonInNew, newJsonInNew);
                }

                string oldJarPath = Path.Combine(newFolder, oldName + ".jar");
                string newJarPath = Path.Combine(newFolder, newName + ".jar");
                if (File.Exists(oldJarPath))
                {
                    File.Move(oldJarPath, newJarPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名过程中发生错误：{ex.Message}\n请检查文件夹权限或手动恢复。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        #endregion

        #region 存档管理（核心逻辑）

        private string GetSavesRootPath()
        {
            if (currentIsolatedValue)
            {
                string selectedVersion = comboBox_Versions.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedVersion))
                {
                    return Path.Combine(Path.Combine(Path.Combine(MinecraftDir, "versions"), selectedVersion), "saves");
                }
            }
            return Path.Combine(MinecraftDir, "saves");
        }

        private void LoadSavesList()
        {
            comboBox_SavesList.Items.Clear();
            string savesPath = GetSavesRootPath();
            if (!Directory.Exists(savesPath))
            {
                if (!currentIsolatedValue || !Directory.Exists(savesPath))
                {
                    try { Directory.CreateDirectory(savesPath); }
                    catch { /* 忽略 */ }
                }
                return;
            }

            foreach (string dir in Directory.GetDirectories(savesPath))
            {
                string worldName = Path.GetFileName(dir);
                if (File.Exists(Path.Combine(dir, "level.dat")))
                    comboBox_SavesList.Items.Add(worldName);
            }

            if (comboBox_SavesList.Items.Count > 0)
                comboBox_SavesList.SelectedIndex = 0;
        }

        private void ComboBox_SavesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 选择了下拉列表中的存档，清除外部路径标记
            _currentWorldPath = null;
            // 恢复标签显示（如果之前显示外部路径，现在恢复）
            UpdateLabelForCurrentPath();
            LoadFromSave();
        }

        private void Button_Fresh_Click(object sender, EventArgs e)
        {
            LoadSavesList();
        }

        /// <summary>
        /// 更新“存档：”标签显示当前路径或存档名
        /// </summary>
        private void UpdateLabelForCurrentPath()
        {
            if (!string.IsNullOrEmpty(_currentWorldPath))
            {
                label_SavesManager.Text = $"存档(外部)：{_currentWorldPath}";
            }
            else if (comboBox_SavesList.SelectedItem != null)
            {
                label_SavesManager.Text = $"存档：{comboBox_SavesList.SelectedItem.ToString()}";
            }
            else
            {
                label_SavesManager.Text = "存档：";
            }
        }

        /// <summary>
        /// 从存档加载所有值到控件（支持外部路径）
        /// </summary>
        private void LoadFromSave()
        {
            string worldPath = null;

            // 优先使用外部路径
            if (!string.IsNullOrEmpty(_currentWorldPath))
            {
                worldPath = _currentWorldPath;
            }
            else
            {
                // 否则从下拉框获取
                if (comboBox_SavesList.SelectedItem == null)
                    return;
                string worldName = comboBox_SavesList.SelectedItem.ToString();
                string savesRoot = GetSavesRootPath();
                worldPath = Path.Combine(savesRoot, worldName);
            }

            if (!Directory.Exists(worldPath))
            {
                MessageBox.Show($"存档文件夹不存在：{worldPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 确保存在 level.dat
            string levelDatPath = Path.Combine(worldPath, "level.dat");
            if (!File.Exists(levelDatPath))
            {
                MessageBox.Show($"找不到 level.dat：{levelDatPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                List<ModificationItem> items = LevelDatManager.GetModifications(worldPath);

                // 重置所有控件
                chk_AllowCommands.Checked = false;
                chk_Hardcore.Checked = false;
                cmb_GameType.SelectedIndex = -1;
                cmb_Difficulty.SelectedIndex = -1;
                chk_DifficultyLocked.Checked = false;
                txt_LevelName.Text = "";
                txt_Spawn.Text = "";
                chk_Initialized.Checked = false;
                chk_KeepInventory.Checked = false;
                chk_DaylightCycle.Checked = false;
                chk_MobSpawning.Checked = false;
                chk_FireTick.Checked = false;
                chk_WeatherCycle.Checked = false;

                foreach (var item in items)
                {
                    switch (item.Name)
                    {
                        case "AllowCommands":
                            chk_AllowCommands.Checked = Convert.ToBoolean(item.CurrentValue);
                            break;
                        case "Hardcore":
                            chk_Hardcore.Checked = Convert.ToBoolean(item.CurrentValue);
                            break;
                        case "GameType":
                            int gt = Convert.ToInt32(item.CurrentValue);
                            cmb_GameType.SelectedIndex = gt >= 0 && gt <= 3 ? gt : 0;
                            break;
                        case "Difficulty":
                            string diff = item.CurrentValue?.ToString() ?? "normal";
                            int diffIndex;
                            switch (diff)
                            {
                                case "peaceful": diffIndex = 0; break;
                                case "easy": diffIndex = 1; break;
                                case "normal": diffIndex = 2; break;
                                case "hard": diffIndex = 3; break;
                                default: diffIndex = 2; break;
                            }
                            cmb_Difficulty.SelectedIndex = diffIndex;
                            break;
                        case "DifficultyLocked":
                            chk_DifficultyLocked.Checked = Convert.ToBoolean(item.CurrentValue);
                            break;
                        case "LevelName":
                            txt_LevelName.Text = item.CurrentValue?.ToString() ?? "";
                            break;
                        case "Spawn":
                            txt_Spawn.Text = item.CurrentValue?.ToString() ?? "";
                            break;
                        case "Initialized":
                            chk_Initialized.Checked = Convert.ToBoolean(item.CurrentValue);
                            break;
                        case "GameRule.keepInventory":
                            chk_KeepInventory.Checked = item.CurrentValue?.ToString() == "true";
                            break;
                        case "GameRule.doDaylightCycle":
                            chk_DaylightCycle.Checked = item.CurrentValue?.ToString() == "true";
                            break;
                        case "GameRule.doMobSpawning":
                            chk_MobSpawning.Checked = item.CurrentValue?.ToString() == "true";
                            break;
                        case "GameRule.doFireTick":
                            chk_FireTick.Checked = item.CurrentValue?.ToString() == "true";
                            break;
                        case "GameRule.doWeatherCycle":
                            chk_WeatherCycle.Checked = item.CurrentValue?.ToString() == "true";
                            break;
                        default:
                            break;
                    }
                }

                // 更新标签显示
                UpdateLabelForCurrentPath();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载存档数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 应用所有修改到存档（支持外部路径）
        /// </summary>
        private void ApplyAllModifications()
        {
            string worldPath = null;
            if (!string.IsNullOrEmpty(_currentWorldPath))
                worldPath = _currentWorldPath;
            else if (comboBox_SavesList.SelectedItem != null)
            {
                string savesRoot = GetSavesRootPath();
                worldPath = Path.Combine(savesRoot, comboBox_SavesList.SelectedItem.ToString());
            }
            else
            {
                MessageBox.Show("请先选择一个存档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 基本设置
                LevelDatManager.ApplyModification(worldPath, "AllowCommands", chk_AllowCommands.Checked);
                LevelDatManager.ApplyModification(worldPath, "Hardcore", chk_Hardcore.Checked);
                LevelDatManager.ApplyModification(worldPath, "GameType", cmb_GameType.SelectedIndex);

                string selectedDiffText = cmb_Difficulty.SelectedItem?.ToString();
                string difficultyValue = "normal";
                if (!string.IsNullOrEmpty(selectedDiffText))
                {
                    int start = selectedDiffText.IndexOf('(');
                    int end = selectedDiffText.IndexOf(')');
                    if (start != -1 && end != -1 && start < end)
                        difficultyValue = selectedDiffText.Substring(start + 1, end - start - 1);
                }
                LevelDatManager.ApplyModification(worldPath, "Difficulty", difficultyValue);

                LevelDatManager.ApplyModification(worldPath, "DifficultyLocked", chk_DifficultyLocked.Checked);

                // 高级设置
                LevelDatManager.ApplyModification(worldPath, "LevelName", txt_LevelName.Text);
                LevelDatManager.ApplyModification(worldPath, "Spawn", txt_Spawn.Text);
                LevelDatManager.ApplyModification(worldPath, "Initialized", chk_Initialized.Checked);

                // 游戏规则
                LevelDatManager.ApplyModification(worldPath, "GameRule.keepInventory", chk_KeepInventory.Checked ? "true" : "false");
                LevelDatManager.ApplyModification(worldPath, "GameRule.doDaylightCycle", chk_DaylightCycle.Checked ? "true" : "false");
                LevelDatManager.ApplyModification(worldPath, "GameRule.doMobSpawning", chk_MobSpawning.Checked ? "true" : "false");
                LevelDatManager.ApplyModification(worldPath, "GameRule.doFireTick", chk_FireTick.Checked ? "true" : "false");
                LevelDatManager.ApplyModification(worldPath, "GameRule.doWeatherCycle", chk_WeatherCycle.Checked ? "true" : "false");

                LoadFromSave();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用修改失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 按钮事件

        private void Btn_ApplyAll_Click(object sender, EventArgs e)
        {
            ApplyAllModifications();
        }

        private void Btn_Resurrect_Click(object sender, EventArgs e)
        {
            string worldPath = null;
            if (!string.IsNullOrEmpty(_currentWorldPath))
                worldPath = _currentWorldPath;
            else if (comboBox_SavesList.SelectedItem != null)
            {
                string savesRoot = GetSavesRootPath();
                worldPath = Path.Combine(savesRoot, comboBox_SavesList.SelectedItem.ToString());
            }
            else
            {
                MessageBox.Show("请先选择一个存档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                LevelDatManager.ResurrectHardcorePlayer(worldPath);
                MessageBox.Show(
                    "✅ 急救成功！\n" +
                    "玩家已重置为生存模式，死亡标记已清除，极限模式保持开启。\n" +
                    "重新进入游戏即可继续冒险。",
                    "成功",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                LoadFromSave();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"急救失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Btn_ReloadFromSave_Click(object sender, EventArgs e)
        {
            LoadFromSave();
        }

        /// <summary>
        /// 额外选择按钮：弹出文件夹选择对话框，加载任意存档
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择包含 level.dat 的存档文件夹";
                dialog.ShowNewFolderButton = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    string levelDatPath = Path.Combine(selectedPath, "level.dat");
                    if (!File.Exists(levelDatPath))
                    {
                        MessageBox.Show("所选文件夹中未找到 level.dat 文件，请选择有效的存档文件夹。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 设置外部路径并加载
                    _currentWorldPath = selectedPath;
                    // 取消下拉框选中（避免混淆）
                    comboBox_SavesList.SelectedIndex = -1;
                    LoadFromSave();
                }
            }
        }

        #endregion

        #region 全局事件

        private void comboBox_GameSettings_VersionIsolated_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_GameSettings_VersionIsolated.SelectedIndex == 0)
                currentIsolatedValue = true;
            else if (comboBox_GameSettings_VersionIsolated.SelectedIndex == 1)
                currentIsolatedValue = false;

            LoadSavesList();
        }

        private void button_Save_Click(object sender, EventArgs e)
        {
            _closeByBackButton = true;
            SaveSettings();
            ApplyAllModifications();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_NotSave_Click(object sender, EventArgs e)
        {
            _closeByBackButton = true;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void button_InputOfficialLauncher_Click(object sender, EventArgs e)
        {
            InputOfficialLauncher form = new InputOfficialLauncher();
            form.ShowDialog();
        }

        private void comboBox_Versions_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox_Versions.SelectedItem?.ToString() ?? "";
            this.textBox_Versions_VerssionManager_NewName.Text = selected;
            _originalVersionName = selected;
            _pendingNewName = null;

            // 切换版本时如果当前有外部路径，清空外部路径并重新加载列表
            _currentWorldPath = null;
            LoadSavesList();
        }

        private void textBox_Versions_VerssionManager_NewName_TextChanged(object sender, EventArgs e)
        {
            string newName = this.textBox_Versions_VerssionManager_NewName.Text.Trim();

            if (string.IsNullOrEmpty(newName) || newName == _originalVersionName)
            {
                _pendingNewName = null;
                textBox_Versions_VerssionManager_NewName.BackColor = Color.White;
                return;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (newName.IndexOfAny(invalidChars) >= 0)
            {
                textBox_Versions_VerssionManager_NewName.BackColor = Color.LightCoral;
                _pendingNewName = null;
                return;
            }

            string versionsDir = Path.Combine(MinecraftDir, "versions");
            if (Directory.Exists(Path.Combine(versionsDir, newName)) && newName != _originalVersionName)
            {
                textBox_Versions_VerssionManager_NewName.BackColor = Color.LightCoral;
                _pendingNewName = null;
                return;
            }

            textBox_Versions_VerssionManager_NewName.BackColor = Color.White;
            _pendingNewName = newName;
        }

        #endregion

        private void comboBox_Java_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoading) return; // 加载过程中不执行任何保存操作

            string selectedText = comboBox_Java.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedText)) return;

            string settingsDir = Path.Combine(LauncherPath, "Launcher Setting");
            string settingsFile = Path.Combine(settingsDir, "Settings.json");

            if (selectedText == "根据版本号智能选择")
            {
                // 清除自定义 Java：将 GameSettings.Java 设为 null
                customJavaFolder = null;
                customJavaPath = null;
                try
                {
                    JObject settingsJson;
                    if (File.Exists(settingsFile))
                    {
                        string jsonContent = File.ReadAllText(settingsFile);
                        settingsJson = JObject.Parse(jsonContent);
                    }
                    else
                    {
                        settingsJson = new JObject();
                        if (!Directory.Exists(settingsDir))
                            Directory.CreateDirectory(settingsDir);
                    }

                    if (settingsJson["GameSettings"] == null)
                        settingsJson["GameSettings"] = new JObject();

                    settingsJson["GameSettings"]["Java"] = null;
                    File.WriteAllText(settingsFile, settingsJson.ToString(Newtonsoft.Json.Formatting.Indented));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清除 Java 路径失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if (selectedText == "未检测到任何安装")
                return;

            string folderName = selectedText;
            string fullPath = Path.Combine(Path.Combine(Path.Combine(javaDir, folderName), "bin"), "java.exe");
            if (!File.Exists(fullPath))
            {
                MessageBox.Show($"选中的文件夹 '{folderName}' 中未找到 bin\\java.exe", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 保存文件夹名
            try
            {
                JObject settingsJson;
                if (File.Exists(settingsFile))
                {
                    string jsonContent = File.ReadAllText(settingsFile);
                    settingsJson = JObject.Parse(jsonContent);
                }
                else
                {
                    settingsJson = new JObject();
                    if (!Directory.Exists(settingsDir))
                        Directory.CreateDirectory(settingsDir);
                }

                if (settingsJson["GameSettings"] == null)
                    settingsJson["GameSettings"] = new JObject();

                settingsJson["GameSettings"]["Java"] = folderName;
                File.WriteAllText(settingsFile, settingsJson.ToString(Newtonsoft.Json.Formatting.Indented));

                customJavaFolder = folderName;
                customJavaPath = fullPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存 Java 路径失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closeByBackButton)
            {
                Application.Exit();
            }
        }
    }
}