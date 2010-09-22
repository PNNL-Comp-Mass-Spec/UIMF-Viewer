using System;
using System.Windows.Forms;
using System.Drawing;

namespace UIMF_File.Utilities
{
    /* **********************************************************************************
     * Normalization Slider
     */
    public class GrayScaleSlider : System.Windows.Forms.Panel
    {
        public System.Windows.Forms.Button btn_GreyValue;

        private bool flag_MouseDown = false;
        private const int MAX_VAL = 255;
        private const int MIN_VAL = 0;
        private int slider_value = 0;
        private System.Windows.Forms.PictureBox ptr_pbTrack;

        public GrayScaleSlider(System.Windows.Forms.PictureBox background)
        {
            this.ptr_pbTrack = background;

            this.btn_GreyValue = new System.Windows.Forms.Button();

            // 
            // btn_GreyValue
            // 
            this.btn_GreyValue.Location = new System.Drawing.Point(0, 12);
            this.btn_GreyValue.BackColor = Color.WhiteSmoke;
            this.btn_GreyValue.ForeColor = Color.Black;
            this.btn_GreyValue.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.btn_GreyValue.Name = "btn_GreyValue";
            this.btn_GreyValue.Size = new System.Drawing.Size(38, 20);
            this.btn_GreyValue.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btn_GreyValue.TabIndex = 50;
            this.btn_GreyValue.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btn_GreyValue_MouseUp);
            this.btn_GreyValue.MouseMove += new System.Windows.Forms.MouseEventHandler(this.btn_GreyValue_MouseMove);
            this.btn_GreyValue.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btn_GreyValue_MouseDown);
            this.btn_GreyValue.Text = "1.0";
            this.btn_GreyValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ptr_pbTrack
            // 
            this.ptr_pbTrack.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ptr_pbTrack.Location = new System.Drawing.Point(14, 16);
            this.ptr_pbTrack.Name = "ptr_pbTrack";
            this.ptr_pbTrack.Size = new System.Drawing.Size(8, 208);
            this.ptr_pbTrack.TabIndex = 51;
            //
            // main slider information
            //
            this.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.BackColor = Color.Transparent;
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                          this.btn_GreyValue,
                                                                          this.ptr_pbTrack });
            this.Location = new System.Drawing.Point(504, 28);
            this.Name = "pnl_Outer";
            this.Size = new System.Drawing.Size(40, 329);
            this.TabIndex = 48;
            this.Resize += new System.EventHandler(this.ResizeThis);

            this.set_Value(30);
            this.Select(true, true);
        }

        public void ResizeThis(object obj, System.EventArgs e)
        {
            this.ptr_pbTrack.Top = 5;

            this.ptr_pbTrack.Height = this.Height - 15;
            this.Refresh();

            this.set_Value(this.slider_value);
        }

        private void btn_GreyValue_MouseDown(object obj, System.Windows.Forms.MouseEventArgs e)
        {
            this.flag_MouseDown = true;
        }
        private void btn_GreyValue_MouseUp(object obj, System.Windows.Forms.MouseEventArgs e)
        {
            this.flag_MouseDown = false;
            this.set_Value(this.slider_value);

            this.Select(true, true);
        }
        private void btn_GreyValue_MouseMove(object obj, System.Windows.Forms.MouseEventArgs e)
        {
            int new_pos;

            if (this.flag_MouseDown)
            {
                new_pos = e.Y + this.btn_GreyValue.Top - 4;

                // ensure the button does not go off the track
                if (new_pos < this.ptr_pbTrack.Top - 4)
                    new_pos = this.ptr_pbTrack.Top - 4;
                else if (new_pos > this.ptr_pbTrack.Height + this.ptr_pbTrack.Top - 12)
                    new_pos = this.ptr_pbTrack.Height + this.ptr_pbTrack.Top - 12;

                this.btn_GreyValue.Top = new_pos;

                // used for brightness - change the color of the back
                this.slider_value = ((MAX_VAL + 1) - (new_pos * (MAX_VAL + 1) / (this.ptr_pbTrack.Height - 12)));
                if (this.slider_value < MIN_VAL)
                    this.slider_value = MIN_VAL;
                this.btn_GreyValue.Text = this.slider_value.ToString("##0");
            }
        }

        public void set_Value(int val)
        {
            try
            {
                if (this.slider_value < MIN_VAL)
                    this.slider_value = MIN_VAL;
                this.slider_value = val;

                this.btn_GreyValue.Top = (MAX_VAL - this.slider_value) * (this.ptr_pbTrack.Height - 12) / MAX_VAL;
                this.btn_GreyValue.Text = this.slider_value.ToString("##0");

                this.Select(true, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Slider.set_Value(): " + ex.ToString());
            }
        }

        public double get_Value()
        {
            //MessageBox.Show((MAX_VAL-this.slider_value).ToString());
            return this.slider_value;
        }
    }
}

