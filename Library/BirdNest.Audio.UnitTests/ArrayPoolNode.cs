using System;

namespace BirdNest.Audio.UnitTests
{
	public class ArrayPoolNode<TClass>
	{		
		public ArrayPoolNode<TClass> Left;

		public int Lowest;
		public TClass[] Data_0;
		public TClass[] Data_1;
		public TClass[] Data_2;
		public int Highest;

		public ArrayPoolNode<TClass> Right;

		public bool Reserve(TClass[] buffer)
		{
			if (Data_0 == null)
			{
				Data_0 = buffer;
				Lowest = buffer.Length;
				Highest = buffer.Length;
				return true;
			} 
			else if (Data_1 == null)
			{
				if (Data_0.Length < buffer.Length)
				{
					Data_1 = buffer;
					Highest = buffer.Length;
					return true;
				} 
				else
				{
					Data_1 = Data_0;
					Data_0 = buffer;
					Lowest = buffer.Length;
					return true;
				}			
			} 
			else if (Data_2 == null)
			{
				if (buffer.Length < Data_0.Length)
				{
					Data_2 = Data_1;
					Data_1 = Data_0;
					Data_0 = buffer;
					Lowest = buffer.Length;
					return true;
				} 
				else if (buffer.Length < Data_1.Length)
				{
					Data_2 = Data_1;
					Data_1 = buffer;
					return true;
				} 
				else
				{
					Data_2 = buffer;
					Highest = buffer.Length;
					return true;
				}				
			}
			else
			{
				return false;
			}
		}

		public bool Take(int minimum, out TClass[] result)
		{
			if (Highest < minimum)
			{
				result = null;
				return false;
			}
			else
			{
				if (Data_0 == null)
				{
					result = null;
					return false;
				}
				else if (Data_0.Length >= minimum)
				{
					result = Data_0;
					Data_0 = Data_1;
					Data_1 = Data_2;
					Lowest = Data_0.Length;
					return true;
				}
				else if (Data_1 != null && Data_1.Length >= minimum)
				{					
					result = Data_1;
					Data_1 = Data_2;
					Data_2 = null;
					return true;
				}
				else if (Data_2 != null && Data_2.Length >= minimum)
				{
					result = Data_1;
					Data_1 = Data_2;
					Data_2 = null;
					return true;
				} 
				else
				{
					result = null;
					return false;
				}
			}
		}

		public bool Under(int minimum)
		{
			return minimum < Lowest;
		}

		public bool InBetween(int minimum)
		{
			return minimum < Highest && minimum >= Lowest;
		}

		public bool IsEmpty()
		{
			return Data_0 == null && Data_1 == null && Data_2 == null;
		}

		public void Reset()
		{
			Data_0 = null;
			Data_1 = null;
			Data_2 = null;
			Left = null;
			Right = null;
			Lowest = 0;
			Highest = 0;
		}
	}
}

