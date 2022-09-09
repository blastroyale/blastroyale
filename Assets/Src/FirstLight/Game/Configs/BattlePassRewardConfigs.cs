using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct BattlePassRewardConfig
	{
		public int Id;
		public Equipment Reward;

		public BattlePassRewardConfig(int id, Equipment reward)
		{
			Id = id;
			Reward = reward;
		}
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="BattlePassRewardConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BattlePassRewardConfigs",
	                 menuName = "ScriptableObjects/Configs/BattlePassRewardConfigs")]
	public class BattlePassRewardConfigs : ScriptableObject, IConfigsContainer<BattlePassRewardConfig>
	{
		[SerializeField] private List<BattlePassRewardConfig> _configs;

		public List<BattlePassRewardConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}