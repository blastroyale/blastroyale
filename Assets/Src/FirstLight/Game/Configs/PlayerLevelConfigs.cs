using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct PlayerLevelConfig
	{
		public uint Level;
		public uint LevelUpXP;
		public SerializedDictionary<GameId,int> Rewards;
		public List<UnlockSystem> Systems;
	}

	[Serializable]
	public enum UnlockSystem
	{
		ShopScreen,
		CollectionsScreen
	}

	public class PlayerLevelConfigs : ScriptableObject, IConfigsContainer<PlayerLevelConfig>
	{
		[SerializeField] private List<PlayerLevelConfig> _configs = new List<PlayerLevelConfig>();

		// ReSharper disable once ConvertToAutoProperty
		public List<PlayerLevelConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}