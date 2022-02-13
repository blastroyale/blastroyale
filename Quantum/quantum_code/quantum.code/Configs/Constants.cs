using Photon.Deterministic;

namespace Quantum
{
	public static unsafe partial class Constants
	{
		public static readonly FP OUT_OF_WORLD_Y_THRESHOLD = -FP._5;
		public static readonly FP CHARGE_VALIDITY_CHECK_DISTANCE_STEP = FP._0_25;
		public static readonly FP ACTOR_AS_TARGET_Y_OFFSET = FP._0_50;
		public static readonly FP SPAWNER_INACTIVE_TIME = FP._2;
		public static readonly FP DROP_OFFSET_RADIUS = FP._1_75;

		public static readonly string StunnedEvent = "OnStunned";
		public static readonly string StunCancelledEvent = "OnStunCancelled";
		public static readonly string StunDurationKey = "StunDuration";
		public static readonly string AimDirectionKey = "AimDirection";
		public static readonly string AttackCooldownKey = "AttackCooldown";
		public static readonly string IsAimingKey = "IsAiming";
		public static readonly string AmmoFilledKey = "AmmoFilled";
		public static readonly FP RaycastAngleSplit = FP._1 * 10;
		
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