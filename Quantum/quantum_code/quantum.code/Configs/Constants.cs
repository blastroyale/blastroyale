using Photon.Deterministic;

namespace Quantum
{
	public static unsafe partial class Constants
	{
		public static readonly string STUNNED_EVENT = "OnStunned";
		public static readonly string STUN_CANCELLED_EVENT = "OnStunCancelled";
		public static readonly string STUN_DURATION_BB_KEY = "StunDuration";
		public static readonly string TARGET_BB_KEY = "Target";
		public static readonly string PROJECTILE_GAME_ID = "ProjectileGameId";
		public static readonly FP PROJECTILE_MAX_RANGE = FP._1000;
		public static readonly FP PROJECTILE_MAX_SPEED = FP._4 * FP._10;
		public static readonly FP FAKE_PROJECTILE_Y_OFFSET = FP._5;
		public static readonly FP STUN_GRENADE_TIME_TO_DAMAGE_MULTIPLIER = FP._5;
		public static readonly FP SHIELDED_CHARGE_POWER_TO_STUN_MULTIPLIER = FP._0;
		public static readonly FP SHIELDED_CHARGE_POWER_TO_AGGRO_MULTIPLIER = FP._0_10;
		public static readonly FP SHIELDED_CHARGE_SHIELD_DURATION_MULTIPLIER = FP._1;
		public static readonly FP OUT_OF_WORLD_Y_THRESHOLD = -FP._5;
		public static readonly FP CHARGE_VALIDITY_CHECK_DISTANCE_STEP = FP._0_25;
		public static readonly FP ACTOR_AS_TARGET_Y_OFFSET = FP._0_50;
		public static readonly FP SPAWNER_INACTIVE_TIME = FP._2;
		public static readonly FP MELEE_WEAPON_RANGE_THRESHOLD = FP._2;
		public static readonly FP DROP_OFFSET_RADIUS = FP._1_75;
		public static readonly GameId DEFAULT_WEAPON_GAME_ID = GameId.Hammer;

		public static readonly string AimDirectionKey = "AimDirection";
		public static readonly string IsShootingKey = "IsShooting";
		
		public static readonly GameIdGroup[] EquipmentSlots = new GameIdGroup[Constants.EQUIPMENT_SLOT_COUNT]
		{
			GameIdGroup.Amulet, GameIdGroup.Armor, GameIdGroup.Boots, GameIdGroup.Helmet, GameIdGroup.Shield,
			GameIdGroup.Weapon
		};
		public static readonly GameIdGroup[] GearSlots = new GameIdGroup[Constants.EQUIPMENT_SLOT_COUNT-1]
		{
			GameIdGroup.Amulet, GameIdGroup.Armor, GameIdGroup.Boots, GameIdGroup.Helmet, GameIdGroup.Shield
		};
	}
}