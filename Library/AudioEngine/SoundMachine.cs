using System;
using OpenTK.Audio;

namespace AudioEngine
{
	public class SoundMachine : ISoundMachine
	{
		public AudioContext[] Contexts {get; private set;}
		public SoundMachine (AudioContext[] contexts)
		{
			this.Contexts = contexts;
		}
	}
}

