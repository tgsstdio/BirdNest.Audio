using System;
using OpenTK.Audio.OpenAL;

namespace BirdNest.Audio
{
	public class WaveInformation
	{
		/// <summary>
		/// Code taken from NAudio.WaveFormat
		/// Initializes a new instance of the <see cref="BigMansStuff.NAudio.FLAC.WaveInformation"/> class.
		/// </summary>
		/// <param name="rate">Rate.</param>
		/// <param name="bits">Bits.</param>
		/// <param name="channels">Channels.</param>
		public WaveInformation(int rate, int bits, int channels)
		{
			if (channels < 1)
			{
				throw new ArgumentOutOfRangeException("channels", "Channels must be 1 or greater");
			}
			this.channels = (short)channels;
			this.sampleRate = rate;
			this.bitsPerSample = (short)bits;
			this.extraSize = 0;

			this.BlockAlign = (short)(channels * (bits / 8));
			this.averageBytesPerSecond = this.sampleRate * this.BlockAlign;

			if (channels == 1)
			{
				switch (bitsPerSample)
				{
					case 8:
						sound_format = ALFormat.Mono8;
						break;
					case 16:
						sound_format = ALFormat.Mono16;
						break;					
					default:
					sound_format = (ALFormat) 0;
						break;
				}					
			}
			else if (channels == 2)
			{
				switch (bitsPerSample)
				{
				case 8:
					sound_format = ALFormat.Stereo8;
					break;
				case 16:
					sound_format = ALFormat.Stereo16;
					break;					
				default:
					sound_format = (ALFormat) 0;
					break;
				}
			}

			// minimum 16 bytes, sometimes 18 for PCM

		}

		//this.waveFormatTag = WaveFormatEncoding.Pcm;
		public ALFormat sound_format {get;private set;}

		/// <summary>number of following bytes</summary>
		protected short extraSize;

		/// <summary>
		/// Returns the number of channels (1=mono,2=stereo etc)
		/// </summary>
		public short channels;

		/// <summary>
		/// Returns the block alignment
		/// </summary>
		public int BlockAlign {get; private set;}

		/// <summary>
		/// Returns the number of bits per sample (usually 16 or 32, sometimes 24 or 8)
		/// Can be 0 for some codecs
		/// </summary>
		public int bitsPerSample;

		/// <summary>
		/// Returns the average number of bytes used per second
		/// </summary>
		public int averageBytesPerSecond;

		/// <summary>
		/// Returns the sample rate (samples per second)
		/// </summary>
		public int sampleRate { get; private set;}
	}
}

