using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumBotDifficultyConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BotDifficultyConfigs", menuName = "ScriptableObjects/Configs/BotDifficultyConfigs")]
	public class BotDifficultyConfigs : QuantumBotDifficultyConfigsAsset, IConfigsContainer<QuantumBotDifficultyConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumBotDifficultyConfig> Configs
		{
			get => Settings.BotDifficulties;
			set => Settings.BotDifficulties = value;
		}
	}
}