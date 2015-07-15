using System;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using BirdNest.Audio;
using System.IO;
using OpenTK.Audio.OpenAL;

namespace OpenALDemo
{
	/// <summary>
	/// Main class.
	/// Sample file "01_Ghosts_I.flac" [Nine Inch Nails presents Ghosts I - IV]
	/// is available for under Creative Commons for non-commercial use (http://creativecommons.org/licenses/by-nc-sa/3.0/us/)
	/// See https://archive.org/details/nineinchnails_ghosts_I_IV
	/// </summary>
	class MainClass
	{
		[STAThread]
		public static void Main (string[] args)
		{
			using (var game = new GameWindow ())
			using (var ac = new AudioContext ())
			{				
				using (var fs = File.OpenRead ("01_Ghosts_I.flac"))
				using (var reader = new FLACDecoder (fs, new FLACPacketQueue (), new EmptyStubLogger ()))
				using (var ms = new MemoryStream())
				{
					Console.WriteLine ("Sample rate : {0}", reader.SampleRate);
					Console.WriteLine ("Duration : {0}", reader.Duration);

					reader.CopyTo (ms);

					byte[] sound_data = ms.ToArray ();

					int buffer = AL.GenBuffer ();
					AL.BufferData(buffer, reader.Format, sound_data, sound_data.Length, reader.SampleRate);
					ALError error_code = AL.GetError ();
					if (error_code != ALError.NoError)
					{
						// respond to load error etc.
						Console.WriteLine(error_code);
					}

					int source = AL.GenSource(); // gen 2 Source Handles

					AL.Source( source, ALSourcei.Buffer, buffer ); // attach the buffer to a source
					error_code = AL.GetError ();
					if (error_code != ALError.NoError)
					{
						// respond to load error etc.
						Console.WriteLine(error_code);
					}


					AL.SourcePlay(source); // start playback
					error_code = AL.GetError ();
					if (error_code != ALError.NoError)
					{
						// respond to load error etc.
						Console.WriteLine(error_code);
					}

					AL.Source( source, ALSourceb.Looping, false ); // source loops infinitely
					error_code = AL.GetError ();
					if (error_code != ALError.NoError)
					{
						// respond to load error etc.
						Console.WriteLine(error_code);
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
					//stream.Update(1.0f / 60f);
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
