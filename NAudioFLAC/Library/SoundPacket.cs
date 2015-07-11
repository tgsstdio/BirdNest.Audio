using System;
using OpenTK.Audio.OpenAL;

namespace BigMansStuff.NAudio.FLAC
{
	public class SoundPacket
	{
		public ALFormat Format;
		public int SampleRate;
		public int Channels;
		public int BlockSize;
		public byte[] Data;
	}
}

