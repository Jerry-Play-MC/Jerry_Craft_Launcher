namespace New_Launcher
{
    partial class ModCardControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.picIcon = new System.Windows.Forms.PictureBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.lblDownloads = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // picIcon
            // 
            this.picIcon.Location = new System.Drawing.Point(4, 3);
            this.picIcon.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.picIcon.Name = "picIcon";
            this.picIcon.Size = new System.Drawing.Size(85, 74);
            this.picIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picIcon.TabIndex = 0;
            this.picIcon.TabStop = false;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(97, 3);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(0, 19);
            this.lblTitle.TabIndex = 1;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Font = new System.Drawing.Font("微软雅黑", 8F);
            this.lblDescription.Location = new System.Drawing.Point(97, 30);
            this.lblDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(0, 20);
            this.lblDescription.TabIndex = 2;
            // 
            // lblDownloads
            // 
            this.lblDownloads.AutoSize = true;
            this.lblDownloads.Font = new System.Drawing.Font("微软雅黑", 7F);
            this.lblDownloads.ForeColor = System.Drawing.Color.Gray;
            this.lblDownloads.Location = new System.Drawing.Point(97, 53);
            this.lblDownloads.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDownloads.Name = "lblDownloads";
            this.lblDownloads.Size = new System.Drawing.Size(0, 17);
            this.lblDownloads.TabIndex = 3;
            // 
            // ModCardControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblDownloads);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.picIcon);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "ModCardControl";
            this.Size = new System.Drawing.Size(1040, 81);
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.PictureBox picIcon;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblDownloads;
    }
}