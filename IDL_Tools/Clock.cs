using System;

namespace IDLTools
{
	/// <summary>
	/// Summary description for Clock.
	/// </summary>
	public class Clock
	{
		private DateTime startTime = DateTime.Now;
		private DateTime thisTime = DateTime.Now;

		private TimeSpan delta;

		public Clock()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public void Reset()
		{
			startTime = DateTime.Now;
		}

		public void Stop()
		{
			thisTime = DateTime.Now;
		}

		public double Milliseconds()
		{
			delta = thisTime-startTime;
			return (delta.TotalMilliseconds);
		}
	}
}
