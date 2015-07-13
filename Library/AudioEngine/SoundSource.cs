using System;
using OpenTK;
using System.Collections.Generic;
using OpenTK.Audio.OpenAL;

namespace AudioEngine
{
	public class SoundSource
	{
		public int Instance { get; set;	}
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }
		public int mSource;

		public void Initialize()
		{
			mTracks = new List<ISoundTrack> ();
			mSource = AL.GenSource ();
		}

		public List<ISoundTrack> mTracks;
		public void Add(ISoundTrack track)
		{
			mTracks.Add (track);

		}
	}
}

