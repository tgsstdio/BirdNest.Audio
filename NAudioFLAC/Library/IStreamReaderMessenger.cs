using System;
using System.IO;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;

namespace BigMansStuff.NAudio.FLAC
{
	public interface IFLACStreamReaderMessenger
	{
		void Warning(string msg);
	}

}