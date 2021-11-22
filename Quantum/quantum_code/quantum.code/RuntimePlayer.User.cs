using Photon.Deterministic;

namespace Quantum 
{
	unsafe partial class RuntimePlayer
	{
		public string PlayerName;
		public GameId Skin;
		public uint PlayerLevel;
		public Equipment Weapon;
		public Equipment[] Gear;

		partial void SerializeUserData(BitStream stream)
		{
			var serializer = new FrameSerializer(DeterministicFrameSerializeMode.Serialize, null, stream);
			var skin = (int) Skin;
			var localWeapon = Weapon;
			
			stream.Serialize(ref PlayerName);
			stream.Serialize(ref skin);
			stream.Serialize(ref PlayerLevel);
			stream.SerializeArrayLength(ref Gear);

			for (var i = 0; i < Gear.Length; i++)
			{
				var localGear = Gear[i];
				
				Equipment.Serialize(&localGear, serializer);

				Gear[i] = localGear;
			}
			
			Equipment.Serialize(&localWeapon, serializer);

			Skin = (GameId) skin;
			Weapon = localWeapon;
		}
	}
}
