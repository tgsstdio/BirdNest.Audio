using System;

namespace AudioEngine
{
	public class SoundEffect : IEffect
	{
		private SoundMachine mMachine;
		private SoundSource mSource;
		private SoundTrack mTrack;
		public SoundEffect (SoundMachine machine, SoundSource source, SoundTrack track)
		{
			mMachine = machine;
			mSource = source;
			mTrack = track;
		}

		#region IEffect implementation

		public void Apply (float now)
		{
			if (mMachine.Contexts.Length > 1)
			{
				foreach (var context in mMachine.Contexts)
				{
					context.MakeCurrent ();
					ApplySound ();
				}
			} 
			else
			{
				ApplySound ();
			}

			throw new NotImplementedException ();
		}

		private void ApplySound()
		{

		}

		public void Reset ()
		{
			if (mMachine.Contexts.Length > 1)
			{
				foreach (var context in mMachine.Contexts)
				{
					context.MakeCurrent ();
					ResetSound ();
				}
			} 
			else
			{
				ResetSound();
			}

			throw new NotImplementedException ();
		}

		private void ResetSound()
		{

		}

		public int Id {
			get;
			set;
		}

		public float Delay {
			get;
			set;
		}

		public float Length {
			get;
			set;
		}

		public float TimeToStart {
			get;
			set;
		}

		public float TimeToEnd {
			get;
			set;
		}

		#endregion
	}
}

