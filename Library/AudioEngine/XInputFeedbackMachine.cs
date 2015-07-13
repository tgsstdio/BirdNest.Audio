using System;
using XInputDotNetPure;

namespace AudioEngine
{
	public class XInputFeedbackMachine : IForceFeedbackMachine
	{
		public class ControllerData
		{
			public PlayerIndex PlayerIndex { get; set; }

			public float Left {
				get;
				set;
			}

			public float Right {
				get;
				set;
			}
		}

		#region IForceFeedbackMachine implementation

		private readonly int NO_OF_CONTROLLERS = 4;
		public ControllerData[] Controllers { get; private set; }
		public void Initialise()
		{
			Controllers = new ControllerData[NO_OF_CONTROLLERS];
			for (int i = 0; i < NO_OF_CONTROLLERS; ++i)
			{
				Controllers[i] = new ControllerData{PlayerIndex = (PlayerIndex) i, Left = 0f, Right = 0f };
			}
		}

		public void Apply ()
		{
			foreach (var data in Controllers) {
				if (GamePad.GetState (data.PlayerIndex).IsConnected) {
					GamePad.SetVibration (data.PlayerIndex, data.Left, data.Right);
				}
			}
		}

		#endregion
	}
}

