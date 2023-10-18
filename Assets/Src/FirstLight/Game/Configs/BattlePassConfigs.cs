using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct BattlePassConfig
	{
		[Tooltip("The price of the Pro BP in BlastBucks")]
		public uint Price;

		public uint CurrentSeason;
		public uint DefaultPointsPerLevel;
		public List<BattlePassLevel> Levels;

		[Serializable]
		public struct BattlePassLevel
		{
			public int RewardId;
			public uint PointsForNextLevel;
		}
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="BattlePassConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BattlePassConfigs", menuName = "ScriptableObjects/Configs/BattlePassConfigs")]
	public class BattlePassConfigs : ScriptableObject, ISingleConfigContainer<BattlePassConfig>
	{
		[SerializeField] private BattlePassConfig _config;

		public BattlePassConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}