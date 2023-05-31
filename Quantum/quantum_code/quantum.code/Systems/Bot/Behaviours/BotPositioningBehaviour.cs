using Photon.Deterministic;

namespace Quantum.Systems.Bot
{
	public unsafe class BotPositioningBehaviour : BotBehaviour
	{
		public override BotBehaviourType[] DisallowedBehaviourTypes => new[] {BotBehaviourType.Static};


		public override bool OnEveryUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			// Update the target teammate of the bot, this is used to follow him. // TODO: Improvement here to auto detect closer team mates, or team mates in danger
			UpdateTargetTeammate(f, ref filter);

			if (CheckIfMoveTargetIsValid(f, ref filter))
			{
				return true;
			}


			// Do not do any decision making if the time has not come, unless a bot have no target to move to
			if (!IsDecisionTime(f, ref filter))
			{
				return false;
			}


			return TryStayCloseToTeammate(f, ref filter)
				|| TryGoToSafeArea(f, ref filter);
		}


		private bool CheckIfMoveTargetIsValid(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			// Check move target in case it disappeared or a bot collected it and needs to move on
			// We set 0.5 second delay before letting a bot making another decision
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				QuantumHelpers.IsDestroyed(f, filter.BotCharacter->MoveTarget))
			{
				filter.BotCharacter->MoveTarget = EntityRef.None;
				filter.NavMeshAgent->Stop(f, filter.Entity, true);
				filter.BotCharacter->NextDecisionTime = f.Time + FP._0_50;
				return true;
			}

			return false;
		}


		// We loop through players to find a reference for alive teammate in case current is dead
		private void UpdateTargetTeammate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			if (filter.BotCharacter->TeamSize <= 1)
			{
				return;
			}

			if (!QuantumHelpers.IsDestroyed(f, filter.BotCharacter->RandomTeammate))
			{
				return;
			}

			var randomTeammate = EntityRef.None;
			var team = f.Get<Targetable>(filter.Entity).Team;

			foreach (var candidate in f.Unsafe.GetComponentBlockIterator<Targetable>())
			{
				if (candidate.Component->Team == team)
				{
					randomTeammate = candidate.Entity;
					break;
				}
			}

			// If no teammates are alive then we let a bot think that they are alone in their team to not look for teammates anymore
			if (randomTeammate == EntityRef.None)
			{
				filter.BotCharacter->TeamSize = 1;
			}

			filter.BotCharacter->RandomTeammate = randomTeammate;
		}


		private bool TryStayCloseToTeammate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			if (filter.BotCharacter->TeamSize <= 1 || QuantumHelpers.IsDestroyed(f, filter.BotCharacter->RandomTeammate))
			{
				return false;
			}

			var teammatePosition = filter.BotCharacter->RandomTeammate.GetPosition(f);
			var botPosition = filter.Transform->Position;
			var vectorToTeammate = teammatePosition - botPosition;
			var maxDistanceSquared = filter.BotCharacter->MaxDistanceToTeammateSquared;

			if (vectorToTeammate.SqrMagnitude < maxDistanceSquared)
			{
				return false;
			}

			var destination = filter.Transform->Position + vectorToTeammate.Normalized * (vectorToTeammate.Magnitude / FP._2);
			var isGoing = IsInCircle(f, ref filter, destination)
				&& QuantumHelpers.SetClosestTarget(f, filter.Entity, destination);

			if (isGoing)
			{
				filter.BotCharacter->MoveTarget = filter.Entity;
			}

			return isGoing;
		}

		private bool TryGoToSafeArea(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			var (circleCenter, circleRadius, circleIsShrinking) = GetCircle(f);

			// Not all game modes have a Shrinking Circle
			if (circleRadius < FP.SmallestNonZero)
			{
				return false;
			}

			var range = FP._0;

			// If circle is shrinking then we try to stay closer to the center
			if (circleIsShrinking)
			{
				range = circleRadius * FP._0_33;
			}
			else
			{
				range = circleRadius * (FP._0_75 + FP._0_10);
			}

			var newPosition = circleCenter;
			var x = f.RNG->Next(-range, range);
			var y = f.RNG->Next(-range, range);
			newPosition.X += x;
			newPosition.Y += y;

			// We try to go into random position OR into circle center (it's good for a very small circle)
			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, newPosition.XOY)
				|| QuantumHelpers.SetClosestTarget(f, filter.Entity, circleCenter.XOY))
			{
				filter.BotCharacter->MoveTarget = filter.Entity;
				return true;
			}

			return false;
		}
	}
}