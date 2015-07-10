using System;
using OpenTK.Audio.OpenAL;

namespace AudioEngine
{
	public class SoundTrack : ISoundTrack
	{
		public SoundTrack ()
		{
		}

		private int mBuffer;
		public void Initialise()
		{
			mBuffer = AL.GenBuffer ();	
		}
	}
}

