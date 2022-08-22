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
		/// Collects this given <paramref name="entity"/> by the given <paramref name="player"/>
		/// </summary>
		internal void Collect(Frame f, EntityRef entity, EntityRef player, PlayerRef playerRef)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(player);
			var isBot = f.Has<BotCharacter>(player);
			var playerData = f.GetPlayerData(playerRef);

			if (Item.IsWeapon())
			{
				var primaryWeapon = isBot || Owner == playerRef ||
				                    (!playerData.Loadout.FirstOrDefault(e => e.IsWeapon()).IsValid() &&
				                     !playerCharacter->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon
					                     .IsValid());

				playerCharacter->AddWeapon(f, player, Item, primaryWeapon);
			}
			else
			{
				playerCharacter->EquipGear(f, player, Item);
			}
		}
	}
}