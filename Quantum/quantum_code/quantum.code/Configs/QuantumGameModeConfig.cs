using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;

namespace Quantum
{
	[Serializable]
	public struct QuantumGameModeConfig
	{
		public string Id;

		[PropertyRange(1, 30), ValidateInput("@MaxPlayers >= MinPlayers")]
		public uint MaxPlayers;

		[PropertyRange(1, 30), ValidateInput("@MaxPlayers >= MinPlayers")]
		public uint MinPlayers;

		[ToggleGroup("Teams")] public bool Teams;
		[ToggleGroup("Teams")] public uint MaxPlayersInTeam;
		[ToggleGroup("Teams")] public uint MinPlayersInTeam;

		public uint Lives; // Note, currently this only works for 1, every other value means "infinite lives"

		
		[FoldoutGroup("Spawning")] public bool SpawnWithLoadout;
		[FoldoutGroup("Spawning")] public bool SkydiveSpawn;
		[FoldoutGroup("Spawning")] public bool SpawnSelection;
		[FoldoutGroup("Spawning")] public bool SpawnPattern;

		public bool DropWeaponOnPickup;

		// UI / Visual
		public bool ShowStatChanges;
		public bool ShowUIMinimap;
		public bool ShowUITimer;
		public bool ShowUIStandingsExtraInfo;
		public bool ShowWeaponSlots;

		public GameCompletionStrategy CompletionStrategy;

		[ShowIf("@CompletionStrategy == GameCompletionStrategy.KillCount")]
		public uint CompletionKillCount;

		[FoldoutGroup("Death drops")] public DeathDropsStrategy WeaponDeathDropStrategy;
		[FoldoutGroup("Death drops")] public DeathDropsStrategy HealthDeathDropStrategy;
		[FoldoutGroup("Death drops")] public DeathDropsStrategy ShieldDeathDropStrategy;
		[FoldoutGroup("Death drops")] public bool DeathMarker;

		[FoldoutGroup("Bots")] public bool BotSearchForCrates;
		[FoldoutGroup("Bots")] public bool BotRespawn;
		[FoldoutGroup("Bots")] public BotWeaponSearchStrategy BotWeaponSearchStrategy;

		public bool GiveRewards;

		public RankSorter RankSorter;
		public RankProcessor RankProcessor;

		[ValueDropdown("GetOptionalSystems"), ListDrawerSettings(Expanded = true)]
		public List<string> Systems;

#region Odin

		private IEnumerable<string> GetOptionalSystems()
		{
			return from assembly in AppDomain.CurrentDomain.GetAssemblies()
			       from type in assembly.GetTypes()
			       where type.IsDefined(typeof(OptionalSystemAttribute))
			       select type.AssemblyQualifiedName;
		}

#endregion
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumGameModeConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumGameModeConfigs
	{
		public List<QuantumGameModeConfig> QuantumConfigs = new List<QuantumGameModeConfig>();

		private IDictionary<string, QuantumGameModeConfig> _dictionary;

		/// <summary>
		/// Requests the <see cref="QuantumGameModeConfig"/> defined by the given <paramref name="name"/>
		/// </summary>
		public QuantumGameModeConfig GetConfig(string name)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<string, QuantumGameModeConfig>();

				for (var i = 0; i < QuantumConfigs.Count; i++)
				{
					_dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
				}
			}

			return _dictionary[name];
		}
	}
}