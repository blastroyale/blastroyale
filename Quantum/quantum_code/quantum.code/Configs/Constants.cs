using Photon.Deterministic;

namespace Quantum
{
	public static unsafe partial class Constants
	{
		public static readonly EquipmentRarity STANDARDISED_EQUIPMENT_RARITY = EquipmentRarity.Rare;

		public static readonly FP OUT_OF_WORLD_Y_THRESHOLD = -FP._5;
		public static readonly FP CHARGE_VALIDITY_CHECK_DISTANCE_STEP = FP._0_25;
		
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
		public static readonly FP INITIAL_AMMO_FILLED = FP._0_10 + FP._0_05 + FP._0_01;
		public static readonly FP CONSUMABLE_POPOUT_DURATION = FP._0_50 + FP._0_10;
		public static readonly FP CHANCE_TO_DROP_WEAPON_ON_DEATH = FP._1 - FP._0_10;
		public static readonly FP SHRINKINGCIRCLE_NAVMESH_CORRECTION_RADIUS = FP._10;

		public const int TEAM_ID_NEUTRAL = 0;
		public const int TEAM_ID_START_PLAYERS = 100;
		public const int TEAM_ID_START_PARTIES = 200;
		public const int TEAM_ID_START_BOT_PARTIES = 300;

		public const string DEAD_EVENT = "OnDead";
		public const string RESPAWN_EVENT = "OnRespawn";
		public const string STUNNED_EVENT = "OnStunned";
		public const string KNOCKED_OUT_EVENT = "OnKnockedOut";
		public const string STUN_CANCELLED_EVENT = "OnStunCancelled";
		public const string CHANGE_WEAPON_EVENT = "OnWeaponChanged";
		public const string STUN_DURATION_KEY = "StunDuration";
		public const string AIM_DIRECTION_KEY = "AimDirection";
		public const string MOVE_DIRECTION_KEY = "MoveDirection";
		public const string TARGET_AIM = "MoveDirection";
		public const string MOVE_SPEED_KEY = "MoveSpeed";
		public const string ACCURACY_LERP = "MoveSpeed";
		public const string HAS_MELEE_WEAPON_KEY = "HasMeleeWeapon";
		public const string BURST_TIME_DELAY = "BurstTimeDelay";
		public const string BURST_SHOT_COUNT = "BurstShotCount";
		public const string NEXT_TAP_TIME = "NextTapTime";
		public const string NEXT_SHOT_TIME = "NextShotTime";
		public const string LAST_SHOT_AT = "LastShotAt";
		public const string RAMP_UP_TIME_START = "RampUpTimeStart";
		public const string IS_AIM_PRESSED_KEY = "IsAimPressed";
		public const string IS_SKYDIVING = "IsSkydiving";
		public const string IS_SHOOTING_KEY = "IsShooting";
		public const string IS_KNOCKED_OUT = "IsKnockedOut";
		
		public static readonly FP MUTATOR_HEALTHPERSECONDS_AMOUNT = FP._10 + FP._5;
		public static readonly FP MUTATOR_HEALTHPERSECONDS_DURATION = FP._3;
		public static readonly FP MUTATOR_SPEEDUP_AMOUNT = FP._1 + FP._0_33;
	}
}