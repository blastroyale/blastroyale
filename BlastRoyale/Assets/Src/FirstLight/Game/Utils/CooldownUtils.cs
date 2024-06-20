using System;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Little tool to control cooldowns in isolation
	/// </summary>
	public class Cooldown
	{
		private DateTime _trigger;
		
		public TimeSpan Delay;

		public Cooldown(TimeSpan delay)
		{
			Delay = delay;
		}

		public bool CheckTrigger()
		{
			if (IsCooldown()) return false;
			Trigger();
			return true;
		}

		public void Trigger() => _trigger = DateTime.UtcNow;

		public bool IsCooldown() => DateTime.UtcNow < _trigger + Delay;

		public void Reset() => _trigger = DateTime.MinValue;
	}
}