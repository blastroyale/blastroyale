using Photon.Deterministic;

namespace Quantum
{
	public static unsafe partial class Constants
	{
		public static readonly FP OUT_OF_WORLD_Y_THRESHOLD = -FP._5;
		public static readonly FP CHARGE_VALIDITY_CHECK_DISTANCE_STEP = FP._0_25;
		public static readonly FP ACTOR_AS_TARGET_Y_OFFSET = FP._0_50;
		public static readonly FP SPAWNER_INACTIVE_TIME = FP._1_50;
		public static readonly FP DROP_OFFSET_RADIUS = FP._1_75;
		public static readonly int BOT_DIFFICULTY_LEVEL = 1;

		public static readonly string DeadEvent = "OnDead";
		public static readonly string RespawnEvent = "OnRespawn";
		public static readonly string StunnedEvent = "OnStunned";
		public static readonly string StunCancelledEvent = "OnStunCancelled";
		public static readonly string StunDurationKey = "StunDuration";
		public static readonly string AimDirectionKey = "AimDirection";
		public static readonly string MoveDirectionKey = "MoveDirection";
		public static readonly string HasMeleeWeaponKey = "HasMeleeWeapon";
		public static readonly string BurstTimeDelay = "BurstTimeDelay";
		public static readonly string BurstShotCount = "BurstShotCount";
		public static readonly string IsAimingKey = "IsAiming";
		public static readonly string AmmoFilledKey = "AmmoFilled";
	}
}