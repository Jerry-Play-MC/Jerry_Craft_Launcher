namespace New_Launcher
{
    partial class Settings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Settings));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.GameSettings = new System.Windows.Forms.TabPage();
            this.panel_GameSettings = new System.Windows.Forms.Panel();
            this.comboBox_Java = new System.Windows.Forms.ComboBox();
            this.label_ChooseJava = new System.Windows.Forms.Label();
            this.comboBox_GameSettings_VersionIsolated = new System.Windows.Forms.ComboBox();
            this.label_GameSettings_VersionIsolated = new System.Windows.Forms.Label();
            this.tabPage_Versions = new System.Windows.Forms.TabPage();
            this.comboBox_Versions = new System.Windows.Forms.ComboBox();
            this.label_Version = new System.Windows.Forms.Label();
            this.tabControl_Versions = new System.Windows.Forms.TabControl();
            this.tabPage_VersionsManage = new System.Windows.Forms.TabPage();
            this.panel_Versions_VersionsManage = new System.Windows.Forms.Panel();
            this.textBox_Versions_VerssionManager_NewName = new System.Windows.Forms.TextBox();
            this.label_Versions_VerssionManager_NewName = new System.Windows.Forms.Label();
            this.tabPage_SavesManager = new System.Windows.Forms.TabPage();
            this.button6 = new System.Windows.Forms.Button();
            this.grp_GameRules = new System.Windows.Forms.GroupBox();
            this.chk_WeatherCycle = new System.Windows.Forms.CheckBox();
            this.chk_FireTick = new System.Windows.Forms.CheckBox();
            this.chk_MobSpawning = new System.Windows.Forms.CheckBox();
            this.chk_DaylightCycle = new System.Windows.Forms.CheckBox();
            this.chk_KeepInventory = new System.Windows.Forms.CheckBox();
            this.flp_Buttons = new System.Windows.Forms.FlowLayoutPanel();
            this.btn_ApplyAll = new System.Windows.Forms.Button();
            this.btn_Resurrect = new System.Windows.Forms.Button();
            this.btn_ReloadFromSave = new System.Windows.Forms.Button();
            this.grp_Advanced = new System.Windows.Forms.GroupBox();
            this.chk_Initialized = new System.Windows.Forms.CheckBox();
            this.txt_Spawn = new System.Windows.Forms.TextBox();
            this.label_lblSpawn = new System.Windows.Forms.Label();
            this.txt_LevelName = new System.Windows.Forms.TextBox();
            this.label_lblLevelName = new System.Windows.Forms.Label();
            this.grp_Basic = new System.Windows.Forms.GroupBox();
            this.chk_DifficultyLocked = new System.Windows.Forms.CheckBox();
            this.cmb_Difficulty = new System.Windows.Forms.ComboBox();
            this.label_lblDifficulty = new System.Windows.Forms.Label();
            this.cmb_GameType = new System.Windows.Forms.ComboBox();
            this.label_lblGameType = new System.Windows.Forms.Label();
            this.chk_Hardcore = new System.Windows.Forms.CheckBox();
            this.chk_AllowCommands = new System.Windows.Forms.CheckBox();
            this.button_Fresh = new System.Windows.Forms.Button();
            this.comboBox_SavesList = new System.Windows.Forms.ComboBox();
            this.label_SavesManager = new System.Windows.Forms.Label();
            this.tabPage_OfficialLauncher = new System.Windows.Forms.TabPage();
            this.panel_OfficialLauncher = new System.Windows.Forms.Panel();
            this.button_InputOfficialLauncher = new System.Windows.Forms.Button();
            this.button_Save = new System.Windows.Forms.Button();
            this.button_NotSave = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.GameSettings.SuspendLayout();
            this.panel_GameSettings.SuspendLayout();
            this.tabPage_Versions.SuspendLayout();
            this.tabControl_Versions.SuspendLayout();
            this.tabPage_VersionsManage.SuspendLayout();
            this.panel_Versions_VersionsManage.SuspendLayout();
            this.tabPage_SavesManager.SuspendLayout();
            this.grp_GameRules.SuspendLayout();
            this.flp_Buttons.SuspendLayout();
            this.grp_Advanced.SuspendLayout();
            this.grp_Basic.SuspendLayout();
            this.tabPage_OfficialLauncher.SuspendLayout();
            this.panel_OfficialLauncher.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.GameSettings);
            this.tabControl.Controls.Add(this.tabPage_Versions);
            this.tabControl.Controls.Add(this.tabPage_OfficialLauncher);
            this.tabControl.Location = new System.Drawing.Point(1, 1);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(797, 393);
            this.tabControl.TabIndex = 0;
            // 
            // GameSettings
            // 
            this.GameSettings.Controls.Add(this.panel_GameSettings);
            this.GameSettings.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.GameSettings.Location = new System.Drawing.Point(4, 25);
            this.GameSettings.Name = "GameSettings";
            this.GameSettings.Padding = new System.Windows.Forms.Padding(3);
            this.GameSettings.Size = new System.Drawing.Size(789, 364);
            this.GameSettings.TabIndex = 0;
            this.GameSettings.Text = "游戏设置";
            this.GameSettings.UseVisualStyleBackColor = true;
            // 
            // panel_GameSettings
            // 
            this.panel_GameSettings.Controls.Add(this.comboBox_Java);
            this.panel_GameSettings.Controls.Add(this.label_ChooseJava);
            this.panel_GameSettings.Controls.Add(this.comboBox_GameSettings_VersionIsolated);
            this.panel_GameSettings.Controls.Add(this.label_GameSettings_VersionIsolated);
            this.panel_GameSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_GameSettings.Location = new System.Drawing.Point(3, 3);
            this.panel_GameSettings.Name = "panel_GameSettings";
            this.panel_GameSettings.Size = new System.Drawing.Size(783, 358);
            this.panel_GameSettings.TabIndex = 0;
            // 
            // comboBox_Java
            // 
            this.comboBox_Java.FormattingEnabled = true;
            this.comboBox_Java.Items.AddRange(new object[] {
            "隔离全部实例",
            "不隔离"});
            this.comboBox_Java.Location = new System.Drawing.Point(95, 40);
            this.comboBox_Java.Name = "comboBox_Java";
            this.comboBox_Java.Size = new System.Drawing.Size(685, 28);
            this.comboBox_Java.TabIndex = 3;
            this.comboBox_Java.SelectedIndexChanged += new System.EventHandler(this.comboBox_Java_SelectedIndexChanged);
            // 
            // label_ChooseJava
            // 
            this.label_ChooseJava.AutoSize = true;
            this.label_ChooseJava.Location = new System.Drawing.Point(4, 43);
            this.label_ChooseJava.Name = "label_ChooseJava";
            this.label_ChooseJava.Size = new System.Drawing.Size(84, 20);
            this.label_ChooseJava.TabIndex = 2;
            this.label_ChooseJava.Text = "Java选择：";
            // 
            // comboBox_GameSettings_VersionIsolated
            // 
            this.comboBox_GameSettings_VersionIsolated.FormattingEnabled = true;
            this.comboBox_GameSettings_VersionIsolated.Items.AddRange(new object[] {
            "隔离全部实例",
            "不隔离"});
            this.comboBox_GameSettings_VersionIsolated.Location = new System.Drawing.Point(95, 6);
            this.comboBox_GameSettings_VersionIsolated.Name = "comboBox_GameSettings_VersionIsolated";
            this.comboBox_GameSettings_VersionIsolated.Size = new System.Drawing.Size(685, 28);
            this.comboBox_GameSettings_VersionIsolated.TabIndex = 1;
            this.comboBox_GameSettings_VersionIsolated.SelectedIndexChanged += new System.EventHandler(this.comboBox_GameSettings_VersionIsolated_SelectedIndexChanged);
            // 
            // label_GameSettings_VersionIsolated
            // 
            this.label_GameSettings_VersionIsolated.AutoSize = true;
            this.label_GameSettings_VersionIsolated.Location = new System.Drawing.Point(5, 9);
            this.label_GameSettings_VersionIsolated.Name = "label_GameSettings_VersionIsolated";
            this.label_GameSettings_VersionIsolated.Size = new System.Drawing.Size(84, 20);
            this.label_GameSettings_VersionIsolated.TabIndex = 0;
            this.label_GameSettings_VersionIsolated.Text = "版本隔离：";
            // 
            // tabPage_Versions
            // 
            this.tabPage_Versions.Controls.Add(this.comboBox_Versions);
            this.tabPage_Versions.Controls.Add(this.label_Version);
            this.tabPage_Versions.Controls.Add(this.tabControl_Versions);
            this.tabPage_Versions.Location = new System.Drawing.Point(4, 25);
            this.tabPage_Versions.Name = "tabPage_Versions";
            this.tabPage_Versions.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_Versions.Size = new System.Drawing.Size(789, 364);
            this.tabPage_Versions.TabIndex = 2;
            this.tabPage_Versions.Text = "版本管理";
            this.tabPage_Versions.UseVisualStyleBackColor = true;
            // 
            // comboBox_Versions
            // 
            this.comboBox_Versions.FormattingEnabled = true;
            this.comboBox_Versions.Location = new System.Drawing.Point(97, 6);
            this.comboBox_Versions.Name = "comboBox_Versions";
            this.comboBox_Versions.Size = new System.Drawing.Size(686, 23);
            this.comboBox_Versions.TabIndex = 2;
            this.comboBox_Versions.SelectedIndexChanged += new System.EventHandler(this.comboBox_Versions_SelectedIndexChanged);
            // 
            // label_Version
            // 
            this.label_Version.AutoSize = true;
            this.label_Version.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_Version.Location = new System.Drawing.Point(7, 6);
            this.label_Version.Name = "label_Version";
            this.label_Version.Size = new System.Drawing.Size(84, 20);
            this.label_Version.TabIndex = 1;
            this.label_Version.Text = "选中版本：";
            // 
            // tabControl_Versions
            // 
            this.tabControl_Versions.Controls.Add(this.tabPage_VersionsManage);
            this.tabControl_Versions.Controls.Add(this.tabPage_SavesManager);
            this.tabControl_Versions.Location = new System.Drawing.Point(3, 35);
            this.tabControl_Versions.Name = "tabControl_Versions";
            this.tabControl_Versions.SelectedIndex = 0;
            this.tabControl_Versions.Size = new System.Drawing.Size(783, 323);
            this.tabControl_Versions.TabIndex = 0;
            // 
            // tabPage_VersionsManage
            // 
            this.tabPage_VersionsManage.Controls.Add(this.panel_Versions_VersionsManage);
            this.tabPage_VersionsManage.Location = new System.Drawing.Point(4, 25);
            this.tabPage_VersionsManage.Name = "tabPage_VersionsManage";
            this.tabPage_VersionsManage.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_VersionsManage.Size = new System.Drawing.Size(775, 294);
            this.tabPage_VersionsManage.TabIndex = 0;
            this.tabPage_VersionsManage.Text = "版本信息管理";
            this.tabPage_VersionsManage.UseVisualStyleBackColor = true;
            // 
            // panel_Versions_VersionsManage
            // 
            this.panel_Versions_VersionsManage.Controls.Add(this.textBox_Versions_VerssionManager_NewName);
            this.panel_Versions_VersionsManage.Controls.Add(this.label_Versions_VerssionManager_NewName);
            this.panel_Versions_VersionsManage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_Versions_VersionsManage.Location = new System.Drawing.Point(3, 3);
            this.panel_Versions_VersionsManage.Name = "panel_Versions_VersionsManage";
            this.panel_Versions_VersionsManage.Size = new System.Drawing.Size(769, 288);
            this.panel_Versions_VersionsManage.TabIndex = 0;
            // 
            // textBox_Versions_VerssionManager_NewName
            // 
            this.textBox_Versions_VerssionManager_NewName.Location = new System.Drawing.Point(78, 3);
            this.textBox_Versions_VerssionManager_NewName.Name = "textBox_Versions_VerssionManager_NewName";
            this.textBox_Versions_VerssionManager_NewName.Size = new System.Drawing.Size(688, 25);
            this.textBox_Versions_VerssionManager_NewName.TabIndex = 1;
            this.textBox_Versions_VerssionManager_NewName.TextChanged += new System.EventHandler(this.textBox_Versions_VerssionManager_NewName_TextChanged);
            // 
            // label_Versions_VerssionManager_NewName
            // 
            this.label_Versions_VerssionManager_NewName.AutoSize = true;
            this.label_Versions_VerssionManager_NewName.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_Versions_VerssionManager_NewName.Location = new System.Drawing.Point(3, 3);
            this.label_Versions_VerssionManager_NewName.Name = "label_Versions_VerssionManager_NewName";
            this.label_Versions_VerssionManager_NewName.Size = new System.Drawing.Size(69, 20);
            this.label_Versions_VerssionManager_NewName.TabIndex = 0;
            this.label_Versions_VerssionManager_NewName.Text = "重命名：";
            // 
            // tabPage_SavesManager
            // 
            this.tabPage_SavesManager.Controls.Add(this.button6);
            this.tabPage_SavesManager.Controls.Add(this.grp_GameRules);
            this.tabPage_SavesManager.Controls.Add(this.flp_Buttons);
            this.tabPage_SavesManager.Controls.Add(this.grp_Advanced);
            this.tabPage_SavesManager.Controls.Add(this.grp_Basic);
            this.tabPage_SavesManager.Controls.Add(this.button_Fresh);
            this.tabPage_SavesManager.Controls.Add(this.comboBox_SavesList);
            this.tabPage_SavesManager.Controls.Add(this.label_SavesManager);
            this.tabPage_SavesManager.Location = new System.Drawing.Point(4, 25);
            this.tabPage_SavesManager.Name = "tabPage_SavesManager";
            this.tabPage_SavesManager.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_SavesManager.Size = new System.Drawing.Size(775, 294);
            this.tabPage_SavesManager.TabIndex = 1;
            this.tabPage_SavesManager.Text = "存档管理";
            this.tabPage_SavesManager.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            this.button6.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button6.Location = new System.Drawing.Point(682, 8);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(90, 30);
            this.button6.TabIndex = 7;
            this.button6.Text = "额外选择";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // grp_GameRules
            // 
            this.grp_GameRules.Controls.Add(this.chk_WeatherCycle);
            this.grp_GameRules.Controls.Add(this.chk_FireTick);
            this.grp_GameRules.Controls.Add(this.chk_MobSpawning);
            this.grp_GameRules.Controls.Add(this.chk_DaylightCycle);
            this.grp_GameRules.Controls.Add(this.chk_KeepInventory);
            this.grp_GameRules.Location = new System.Drawing.Point(6, 206);
            this.grp_GameRules.Name = "grp_GameRules";
            this.grp_GameRules.Size = new System.Drawing.Size(440, 85);
            this.grp_GameRules.TabIndex = 5;
            this.grp_GameRules.TabStop = false;
            this.grp_GameRules.Text = "游戏规则";
            // 
            // chk_WeatherCycle
            // 
            this.chk_WeatherCycle.AutoSize = true;
            this.chk_WeatherCycle.Location = new System.Drawing.Point(215, 22);
            this.chk_WeatherCycle.Name = "chk_WeatherCycle";
            this.chk_WeatherCycle.Size = new System.Drawing.Size(89, 19);
            this.chk_WeatherCycle.TabIndex = 4;
            this.chk_WeatherCycle.Text = "天气变化";
            this.chk_WeatherCycle.UseVisualStyleBackColor = true;
            // 
            // chk_FireTick
            // 
            this.chk_FireTick.AutoSize = true;
            this.chk_FireTick.Location = new System.Drawing.Point(120, 48);
            this.chk_FireTick.Name = "chk_FireTick";
            this.chk_FireTick.Size = new System.Drawing.Size(89, 19);
            this.chk_FireTick.TabIndex = 3;
            this.chk_FireTick.Text = "火焰蔓延";
            this.chk_FireTick.UseVisualStyleBackColor = true;
            // 
            // chk_MobSpawning
            // 
            this.chk_MobSpawning.AutoSize = true;
            this.chk_MobSpawning.Location = new System.Drawing.Point(120, 22);
            this.chk_MobSpawning.Name = "chk_MobSpawning";
            this.chk_MobSpawning.Size = new System.Drawing.Size(89, 19);
            this.chk_MobSpawning.TabIndex = 2;
            this.chk_MobSpawning.Text = "生物生成";
            this.chk_MobSpawning.UseVisualStyleBackColor = true;
            // 
            // chk_DaylightCycle
            // 
            this.chk_DaylightCycle.AutoSize = true;
            this.chk_DaylightCycle.Location = new System.Drawing.Point(10, 48);
            this.chk_DaylightCycle.Name = "chk_DaylightCycle";
            this.chk_DaylightCycle.Size = new System.Drawing.Size(89, 19);
            this.chk_DaylightCycle.TabIndex = 1;
            this.chk_DaylightCycle.Text = "昼夜交替";
            this.chk_DaylightCycle.UseVisualStyleBackColor = true;
            // 
            // chk_KeepInventory
            // 
            this.chk_KeepInventory.AutoSize = true;
            this.chk_KeepInventory.Location = new System.Drawing.Point(10, 22);
            this.chk_KeepInventory.Name = "chk_KeepInventory";
            this.chk_KeepInventory.Size = new System.Drawing.Size(104, 19);
            this.chk_KeepInventory.TabIndex = 0;
            this.chk_KeepInventory.Text = "死亡不掉落";
            this.chk_KeepInventory.UseVisualStyleBackColor = true;
            // 
            // flp_Buttons
            // 
            this.flp_Buttons.Controls.Add(this.btn_ApplyAll);
            this.flp_Buttons.Controls.Add(this.btn_Resurrect);
            this.flp_Buttons.Controls.Add(this.btn_ReloadFromSave);
            this.flp_Buttons.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flp_Buttons.Location = new System.Drawing.Point(452, 206);
            this.flp_Buttons.Name = "flp_Buttons";
            this.flp_Buttons.Size = new System.Drawing.Size(317, 85);
            this.flp_Buttons.TabIndex = 6;
            // 
            // btn_ApplyAll
            // 
            this.btn_ApplyAll.Location = new System.Drawing.Point(3, 3);
            this.btn_ApplyAll.Name = "btn_ApplyAll";
            this.btn_ApplyAll.Size = new System.Drawing.Size(150, 30);
            this.btn_ApplyAll.TabIndex = 0;
            this.btn_ApplyAll.Text = "✅ 应用全部修改";
            this.btn_ApplyAll.UseVisualStyleBackColor = true;
            // 
            // btn_Resurrect
            // 
            this.btn_Resurrect.BackColor = System.Drawing.Color.LightCoral;
            this.btn_Resurrect.Location = new System.Drawing.Point(3, 39);
            this.btn_Resurrect.Name = "btn_Resurrect";
            this.btn_Resurrect.Size = new System.Drawing.Size(150, 30);
            this.btn_Resurrect.TabIndex = 1;
            this.btn_Resurrect.Text = "💀 极限模式救急";
            this.btn_Resurrect.UseVisualStyleBackColor = false;
            // 
            // btn_ReloadFromSave
            // 
            this.btn_ReloadFromSave.Location = new System.Drawing.Point(159, 3);
            this.btn_ReloadFromSave.Name = "btn_ReloadFromSave";
            this.btn_ReloadFromSave.Size = new System.Drawing.Size(150, 30);
            this.btn_ReloadFromSave.TabIndex = 2;
            this.btn_ReloadFromSave.Text = "🔄 从存档重载";
            this.btn_ReloadFromSave.UseVisualStyleBackColor = true;
            // 
            // grp_Advanced
            // 
            this.grp_Advanced.Controls.Add(this.chk_Initialized);
            this.grp_Advanced.Controls.Add(this.txt_Spawn);
            this.grp_Advanced.Controls.Add(this.label_lblSpawn);
            this.grp_Advanced.Controls.Add(this.txt_LevelName);
            this.grp_Advanced.Controls.Add(this.label_lblLevelName);
            this.grp_Advanced.Location = new System.Drawing.Point(382, 45);
            this.grp_Advanced.Name = "grp_Advanced";
            this.grp_Advanced.Size = new System.Drawing.Size(387, 155);
            this.grp_Advanced.TabIndex = 4;
            this.grp_Advanced.TabStop = false;
            this.grp_Advanced.Text = "高级设置";
            // 
            // chk_Initialized
            // 
            this.chk_Initialized.AutoSize = true;
            this.chk_Initialized.Location = new System.Drawing.Point(10, 80);
            this.chk_Initialized.Name = "chk_Initialized";
            this.chk_Initialized.Size = new System.Drawing.Size(201, 19);
            this.chk_Initialized.TabIndex = 4;
            this.chk_Initialized.Text = "已初始化 (Initialized)";
            this.chk_Initialized.UseVisualStyleBackColor = true;
            // 
            // txt_Spawn
            // 
            this.txt_Spawn.Location = new System.Drawing.Point(140, 51);
            this.txt_Spawn.Name = "txt_Spawn";
            this.txt_Spawn.Size = new System.Drawing.Size(230, 25);
            this.txt_Spawn.TabIndex = 3;
            // 
            // label_lblSpawn
            // 
            this.label_lblSpawn.AutoSize = true;
            this.label_lblSpawn.Location = new System.Drawing.Point(10, 54);
            this.label_lblSpawn.Name = "label_lblSpawn";
            this.label_lblSpawn.Size = new System.Drawing.Size(163, 15);
            this.label_lblSpawn.TabIndex = 2;
            this.label_lblSpawn.Text = "出生点 (x,y,z,dim)：";
            // 
            // txt_LevelName
            // 
            this.txt_LevelName.Location = new System.Drawing.Point(90, 22);
            this.txt_LevelName.Name = "txt_LevelName";
            this.txt_LevelName.Size = new System.Drawing.Size(180, 25);
            this.txt_LevelName.TabIndex = 1;
            // 
            // label_lblLevelName
            // 
            this.label_lblLevelName.AutoSize = true;
            this.label_lblLevelName.Location = new System.Drawing.Point(10, 25);
            this.label_lblLevelName.Name = "label_lblLevelName";
            this.label_lblLevelName.Size = new System.Drawing.Size(82, 15);
            this.label_lblLevelName.TabIndex = 0;
            this.label_lblLevelName.Text = "存档名称：";
            // 
            // grp_Basic
            // 
            this.grp_Basic.Controls.Add(this.chk_DifficultyLocked);
            this.grp_Basic.Controls.Add(this.cmb_Difficulty);
            this.grp_Basic.Controls.Add(this.label_lblDifficulty);
            this.grp_Basic.Controls.Add(this.cmb_GameType);
            this.grp_Basic.Controls.Add(this.label_lblGameType);
            this.grp_Basic.Controls.Add(this.chk_Hardcore);
            this.grp_Basic.Controls.Add(this.chk_AllowCommands);
            this.grp_Basic.Location = new System.Drawing.Point(6, 45);
            this.grp_Basic.Name = "grp_Basic";
            this.grp_Basic.Size = new System.Drawing.Size(370, 155);
            this.grp_Basic.TabIndex = 3;
            this.grp_Basic.TabStop = false;
            this.grp_Basic.Text = "基本设置";
            // 
            // chk_DifficultyLocked
            // 
            this.chk_DifficultyLocked.AutoSize = true;
            this.chk_DifficultyLocked.Location = new System.Drawing.Point(10, 130);
            this.chk_DifficultyLocked.Name = "chk_DifficultyLocked";
            this.chk_DifficultyLocked.Size = new System.Drawing.Size(241, 19);
            this.chk_DifficultyLocked.TabIndex = 6;
            this.chk_DifficultyLocked.Text = "锁定难度 (DifficultyLocked)";
            this.chk_DifficultyLocked.UseVisualStyleBackColor = true;
            // 
            // cmb_Difficulty
            // 
            this.cmb_Difficulty.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_Difficulty.Items.AddRange(new object[] {
            "和平 (peaceful)",
            "简单 (easy)",
            "普通 (normal)",
            "困难 (hard)"});
            this.cmb_Difficulty.Location = new System.Drawing.Point(100, 103);
            this.cmb_Difficulty.Name = "cmb_Difficulty";
            this.cmb_Difficulty.Size = new System.Drawing.Size(120, 23);
            this.cmb_Difficulty.TabIndex = 5;
            // 
            // label_lblDifficulty
            // 
            this.label_lblDifficulty.AutoSize = true;
            this.label_lblDifficulty.Location = new System.Drawing.Point(10, 107);
            this.label_lblDifficulty.Name = "label_lblDifficulty";
            this.label_lblDifficulty.Size = new System.Drawing.Size(52, 15);
            this.label_lblDifficulty.TabIndex = 4;
            this.label_lblDifficulty.Text = "难度：";
            // 
            // cmb_GameType
            // 
            this.cmb_GameType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_GameType.Items.AddRange(new object[] {
            "生存 (0)",
            "创造 (1)",
            "冒险 (2)",
            "旁观 (3)"});
            this.cmb_GameType.Location = new System.Drawing.Point(100, 74);
            this.cmb_GameType.Name = "cmb_GameType";
            this.cmb_GameType.Size = new System.Drawing.Size(120, 23);
            this.cmb_GameType.TabIndex = 3;
            // 
            // label_lblGameType
            // 
            this.label_lblGameType.AutoSize = true;
            this.label_lblGameType.Location = new System.Drawing.Point(10, 78);
            this.label_lblGameType.Name = "label_lblGameType";
            this.label_lblGameType.Size = new System.Drawing.Size(82, 15);
            this.label_lblGameType.TabIndex = 2;
            this.label_lblGameType.Text = "游戏模式：";
            // 
            // chk_Hardcore
            // 
            this.chk_Hardcore.AutoSize = true;
            this.chk_Hardcore.Location = new System.Drawing.Point(10, 48);
            this.chk_Hardcore.Name = "chk_Hardcore";
            this.chk_Hardcore.Size = new System.Drawing.Size(177, 19);
            this.chk_Hardcore.TabIndex = 1;
            this.chk_Hardcore.Text = "极限模式 (Hardcore)";
            this.chk_Hardcore.UseVisualStyleBackColor = true;
            // 
            // chk_AllowCommands
            // 
            this.chk_AllowCommands.AutoSize = true;
            this.chk_AllowCommands.Location = new System.Drawing.Point(10, 22);
            this.chk_AllowCommands.Name = "chk_AllowCommands";
            this.chk_AllowCommands.Size = new System.Drawing.Size(217, 19);
            this.chk_AllowCommands.TabIndex = 0;
            this.chk_AllowCommands.Text = "允许作弊 (AllowCommands)";
            this.chk_AllowCommands.UseVisualStyleBackColor = true;
            // 
            // button_Fresh
            // 
            this.button_Fresh.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_Fresh.Location = new System.Drawing.Point(586, 8);
            this.button_Fresh.Name = "button_Fresh";
            this.button_Fresh.Size = new System.Drawing.Size(90, 30);
            this.button_Fresh.TabIndex = 2;
            this.button_Fresh.Text = "刷新列表";
            this.button_Fresh.UseVisualStyleBackColor = true;
            // 
            // comboBox_SavesList
            // 
            this.comboBox_SavesList.FormattingEnabled = true;
            this.comboBox_SavesList.Location = new System.Drawing.Point(66, 13);
            this.comboBox_SavesList.Name = "comboBox_SavesList";
            this.comboBox_SavesList.Size = new System.Drawing.Size(514, 23);
            this.comboBox_SavesList.TabIndex = 1;
            // 
            // label_SavesManager
            // 
            this.label_SavesManager.AutoSize = true;
            this.label_SavesManager.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_SavesManager.Location = new System.Drawing.Point(6, 15);
            this.label_SavesManager.Name = "label_SavesManager";
            this.label_SavesManager.Size = new System.Drawing.Size(54, 20);
            this.label_SavesManager.TabIndex = 0;
            this.label_SavesManager.Text = "存档：";
            // 
            // tabPage_OfficialLauncher
            // 
            this.tabPage_OfficialLauncher.Controls.Add(this.panel_OfficialLauncher);
            this.tabPage_OfficialLauncher.Location = new System.Drawing.Point(4, 25);
            this.tabPage_OfficialLauncher.Name = "tabPage_OfficialLauncher";
            this.tabPage_OfficialLauncher.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_OfficialLauncher.Size = new System.Drawing.Size(789, 364);
            this.tabPage_OfficialLauncher.TabIndex = 1;
            this.tabPage_OfficialLauncher.Text = "官方启动器";
            this.tabPage_OfficialLauncher.UseVisualStyleBackColor = true;
            // 
            // panel_OfficialLauncher
            // 
            this.panel_OfficialLauncher.Controls.Add(this.button_InputOfficialLauncher);
            this.panel_OfficialLauncher.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_OfficialLauncher.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.panel_OfficialLauncher.Location = new System.Drawing.Point(3, 3);
            this.panel_OfficialLauncher.Name = "panel_OfficialLauncher";
            this.panel_OfficialLauncher.Size = new System.Drawing.Size(783, 358);
            this.panel_OfficialLauncher.TabIndex = 0;
            // 
            // button_InputOfficialLauncher
            // 
            this.button_InputOfficialLauncher.Location = new System.Drawing.Point(4, 3);
            this.button_InputOfficialLauncher.Name = "button_InputOfficialLauncher";
            this.button_InputOfficialLauncher.Size = new System.Drawing.Size(776, 51);
            this.button_InputOfficialLauncher.TabIndex = 0;
            this.button_InputOfficialLauncher.Text = "将此版本文件夹导入官方启动器（导入期间请勿运行官方启动器！！！）";
            this.button_InputOfficialLauncher.UseVisualStyleBackColor = true;
            this.button_InputOfficialLauncher.Click += new System.EventHandler(this.button_InputOfficialLauncher_Click);
            // 
            // button_Save
            // 
            this.button_Save.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_Save.Location = new System.Drawing.Point(690, 400);
            this.button_Save.Name = "button_Save";
            this.button_Save.Size = new System.Drawing.Size(98, 38);
            this.button_Save.TabIndex = 1;
            this.button_Save.Text = "保存";
            this.button_Save.UseVisualStyleBackColor = true;
            this.button_Save.Click += new System.EventHandler(this.button_Save_Click);
            // 
            // button_NotSave
            // 
            this.button_NotSave.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_NotSave.Location = new System.Drawing.Point(586, 400);
            this.button_NotSave.Name = "button_NotSave";
            this.button_NotSave.Size = new System.Drawing.Size(98, 38);
            this.button_NotSave.TabIndex = 2;
            this.button_NotSave.Text = "不保存";
            this.button_NotSave.UseVisualStyleBackColor = true;
            this.button_NotSave.Click += new System.EventHandler(this.button_NotSave_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(150, 30);
            this.button1.TabIndex = 0;
            this.button1.Text = "✅ 应用全部修改";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(452, 206);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(317, 85);
            this.flowLayoutPanel1.TabIndex = 6;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(3, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(150, 30);
            this.button2.TabIndex = 0;
            this.button2.Text = "✅ 应用全部修改";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(3, 39);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(150, 30);
            this.button3.TabIndex = 1;
            this.button3.Text = "💀 极限模式救急";
            this.button3.UseVisualStyleBackColor = false;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(159, 3);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(150, 30);
            this.button4.TabIndex = 2;
            this.button4.Text = "🔄 从存档重载";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(3, 3);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(150, 30);
            this.button5.TabIndex = 0;
            this.button5.Text = "✅ 应用全部修改";
            this.button5.UseVisualStyleBackColor = true;
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button_NotSave);
            this.Controls.Add(this.button_Save);
            this.Controls.Add(this.tabControl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Settings";
            this.Text = "设置";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Settings_FormClosing);
            this.Load += new System.EventHandler(this.Settings_Load);
            this.tabControl.ResumeLayout(false);
            this.GameSettings.ResumeLayout(false);
            this.panel_GameSettings.ResumeLayout(false);
            this.panel_GameSettings.PerformLayout();
            this.tabPage_Versions.ResumeLayout(false);
            this.tabPage_Versions.PerformLayout();
            this.tabControl_Versions.ResumeLayout(false);
            this.tabPage_VersionsManage.ResumeLayout(false);
            this.panel_Versions_VersionsManage.ResumeLayout(false);
            this.panel_Versions_VersionsManage.PerformLayout();
            this.tabPage_SavesManager.ResumeLayout(false);
            this.tabPage_SavesManager.PerformLayout();
            this.grp_GameRules.ResumeLayout(false);
            this.grp_GameRules.PerformLayout();
            this.flp_Buttons.ResumeLayout(false);
            this.grp_Advanced.ResumeLayout(false);
            this.grp_Advanced.PerformLayout();
            this.grp_Basic.ResumeLayout(false);
            this.grp_Basic.PerformLayout();
            this.tabPage_OfficialLauncher.ResumeLayout(false);
            this.panel_OfficialLauncher.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        // 已声明所有控件（设计器会自动生成字段，但为了安全，我们显式声明）
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage GameSettings;
        private System.Windows.Forms.Panel panel_GameSettings;
        private System.Windows.Forms.ComboBox comboBox_GameSettings_VersionIsolated;
        private System.Windows.Forms.Label label_GameSettings_VersionIsolated;
        private System.Windows.Forms.Button button_Save;
        private System.Windows.Forms.Button button_NotSave;
        private System.Windows.Forms.TabPage tabPage_OfficialLauncher;
        private System.Windows.Forms.Panel panel_OfficialLauncher;
        private System.Windows.Forms.Button button_InputOfficialLauncher;
        private System.Windows.Forms.TabPage tabPage_Versions;
        private System.Windows.Forms.TabControl tabControl_Versions;
        private System.Windows.Forms.TabPage tabPage_VersionsManage;
        private System.Windows.Forms.ComboBox comboBox_Versions;
        private System.Windows.Forms.Label label_Version;
        private System.Windows.Forms.Panel panel_Versions_VersionsManage;
        private System.Windows.Forms.Label label_Versions_VerssionManager_NewName;
        private System.Windows.Forms.TextBox textBox_Versions_VerssionManager_NewName;
        private System.Windows.Forms.TabPage tabPage_SavesManager;
        private System.Windows.Forms.ComboBox comboBox_SavesList;
        private System.Windows.Forms.Label label_SavesManager;
        private System.Windows.Forms.Button button_Fresh;

        // 新增控件字段（设计器已生成，但为防止遗漏，显式列出）
        private System.Windows.Forms.GroupBox grp_Basic;
        private System.Windows.Forms.GroupBox grp_Advanced;
        private System.Windows.Forms.GroupBox grp_GameRules;
        private System.Windows.Forms.FlowLayoutPanel flp_Buttons;

        private System.Windows.Forms.CheckBox chk_AllowCommands;
        private System.Windows.Forms.CheckBox chk_Hardcore;
        private System.Windows.Forms.ComboBox cmb_GameType;
        private System.Windows.Forms.ComboBox cmb_Difficulty;
        private System.Windows.Forms.CheckBox chk_DifficultyLocked;

        private System.Windows.Forms.TextBox txt_LevelName;
        private System.Windows.Forms.TextBox txt_Spawn;
        private System.Windows.Forms.CheckBox chk_Initialized;

        private System.Windows.Forms.CheckBox chk_KeepInventory;
        private System.Windows.Forms.CheckBox chk_DaylightCycle;
        private System.Windows.Forms.CheckBox chk_MobSpawning;
        private System.Windows.Forms.CheckBox chk_FireTick;
        private System.Windows.Forms.CheckBox chk_WeatherCycle;

        private System.Windows.Forms.Button btn_ApplyAll;
        private System.Windows.Forms.Button btn_Resurrect;
        private System.Windows.Forms.Button btn_ReloadFromSave;

        // 辅助标签（设计器生成的，但为了安全，我们也显式声明）
        private System.Windows.Forms.Label label_lblGameType;
        private System.Windows.Forms.Label label_lblDifficulty;
        private System.Windows.Forms.Label label_lblLevelName;
        private System.Windows.Forms.Label label_lblSpawn;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.ComboBox comboBox_Java;
        private System.Windows.Forms.Label label_ChooseJava;
    }
}