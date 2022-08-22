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
		public string Name;

		[PropertyRange(1, 30), ValidateInput("@MaxPlayers >= MinPlayers")]
		public uint MaxPlayers;

		[PropertyRange(1, 30), ValidateInput("@MaxPlayers >= MinPlayers")]
		public uint MinPlayers;

		[ToggleGroup("Teams")] public bool Teams;
		[ToggleGroup("Teams")] public uint MaxPlayersInTeam;
		[ToggleGroup("Teams")] public uint MinPlayersInTeam;

		public uint Lives;

		[ValueDropdown("GetOptionalSystems"), ListDrawerSettings(Expanded = true)]
		public List<Type> Systems;

#region Odin

		private IEnumerable<Type> GetOptionalSystems()
		{
			return from assembly in AppDomain.CurrentDomain.GetAssemblies()
			       from type in assembly.GetTypes()
			       where type.IsDefined(typeof(OptionalSystemAttribute))
			       select type;
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
					_dictionary.Add(QuantumConfigs[i].Name, QuantumConfigs[i]);
				}
			}

			return _dictionary[name];
		}
	}
}