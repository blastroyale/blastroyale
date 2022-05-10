
using System;
using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command creates a dummy bot character in the game
	/// </summary>
	public unsafe class DummySpawnCommand : CommandBase
	{
		public FPVector3 Position;
		public FPQuaternion Rotation;
		public int Health;
		
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref Position);
			stream.Serialize(ref Rotation);
			stream.Serialize(ref Health);
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var e = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.DummyCharacterPrototype.Id));
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			transform->Position = Position;
			transform->Rotation = Rotation;

			f.Add(e, new DummyCharacter());
		}
	}
}