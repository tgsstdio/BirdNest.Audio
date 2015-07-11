using System;
using System.Collections.Concurrent;

namespace BigMansStuff.NAudio.FLAC
{
	public class SoundPacketQueue : ISoundPacketQueue
	{
		private readonly ConcurrentQueue<SoundPacket> mQueue;
		public SoundPacketQueue ()
		{
			mQueue = new ConcurrentQueue<SoundPacket> ();
		}

		#region ISoundPacketQueue implementation

		public void Enqueue (SoundPacket packet)
		{
			mQueue.Enqueue (packet);
		}

		public bool TryDequeue (out SoundPacket packet)
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

