using System;

namespace BigMansStuff.NAudio.FLAC
{
	public interface ISoundPacketQueue
	{
		void Enqueue(SoundPacket packet);
		bool TryDequeue(out SoundPacket packet);
		bool IsEmpty();
	}
}

