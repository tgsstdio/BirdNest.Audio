using System.Collections.Concurrent;

namespace BirdNest.Audio
{
	public class FLACPacketQueue : IFLACPacketQueue
	{
		private readonly ConcurrentQueue<FLACPacket> mQueue;
		public FLACPacketQueue ()
		{
			mQueue = new ConcurrentQueue<FLACPacket> ();
		}

		#region ISoundPacketQueue implementation

		public void Enqueue (FLACPacket packet)
		{
			mQueue.Enqueue (packet);
		}

		public bool TryPeek (out FLACPacket packet)
		{
			return mQueue.TryPeek (out packet);
		}

		public bool TryDequeue (out FLACPacket packet)
		{
			return mQueue.TryDequeue (out packet);
		}

		public bool IsEmpty()
		{
			return mQueue.IsEmpty;
		}

		#endregion
	}
}

