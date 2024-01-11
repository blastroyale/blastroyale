using System.Linq;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct EquipmentCollectable
	{
		/// <summary>
		/// Initializes this Weapon pick up with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation, FPVector3 originPos, ref Equipment equipment, EntityRef spawner,
		                   PlayerRef owner = new PlayerRef())
		{
			var collectable = new Collectable {GameId = equipment.GameId, PickupRadius = f.GameConfig.CollectableEquipmentPickupRadius, AllowedToPickupTime = f.Time + Constants.CONSUMABLE_POPOUT_DURATION};
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			transform->Position = position;
			transform->Rotation = rotation;

			collectable.Spawner = spawner;
			collectable.OriginPosition = originPos;

			Item = equipment;
			Owner = owner;

			f.Add(e, collectable);
			
			var collider = f.Unsafe.GetPointer<PhysicsCollider3D>(e);
			collider->Shape.Sphere.Radius = f.GameConfig.CollectableEquipmentPickupRadius;
		}

		/// <summary>
		/// Collects this given <paramref name="entity"/> by the given <paramref name="playerEntity"/>
		/// </summary>
		internal void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef playerRef)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var isBot = f.Has<BotCharacter>(playerEntity);

			var loadoutWeapon = playerCharacter->GetLoadoutWeapon(f);


			if (Item.IsWeapon())
			{
				var primaryWeapon = isBot || 
										Owner == playerRef ||
										// If you don't have a weapon in loadout and you don't already have a weapon in slot 1
										(!loadoutWeapon.IsValid() && !playerCharacter->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon.IsValid()) ||
										// If you got the same type of weapon you have in loadout
										Item.GameId == loadoutWeapon.GameId;
				
				// If a player has loadout weapon then we inherit rarity and faction from it
				if (loadoutWeapon.IsValid())
				{
					Item.Rarity = loadoutWeapon.Rarity;
					Item.Faction = loadoutWeapon.Faction;
				}
				
				playerCharacter->AddWeapon(f, playerEntity, ref Item, primaryWeapon);
			}

			f.Events.OnEquipmentCollected(entity, playerRef, playerEntity);
		}
	}
}
