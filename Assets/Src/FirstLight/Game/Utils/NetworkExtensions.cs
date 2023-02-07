using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;

namespace FirstLight.Game.Utils
{
	public static class NetworkExtensions
	{
		public static QuantumMapConfig Map(this MatchRoomSetup setup)
		{
			var cfgProvider = MainInstaller.Resolve<IGameServices>().ConfigsProvider;
			return cfgProvider.GetConfig<QuantumMapConfig>(setup.MapId);
		}

		public static QuantumGameModeConfig GameMode(this MatchRoomSetup setup)
		{
			var cfgProvider = MainInstaller.Resolve<IGameServices>().ConfigsProvider;
			return cfgProvider.GetConfig<QuantumGameModeConfig>(setup.GameModeHash);
		}
	}
}