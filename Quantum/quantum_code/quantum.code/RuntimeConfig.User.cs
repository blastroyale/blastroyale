using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum 
{
	partial class RuntimeConfig
	{
		// Non Serialized Map Data
		[NonSerialized] public int MapId;
		[NonSerialized] public string GameModeId;
		[NonSerialized] public string[] Mutators;
		[NonSerialized] public int BotOverwriteDifficulty;
		
		public AssetRefQuantumGameConfigs GameConfigs;
		public AssetRefQuantumMapConfigs MapConfigs;
		public AssetRefQuantumGameModeConfigs GameModeConfigs;
		public AssetRefQuantumBotConfigs BotConfigs;
		public AssetRefQuantumBotDifficultyConfigs BotDifficultyConfigs;
		public AssetRefQuantumWeaponConfigs WeaponConfigs;
		public AssetRefQuantumConsumableConfigs ConsumableConfigs;
		public AssetRefQuantumChestConfigs ChestConfigs;
		public AssetRefQuantumSpecialConfigs SpecialConfigs;
		public AssetRefQuantumAssetConfigs AssetConfigs;
		public AssetRefQuantumDestructibleConfigs DestructibleConfigs;
		public AssetRefQuantumShrinkingCircleConfigs ShrinkingCircleConfigs;
		public AssetRefQuantumEquipmentStatConfigs EquipmentStatConfigs;
		public AssetRefQuantumBaseEquipmentStatConfigs BaseEquipmentStatConfigs;
		public AssetRefQuantumStatConfigs StatConfigs;
		public AssetRefQuantumEquipmentMaterialStatConfigs EquipmentMaterialStatConfigs;
		public AssetRefQuantumMutatorConfigs MutatorConfigs;
		public AssetRefQuantumReviveConfigs ReviveConfigs;
		
		partial void SerializeUserData(BitStream stream)
		{
			stream.Serialize(ref MapId);
			stream.Serialize(ref GameModeId);
			stream.SerializeArrayLength(ref Mutators);
			for (var i = 0; i < Mutators.Length; i++)
			{
				stream.Serialize(ref Mutators[i]);
			}
			stream.Serialize(ref BotOverwriteDifficulty);
			stream.Serialize(ref GameConfigs);
			stream.Serialize(ref MapConfigs);
			stream.Serialize(ref GameModeConfigs);
			stream.Serialize(ref BotConfigs);
			stream.Serialize(ref BotDifficultyConfigs);
			stream.Serialize(ref WeaponConfigs);
			stream.Serialize(ref ConsumableConfigs);
			stream.Serialize(ref ChestConfigs);
			stream.Serialize(ref SpecialConfigs);
			stream.Serialize(ref AssetConfigs);
			stream.Serialize(ref DestructibleConfigs);
			stream.Serialize(ref ShrinkingCircleConfigs);
			stream.Serialize(ref EquipmentStatConfigs);
			stream.Serialize(ref BaseEquipmentStatConfigs);
			stream.Serialize(ref StatConfigs);
			stream.Serialize(ref EquipmentMaterialStatConfigs);
			stream.Serialize(ref MutatorConfigs);
			stream.Serialize(ref ReviveConfigs);
		}
	}
}