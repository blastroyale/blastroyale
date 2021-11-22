
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
			var targetable = new Targetable();
			var e = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.DummyCharacterPrototype.Id));
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var dummyHealth = Health;
			
			transform->Position = Position;
			transform->Rotation = Rotation;
			targetable.Team = (int) TeamType.Neutral;
			targetable.IsUntargetable = false;
			
			f.Add(e, targetable);
			f.Add(e, new Stats(dummyHealth, 0, 0, 0, 0));
			f.Add(e, new DummyCharacter());
		}
	}
}