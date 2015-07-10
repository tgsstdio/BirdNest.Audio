using System;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using BigMansStuff.NAudio.FLAC;

namespace OpenALDemo
{
	class MainClass
	{
		[STAThread]
		public static void Main (string[] args)
		{
			using (var game = new GameWindow ())
			using (var ac = new AudioContext ())
			{
				Console.WriteLine ("Hello World!");

				using (var stream = new FLACFileReader("01_Ghosts_I.flac"))
				{
					int MAX_BUFFER = 8800;
					byte[] soundData = new byte[MAX_BUFFER];

					int count = 0;
					while (stream.Read (soundData, 0, MAX_BUFFER) > 0)
					{
						++count;
					}
					
					int buffer = AL.GenBuffer ();
					AL.BufferData(buffer, stream.WaveFormat.sound_format, soundData, MAX_BUFFER, stream.WaveFormat.sampleRate);
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
