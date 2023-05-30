using Photon.Deterministic;

namespace Quantum.Systems.Bot
{
	public unsafe class BotCollectableBehaviour : BotBehaviour
	{
		public override bool OnEveryUpdate(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			if (CheckIfFinishedCollecting(f, ref filter))
			{
				return true;
			}


			return TryGoForClosestCollectable(f, ref filter);
		}


		private bool TryGoForClosestCollectable(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			// Strategy is to pick up everything you can possible pick up
			// So we will look at closest pickups and discard things that we can't / don't need to pickup

			var sqrDistance = FP.MaxValue;
			var collectablePosition = FPVector3.Zero;
			var collectableEntity = EntityRef.None;
			var iterator = f.Unsafe.GetComponentBlockIterator<Collectable>();

			var botPosition = filter.Transform->Position;
			var stats = f.Get<Stats>(filter.Entity);
			var maxShields = stats.Values[(int) StatType.Shield].StatValue;
			var currentAmmo = filter.PlayerCharacter->GetAmmoAmountFilled(f, filter.Entity);
			var maxHealth = stats.Values[(int) StatType.Health].StatValue;

			var needWeapon = filter.PlayerCharacter->HasMeleeWeapon(f, filter.Entity) || currentAmmo < FP.SmallestNonZero;
			var needAmmo = currentAmmo < FP._0_99;
			var needShields = stats.CurrentShield < maxShields;
			var needHealth = stats.CurrentHealth < maxHealth;

			foreach (var collectableCandidate in iterator)
			{
				if (filter.BotCharacter->BlacklistedMoveTarget == collectableCandidate.Entity)
				{
					continue;
				}

				if (collectableCandidate.Component->GameId.IsInGroup(GameIdGroup.Weapon) && !needWeapon)
				{
					continue;
				}

				if (f.TryGet<Consumable>(collectableCandidate.Entity, out var consumable))
				{
					var usefulConsumable = consumable.ConsumableType switch
					{
						ConsumableType.Ammo   => needAmmo,
						ConsumableType.Shield => needShields,
						ConsumableType.Health => needHealth,
						_                     => true
					};

					if (!usefulConsumable)
					{
						continue;
					}
				}

				var positionCandidate = f.Get<Transform3D>(collectableCandidate.Entity).Position;
				var newSqrDistance = (positionCandidate - botPosition).SqrMagnitude;

				if (IsInVisionRange(newSqrDistance, ref filter)
					&& newSqrDistance < sqrDistance
					&& IsInCircle(f, ref filter, positionCandidate))
				{
					sqrDistance = newSqrDistance;
					collectablePosition = positionCandidate;
					collectableEntity = collectableCandidate.Entity;
				}
			}

			if (collectableEntity == EntityRef.None)
			{
				return false;
			}

			if (filter.NavMeshAgent->IsActive
				&& filter.BotCharacter->MoveTarget == collectableEntity)
			{
				return true;
			}

			if (QuantumHelpers.SetClosestTarget(f, filter.Entity, collectablePosition))
			{
				filter.BotCharacter->MoveTarget = collectableEntity;
				return true;
			}

			return false;
		}


		private bool CheckIfFinishedCollecting(Frame f, ref BotCharacterSystem.BotCharacterFilter filter)
		{
			if (IsCollecting(f, ref filter, out var collectable))
			{
				filter.BotCharacter->NextDecisionTime = collectable.CollectorsEndTime[filter.PlayerCharacter->Player] + FP._0_50;
				filter.BotCharacter->StuckDetectionPosition = FPVector3.Zero;
				return true;
			}

			return false;
		}

		private bool IsCollecting(Frame f, ref BotCharacterSystem.BotCharacterFilter filter, out Collectable collectable)
		{
			if (filter.BotCharacter->MoveTarget != EntityRef.None &&
				f.TryGet(filter.BotCharacter->MoveTarget, out collectable) &&
				collectable.IsCollecting(filter.PlayerCharacter->Player))
			{
				return true;
			}

			collectable = default;
			return false;
		}
	}
}