using System;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace IDLTools
{
	/// <summary>
	/// Summary description for App.
	/// </summary>
	public class App
	{
		[DllImport("kernel32.dll")]
		public static extern bool Beep(int freq,int duration);

		public static bool EmulationMode = true;

		public static void Error(Exception e)
		{
			try
			{
				MessageBox.Show (e.ToString());
			}
			catch{}
		}

		public static void Delay(int mSec)
		{
			DateTime start = System.DateTime.Now;
			while(true)
			{
				Application.DoEvents();
				DateTime elapsed = System.DateTime.Now;

				System.TimeSpan time = elapsed - start;
					
				if(time.TotalMilliseconds > mSec)
				{
					return;
				}
			}
		}

		private static string homedir = "";
		public static string HomeDir 
		{
			get {return homedir;}
			set {homedir = value;}
		}


		public App()
		{
			//
			// TODO: Add constructor logic here
			//
		}
	}
}
