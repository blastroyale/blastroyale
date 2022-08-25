using System.Linq;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct EquipmentCollectable
	{
		/// <summary>
		/// Initializes this Weapon pick up with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, FPVector3 position, FPQuaternion rotation, Equipment equipment,
		                   PlayerRef owner = new PlayerRef())
		{
			var collectable = new Collectable {GameId = equipment.GameId};
			var transform = f.Unsafe.GetPointer<Transform3D>(e);

			transform->Position = position;
			transform->Rotation = rotation;

			Item = equipment;
			Owner = owner;

			f.Add(e, collectable);
		}

		/// <summary>
		/// Collects this given <paramref name="entity"/> by the given <paramref name="playerEntity"/>
		/// </summary>
		internal void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef playerRef)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			var isBot = f.Has<BotCharacter>(playerEntity);
			var playerData = f.GetPlayerData(playerRef);
			var loadoutWeapon = playerData.Loadout.FirstOrDefault(e => e.IsWeapon());

			if (Item.IsWeapon())
			{
				var primaryWeapon = isBot || 
										Owner == playerRef ||
										// If you don't have a weapon in loadout and you don't already have a weapon in slot 1
										(!loadoutWeapon.IsValid() && !playerCharacter->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon.IsValid()) ||
										// If you got the same type of weapon you have in loadout
										Item.GameId == loadoutWeapon.GameId;

				playerCharacter->AddWeapon(f, playerEntity, Item, primaryWeapon);
			}
			else
			{
				playerCharacter->EquipGear(f, playerEntity, Item);
			}

			f.Events.OnEquipmentCollected(entity, playerRef, playerEntity, Item);
		}
	}
}