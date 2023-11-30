using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public class QuantumBotDifficultyConfig
	{
		public uint MinTrophies;
		public uint MaxTrophies;
		public uint BotDifficulty;
	}
	
	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumBotDifficultyConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumBotDifficultyConfigs
	{
		public List<QuantumBotDifficultyConfig> BotDifficulties = new List<QuantumBotDifficultyConfig>();
	}
}