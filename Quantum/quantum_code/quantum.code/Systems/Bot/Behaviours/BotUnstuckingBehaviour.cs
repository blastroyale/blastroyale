using Photon.Deterministic;

namespace Quantum.Systems.Bot
{
	public unsafe class BotUnstuckingBehaviour : BotBehaviour
	{
		public override BotBehaviourType[] DisallowedBehaviourTypes => new[] {BotBehaviourType.Static};

		public override bool OnEveryUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			if (!IsDecisionTime(f, ref filter))
			{
				return false;
			}

			return CheckIfBotIsStuck(f, ref filter);
		}

		private bool CheckIfBotIsStuck(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			// Check that bot isn't stuck in place. If it does then force repath
			// If this solution won't prevent bots from standing in one place doing nothing then we should consider
			// doing check "filter.NavMeshAgent->IsOnLink(f)" instead of "filter.NavMeshAgent->IsActive"
			// because sometimes "filter.NavMeshAgent->IsActive" is TRUE even though the actor doesn't visually move,
			// hence why we are Forcing Repath;
			// Another reason why a bot isn't going anywhere can be because the point they want to go to is
			// unreachable due to how navmesh were baked or consumable placed
			if (FPVector3.DistanceSquared(filter.BotCharacter->StuckDetectionPosition,
					filter.Transform->Position) < Constants.BOT_STUCK_DETECTION_SQR_DISTANCE)
			{
				// Blacklist Entity that we are trying to move to so we don't try to go to it again
				filter.BotCharacter->BlacklistedMoveTarget = filter.BotCharacter->MoveTarget;
				filter.BotCharacter->MoveTarget = EntityRef.None;

				// In case a bot is stuck - try moving a little bit sideways or back
				var sidewaysPosition = filter.Transform->Position;
				var direction = f.RNG->Next();
				if (direction < FP._0_33)
				{
					sidewaysPosition += filter.Transform->Left * FP._3;
				}
				else if (direction < (FP._0_33 * FP._2))
				{
					sidewaysPosition += filter.Transform->Right * FP._3;
				}
				else
				{
					sidewaysPosition += filter.Transform->Back * FP._3;
				}

				if (QuantumHelpers.SetClosestTarget(f, filter.Entity, sidewaysPosition))
				{
					filter.BotCharacter->NextDecisionTime = f.Time + FP._1;
					filter.BotCharacter->StuckDetectionPosition = FPVector3.Zero;
					filter.BotCharacter->MoveTarget = filter.Entity;
					return true;
				}
			}

			filter.BotCharacter->StuckDetectionPosition = filter.Transform->Position;
			return false;
		}
	}
}