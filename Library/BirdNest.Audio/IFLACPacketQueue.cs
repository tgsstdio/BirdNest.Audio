namespace BirdNest.Audio
{
	public interface IFLACPacketQueue
	{
		void Enqueue(FLACPacket packet);
		bool TryDequeue(out FLACPacket packet);
		bool TryPeek (out FLACPacket packet);
		bool IsEmpty();
	}
}

