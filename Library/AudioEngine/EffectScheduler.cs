using System;

namespace AudioEngine
{
	public class EffectScheduler
	{
		private float mCurrentTime;
		private IForceFeedbackMachine[] mMachines;
		public EffectScheduler (IForceFeedbackMachine[] machines, float currentTime)
		{
			mMachines = machines;
			mCurrentTime = currentTime;
		}

		private C5.TreeBag<IEffect> mQueuedEffects;
		private C5.TreeBag<IEffect> mActiveEffects;
		private int mNoOfActiveEffects;
		private int mNoOfQueuedEffects;
		public void Initialise()
		{
			mNoOfActiveEffects = 0;
			mNoOfQueuedEffects = 0;
			mQueuedEffects = new C5.TreeBag<IEffect> (new TimeToStartCutoffComparer());
			mActiveEffects = new C5.TreeBag<IEffect> (new TimeToEndCutoffComparer());
		}

		public void Raise(IEffect effect)
		{
			effect.TimeToStart= mCurrentTime + effect.Delay;
			effect.TimeToEnd = effect.TimeToStart + effect.Length;

			mQueuedEffects.Add(effect);
			++mNoOfQueuedEffects;
		}

		private class Cutoff : IEffect
		{
			#region IEffect implementation

			public void Apply (float now)
			{
				throw new NotImplementedException ();
			}

			public void Reset ()
			{
				throw new NotImplementedException ();
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

		public void Update(float timeStepInSeconds)
		{
			float now = mCurrentTime + timeStepInSeconds;
			IEffect queuedTo = new Cutoff{ TimeToStart = now };
			IEffect activeTo = new Cutoff{ TimeToEnd = now };

			// MOVE EFFECTS TO APPLIED LISTS
			MoveQueuedEffectsToActive (queuedTo);

			// RESET MOTORS
			ResetAndRemoveExpiryEffects (activeTo);
			UpdateActiveEffects (now);
			ApplyEffect ();
			mCurrentTime = now;
		}

		private void UpdateActiveEffects(float now)
		{
			foreach (var effect in mActiveEffects)
			{
				effect.Apply (now);
			}
		}

		private void ApplyEffect()
		{
			foreach (var machine in mMachines)
			{
				machine.Apply ();
			}
		}

		private void MoveQueuedEffectsToActive (IEffect cutoff)
		{
			foreach (var effect in mQueuedEffects.RangeTo (cutoff)) 
			{
				mActiveEffects.Add (effect);
				++mNoOfActiveEffects;
				--mNoOfQueuedEffects;				
			}
			mQueuedEffects.RemoveRangeTo (cutoff);
		}

		private void ResetAndRemoveExpiryEffects (IEffect cutoff)
		{
			foreach (var effect in mActiveEffects.RangeTo (cutoff)) 
			{
				effect.Reset ();
				--mNoOfActiveEffects;
			}
			mActiveEffects.RemoveRangeTo (cutoff);
		}

	}
}

