namespace New_Launcher
{
    partial class CreateRole
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateRole));
            this.label_CreateARole = new System.Windows.Forms.Label();
            this.label_RoleName = new System.Windows.Forms.Label();
            this.textBox_RoleName = new System.Windows.Forms.TextBox();
            this.button_Save = new System.Windows.Forms.Button();
            this.button_Skip = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label_CreateARole
            // 
            this.label_CreateARole.AutoSize = true;
            this.label_CreateARole.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_CreateARole.Location = new System.Drawing.Point(13, 13);
            this.label_CreateARole.Name = "label_CreateARole";
            this.label_CreateARole.Size = new System.Drawing.Size(114, 20);
            this.label_CreateARole.TabIndex = 0;
            this.label_CreateARole.Text = "创建一个角色：";
            // 
            // label_RoleName
            // 
            this.label_RoleName.AutoSize = true;
            this.label_RoleName.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_RoleName.Location = new System.Drawing.Point(13, 33);
            this.label_RoleName.Name = "label_RoleName";
            this.label_RoleName.Size = new System.Drawing.Size(69, 20);
            this.label_RoleName.TabIndex = 1;
            this.label_RoleName.Text = "角色名：";
            // 
            // textBox_RoleName
            // 
            this.textBox_RoleName.Location = new System.Drawing.Point(88, 33);
            this.textBox_RoleName.Name = "textBox_RoleName";
            this.textBox_RoleName.Size = new System.Drawing.Size(700, 25);
            this.textBox_RoleName.TabIndex = 2;
            // 
            // button_Save
            // 
            this.button_Save.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_Save.Location = new System.Drawing.Point(701, 400);
            this.button_Save.Name = "button_Save";
            this.button_Save.Size = new System.Drawing.Size(87, 38);
            this.button_Save.TabIndex = 3;
            this.button_Save.Text = "保存";
            this.button_Save.UseVisualStyleBackColor = true;
            this.button_Save.Click += new System.EventHandler(this.button_Save_Click);
            // 
            // button_Skip
            // 
            this.button_Skip.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_Skip.Location = new System.Drawing.Point(608, 400);
            this.button_Skip.Name = "button_Skip";
            this.button_Skip.Size = new System.Drawing.Size(87, 38);
            this.button_Skip.TabIndex = 4;
            this.button_Skip.Text = "取消";
            this.button_Skip.UseVisualStyleBackColor = true;
            this.button_Skip.Click += new System.EventHandler(this.button_Skip_Click);
            // 
            // CreateRole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button_Skip);
            this.Controls.Add(this.button_Save);
            this.Controls.Add(this.textBox_RoleName);
            this.Controls.Add(this.label_RoleName);
            this.Controls.Add(this.label_CreateARole);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CreateRole";
            this.Text = "创建角色";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CreateRole_FormClosing);
            this.Load += new System.EventHandler(this.CreateRole_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_CreateARole;
        private System.Windows.Forms.Label label_RoleName;
        private System.Windows.Forms.TextBox textBox_RoleName;
        private System.Windows.Forms.Button button_Save;
        private System.Windows.Forms.Button button_Skip;
    }
}