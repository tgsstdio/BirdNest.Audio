using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Input;
using OpenTK.Audio.OpenAL;
using System.IO;
using FlacBox;
using System.Media;

namespace AudioEngine
{
	class MainClass
	{
		[STAThread]
		public static void Main (string[] args)
		{
			using (var game = new GameWindow ())
   			using (var ac = new AudioContext())
			{
//				var bag = new C5.TreeBag<IEffect> (new TimeToStartCutoffComparer ());
////				bag.Add (new SoundEffect{Id = 1, TimeToStart = 1.0f });
////				bag.Add (new SoundEffect{Id = 2, TimeToStart = 2.0f });
////				bag.Add (new SoundEffect{Id = 3, TimeToStart = 1.5f });
////				bag.Add (new SoundEffect{Id = 5, TimeToStart = 1.5f });
////				bag.Add (new SoundEffect{Id = 4, TimeToStart = 1.2f });
//
//				foreach (IEffect effect in bag.RangeTo(new SoundEffect{TimeToStart=1.5f}))
//				{
//					Console.WriteLine ("Effect {0} - {1}", effect.Id, effect.TimeToStart);
//				}
				//var audio = new SoundMachine(new [] {ac});
				//var scheduler = new EffectScheduler (new [] { audio }, 0f);
				 
				using (var fs = File.OpenRead ("01_Ghosts_I.flac"))
				using (var wav = new WaveOverFlacStream(fs, WaveOverFlacStreamMode.Decode))
				using (var ms = new MemoryStream())
				{
					wav.CopyTo(ms);

					if (wav.StreamInfo == null)
					{
						throw new InvalidDataException ("FLAC: Missing wav stream info");
					}
					else
					{
						var sound_data = ms.ToArray();

						int channels = wav.StreamInfo.ChannelsCount;
						int bits_per_sample = wav.StreamInfo.BitsPerSample;
						int sample_rate = wav.StreamInfo.SampleRate;

						var sound_format =
							channels == 1 && bits_per_sample == 8 ? ALFormat.Mono8 :
							channels == 1 && bits_per_sample == 16 ? ALFormat.Mono16 :
							channels == 2 && bits_per_sample == 8 ? ALFormat.Stereo8 :
							channels == 2 && bits_per_sample == 16 ? ALFormat.Stereo16 :
							(ALFormat)0; // unknown

						Console.WriteLine ("Seconds : {0}",((wav.StreamInfo.TotalSampleCount / sample_rate) + ((wav.StreamInfo.TotalSampleCount % sample_rate)/(sample_rate))) );

						int buffer = AL.GenBuffer ();
						AL.BufferData(buffer, sound_format, sound_data, sound_data.Length, sample_rate);
						ALError error_code = AL.GetError ();
						if (error_code != ALError.NoError)
						{
							// respond to load error etc.
							Console.WriteLine(error_code);
						}

						int source = AL.GenSource(); // gen 2 Source Handles

						AL.Source( source, ALSourcei.Buffer, buffer ); // attach the buffer to a source

						AL.SourcePlay(source); // start playback
						AL.Source( source, ALSourceb.Looping, false ); // source loops infinitely

					}
				}


				game.Load += (sender, e) =>
				{
					// setup settings, load textures, sounds
					game.VSync = VSyncMode.On;
				};

				game.Unload += (sender, e) => 
				{
				};

				game.KeyDown += (object sender, KeyboardKeyEventArgs e) => 
				{
					if (e.Key == Key.Space)
					{
						game.Exit();
					}
				};

				game.UpdateFrame += (sender, e) =>
				{
					// add game logic, input handling

					// update shader uniforms

					// update shader mesh
				};

				game.RenderFrame += (sender, e) =>
				{
					GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

					game.SwapBuffers();
				};

				game.Resize += (sender, e) =>
				{
					GL.Viewport(0, 0, game.Width, game.Height);
				};

				game.Run(60.0);
			}
		}
	}
}
