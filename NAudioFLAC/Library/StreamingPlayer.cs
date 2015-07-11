using System;
using OpenTK.Audio.OpenAL;

namespace BigMansStuff.NAudio.FLAC
{
	public class StreamingPlayer
	{
		const int NUM_BUFFERS = 4;

		private int[] buffers;  // Buffers to queue our data into
		private int source;            				  // Our source's identifier for OpenAL
		public ALFormat Format;                	// A simple OpenAL-usable format description

		private long mALCurrentWritePosition;
		private long mALStreamingWriteUpperLimit;
		private int mStreamingBufferSize;

		//#define OPENAL_BUFFER_SIZE 65536 // 65536 bytes = 64KB
		private const Int64 OPENAL_BUFFER_SIZE  = 16384;

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

		public StreamingPlayer ()
		{

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

			mALCurrentWritePosition = 0;
			mALStreamingWriteUpperLimit = 0;
			mStreamingBufferSize = 0;
	
		}

		private bool CheckStream(int buffer)
		{
			throw new NotImplementedException ();

//			if ( !IsReady() ) return false;
//
//			byte[] data = new byte[OPENAL_BUFFER_SIZE];
//			mStreamingBufferSize = 0;
//
//			mALStreamingBufferData = data;
//			mALCurrentWritePosition = 0;
//			mALStreamingWriteUpperLimit = OPENAL_BUFFER_SIZE;
//
//			while ( mStreamingBufferSize < OPENAL_BUFFER_SIZE )
//			{
//				var state = LibFLACSharp.FLAC__stream_decoder_get_state(mDecoderContext);
//
//				if (state == LibFLACSharp.StreamDecoderState.EndOfStream)
//				{
//					if (IsLooping)
//					{
//						//FLAC::Decoder::Stream::flush();
//						//FLAC::Decoder::Stream::seek_absolute(0);
//
//						LibFLACSharp.FLAC__stream_decoder_reset (mDecoderContext);
//					}
//					else
//					{
//						mHitEOFYet = true;
//						break;
//					}
//				}
//				else if ( state >= LibFLACSharp.StreamDecoderState.OggError )
//				{
//					// Critical error occured
//					Warning(string.Format("FLAC: Decoding returned with critical state: {0}", state) );
//					break;
//				}
//
//				FLACCheck (LibFLACSharp.FLAC__stream_decoder_process_single (mDecoderContext), "process single");
//
//				// if we can't fit an additional frame into the buffer, quit
//				if (mSizeOfLastWrite > mALStreamingWriteUpperLimit - mALCurrentWritePosition )
//				{
//					break;
//				}
//			}
//
//			if (mHitEOFYet)
//			{
//				mIsFinished = true;
//				return false;
//			}
//			else
//			{
//				BufferData(buffer, data, mStreamingBufferSize, sampleRate);
//			}
//
//			return true;
		}

		public void Update(float updateTime)
		{
			throw new NotImplementedException ();
//			if (mIsFinished)
//			{
//				DestroyAL();
//				return;
//			}
//
//			if (IsReady())
//			{
//				Warning("OpenAL: Sample update requested while not ready. Skipping Update.\n");
//				return;
//			}
//
//			/***
//			 * Let's do any processing that needs to be done for syncronizing with the game engine
//			 * prior to working on the buffers.
//			 ***/
//			UpdatePositional(updateTime);
//			UpdateBuffers(updateTime);
//
////			RequiresSync = false;
		}

		void Stop()
		{
			throw new NotImplementedException ();
//			if (!IsPlaying())
//				return; // Whachootockinaboutwillis?
//
//			AL.SourceRewind(source);
//			var error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				Warning("OpenAL: Error stopping a sound. This is less than good news.\n");
//				OPENAL_ERROR(error);
//			}
//
//			ClearBuffers();
		}

		void Pause()
		{
			throw new NotImplementedException ();
			//Warning("OpenAL: Pausing hasn't been implemented yet?! That's ridiculous...\n");
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

		private void ClearBuffers()
		{
			throw new NotImplementedException ();
//			if (IsPlaying())
//			{
//				DevMsg("OpenAL: ClearBuffers() called while playing. Sample will stop now.\n");
//				Stop();
//			}
//
//			AL.Source(source, ALSourcei.Buffer, 0);
//
//			var error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				Warning("OpenAL: An error occured while attempting to clear a source's buffers.\n");
//				OPENAL_ERROR(error);
//			}
		}

		/***
 		* Keep those buffers flowing.
 		***/
		private void BufferData(int bufferID, byte[] data, int size, int freq)
		{
			throw new NotImplementedException ();
//			AL.BufferData<byte>(bufferID, Format, data, size, freq);
//			var error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				Warning("OpenAL: There was an error buffering audio data. Releasing deadly neurotoxin in 3... 2.. 1..\n");
//				OPENAL_ERROR(error);
//			}
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

//				error = AL.GetError();
//				if ( error != ALError.NoError)
//				{
//					Warning("OpenAL: Couldn't update rolloff factor to enable positional audio.\n");
//					OPENAL_ERROR(error);
//				}
			}
			else
			{
				AL.Source(source, ALSourceb.SourceRelative, true);
				AL.Source(source, ALSourcef.RolloffFactor, 0f);

//				error = AL.GetError();
//				if ( error != ALError.NoError)
//				{
//					Warning("OpenAL: Couldn't update rolloff factor to disable positional audio.\n");
//					OPENAL_ERROR(error);
//				}
			}

			RequiresSync = true;
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
			mIsReady = false;
			ClearBuffers();
			DestroyAL ();

			if (disposing)
			{
				// managed code
			}

			mIsDisposed = true;
		}

		private void DestroyAL()
		{
			throw new NotImplementedException ();
//			mIsFinished = true; // Mark this for deleting and to be ignored by the thread.
//
//			Stop();
//
//			if (AL.IsSource (source))
//			{
//				AL.DeleteSource (source);
//				var error = AL.GetError ();
//				if (error != ALError.NoError)
//				{
//					Warning ("OpenAL: Error deleting a sound source. Destroying anyway.\n");
//					OPENAL_ERROR (error);
//				}
//			}
//
//			if (buffers != null)
//			{
//				AL.DeleteBuffers (buffers);
//				var error = AL.GetError ();
//				if (error != ALError.NoError)
//				{
//					Warning ("OpenAL: Error deleting buffers. Destroying anyway.\n");
//					OPENAL_ERROR (error);
//				}
//			}
		}

		private void InitAL()
		{
			buffers = AL.GenBuffers(NUM_BUFFERS);
//			ALError error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				Warning("OpenAL: Error generating a sample's buffers. Sample will not play.\n");
//				OPENAL_ERROR(error);
//				return;
//			}
//
//			source = AL.GenSource();
//			error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				Warning("OpenAL: Error generating a sample's source. Sample will not play.\n");
//				OPENAL_ERROR(error);
//				return;
//			}

			//AL.Source(source, ALSourcef.ReferenceDistance, valveUnitsPerMeter);
//			error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				Warning("OpenAL: You need to update your audio drivers or OpenAL for sound to work properly.\n");
//			}
//
			mIsReady = true;
		}

		bool IsPlaying()
		{
			int state;
			AL.GetSource(source, ALGetSourcei.SourceState, out state);

//			var error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				OPENAL_ERROR(error); // Spy's sappin' mah error buffer!
//			}

			return (state == (int) ALSourceState.Playing);
		}

		/***
		//* Checks whether or not this sample is currently ready to be played.
		// 		***/
		public bool IsReady()
		{
			return mIsReady && !mIsFinished;
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
//				if (error != ALError.NoError)
//				{
//					Warning("OpenAL: There was an error unqueuing a buffer. Issues may arise.\n");
//					OPENAL_ERROR(error);
//				}

				active = CheckStream(buffer[0]);

				// I know that this block seems odd, but it's buffer overrun protection. Keep it here.
				if (active)
				{
					AL.SourceQueueBuffers(source, 1, buffer);
//					error = AL.GetError();
//					if (error != ALError.NoError)
//					{
//						Warning("OpenAL: There was an error queueing a buffer. Expect some turbulence.\n");
//						OPENAL_ERROR(error);
//					}

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
		public void Play()
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

//			if (buffersToQueue == 0)
//			{
//				Warning("OpenAL: Couldn't play a stream.\n");
//				return;
//			}

			ALError error;

			AL.SourceQueueBuffers(source, buffersToQueue, buffers);
//			error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				Warning("OpenAL: There was an error queueing buffers. This will probably fix itself, but it's still not ideal.\n");
//				OPENAL_ERROR(error);
//			}

			AL.SourcePlay(source);

//			error = AL.GetError();
//			if ( error != ALError.NoError)
//			{
//				Warning("OpenAL: Playing an audio sample failed horribly.\n");
//				OPENAL_ERROR(error);
//			}
		}
	}
}

