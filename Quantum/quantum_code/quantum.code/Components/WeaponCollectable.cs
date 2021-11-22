using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct WeaponCollectable
	{
		/// <summary>
		/// Initializes this Weapon pick up with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation, QuantumWeaponConfig config)
		{
			var collectable = new Collectable {GameId = config.Id };
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			
			transform->Position = position;
			transform->Rotation = rotation;
			
			f.Add(e, collectable);
		}
	}
}