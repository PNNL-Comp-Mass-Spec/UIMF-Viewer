namespace DataIO
{
    partial class ctlMessageViewer
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtb = new System.Windows.Forms.RichTextBox();
            this.pnlMessagTypes = new System.Windows.Forms.Panel();
            this.lblMessageCount = new System.Windows.Forms.Label();
            this.btnClear = new System.Windows.Forms.Button();
            this.pnlMessagTypes.SuspendLayout();
            this.SuspendLayout();
            // 
            // rtb
            // 
            this.rtb.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.rtb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtb.Location = new System.Drawing.Point(0, 19);
            this.rtb.Name = "rtb";
            this.rtb.Size = new System.Drawing.Size(241, 113);
            this.rtb.TabIndex = 22;
            this.rtb.Text = "";
            // 
            // pnlMessagTypes
            // 
            this.pnlMessagTypes.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnlMessagTypes.Controls.Add(this.lblMessageCount);
            this.pnlMessagTypes.Controls.Add(this.btnClear);
            this.pnlMessagTypes.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlMessagTypes.Location = new System.Drawing.Point(0, 0);
            this.pnlMessagTypes.Name = "pnlMessagTypes";
            this.pnlMessagTypes.Size = new System.Drawing.Size(241, 19);
            this.pnlMessagTypes.TabIndex = 23;
            // 
            // lblMessageCount
            // 
            this.lblMessageCount.AutoSize = true;
            this.lblMessageCount.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblMessageCount.Location = new System.Drawing.Point(185, 0);
            this.lblMessageCount.Name = "lblMessageCount";
            this.lblMessageCount.Size = new System.Drawing.Size(13, 13);
            this.lblMessageCount.TabIndex = 1;
            this.lblMessageCount.Text = "0";
            // 
            // btnClear
            // 
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnClear.Font = new System.Drawing.Font("Times New Roman", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClear.Location = new System.Drawing.Point(198, 0);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(39, 15);
            this.btnClear.TabIndex = 25;
            this.btnClear.Text = "Clear";
            this.btnClear.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // ctlMessageViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.rtb);
            this.Controls.Add(this.pnlMessagTypes);
            this.Name = "ctlMessageViewer";
            this.Size = new System.Drawing.Size(241, 132);
            this.pnlMessagTypes.ResumeLayout(false);
            this.pnlMessagTypes.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtb;
        private System.Windows.Forms.Panel pnlMessagTypes;
        private System.Windows.Forms.Label lblMessageCount;
        private System.Windows.Forms.Button btnClear;
    }
}
