using System;
using Photon.Deterministic;

namespace Quantum 
{

	unsafe partial class RuntimePlayer
	{
		public string PlayerId;
		public string PlayerName;
		public GameId Skin;
		public GameId DeathMarker;
		public GameId Glider;
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
			var skin = (int) Skin;
			var deathMarker = (int) DeathMarker;
			var glider = (int) Glider;

			stream.Serialize(ref PlayerId);
			stream.Serialize(ref PlayerName);
			stream.Serialize(ref skin);
			stream.Serialize(ref deathMarker);
			stream.Serialize(ref glider);
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

			Skin = (GameId) skin;
			DeathMarker = (GameId) deathMarker;
			Glider = (GameId) glider;
		}
	}
}
