using System;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using BirdNest.Audio;
using System.IO;

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
			using (var fs = File.OpenRead("01_Ghosts_I.flac"))
			using (var reader = new FLACDecoder(fs, new FLACPacketQueue(), new EmptyStubLogger()))					
			{				
				int totalBytesRead = 0;

				const int MAX_BUFFER = 4096;
				byte[] buffer = new byte[MAX_BUFFER];

				Console.WriteLine ("Sample rate : {0}", reader.SampleRate);
				Console.WriteLine ("Duration : {0}", reader.Duration);
				bool isRunning = true;
				while (isRunning)
				{
					var count = reader.Read(buffer, 0, MAX_BUFFER);
					totalBytesRead += count;
					if (count < MAX_BUFFER)
					{
						isRunning = false;
					}

					if (!isRunning)
					{
						reader.Dispose();
					}
				}

				//stream.Play ();
				Console.WriteLine ("Total bytes loaded : {0} vs. {1}", totalBytesRead, reader.Length);

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
