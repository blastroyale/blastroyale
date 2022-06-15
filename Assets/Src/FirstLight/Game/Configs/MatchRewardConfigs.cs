using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct MatchRewardConfig
	{
		public short Placement;
		public GameIdUintDictionary Rewards;
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
	}

	[Serializable]
	public class GameIdUintDictionary : UnitySerializedDictionary<GameId, uint>
	{
	}
}