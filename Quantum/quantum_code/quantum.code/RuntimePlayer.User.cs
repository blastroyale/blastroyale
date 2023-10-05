using System;
using Photon.Deterministic;

namespace Quantum 
{

	unsafe partial class RuntimePlayer
	{
		public string PlayerId;
		public string PlayerName;
		public GameId[] Cosmetics;
		public uint PlayerLevel;
		public uint PlayerTrophies;
		public uint LeaderboardRank;
		public FPVector2 NormalizedSpawnPosition;
		public string PartyId = string.Empty;
		public Equipment Weapon;
		public Equipment[] Loadout;
		public EquipmentSimulationMetadata[] LoadoutMetadata;
		public string AvatarUrl;
		public bool UseBotBehaviour;

		partial void SerializeUserData(BitStream stream)
		{
			var serializer = new FrameSerializer(DeterministicFrameSerializeMode.Serialize, null, stream);

			stream.Serialize(ref PlayerId);
			stream.Serialize(ref PlayerName);
			stream.Serialize(ref PlayerLevel);
			stream.Serialize(ref PlayerTrophies);
			stream.Serialize(ref NormalizedSpawnPosition);
			stream.Serialize(ref PartyId);
			stream.Serialize(ref AvatarUrl);
			stream.Serialize(ref UseBotBehaviour);
			stream.SerializeArrayLength(ref Loadout);
			stream.Serialize(ref LeaderboardRank);

			for (var i = 0; i < Loadout.Length; i++)
			{
				var localGear = Loadout[i];

				Equipment.Serialize(&localGear, serializer);

				if (localGear.IsWeapon())
				{
					Weapon = localGear;
				}

				Loadout[i] = localGear;
			}
			
			stream.SerializeArrayLength(ref LoadoutMetadata);
			for (var i = 0; i < LoadoutMetadata.Length; i++)
			{
				var metadata = LoadoutMetadata[i];
				EquipmentSimulationMetadata.Serialize(&metadata, serializer);
				LoadoutMetadata[i] = metadata;
			}

			
			stream.SerializeArrayLength(ref Cosmetics);
			for (var i = 0; i < Cosmetics.Length; i++)
			{
				var skinId = (int)Cosmetics[i];
				stream.Serialize(ref skinId);
				Cosmetics[i] = (GameId)skinId;
			}
		}
	}
}
