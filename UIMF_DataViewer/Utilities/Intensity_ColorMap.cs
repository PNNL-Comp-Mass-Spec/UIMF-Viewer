using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace UIMF_File.Utilities
{
	/// <summary>
	/// Summary description for ColorBlend.
	/// </summary>
    public class Intensity_ColorMap : System.Windows.Forms.Panel
    {            
        public System.Windows.Forms.Button[] btn_Slider;
        private Rectangle rect_colors;
        private ColorBlend color_blend;
        private float[] color_positions;
        private System.Windows.Forms.Panel panel1;

        //private float[] COLOR_POS = new float[] { 0.0f, 0.02F, .035f, .09f, .14f, .25f, .6F, 1.0f };
        //private float[] COLOR_POS = new float[] { 0.0f, 0.4F, .75f, .86f, .91f, .95f, .995F, 1.0f };
        private float[] COLOR_POS = new float[] { 0.0f, 0.4F, .75f, .86f, .91f, .975f, .995F, 1.0f };
        public System.Windows.Forms.Label lbl_MaxIntensity;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public Intensity_ColorMap()
        {
            int i;
            this.rect_colors = new Rectangle(7, 10,  6, 300);
            
            this.color_blend = new ColorBlend();
            this.color_positions = new float[] { COLOR_POS[0], COLOR_POS[1], COLOR_POS[2], COLOR_POS[3], COLOR_POS[4], COLOR_POS[5], COLOR_POS[6], COLOR_POS[7] };
            //this.color_blend.Colors = new Color[] { Color.DarkBlue, Color.Blue, Color.SkyBlue, Color.Lime, Color.GreenYellow, Color.Yellow, Color.Red, Color.Purple };
            this.color_blend.Colors = new Color[] { Color.Purple, Color.Red, Color.Yellow, Color.GreenYellow, Color.Lime, Color.SkyBlue, Color.Blue, Color.DarkBlue };
            this.color_blend.Positions = new float[color_positions.Length];
            for (i=0; i<color_positions.Length; i++)
                color_blend.Positions[i] = color_positions[i];

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            this.Paint += new PaintEventHandler( this.ColorBlender_Paint );
            this.Resize += new System.EventHandler( this.ResizeThis );

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            // btn_Slider[]
            btn_Slider = new System.Windows.Forms.Button[color_blend.Colors.Length-2];
            for (i=0; i<color_blend.Colors.Length-2; i++)
            {
                btn_Slider[i] = new System.Windows.Forms.Button();

                // 
                // btn_Slider
                // 
                this.btn_Slider[i].BackColor = color_blend.Colors[i+1];
                this.btn_Slider[i].FlatStyle = System.Windows.Forms.FlatStyle.Standard;
                this.btn_Slider[i].Location = new System.Drawing.Point(24, 8);
                this.btn_Slider[i].Name = "button_"+i.ToString("00");
                this.btn_Slider[i].Size = new System.Drawing.Size(22, 8);
                this.btn_Slider[i].TabIndex = i+1; // using this as a pointer to the index.
                this.btn_Slider[i].TabStop = false;
                this.btn_Slider[i].Text = "";

                this.btn_Slider[i].Left = 7;
                this.btn_Slider[i].Top = (int)(((float) rect_colors.Height) * color_blend.Positions[i+1]); // - this.btn_Slider[i].Height;
                this.btn_Slider[i].Width = 7 + this.rect_colors.Width;

                this.btn_Slider[i].MouseDown += new MouseEventHandler( this.slider_MouseDown );
                this.btn_Slider[i].MouseMove += new MouseEventHandler( this.slider_MouseMove );
                this.btn_Slider[i].MouseUp += new MouseEventHandler( this.slider_MouseUp );
                this.Controls.Add(this.btn_Slider[i]);
            }

            // 
            // lbl_MaxIntensity
            // 
            this.lbl_MaxIntensity = new System.Windows.Forms.Label();
            this.lbl_MaxIntensity.Font = new System.Drawing.Font("Verdana", 6.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.lbl_MaxIntensity.Location = new System.Drawing.Point(0,0);
            this.lbl_MaxIntensity.Name = "lbl_MaxIntensity";
            this.lbl_MaxIntensity.Size = new System.Drawing.Size(26, 16);
            this.lbl_MaxIntensity.TabIndex = 38;
            this.lbl_MaxIntensity.Text = "MAX";
            this.lbl_MaxIntensity.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.lbl_MaxIntensity.MouseEnter += new System.EventHandler( this.lbl_MaxIntensity_MouseEnter );
            this.lbl_MaxIntensity.MouseLeave += new System.EventHandler( this.lbl_MaxIntensity_MouseLeave );
            this.Controls.Add(this.lbl_MaxIntensity);


            this.Width = 16 + this.rect_colors.Width;
            this.TabStop = false;
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // ColorBlender
            // 
            this.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.Location = new System.Drawing.Point(4, 8);
            this.Name = "ColorBlender";
            this.Size = new System.Drawing.Size(72, 136);
            this.TabIndex = 0;
        }
        #endregion
	
        private void ResizeThis(object sender, System.EventArgs e)
        { 
            rect_colors = new Rectangle(12, 10,  4, this.Height-20);
            for (int i=0; i<color_blend.Positions.Length-2; i++)
                this.btn_Slider[i].Top = (int)(((float) rect_colors.Height) * color_blend.Positions[i+1]); // - this.btn_Slider[i].Height;
    
            this.Invalidate();
        }

        public void lbl_MaxIntensity_MouseEnter(object obj, System.EventArgs e)
        {
            this.lbl_MaxIntensity.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        }
        public void lbl_MaxIntensity_MouseLeave(object obj, System.EventArgs e)
        {
            this.lbl_MaxIntensity.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lbl_MaxIntensity.Refresh();
        }

        public void set_MaxIntensity(int max_intensity)
        {
            this.lbl_MaxIntensity.Text = "MAX"; // max_intensity.ToString();
        }

        private void ColorBlender_Paint(object sender, PaintEventArgs e)
        {
            int i;

            using (LinearGradientBrush the_brush =
                       new LinearGradientBrush(rect_colors, Color.Blue, Color.Red, LinearGradientMode.Vertical))
            {
                // Define a color blend.
                for (i=0; i<color_blend.Positions.Length; i++)
                    color_blend.Positions[i] = color_positions[i];
                the_brush.InterpolationColors = color_blend;

                // Draw.
                e.Graphics.FillRectangle(the_brush, rect_colors);
            }
        }

        public System.Windows.Forms.Button btn_selected;
        private void slider_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.btn_selected = (System.Windows.Forms.Button) sender;
        }
   
        private void slider_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            int new_pos;

            if (this.btn_selected != null)
            {
                new_pos = e.Y+this.btn_selected.Top;

                // ensure the button does not go off the track
                if (e.Y < 0)
                    this.bump_Color_Up(new_pos, this.btn_Slider[this.btn_selected.TabIndex-1]);
                else if (e.Y > 0)
                    this.bump_Color_Down(new_pos, this.btn_Slider[this.btn_selected.TabIndex-1]);

                this.Invalidate();
            }
        }

        private int bump_Color_Down(int top_position, System.Windows.Forms.Button moved_button)
        {
            int new_position = top_position;

            if (moved_button.TabIndex < this.btn_Slider.Length)
            {
                if (top_position > this.btn_Slider[moved_button.TabIndex].Top - moved_button.Height)
                {
                    new_position = bump_Color_Down(top_position+moved_button.Height, this.btn_Slider[moved_button.TabIndex]);
                    new_position -= moved_button.Height;
                }
            }
            else if (top_position > this.rect_colors.Height+this.rect_colors.Top - 4)
                new_position = this.rect_colors.Height+this.rect_colors.Top - 4;

            moved_button.Top = new_position;
            this.color_positions[moved_button.TabIndex] = (float) (moved_button.Top + (moved_button.Height/2)) / (float) this.rect_colors.Height;
            return new_position;
        }
        private int bump_Color_Up(int top_position, System.Windows.Forms.Button moved_button)
        {
            int new_position = top_position;

            if (moved_button.TabIndex > 1)
            {
                // if bumped into the button above
                if (top_position <= this.btn_Slider[moved_button.TabIndex - 2].Top+moved_button.Height)
                {
                    new_position = bump_Color_Up(top_position - moved_button.Height, this.btn_Slider[moved_button.TabIndex - 2]);
                    new_position += moved_button.Height;
                }
            }
            else if (top_position < this.rect_colors.Top-4)
            {
                new_position = this.rect_colors.Top-4;
            }
                
            moved_button.Top = new_position;
            this.color_positions[moved_button.TabIndex] = (float) (moved_button.Top + (moved_button.Height/2)) / (float) this.rect_colors.Height;
            return new_position;
        }

        private void slider_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.btn_selected = (System.Windows.Forms.Button) null;
            this.Invalidate();
        }

        public void reset_Settings()
        {
            for (int i=0; i<this.btn_Slider.Length; i++)
                this.color_positions[i+1] = this.color_blend.Positions[i+1] = this.COLOR_POS[i+1];
            ResizeThis((object) null, (System.EventArgs) null);
        }

        public unsafe void getRGB(float wl, PixelData *p)
        {
            float interp;
            int red, green, blue;

            for (int i=1; i<this.color_blend.Positions.Length; i++)
            {
                if (((float) 1.0-wl) <= this.color_blend.Positions[i])
                {
                    interp = (((float) 1.0-wl) - this.color_blend.Positions[i-1])/(this.color_blend.Positions[i] - this.color_blend.Positions[i-1]);

                    red = (int) (((float) (this.color_blend.Colors[i].R - this.color_blend.Colors[i-1].R)) * interp) + this.color_blend.Colors[i-1].R;
                    green = (int) (((float) (this.color_blend.Colors[i].G - this.color_blend.Colors[i-1].G) * interp)) + this.color_blend.Colors[i-1].G;
                    blue = (int) (((float) (this.color_blend.Colors[i].B - this.color_blend.Colors[i-1].B) * interp)) + this.color_blend.Colors[i-1].B;
                   
                    p->red = (byte) red;
                    p->green = (byte) green;
                    p->blue = (byte) blue;

                    return;
                }
            }

            // should never get here.
            p->red = 0;
            p->green = 0xFF;
            p->blue = 0;
            MessageBox.Show("Pixel Failure in ION_ColorMap.cs:  "+wl.ToString("0.000"));
        }
    }
}
