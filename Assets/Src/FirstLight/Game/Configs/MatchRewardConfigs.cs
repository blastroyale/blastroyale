using System;
using System.Collections.Generic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct RewardPairSerialized
	{
		public GameId RewardKey;
		public uint RewardValue;

		public RewardPairSerialized(GameId key, uint value)
		{
			RewardKey = key;
			RewardValue = value;
		}
	}
	
	[Serializable]
	public struct MatchRewardConfig
	{
		public GameMode GameMode;
		public short Placement;
		public Dictionary<GameId, uint> RewardPairs;

		public List<RewardPairSerialized> RewardPairsSerialized;

		/// <summary>
		/// This populates a secondary KVP list so the rewards can be seen in the inspector.
		/// </summary>
		public void SerializeRewardPairs()
		{
			if (RewardPairsSerialized == null)
			{
				RewardPairsSerialized = new List<RewardPairSerialized>();
			}
			else
			{
				RewardPairsSerialized.Clear();
			}

			foreach (var pair in RewardPairs)
			{
				RewardPairsSerialized.Add(new RewardPairSerialized(pair.Key, pair.Value));
			}
		}
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="MatchRewardConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MatchRewardConfigs", menuName = "ScriptableObjects/Configs/MatchRewardConfigs")]
	public class MatchRewardConfigs : ScriptableObject, IConfigsContainer<MatchRewardConfig>
	{
		[SerializeField] private List<MatchRewardConfig> _configs = new List<MatchRewardConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<MatchRewardConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}

		/// <summary>
		/// Button in inspector - goes into all the reward configs, and serializes the reward pairs.
		/// The RewardPairs inside the configs is a dictionary, so it cant be visualised in inspector by default.
		/// This populates a secondary KVP lists in each config, so the rewards can be seen in the inspector.
		/// </summary>
		[Button]
		public void SerializeAllRewardPairs()
		{
			foreach (var config in Configs)
			{
				config.SerializeRewardPairs();
			}
		}
	}
}