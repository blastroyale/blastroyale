using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumGameModeConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GameModeConfigs", menuName = "ScriptableObjects/Configs/GameModeConfigs")]
	public class GameModeConfigs : QuantumGameModeConfigsAsset, IConfigsContainer<QuantumGameModeConfig>
	{
		public List<QuantumGameModeConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}