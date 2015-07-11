using System;
using System.IO;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;

namespace BigMansStuff.NAudio.FLAC
{
	public class FLACStreamReader : IDisposable
	{
		private Stream mStream;
		private ISoundPacketQueue mOutput;
		private IFLACStreamReaderMessenger mLogger;
		public byte[] StreamBuffer { get; private set; }

		public FLACStreamReader (Stream stream, ISoundPacketQueue queue, IFLACStreamReaderMessenger messenger) 
			: this(stream, queue, messenger, new byte[DEFAULT_MAX_BUFFER_SIZE])					
		{	
			
		}

		public FLACStreamReader (Stream stream, ISoundPacketQueue queue, IFLACStreamReaderMessenger messenger, byte[] buffer)
		{
			mStream = stream;
			mOutput = queue;
			mLogger = messenger;
			StreamBuffer = buffer;
		}

		//#define OPENAL_BUFFER_SIZE 65536 // 65536 bytes = 64KB
		private const Int64 DEFAULT_MAX_BUFFER_SIZE  = 16384;
		private ALFormat mFormat;                	// A simple OpenAL-usable format description
		private int mSampleRate;
		private bool mHitEOFYet;
		private IntPtr mDecoderContext;

		public void ReadToEnd()
		{
			mSampleRate = 0;
			mHitEOFYet = false;

			mDecoderContext = LibFLACSharp.FLAC__stream_decoder_new();
			if (mDecoderContext == IntPtr.Zero)
			{
				throw new ApplicationException("FLAC: Could not initialize stream decoder!");
			}

			if (LibFLACSharp.FLAC__stream_decoder_init_stream(mDecoderContext,
				this.ReadCallback,
				this.SeekCallback,
				this.TellCallback,
				this.LengthCallback,
				this.EOFCallback,
				this.WriteCallback,
				this.MetadataCallback,
				this.ErrorCallback,
				IntPtr.Zero) != 0)
			{				
				throw new ApplicationException("FLAC: Could not open stream for reading!");
			}

		}

		/// <summary>
		/// Helper utility function - Checks the result of a libFlac function by throwing an exception if the result was false
		/// </summary>
		/// <param name="result"></param>
		/// <param name="operation"></param>
		private void FLACCheck(bool result, string operation)
		{
			if (!result)
			{
				var decoderState = LibFLACSharp.FLAC__stream_decoder_get_state(mDecoderContext);
				throw new ApplicationException (string.Format ("FLAC: Could not {0} - {1}!", operation, decoderState));
			}
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private bool mIsDisposed = false;

		/// <summary>
		/// Disposes this WaveStream
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (mIsDisposed)
				return;

			// unmanaged memory
			mHitEOFYet = false;

			if (mDecoderContext != IntPtr.Zero)
			{
				FLACCheck(
					LibFLACSharp.FLAC__stream_decoder_finish(mDecoderContext),
					"finalize stream decoder");

				FLACCheck(
					LibFLACSharp.FLAC__stream_decoder_delete(mDecoderContext),
					"dispose of stream decoder instance");

				mDecoderContext = IntPtr.Zero;
			}

			if (disposing)
			{
				// managed code
				mStream.Close ();

				mFLACChannelData_Mono_0 = null;
				mFLACChannelData_Stereo_1 = null;

				StreamBuffer = null;
			}

			mIsDisposed = true;
		}

		#endregion

		#region Callbacks

		protected LibFLACSharp.StreamDecoderReadStatus ReadCallback(IntPtr context, IntPtr buffer, ref IntPtr bytes, IntPtr userData)
		{
			Int64 noOfBytes = bytes.ToInt64();

			if(noOfBytes > 0) 
			{
				int length = (noOfBytes < StreamBuffer.Length) ? (int) noOfBytes : (int) StreamBuffer.Length;
				int count = mStream.Read (StreamBuffer, 0, length);
				Marshal.Copy (StreamBuffer, 0, buffer, count);

				if (count < 0)
				{
					mHitEOFYet = true;
					return LibFLACSharp.StreamDecoderReadStatus.ReadStatusAbort;
				} 
				else if (count < length)
				{
					mHitEOFYet = true;
					bytes = (IntPtr) count;
					return LibFLACSharp.StreamDecoderReadStatus.ReadStatusEndOfStream;				
				}
				else
				{
					bytes = (IntPtr) count;
					return LibFLACSharp.StreamDecoderReadStatus.ReadStatusContinue;
				}

			}
			else
			{
				mHitEOFYet = true;
				return LibFLACSharp.StreamDecoderReadStatus.ReadStatusAbort;
			}
		}

		protected LibFLACSharp.StreamDecoderSeekStatus SeekCallback(IntPtr context, UInt64 absoluteByteOffset, IntPtr userData)
		{
			if (mStream.CanSeek)
			{
				try
				{
					long offset = Convert.ToInt64(absoluteByteOffset);
					mStream.Seek(offset, SeekOrigin.Begin);
					return LibFLACSharp.StreamDecoderSeekStatus.SeekStatusOk;
				} 
				catch (NotImplementedException)
				{
					return LibFLACSharp.StreamDecoderSeekStatus.SeekStatusUnsupported;
				} 
				catch (Exception)
				{
					return LibFLACSharp.StreamDecoderSeekStatus.SeekStatusError;
				}
			}
			else
			{
				return LibFLACSharp.StreamDecoderSeekStatus.SeekStatusUnsupported;
			}
		}

		protected LibFLACSharp.StreamDecoderTellStatus TellCallback(IntPtr context, ref UInt64 absoluteByteOffset, IntPtr userData)
		{
			try
			{
				absoluteByteOffset = Convert.ToUInt64(mStream.Position);
				return LibFLACSharp.StreamDecoderTellStatus.TellStatusOK;
			}
			catch(NotImplementedException)
			{
				return LibFLACSharp.StreamDecoderTellStatus.TellStatusUnsupported;
			}
			catch(Exception)
			{
				return LibFLACSharp.StreamDecoderTellStatus.TellStatusError;
			}
		}

		protected LibFLACSharp.StreamDecoderLengthStatus LengthCallback(IntPtr context, ref UInt64 streamLength, IntPtr userData)
		{
			try
			{
				long length = mStream.Length;
				streamLength = Convert.ToUInt64(length);
				return LibFLACSharp.StreamDecoderLengthStatus.LengthStatusOk;
			}
			catch(NotImplementedException)
			{
				return LibFLACSharp.StreamDecoderLengthStatus.LengthStatusUnsupported;
			}
			catch(Exception)
			{
				return LibFLACSharp.StreamDecoderLengthStatus.LengthStatusError;
			}
		}

		protected int EOFCallback(IntPtr context, IntPtr userData)
		{	
			return (mHitEOFYet) ? 1 : 0;

//			if (mStream.Position >= mStream.Length)
//			{
//				m_bHitEOF = true;
//				return 1;
//			}
//			else
//			{
//				return 0;
//			}
		}

		private void CopyRawFLACBufferData (IntPtr src, int offset, ref int[] dest, int blockSize)
		{
			InitialiseRawFLACBuffer (ref dest, blockSize);
			IntPtr channelDataPtr = Marshal.ReadIntPtr (src, offset * IntPtr.Size);
			Marshal.Copy (channelDataPtr, dest, 0, blockSize);
		}

		private void InitialiseRawFLACBuffer (ref int[] flacBuffer, int blockSize)
		{			
			if (flacBuffer == null)
			{
				// First time - Create Flac sample buffer
				flacBuffer = new int[blockSize];
			}
			else if (flacBuffer.Length < blockSize)
			{
				flacBuffer = new int[blockSize];
			}
		}

		private int[] mFLACChannelData_Mono_0;
		private int[] mFLACChannelData_Stereo_1;

		/// <summary>
		/// FLAC Write Call Back - libFlac notifies back on a frame that was read from the source file and written as a frame
		/// </summary>
		/// <param name="context"></param>
		/// <param name="frame"></param>
		/// <param name="buffer"></param>
		/// <param name="clientData"></param>
		protected LibFLACSharp.StreamDecoderWriteStatus WriteCallback(IntPtr context, IntPtr frame, IntPtr buffer, IntPtr clientData)
		{

			// Read the FLAC Frame into a memory samples buffer (m_flacSamples)
			LibFLACSharp.FlacFrame flacFrame = (LibFLACSharp.FlacFrame)Marshal.PtrToStructure(frame, typeof(LibFLACSharp.FlacFrame));

			// TODO: Write functions for handling 8-bit audio as well
			if (flacFrame.Header.BitsPerSample != 16)
			{
				mLogger.Warning(string.Format("FLAC: Unsupported bit-rate: {0}", flacFrame.Header.BitsPerSample));
				return LibFLACSharp.StreamDecoderWriteStatus.WriteStatusAbort;
			}

			var packet = new SoundPacket ();
			packet.Channels = flacFrame.Header.Channels;
			packet.Format = mFormat;
			packet.SampleRate = flacFrame.Header.SampleRate;
			packet.BlockSize = flacFrame.Header.BlockSize;

			CopyRawFLACBufferData (buffer, 0, ref mFLACChannelData_Mono_0, packet.BlockSize);

			if (packet.Channels == 2)
			{
				// Stereo				
				CopyRawFLACBufferData (buffer, 1, ref mFLACChannelData_Stereo_1, packet.BlockSize);

				packet.Data = new byte[4 * packet.BlockSize];
				uint writePosition = 0;

				for(uint i = 0; i < packet.BlockSize; i++) 
				{
					int leftChannel = mFLACChannelData_Mono_0[i];
					int rightChannel = mFLACChannelData_Stereo_1[i];

					packet.Data[writePosition++] =  (byte)(leftChannel >> 0);
					packet.Data[writePosition++] =(byte)(leftChannel >> 8);

					packet.Data[writePosition++] = (byte)(rightChannel >> 0);
					packet.Data[writePosition++] =  (byte)(rightChannel >> 8);
				}
			}
			else
			{
				packet.Data = new byte[2 * packet.BlockSize];
				uint writePosition = 0;

				// Mono
				for(uint i = 0; i < packet.BlockSize; i++) 
				{
					int leftChannel = mFLACChannelData_Mono_0[i];
					packet.Data[writePosition++] =  (byte)(leftChannel >> 0);
					packet.Data[writePosition++] = (byte)(leftChannel >> 8);
				}
			}

			mOutput.Enqueue (packet);
			return LibFLACSharp.StreamDecoderWriteStatus.WriteStatusContinue;
		}

		protected void MetadataCallback(IntPtr context, IntPtr metadata, IntPtr userData)
		{
			LibFLACSharp.FLACMetaData flacMetaData = (LibFLACSharp.FLACMetaData) Marshal.PtrToStructure(metadata, typeof(LibFLACSharp.FLACMetaData));

			if (flacMetaData.MetaDataType == LibFLACSharp.FLACMetaDataType.StreamInfo)
			{
				GCHandle pinnedStreamInfo = GCHandle.Alloc(flacMetaData.Data, GCHandleType.Pinned);
				try
				{
					var streamInfo = (LibFLACSharp.FLACStreamInfo) Marshal.PtrToStructure(
						pinnedStreamInfo.AddrOfPinnedObject(),
						typeof(LibFLACSharp.FLACStreamInfo));

					int bits = streamInfo.BitsPerSample;
					int channels = streamInfo.Channels;
					mSampleRate = streamInfo.SampleRate;

					if (bits == 16)
					{
						mFormat = channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;
							}
					else if ( bits == 8 )
					{
						mFormat = channels == 2 ? ALFormat.Stereo8 : ALFormat.Mono8;
					}
					else
					{
						mLogger.Warning(string.Format("FLAC: Unsupported sample bit size: {0}\n", bits));
					}
				}
				finally
				{
					pinnedStreamInfo.Free();
				}

			}
		}

		/// <summary>
		/// FLAC Error Call Back - libFlac notifies about a decoding error
		/// </summary>
		/// <param name="context"></param>
		/// <param name="status"></param>
		/// <param name="userData"></param>
		private void ErrorCallback(IntPtr context, LibFLACSharp.DecodeError status, IntPtr userData)
		{
			var decoderState = LibFLACSharp.FLAC__stream_decoder_get_state(mDecoderContext);
			throw new ApplicationException(string.Format("FLAC: Could not decode frame: {0} - {1}!", status, decoderState));
		}

		#endregion
				
	}
}