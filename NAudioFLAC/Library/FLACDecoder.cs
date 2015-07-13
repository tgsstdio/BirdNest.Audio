using System;
using System.IO;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;
using LibFLACSharp;

namespace BirdNest.Audio
{
	public class FLACDecoder : Stream
	{
		private Stream mStream;
		private IFLACPacketQueue mPacketQueue;
		private IFLACDecoderLogger mLogger;
		private byte[] mInstreamBuffer;
		//#define 65536 bytes = 64KB
		private const Int64 DEFAULT_MAX_BUFFER_SIZE  = 16384;

		public FLACDecoder (Stream stream, IFLACPacketQueue queue, IFLACDecoderLogger logger) 
			: this(stream, queue, logger, new byte[DEFAULT_MAX_BUFFER_SIZE])					
		{	
			
		}

		private LibFLAC.DecoderReadCallback mReadCallback;
		private LibFLAC.DecoderSeekCallback mSeekCallback;
		private LibFLAC.DecoderTellCallback mTellCallback;
		private LibFLAC.DecoderLengthCallback mLengthCallback;
		private LibFLAC.DecoderEofCallback mEOFCallback;
		private LibFLAC.DecoderWriteCallbackWithStatus mWriteCallback;
		private LibFLAC.Decoder_MetadataCallback mMetadataCallback;
		private LibFLAC.Decoder_ErrorCallback mErrorCallback;
		private void SetupCallbacks ()
		{
			mReadCallback = new LibFLAC.DecoderReadCallback (this.ReadCallback);
			mSeekCallback = new LibFLAC.DecoderSeekCallback (this.SeekCallback);
			mTellCallback = new LibFLAC.DecoderTellCallback (this.TellCallback);
			mLengthCallback = new LibFLAC.DecoderLengthCallback (this.LengthCallback);
			mEOFCallback = new LibFLAC.DecoderEofCallback (this.EOFCallback);
			mWriteCallback = new LibFLAC.DecoderWriteCallbackWithStatus (this.WriteCallback);
			mMetadataCallback = new LibFLAC.Decoder_MetadataCallback (this.MetadataCallback);
			mErrorCallback = new LibFLAC.Decoder_ErrorCallback (this.ErrorCallback);
		}

		private void SetupDecoder ()
		{
			mDecoderContext = LibFLAC.FLAC__stream_decoder_new ();
			if (mDecoderContext == IntPtr.Zero)
			{
				throw new ApplicationException ("FLAC: Could not initialize stream decoder!");
			}
		}

		private void SetupFLACStream ()
		{
			if (LibFLAC.FLAC__stream_decoder_init_stream (mDecoderContext, mReadCallback, mSeekCallback, mTellCallback, mLengthCallback, mEOFCallback, mWriteCallback, mMetadataCallback, mErrorCallback, IntPtr.Zero) != 0)
			{
				throw new ApplicationException ("FLAC: Could not open stream for reading!");
			}
		}

		private void SetupStreamInfo ()
		{
			// Process the meta-data (but not the audio frames) so we can prepare the NAudio wave format
			FLACCheck (LibFLAC.FLAC__stream_decoder_process_until_end_of_metadata (mDecoderContext), "Could not process until end of metadata");
		}

		public FLACDecoder (Stream stream, IFLACPacketQueue queue, IFLACDecoderLogger logger, byte[] buffer)
		{
			mStream = stream;
			mPacketQueue = queue;
			mLogger = logger;
			mInstreamBuffer = buffer;

			mHitEOFYet = false;

			SetupDecoder ();

			SetupCallbacks ();

			SetupFLACStream ();

			SetupStreamInfo ();
		}

		private bool mHitEOFYet;
		private IntPtr mDecoderContext;

		/// <summary>
		/// Helper utility function - Checks the result of a libFlac function by throwing an exception if the result was false
		/// </summary>
		/// <param name="result"></param>
		/// <param name="operation"></param>
		private void FLACCheck(bool result, string operation)
		{
			if (!result)
			{
				var decoderState = LibFLAC.FLAC__stream_decoder_get_state(mDecoderContext);
				throw new ApplicationException (string.Format ("FLAC: Could not {0} - {1}!", operation, decoderState));
			}
		}

		#region implemented abstract members of Stream

		public override void Flush ()
		{
			throw new NotImplementedException ();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException ();
		}

		public override void SetLength (long value)
		{
			throw new NotImplementedException ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			int localOffset = offset;
			int spaceRemaining = count;
			int bytesRead = 0;

			// copy all data remaining in current packet
			while (spaceRemaining > 0)
			{

				// 1. pop packet from queue

				// while there's space left on buffer and stream & flac are not empty
				// 1. check for another packet in queue
				// 2. else request another packet via callbacks

				RequestAnotherFLACPacket ();

				FLACPacket current;
				if (mPacketQueue.TryPeek (out current))
				{
					int bytesLeft = current.Data.Length - current.Offset;

					// if packet is larger than remaining space
					if (bytesLeft > spaceRemaining)
					{
						// 1. copy current.data to buffer
						Array.Copy (buffer, localOffset, current.Data, current.Offset, spaceRemaining);

						// 2. adjust offset in current
						current.Offset += spaceRemaining;

						// 3. adjust bytes read
						bytesRead += spaceRemaining;

						// 4. set space remaining to zero
						spaceRemaining = 0;
					} 
					else if (bytesLeft > 0 && bytesLeft <= spaceRemaining)
					{
						// 1. copy all data from buffer 
						Array.Copy (buffer, localOffset, current.Data, current.Offset, bytesLeft);

						// 2. adjust local offset 
						localOffset += bytesLeft;

						// 3. subtract space remaining for packet length
						spaceRemaining -= bytesLeft;

						// 4. adjust bytes read
						bytesRead += bytesLeft;

						// 5. pop buffer 
						PopTopOffQueue ();
					}
				} 
				else
				{
					// else if current packet is empty 
					// FLAC : if empty,
					break;
				}
			}
			return bytesRead;
//
//			if (mHitEOFYet)
//				return 0;
//
//			var state = LibFLAC.FLAC__stream_decoder_get_state(mDecoderContext);
//			if (state == LibFLAC.StreamDecoderState.EndOfStream)
//			{
//				return 0;
//			}
//			else if ( state >= LibFLAC.StreamDecoderState.OggError )
//			{
//				// Critical error occured
//				throw new ApplicationException(string.Format("FLAC: Decoding returned with critical state: {0}", state));
//			}
//			else
//			{

		}

		private void RequestAnotherFLACPacket()
		{
			if (mPacketQueue.IsEmpty ())
			{		
				var state = LibFLAC.FLAC__stream_decoder_get_state (mDecoderContext);

				if (state < LibFLAC.StreamDecoderState.EndOfStream)
				{			
					FLACCheck (LibFLAC.FLAC__stream_decoder_process_single (mDecoderContext), "process single");
				}
				// else if (flacStatus < LibFLAC.StreamDecoderState.EndOfStream) {}
				else if ( state >= LibFLAC.StreamDecoderState.OggError )
				{
					// Critical error occured
					throw new ApplicationException(string.Format("FLAC: Decoding returned with critical state: {0}", state));
				}
			}
		}

		private void PopTopOffQueue()
		{
			FLACPacket top;
			if (!mPacketQueue.TryDequeue (out top))
			{
				throw new Exception ("FLAC - queue error");
			}
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException ();
		}

		public override bool CanRead {
			get {
				return mStream.CanRead;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return false;
			}
		}

		private int mBlockAlign;
		private long mTotalSamples;
		private long mFLACLength;
		public override long Length {
			get {
				return mFLACLength;
			}
		}

		public override long Position {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		#endregion

		#region IDisposable implementation

		private bool mIsDisposed = false;

		/// <summary>
		/// Disposes this WaveStream
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (mIsDisposed)
				return;

			// unmanaged memory
			mHitEOFYet = false;

			if (mDecoderContext != IntPtr.Zero)
			{
				FLACCheck(
					LibFLAC.FLAC__stream_decoder_finish(mDecoderContext),
					"finalize stream decoder");

				FLACCheck(
					LibFLAC.FLAC__stream_decoder_delete(mDecoderContext),
					"dispose of stream decoder instance");

				mDecoderContext = IntPtr.Zero;
			}

			if (disposing)
			{
				// managed code
				mStream.Close ();

				mFLACChannelData_Mono_0 = null;
				mFLACChannelData_Stereo_1 = null;

				mInstreamBuffer = null;
			}

			mIsDisposed = true;
			base.Dispose (true);
		}

		#endregion

		#region Callbacks

		protected LibFLAC.StreamDecoderReadStatus ReadCallback(IntPtr context, IntPtr buffer, ref IntPtr bytes, IntPtr userData)
		{
			if (mInstreamBuffer == null)
			{
				return LibFLAC.StreamDecoderReadStatus.ReadStatusAbort;
			}

			Int64 noOfBytes = bytes.ToInt64();

			if(noOfBytes > 0) 
			{
				int length = (noOfBytes < mInstreamBuffer.Length) ? (int) noOfBytes : (int) mInstreamBuffer.Length;
				int count = mStream.Read (mInstreamBuffer, 0, length);
				Marshal.Copy (mInstreamBuffer, 0, buffer, count);

				if (count < 0)
				{
					mHitEOFYet = true;
					return LibFLAC.StreamDecoderReadStatus.ReadStatusAbort;
				} 
				else if (count < length)
				{
					mHitEOFYet = true;
					bytes = (IntPtr) count;
					return LibFLAC.StreamDecoderReadStatus.ReadStatusEndOfStream;				
				}
				else
				{
					bytes = (IntPtr) count;
					return LibFLAC.StreamDecoderReadStatus.ReadStatusContinue;
				}

			}
			else
			{
				mHitEOFYet = true;
				return LibFLAC.StreamDecoderReadStatus.ReadStatusAbort;
			}
		}

		protected LibFLAC.StreamDecoderSeekStatus SeekCallback(IntPtr context, UInt64 absoluteByteOffset, IntPtr userData)
		{
			if (mStream.CanSeek)
			{
				try
				{
					long offset = Convert.ToInt64(absoluteByteOffset);
					mStream.Seek(offset, SeekOrigin.Begin);
					return LibFLAC.StreamDecoderSeekStatus.SeekStatusOk;
				} 
				catch (NotImplementedException)
				{
					return LibFLAC.StreamDecoderSeekStatus.SeekStatusUnsupported;
				} 
				catch (Exception)
				{
					return LibFLAC.StreamDecoderSeekStatus.SeekStatusError;
				}
			}
			else
			{
				return LibFLAC.StreamDecoderSeekStatus.SeekStatusUnsupported;
			}
		}

		protected LibFLAC.StreamDecoderTellStatus TellCallback(IntPtr context, ref UInt64 absoluteByteOffset, IntPtr userData)
		{
			try
			{
				absoluteByteOffset = Convert.ToUInt64(mStream.Position);
				return LibFLAC.StreamDecoderTellStatus.TellStatusOK;
			}
			catch(NotImplementedException)
			{
				return LibFLAC.StreamDecoderTellStatus.TellStatusUnsupported;
			}
			catch(Exception)
			{
				return LibFLAC.StreamDecoderTellStatus.TellStatusError;
			}
		}

		protected LibFLAC.StreamDecoderLengthStatus LengthCallback(IntPtr context, ref UInt64 streamLength, IntPtr userData)
		{
			try
			{
				long length = mStream.Length;
				streamLength = Convert.ToUInt64(length);
				return LibFLAC.StreamDecoderLengthStatus.LengthStatusOk;
			}
			catch(NotImplementedException)
			{
				return LibFLAC.StreamDecoderLengthStatus.LengthStatusUnsupported;
			}
			catch(Exception)
			{
				return LibFLAC.StreamDecoderLengthStatus.LengthStatusError;
			}
		}

		// A simple OpenAL-usable format description
		public ALFormat Format {get; private set;}
		public int Channels { get; private set; }
		public int SampleRate {get; private set;}
		public int BitsPerSample { get; private set; }
		protected void MetadataCallback(IntPtr context, IntPtr metadata, IntPtr userData)
		{
			LibFLAC.FLACMetaData flacMetaData = (LibFLAC.FLACMetaData) Marshal.PtrToStructure(metadata, typeof(LibFLAC.FLACMetaData));

			if (flacMetaData.MetaDataType == LibFLAC.FLACMetaDataType.StreamInfo)
			{
				GCHandle pinnedStreamInfo = GCHandle.Alloc(flacMetaData.Data, GCHandleType.Pinned);
				try
				{
					var streamInfo = (LibFLAC.FLACStreamInfo) Marshal.PtrToStructure(
						pinnedStreamInfo.AddrOfPinnedObject(),
						typeof(LibFLAC.FLACStreamInfo));

					this.BitsPerSample = streamInfo.BitsPerSample;
					this.Channels = streamInfo.Channels;
					this.SampleRate = streamInfo.SampleRate;

					mBlockAlign = (this.Channels * (BitsPerSample / 8));
					mTotalSamples = (long)(streamInfo.TotalSamplesHi << 32) + (long)streamInfo.TotalSamplesLo;
					mFLACLength = mBlockAlign * mTotalSamples;

					if (this.BitsPerSample == 16)
					{
						this.Format = this.Channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;
					}
					else if (this.BitsPerSample == 8 )
					{
						this.Format = this.Channels == 2 ? ALFormat.Stereo8 : ALFormat.Mono8;
					}
					else
					{
						mLogger.Warning(string.Format("FLAC: Unsupported sample bit size: {0}\n", BitsPerSample));
					}
				}
				finally
				{
					pinnedStreamInfo.Free();
				}

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
		protected LibFLAC.StreamDecoderWriteStatus WriteCallback(IntPtr context, IntPtr frame, IntPtr buffer, IntPtr clientData)
		{
			// Read the FLAC Frame into a memory samples buffer (m_flacSamples)
			LibFLAC.FlacFrame flacFrame = (LibFLAC.FlacFrame)Marshal.PtrToStructure(frame, typeof(LibFLAC.FlacFrame));

			// TODO: Write functions for handling 8-bit audio as well
			if (flacFrame.Header.BitsPerSample != 16)
			{
				mLogger.Warning(string.Format("FLAC: Unsupported bit-rate: {0}", flacFrame.Header.BitsPerSample));
				return LibFLAC.StreamDecoderWriteStatus.WriteStatusAbort;
			}

			var packet = new FLACPacket ();
			packet.Channels = flacFrame.Header.Channels;
			//packet.Format = this.Format;
			packet.SampleRate = flacFrame.Header.SampleRate;
			packet.BlockSize = flacFrame.Header.BlockSize;
			packet.Offset = 0;

			CopyRawFLACBufferData (buffer, 0, ref mFLACChannelData_Mono_0, packet.BlockSize);

			// 16 bit only
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
			mPacketQueue.Enqueue (packet);
			return LibFLAC.StreamDecoderWriteStatus.WriteStatusContinue;
		}



		/// <summary>
		/// FLAC Error Call Back - libFlac notifies about a decoding error
		/// </summary>
		/// <param name="context"></param>
		/// <param name="status"></param>
		/// <param name="userData"></param>
		private void ErrorCallback(IntPtr context, LibFLAC.DecodeError status, IntPtr userData)
		{
			var decoderState = LibFLAC.FLAC__stream_decoder_get_state(context);
			throw new ApplicationException(string.Format("FLAC: Could not decode frame: {0} - {1}!", status, decoderState));
		}

		#endregion				

	}
}