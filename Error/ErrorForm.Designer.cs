namespace New_Launcher.Error
{
    partial class ErrorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorForm));
            this.label_Error = new System.Windows.Forms.Label();
            this.richTextBox_ErrorText = new System.Windows.Forms.RichTextBox();
            this.button_OutputErrorLog = new System.Windows.Forms.Button();
            this.button_Exit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label_Error
            // 
            this.label_Error.AutoSize = true;
            this.label_Error.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_Error.Location = new System.Drawing.Point(12, 9);
            this.label_Error.Name = "label_Error";
            this.label_Error.Size = new System.Drawing.Size(639, 40);
            this.label_Error.TabIndex = 0;
            this.label_Error.Text = "遇到了一些错误，详情见下。\r\n如果你需要转发给别人，请不要仅仅为这个页面截图，而是将导出的错误报告完整发送给他人。";
            // 
            // richTextBox_ErrorText
            // 
            this.richTextBox_ErrorText.Location = new System.Drawing.Point(12, 52);
            this.richTextBox_ErrorText.Name = "richTextBox_ErrorText";
            this.richTextBox_ErrorText.ReadOnly = true;
            this.richTextBox_ErrorText.Size = new System.Drawing.Size(776, 347);
            this.richTextBox_ErrorText.TabIndex = 1;
            this.richTextBox_ErrorText.Text = "";
            this.richTextBox_ErrorText.TextChanged += new System.EventHandler(this.richTextBox_ErrorText_TextChanged);
            // 
            // button_OutputErrorLog
            // 
            this.button_OutputErrorLog.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_OutputErrorLog.Location = new System.Drawing.Point(568, 405);
            this.button_OutputErrorLog.Name = "button_OutputErrorLog";
            this.button_OutputErrorLog.Size = new System.Drawing.Size(220, 33);
            this.button_OutputErrorLog.TabIndex = 2;
            this.button_OutputErrorLog.Text = "导出错误报告";
            this.button_OutputErrorLog.UseVisualStyleBackColor = true;
            this.button_OutputErrorLog.Click += new System.EventHandler(this.button_OutputErrorLog_Click);
            // 
            // button_Exit
            // 
            this.button_Exit.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_Exit.Location = new System.Drawing.Point(446, 405);
            this.button_Exit.Name = "button_Exit";
            this.button_Exit.Size = new System.Drawing.Size(116, 33);
            this.button_Exit.TabIndex = 3;
            this.button_Exit.Text = "退出";
            this.button_Exit.UseVisualStyleBackColor = true;
            this.button_Exit.Click += new System.EventHandler(this.button_Exit_Click);
            // 
            // ErrorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button_Exit);
            this.Controls.Add(this.button_OutputErrorLog);
            this.Controls.Add(this.richTextBox_ErrorText);
            this.Controls.Add(this.label_Error);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ErrorForm";
            this.Text = "遇到错误";
            this.Load += new System.EventHandler(this.ErrorForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Error;
        private System.Windows.Forms.RichTextBox richTextBox_ErrorText;
        private System.Windows.Forms.Button button_OutputErrorLog;
        private System.Windows.Forms.Button button_Exit;
    }
}