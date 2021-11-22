using Photon.Deterministic;
using Quantum.Commands;

namespace Quantum 
{
	public static class CommandSetup 
	{
		public static DeterministicCommand[] CreateCommands(RuntimeConfig gameConfig, SimulationConfig simulationConfig) 
		{
			return new DeterministicCommand[] {

				// user commands go here
				new CheatLocalPlayerKillCommand(),
				new CheatCompleteKillCountCommand(),
				new CheatMakeLocalPlayerSuperToughCommand(),
				new CheatMakeLocalPlayerBigDamagerCommand(),
				new PlayerQuitCommand(),
				new SpecialUsedCommand(),
				new PlayerEmojiCommand(),
				new PlayerRespawnCommand(),
				new WeaponSpawnCommand(),
				new CollectablePlatformSpawnCommand(),
				new DummySpawnCommand(),
			};
		}
	}
}
