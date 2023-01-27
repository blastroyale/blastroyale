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
		public uint PlayerLevel;
		public uint PlayerTrophies;
		public FPVector2 NormalizedSpawnPosition;
		public int TeamId = -1;
		public Equipment Weapon;
		public Equipment[] Loadout;
		public EquipmentSimulationMetadata[] LoadoutMetadata;

		partial void SerializeUserData(BitStream stream)
		{
			var serializer = new FrameSerializer(DeterministicFrameSerializeMode.Serialize, null, stream);
			var skin = (int) Skin;
			var deathMarker = (int) DeathMarker;

			stream.Serialize(ref PlayerId);
			stream.Serialize(ref PlayerName);
			stream.Serialize(ref skin);
			stream.Serialize(ref deathMarker);
			stream.Serialize(ref PlayerLevel);
			stream.Serialize(ref PlayerTrophies);
			stream.Serialize(ref NormalizedSpawnPosition);
			stream.Serialize(ref TeamId);
			stream.SerializeArrayLength(ref Loadout);

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
				Equipment.Serialize(&metadata, serializer);
				LoadoutMetadata[i] = metadata;
			}

			Skin = (GameId) skin;
			DeathMarker = (GameId) deathMarker;
		}
	}
}
