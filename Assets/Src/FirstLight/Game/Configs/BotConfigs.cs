using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumBotConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BotConfigs", menuName = "ScriptableObjects/Configs/BotConfig")]
	public class BotConfigs : QuantumBotConfigsAsset, IConfigsContainer<QuantumBotConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumBotConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}