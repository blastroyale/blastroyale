using Photon.Deterministic;
using System;

namespace Quantum 
{
	partial class RuntimeConfig
	{
		// Non Serialized Map Data
		[NonSerialized] public int MapId;
		[NonSerialized] public int BotDifficultyLevel;
		[NonSerialized] public int PlayersLimit;
		[NonSerialized] public int GameEndTarget;
		[NonSerialized] public GameMode GameMode;
		[NonSerialized] public bool IsTestMap;
		
		public AssetRefQuantumGameConfigs GameConfigs;
		public AssetRefQuantumBotConfigs BotConfigs;
		public AssetRefQuantumWeaponConfigs WeaponConfigs;
		public AssetRefQuantumGearConfigs GearConfigs;
		public AssetRefQuantumConsumableConfigs ConsumableConfigs;
		public AssetRefQuantumSpecialConfigs SpecialConfigs;
		public AssetRefQuantumAssetConfigs AssetConfigs;
		public AssetRefQuantumDestructibleConfigs DestructibleConfigs;
		public AssetRefQuantumShrinkingCircleConfigs ShrinkingCircleConfigs;
		
		partial void SerializeUserData(BitStream stream)
		{
			var gameMode = (int) GameMode;
			
			stream.Serialize(ref MapId);
			stream.Serialize(ref gameMode);
			stream.Serialize(ref BotDifficultyLevel);
			stream.Serialize(ref PlayersLimit);
			stream.Serialize(ref GameEndTarget);
			stream.Serialize(ref IsTestMap);
			stream.Serialize(ref GameConfigs);
			stream.Serialize(ref BotConfigs);
			stream.Serialize(ref WeaponConfigs);
			stream.Serialize(ref GearConfigs);
			stream.Serialize(ref ConsumableConfigs);
			stream.Serialize(ref SpecialConfigs);
			stream.Serialize(ref AssetConfigs);
			stream.Serialize(ref DestructibleConfigs);
			stream.Serialize(ref ShrinkingCircleConfigs);
			
			GameMode = (GameMode) gameMode;
		}
	}
}