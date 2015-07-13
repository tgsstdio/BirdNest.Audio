using System;
using System.Collections.Generic;

namespace AudioEngine
{
	public class TimeToEndCutoffComparer : IComparer<IEffect>
	{
		#region IComparer implementation
		public int Compare (IEffect x, IEffect y)
		{
			if (x.TimeToEnd <= y.TimeToEnd)
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

