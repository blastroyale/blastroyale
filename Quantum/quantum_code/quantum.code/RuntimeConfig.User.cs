using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
	partial class RuntimeConfig
	{ 
		public SimulationMatchConfig MatchConfigs;
		
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
			MatchConfigs ??= new SimulationMatchConfig();
			MatchConfigs.Serialize(stream);
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