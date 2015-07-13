using System;
using System.Collections.Generic;

namespace AudioEngine
{
	public class TimeToStartCutoffComparer : IComparer<IEffect>
	{
		#region IComparer implementation
		public int Compare (IEffect x, IEffect y)
		{
			if (x.TimeToStart <= y.TimeToStart)
			{
				return -1;
			} 
			else 
			{
				return 1;
			} 
		}
		#endregion
	}
}

