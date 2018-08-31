using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace UIMF_File.Utilities
{
	/// <summary>
	/// Summary description for VerticalLabel.
	/// </summary>
	public class VerticalLabel : System.Windows.Forms.Label
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public VerticalLabel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

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

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// VerticalLabel
			//
			this.Text = "Hello there";

		}
		#endregion

		protected override void OnPaint(PaintEventArgs e)
		{
			//base.OnPaint (e);
			Graphics g = e.Graphics;
			//g.TranslateTransform(50.0f, 50.0f);
			System.Drawing.SizeF s = e.Graphics.MeasureString(this.Text, this.Font);
			g.RotateTransform(270.0f);
			g.TranslateTransform(-s.Width, 0);
			g.DrawString(this.Text, this.Font, System.Drawing.Brushes.Black, 0.0f, 0.0f);
		}

	}
}











