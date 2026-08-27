namespace New_Launcher
{
    partial class LoadingForm
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
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 40);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(360, 23);
            this.progressBar.TabIndex = 0;
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelStatus.Location = new System.Drawing.Point(12, 15);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(96, 20);
            this.labelStatus.TabIndex = 1;
            this.labelStatus.Text = "正在启动……";
            // 
            // LoadingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 81);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoadingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "启动器加载中...";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelStatus;
    }
}