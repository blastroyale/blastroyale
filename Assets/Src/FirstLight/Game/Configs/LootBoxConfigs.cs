using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct LootBoxConfig
	{
		public int Id;
		public int Tier;
		public GameId LootBoxId;
		public List<EquipmentRarity> GuaranteeDrop;
		public List<Pair<uint, EquipmentRarity>> Rarities;
		public List<Pair<GameId, EquipmentRarity>> FixedItems;
		public uint ItemsAmount;
		public uint SecondsToOpen;

		/// <summary>
		/// Checks if this Loot box is a <see cref="GameIdGroup.Core"/> that automatically opens without needing to be queued up
		/// </summary>
		public bool IsAutoOpenCore => SecondsToOpen == 0;
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="LootBoxConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "LootBoxConfigs", menuName = "ScriptableObjects/Configs/LootBoxConfigs")]
	public class LootBoxConfigs : ScriptableObject, IConfigsContainer<LootBoxConfig>
	{
		[SerializeField] private List<LootBoxConfig> _configs = new();
		
		// ReSharper disable once ConvertToAutoProperty
		public List<LootBoxConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}