using Photon.Deterministic;

namespace Quantum
{
	public static unsafe partial class Constants
	{
		public static readonly EquipmentRarity STANDARDISED_EQUIPMENT_RARITY = EquipmentRarity.Rare;

		public static readonly FP OUT_OF_WORLD_Y_THRESHOLD = -FP._5;
		public static readonly FP CHARGE_VALIDITY_CHECK_DISTANCE_STEP = FP._0_25;
		public static readonly FP ACTOR_AS_TARGET_Y_OFFSET = FP._0_50;
		public static readonly FP SPECIAL_CHARGE_Y_OFFSET = FP._0_10;
		public static readonly FP SPAWNER_INACTIVE_TIME = FP._1_50;
		public static readonly FP DROP_OFFSET_RADIUS = FP._1_25;
		public static readonly int OFFHAND_POOLSIZE = 20;
		public static readonly FP BOT_STUCK_DETECTION_SQR_DISTANCE = FP._2;
		public static readonly FP BOT_AMMO_REDUCE_THRESHOLD = FP._0_20;
		public static readonly int BURST_INTERVAL_DIVIDER = 3;
		public static readonly FP SELF_DAMAGE_MODIFIER= FP._0_75;
		public static readonly FP PICKUP_SPEED_MINIMUM = FP._0_10;
		public static readonly double[] APPRX_NORMAL_DISTRIBUTION = {1, 13, 35, 65, 87, 99, 100};
		public static readonly FP TAP_TO_USE_SPECIAL_AIMING_OFFSET = FP._0_75 + FP._0_10;
		public static readonly FP INITIAL_AMMO_FILLED = FP._0_10 + FP._0_05 + FP._0_03;
		public static readonly FP CONSUMABLE_POPOUT_DURATION = FP._0_50 + FP._0_10;
		public static readonly FP CHANCE_TO_DROP_WEAPON_ON_DEATH = FP._1 - FP._0_10;

		public static readonly int TEAM_ID_NEUTRAL = 0;
		public static readonly int TEAM_ID_START_PLAYERS = 100;
		public static readonly int TEAM_ID_START_PARTIES = 200;
		public static readonly int TEAM_ID_START_BOT_PARTIES = 300;

		public static readonly string DeadEvent = "OnDead";
		public static readonly string RespawnEvent = "OnRespawn";
		public static readonly string StunnedEvent = "OnStunned";
		public static readonly string KnockedOutEvent = "OnKnockedOut";
		public static readonly string StunCancelledEvent = "OnStunCancelled";
		public static readonly string ChangeWeaponEvent = "OnWeaponChanged";
		public static readonly string StunDurationKey = "StunDuration";
		public static readonly string AimDirectionKey = "AimDirection";
		public static readonly string MoveDirectionKey = "MoveDirection";
		public static readonly string TargetAim = "MoveDirection";
		public static readonly string MoveSpeedKey = "MoveSpeed";
		public static readonly string AccuracyLerp = "MoveSpeed";
		public static readonly string HasMeleeWeaponKey = "HasMeleeWeapon";
		public static readonly string BurstTimeDelay = "BurstTimeDelay";
		public static readonly string BurstShotCount = "BurstShotCount";
		public static readonly string NextTapTime = "NextTapTime";
		public static readonly string NextShotTime = "NextShotTime";
		public static readonly string LastShotAt = "LastShotAt";
		public static readonly string RampUpTimeStart = "RampUpTimeStart";
		public static readonly string IsAimPressedKey = "IsAimPressed";
		public static readonly string IsSkydiving = "IsSkydiving";
		public static readonly string IsShootingKey = "IsShooting";
		public static readonly string IsKnockedOut = "IsKnockedOut";
	}
}