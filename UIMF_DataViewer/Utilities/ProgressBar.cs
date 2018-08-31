using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace UIMF_File.Utilities
{
    /// <summary>
    /// Summary description for ProgressBar.
    /// </summary>
    public class progress_Processing : System.Windows.Forms.Form
    {
        public System.Windows.Forms.Label lbl_Processing;
        public System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.Button btn_Continue;
        private System.Windows.Forms.ProgressBar progress_Slider;

        public bool flag_Stop = false;
        private RichTextBox rtb_Status;
        private ArrayList queue_Status;

        private Font font_Error;
        private Font font_Messages;

        private bool flag_Errors = false;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public progress_Processing()
        {
            this.flag_Errors = false;

            this.queue_Status = new ArrayList();
            this.font_Messages = new Font("Verdana", 7);
            this.font_Error = new Font("Verdana", 8);

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            this.VisibleChanged += new EventHandler(progress_Processing_VisibleChanged);

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
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Continue = new System.Windows.Forms.Button();
            this.progress_Slider = new System.Windows.Forms.ProgressBar();
            this.rtb_Status = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            //
            // lbl_Processing
            //
            this.lbl_Processing.BackColor = System.Drawing.Color.Transparent;
            this.lbl_Processing.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Processing.Location = new System.Drawing.Point(16, 4);
            this.lbl_Processing.Name = "lbl_Processing";
            this.lbl_Processing.Size = new System.Drawing.Size(392, 16);
            this.lbl_Processing.TabIndex = 1;
            this.lbl_Processing.Text = "frame";
            this.lbl_Processing.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btn_Cancel
            //
            this.btn_Cancel.BackColor = System.Drawing.Color.Crimson;
            this.btn_Cancel.Font = new System.Drawing.Font("Lucida Sans", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Cancel.ForeColor = System.Drawing.Color.White;
            this.btn_Cancel.Location = new System.Drawing.Point(408, 48);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(104, 28);
            this.btn_Cancel.TabIndex = 2;
            this.btn_Cancel.Text = "Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = false;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            //
            // label1
            //
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 24);
            this.label1.TabIndex = 4;
            this.label1.Text = "Status";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // btn_Continue
            //
            this.btn_Continue.BackColor = System.Drawing.Color.DarkSlateGray;
            this.btn_Continue.Font = new System.Drawing.Font("Lucida Sans", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Continue.ForeColor = System.Drawing.Color.White;
            this.btn_Continue.Location = new System.Drawing.Point(408, 92);
            this.btn_Continue.Name = "btn_Continue";
            this.btn_Continue.Size = new System.Drawing.Size(104, 28);
            this.btn_Continue.TabIndex = 5;
            this.btn_Continue.Text = "Continue";
            this.btn_Continue.UseVisualStyleBackColor = false;
            this.btn_Continue.Click += new System.EventHandler(this.btn_Continue_Click);
            //
            // progress_Slider
            //
            this.progress_Slider.BackColor = System.Drawing.Color.DimGray;
            this.progress_Slider.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.progress_Slider.Location = new System.Drawing.Point(14, 23);
            this.progress_Slider.Name = "progress_Slider";
            this.progress_Slider.Size = new System.Drawing.Size(498, 10);
            this.progress_Slider.Step = 1;
            this.progress_Slider.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progress_Slider.TabIndex = 7;
            //
            // rtb_Status
            //
            this.rtb_Status.BackColor = System.Drawing.Color.Silver;
            this.rtb_Status.Location = new System.Drawing.Point(12, 64);
            this.rtb_Status.Name = "rtb_Status";
            this.rtb_Status.Size = new System.Drawing.Size(384, 128);
            this.rtb_Status.TabIndex = 8;
            this.rtb_Status.Text = "";
            //
            // progress_Processing
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.BackgroundImage = global::UIMF_DataViewer.Properties.Resources.ripple21;
            this.ClientSize = new System.Drawing.Size(525, 203);
            this.Controls.Add(this.rtb_Status);
            this.Controls.Add(this.progress_Slider);
            this.Controls.Add(this.btn_Continue);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.lbl_Processing);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "progress_Processing";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "processing... ";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.progress_Processing_Closing);
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
        public int time_msec = 0;
        public bool flag_Busy = false;
        public void SetValue(int newValue, int msec)
        {
            this.time_msec = msec;

            current_value = newValue+1;
            if ((double)current_value > (double)progress_Slider.Maximum)
                current_value = progress_Slider.Maximum;

            Invoke(new ThreadStart(invoke_Slider));
        }

        public void invoke_Slider()
        {
            if ((double)current_value <= (double)progress_Slider.Maximum)
                progress_Slider.Value = (int)current_value;

            if (this.time_msec > 0)
                this.lbl_Processing.Text = "Frame " + current_value.ToString() + " of " + progress_Slider.Maximum.ToString()+"...  ("+this.time_msec.ToString()+" msec)";
            else
                this.lbl_Processing.Text = "Frame " + current_value.ToString() + " of " + progress_Slider.Maximum.ToString() + "...";

            this.handle_rtb_StatusUpdates();

            this.Update();
        }

        public void add_Status(string message, bool flag_error)
        {
            Status_Messages sm;
            if (flag_error)
            {
                this.flag_Errors = true;
                sm = new Status_Messages(message, Color.Red, true);
            }
            else
                sm = new Status_Messages(message, Color.Black, true);

            this.queue_Status.Insert(0, sm);
        }

        public bool Success()
        {
            return !this.flag_Errors;
        }

        public void clear_Status()
        {
            this.queue_Status.Clear();
        }

        public void Reset()
        {
            progress_Slider.Value = this.progress_Slider.Minimum;
        }

        public void Initialize()
        {
            this.clear_Status();
            this.flag_Errors = false;

            this.SetValue(0, 0);
            this.Update();
        }

        private void progress_Processing_VisibleChanged(object sender, System.EventArgs e)
        {
            this.flag_Stop = false;
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            this.flag_Stop = true;
        }
        private void progress_Processing_Closing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            this.flag_Stop = true;
        }
        private void btn_Continue_Click(object sender, System.EventArgs e)
        {
            this.btn_Continue.Hide();
            this.btn_Cancel.Enabled = true;

            this.Hide();
            this.Update();
        }

        // /////////////////////////////////////////////////////////
        // rich text box control
        //

        private void handle_rtb_StatusUpdates()
        {
            int rtb_length;
            Status_Messages first_message;

            try
            {
                //if (this.flag_IDLE)
                {
                    for (int i = 0; i < this.queue_Status.Count; i++)
                    {
                        first_message = (Status_Messages)this.queue_Status[i];

                        if (first_message != null) // should never happen
                        {
                            rtb_length = this.rtb_Status.Text.Length;
                            this.rtb_Status.AppendText(first_message.message+"\n");
#if STATUS_LOGGING && !DESKTOP
                            if (this.flag_LIVE)
                            {
                                try
                                {
                                    this.sw_StatusLog.WriteLine(DateTime.Now.ToLongTimeString() + ": " + first_message.message.Trim());
                                }
                                catch (Exception ex)
                                {
                                    this.rtb_StatusUpdate.AppendText("failed to update status log\n");
                                }
                            }
#endif
                            this.rtb_Status.Select(rtb_length, this.rtb_Status.Text.Length - rtb_length);
                            if (first_message.flag_status)
                                this.rtb_Status.SelectionFont = this.font_Error;
                            else
                                this.rtb_Status.SelectionFont = this.font_Messages;
                            this.rtb_Status.SelectionColor = first_message.font_color;

                            this.rtb_Status.ScrollToCaret();
                            this.rtb_Status.Update();
                        }
                    }
                    this.queue_Status.Clear();
                }

                while (this.queue_Status.Count > 0)
                {
                    if (this.rtb_Status.Lines.Length > 100)
                    {
                        try
                        {
                            this.rtb_Status.SelectionStart = 0;
                            this.rtb_Status.Update();

                            this.rtb_Status.Clear();
                            this.rtb_Status.Update();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("ERROR: handle_rtb_StatusUpdates(): " + ex.ToString());
                        }
                    }

                    first_message = (Status_Messages)this.queue_Status[0];
                    this.queue_Status.RemoveAt(0);

                    if (first_message != null)
                    {
                        rtb_length = this.rtb_Status.Text.Length;
                        this.rtb_Status.AppendText(first_message.message+"\n");
#if STATUS_LOGGING && !DESKTOP
                        if (this.flag_LIVE)
                        {
                            try
                            {
                                this.sw_StatusLog.WriteLine(DateTime.Now.ToLongTimeString() + ": " + first_message.message.Trim());
                            }
                            catch (Exception ex)
                            {
                                this.rtb_StatusUpdate.AppendText("failed to update status log\n");
                            }
                        }
#endif
                        this.rtb_Status.Select(rtb_length, this.rtb_Status.Text.Length - rtb_length);
                        if (first_message.flag_status)
                            this.rtb_Status.SelectionFont = this.font_Error;
                        else
                            this.rtb_Status.SelectionFont = this.font_Messages;
                        this.rtb_Status.SelectionColor = first_message.font_color;

                        this.rtb_Status.ScrollToCaret();
                        this.rtb_Status.Update();
                    }
                }

                GC.Collect();

#if STATUS_LOGGING && !DESKTOP
                this.sw_StatusLog.Flush();
#endif
            }
            catch (Exception ex)
            {
                if ((ex.GetHashCode() != 49538252) && (ex.GetHashCode() != 42931033) && (ex.GetHashCode() != 62476613)) // shutting down, i think...
                    MessageBox.Show("failed in handle_rtb_StatusUpdates(): " + ex.ToString() + "\n" + ex.GetHashCode().ToString());
            }
        }
    }

    public class Status_Messages
    {
        public string message;
        public Color font_color;
        public bool flag_status;

        public Status_Messages(string msg, Color fc, bool fs)
        {
            message = msg;
            font_color = fc;
            flag_status = fs;
        }
    }
}
