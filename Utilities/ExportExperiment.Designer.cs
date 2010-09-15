namespace IonMobility.Utilities
{
    partial class ExportExperiment
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
            this.btn_ExportExperimentOK = new System.Windows.Forms.Button();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.num_Step = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.num_Merge = new System.Windows.Forms.NumericUpDown();
            this.tb_Name = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tb_Directory = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.num_Step)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_Merge)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_ExportExperimentOK
            // 
            this.btn_ExportExperimentOK.BackColor = System.Drawing.Color.RoyalBlue;
            this.btn_ExportExperimentOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_ExportExperimentOK.ForeColor = System.Drawing.Color.White;
            this.btn_ExportExperimentOK.Location = new System.Drawing.Point(176, 212);
            this.btn_ExportExperimentOK.Name = "btn_ExportExperimentOK";
            this.btn_ExportExperimentOK.Size = new System.Drawing.Size(95, 35);
            this.btn_ExportExperimentOK.TabIndex = 4;
            this.btn_ExportExperimentOK.Text = "OK";
            this.btn_ExportExperimentOK.UseVisualStyleBackColor = false;
            this.btn_ExportExperimentOK.Click += new System.EventHandler(this.btn_ExportExperimentOK_Click);
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.BackColor = System.Drawing.Color.RoyalBlue;
            this.btn_Cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Cancel.ForeColor = System.Drawing.Color.White;
            this.btn_Cancel.Location = new System.Drawing.Point(68, 212);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(95, 35);
            this.btn_Cancel.TabIndex = 5;
            this.btn_Cancel.Text = "Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = false;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            // 
            // num_Step
            // 
            this.num_Step.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_Step.Location = new System.Drawing.Point(152, 124);
            this.num_Step.Name = "num_Step";
            this.num_Step.Size = new System.Drawing.Size(88, 24);
            this.num_Step.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(36, 128);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(110, 18);
            this.label1.TabIndex = 3;
            this.label1.Text = "Frames to Step";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(24, 156);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(122, 18);
            this.label2.TabIndex = 5;
            this.label2.Text = "Frames to Merge";
            // 
            // num_Merge
            // 
            this.num_Merge.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_Merge.Location = new System.Drawing.Point(152, 152);
            this.num_Merge.Name = "num_Merge";
            this.num_Merge.Size = new System.Drawing.Size(88, 24);
            this.num_Merge.TabIndex = 3;
            // 
            // tb_Name
            // 
            this.tb_Name.Location = new System.Drawing.Point(56, 88);
            this.tb_Name.Name = "tb_Name";
            this.tb_Name.Size = new System.Drawing.Size(224, 20);
            this.tb_Name.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 64);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(130, 18);
            this.label3.TabIndex = 7;
            this.label3.Text = "Experiment Name:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(12, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(116, 18);
            this.label4.TabIndex = 9;
            this.label4.Text = "Directory Name:";
            // 
            // tb_Directory
            // 
            this.tb_Directory.Location = new System.Drawing.Point(56, 36);
            this.tb_Directory.Name = "tb_Directory";
            this.tb_Directory.Size = new System.Drawing.Size(224, 20);
            this.tb_Directory.TabIndex = 0;
            // 
            // ExportExperiment
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tb_Directory);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tb_Name);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.num_Merge);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.num_Step);
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.btn_ExportExperimentOK);
            this.Name = "ExportExperiment";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Create Merged Experiment";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.num_Step)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_Merge)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button btn_ExportExperimentOK;
        private System.Windows.Forms.Button btn_Cancel;
        public System.Windows.Forms.NumericUpDown num_Step;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.NumericUpDown num_Merge;
        public System.Windows.Forms.TextBox tb_Name;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        public System.Windows.Forms.TextBox tb_Directory;
    }
}