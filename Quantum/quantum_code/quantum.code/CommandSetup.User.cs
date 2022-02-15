using System.Collections.Generic;
using Photon.Deterministic;
using Quantum.Commands;

namespace Quantum 
{
	public static partial class DeterministicCommandSetup 
	{
		static partial void AddCommandFactoriesUser(ICollection<IDeterministicCommandFactory> factories, RuntimeConfig gameConfig, SimulationConfig simulationConfig)
		{
			factories.Add(new CheatLocalPlayerKillCommand());
			factories.Add(new CheatCompleteKillCountCommand()); 
			factories.Add(new CheatMakeLocalPlayerSuperToughCommand());
			factories.Add(new CheatMakeLocalPlayerBigDamagerCommand());
			factories.Add(new PlayerQuitCommand());
			factories.Add(new SpecialUsedCommand());
			factories.Add(new PlayerEmojiCommand());
			factories.Add(new PlayerRespawnCommand());
			factories.Add(new WeaponSpawnCommand());
			factories.Add(new CollectablePlatformSpawnCommand());
			factories.Add(new DummySpawnCommand());
		}
	}
}
