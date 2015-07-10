using System;

namespace AudioEngine
{
	public interface IEffect
	{
		int Id { get; set; }
		float Delay { get; set; }
		float Length { get; set; }
		float TimeToStart { get; set; }
		float TimeToEnd { get; set; }
		void Apply(float now);
		void Reset();
	}
}

