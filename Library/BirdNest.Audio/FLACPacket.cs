namespace BirdNest.Audio
{
	public class FLACPacket
	{
		public int SampleRate;
		public int Channels;
		public int BlockSize;
		public byte[] Data;
		public int Offset;
	}
}

