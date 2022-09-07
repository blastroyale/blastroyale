using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct BattlePassConfig
	{
		public uint PointsPerLevel;

		public List<BattlePassLevel> Levels;

		[Serializable]
		public struct BattlePassLevel
		{
			public int RewardId;
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