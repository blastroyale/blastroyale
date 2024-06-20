using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct TrophyRewardConfig
	{
		public int MatchRewardId;
		public int Placement;
		public int TeamSize;
		public SerializedDictionary<int, int> BracketReward;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="TrophyRewardConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "TrophyRewardConfigs", menuName = "ScriptableObjects/Configs/TrophyRewardConfigs")]
	public class TrophyRewardConfigs : ScriptableObject, IConfigsContainer<TrophyRewardConfig>
	{
		[SerializeField] private List<TrophyRewardConfig> _configs = new List<TrophyRewardConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<TrophyRewardConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}