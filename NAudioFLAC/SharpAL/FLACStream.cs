using System;
using System.IO;
using OpenTK.Audio.OpenAL;

namespace SharpAL
{
	public class FLACStream : Stream
	{
		const int NUM_BUFFERS = 4;

		uint[] buffers = new uint[NUM_BUFFERS];  // Buffers to queue our data into
		uint source;            				  // Our source's identifier for OpenAL
		ALFormat format;                	// A simple OpenAL-usable format description

		//#define OPENAL_BUFFER_SIZE 65536 // 65536 bytes = 64KB
		const int  OPENAL_BUFFER_SIZE  = 16384;

		public bool m_bStreaming; // Are we in streaming mode, or should we preload the data?
		public bool m_bFinished;  // Are we finished playing this sound? Can we delete this?
		public bool m_bLooping;   // Is this sample in a constant looping state?
		public bool m_bReady;     // Are we ready to play this file?
		public bool m_bRequiresSync; // If this is true, we syncronize with the engine.
		public bool m_bPositional; // Are we placed in a world position?
		public bool m_bPersistent; // Do not delete this sample automatically, used for pointers that are stored

		public float[] m_fPosition = new float[3]; // Where is this sample source?
		public float[] m_fVelocity = new float[3]; // In which velocity is our source playing?
		public float m_fGain; // This is the gain of our sound
		public float m_fFadeScalar; // The gain of our sound is multiplied by this

		Stream flacFile;

		byte[] m_pWriteData;
		byte[] m_pWriteDataEnd;
		int size;
		int sampleRate;
		int sizeOfLast;
		bool m_bHitEOF;

		void DevMsg(string message)
		{

		}

		void OPENAL_ERROR(ALError error)
		{

		}

		void Warning(string message)
		{

		}

		void Update(float updateTime)
		{
			if (m_bFinished)
			{
				Destroy();
				return;
			}

			if (!IsReady())
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

			m_bRequiresSync = false;
		}

		void UpdateBuffers(float lastUpdate)
		{
			int state, processed;
			bool active = false;

			AL.GetSource(source, ALGetSourcei.SourceState, out state);
			AL.GetSource(source, ALGetSourcei.BuffersProcessed, out processed);

			while (processed-- > 0)
			{
				uint buffer = 0;
				ALError error;

				AL.SourceUnqueueBuffers(source, 1, ref buffer);
				error = AL.GetError();
				if (error != ALError.NoError)
				{
					Warning("OpenAL: There was an error unqueuing a buffer. Issues may arise.\n");
					OPENAL_ERROR(error);
				}

				active = CheckStream(buffer);

				// I know that this block seems odd, but it's buffer overrun protection. Keep it here.
				if (active)
				{
					AL.SourceQueueBuffers(source, 1, ref buffer);
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
			if (!m_bRequiresSync && !m_pLinkedEntity) return;

			float[] position = new float[3];
			float[] velocity = new float[3];

			if (m_pLinkedEntity )
			{
				/*
		// TODO: Provide methods for better control of this position
				position[0] = m_pLinkedEntity->GetAbsOrigin().x;
				position[1] = m_pLinkedEntity->GetAbsOrigin().y;
				position[2] = m_pLinkedEntity->GetAbsOrigin().y;

				velocity[0] = m_pLinkedEntity->GetAbsVelocity().x;
				velocity[1] = m_pLinkedEntity->GetAbsVelocity().y;
				velocity[2] = m_pLinkedEntity->GetAbsVelocity().z;
				*/

				position[0] = m_pLinkedEntity->GetLocalOrigin().x;
				position[1] = m_pLinkedEntity->GetLocalOrigin().y;
				position[2] = m_pLinkedEntity->GetLocalOrigin().y;

				velocity[0] = m_pLinkedEntity->GetLocalVelocity().x;
				velocity[1] = m_pLinkedEntity->GetLocalVelocity().y;
				velocity[2] = m_pLinkedEntity->GetLocalVelocity().z;
			}
			else
			{
				if (m_bPositional)
				{
					position[0] = m_fPosition[0];
					position[1] = m_fPosition[1];
					position[2] = m_fPosition[2];

					velocity[0] = m_fVelocity[0];
					velocity[1] = m_fVelocity[1];
					velocity[2] = m_fVelocity[2];
				}
				else
				{
					position[0] = 0.0f;
					position[1] = 0.0f;
					position[2] = 0.0f;

					velocity[0] = 0.0f;
					velocity[1] = 0.0f;
					velocity[2] = 0.0f;
				}
			}

			// alSource3f(source, AL_POSITION, VALVEUNITS_TO_METERS(position[0]), VALVEUNITS_TO_METERS(position[1]), VALVEUNITS_TO_METERS(position[2]));
			AL.Source(source, ALSource3f.Position, position[0], position[1], position[2]);
			if (AL.GetError() !=  ALError.NoError)
				Warning("OpenAL: Couldn't update a source's position.\n");

			// alSource3f(source, AL_VELOCITY, VALVEUNITS_TO_METERS(velocity[0]), VALVEUNITS_TO_METERS(velocity[1]), VALVEUNITS_TO_METERS(velocity[2]));
			AL.Source(source, ALSource3f.Velocity, velocity[0], velocity[1], velocity[2]);
			if (AL.GetError() != ALError.NoError)
				Warning("OpenAL: Couldn't update a source's velocity.\n");

			AL.Source(source, ALSourcef.Gain, m_fGain * m_fFadeScalar);
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
		bool IsReady()
		{
			return m_bReady && !m_bFinished;
		}

		bool IsFinished()
		{
			if (m_bFinished)
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

		bool IsPositional()
		{
			return m_bPositional;
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

			m_bPositional = positional;
			ALError error;
			if (m_bPositional)
			{
				m_bRequiresSync = true;
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

			m_bRequiresSync = true;
		}

		void SetPosition(float x, float y, float z)
		{
			m_fPosition[0] = x;
			m_fPosition[1] = y;
			m_fPosition[2] = z;

			m_bRequiresSync = true;
		}

		void SetPosition(float[] position)
		{
			m_fPosition[0] = position[0];
			m_fPosition[1] = position[1];
			m_fPosition[2] = position[2];

			m_bRequiresSync = true;
		}

		void SetVelocity(float[] velocity)
		{
			m_fVelocity[0] = velocity[0];
			m_fVelocity[1] = velocity[1];
			m_fVelocity[2] = velocity[2];

			m_bRequiresSync = true;
		}

		void SetGain(float newGain) { m_fGain = newGain; }

		//		/*
		//        NOTE: All samples that do not call Persist() will be automatically deleted
		//        when they're done playing to prevent memory leaks. Call Persist() on any
		//        sample that you plan on storing to prevent this from happening.
		//    	*/
		void Persist() {
			m_bPersistent = true; 
		}

		bool IsPersistent() { 
			return m_bPersistent; 
		}

		void SetLooping(bool shouldLoop)
		{
			m_bLooping = shouldLoop;
		}

		void ClearBuffers()
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
		void BufferData(uint bufferID, ALFormat format, IntPtr data, int size, int freq)
		{
			AL.BufferData(bufferID, format, data, size, freq);
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
			flacFile = stream;

			m_bStreaming = false;
			m_bFinished = false;
			m_bLooping = false;
			m_bReady = false;
			m_bRequiresSync = true;
			m_bPositional = false;
			m_bPersistent = false;

			m_fGain = 1.0f;
			m_fFadeScalar = 1.0f;

			m_fPosition[0] = 0.0f;
			m_fPosition[1] = 0.0f;
			m_fPosition[2] = 0.0f;
			m_fVelocity[0] = 0.0f;
			m_fVelocity[1] = 0.0f;
			m_fVelocity[2] = 0.0f;

			//			metadata = new KeyValues(NULL);
			//
			//			m_pLinkedEntity = NULL;

			m_pWriteData = null;
			m_pWriteDataEnd = null;
			size = 0;
			sampleRate = 0;
			sizeOfLast = 0;
			m_bHitEOF = false;			
		}

		bool CheckStream(uint buffer)
		{
			if ( !IsReady() ) return false;

			char[] data = new char[OPENAL_BUFFER_SIZE];
			size = 0;

			m_pWriteData = data;
			m_pWriteDataEnd = m_pWriteData + OPENAL_BUFFER_SIZE;

			while ( size < OPENAL_BUFFER_SIZE )
			{
				FLAC__StreamDecoderState state = FLAC::Decoder::Stream::get_state();

				if (state == FLAC__STREAM_DECODER_END_OF_STREAM)
				{
					if (m_bLooping)
					{
						//FLAC::Decoder::Stream::flush();
						//FLAC::Decoder::Stream::seek_absolute(0);

						FLAC::Decoder::Stream::reset();
					}
					else
					{
						m_bHitEOF = true;
						break;
					}
				}
				else if ( state >= FLAC__STREAM_DECODER_OGG_ERROR )
				{
					// Critical error occured
					Warning("FLAC: Decoding returned with critical state: %s", FLAC__StreamDecoderStateString[state] );
					break;
				}

				if ( !FLAC::Decoder::Stream::process_single() )
				{
					Warning("FLAC: Processing of a single frame failed!\n");
					break;
				}

				// if we can't fit an additional frame into the buffer, quit
				if (sizeOfLast > m_pWriteDataEnd-m_pWriteData )
				{
					break;
				}
			}

			if (m_bHitEOF)
			{
				m_bFinished = true;
				return false;
			}
			else
			{
				BufferData(buffer, format, data, size, sampleRate);
			}

			return true;
		}

		public void Open(string filename)
		{
			char[] abspath = new char[MAX_PATH_LENGTH];

			m_pWriteData = null;
			m_pWriteDataEnd = null;
			size = 0;
			sampleRate = 0;
			sizeOfLast = 0;
			m_bHitEOF = false;

			if (!FLAC::Decoder::Stream::is_valid())
			{
				FLAC__StreamDecoderState state = FLAC::Decoder::Stream::get_state();
				Warning("FLAC: Unable to initialize: %s", FLAC__StreamDecoderStateString[state] );
				return;
			}

			// Gets an absolute path to the provided filename
			g_OpenALGameSystem.GetSoundPath(filename, abspath, sizeof(abspath));

			flacFile = filesystem->Open(abspath, "rb");

			if (!flacFile)
			{
				Warning("FLAC: Could not open flac file: %s. Aborting.\n", filename);
				return;
			}

			FLAC__StreamDecoderInitStatus status = FLAC::Decoder::Stream::init();

			if (status != FLAC__STREAM_DECODER_INIT_STATUS_OK)
			{
				filesystem->Close(flacFile);
				Warning("FLAC: Critical stream decoder init status: %s", FLAC__StreamDecoderInitStatusString[status] );
				return;
			}

			m_pWriteData = null;
			m_bHitEOF = false;

			m_bFinished = false; // Sample has just started, assume not finished

			InitAL();
		}

		public override void Close()
		{
			m_bReady = false;
			m_bHitEOF = false;
			ClearBuffers();
			flacFile.Close ();
			//filesystem->Close(flacFile);

			if ( !FLAC::Decoder::Stream::finish() )
				Warning("FLAC; Memory allocation error on stream finish!\n");
		}

		void InitAL()
		{
			AL.GenBuffers(NUM_BUFFERS, buffers);
			ALError error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error generating a sample's buffers. Sample will not play.\n");
				OPENAL_ERROR(error);
				return;
			}

			AL.GenSource(out source);
			error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error generating a sample's source. Sample will not play.\n");
				OPENAL_ERROR(error);
				return;
			}

			AL.Source(source, ALSourcef.ReferenceDistance, valveUnitsPerMeter);
			error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: You need to update your audio drivers or OpenAL for sound to work properly.\n");
			}

			m_bReady = InitFormat();
			g_OpenALGameSystem.Add(this);
		}

		void Destroy()
		{
			m_bFinished = true; // Mark this for deleting and to be ignored by the thread.

			Stop();
			DestroyFormat();

			AL.DeleteSource(ref source);
			ALError error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error deleting a sound source. Destroying anyway.\n");
				OPENAL_ERROR(error);
			}

			AL.DeleteBuffers(NUM_BUFFERS, ref buffers);
			error = AL.GetError();
			if ( error != ALError.NoError)
			{
				Warning("OpenAL: Error deleting buffers. Destroying anyway.\n");
				OPENAL_ERROR(error);
			}
		}

		#region Callbacks

		FLAC__StreamDecoderReadStatus read_callback(FLAC__byte buffer[], size_t *bytes)
		{
			if(*bytes > 0) 
			{
				int size = filesystem->ReadEx(buffer, sizeof(FLAC__byte), *bytes, flacFile);

				if (size < 0)
				{
					m_bHitEOF = true;
					return FLAC__STREAM_DECODER_READ_STATUS_ABORT;
				}
				else if (size == 0)
				{
					m_bHitEOF = true;
					return FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM;
				}
				else
				{
					return FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
				}
			}
			else
			{
				m_bHitEOF = true;
				return FLAC__STREAM_DECODER_READ_STATUS_ABORT;
			}
		}

		FLAC__StreamDecoderSeekStatus seek_callback(FLAC__uint64 absolute_byte_offset)
		{
			filesystem->Seek( flacFile, (off_t)absolute_byte_offset, FILESYSTEM_SEEK_HEAD );
			return FLAC__STREAM_DECODER_SEEK_STATUS_OK;
		}

		FLAC__StreamDecoderTellStatus tell_callback(FLAC__uint64 *absolute_byte_offset)
		{
			int offset = filesystem->Tell( flacFile );
			*absolute_byte_offset = (FLAC__uint64)offset;
			return FLAC__STREAM_DECODER_TELL_STATUS_OK;
		}

		FLAC__StreamDecoderLengthStatus length_callback(FLAC__uint64 *stream_length)
		{
			int size = filesystem->Size( flacFile );
			*stream_length = (FLAC__uint64)size;
			return FLAC__STREAM_DECODER_LENGTH_STATUS_OK;
		}

		bool eof_callback()
		{
			bool hitEOF = filesystem->EndOfFile( flacFile );

			if (hitEOF)
			{
				m_bHitEOF = true;
			}

			return hitEOF;
		}

		FLAC__StreamDecoderWriteStatus write_callback(const ::FLAC__Frame *frame, const FLAC__int32 * const buffer[])
		{
			register signed int sample0, sample1;

			// TODO: Write functions for handling 8-bit audio as well
			if (frame->header.bits_per_sample != 16)
			{
				Warning("FLAC: Unsupported bit-rate: %i", frame->header.bits_per_sample);
				return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
			}

			/* write decoded PCM samples */
			if (frame->header.channels == 2)
			{
				// Stereo
				for(unsigned int i = 0; i < frame->header.blocksize; i++) 
				{
					if (m_pWriteData != m_pWriteDataEnd)
					{
						sample0 = buffer[0][i];
						sample1 = buffer[1][i];

						m_pWriteData[0] = sample0 >> 0;
						m_pWriteData[1] = sample0 >> 8;

						m_pWriteData[2] = sample1 >> 0;
						m_pWriteData[3] = sample1 >> 8;

						m_pWriteData += 4;
					}
					else
					{
						m_bHitEOF = true;
						return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
					}
				}

				sizeOfLast = frame->header.blocksize * 2 * 2;   
				size += sizeOfLast;
			}
			else
			{
				// Mono
				for(unsigned int i = 0; i < frame->header.blocksize; i++) 
				{
					if (m_pWriteData != m_pWriteDataEnd)
					{
						sample0 = buffer[0][i];

						m_pWriteData[0] = sample0 >> 0;
						m_pWriteData[1] = sample0 >> 8;

						m_pWriteData += 2;
					}
					else
					{
						m_bHitEOF = true;
						return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
					}
				}

				sizeOfLast = frame->header.blocksize * 2;   
				size += sizeOfLast;
			}

			return FLAC__STREAM_DECODER_WRITE_STATUS_CONTINUE;
		}

		void metadata_callback(const FLAC__StreamMetadata *metadata)
		{
			int bits = metadata->data.stream_info.bits_per_sample;
			int channels = metadata->data.stream_info.channels;
			sampleRate = metadata->data.stream_info.sample_rate;

			if (bits == 16)
			{
				format = channels == 2 ? AL_FORMAT_STEREO16 : AL_FORMAT_MONO16;
			}
			else if ( bits == 8 )
			{
				format = channels == 2 ? AL_FORMAT_STEREO8 : AL_FORMAT_MONO8;
			}
			else
			{
				Warning("FLAC: Unsupported sample bit size: %i\n", bits);
			}

			// Debug header
			if (!m_bLooping)
			{
				Msg("FLAC: %i bits %s audio at %i\n",
					bits,
					channels == 2 ? "stereo" : "mono",
					sampleRate);
			}
		}

		void error_callback(::FLAC__StreamDecoderErrorStatus status)
		{
			// All calls to this function is critical
			Warning("FLAC: Got decoding error callback: %s\n", FLAC__StreamDecoderErrorStatusString[status]);
		}

		#endregion

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
			throw new NotImplementedException ();
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
				throw new NotImplementedException ();
			}
		}
		public override bool CanWrite {
			get {
				return false;
			}
		}
		public override long Length {
			get {
				throw new NotImplementedException ();
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
		
	}
}