namespace New_Launcher.Roles
{
    partial class ManageRoles
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

        private void InitializeComponent()
        {
            this.listViewRoles = new System.Windows.Forms.ListView();
            this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderDefault = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnSetDefault = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listViewRoles
            // 
            this.listViewRoles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderType,
            this.columnHeaderDefault});
            this.listViewRoles.FullRowSelect = true;
            this.listViewRoles.HideSelection = false;
            this.listViewRoles.Location = new System.Drawing.Point(12, 12);
            this.listViewRoles.Name = "listViewRoles";
            this.listViewRoles.Size = new System.Drawing.Size(670, 426);
            this.listViewRoles.TabIndex = 0;
            this.listViewRoles.UseCompatibleStateImageBehavior = false;
            this.listViewRoles.View = System.Windows.Forms.View.Details;
            this.listViewRoles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listViewRoles_MouseDoubleClick);
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "角色名";
            this.columnHeaderName.Width = 120;
            // 
            // columnHeaderType
            // 
            this.columnHeaderType.Text = "类型";
            this.columnHeaderType.Width = 100;
            // 
            // columnHeaderDefault
            // 
            this.columnHeaderDefault.Text = "默认";
            this.columnHeaderDefault.Width = 80;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(688, 300);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(100, 30);
            this.btnAdd.TabIndex = 1;
            this.btnAdd.Text = "添加角色";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(688, 336);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(100, 30);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "删除角色";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnSetDefault
            // 
            this.btnSetDefault.Location = new System.Drawing.Point(688, 372);
            this.btnSetDefault.Name = "btnSetDefault";
            this.btnSetDefault.Size = new System.Drawing.Size(100, 30);
            this.btnSetDefault.TabIndex = 3;
            this.btnSetDefault.Text = "设为默认";
            this.btnSetDefault.UseVisualStyleBackColor = true;
            this.btnSetDefault.Click += new System.EventHandler(this.btnSetDefault_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(688, 408);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 30);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ManageRoles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnSetDefault);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.listViewRoles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManageRoles";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "角色管理";
            this.Load += new System.EventHandler(this.ManageRoles_Load);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.ListView listViewRoles;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderType;
        private System.Windows.Forms.ColumnHeader columnHeaderDefault;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnSetDefault;
        private System.Windows.Forms.Button btnClose;
    }
}