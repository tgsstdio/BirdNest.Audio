using System;

namespace AudioEngine
{
	public class XInputFeedbackEffect : IEffect, IForceFeedbackEffect
	{
		private XInputFeedbackMachine mFeedbackSystem;
		public XInputFeedbackEffect (XInputFeedbackMachine machine)
		{
			mFeedbackSystem = machine;
		}

		public int PlayerIndex {get;set;}
		public int MotorIndex {get;set;}
		public FeedbackType FeedbackType { get; set;}

		public void Apply (float now)
		{
			float value = 0;
			var dest = mFeedbackSystem.Controllers [PlayerIndex];

			float attackEnd = TimeToStart + AttackLength;
			float fadeStart = TimeToStart + Length - FadeLength;

			if (FeedbackType == FeedbackType.Square) 
			{
				float attackLvl = (AttackLevel > 0f) ? AttackLevel : MagnitudeLevel;
				float fadeLvl = (FadeLevel > 0f) ? FadeLevel : MagnitudeLevel;

				if (now <= attackEnd) {
					value = attackLvl;
				} else if (now <= fadeStart) {
					value = MagnitudeLevel;
				} else if (now <= TimeToEnd) {
					value = fadeLvl;
				} else {
					value = 0f;
				}
			} 
			else if (FeedbackType == FeedbackType.Linear) 
			{
				float y0 = 0f;
				float y1 = 0f;
				float x0 = 0f;
				float x1 = 0f;

				float attackLvl = (AttackLevel > 0f) ? AttackLevel : 0;
				float fadeLvl = (FadeLevel > 0f) ? FadeLevel : 0;			

				bool calculationRequired = true;

				if (now <= attackEnd) {
					x0 = TimeToStart;
					x1 = attackEnd;
					y0 = attackLvl;
					y1 = MagnitudeLevel;
				} else if (now <= fadeStart) {
					value = MagnitudeLevel;
					calculationRequired = false;
				} else if (now <= TimeToEnd) {
					x0 = fadeStart;
					x1 = TimeToEnd;
					y0 = MagnitudeLevel;
					y1 = fadeLvl;
				} else {
					value = 0f;
					calculationRequired = false;
				}

				if (calculationRequired)
				{
					float m = (y1 - y0) / (x1 - x0);
					float c = y0 - (x0 * m);

					value = (m * now) + c;
				}
			}

			if (MotorIndex == 0) 
			{
				dest.Left = value;
			}
			else if (MotorIndex == 1) 
			{
				dest.Right = value;
			}
		}

		public void Reset ()
		{
			var data = mFeedbackSystem.Controllers [PlayerIndex];
			if (MotorIndex == 0) {
				data.Left = 0f;
			} else if (MotorIndex == 1) {
				data.Right = 0f;
			}
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

		public float AttackLength {	get; set; }
		public float AttackLevel { get;	set; }
		public float MagnitudeLevel { get; set;	}
		public float FadeLength { get; set;	}
		public float FadeLevel { get; set; }

	}
}

