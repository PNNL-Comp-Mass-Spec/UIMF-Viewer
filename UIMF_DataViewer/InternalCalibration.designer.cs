namespace UIMF_File
{
    partial class InternalCalibration
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btn_Calibrate = new System.Windows.Forms.Button();
            this.dg_Calibrants = new System.Windows.Forms.DataGridView();
            this.col0_Enable = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.col1_Calibrant = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col2_MZ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col3_Charge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col4_TOF = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col5_Bins = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col7_ErrorPPM = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col8_MZExp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col9_TOFExperimental = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tb_Slope = new System.Windows.Forms.TextBox();
            this.tb_Intercept = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.num_NoiseLevel = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.cb_NoiseLevel = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dg_Calibrants)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_NoiseLevel)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Calibrate
            // 
            this.btn_Calibrate.BackColor = System.Drawing.Color.PaleGreen;
            this.btn_Calibrate.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Calibrate.Location = new System.Drawing.Point(772, 424);
            this.btn_Calibrate.Name = "btn_Calibrate";
            this.btn_Calibrate.Size = new System.Drawing.Size(144, 31);
            this.btn_Calibrate.TabIndex = 1;
            this.btn_Calibrate.Text = "Calibrate Frames";
            this.btn_Calibrate.UseVisualStyleBackColor = false;
            // 
            // dg_Calibrants
            // 
            this.dg_Calibrants.AllowUserToResizeColumns = false;
            this.dg_Calibrants.AllowUserToResizeRows = false;
            this.dg_Calibrants.BackgroundColor = System.Drawing.Color.DimGray;
            this.dg_Calibrants.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dg_Calibrants.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.SteelBlue;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.PowderBlue;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dg_Calibrants.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dg_Calibrants.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dg_Calibrants.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.col0_Enable,
            this.col1_Calibrant,
            this.col2_MZ,
            this.col3_Charge,
            this.Col4_TOF,
            this.col5_Bins,
            this.col7_ErrorPPM,
            this.col8_MZExp,
            this.col9_TOFExperimental});
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.Color.PowderBlue;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dg_Calibrants.DefaultCellStyle = dataGridViewCellStyle10;
            this.dg_Calibrants.GridColor = System.Drawing.Color.DimGray;
            this.dg_Calibrants.Location = new System.Drawing.Point(12, 16);
            this.dg_Calibrants.MultiSelect = false;
            this.dg_Calibrants.Name = "dg_Calibrants";
            this.dg_Calibrants.RowHeadersVisible = false;
            this.dg_Calibrants.RowHeadersWidth = 25;
            this.dg_Calibrants.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dg_Calibrants.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dg_Calibrants.Size = new System.Drawing.Size(932, 396);
            this.dg_Calibrants.TabIndex = 87;
            // 
            // col0_Enable
            // 
            this.col0_Enable.HeaderText = "";
            this.col0_Enable.Name = "col0_Enable";
            this.col0_Enable.Width = 20;
            // 
            // col1_Calibrant
            // 
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.col1_Calibrant.DefaultCellStyle = dataGridViewCellStyle2;
            this.col1_Calibrant.HeaderText = "Calibrant Name";
            this.col1_Calibrant.Name = "col1_Calibrant";
            this.col1_Calibrant.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.col1_Calibrant.Width = 175;
            // 
            // col2_MZ
            // 
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.Format = "N5";
            dataGridViewCellStyle3.NullValue = null;
            this.col2_MZ.DefaultCellStyle = dataGridViewCellStyle3;
            this.col2_MZ.HeaderText = "M/Z";
            this.col2_MZ.Name = "col2_MZ";
            this.col2_MZ.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.col2_MZ.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.col2_MZ.Width = 110;
            // 
            // col3_Charge
            // 
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.col3_Charge.DefaultCellStyle = dataGridViewCellStyle4;
            this.col3_Charge.HeaderText = "Charge";
            this.col3_Charge.Name = "col3_Charge";
            this.col3_Charge.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.col3_Charge.Width = 70;
            // 
            // Col4_TOF
            // 
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.Format = "N4";
            dataGridViewCellStyle5.NullValue = null;
            this.Col4_TOF.DefaultCellStyle = dataGridViewCellStyle5;
            this.Col4_TOF.HeaderText = "TOF (usec)";
            this.Col4_TOF.Name = "Col4_TOF";
            this.Col4_TOF.ReadOnly = true;
            this.Col4_TOF.Width = 110;
            // 
            // col5_Bins
            // 
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.Format = "N2";
            dataGridViewCellStyle6.NullValue = null;
            this.col5_Bins.DefaultCellStyle = dataGridViewCellStyle6;
            this.col5_Bins.DividerWidth = 25;
            this.col5_Bins.HeaderText = "Bins";
            this.col5_Bins.Name = "col5_Bins";
            this.col5_Bins.ReadOnly = true;
            this.col5_Bins.Width = 110;
            // 
            // col7_ErrorPPM
            // 
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.Format = "N2";
            dataGridViewCellStyle7.NullValue = null;
            this.col7_ErrorPPM.DefaultCellStyle = dataGridViewCellStyle7;
            this.col7_ErrorPPM.HeaderText = "Error PPM";
            this.col7_ErrorPPM.Name = "col7_ErrorPPM";
            this.col7_ErrorPPM.ReadOnly = true;
            this.col7_ErrorPPM.Width = 75;
            // 
            // col8_MZExp
            // 
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle8.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.Format = "N5";
            dataGridViewCellStyle8.NullValue = null;
            this.col8_MZExp.DefaultCellStyle = dataGridViewCellStyle8;
            this.col8_MZExp.HeaderText = "M/Z Experimental";
            this.col8_MZExp.Name = "col8_MZExp";
            this.col8_MZExp.ReadOnly = true;
            this.col8_MZExp.Width = 120;
            // 
            // col9_TOFExperimental
            // 
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle9.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle9.Format = "N4";
            dataGridViewCellStyle9.NullValue = null;
            this.col9_TOFExperimental.DefaultCellStyle = dataGridViewCellStyle9;
            this.col9_TOFExperimental.HeaderText = "TOF (usec) Experimental";
            this.col9_TOFExperimental.Name = "col9_TOFExperimental";
            this.col9_TOFExperimental.ReadOnly = true;
            this.col9_TOFExperimental.Width = 120;
            // 
            // tb_Slope
            // 
            this.tb_Slope.Location = new System.Drawing.Point(132, 448);
            this.tb_Slope.Name = "tb_Slope";
            this.tb_Slope.Size = new System.Drawing.Size(252, 20);
            this.tb_Slope.TabIndex = 88;
            // 
            // tb_Intercept
            // 
            this.tb_Intercept.Location = new System.Drawing.Point(132, 476);
            this.tb_Intercept.Name = "tb_Intercept";
            this.tb_Intercept.Size = new System.Drawing.Size(252, 20);
            this.tb_Intercept.TabIndex = 89;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(36, 452);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 90;
            this.label1.Text = "Slope";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(36, 480);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 91;
            this.label2.Text = "Intercept";
            // 
            // num_NoiseLevel
            // 
            this.num_NoiseLevel.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.num_NoiseLevel.Location = new System.Drawing.Point(636, 476);
            this.num_NoiseLevel.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.num_NoiseLevel.Name = "num_NoiseLevel";
            this.num_NoiseLevel.Size = new System.Drawing.Size(132, 20);
            this.num_NoiseLevel.TabIndex = 92;
            this.num_NoiseLevel.Value = new decimal(new int[] {
            600,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(492, 480);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 93;
            this.label3.Text = "Noise Level";
            // 
            // cb_NoiseLevel
            // 
            this.cb_NoiseLevel.AutoSize = true;
            this.cb_NoiseLevel.Location = new System.Drawing.Point(528, 448);
            this.cb_NoiseLevel.Name = "cb_NoiseLevel";
            this.cb_NoiseLevel.Size = new System.Drawing.Size(123, 17);
            this.cb_NoiseLevel.TabIndex = 94;
            this.cb_NoiseLevel.Text = "Use this Noise Level";
            this.cb_NoiseLevel.UseVisualStyleBackColor = true;
            // 
            // InternalCalibration
            // 
            this.BackColor = System.Drawing.Color.Silver;
            this.Controls.Add(this.cb_NoiseLevel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.num_NoiseLevel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tb_Intercept);
            this.Controls.Add(this.tb_Slope);
            this.Controls.Add(this.dg_Calibrants);
            this.Controls.Add(this.btn_Calibrate);
            this.Name = "InternalCalibration";
            this.Size = new System.Drawing.Size(1072, 609);
            ((System.ComponentModel.ISupportInitialize)(this.dg_Calibrants)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_NoiseLevel)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button btn_Calibrate;
        public System.Windows.Forms.DataGridView dg_Calibrants;
        private System.Windows.Forms.TextBox tb_Slope;
        private System.Windows.Forms.TextBox tb_Intercept;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridViewCheckBoxColumn col0_Enable;
        private System.Windows.Forms.DataGridViewTextBoxColumn col1_Calibrant;
        private System.Windows.Forms.DataGridViewTextBoxColumn col2_MZ;
        private System.Windows.Forms.DataGridViewTextBoxColumn col3_Charge;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col4_TOF;
        private System.Windows.Forms.DataGridViewTextBoxColumn col5_Bins;
        private System.Windows.Forms.DataGridViewTextBoxColumn col7_ErrorPPM;
        private System.Windows.Forms.DataGridViewTextBoxColumn col8_MZExp;
        private System.Windows.Forms.DataGridViewTextBoxColumn col9_TOFExperimental;
        public System.Windows.Forms.NumericUpDown num_NoiseLevel;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.CheckBox cb_NoiseLevel;
    }
}
