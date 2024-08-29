using System.Linq;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct EquipmentCollectable
	{

		public FP PickupRadius(Frame f)
		{
			return f.GameConfig.CollectableEquipmentPickupRadius;
		}
		
		public FP AllowedPickupTime(Frame f)
		{
			return f.Time + Constants.CONSUMABLE_POPOUT_DURATION;
		}
		
		/// <summary>
		/// Initializes this Weapon pick up with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, FPVector2 position, FP rotation, FPVector2 originPos, in Equipment equipment, EntityRef spawner,
		                   PlayerRef owner = new PlayerRef())
		{
			var collectable = new Collectable {GameId = equipment.GameId };
			var transform = f.Unsafe.GetPointer<Transform2D>(e);

			transform->Position = position;
			transform->Rotation = rotation;

			collectable.Spawner = spawner;
			collectable.OriginPosition = originPos;

			Item = equipment;
			Owner = owner;

			f.Add(e, collectable);
			
			var collider = f.Unsafe.GetPointer<PhysicsCollider2D>(e);
			collider->Shape.Circle.Radius = f.GameConfig.CollectableEquipmentPickupRadius;
		}

		/// <summary>
		/// Collects this given <paramref name="entity"/> by the given <paramref name="playerEntity"/>
		/// </summary>
		internal void Collect(Frame f, EntityRef entity, EntityRef playerEntity, PlayerRef playerRef)
		{
			var isBot = f.Has<BotCharacter>(playerEntity);
			if (Item.IsWeapon())
			{
				var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
				
				var primaryWeapon = isBot || 
										Owner == playerRef ||
										// If you don't have a weapon in loadout and you don't already have a weapon in slot 1
										(!playerCharacter->WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon.IsValid());
				
				playerCharacter->AddWeapon(f, playerEntity, Item, primaryWeapon);
				
				var gameContainer = f.Unsafe.GetPointerSingleton<GameContainer>();
				var playerDataPointer = gameContainer->PlayersData.GetPointer(playerRef);
				playerDataPointer->GunsCollectedCount++;
			}

			f.Events.OnEquipmentCollected(entity, playerRef, playerEntity);
		}
	}
}
