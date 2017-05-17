//#define MOUSE_POSITION

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using UIMFLibrary;

namespace UIMF_DataViewer
{
    public partial class InstrumentSettings : UserControl
    {
        private const int TOP_SETTINGS = 400;
        private const int LEFT_SETTINGS = 0;

        private const int TOP_ENTRANCE = 270;
        private const int LEFT_ENTRANCE = 440;

        private const int TOP_TUBE = 0;
        private const int LEFT_TUBE = 340;

        private const int TOP_EXIT = -240;
        private const int LEFT_EXIT = 80;

        private const int NUM_VALUES = 31;
        private System.Windows.Forms.Label[] lbl_Desc;
        private System.Windows.Forms.TextBox[] tb_Value;

        public double[] default_FragmentationVoltages;

        private Graphics pnl_graphics;
        private Brush hilight_brush;
        private Pen hilight_line;
        private Brush shadow_brush;
        private Pen shadow_line;
#if MOUSE_POSITION
        private System.Windows.Forms.Label lbl_MouseClick;
#endif
        private int current_center_width;
        private int current_center_height;

        public InstrumentSettings()
        {
            InitializeComponent();

#if MOUSE_POSITION
            this.MouseDown += new MouseEventHandler(InstrumentSettings_MouseDown);
            this.pb_Entrance.MouseDown += new MouseEventHandler(pb_Entrance_MouseDown);

            this.lbl_MouseClick = new System.Windows.Forms.Label();
            // 
            // lbl_MouseClick
            // 
            this.lbl_MouseClick.ForeColor = System.Drawing.Color.Black;
            this.lbl_MouseClick.Location = new System.Drawing.Point(500, 4);
            this.lbl_MouseClick.Name = "lbl_MouseClick";
            this.lbl_MouseClick.Size = new System.Drawing.Size(196, 24);
            this.lbl_MouseClick.TabIndex = 1;
            this.lbl_MouseClick.Text = "mouse click";

            this.Controls.Add(this.lbl_MouseClick);
#endif
            this.BuildInterface();

            this.Paint += new PaintEventHandler(InstrumentSettings_Paint);
        }

#if MOUSE_POSITION
        private void pb_Entrance_MouseDown(object obj, System.Windows.Forms.MouseEventArgs e)
        {
            this.lbl_MouseClick.Text = (e.X - LEFT_EXIT + this.pb_Tube.Left).ToString() + ", " + (e.Y - TOP_EXIT + this.pb_Tube.Top).ToString();
        }
        private void InstrumentSettings_MouseDown(object obj, System.Windows.Forms.MouseEventArgs e)
        {
            this.lbl_MouseClick.Text = (e.X - LEFT_EXIT).ToString() + ", " + (e.Y - TOP_EXIT).ToString();
        }
#endif

        public void set_defaultFragmentationVoltages(double[] voltages)
        {
            if ((voltages != null) && (voltages.Length == 4))
            {
                default_FragmentationVoltages = voltages;
            }
        }

        public void update_Frame(FrameParameters fp)
        {
            // Entrance
            if (fp.voltEntranceCondLmt < 1.0)
            {
                this.tb_Value[0].BackColor = Color.DarkGray;
                this.tb_Value[0].Text = "";
            }
            else
                this.tb_Value[0].Text = fp.voltEntranceCondLmt.ToString("0.00") + " volts";
            if (fp.voltTrapOut < 1.0)
            {
                this.tb_Value[1].BackColor = Color.DarkGray;
                this.tb_Value[1].Text = "";
            }
            else
                this.tb_Value[1].Text = fp.voltTrapOut.ToString("0.00") + " volts";
            if (fp.voltTrapIn < 1.0)
            {
                this.tb_Value[2].BackColor = Color.DarkGray;
                this.tb_Value[2].Text = "";
            }
            else
                this.tb_Value[2].Text = fp.voltTrapIn.ToString("0.00") + " volts";
            if (fp.ESIVoltage < 1.0)
            {
                this.tb_Value[3].BackColor = Color.DarkGray;
                this.tb_Value[3].Text = "";
            }
            else
                this.tb_Value[3].Text = fp.ESIVoltage.ToString("0.00") + " volts";
            /*  if (fp.voltJetDist < 1.0)
              {
                            this.tb_Value[4].BackColor = Color.LightGray;
        this.tb_Value[4].Text = "";
              }
              else
                  this.tb_Value[4].Text = fp.voltJetDist.ToString("0.00") + " volts";
             */
            if (fp.voltCapInlet < 1.0)
            {
                this.tb_Value[4].BackColor = Color.DarkGray;
                this.tb_Value[4].Text = "";
            }
            else
                this.tb_Value[4].Text = fp.voltCapInlet.ToString("0.00") + " volts";
            if (fp.voltEntranceIFTIn < 1.0)
            {
                this.tb_Value[5].BackColor = Color.DarkGray;
                this.tb_Value[5].Text = "";
            }
            else
                this.tb_Value[5].Text = fp.voltEntranceIFTIn.ToString("0.00") + " volts";
            if (fp.voltEntranceIFTOut < 1.0)
            {
                this.tb_Value[6].BackColor = Color.DarkGray;
                this.tb_Value[6].Text = "";
            }
            else
                this.tb_Value[6].Text = fp.voltEntranceIFTOut.ToString("0.00") + " volts";

            // Tube
            if (fp.Temperature < 1.0)
            {
                this.tb_Value[7].BackColor = Color.DarkGray;
                this.tb_Value[7].Text = "";
            }
            else
                this.tb_Value[7].Text = fp.Temperature.ToString("0.0") + " C";
            if (fp.RearIonFunnelPressure < 1.0)
            {
                this.tb_Value[8].BackColor = Color.DarkGray;
                this.tb_Value[8].Text = "";
            }
            else
                this.tb_Value[8].Text = fp.RearIonFunnelPressure.ToString("0.00") + " mTorr";
            if (fp.HighPressureFunnelPressure < 1.0)
            {
                this.tb_Value[9].BackColor = Color.DarkGray;
                this.tb_Value[9].Text = "";
            }
            else
                this.tb_Value[9].Text = fp.HighPressureFunnelPressure.ToString("0.00") + " mTorr";
            if (fp.IonFunnelTrapPressure < 1.0)
            {
                this.tb_Value[10].BackColor = Color.DarkGray;
                this.tb_Value[10].Text = "";
            }
            else
                this.tb_Value[10].Text = fp.IonFunnelTrapPressure.ToString("0.00") + " mTorr";
            if (fp.QuadrupolePressure < 1.0)
            {
                this.tb_Value[11].BackColor = Color.DarkGray;
                this.tb_Value[11].Text = "";
            }
            else
                this.tb_Value[11].Text = fp.QuadrupolePressure.ToString("0.00") + " mTorr";
            if (fp.FloatVoltage < 1.0)
            {
                this.tb_Value[12].BackColor = Color.DarkGray;
                this.tb_Value[12].Text = "";
            }
            else
                this.tb_Value[12].Text = fp.FloatVoltage.ToString("0.00") + " volts";

            // Exit
            if (fp.voltIMSOut < 1.0)
            {
                this.tb_Value[13].BackColor = Color.DarkGray;
                this.tb_Value[13].Text = "";
            }
            else
                this.tb_Value[13].Text = fp.voltIMSOut.ToString("0.00") + " volts";
            if (fp.voltExitIFTIn < 1.0)
            {
                this.tb_Value[14].BackColor = Color.DarkGray;
                this.tb_Value[14].Text = "";
            }
            else
                this.tb_Value[14].Text = fp.voltExitIFTIn.ToString("0.00") + " volts";
            if (fp.voltExitIFTOut < 1.0)
            {
                this.tb_Value[15].BackColor = Color.DarkGray;
                this.tb_Value[15].Text = "";
            }
            else
                this.tb_Value[15].Text = fp.voltExitIFTOut.ToString("0.00") + " volts";
            if (fp.voltExitCondLmt < 1.0)
            {
                this.tb_Value[16].BackColor = Color.DarkGray;
                this.tb_Value[16].Text = "";
            }
            else
                this.tb_Value[16].Text = fp.voltExitCondLmt.ToString("0.00") + " volts";
            if (fp.voltQuad1 < 1.0)
            {
                this.tb_Value[17].BackColor = Color.DarkGray;
                this.tb_Value[17].Text = "";
            }
            else
                this.tb_Value[17].Text = fp.voltQuad1.ToString("0.00") + " volts";
            if (fp.voltCond1 < 1.0)
            {
                this.tb_Value[18].BackColor = Color.DarkGray;
                this.tb_Value[18].Text = "";
            }
            else
                this.tb_Value[18].Text = fp.voltCond1.ToString("0.00") + " volts";
            if (fp.voltQuad2 < 1.0)
            {
                this.tb_Value[19].BackColor = Color.DarkGray;
                this.tb_Value[19].Text = "";
            }
            else
                this.tb_Value[19].Text = fp.voltQuad2.ToString("0.00") + " volts";
            if (fp.voltCond2 < 1.0)
            {
                this.tb_Value[20].BackColor = Color.DarkGray;
                this.tb_Value[20].Text = "";
            }
            else
                this.tb_Value[20].Text = fp.voltCond2.ToString("0.00") + " volts";

            this.tb_Value[21].Text = fp.FrameNum.ToString("0");
            this.tb_Value[22].Text = fp.Scans.ToString("0");
            this.tb_Value[23].Text = fp.Accumulations.ToString("0");
            this.tb_Value[24].Text = fp.TOFLosses.ToString("0") + " scans";

            if (fp.FragmentationProfile != null)
            {
                this.tb_Value[25].Text = fp.FragmentationProfile[0].ToString("0") + " volts";
                this.tb_Value[26].Text = fp.FragmentationProfile[1].ToString("0") + " volts";
                this.tb_Value[27].Text = fp.FragmentationProfile[2].ToString("0") + " volts";
                this.tb_Value[28].Text = fp.FragmentationProfile[3].ToString("0") + " volts";
            }
            else
            {
                this.tb_Value[25].Text = "NA";
                this.tb_Value[26].Text = "NA";
                this.tb_Value[27].Text = "NA";
                this.tb_Value[28].Text = "NA";
            }

            this.tb_Value[29].Text = fp.FrameType.ToString();
            if ((this.default_FragmentationVoltages != null) && (fp.FragmentationProfile != null) && ((fp.FragmentationProfile.Length == 4) && (this.default_FragmentationVoltages.Length == 4)))
                this.tb_Value[30].Text = (fp.FragmentationProfile[0] - this.default_FragmentationVoltages[0]).ToString() + " volts";
            else
                this.tb_Value[30].Text = "N/A";

            this.Resize_This();
        }

        private void BuildInterface()
        {
            this.hilight_brush = new SolidBrush(Color.LightSalmon);
            this.hilight_line = new Pen(hilight_brush, 2);
            this.shadow_brush = new SolidBrush(Color.DarkSlateGray);
            this.shadow_line = new Pen(shadow_brush, 2);

            this.lbl_Desc = new Label[NUM_VALUES];
            this.tb_Value = new TextBox[NUM_VALUES];
            for (int i = 0; i < NUM_VALUES; i++)
            {
                this.lbl_Desc[i] = new Label();
                this.tb_Value[i] = new TextBox();
                // 
                // lbl_Desc
                // 
                this.lbl_Desc[i].Cursor = System.Windows.Forms.Cursors.SizeAll;
                this.lbl_Desc[i].ForeColor = System.Drawing.Color.Black;
                this.lbl_Desc[i].ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
                this.lbl_Desc[i].Location = new System.Drawing.Point(208, 20);
                this.lbl_Desc[i].Name = "lbl_Desc_" + i.ToString("000");
                this.lbl_Desc[i].Size = new System.Drawing.Size(132, 20);
                this.lbl_Desc[i].TabIndex = 1;
                this.lbl_Desc[i].Text = "";
                this.lbl_Desc[i].TextAlign = System.Drawing.ContentAlignment.MiddleRight;
                // 
                // tb_Value
                // 
                this.tb_Value[i].BackColor = System.Drawing.Color.WhiteSmoke;
                this.tb_Value[i].ForeColor = System.Drawing.Color.Black;
                this.tb_Value[i].BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                this.tb_Value[i].Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                this.tb_Value[i].Location = new System.Drawing.Point(344, 16);
                this.tb_Value[i].Name = "lbl_Value";
                this.tb_Value[i].Size = new System.Drawing.Size(95, 24);
                this.tb_Value[i].TabIndex = 2;
                this.tb_Value[i].Text = "volts";
                this.tb_Value[i].TextAlign = HorizontalAlignment.Center;
                this.tb_Value[i].ReadOnly = true;
                this.tb_Value[i].TabStop = false;

                this.Controls.Add(this.lbl_Desc[i]);
                this.Controls.Add(this.tb_Value[i]);
            }
        }

        public void Resize_This()
        {
            this.current_center_width = this.Parent.Width / 2;
            this.current_center_height = this.Parent.Height / 2;

            // Entrance
            this.lbl_Desc[0].Text = "Cond Lmt";
            this.lbl_Desc[0].Top = this.current_center_height - TOP_ENTRANCE + 60;
            this.tb_Value[0].Top = this.lbl_Desc[0].Top - 2;
            this.lbl_Desc[0].Width = 70;
            this.lbl_Desc[0].Left = this.current_center_width - LEFT_ENTRANCE + 600;
            this.tb_Value[0].Left = this.lbl_Desc[0].Left + this.lbl_Desc[0].Width + 5;

            this.lbl_Desc[1].Text = "IFT Out";
            this.lbl_Desc[1].Top = this.current_center_height - TOP_ENTRANCE + 15;
            this.tb_Value[1].Top = this.lbl_Desc[1].Top - 2;
            this.lbl_Desc[1].Left = this.current_center_width - LEFT_ENTRANCE + 90;
            this.tb_Value[1].Left = this.lbl_Desc[1].Left + this.lbl_Desc[1].Width + 5;

            this.lbl_Desc[2].Text = "IFT In";
            this.lbl_Desc[2].Top = this.lbl_Desc[1].Top + this.lbl_Desc[1].Height + 5;
            this.tb_Value[2].Top = this.lbl_Desc[2].Top - 2;
            this.lbl_Desc[2].Left = this.lbl_Desc[1].Left;
            this.tb_Value[2].Left = this.lbl_Desc[2].Left + this.lbl_Desc[2].Width + 5;

            this.lbl_Desc[3].Text = "ESI";
            this.lbl_Desc[3].Top = this.current_center_height - TOP_ENTRANCE + 90;
            this.tb_Value[3].Top = this.lbl_Desc[3].Top - 2;
            this.lbl_Desc[3].Left = this.current_center_width - LEFT_ENTRANCE;
            this.tb_Value[3].Left = this.lbl_Desc[3].Left + this.lbl_Desc[3].Width + 5;

            this.lbl_Desc[4].Text = "Analyte Inlet";
            this.lbl_Desc[4].Top = this.current_center_height - TOP_ENTRANCE + 140;
            this.tb_Value[4].Top = this.lbl_Desc[4].Top - 2;
            this.lbl_Desc[4].Left = this.current_center_width - LEFT_ENTRANCE + 45;
            this.tb_Value[4].Left = this.lbl_Desc[4].Left + this.lbl_Desc[4].Width + 5;

            this.lbl_Desc[5].Text = "HP Funnel In";
            this.lbl_Desc[5].Top = this.current_center_height - TOP_ENTRANCE + 180;
            this.tb_Value[5].Top = this.lbl_Desc[5].Top - 2;
            this.lbl_Desc[5].Left = this.current_center_width - LEFT_ENTRANCE + 85;
            this.tb_Value[5].Left = this.lbl_Desc[5].Left + this.lbl_Desc[5].Width + 5;

            this.lbl_Desc[6].Text = "HP Funnel Out";
            this.lbl_Desc[6].Top = this.lbl_Desc[5].Top + this.lbl_Desc[5].Height + 5;
            this.tb_Value[6].Top = this.lbl_Desc[6].Top - 2;
            this.lbl_Desc[6].Left = this.lbl_Desc[5].Left;
            this.tb_Value[6].Left = this.lbl_Desc[6].Left + this.lbl_Desc[6].Width + 5;

            this.lbl_Desc[7].Text = "Temperature";
            this.lbl_Desc[7].Top = this.current_center_height - TOP_ENTRANCE + 200;
            this.tb_Value[7].Top = this.lbl_Desc[7].Top - 2;
            this.lbl_Desc[7].Left = this.current_center_width - LEFT_ENTRANCE + 490;
            this.tb_Value[7].Left = this.lbl_Desc[7].Left + this.lbl_Desc[7].Width + 5;
            this.tb_Value[7].ForeColor = Color.Red;

            // Tube
            this.lbl_Desc[8].Text = "Rear Ion Funnel Pressure";
            this.lbl_Desc[8].Top = this.current_center_height - TOP_TUBE - 5;
            this.tb_Value[8].Top = this.lbl_Desc[8].Top - 2;
            this.lbl_Desc[8].Width = 230;
            this.lbl_Desc[8].Left = this.current_center_width - LEFT_TUBE + 170;
            this.tb_Value[8].Left = this.lbl_Desc[8].Left + this.lbl_Desc[8].Width + 5;
            this.tb_Value[8].ForeColor = Color.Blue;

            this.lbl_Desc[9].Text = "HP Funnel Pressure";
            this.lbl_Desc[9].Top = this.current_center_height - TOP_TUBE + 25;
            this.tb_Value[9].Top = this.lbl_Desc[9].Top - 2;
            this.lbl_Desc[9].Left = this.current_center_width - LEFT_TUBE + 85;
            this.tb_Value[9].Left = this.lbl_Desc[9].Left + this.lbl_Desc[9].Width + 5;
            this.tb_Value[9].ForeColor = Color.Blue;

            this.lbl_Desc[10].Text = "Ion Funnel Trap Pressure";
            this.lbl_Desc[10].Top = this.current_center_height - TOP_TUBE + 150;
            this.tb_Value[10].Top = this.lbl_Desc[10].Top - 2;
            this.lbl_Desc[10].Width = 170;
            this.lbl_Desc[10].Left = this.current_center_width - LEFT_TUBE + 160;
            this.tb_Value[10].Left = this.lbl_Desc[10].Left + this.lbl_Desc[10].Width + 5;
            this.tb_Value[10].ForeColor = Color.Blue;

            this.lbl_Desc[11].Text = "Quads Pressure";
            this.lbl_Desc[11].Top = this.current_center_height - TOP_TUBE + 120;
            this.tb_Value[11].Top = this.lbl_Desc[11].Top - 2;
            this.lbl_Desc[11].Left = this.current_center_width - LEFT_TUBE + 390;
            this.tb_Value[11].Left = this.lbl_Desc[11].Left + this.lbl_Desc[11].Width + 5;
            this.tb_Value[11].ForeColor = Color.Blue;

            this.lbl_Desc[12].Text = "Float Voltage";
            this.lbl_Desc[12].Top = this.current_center_height - TOP_TUBE + 185;
            this.tb_Value[12].Top = this.lbl_Desc[12].Top - 2;
            this.lbl_Desc[12].Width = 90;
            this.lbl_Desc[12].Left = this.current_center_width - LEFT_TUBE + 115;
            this.tb_Value[12].Left = this.lbl_Desc[12].Left + this.lbl_Desc[12].Width + 5;

            // exit
            this.lbl_Desc[13].Text = "IMS Out";
            this.lbl_Desc[13].Top = this.current_center_height - TOP_EXIT + 15;
            this.tb_Value[13].Top = this.lbl_Desc[13].Top - 2;
            this.lbl_Desc[13].Width = 80;
            this.lbl_Desc[13].Left = this.current_center_width - LEFT_EXIT - 210;
            this.tb_Value[13].Left = this.lbl_Desc[13].Left + this.lbl_Desc[13].Width + 5;

            this.lbl_Desc[14].Text = "RIF In";
            this.lbl_Desc[14].Top = this.current_center_height - TOP_EXIT - 25;
            this.tb_Value[14].Top = this.lbl_Desc[14].Top - 2;
            this.lbl_Desc[14].Width = 50;
            this.lbl_Desc[14].Left = this.current_center_width - LEFT_EXIT + 220;
            this.tb_Value[14].Left = this.lbl_Desc[14].Left + this.lbl_Desc[14].Width + 5;

            this.lbl_Desc[15].Text = "RIF Out";
            this.lbl_Desc[15].Top = this.current_center_height - TOP_EXIT + 5;
            this.tb_Value[15].Top = this.lbl_Desc[15].Top - 2;
            this.lbl_Desc[15].Width = 60;
            this.lbl_Desc[15].Left = this.current_center_width - LEFT_EXIT + 210;
            this.tb_Value[15].Left = this.lbl_Desc[15].Left + this.lbl_Desc[15].Width + 5;

            this.lbl_Desc[16].Text = "Cond Limit";
            this.lbl_Desc[16].Top = this.current_center_height - TOP_EXIT + 80;
            this.tb_Value[16].Top = this.lbl_Desc[16].Top - 2;
            this.lbl_Desc[16].Width = 75;
            this.lbl_Desc[16].Left = this.current_center_width - LEFT_EXIT + 240;
            this.tb_Value[16].Left = this.lbl_Desc[16].Left + this.lbl_Desc[16].Width + 5;

            this.lbl_Desc[17].Text = "Quad 1";
            this.lbl_Desc[17].Top = this.current_center_height - TOP_EXIT + 60;
            this.tb_Value[17].Top = this.lbl_Desc[17].Top - 2;
            this.lbl_Desc[17].Left = this.current_center_width - LEFT_EXIT - 300;
            this.tb_Value[17].Left = this.lbl_Desc[17].Left + this.lbl_Desc[17].Width + 5;

            this.lbl_Desc[18].Text = "Cond 1";
            this.lbl_Desc[18].Top = this.current_center_height - TOP_EXIT + 85;
            this.tb_Value[18].Top = this.lbl_Desc[18].Top - 2;
            this.lbl_Desc[18].Left = this.current_center_width - LEFT_EXIT - 300;
            this.tb_Value[18].Left = this.lbl_Desc[18].Left + this.lbl_Desc[18].Width + 5;

            this.lbl_Desc[19].Text = "Quad 2";
            this.lbl_Desc[19].Top = this.current_center_height - TOP_EXIT + 115;
            this.tb_Value[19].Top = this.lbl_Desc[19].Top - 2;
            this.lbl_Desc[19].Left = this.current_center_width - LEFT_EXIT - 300;
            this.tb_Value[19].Left = this.lbl_Desc[19].Left + this.lbl_Desc[19].Width + 5;

            this.lbl_Desc[20].Text = "Cond 2";
            this.lbl_Desc[20].Top = this.current_center_height - TOP_EXIT + 140;
            this.tb_Value[20].Top = this.lbl_Desc[20].Top - 2;
            this.lbl_Desc[20].Left = this.current_center_width - LEFT_EXIT - 300;
            this.tb_Value[20].Left = this.lbl_Desc[20].Left + this.lbl_Desc[20].Width + 5;

            // Settings
            this.lbl_Desc[21].Text = "Frame Number";
            this.lbl_Desc[21].Top = this.current_center_height - TOP_SETTINGS;
            this.tb_Value[21].Top = this.lbl_Desc[21].Top - 2;
            this.lbl_Desc[21].Left = this.current_center_width - LEFT_SETTINGS - 370;
            this.tb_Value[21].Left = this.lbl_Desc[21].Left + this.lbl_Desc[21].Width + 5;

            this.lbl_Desc[22].Text = "Scans";
            this.lbl_Desc[22].Top = this.current_center_height - TOP_SETTINGS + 30;
            this.tb_Value[22].Top = this.lbl_Desc[22].Top - 2;
            this.lbl_Desc[22].Left = this.current_center_width - LEFT_SETTINGS - 370;
            this.tb_Value[22].Left = this.lbl_Desc[22].Left + this.lbl_Desc[22].Width + 5;

            this.lbl_Desc[23].Text = "Accumulations";
            this.lbl_Desc[23].Top = this.current_center_height - TOP_SETTINGS + 55;
            this.tb_Value[23].Top = this.lbl_Desc[23].Top - 2;
            this.lbl_Desc[23].Left = this.current_center_width - LEFT_SETTINGS - 370;
            this.tb_Value[23].Left = this.lbl_Desc[23].Left + this.lbl_Desc[23].Width + 5;

            this.lbl_Desc[24].Text = "TOF Losses";
            this.lbl_Desc[24].Top = this.current_center_height - TOP_SETTINGS + 85;
            this.tb_Value[24].Top = this.lbl_Desc[24].Top - 2;
            this.lbl_Desc[24].Left = this.current_center_width - LEFT_SETTINGS - 370;
            this.tb_Value[24].Left = this.lbl_Desc[24].Left + this.lbl_Desc[24].Width + 5;

            // Fragmentation
            this.lbl_Desc[25].Text = "Frag Ch1";
            this.lbl_Desc[25].Top = this.current_center_height - TOP_SETTINGS + 25;
            this.tb_Value[25].Top = this.lbl_Desc[25].Top - 2;
            this.lbl_Desc[25].Left = this.current_center_width - LEFT_SETTINGS - 45;
            this.tb_Value[25].Left = this.lbl_Desc[25].Left + this.lbl_Desc[25].Width + 5;

            this.lbl_Desc[26].Text = "Frag Ch2";
            this.lbl_Desc[26].Top = this.current_center_height - TOP_SETTINGS + 50;
            this.tb_Value[26].Top = this.lbl_Desc[26].Top - 2;
            this.lbl_Desc[26].Left = this.current_center_width - LEFT_SETTINGS - 45;
            this.tb_Value[26].Left = this.lbl_Desc[26].Left + this.lbl_Desc[26].Width + 5;

            this.lbl_Desc[27].Text = "Frag Ch3";
            this.lbl_Desc[27].Top = this.current_center_height - TOP_SETTINGS + 75;
            this.tb_Value[27].Top = this.lbl_Desc[27].Top - 2;
            this.lbl_Desc[27].Left = this.current_center_width - LEFT_SETTINGS - 45;
            this.tb_Value[27].Left = this.lbl_Desc[27].Left + this.lbl_Desc[27].Width + 5;

            this.lbl_Desc[28].Text = "Frag Ch4";
            this.lbl_Desc[28].Top = this.current_center_height - TOP_SETTINGS + 100;
            this.tb_Value[28].Top = this.lbl_Desc[28].Top - 2;
            this.lbl_Desc[28].Left = this.current_center_width - LEFT_SETTINGS - 45;
            this.tb_Value[28].Left = this.lbl_Desc[28].Left + this.lbl_Desc[28].Width + 5;

            this.lbl_Desc[29].Text = "Frame Type";
            this.lbl_Desc[29].Top = this.current_center_height - TOP_SETTINGS - 10;
            this.tb_Value[29].Top = this.lbl_Desc[29].Top - 2;
            this.lbl_Desc[29].Left = this.current_center_width - LEFT_SETTINGS+10;
            this.tb_Value[29].Left = this.lbl_Desc[29].Left + this.lbl_Desc[29].Width + 5;

            this.lbl_Desc[30].Text = "CE";
            this.lbl_Desc[30].Top = this.current_center_height - TOP_SETTINGS + 25;
            this.tb_Value[30].Top = this.lbl_Desc[30].Top - 2;
            this.lbl_Desc[30].Left = this.lbl_Desc[25].Left + this.lbl_Desc[25].Width + 10;
            this.tb_Value[30].Left = this.lbl_Desc[30].Left + this.lbl_Desc[30].Width + 5;
        }

        public void InstrumentSettings_Paint(object obj, PaintEventArgs e)
        {
            int left_entrance;
            int top_entrance;
            int left_tube;
            int top_tube;
            int left_exit;
            int top_exit;

            this.pnl_graphics = this.CreateGraphics();
            this.pb_Entrance.Hide();
            this.pb_Tube.Hide();
            this.pb_Exit.Hide();

            // Entrance
            left_entrance = current_center_width - LEFT_ENTRANCE + 330;
            top_entrance = this.current_center_height - TOP_ENTRANCE + 30;
            this.pnl_graphics.DrawImage(this.pb_Entrance.Image, left_entrance, top_entrance);

            this.pnl_graphics.DrawLine(shadow_line, this.lbl_Desc[0].Left - 5, this.tb_Value[0].Top + 11, left_entrance + 213, top_entrance + 78);
            this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[1].Left + this.tb_Value[1].Width + 5, this.tb_Value[1].Top + 11, left_entrance + 204, top_entrance + 78);
            this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[2].Left + this.tb_Value[2].Width + 5, this.tb_Value[2].Top + 11, left_entrance + 135, top_entrance + 80);
            // this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[3].Left + this.tb_Value[3].Width + 5, this.tb_Value[3].Top + 11, LEFT_ENTRANCE + 507, TOP_ENTRANCE + 108);
            this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[4].Left + this.tb_Value[4].Width + 5, this.tb_Value[4].Top + 11, left_entrance, top_entrance + 96);
            this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[5].Left + this.tb_Value[5].Width + 5, this.tb_Value[5].Top + 11, left_entrance + 50, top_entrance + 83);
            this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[6].Left + this.tb_Value[6].Width + 5, this.tb_Value[6].Top + 11, left_entrance + 128, top_entrance + 83);

            this.pnl_graphics.DrawLine(hilight_line, this.lbl_Desc[0].Left - 5, this.tb_Value[0].Top + 10, left_entrance + 212, top_entrance + 77);
            this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[1].Left + this.tb_Value[1].Width + 5, this.tb_Value[1].Top + 10, left_entrance + 204, top_entrance + 77);
            this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[2].Left + this.tb_Value[2].Width + 5, this.tb_Value[2].Top + 10, left_entrance + 135, top_entrance + 79);
            // this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[3].Left + this.tb_Value[3].Width + 5, this.tb_Value[3].Top + 10, LEFT_ENTRANCE + 507, TOP_ENTRANCE + 107);
            this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[4].Left + this.tb_Value[4].Width + 5, this.tb_Value[4].Top + 10, left_entrance, top_entrance + 95);
            this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[5].Left + this.tb_Value[5].Width + 5, this.tb_Value[5].Top + 10, left_entrance + 50, top_entrance + 82);
            this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[6].Left + this.tb_Value[6].Width + 5, this.tb_Value[6].Top + 10, left_entrance + 128, top_entrance + 82);

            // Tube
            left_tube = this.current_center_width - LEFT_TUBE;
            top_tube = this.current_center_height - TOP_TUBE;
            this.pnl_graphics.DrawImage(this.pb_Tube.Image, left_tube, top_tube);

            this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[8].Left + this.tb_Value[8].Width + 5, this.tb_Value[8].Top + 11, left_tube + 575, top_tube + 65);
            this.pnl_graphics.DrawLine(shadow_line, this.lbl_Desc[9].Left - 5, this.tb_Value[9].Top + 11, left_tube + 50, top_tube + 104);
            this.pnl_graphics.DrawLine(shadow_line, this.lbl_Desc[10].Left - 5, this.tb_Value[10].Top + 11, left_tube + 130, top_tube + 137);
            this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[11].Left + this.tb_Value[11].Width + 5, this.tb_Value[11].Top + 11, left_tube + 650, top_tube + 70);
            this.pnl_graphics.DrawLine(shadow_line, this.lbl_Desc[12].Left - 5, this.tb_Value[12].Top + 11, left_tube + 45, top_tube + 155);

            this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[8].Left + this.tb_Value[8].Width + 5, this.tb_Value[8].Top + 10, left_tube + 575, top_tube + 64);
            this.pnl_graphics.DrawLine(hilight_line, this.lbl_Desc[9].Left - 5, this.tb_Value[9].Top + 10, left_tube + 50, top_tube + 103);
            this.pnl_graphics.DrawLine(hilight_line, this.lbl_Desc[10].Left - 5, this.tb_Value[10].Top + 10, left_tube + 130, top_tube + 136);
            this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[11].Left + this.tb_Value[11].Width + 5, this.tb_Value[11].Top + 10, left_tube + 650, top_tube + 69);
            this.pnl_graphics.DrawLine(hilight_line, this.lbl_Desc[12].Left - 5, this.tb_Value[12].Top + 10, left_tube + 45, top_tube + 154);

            // Exit
            left_exit = this.current_center_width - LEFT_EXIT;
            top_exit = this.current_center_height - TOP_EXIT;
            this.pnl_graphics.DrawImage(this.pb_Exit.Image, left_exit, top_exit);

            this.pnl_graphics.DrawLine(shadow_line, this.tb_Value[13].Left + this.tb_Value[13].Width + 5, this.tb_Value[13].Top + 11, left_exit+15, top_exit+80);
            this.pnl_graphics.DrawLine(shadow_line, this.lbl_Desc[14].Left + 10, this.tb_Value[14].Top + 11, left_exit+20, top_exit+80);
            this.pnl_graphics.DrawLine(shadow_line, this.lbl_Desc[15].Left - 5, this.tb_Value[15].Top + 11, left_exit+97, top_exit+76);
            this.pnl_graphics.DrawLine(shadow_line, this.lbl_Desc[16].Left - 5, this.tb_Value[16].Top + 11, left_exit+187, top_exit + 72);

            this.pnl_graphics.DrawLine(hilight_line, this.tb_Value[13].Left + this.tb_Value[13].Width + 5, this.tb_Value[13].Top + 10, left_exit+15, top_exit+79);
            this.pnl_graphics.DrawLine(hilight_line, this.lbl_Desc[14].Left + 10, this.tb_Value[14].Top + 10, left_exit+20, top_exit+79);
            this.pnl_graphics.DrawLine(hilight_line, this.lbl_Desc[15].Left - 5, this.tb_Value[15].Top + 10, left_exit+97, top_exit+75);
            this.pnl_graphics.DrawLine(hilight_line, this.lbl_Desc[16].Left - 5, this.tb_Value[16].Top + 10, left_exit+187, top_exit+71);
        }
    }
}
