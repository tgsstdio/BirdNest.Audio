using System;

namespace AudioEngine
{
	public interface IForceFeedbackEffect
	{
		FeedbackType FeedbackType { get; set;}
		float AttackLength {get;set;}
		float AttackLevel {get;set;}
		float MagnitudeLevel { get; set;}
		float FadeLength {get;set;}
		float FadeLevel {get;set;}		
	}
}

