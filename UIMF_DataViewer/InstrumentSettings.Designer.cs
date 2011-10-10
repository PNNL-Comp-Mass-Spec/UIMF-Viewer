namespace UIMF_DataViewer
{
    partial class InstrumentSettings
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pb_Entrance = new System.Windows.Forms.PictureBox();
            this.pb_Tube = new System.Windows.Forms.PictureBox();
            this.pb_Exit = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Entrance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Tube)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Exit)).BeginInit();
            this.SuspendLayout();
            // 
            // pb_Entrance
            // 
            this.pb_Entrance.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pb_Entrance.Image = global::UIMF_DataViewer.Properties.Resources.Entrance_IMS41;
            this.pb_Entrance.Location = new System.Drawing.Point(56, 40);
            this.pb_Entrance.Margin = new System.Windows.Forms.Padding(4);
            this.pb_Entrance.Name = "pb_Entrance";
            this.pb_Entrance.Size = new System.Drawing.Size(251, 157);
            this.pb_Entrance.TabIndex = 0;
            this.pb_Entrance.TabStop = false;
            // 
            // pb_Tube
            // 
            this.pb_Tube.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pb_Tube.Image = global::UIMF_DataViewer.Properties.Resources.IMS4a;
            this.pb_Tube.Location = new System.Drawing.Point(16, 248);
            this.pb_Tube.Margin = new System.Windows.Forms.Padding(4);
            this.pb_Tube.Name = "pb_Tube";
            this.pb_Tube.Size = new System.Drawing.Size(668, 157);
            this.pb_Tube.TabIndex = 3;
            this.pb_Tube.TabStop = false;
            // 
            // pb_Exit
            // 
            this.pb_Exit.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pb_Exit.Image = global::UIMF_DataViewer.Properties.Resources.Exit_IMS4;
            this.pb_Exit.Location = new System.Drawing.Point(364, 32);
            this.pb_Exit.Margin = new System.Windows.Forms.Padding(4);
            this.pb_Exit.Name = "pb_Exit";
            this.pb_Exit.Size = new System.Drawing.Size(212, 168);
            this.pb_Exit.TabIndex = 5;
            this.pb_Exit.TabStop = false;
            // 
            // InstrumentSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Silver;
            this.Controls.Add(this.pb_Exit);
            this.Controls.Add(this.pb_Tube);
            this.Controls.Add(this.pb_Entrance);
            this.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.Black;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "InstrumentSettings";
            this.Size = new System.Drawing.Size(697, 596);
            ((System.ComponentModel.ISupportInitialize)(this.pb_Entrance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Tube)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Exit)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pb_Entrance;
        private System.Windows.Forms.PictureBox pb_Tube;
        private System.Windows.Forms.PictureBox pb_Exit;
    }
}
