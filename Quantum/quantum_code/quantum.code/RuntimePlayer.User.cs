using Photon.Deterministic;

namespace Quantum 
{
	unsafe partial class RuntimePlayer
	{
		public string PlayerName;
		public GameId Skin;
		public uint PlayerLevel;
		public uint PlayerTrophies;
		public FPVector2 NormalizedSpawnPosition;
		public Equipment[] EquippedItems;

		partial void SerializeUserData(BitStream stream)
		{
			var serializer = new FrameSerializer(DeterministicFrameSerializeMode.Serialize, null, stream);
			var skin = (int) Skin;
			
			stream.Serialize(ref PlayerName);
			stream.Serialize(ref skin);
			stream.Serialize(ref PlayerLevel);
			stream.Serialize(ref PlayerTrophies);
			stream.Serialize(ref NormalizedSpawnPosition);
			stream.SerializeArrayLength(ref EquippedItems);

			for (var i = 0; i < EquippedItems.Length; i++)
			{
				var localGear = EquippedItems[i];
				
				Equipment.Serialize(&localGear, serializer);

				EquippedItems[i] = localGear;
			}

			Skin = (GameId) skin;
		}
	}
}
