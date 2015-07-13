using NUnit.Framework;
using System;

namespace BirdNest.Audio.UnitTests
{
	[TestFixture ()]
	public class Test
	{
		[Test ()]
		public void TestCase ()
		{
			ArrayPool<byte> pool = new ArrayPool<byte> ();
			byte[] buffer; 
			bool isNewArray = pool.Take (10, out buffer);
			Assert.IsTrue (isNewArray);
			pool.Release (buffer);
			byte[] second;
			isNewArray = pool.Take (5, out second);			
			Assert.IsFalse (isNewArray);
			Assert.AreSame (buffer, second);
		}

		[Test ()]
		public void InitTest ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();
			Assert.IsNull (root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);
			Assert.IsNull (root.Left);
			Assert.IsNull (root.Right);
		}

		[Test ()]
		public void AddOne ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();
			Assert.IsNull (root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);

			var first = new byte[5];
			bool result = root.Reserve (first);
			Assert.AreSame (first, root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);
		}

		[Test ()]
		public void Add6Then5 ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();
			Assert.IsNull (root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);

			var second = new byte[6];
			root.Reserve (second);

			var first = new byte[5];
			var result = root.Reserve (first);
			Assert.IsTrue (result);
			Assert.AreSame (first, root.Data_0);
			Assert.AreSame (second, root.Data_1);
		}

		[Test ()]
		public void Add6Then5Then6 ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();
			Assert.IsNull (root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);

			var second = new byte[6];
			root.Reserve (second);

			var first = new byte[5];
			root.Reserve (first);

			var third = new byte[6];
			var result = root.Reserve (third);
			Assert.IsTrue (result);
			Assert.AreSame (first, root.Data_0);
			Assert.AreSame (second, root.Data_1);
			Assert.AreSame (third, root.Data_2);
		}

		[Test ()]
		public void TooFullOver ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();
			Assert.IsNull (root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);

			var second = new byte[6];
			root.Reserve (second);

			var first = new byte[5];
			root.Reserve (first);

			var third = new byte[6];
			root.Reserve (third);

			var fourth = new byte[7];
			var result = root.Reserve (fourth);
			Assert.IsFalse (result);
			Assert.AreSame (first, root.Data_0);
			Assert.AreSame (second, root.Data_1);
			Assert.AreSame (third, root.Data_2);
		}

		[Test ()]
		public void TooFullSame ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();
			Assert.IsNull (root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);

			var second = new byte[6];
			root.Reserve (second);

			var first = new byte[5];
			root.Reserve (first);

			var third = new byte[6];
			root.Reserve (third);

			var fourth = new byte[6];
			var result = root.Reserve (fourth);
			Assert.IsFalse (result);
			Assert.AreSame (first, root.Data_0);
			Assert.AreSame (second, root.Data_1);
			Assert.AreSame (third, root.Data_2);
		}

		[Test ()]
		public void TooFullUnder ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();
			Assert.IsNull (root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);

			var second = new byte[6];
			root.Reserve (second);

			var first = new byte[5];
			root.Reserve (first);

			var third = new byte[6];
			root.Reserve (third);

			var fourth = new byte[4];
			var result = root.Reserve (fourth);
			Assert.IsFalse (result);
			Assert.AreSame (first, root.Data_0);
			Assert.AreSame (second, root.Data_1);
			Assert.AreSame (third, root.Data_2);
		}

		[Test ()]
		public void Reset ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();
			Assert.IsTrue (root.IsEmpty ());

			var first = new byte[6];
			root.Reserve (first);

			Assert.IsFalse (root.IsEmpty ());
			root.Reset ();
			Assert.IsTrue (root.IsEmpty ());
			Assert.AreEqual (0, root.Lowest);
			Assert.AreEqual (0, root.Highest);
			Assert.IsNull (root.Data_0);
			Assert.IsNull (root.Data_1);
			Assert.IsNull (root.Data_2);
			Assert.IsNull (root.Left);
			Assert.IsNull (root.Right);
		}

		static ArrayPoolNode<byte> Split (ArrayPoolNode<byte> root, byte[] low)
		{
			ArrayPoolNode<byte> left = null;
			if (root.Under (low.Length))
			{
				left = new ArrayPoolNode<byte> ();
				root.Left = left;
				left.Right = root;
				left.Reserve (low);
			}
			else
			{
				if (low.Length >= root.Highest)
				{
					left = new ArrayPoolNode<byte> ();

				}
			}
			return left;
		}

		[Test ()]
		public void SplitLow ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();

			var first = new byte[4];
			root.Reserve (first);

			var second = new byte[5];
			root.Reserve (second);

			var third = new byte[6];
			root.Reserve (third);

			Assert.AreSame (first, root.Data_0);
			Assert.AreSame (second, root.Data_1);
			Assert.AreSame (third, root.Data_2);

			var low = new byte[3];

			var left = Split (root, low);

			Assert.IsNotNull (left);
			Assert.IsNotNull (root.Left);
			Assert.AreEqual (low, left.Data_0);
			Assert.AreEqual (left, root.Left);
			Assert.IsNull (left.Left);
			Assert.IsNull (root.Right);
			Assert.IsNull (left.Data_1);
			Assert.IsNull (left.Data_2);
		}

		[Test ()]
		public void SplitMid ()
		{
			ArrayPoolNode<byte> root = new ArrayPoolNode<byte> ();

			var first = new byte[4];
			root.Reserve (first);

			var second = new byte[5];
			root.Reserve (second);

			var third = new byte[6];
			root.Reserve (third);

			Assert.AreSame (first, root.Data_0);
			Assert.AreSame (second, root.Data_1);
			Assert.AreSame (third, root.Data_2);

			var mid = new byte[5];

			var left = Split (root, mid);

			Assert.IsNotNull (left);
			Assert.IsNotNull (root.Left);
			Assert.AreEqual (mid, left.Data_0);
			Assert.AreEqual (left, root.Left);
			Assert.IsNull (left.Left);
			Assert.IsNull (root.Right);
			Assert.IsNull (left.Data_1);
			Assert.IsNull (left.Data_2);
		}
	}
}

