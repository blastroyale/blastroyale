using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	public class UnlockReward : IReward
	{
		public UnlockSystem UnlockSystem { get; }
		public GameId GameId { get; }
		public uint Amount { get; }
		public string DisplayName => UnlockSystem.ToString().ToUpper(); // TODO: Move to localizations

		public UnlockReward(UnlockSystem unlockSystem)
		{
			UnlockSystem = unlockSystem;
		}
	}
}