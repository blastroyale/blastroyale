using Photon.Deterministic;
using Quantum.Systems;
using Quantum.Systems.Bots;

namespace Quantum
{
	/// <summary>
	/// Centralized place for bot state changing functions
	/// All functions in this file should be GETTERS or SETTERS
	/// </summary>
	public unsafe static class BotState
	{
		/// <summary>
		/// Checks if a given bot is ready to take a decision
		/// </summary>
		public static bool GetCanTakeDecision(this ref BotCharacter bot, Frame f)
		{
			return bot.MoveTarget == EntityRef.None || f.Time > bot.NextDecisionTime;
		}

		public static void SetNextDecisionDelay(this ref BotCharacter bot, Frame f, in FP seconds)
		{
			bot.NextDecisionTime = f.Time + seconds;
		}

		public static bool IsLowLife(this ref BotCharacter bot, in EntityRef entity, Frame f)
		{
			return Stats.HealthRatio(entity, f) < FP._0_20;
		}

		/// <summary>
		/// Sets on how many seconds the bot should attempt to take a new decision
		/// </summary>
		public static void SetSearchForEnemyDelay(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f)
		{
			(*botFilter.BotCharacter).SetSearchForEnemyDelay(f);
		}

		public static void SetSearchForEnemyDelay(this ref BotCharacter bot, Frame f)
		{
			bot.NextLookForTargetsToShootAtTime = f.Time + bot.LookForTargetsToShootAtInterval;
		}

		/// <summary>
		/// Flags the bot as having a waypoint to go towards
		/// Will add decision delay to the bot
		/// </summary>
		public static void SetHasWaypoint(this ref BotCharacter bot, in EntityRef entity, Frame f)
		{
			BotLogger.LogAction(bot, "Set Waypoint");
			bot.MoveTarget = entity;
			bot.NextDecisionTime = f.Time + bot.DecisionInterval;
			bot.StuckDetectionPosition = f.Get<Transform3D>(entity).Position.XZ;
		}

		/// <summary>
		/// Resets the bot target waypoint. This is done by setting no MoveTarget and lowering next decision time so next
		/// decision is done shortly
		/// </summary>
		public static void ResetTargetWaypoint(this BotCharacter bot, Frame f)
		{
			BotLogger.LogAction(bot, "Clear waypoint");
			bot.MoveTarget = EntityRef.None;
			bot.NextDecisionTime = f.Time;
			bot.StuckDetectionPosition = FPVector2.Zero;
		}

		/// <summary>
		/// Flags the bot as having a waypoint to go towards
		/// Will add decision delay to the bot
		/// </summary>
		public static void SetHasWaypoint(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f)
		{
			botFilter.BotCharacter->SetHasWaypoint(botFilter.Entity, f);
		}

		/// <summary>
		/// Checks if the given bot has or not a waypoint (moving)
		/// </summary>
		public static bool HasWaypoint(this ref BotCharacter bot, EntityRef entity, Frame f)
		{
			//////////////////////////////////////////////////////////////////////////////////////////////////////////
			// This STUCK DETECTION code is HIDING the issue about bots being stuck, it's a WORKAROUND
			// IF YOU EVER want to remove this code be sure to FIND OUT WHY bots stuck going into walls when they
			// are trying to Wander or GoToSafeArea; it's not happening really when bots go for Consumables, so potentially
			// the issue is somewhere where we convert randomly chosen position into navmesh position
			if (bot.StuckDetectionPosition != FPVector2.Zero
				&& FPVector2.DistanceSquared(bot.StuckDetectionPosition, f.Get<Transform3D>(entity).Position.XZ)
				< Constants.BOT_STUCK_DETECTION_SQR_DISTANCE)
			{
				bot.ResetTargetWaypoint(f);
				return false;
			}

			bot.StuckDetectionPosition = f.Get<Transform3D>(entity).Position.XZ;
			//////////////////////////////////////////////////////////////////////////////////////////////////////////

			return bot.MoveTarget.IsValid;
		}

		/// <summary>
		/// Checks if a given bot is not doing shit
		/// </summary>
		public static bool IsDoingJackShit(this ref BotCharacter bot)
		{
			return !bot.MoveTarget.IsValid && !bot.Target.IsValid;
		}

		/// <summary>
		/// Sets the bot attack target.
		/// Will set the needed keys on bots BB component and rotate the bot towards the target.
		/// Will also set the "Target" property of the bot.
		/// </summary>
		public static void SetAttackTarget(this ref BotCharacter bot, in EntityRef botEntity, Frame f, in EntityRef target)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botEntity);
			var player = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
			var weaponConfig = f.WeaponConfigs.GetConfig(player->CurrentWeapon.GameId);
			bb->Set(f, Constants.IsAimPressedKey, true);
			if (bot.Target != target)
			{
				PlayerCharacterSystem.OnStartAiming(f, bb, weaponConfig);
			}

			bot.Target = target;
		}

		public static void SetAttackTarget(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f,
										   in EntityRef target)
		{
			botFilter.BotCharacter->SetAttackTarget(botFilter.Entity, f, target);
		}

		public static bool IsInCircle(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f, in BotUpdateGlobalContext botCtx, FPVector3 positionToCheck)
		{
			return IsInCircle(botCtx.circleCenter, botCtx.circleRadius, botCtx.circleIsShrinking, positionToCheck);
		}

		public static bool IsInCircle(FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking, FPVector3 positionToCheck)
		{
			// If circle doesn't exist then we always return true
			if (circleRadius < FP.SmallestNonZero)
			{
				return true;
			}

			var distanceSqr = (positionToCheck.XZ - circleCenter).SqrMagnitude;

			// If circle is shrinking then it's risky to get to consumables on the edge so we don't do it
			if (circleIsShrinking)
			{
				return distanceSqr <= (circleRadius * circleRadius) * (FP._0_20 + FP._0_10);
			}

			return distanceSqr <= (circleRadius * circleRadius) * (FP._0_75);
		}
	}
}