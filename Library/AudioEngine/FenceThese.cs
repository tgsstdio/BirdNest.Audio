using System;
using OpenTK.Graphics.OpenGL;

namespace AudioEngine
{
	public class FenceThese
	{
		private Action[] mActionsAfter;
		private Action[] mActionsBefore;
		private long mDurationInNanoSecs;
		private int mNoOfTimes;
		public IntPtr SyncObject { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AudioEngine.FenceThese"/> class.
		/// </summary>
		/// <param name="before">GPU based actions to run before fence is added to GPU command list.</param>
		/// <param name="after">Actions for CPU (i.e. not GPU based) to complete in the meanwhile while waiting with the same thread.</param>
		/// <param name="fallbackInNanoSecs">Fallback in approximate nanoseconds for blocking waits</param>
		/// <param name="noOfTimes">No of times to repeat blocking waits </param>
		public FenceThese (Action[] before, Action[] after, int noOfTimes, long fallbackInNanoSecs)
		{
			mNoOfTimes = noOfTimes;
			mDurationInNanoSecs = fallbackInNanoSecs;
			mActionsAfter = after;
			mActionsBefore = before;
			IsWaiting = false;
			Index = 0;
		}

		public int Index { get; private set; }
		public bool IsWaiting { get; private set; }

		private void CleanUp()
		{
			GL.DeleteSync (this.SyncObject);
		}

		private void BlockingWait ()
		{
			int times = 0;
			do
			{
				WaitSyncStatus status = GL.ClientWaitSync (SyncObject, ClientWaitSyncFlags.None, mDurationInNanoSecs);
				if (status == WaitSyncStatus.WaitFailed)
				{
					throw new InvalidOperationException ("GPU Wait sync failed - surplus actions completed");
				}
				else if (status == WaitSyncStatus.ConditionSatisfied || status == WaitSyncStatus.AlreadySignaled)
				{
					IsWaiting = false;
					return;
				}
				++times;
			}
			while (times < mNoOfTimes);

			// TODO : final fallback ?? a second of waiting
		}

		private bool ActionsRemain ()
		{
			return Index < mActionsAfter.Length;
		}

		private void PerformNextAction ()
		{
			mActionsAfter [Index] ();
			++Index;
		}

		private void FinishOffRemainingTasks ()
		{
			// complete any surplus actions
			while (ActionsRemain ())
			{
				PerformNextAction ();
			}
		}

		private void NonBlockingWait ()
		{
			// only on the first time
			ClientWaitSyncFlags waitOption = ClientWaitSyncFlags.SyncFlushCommandsBit;
			while (ActionsRemain ())
			{
				WaitSyncStatus result = GL.ClientWaitSync (SyncObject, waitOption, 0);
				waitOption = ClientWaitSyncFlags.None;
				if (result == WaitSyncStatus.WaitFailed)
				{
					throw new InvalidOperationException ("GPU Wait sync failed - surplus actions incomplete");
				}
				if (result == WaitSyncStatus.ConditionSatisfied || result == WaitSyncStatus.AlreadySignaled)
				{
					IsWaiting = false;
					break;
				}
				// perform next action in the meanwhile
				PerformNextAction ();
			}
			FinishOffRemainingTasks ();
		}

		public void Run()
		{
			foreach (Action doable in mActionsBefore)
			{
				doable ();
			}
			SyncObject = GL.FenceSync (SyncCondition.SyncGpuCommandsComplete, 0);
			IsWaiting = true;

			NonBlockingWait ();
			if (IsWaiting)
			{
				BlockingWait ();
			}
			CleanUp ();
		}
	}
}

