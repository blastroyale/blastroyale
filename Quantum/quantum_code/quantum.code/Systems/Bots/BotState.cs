using System.Diagnostics;
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

		public static void SetNextDecisionDelay(this ref BotCharacter bot, EntityRef botEntity, Frame f, in FP seconds)
		{
			bot.NextDecisionTime = f.Time + seconds;
		}

		public static void ResetDecisionDelay(this ref BotCharacter bot, Frame f)
		{
			bot.NextDecisionTime = f.Time;
		}

		public static bool IsLowLife(this ref BotCharacter bot, in EntityRef entity, Frame f)
		{
			return Stats.VitalityRatio(entity, f) < FP._0_20;
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
		/// Resets the bot target waypoint. This is done by setting no MoveTarget and lowering next decision time so next
		/// decision is done shortly
		/// </summary>
		public static void ResetTargetWaypoint(this ref BotCharacter bot, EntityRef botEntity, Frame f)
		{
			BotLogger.LogAction(f, botEntity, "clear waypoint", TraceLevel.Warning);
			bot.ResetDecisionDelay(f);
			bot.StuckDetectionPosition = FPVector2.Zero;
			bot.MovementType = BotMovementType.None;
		}

		public static void StopMovement(this ref BotCharacter bot, Frame f, EntityRef entityRef)
		{
			if (f.Unsafe.TryGetPointer<NavMeshPathfinder>(entityRef, out var navMeshAgent))
			{
				bot.StopMovement(f, entityRef, navMeshAgent);
			}
		}

		public static void StopMovement(this ref BotCharacter bot, Frame f, EntityRef entityRef, NavMeshPathfinder* pathfinder)
		{
			BotLogger.LogAction(f, entityRef, "stop movement", TraceLevel.Warning);
			pathfinder->Stop(f, entityRef, true);
			bot.ResetTargetWaypoint(entityRef, f);
			bot.Target = EntityRef.None;
		}


		/// <summary>
		/// Checks if a given bot is not doing shit
		/// </summary>
		public static bool IsDoingJackShit(this ref BotCharacter bot)
		{
			return (!bot.MoveTarget.IsValid && !bot.Target.IsValid) || bot.MovementType == BotMovementType.None;
		}

		/// <summary>
		/// Sets the bot attack target.
		/// Will set the needed keys on bots BB component and rotate the bot towards the target.
		/// Will also set the "Target" property of the bot.
		/// </summary>
		public static void SetAttackTarget(this ref BotCharacter bot, in EntityRef botEntity, Frame f, in EntityRef target)
		{
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(botEntity);
			bb->Set(f, Constants.IS_AIM_PRESSED_KEY, true);
			if (bot.Target != target)
			{
				var player = f.Unsafe.GetPointer<PlayerCharacter>(botEntity);
				var weaponConfig = f.WeaponConfigs.GetConfig(player->CurrentWeapon.GameId);
				PlayerCharacterSystem.OnStartAiming(f, bb, weaponConfig);
				BotLogger.LogAction(f, botEntity, "changing target to" + target);
			}

			bot.Target = target;
		}

		public static void SetAttackTarget(this ref BotCharacterSystem.BotCharacterFilter botFilter, Frame f,
										   in EntityRef target)
		{
			botFilter.BotCharacter->SetAttackTarget(botFilter.Entity, f, target);
		}
		

		public static bool IsInCircleWithSpareSpace(this ref BotCharacterSystem.BotCharacterFilter filter, Frame f, in BotUpdateGlobalContext botCtx, FPVector2 positionToCheck)
		{
			return IsInCircleWithSpareSpace(botCtx.circleCenter, botCtx.circleRadius, botCtx.circleIsShrinking, positionToCheck);
		}

		public static bool IsPositionSafe(in BotUpdateGlobalContext ctx,
										  in BotCharacterSystem.BotCharacterFilter bot,
										  FPVector2 position)
		{
			FPVector2 center;
			FP radius;
			if (ctx.circleTimeToShrink < bot.BotCharacter->TimeStartRunningFromCircle)
			{
				center = ctx.circleTargetCenter;
				radius = ctx.circleTargetRadius;
			}
			else
			{
				radius = ctx.circleRadius;
				center = ctx.circleCenter;
				if (ctx.circleIsShrinking)
				{
					radius -= FP._10 * FP._2;
				}
			}

			if (radius < FP.SmallestNonZero)
			{
				return true;
			}

			var circleRadiusSqr = radius * radius;
			var distanceSqr = (position - center).SqrMagnitude;
			return distanceSqr <= circleRadiusSqr;
		}


		public static bool IsInCircleWithSpareSpace(FPVector2 circleCenter, FP circleRadius, bool circleIsShrinking, FPVector2 positionToCheck)
		{
			// If circle doesn't exist then we always return true
			circleRadius = circleRadius - FP._10 - FP._10;
			if (circleIsShrinking)
			{
				circleRadius -= FP._10;
			}

			if (circleRadius < FP.SmallestNonZero)
			{
				return true;
			}

			var circleRadiusSqr = circleRadius * circleRadius;
			var distanceSqr = (positionToCheck - circleCenter).SqrMagnitude;
			return distanceSqr <= circleRadiusSqr;
		}

		public static bool IsInCircle(FPVector2 circleCenter, FP circleRadius, FPVector2 positionToCheck)
		{
			// If circle doesn't exist then we always return true
			if (circleRadius < FP.SmallestNonZero)
			{
				return true;
			}

			var circleRadiusSqr = circleRadius * circleRadius;
			var distanceSqr = (positionToCheck - circleCenter).SqrMagnitude;
			return distanceSqr <= circleRadiusSqr;
		}
	}
}