using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using UnityEngine;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct TrophyRewardConfig
	{
		public int[] reward;
		public int trophyRange;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="TrophyRewardConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "TrophyRewardConfigs", menuName = "ScriptableObjects/Configs/TrophyRewardConfigs")]
	public class TrophyRwardConfigs : ScriptableObject, IConfigsContainer<TrophyRewardConfig>
	{
		[SerializeField] private List<TrophyRewardConfig> _configs = new List<TrophyRewardConfig>();

		public List<TrophyRewardConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}

	}
}