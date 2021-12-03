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

		/// <summary>
		/// Collects this given <paramref name="entity"/> by the given <paramref name="player"/>
		/// </summary>
		internal void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef player)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var collectable = f.Get<Collectable>(entity);
			
			playerCharacter->SetWeapon(f, playerEntity, collectable.GameId, ItemRarity.Common, 1);
		}
	}
}