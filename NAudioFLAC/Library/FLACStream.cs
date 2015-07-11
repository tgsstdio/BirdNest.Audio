using System;
using System.IO;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;

namespace BigMansStuff.NAudio.FLAC
{
	public class FLACStream : Stream
	{
		const int NUM_BUFFERS = 4;

		private int[] buffers;  // Buffers to queue our data into
		private int source;            				  // Our source's identifier for OpenAL
		public ALFormat Format;                	// A simple OpenAL-usable format description

		//#define OPENAL_BUFFER_SIZE 65536 // 65536 bytes = 64KB
		private const int  OPENAL_BUFFER_SIZE  = 16384;

		public bool IsStreaming; // Are we in streaming mode, or should we preload the data?
		private bool mIsFinished;  // Are we finished playing this sound? Can we delete this?
		public bool IsLooping;   // Is this sample in a constant looping state?
		private bool mIsReady;     // Are we ready to play this file?
		public bool RequiresSync; // If this is true, we syncronize with the engine.
		public bool IsPositional; // Are we placed in a world position?
		public bool IsPersistent; // Do not delete this sample automatically, used for pointers that are stored

		private float[] m_fPosition = new float[3]; // Where is this sample source?
		private float[] m_fVelocity = new float[3]; // In which velocity is our source playing?
		public float Gain; // This is the gain of our sound
		public float FadeScalar; // The gain of our sound is multiplied by this

		private Stream mStream;

		private byte[] mALStreamingBufferData;
		private int[] mFLACChannelData_Mono_0; // for mono & stereo
		private int[] mFLACChannelData_Stereo_1; // for stereo
		private long mALCurrentWritePosition;
		private long mALStreamingWriteUpperLimit;
		private int mStreamingBufferSize;
		private int sampleRate;
		private int mSizeOfLastWrite;
		private bool mHitEOFYet;

		private void Msg(string message)
		{

		}

		private void DevMsg(string message)
		{

		}

		private void OPENAL_ERROR(ALError error)
		{

		}

		private void Warning(string message)
		{

		}

		public void Update(float updateTime)
		{
			if (mIsFinished)
			{
				DestroyAL();
				return;
			}

			if (IsReady())
			{
				Warning("OpenAL: Sample update requested while not ready. Skipping Update.\n");
				return;
			}

			/***
			 * Let's do any processing that needs to be done for syncronizing with the game engine
			 * prior to working on the buffers.
			 ***/
			UpdatePositional(updateTime);
			UpdateBuffers(updateTime);

			SubUpdate();

			RequiresSync = false;
		}

		private void UpdateBuffers(float lastUpdate)
		{
			int state, processed;
			bool active = false;

			AL.GetSource(source, ALGetSourcei.SourceState, out state);
			AL.GetSource(source, ALGetSourcei.BuffersProcessed, out processed);

			while (processed-- > 0)
			{
				int[] buffer = new int[1];
				ALError error;

				AL.SourceUnqueueBuffers(source, 1, buffer);
				error = AL.GetError();
				if (error != ALError.NoError)
				{
					Warning("OpenAL: There was an error unqueuing a buffer. Issues may arise.\n");
					OPENAL_ERROR(error);
				}

				active = CheckStream(buffer[0]);

				// I know that this block seems odd, but it's buffer overrun protection. Keep it here.
				if (active)
				{
					AL.SourceQueueBuffers(source, 1, buffer);
					error = AL.GetError();
					if (error != ALError.NoError)
					{
						Warning("OpenAL: There was an error queueing a buffer. Expect some turbulence.\n");
						OPENAL_ERROR(error);
					}

					if (state != (int) ALSourceState.Playing && state != (int) ALSourceState.Paused)
					{
						AL.SourcePlay(source);
					}
				}
			}
		}

		void UpdatePositional(float lastUpdate)
		{
//			if (!m_bRequiresSync && !m_pLinkedEntity) return;
//
//			float[] position = new float[3];
//			float[] velocity = new float[3];
//
//			if (m_pLinkedEntity )
//			{
//				/*
//		// TODO: Provide methods for better control of this position
//				position[0] = m_pLinkedEntity->GetAbsOrigin().x;
//				position[1] = m_pLinkedEntity->GetAbsOrigin().y;
//				position[2] = m_pLinkedEntity->GetAbsOrigin().y;
//
//				velocity[0] = m_pLinkedEntity->GetAbsVelocity().x;
//				velocity[1] = m_pLinkedEntity->GetAbsVelocity().y;
//				velocity[2] = m_pLinkedEntity->GetAbsVelocity().z;
//				*/
//
//				position[0] = m_pLinkedEntity->GetLocalOrigin().x;
//				position[1] = m_pLinkedEntity->GetLocalOrigin().y;
//				position[2] = m_pLinkedEntity->GetLocalOrigin().y;
//
//				velocity[0] = m_pLinkedEntity->GetLocalVelocity().x;
//				velocity[1] = m_pLinkedEntity->GetLocalVelocity().y;
//				velocity[2] = m_pLinkedEntity->GetLocalVelocity().z;
//			}
//			else
//			{
//				if (m_bPositional)
//				{
//					position[0] = m_fPosition[0];
//					position[1] = m_fPosition[1];
//					position[2] = m_fPosition[2];
//
//					velocity[0] = m_fVelocity[0];
//					velocity[1] = m_fVelocity[1];
//					velocity[2] = m_fVelocity[2];
//				}
//				else
//				{
//					position[0] = 0.0f;
//					position[1] = 0.0f;
//					position[2] = 0.0f;
//
//					velocity[0] = 0.0f;
//					velocity[1] = 0.0f;
//					velocity[2] = 0.0f;
//				}
//			}
//
//			// alSource3f(source, AL_POSITION, VALVEUNITS_TO_METERS(position[0]), VALVEUNITS_TO_METERS(position[1]), VALVEUNITS_TO_METERS(position[2]));
//			AL.Source(source, ALSource3f.Position, position[0], position[1], position[2]);
//			if (AL.GetError() !=  ALError.NoError)
//				Warning("OpenAL: Couldn't update a source's position.\n");
//
//			// alSource3f(source, AL_VELOCITY, VALVEUNITS_TO_METERS(velocity[0]), VALVEUNITS_TO_METERS(velocity[1]), VALVEUNITS_TO_METERS(velocity[2]));
//			AL.Source(source, ALSource3f.Velocity, velocity[0], velocity[1], velocity[2]);
//			if (AL.GetError() != ALError.NoError)
//				Warning("OpenAL: Couldn't update a source's velocity.\n");
//
//			AL.Source(source, ALSourcef.Gain, m_fGain * m_fFadeScalar);
		}

		/***
		* Generic playback controls
		***/
		void Play()
		{
			int buffersToQueue = 0;

			if (IsPlaying())
				return; // Well, that was easy!

			for (int i=0; i < NUM_BUFFERS; ++i)
			{
				if (CheckStream(buffers[i]))
				{
					++buffersToQueue;
				}
			}

			if (buffersToQueue == 0)
			{
				Warning("OpenAL: Couldn't play a stream.\n");
				return;
			}

			ALError error;

			AL.SourceQueueBuffers(source, buffersToQueue, buffers);
			error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: There was an error queueing buffers. This will probably fix itself, but it's still not ideal.\n");
				OPENAL_ERROR(error);
			}

			AL.SourcePlay(source);

			error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Playing an audio sample failed horribly.\n");
				OPENAL_ERROR(error);
			}
		}

		void Stop()
		{
			if (!IsPlaying())
				return; // Whachootockinaboutwillis?

			AL.SourceRewind(source);
			var error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error stopping a sound. This is less than good news.\n");
				OPENAL_ERROR(error);
			}

			ClearBuffers();
		}

		void Pause()
		{
			Warning("OpenAL: Pausing hasn't been implemented yet?! That's ridiculous...\n");
		}

		bool IsPlaying()
		{
			int state;
			AL.GetSource(source, ALGetSourcei.SourceState, out state);

			var error = AL.GetError();
			if ( error != ALError.NoError)
			{
				OPENAL_ERROR(error); // Spy's sappin' mah error buffer!
			}

			return (state == (int) ALSourceState.Playing);
		}

		/***
		//* Checks whether or not this sample is currently ready to be played.
		// 		***/
		public bool IsReady()
		{
			return mIsReady && !mIsFinished;
		}

		bool IsFinished()
		{
			if (mIsFinished)
			{
				float seconds_played;
				AL.GetSource(source, ALSourcef.SecOffset, out seconds_played);

				var error = AL.GetError();
				if ( error == ALError.NoError)
				{
					// HACKHACK: Samples that are done streaming the same frame as they 
					// start playing, will be deleted instantly
					return seconds_played > 0.1f;
				}

				return true;
			}

			return false;
		}

		//		/***
		//		 * Methods for updating the source's position/velocity/etc
		//		 ***/
		void SetPositional(bool positional)
		{
			const float BASE_ROLLOFF_FACTOR = 1.0f;

			bool already_positional;
			AL.GetSource(source, ALSourceb.SourceRelative, out already_positional);

			// We don't need to set positional if we've already done so!
			if (already_positional)
			{
				return;
			}

			IsPositional = positional;
			ALError error;
			if (IsPositional)
			{
				RequiresSync = true;
				AL.Source(source, ALSourceb.SourceRelative, false);
				AL.Source(source, ALSourcef.RolloffFactor, BASE_ROLLOFF_FACTOR * 200);

				error = AL.GetError();
				if ( error != ALError.NoError)
				{
					Warning("OpenAL: Couldn't update rolloff factor to enable positional audio.\n");
					OPENAL_ERROR(error);
				}
			}
			else
			{
				AL.Source(source, ALSourceb.SourceRelative, true);
				AL.Source(source, ALSourcef.RolloffFactor, 0f);

				error = AL.GetError();
				if ( error != ALError.NoError)
				{
					Warning("OpenAL: Couldn't update rolloff factor to disable positional audio.\n");
					OPENAL_ERROR(error);
				}
			}

			RequiresSync = true;
		}

		void SetPosition(float x, float y, float z)
		{
			m_fPosition[0] = x;
			m_fPosition[1] = y;
			m_fPosition[2] = z;

			RequiresSync = true;
		}

		void SetPosition(float[] position)
		{
			m_fPosition[0] = position[0];
			m_fPosition[1] = position[1];
			m_fPosition[2] = position[2];

			RequiresSync = true;
		}

		void SetVelocity(float[] velocity)
		{
			m_fVelocity[0] = velocity[0];
			m_fVelocity[1] = velocity[1];
			m_fVelocity[2] = velocity[2];

			RequiresSync = true;
		}

		void SetGain(float newGain) { Gain = newGain; }

		//		/*
		//        NOTE: All samples that do not call Persist() will be automatically deleted
		//        when they're done playing to prevent memory leaks. Call Persist() on any
		//        sample that you plan on storing to prevent this from happening.
		//    	*/
		void Persist() {
			IsPersistent = true; 
		}

		void SetLooping(bool shouldLoop)
		{
			IsLooping = shouldLoop;
		}

		private void ClearBuffers()
		{
			if (IsPlaying())
			{
				DevMsg("OpenAL: ClearBuffers() called while playing. Sample will stop now.\n");
				Stop();
			}

			AL.Source(source, ALSourcei.Buffer, 0);

			var error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: An error occured while attempting to clear a source's buffers.\n");
				OPENAL_ERROR(error);
			}
		}

		/***
 		* Keep those buffers flowing.
 		***/
		private void BufferData(int bufferID, byte[] data, int size, int freq)
		{
			AL.BufferData<byte>(bufferID, Format, data, size, freq);
			var error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: There was an error buffering audio data. Releasing deadly neurotoxin in 3... 2.. 1..\n");
				OPENAL_ERROR(error);
			}
		}

		// Methods specific formats use to fully support/define the sample
		bool InitFormat() 
		{
			return true; 
		}

		void DestroyFormat()
		{

		}

		// This is the update function for subclasses
		// The reason we do it like this is to prevent subclasses from interfering 
		// with vital processes that all samples share
		void SubUpdate()
		{

		} 

		void UpdateMetadata()
		{

		}

		public FLACStream (Stream stream)
		{
			mStream = stream;

			IsStreaming = false;
			mIsFinished = false;
			IsLooping = false;
			mIsReady = false;
			RequiresSync = true;
			IsPositional = false;
			IsPersistent = false;

			Gain = 1.0f;
			FadeScalar = 1.0f;

			m_fPosition[0] = 0.0f;
			m_fPosition[1] = 0.0f;
			m_fPosition[2] = 0.0f;
			m_fVelocity[0] = 0.0f;
			m_fVelocity[1] = 0.0f;
			m_fVelocity[2] = 0.0f;

			//			metadata = new KeyValues(NULL);
			//
			//			m_pLinkedEntity = NULL;

			mALStreamingBufferData = null;
			mALCurrentWritePosition = 0;
			mALStreamingWriteUpperLimit = 0;
			mStreamingBufferSize = 0;
			sampleRate = 0;
			mSizeOfLastWrite = 0;
			mHitEOFYet = false;			
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

		private bool CheckStream(int buffer)
		{
			if ( !IsReady() ) return false;

			byte[] data = new byte[OPENAL_BUFFER_SIZE];
			mStreamingBufferSize = 0;

			mALStreamingBufferData = data;
			mALCurrentWritePosition = 0;
			mALStreamingWriteUpperLimit = OPENAL_BUFFER_SIZE;

			while ( mStreamingBufferSize < OPENAL_BUFFER_SIZE )
			{
				var state = LibFLACSharp.FLAC__stream_decoder_get_state(mDecoderContext);

				if (state == LibFLACSharp.StreamDecoderState.EndOfStream)
				{
					if (IsLooping)
					{
						//FLAC::Decoder::Stream::flush();
						//FLAC::Decoder::Stream::seek_absolute(0);

						LibFLACSharp.FLAC__stream_decoder_reset (mDecoderContext);
					}
					else
					{
						mHitEOFYet = true;
						break;
					}
				}
				else if ( state >= LibFLACSharp.StreamDecoderState.OggError )
				{
					// Critical error occured
					Warning(string.Format("FLAC: Decoding returned with critical state: {0}", state) );
					break;
				}

				FLACCheck (LibFLACSharp.FLAC__stream_decoder_process_single (mDecoderContext), "process single");

				// if we can't fit an additional frame into the buffer, quit
				if (mSizeOfLastWrite > mALStreamingWriteUpperLimit - mALCurrentWritePosition )
				{
					break;
				}
			}

			if (mHitEOFYet)
			{
				mIsFinished = true;
				return false;
			}
			else
			{
				BufferData(buffer, data, mStreamingBufferSize, sampleRate);
			}

			return true;
		}

		private IntPtr mDecoderContext;
		public void Run()
		{
			mALStreamingBufferData = null;
			mALCurrentWritePosition = 0;
			mALStreamingWriteUpperLimit = 0;
			mStreamingBufferSize = 0;
			sampleRate = 0;
			mSizeOfLastWrite = 0;
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

			mALStreamingBufferData = null;
			mHitEOFYet = false;

			mIsFinished = false; // Sample has just started, assume not finished

			InitAL();
		}

		#region Dispose

		/// <summary>
		/// Disposes this WaveStream
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// unmanaged memory
			mIsReady = false;
			mHitEOFYet = false;
			ClearBuffers();
			mStream.Close ();
			DestroyAL ();

			if (disposing)
			{
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

				//                if (m_stream != null)
				//                {
				//                    m_stream.Close();
				//                    m_stream.Dispose();
				//                    m_stream = null;
				//                }
				//
				//                if (m_reader != null)
				//                {
				//                    m_reader.Close();
				//                    m_reader = null;
				//                }
			}
			base.Dispose(disposing);
		}

		#endregion

		private void InitAL()
		{
			buffers = AL.GenBuffers(NUM_BUFFERS);
			ALError error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error generating a sample's buffers. Sample will not play.\n");
				OPENAL_ERROR(error);
				return;
			}

			source = AL.GenSource();
			error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error generating a sample's source. Sample will not play.\n");
				OPENAL_ERROR(error);
				return;
			}

			//AL.Source(source, ALSourcef.ReferenceDistance, valveUnitsPerMeter);
			error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: You need to update your audio drivers or OpenAL for sound to work properly.\n");
			}

			mIsReady = InitFormat();
		}

		private void DestroyAL()
		{
			mIsFinished = true; // Mark this for deleting and to be ignored by the thread.

			Stop();
			DestroyFormat();

			AL.DeleteSource(source);
			ALError error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error deleting a sound source. Destroying anyway.\n");
				OPENAL_ERROR(error);
			}

			AL.DeleteBuffers(buffers);
			error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error deleting buffers. Destroying anyway.\n");
				OPENAL_ERROR(error);
			}
		}

		#region Callbacks

		protected LibFLACSharp.StreamDecoderReadStatus ReadCallback(IntPtr context, IntPtr buffer, ref IntPtr bytes, IntPtr userData)
		{
			UInt64 noOfBytes = Convert.ToUInt64 (bytes);

			if(noOfBytes > 0) 
			{
				int length = (noOfBytes < OPENAL_BUFFER_SIZE) ? (int)noOfBytes : OPENAL_BUFFER_SIZE;
				byte[] streamChunk = new byte[length];
				int chunkSize = mStream.Read (streamChunk, 0, length);
				Marshal.Copy (streamChunk, 0, buffer, chunkSize);

				if (chunkSize < 0)
				{
					mHitEOFYet = true;
					return LibFLACSharp.StreamDecoderReadStatus.ReadStatusAbort;
				}
				else if (chunkSize == 0)
				{
					mHitEOFYet = true;
					return LibFLACSharp.StreamDecoderReadStatus.ReadStatusEndOfStream;
				}
				else
				{
					bytes = (IntPtr) chunkSize;
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


		private int m_samplesPerChannel;
		/// <summary>
		/// FLAC Write Call Back - libFlac notifies back on a frame that was read from the source file and written as a frame
		/// </summary>
		/// <param name="context"></param>
		/// <param name="frame"></param>
		/// <param name="buffer"></param>
		/// <param name="clientData"></param>
		protected LibFLACSharp.StreamDecoderWriteStatus WriteCallback(IntPtr context, IntPtr frame, IntPtr buffer, IntPtr clientData)
		{
			int leftChannel;
			int rightChannel;

			// Read the FLAC Frame into a memory samples buffer (m_flacSamples)
			LibFLACSharp.FlacFrame flacFrame = (LibFLACSharp.FlacFrame)Marshal.PtrToStructure(frame, typeof(LibFLACSharp.FlacFrame));

			// TODO: Write functions for handling 8-bit audio as well
			if (flacFrame.Header.BitsPerSample != 16)
			{
				Warning(string.Format("FLAC: Unsupported bit-rate: {0}", flacFrame.Header.BitsPerSample));
				return LibFLACSharp.StreamDecoderWriteStatus.WriteStatusAbort;
			}

			m_samplesPerChannel = flacFrame.Header.BlockSize;
			if (mFLACChannelData_Mono_0 == null)
			{
				// First time - Create Flac sample buffer
				mFLACChannelData_Mono_0 = new int[m_samplesPerChannel];
			}

			if (mFLACChannelData_Stereo_1 == null)
			{
				// First time - Create Flac sample buffer
				mFLACChannelData_Stereo_1 = new int[m_samplesPerChannel];
			}

			IntPtr pChannelBits = Marshal.ReadIntPtr(buffer, IntPtr.Size);
			Marshal.Copy(pChannelBits, mFLACChannelData_Mono_0, 0, m_samplesPerChannel);

			if (flacFrame.Header.Channels == 2)
			{
				IntPtr nextBits = Marshal.ReadIntPtr(buffer, 1 * IntPtr.Size);
				Marshal.Copy(nextBits, mFLACChannelData_Stereo_1, 0, m_samplesPerChannel);
			}

			/* write decoded PCM samples */
			if (flacFrame.Header.Channels == 2)
			{
				// Stereo
				for(uint i = 0; i < flacFrame.Header.BlockSize; i++) 
				{
					if (mALCurrentWritePosition != mALStreamingWriteUpperLimit)
					{
						leftChannel = mFLACChannelData_Mono_0[i];
						rightChannel = mFLACChannelData_Stereo_1[i];

						mALStreamingBufferData[mALCurrentWritePosition] =  Convert.ToByte(leftChannel >> 0);
						mALStreamingBufferData[mALCurrentWritePosition + 1] = Convert.ToByte(leftChannel >> 8);

						mALStreamingBufferData[mALCurrentWritePosition + 2] = Convert.ToByte(rightChannel >> 0);
						mALStreamingBufferData[mALCurrentWritePosition + 3] =  Convert.ToByte(rightChannel >> 8);

						mALCurrentWritePosition += 4;
					}
					else
					{
						mHitEOFYet = true;
						return LibFLACSharp.StreamDecoderWriteStatus.WriteStatusAbort;
					}
				}

				mSizeOfLastWrite = flacFrame.Header.BlockSize * 4;   
				mStreamingBufferSize += mSizeOfLastWrite;
			}
			else
			{
				// Mono
				for(uint i = 0; i < flacFrame.Header.BlockSize; i++) 
				{
					if (mALCurrentWritePosition != mALStreamingWriteUpperLimit)
					{
						leftChannel = mFLACChannelData_Mono_0[i];

						mALStreamingBufferData[mALCurrentWritePosition] =  Convert.ToByte(leftChannel >> 0);
						mALStreamingBufferData[mALCurrentWritePosition + 1] = Convert.ToByte(leftChannel >> 8);

						mALCurrentWritePosition += 2;
					}
					else
					{
						mHitEOFYet = true;
						return LibFLACSharp.StreamDecoderWriteStatus.WriteStatusAbort;
					}
				}

				mSizeOfLastWrite = flacFrame.Header.BlockSize * 2;
				mStreamingBufferSize += mSizeOfLastWrite;
			}

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
					sampleRate = streamInfo.SampleRate;

					if (bits == 16)
					{
						Format = channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;
							}
					else if ( bits == 8 )
					{
						Format = channels == 2 ? ALFormat.Stereo8 : ALFormat.Mono8;
					}
					else
					{
						Warning(string.Format("FLAC: Unsupported sample bit size: {0}\n", bits));
					}

					// Debug header
					if (!IsLooping)
					{
						Msg(string.Format("FLAC: {0} bits {1} audio at {2}\n",
							bits,
							channels == 2 ? "stereo" : "mono",
							sampleRate));
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

		#region implemented abstract members of Stream

		public override void Flush ()
		{
			throw new NotImplementedException ();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			return mStream.Seek (offset, origin);
		}

		public override void SetLength (long value)
		{
			throw new NotImplementedException ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			return mStream.Read (buffer, offset, count);
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException ();
		}

		public override bool CanRead {
			get {
				return true;
			}
		}
		public override bool CanSeek {
			get {
				return mStream.CanSeek;
			}
		}
		public override bool CanWrite {
			get {
				return false;
			}
		}
		public override long Length {
			get {
				return mStream.Length;
			}
		}
		public override long Position {
			get {
				return mStream.Position;
			}
			set {
				throw new NotImplementedException ();
			}
		}
		#endregion
		
	}
}