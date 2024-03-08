using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public class ReviveEntry
	{
		public FP DamageTickInterval;
		public FP DamagePerTick;
		public FP DamagePerShot;
		public FP TimeToRevive;
		public FP LifePercentageOnRevived;
		public FP LifePercentageOnKnockedOut;
		public FP MoveSpeedMultiplier;
		public FP ReviveColliderRange;
		public FP ProgressDownSpeedMultiplier;
	}

	[Serializable]
	public class GameModeReviveConfig
	{
		public List<ReviveEntry> AllowedRevives = new List<ReviveEntry>();
	}


	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumMapConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumReviveConfigs
	{
		public bool FullyDisable;
		public FP ReviveAnimationDuration;
		public QuantumPerGameModeConfig<GameModeReviveConfig> PerGameMode = new QuantumPerGameModeConfig<GameModeReviveConfig>();
	}
}