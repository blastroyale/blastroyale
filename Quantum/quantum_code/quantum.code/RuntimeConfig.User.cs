using Photon.Deterministic;
using System;

namespace Quantum 
{
	partial class RuntimeConfig
	{
		// Non Serialized Map Data
		[NonSerialized] public int MapId;
		
		public AssetRefQuantumGameConfigs GameConfigs;
		public AssetRefQuantumMapConfigs MapConfigs;
		public AssetRefQuantumBotConfigs BotConfigs;
		public AssetRefQuantumWeaponConfigs WeaponConfigs;
		public AssetRefQuantumConsumableConfigs ConsumableConfigs;
		public AssetRefQuantumChestConfigs ChestConfigs;
		public AssetRefQuantumSpecialConfigs SpecialConfigs;
		public AssetRefQuantumAssetConfigs AssetConfigs;
		public AssetRefQuantumDestructibleConfigs DestructibleConfigs;
		public AssetRefQuantumShrinkingCircleConfigs ShrinkingCircleConfigs;
		public AssetRefQuantumEquipmentStatsConfigs EquipmentStatsConfigs;
		public AssetRefQuantumBaseEquipmentStatsConfigs BaseEquipmentStatsConfigs;
		public AssetRefQuantumEquipmentMaterialStatsConfigs EquipmentMaterialStatsConfigs;
		
		partial void SerializeUserData(BitStream stream)
		{
			stream.Serialize(ref MapId);
			stream.Serialize(ref GameConfigs);
			stream.Serialize(ref MapConfigs);
			stream.Serialize(ref BotConfigs);
			stream.Serialize(ref WeaponConfigs);
			stream.Serialize(ref ConsumableConfigs);
			stream.Serialize(ref ChestConfigs);
			stream.Serialize(ref SpecialConfigs);
			stream.Serialize(ref AssetConfigs);
			stream.Serialize(ref DestructibleConfigs);
			stream.Serialize(ref ShrinkingCircleConfigs);
			stream.Serialize(ref EquipmentStatsConfigs);
			stream.Serialize(ref BaseEquipmentStatsConfigs);
			stream.Serialize(ref EquipmentMaterialStatsConfigs);
		}
	}
}