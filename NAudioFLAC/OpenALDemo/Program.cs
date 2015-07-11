using System;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using BigMansStuff.NAudio.FLAC;
using System.IO;

namespace OpenALDemo
{
	class MainClass
	{
		[STAThread]
		public static void Main (string[] args)
		{
			using (var game = new GameWindow ())
			using (var ac = new AudioContext ())
			using (var fs = File.OpenRead("01_Ghosts_I.flac"))
			using (var stream = new FLACStreamReader(fs, new SoundPacketQueue(), new FLACStreamReaderMessager()))					
			{				
				stream.ReadToEnd ();
				//stream.Play ();
				Console.WriteLine ("Hello World!");

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
