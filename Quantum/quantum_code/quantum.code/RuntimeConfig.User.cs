using Photon.Deterministic;
using System;

namespace Quantum 
{
	partial class RuntimeConfig
	{
		// Non Serialized Map Data
		[NonSerialized] public GameId MapId;
		[NonSerialized] public int BotDifficultyLevel;
		[NonSerialized] public int TotalFightersLimit;
		[NonSerialized] public uint DeathmatchKillCount;
		
		public AssetRefQuantumGameConfigs GameConfigs;
		public AssetRefQuantumBotConfigs BotConfigs;
		public AssetRefQuantumMultishotConfigs MultishotConfigs;
		public AssetRefQuantumFrontshotConfigs FrontshotConfigs;
		public AssetRefQuantumDiagonalshotConfigs DiagonalshotConfigs;
		public AssetRefQuantumWeaponConfigs WeaponConfigs;
		public AssetRefQuantumGearConfigs GearConfigs;
		public AssetRefQuantumConsumableConfigs ConsumableConfigs;
		public AssetRefQuantumSpecialConfigs SpecialConfigs;
		public AssetRefQuantumHazardConfigs HazardConfigs;
		public AssetRefQuantumAssetConfigs AssetConfigs;
		public AssetRefQuantumDestructibleConfigs DestructibleConfigs;
		
		partial void SerializeUserData(BitStream stream)
		{
			var mapId = (int) MapId;
			
			stream.Serialize(ref mapId);
			stream.Serialize(ref BotDifficultyLevel);
			stream.Serialize(ref TotalFightersLimit);
			stream.Serialize(ref DeathmatchKillCount);
			stream.Serialize(ref GameConfigs);
			stream.Serialize(ref MultishotConfigs);
			stream.Serialize(ref FrontshotConfigs);
			stream.Serialize(ref DiagonalshotConfigs);
			stream.Serialize(ref WeaponConfigs);
			stream.Serialize(ref GearConfigs);
			stream.Serialize(ref ConsumableConfigs);
			stream.Serialize(ref SpecialConfigs);
			stream.Serialize(ref HazardConfigs);
			stream.Serialize(ref AssetConfigs);
			stream.Serialize(ref DestructibleConfigs);
			stream.Serialize(ref BotConfigs);
			
			MapId = (GameId) mapId;
		}
	}
}