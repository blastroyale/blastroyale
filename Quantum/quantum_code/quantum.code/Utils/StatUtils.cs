using Photon.Deterministic;

namespace Quantum
{
	public class StatUtils
	{
		public static FP GetHealthPercentage(in Stats stats)
		{
			var maxHealth = FPMath.RoundToInt(stats.GetStatData(StatType.Health).StatValue);
			return (FP)stats.CurrentHealth / (FP)maxHealth;
		}

		public static FP GetShieldPercentage(in Stats stats)
		{
			var shieldCapacity = FPMath.RoundToInt(stats.GetStatData(StatType.Shield).StatValue);
			return (FP)stats.CurrentShield / (FP)shieldCapacity;
		}
	}
}