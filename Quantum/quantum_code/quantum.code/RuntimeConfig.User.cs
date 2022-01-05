using Photon.Deterministic;
using System;

namespace Quantum 
{
	partial class RuntimeConfig
	{
		// Non Serialized Map Data
		[NonSerialized] public GameId MapId;
		[NonSerialized] public int BotDifficultyLevel;
		[NonSerialized] public int PlayersLimit;
		[NonSerialized] public int GameEndTarget;
		[NonSerialized] public GameMode GameMode;
		
		public AssetRefQuantumGameConfigs GameConfigs;
		public AssetRefQuantumBotConfigs BotConfigs;
		public AssetRefQuantumWeaponConfigs WeaponConfigs;
		public AssetRefQuantumGearConfigs GearConfigs;
		public AssetRefQuantumConsumableConfigs ConsumableConfigs;
		public AssetRefQuantumSpecialConfigs SpecialConfigs;
		public AssetRefQuantumHazardConfigs HazardConfigs;
		public AssetRefQuantumAssetConfigs AssetConfigs;
		public AssetRefQuantumDestructibleConfigs DestructibleConfigs;
		public AssetRefQuantumShrinkingCircleConfigs ShrinkingCircleConfigs;
		
		partial void SerializeUserData(BitStream stream)
		{
			var mapId = (int) MapId;
			var gameMode = (int) GameMode;
			
			stream.Serialize(ref mapId);
			stream.Serialize(ref gameMode);
			stream.Serialize(ref BotDifficultyLevel);
			stream.Serialize(ref PlayersLimit);
			stream.Serialize(ref GameEndTarget);
			stream.Serialize(ref GameConfigs);
			stream.Serialize(ref BotConfigs);
			stream.Serialize(ref WeaponConfigs);
			stream.Serialize(ref GearConfigs);
			stream.Serialize(ref ConsumableConfigs);
			stream.Serialize(ref SpecialConfigs);
			stream.Serialize(ref HazardConfigs);
			stream.Serialize(ref AssetConfigs);
			stream.Serialize(ref DestructibleConfigs);
			stream.Serialize(ref ShrinkingCircleConfigs);
			
			MapId = (GameId) mapId;
			GameMode = (GameMode) gameMode;
		}
	}
}