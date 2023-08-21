using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	public class UnlockReward : IReward
	{
		public UnlockSystem UnlockSystem { get; }
		public GameId GameId { get; }
		public uint Amount { get; }
		public string DisplayName { get; } = "SHOP";

		public UnlockReward(UnlockSystem unlockSystem)
		{
			UnlockSystem = unlockSystem;
		}
	}
}