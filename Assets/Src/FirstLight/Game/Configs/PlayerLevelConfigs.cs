using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct PlayerLevelConfig
	{
		public uint Level;
		public uint LevelUpXP;
		public GameId RewardId;
		public int RewardValue;
		public List<UnlockSystem> Systems;
	}

	[Serializable]
	public enum UnlockSystem
	{
		Shop, 
		Enhancement,
		Fusion,
		Crates
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="PlayerLevelConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "PlayerLevelConfigs", menuName = "ScriptableObjects/Configs/PlayerLevelConfigs")]
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