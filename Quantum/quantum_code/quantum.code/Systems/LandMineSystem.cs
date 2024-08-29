using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	public unsafe struct LandMineFilter
	{
		public EntityRef Entity;
		public Transform2D* Transform;
		public LandMine* LandMine;
	}

	public unsafe class LandMineSystem : SystemMainThreadFilter<LandMineFilter>, ISignalOnTriggerEnter2D, ISignalUseGenericSpecial
	{
		private FP timeToExplode = FP._1;
		private uint knockBack = 3;


		public void OnTriggerEnter2D(Frame f, TriggerInfo2D info)
		{
			if (!f.Unsafe.TryGetPointer<LandMine>(info.Entity, out var landMine) ||
				!f.Unsafe.TryGetPointer<Stats>(info.Other, out var damaged)) return;

			if (landMine->TriggerableAfter == FP._0 || landMine->TriggerableAfter > f.Time) return;
			
			// The owner can't trigger the landmine
			if (landMine->Owner == info.Other) return;
			
			// Teammates can't trigger the landmine too
			if (f.Unsafe.TryGetPointer<Targetable>(landMine->Owner, out var ownerTeam)
				&& f.Unsafe.TryGetPointer<Targetable>(info.Other, out var collidedTeam))
			{
				if (ownerTeam->Team == collidedTeam->Team)
				{
					return;
				}
			}

			// Triggers the mine here and wait a few seconds before exploding
			TriggerLandMine(f, info.Entity, info.Other, landMine);
		}

		private static void TriggerLandMine(Frame f, EntityRef mineEntity, EntityRef trigerrer, LandMine* landMine)
		{
			landMine->TriggeredTime = f.Time;

			f.Remove<PhysicsCollider2D>(mineEntity);
			f.Events.LandMineTriggered(mineEntity, trigerrer, landMine->Radius);
		}

		public override void Update(Frame f, ref LandMineFilter filter)
		{
			if (filter.LandMine->AutoTrigerred)
			{
				if (filter.LandMine->TriggeredTime <= f.Time)
				{
					TriggerLandMine(f, filter.Entity, EntityRef.None, filter.LandMine);
					filter.LandMine->AutoTrigerred = false;
					return;
				}
			}

			var shouldExplode = filter.LandMine->TriggeredTime != FP._0 && filter.LandMine->TriggeredTime + timeToExplode < f.Time;
			if (!shouldExplode)
			{
				return;
			}


			var spell = Spell.CreateInstant(f, filter.Entity, filter.LandMine->Owner, filter.Entity, filter.LandMine->Damage, knockBack,
				filter.Transform->Position, 0);

			QuantumHelpers.ProcessAreaHit(f, filter.LandMine->Radius, &spell);
			f.Events.LandMineExploded(filter.Entity, filter.Transform->Position);
			f.Destroy(filter.Entity);
		}


		public void UseGenericSpecial(Frame f, Special special, EntityRef attacker, FPVector2 aimDirection, FP maxRange)
		{
			if (special.SpecialId != GameId.SpecialLandmine)
			{
				return;
			}

			var attackerTransform = f.Unsafe.GetPointer<Transform2D>(attacker);
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(attacker);
			var stats = f.Unsafe.GetPointer<Stats>(attacker);
			// Dirty ugly workaround to get the health in percentage, gets the value from the owner percentage
			var targetHP = stats->GetStatData(StatType.Health).BaseValue;
			var damage = targetHP * special.SpecialPower;

			var aimInput = FPVector2.ClampMagnitude(aimDirection, FP._1);
			var position = attackerTransform->Position + FPVector2.Rotate(playerCharacter->ProjectileSpawnOffset, aimInput.ToRotation());
			var minePrototype = f.AssetConfigs.LandMinePrototype;
			var mine = f.Create(minePrototype);

			var shape = Shape2D.CreateCircle(special.Radius);
			var mineComponent = f.Unsafe.GetPointer<LandMine>(mine);
			var mineCollider = f.Unsafe.GetPointer<PhysicsCollider2D>(mine);
			mineCollider->Shape = shape;
			mineComponent->TriggerableAfter = f.Time + special.Speed;
			mineComponent->Radius = special.Radius;
			mineComponent->Damage = (uint)damage;
			mineComponent->Owner = attacker;
			mine.SetPosition(f, position);

			// Check explosiont rigger
			var hits = f.Physics2D.OverlapShape(position, 0, shape,
				f.Context.TargetAllLayerMask, QueryOptions.HitDynamics | QueryOptions.HitKinematics);
			for (var j = 0; j < hits.Count; j++)
			{
				if (hits[j].Entity == attacker) continue;
				if (f.TryGet<Stats>(hits[j].Entity, out var targetStats))
				{
					mineComponent->TriggeredTime = f.Time + FP._1;
					mineComponent->AutoTrigerred = true;
					return;
				}
			}
		}
	}
}