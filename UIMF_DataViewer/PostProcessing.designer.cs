namespace UIMF_File
{
    partial class PostProcessing
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.btn_AttemptCalibration = new System.Windows.Forms.Button();
            this.dg_Calibrants = new System.Windows.Forms.DataGridView();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_ExperimentCalibration = new System.Windows.Forms.Button();
            this.lbl_ExperimentalIntercept = new System.Windows.Forms.Label();
            this.lbl_ExperimentalSlope = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btn_ManualCalibration = new System.Windows.Forms.Button();
            this.lbl_CalculatedIntercept = new System.Windows.Forms.Label();
            this.lbl_CalculatedSlope = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.num_ion2_MZValue = new System.Windows.Forms.NumericUpDown();
            this.num_ion1_MZValue = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.num_ion2_TOFBin = new System.Windows.Forms.NumericUpDown();
            this.num_ion1_TOFBin = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.pnl_Success = new System.Windows.Forms.Panel();
            this.btn_DecodeMultiplexing = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tb_SaveDecodeFilename = new System.Windows.Forms.TextBox();
            this.Label18 = new System.Windows.Forms.Label();
            this.Label16 = new System.Windows.Forms.Label();
            this.btn_DecodeDirectoryBrowse = new System.Windows.Forms.Button();
            this.tb_SaveDecodeDirectory = new System.Windows.Forms.TextBox();
            this.gb_Compress4GHz = new System.Windows.Forms.GroupBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tb_SaveCompressFilename = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.btn_CompressDirectoryBrowse = new System.Windows.Forms.Button();
            this.tb_SaveCompressDirectory = new System.Windows.Forms.TextBox();
            this.btn_Compress1GHz = new System.Windows.Forms.Button();
            this.col0_Enable = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.col1_Calibrant = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col2_MZ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col3_Charge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col4_TOF = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col5_Bins = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col7_ErrorPPM = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col8_MZExp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col9_TOFExperimental = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dg_Calibrants)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_ion2_MZValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_ion1_MZValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_ion2_TOFBin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_ion1_TOFBin)).BeginInit();
            this.pnl_Success.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.gb_Compress4GHz.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_AttemptCalibration
            // 
            this.btn_AttemptCalibration.BackColor = System.Drawing.Color.LightSalmon;
            this.btn_AttemptCalibration.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_AttemptCalibration.Location = new System.Drawing.Point(628, 420);
            this.btn_AttemptCalibration.Name = "btn_AttemptCalibration";
            this.btn_AttemptCalibration.Size = new System.Drawing.Size(288, 39);
            this.btn_AttemptCalibration.TabIndex = 1;
            this.btn_AttemptCalibration.Text = "Attempt to Calibrate Frame";
            this.btn_AttemptCalibration.UseVisualStyleBackColor = false;
            // 
            // dg_Calibrants
            // 
            this.dg_Calibrants.AllowUserToDeleteRows = false;
            this.dg_Calibrants.AllowUserToResizeColumns = false;
            this.dg_Calibrants.AllowUserToResizeRows = false;
            this.dg_Calibrants.BackgroundColor = System.Drawing.Color.DimGray;
            this.dg_Calibrants.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dg_Calibrants.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
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
            this.dg_Calibrants.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dg_Calibrants.GridColor = System.Drawing.Color.DimGray;
            this.dg_Calibrants.Location = new System.Drawing.Point(12, 16);
            this.dg_Calibrants.MultiSelect = false;
            this.dg_Calibrants.Name = "dg_Calibrants";
            this.dg_Calibrants.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dg_Calibrants.RowHeadersVisible = false;
            this.dg_Calibrants.RowHeadersWidth = 25;
            this.dg_Calibrants.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dg_Calibrants.RowTemplate.DividerHeight = 1;
            this.dg_Calibrants.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dg_Calibrants.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dg_Calibrants.ShowCellErrors = false;
            this.dg_Calibrants.ShowCellToolTips = false;
            this.dg_Calibrants.ShowEditingIcon = false;
            this.dg_Calibrants.ShowRowErrors = false;
            this.dg_Calibrants.Size = new System.Drawing.Size(932, 396);
            this.dg_Calibrants.TabIndex = 87;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 24);
            this.label1.TabIndex = 90;
            this.label1.Text = "Slope";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(8, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 24);
            this.label2.TabIndex = 91;
            this.label2.Text = "Intercept";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btn_ExperimentCalibration
            // 
            this.btn_ExperimentCalibration.BackColor = System.Drawing.Color.LightSalmon;
            this.btn_ExperimentCalibration.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_ExperimentCalibration.Location = new System.Drawing.Point(44, 72);
            this.btn_ExperimentCalibration.Name = "btn_ExperimentCalibration";
            this.btn_ExperimentCalibration.Size = new System.Drawing.Size(196, 48);
            this.btn_ExperimentCalibration.TabIndex = 93;
            this.btn_ExperimentCalibration.Text = "Apply Calibration Values to All Frames";
            this.btn_ExperimentCalibration.UseVisualStyleBackColor = false;
            // 
            // lbl_ExperimentalIntercept
            // 
            this.lbl_ExperimentalIntercept.BackColor = System.Drawing.Color.Gainsboro;
            this.lbl_ExperimentalIntercept.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbl_ExperimentalIntercept.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_ExperimentalIntercept.Location = new System.Drawing.Point(96, 36);
            this.lbl_ExperimentalIntercept.Name = "lbl_ExperimentalIntercept";
            this.lbl_ExperimentalIntercept.Size = new System.Drawing.Size(164, 24);
            this.lbl_ExperimentalIntercept.TabIndex = 97;
            this.lbl_ExperimentalIntercept.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lbl_ExperimentalSlope
            // 
            this.lbl_ExperimentalSlope.BackColor = System.Drawing.Color.Gainsboro;
            this.lbl_ExperimentalSlope.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbl_ExperimentalSlope.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_ExperimentalSlope.Location = new System.Drawing.Point(96, 8);
            this.lbl_ExperimentalSlope.Name = "lbl_ExperimentalSlope";
            this.lbl_ExperimentalSlope.Size = new System.Drawing.Size(164, 24);
            this.lbl_ExperimentalSlope.TabIndex = 96;
            this.lbl_ExperimentalSlope.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btn_ManualCalibration);
            this.groupBox2.Controls.Add(this.lbl_CalculatedIntercept);
            this.groupBox2.Controls.Add(this.lbl_CalculatedSlope);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.num_ion2_MZValue);
            this.groupBox2.Controls.Add(this.num_ion1_MZValue);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.num_ion2_TOFBin);
            this.groupBox2.Controls.Add(this.num_ion1_TOFBin);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(20, 428);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(568, 164);
            this.groupBox2.TabIndex = 95;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Manual Calibration Calculator";
            // 
            // btn_ManualCalibration
            // 
            this.btn_ManualCalibration.BackColor = System.Drawing.Color.LightSalmon;
            this.btn_ManualCalibration.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_ManualCalibration.Location = new System.Drawing.Point(348, 96);
            this.btn_ManualCalibration.Name = "btn_ManualCalibration";
            this.btn_ManualCalibration.Size = new System.Drawing.Size(200, 52);
            this.btn_ManualCalibration.TabIndex = 102;
            this.btn_ManualCalibration.Text = "Apply Calibration Values to this Frame";
            this.btn_ManualCalibration.UseVisualStyleBackColor = false;
            // 
            // lbl_CalculatedIntercept
            // 
            this.lbl_CalculatedIntercept.BackColor = System.Drawing.Color.Gainsboro;
            this.lbl_CalculatedIntercept.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbl_CalculatedIntercept.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_CalculatedIntercept.Location = new System.Drawing.Point(424, 64);
            this.lbl_CalculatedIntercept.Name = "lbl_CalculatedIntercept";
            this.lbl_CalculatedIntercept.Size = new System.Drawing.Size(128, 24);
            this.lbl_CalculatedIntercept.TabIndex = 101;
            this.lbl_CalculatedIntercept.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lbl_CalculatedSlope
            // 
            this.lbl_CalculatedSlope.BackColor = System.Drawing.Color.Gainsboro;
            this.lbl_CalculatedSlope.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbl_CalculatedSlope.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_CalculatedSlope.Location = new System.Drawing.Point(424, 36);
            this.lbl_CalculatedSlope.Name = "lbl_CalculatedSlope";
            this.lbl_CalculatedSlope.Size = new System.Drawing.Size(128, 24);
            this.lbl_CalculatedSlope.TabIndex = 100;
            this.lbl_CalculatedSlope.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(340, 36);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(80, 24);
            this.label10.TabIndex = 98;
            this.label10.Text = "Slope";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label11
            // 
            this.label11.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(340, 64);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(80, 24);
            this.label11.TabIndex = 99;
            this.label11.Text = "Intercept";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // num_ion2_MZValue
            // 
            this.num_ion2_MZValue.DecimalPlaces = 4;
            this.num_ion2_MZValue.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_ion2_MZValue.Location = new System.Drawing.Point(188, 124);
            this.num_ion2_MZValue.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.num_ion2_MZValue.Name = "num_ion2_MZValue";
            this.num_ion2_MZValue.Size = new System.Drawing.Size(104, 25);
            this.num_ion2_MZValue.TabIndex = 8;
            this.num_ion2_MZValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.num_ion2_MZValue.ValueChanged += new System.EventHandler(this.num_CalculateCalibration_ValueChanged);
            // 
            // num_ion1_MZValue
            // 
            this.num_ion1_MZValue.DecimalPlaces = 4;
            this.num_ion1_MZValue.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_ion1_MZValue.Location = new System.Drawing.Point(188, 96);
            this.num_ion1_MZValue.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.num_ion1_MZValue.Name = "num_ion1_MZValue";
            this.num_ion1_MZValue.Size = new System.Drawing.Size(104, 25);
            this.num_ion1_MZValue.TabIndex = 7;
            this.num_ion1_MZValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.num_ion1_MZValue.ValueChanged += new System.EventHandler(this.num_CalculateCalibration_ValueChanged);
            // 
            // label7
            // 
            this.label7.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(16, 124);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(56, 24);
            this.label7.TabIndex = 6;
            this.label7.Text = "Ion 2";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(16, 96);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 24);
            this.label6.TabIndex = 5;
            this.label6.Text = "Ion 1";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // num_ion2_TOFBin
            // 
            this.num_ion2_TOFBin.DecimalPlaces = 3;
            this.num_ion2_TOFBin.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_ion2_TOFBin.Location = new System.Drawing.Point(76, 124);
            this.num_ion2_TOFBin.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.num_ion2_TOFBin.Name = "num_ion2_TOFBin";
            this.num_ion2_TOFBin.Size = new System.Drawing.Size(96, 25);
            this.num_ion2_TOFBin.TabIndex = 4;
            this.num_ion2_TOFBin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.num_ion2_TOFBin.ValueChanged += new System.EventHandler(this.num_CalculateCalibration_ValueChanged);
            // 
            // num_ion1_TOFBin
            // 
            this.num_ion1_TOFBin.DecimalPlaces = 3;
            this.num_ion1_TOFBin.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.num_ion1_TOFBin.Location = new System.Drawing.Point(76, 96);
            this.num_ion1_TOFBin.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.num_ion1_TOFBin.Name = "num_ion1_TOFBin";
            this.num_ion1_TOFBin.Size = new System.Drawing.Size(96, 25);
            this.num_ion1_TOFBin.TabIndex = 3;
            this.num_ion1_TOFBin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.num_ion1_TOFBin.ValueChanged += new System.EventHandler(this.num_CalculateCalibration_ValueChanged);
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(84, 52);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(84, 40);
            this.label5.TabIndex = 2;
            this.label5.Text = "Known TOF Bin";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(196, 52);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 40);
            this.label4.TabIndex = 1;
            this.label4.Text = "Desired M/Z Value";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pnl_Success
            // 
            this.pnl_Success.Controls.Add(this.btn_ExperimentCalibration);
            this.pnl_Success.Controls.Add(this.lbl_ExperimentalIntercept);
            this.pnl_Success.Controls.Add(this.label2);
            this.pnl_Success.Controls.Add(this.label1);
            this.pnl_Success.Controls.Add(this.lbl_ExperimentalSlope);
            this.pnl_Success.Location = new System.Drawing.Point(636, 464);
            this.pnl_Success.Name = "pnl_Success";
            this.pnl_Success.Size = new System.Drawing.Size(272, 128);
            this.pnl_Success.TabIndex = 98;
            // 
            // btn_DecodeMultiplexing
            // 
            this.btn_DecodeMultiplexing.BackColor = System.Drawing.Color.PowderBlue;
            this.btn_DecodeMultiplexing.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_DecodeMultiplexing.Location = new System.Drawing.Point(768, 28);
            this.btn_DecodeMultiplexing.Name = "btn_DecodeMultiplexing";
            this.btn_DecodeMultiplexing.Size = new System.Drawing.Size(120, 52);
            this.btn_DecodeMultiplexing.TabIndex = 103;
            this.btn_DecodeMultiplexing.Text = "Decode Experiment";
            this.btn_DecodeMultiplexing.UseVisualStyleBackColor = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.tb_SaveDecodeFilename);
            this.groupBox1.Controls.Add(this.Label18);
            this.groupBox1.Controls.Add(this.Label16);
            this.groupBox1.Controls.Add(this.btn_DecodeDirectoryBrowse);
            this.groupBox1.Controls.Add(this.tb_SaveDecodeDirectory);
            this.groupBox1.Controls.Add(this.btn_DecodeMultiplexing);
            this.groupBox1.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(20, 608);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(900, 96);
            this.groupBox1.TabIndex = 104;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Decode Multiplexed Experiment";
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Cursor = System.Windows.Forms.Cursors.Default;
            this.label3.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(536, 28);
            this.label3.Name = "label3";
            this.label3.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label3.Size = new System.Drawing.Size(144, 28);
            this.label3.TabIndex = 109;
            this.label3.Text = "_decoded.UIMF";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tb_SaveDecodeFilename
            // 
            this.tb_SaveDecodeFilename.AcceptsReturn = true;
            this.tb_SaveDecodeFilename.BackColor = System.Drawing.Color.White;
            this.tb_SaveDecodeFilename.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.tb_SaveDecodeFilename.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tb_SaveDecodeFilename.ForeColor = System.Drawing.SystemColors.WindowText;
            this.tb_SaveDecodeFilename.Location = new System.Drawing.Point(160, 28);
            this.tb_SaveDecodeFilename.MaxLength = 0;
            this.tb_SaveDecodeFilename.Name = "tb_SaveDecodeFilename";
            this.tb_SaveDecodeFilename.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tb_SaveDecodeFilename.Size = new System.Drawing.Size(372, 25);
            this.tb_SaveDecodeFilename.TabIndex = 107;
            // 
            // Label18
            // 
            this.Label18.BackColor = System.Drawing.Color.Transparent;
            this.Label18.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label18.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label18.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label18.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label18.Location = new System.Drawing.Point(12, 28);
            this.Label18.Name = "Label18";
            this.Label18.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label18.Size = new System.Drawing.Size(144, 24);
            this.Label18.TabIndex = 108;
            this.Label18.Text = "Save Filename:";
            this.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label16
            // 
            this.Label16.BackColor = System.Drawing.Color.Transparent;
            this.Label16.Cursor = System.Windows.Forms.Cursors.Default;
            this.Label16.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label16.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Label16.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label16.Location = new System.Drawing.Point(32, 60);
            this.Label16.Name = "Label16";
            this.Label16.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Label16.Size = new System.Drawing.Size(124, 24);
            this.Label16.TabIndex = 105;
            this.Label16.Text = "Save Directory:";
            this.Label16.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btn_DecodeDirectoryBrowse
            // 
            this.btn_DecodeDirectoryBrowse.BackColor = System.Drawing.Color.Gainsboro;
            this.btn_DecodeDirectoryBrowse.Cursor = System.Windows.Forms.Cursors.Default;
            this.btn_DecodeDirectoryBrowse.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_DecodeDirectoryBrowse.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btn_DecodeDirectoryBrowse.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_DecodeDirectoryBrowse.Location = new System.Drawing.Point(644, 56);
            this.btn_DecodeDirectoryBrowse.Name = "btn_DecodeDirectoryBrowse";
            this.btn_DecodeDirectoryBrowse.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btn_DecodeDirectoryBrowse.Size = new System.Drawing.Size(100, 32);
            this.btn_DecodeDirectoryBrowse.TabIndex = 106;
            this.btn_DecodeDirectoryBrowse.Text = "Browse";
            this.btn_DecodeDirectoryBrowse.UseVisualStyleBackColor = false;
            this.btn_DecodeDirectoryBrowse.Click += new System.EventHandler(this.btn_DecodeDirectoryBrowse_Click);
            // 
            // tb_SaveDecodeDirectory
            // 
            this.tb_SaveDecodeDirectory.AcceptsReturn = true;
            this.tb_SaveDecodeDirectory.BackColor = System.Drawing.SystemColors.Window;
            this.tb_SaveDecodeDirectory.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.tb_SaveDecodeDirectory.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tb_SaveDecodeDirectory.ForeColor = System.Drawing.SystemColors.WindowText;
            this.tb_SaveDecodeDirectory.Location = new System.Drawing.Point(160, 60);
            this.tb_SaveDecodeDirectory.MaxLength = 0;
            this.tb_SaveDecodeDirectory.Name = "tb_SaveDecodeDirectory";
            this.tb_SaveDecodeDirectory.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tb_SaveDecodeDirectory.Size = new System.Drawing.Size(476, 25);
            this.tb_SaveDecodeDirectory.TabIndex = 104;
            this.tb_SaveDecodeDirectory.Text = "C:\\";
            // 
            // gb_Compress4GHz
            // 
            this.gb_Compress4GHz.Controls.Add(this.label8);
            this.gb_Compress4GHz.Controls.Add(this.tb_SaveCompressFilename);
            this.gb_Compress4GHz.Controls.Add(this.label9);
            this.gb_Compress4GHz.Controls.Add(this.label12);
            this.gb_Compress4GHz.Controls.Add(this.btn_CompressDirectoryBrowse);
            this.gb_Compress4GHz.Controls.Add(this.tb_SaveCompressDirectory);
            this.gb_Compress4GHz.Controls.Add(this.btn_Compress1GHz);
            this.gb_Compress4GHz.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gb_Compress4GHz.Location = new System.Drawing.Point(20, 720);
            this.gb_Compress4GHz.Name = "gb_Compress4GHz";
            this.gb_Compress4GHz.Size = new System.Drawing.Size(900, 96);
            this.gb_Compress4GHz.TabIndex = 105;
            this.gb_Compress4GHz.TabStop = false;
            this.gb_Compress4GHz.Text = "Compress Experiment from 4GHz to 1 GHz data";
            // 
            // label8
            // 
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Cursor = System.Windows.Forms.Cursors.Default;
            this.label8.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label8.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label8.Location = new System.Drawing.Point(536, 28);
            this.label8.Name = "label8";
            this.label8.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label8.Size = new System.Drawing.Size(144, 28);
            this.label8.TabIndex = 109;
            this.label8.Text = "_1GHz.UIMF";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tb_SaveCompressFilename
            // 
            this.tb_SaveCompressFilename.AcceptsReturn = true;
            this.tb_SaveCompressFilename.BackColor = System.Drawing.Color.White;
            this.tb_SaveCompressFilename.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.tb_SaveCompressFilename.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tb_SaveCompressFilename.ForeColor = System.Drawing.SystemColors.WindowText;
            this.tb_SaveCompressFilename.Location = new System.Drawing.Point(160, 28);
            this.tb_SaveCompressFilename.MaxLength = 0;
            this.tb_SaveCompressFilename.Name = "tb_SaveCompressFilename";
            this.tb_SaveCompressFilename.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tb_SaveCompressFilename.Size = new System.Drawing.Size(372, 25);
            this.tb_SaveCompressFilename.TabIndex = 107;
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Cursor = System.Windows.Forms.Cursors.Default;
            this.label9.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label9.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label9.Location = new System.Drawing.Point(12, 28);
            this.label9.Name = "label9";
            this.label9.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label9.Size = new System.Drawing.Size(144, 24);
            this.label9.TabIndex = 108;
            this.label9.Text = "Save Filename:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label12
            // 
            this.label12.BackColor = System.Drawing.Color.Transparent;
            this.label12.Cursor = System.Windows.Forms.Cursors.Default;
            this.label12.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label12.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label12.Location = new System.Drawing.Point(32, 60);
            this.label12.Name = "label12";
            this.label12.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label12.Size = new System.Drawing.Size(124, 24);
            this.label12.TabIndex = 105;
            this.label12.Text = "Save Directory:";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btn_CompressDirectoryBrowse
            // 
            this.btn_CompressDirectoryBrowse.BackColor = System.Drawing.Color.Gainsboro;
            this.btn_CompressDirectoryBrowse.Cursor = System.Windows.Forms.Cursors.Default;
            this.btn_CompressDirectoryBrowse.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_CompressDirectoryBrowse.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btn_CompressDirectoryBrowse.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_CompressDirectoryBrowse.Location = new System.Drawing.Point(644, 56);
            this.btn_CompressDirectoryBrowse.Name = "btn_CompressDirectoryBrowse";
            this.btn_CompressDirectoryBrowse.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btn_CompressDirectoryBrowse.Size = new System.Drawing.Size(100, 32);
            this.btn_CompressDirectoryBrowse.TabIndex = 106;
            this.btn_CompressDirectoryBrowse.Text = "Browse";
            this.btn_CompressDirectoryBrowse.UseVisualStyleBackColor = false;
            this.btn_CompressDirectoryBrowse.Click += new System.EventHandler(this.btn_CompressDirectoryBrowse_Click);
            // 
            // tb_SaveCompressDirectory
            // 
            this.tb_SaveCompressDirectory.AcceptsReturn = true;
            this.tb_SaveCompressDirectory.BackColor = System.Drawing.SystemColors.Window;
            this.tb_SaveCompressDirectory.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.tb_SaveCompressDirectory.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tb_SaveCompressDirectory.ForeColor = System.Drawing.SystemColors.WindowText;
            this.tb_SaveCompressDirectory.Location = new System.Drawing.Point(160, 60);
            this.tb_SaveCompressDirectory.MaxLength = 0;
            this.tb_SaveCompressDirectory.Name = "tb_SaveCompressDirectory";
            this.tb_SaveCompressDirectory.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tb_SaveCompressDirectory.Size = new System.Drawing.Size(476, 25);
            this.tb_SaveCompressDirectory.TabIndex = 104;
            this.tb_SaveCompressDirectory.Text = "C:\\";
            // 
            // btn_Compress1GHz
            // 
            this.btn_Compress1GHz.BackColor = System.Drawing.Color.PowderBlue;
            this.btn_Compress1GHz.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Compress1GHz.Location = new System.Drawing.Point(768, 28);
            this.btn_Compress1GHz.Name = "btn_Compress1GHz";
            this.btn_Compress1GHz.Size = new System.Drawing.Size(120, 52);
            this.btn_Compress1GHz.TabIndex = 103;
            this.btn_Compress1GHz.Text = "Compress Experiment";
            this.btn_Compress1GHz.UseVisualStyleBackColor = false;
            // 
            // col0_Enable
            // 
            this.col0_Enable.DividerWidth = 1;
            this.col0_Enable.HeaderText = "";
            this.col0_Enable.Name = "col0_Enable";
            this.col0_Enable.Width = 20;
            // 
            // col1_Calibrant
            // 
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.col1_Calibrant.DefaultCellStyle = dataGridViewCellStyle2;
            this.col1_Calibrant.DividerWidth = 1;
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
            this.col2_MZ.DividerWidth = 1;
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
            this.col3_Charge.DividerWidth = 1;
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
            this.Col4_TOF.DividerWidth = 1;
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
            this.col7_ErrorPPM.DividerWidth = 1;
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
            this.col8_MZExp.DividerWidth = 1;
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
            this.col9_TOFExperimental.DividerWidth = 1;
            this.col9_TOFExperimental.HeaderText = "TOF (usec) Experimental";
            this.col9_TOFExperimental.Name = "col9_TOFExperimental";
            this.col9_TOFExperimental.ReadOnly = true;
            this.col9_TOFExperimental.Width = 120;
            // 
            // PostProcessing
            // 
            this.BackColor = System.Drawing.Color.Silver;
            this.Controls.Add(this.gb_Compress4GHz);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pnl_Success);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.dg_Calibrants);
            this.Controls.Add(this.btn_AttemptCalibration);
            this.Name = "PostProcessing";
            this.Size = new System.Drawing.Size(1072, 847);
            ((System.ComponentModel.ISupportInitialize)(this.dg_Calibrants)).EndInit();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.num_ion2_MZValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_ion1_MZValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_ion2_TOFBin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_ion1_TOFBin)).EndInit();
            this.pnl_Success.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gb_Compress4GHz.ResumeLayout(false);
            this.gb_Compress4GHz.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Button btn_AttemptCalibration;
        public System.Windows.Forms.DataGridView dg_Calibrants;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.Button btn_ExperimentCalibration;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown num_ion1_TOFBin;
        private System.Windows.Forms.Label lbl_ExperimentalSlope;
        private System.Windows.Forms.NumericUpDown num_ion2_MZValue;
        private System.Windows.Forms.NumericUpDown num_ion1_MZValue;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown num_ion2_TOFBin;
        private System.Windows.Forms.Label lbl_ExperimentalIntercept;
        public System.Windows.Forms.Button btn_ManualCalibration;
        private System.Windows.Forms.Label lbl_CalculatedIntercept;
        private System.Windows.Forms.Label lbl_CalculatedSlope;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Panel pnl_Success;
        public System.Windows.Forms.Button btn_DecodeMultiplexing;
        private System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.Label Label16;
        public System.Windows.Forms.Button btn_DecodeDirectoryBrowse;
        public System.Windows.Forms.TextBox tb_SaveDecodeDirectory;
        public System.Windows.Forms.TextBox tb_SaveDecodeFilename;
        public System.Windows.Forms.Label Label18;
        public System.Windows.Forms.Label label3;
        public System.Windows.Forms.Label label8;
        public System.Windows.Forms.TextBox tb_SaveCompressFilename;
        public System.Windows.Forms.Label label9;
        public System.Windows.Forms.Label label12;
        public System.Windows.Forms.Button btn_CompressDirectoryBrowse;
        public System.Windows.Forms.TextBox tb_SaveCompressDirectory;
        public System.Windows.Forms.Button btn_Compress1GHz;
        public System.Windows.Forms.GroupBox gb_Compress4GHz;
        private System.Windows.Forms.DataGridViewCheckBoxColumn col0_Enable;
        private System.Windows.Forms.DataGridViewTextBoxColumn col1_Calibrant;
        private System.Windows.Forms.DataGridViewTextBoxColumn col2_MZ;
        private System.Windows.Forms.DataGridViewTextBoxColumn col3_Charge;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col4_TOF;
        private System.Windows.Forms.DataGridViewTextBoxColumn col5_Bins;
        private System.Windows.Forms.DataGridViewTextBoxColumn col7_ErrorPPM;
        private System.Windows.Forms.DataGridViewTextBoxColumn col8_MZExp;
        private System.Windows.Forms.DataGridViewTextBoxColumn col9_TOFExperimental;
    }
}
