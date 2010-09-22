using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;

namespace UIMF_File.Utilities
{
	/// <summary>
	/// Summary description for ProgressBar.
	/// </summary>
	public class progress_Processing : System.Windows.Forms.Form
	{
        public System.Windows.Forms.Label lbl_Processing;
        public System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.ListBox lb_Warnings;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.Button btn_Continue;
        private System.Windows.Forms.ProgressBar progress_Slider;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public progress_Processing()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            // it appears as thought the frame does not have a parent and
            // so it is not created shown.  By not being shown, the buttons are
            // not initialized correctly and can not be enabled.
            this.Show();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
            this.btn_Cancel.Enabled = true;
            this.btn_Continue.Hide();

		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.lbl_Processing = new System.Windows.Forms.Label();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.lb_Warnings = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Continue = new System.Windows.Forms.Button();
            this.progress_Slider = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // lbl_Processing
            // 
            this.lbl_Processing.Font = new System.Drawing.Font("Verdana", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Processing.Location = new System.Drawing.Point(24, 4);
            this.lbl_Processing.Name = "lbl_Processing";
            this.lbl_Processing.Size = new System.Drawing.Size(384, 16);
            this.lbl_Processing.TabIndex = 1;
            this.lbl_Processing.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.BackColor = System.Drawing.Color.Crimson;
            this.btn_Cancel.Font = new System.Drawing.Font("Lucida Sans", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Cancel.ForeColor = System.Drawing.Color.White;
            this.btn_Cancel.Location = new System.Drawing.Point(336, 44);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(80, 24);
            this.btn_Cancel.TabIndex = 2;
            this.btn_Cancel.Text = "Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = false;
            // 
            // lb_Warnings
            // 
            this.lb_Warnings.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lb_Warnings.ForeColor = System.Drawing.Color.Crimson;
            this.lb_Warnings.HorizontalScrollbar = true;
            this.lb_Warnings.Location = new System.Drawing.Point(8, 60);
            this.lb_Warnings.Name = "lb_Warnings";
            this.lb_Warnings.Size = new System.Drawing.Size(312, 82);
            this.lb_Warnings.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(8, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(176, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "Warnings";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Continue
            // 
            this.btn_Continue.BackColor = System.Drawing.Color.DarkSlateGray;
            this.btn_Continue.Font = new System.Drawing.Font("Lucida Sans", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Continue.ForeColor = System.Drawing.Color.White;
            this.btn_Continue.Location = new System.Drawing.Point(336, 88);
            this.btn_Continue.Name = "btn_Continue";
            this.btn_Continue.Size = new System.Drawing.Size(80, 24);
            this.btn_Continue.TabIndex = 5;
            this.btn_Continue.Text = "Continue";
            this.btn_Continue.UseVisualStyleBackColor = false;
            this.btn_Continue.Click += new System.EventHandler(this.btn_Continue_Click);
            // 
            // progress_Slider
            // 
            this.progress_Slider.Location = new System.Drawing.Point(14, 23);
            this.progress_Slider.Name = "progress_Slider";
            this.progress_Slider.Size = new System.Drawing.Size(394, 10);
            this.progress_Slider.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progress_Slider.TabIndex = 7;
            this.progress_Slider.Value = 40;
            // 
            // progress_Slider
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(426, 154);
            this.ControlBox = false;
            this.Controls.Add(this.progress_Slider);
            this.Controls.Add(this.btn_Continue);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lb_Warnings);
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.lbl_Processing);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "progress_Slider";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "processing... ";
            this.TopMost = true;
            this.ResumeLayout(false);

        }
#endregion

		public string Caption
		{
			set { this.Text = value; }
			get { return this.Text; }
		}

        public string Title
        {
            set { this.lbl_Processing.Text = value; }
            get { return this.lbl_Processing.Text; }
        }

		public int Min
		{
            set { this.progress_Slider.Minimum = value; }
            get { return this.progress_Slider.Minimum; }
		}
		public int Max
		{
            set { this.progress_Slider.Maximum = value; }
            get { return progress_Slider.Maximum; }
		}

        public int current_value = 0;
        public bool flag_Busy = false;
        public void SetValue(int newValue)
		{
            current_value = newValue+1;

            if (flag_Busy)
                return;
            flag_Busy = true;

            // update only 20 times.  This thermometer is SLOW!
            // if (((double) plus1)/25.0 != (double) (plus1/25))
            //    return;

            if ((double)current_value <= (double)progress_Slider.Maximum)
                progress_Slider.Value = (int)current_value;

			this.Update();

            flag_Busy = false;
		}

        public void add_Warning(string warning)
        {
            this.lb_Warnings.Items.Add(warning);
        }

        public void clear_Warning()
        {
            this.lb_Warnings.Items.Clear();
        }

		public void Reset()
		{
            progress_Slider.Value = this.progress_Slider.Minimum;
        }

        public void Initialize(string directory)
        {
            this.Text = Path.GetFileName(directory);
            this.lb_Warnings.Items.Clear();
            this.SetValue(0);
            this.Update();
        }

        public void Hide_Frame()
        {
#if false
    this.Hide();
            return;
#else
           // this.progress_Slider.Hide();
            // something strange going on.  You include this section of the code
            // and you will find yourself with a progress frame that will halt the 
            // system... yikes!

            if (this.lb_Warnings.Items.Count > 0)
            {
                this.btn_Continue.Show();
                this.btn_Cancel.Enabled = false;
            }
            else
            {
                this.Hide();
            }
            this.Update();
#endif
        }

        private void btn_Continue_Click(object sender, System.EventArgs e)
        {
            this.btn_Continue.Hide();
            this.btn_Cancel.Enabled = true;

            this.Hide();
            this.Update();
        }
	}
}
