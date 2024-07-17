using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="ShrinkingCircle"/>
	/// </summary>
	[OptionalSystem]
	public unsafe class ShrinkingCircleSystem : SystemMainThread
	{
		/// <inheritdoc />
		public override bool StartEnabled => false;

		/// <inheritdoc />
		public override void OnEnabled(Frame f)
		{
			base.OnEnabled(f);

			var circle = f.Unsafe.GetOrAddSingletonPointer<ShrinkingCircle>();
			
			if(f.Context.MapConfig.IsLegacyMap)
			{
				circle->CurrentRadius = f.Map.WorldSize / FP._2;
			}
			else
			{
				circle->CurrentRadius = (f.Map.WorldSize / FP._2) * FPMath.Sqrt(FP._2);
			}
			circle->Step = -1;
		}

		/// <inheritdoc />
		public override void Update(Frame f)
		{
			if (!f.Context.GameModeConfig.ShrinkingCircleCenteredOnPlayer ||
				f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[0].Entity != EntityRef.None)
			{
				var circle = f.Unsafe.GetPointerSingleton<ShrinkingCircle>();

				// Set initial shrinking circle data
				if (circle->Step < 0)
				{
					var config = f.Context.MapShrinkingCircleConfigs[0];
					SetShrinkingCircleData(f, circle, config);
				}

				ProcessShrinkingCircle(f, circle);
				circle->GetMovingCircle(f, out var center, out var radius);

				foreach (var pair in f.Unsafe.GetComponentBlockIterator<AlivePlayerCharacter>())
				{
					var transform = f.Unsafe.GetPointer<Transform3D>(pair.Entity);
					var position = transform->Position;
					var isInside = (position.XZ - center).SqrMagnitude < radius * radius;

					if (pair.Component->TakingCircleDamage && isInside)
					{
						RemoveShrinkingDamage(f, pair.Entity);
					}
					else if (!pair.Component->TakingCircleDamage && !isInside)
					{
						AddShrinkingDamage(f, pair.Entity, position);
					}
				}
			}
		}

		private void ProcessShrinkingCircle(Frame f, ShrinkingCircle* circle)
		{
			if (f.Time < circle->ShrinkingStartTime + circle->ShrinkingDurationTime)
			{
				return;
			}

			if (circle->Step >= f.Context.MapShrinkingCircleConfigs.Count)
			{
				circle->ShrinkingStartTime = int.MaxValue;
				circle->ShrinkingDurationTime = int.MaxValue;
				circle->CurrentRadius = circle->TargetRadius;
				circle->CurrentCircleCenter = circle->TargetCircleCenter;

				return;
			}

			circle->ShrinkingStartTime += circle->ShrinkingDurationTime;
			circle->CurrentRadius = circle->TargetRadius;

			var config = f.Context.MapShrinkingCircleConfigs[circle->Step];
			SetShrinkingCircleData(f, circle, config);
		}

		private void SetShrinkingCircleData(Frame f, ShrinkingCircle* circle, in QuantumShrinkingCircleConfig config)
		{
			if (f.Context.GameModeConfig.ShrinkingCircleCenteredOnPlayer)
			{
				SetShrinkingCircleCenteredOnLocalPlayer(circle, f);
			}

			circle->Step = config.Step;
			circle->ShrinkingStartTime = f.Time.AsInt + config.DelayTime.AsInt + config.WarningTime.AsInt; // Get time as int to round it down
			circle->CurrentCircleCenter = circle->TargetCircleCenter;
			circle->TargetRadius = circle->CurrentRadius * config.ShrinkingSizeK;
			
			circle->ShrinkingDurationTime = config.ShrinkingTime.AsInt; // TODO: Storing configs in components isn't ideal
			circle->Damage = config.MaxHealthDamage; // TODO: Storing configs in components isn't ideal
			circle->ShrinkingWarningTime = config.WarningTime.AsInt; // TODO: Storing configs in components isn't ideal

			var fitRadius = circle->CurrentRadius - circle->TargetRadius;

			var newSafeSpaceAreaSizeK = f.Context.Mutators.HasFlagFast(Mutator.SafeZoneInPlayableArea)
											? FPMath.Clamp(config.NewSafeSpaceAreaSizeK, FP._0, FP._1)
											: config.NewSafeSpaceAreaSizeK;
			var radiusToPickNewCenter = FPMath.Max(0, fitRadius * newSafeSpaceAreaSizeK);
			var halfWorldSize = f.Map.WorldSize / FP._2;
			
			// We use mathematical randomization to find a potential new center
			var targetPos = new FPVector3(circle->CurrentCircleCenter.X + f.RNG->Next(-radiusToPickNewCenter, radiusToPickNewCenter),
										  FP._0,
										  circle->CurrentCircleCenter.Y - f.RNG->Next(-radiusToPickNewCenter, radiusToPickNewCenter));
			
			// Then we ensure that this center is not outside of map boundaries
			targetPos.X = FPMath.Clamp(targetPos.X, -halfWorldSize, halfWorldSize);
			targetPos.Z = FPMath.Clamp(targetPos.Z, -halfWorldSize, halfWorldSize);
			
			// Then we correct this potential new center so it's on the NavMesh
			// we skip early steps whose circles are big enough to not require correction
			if (config.Step > 1)
			{
				QuantumHelpers.TryFindPosOnNavMesh(f, targetPos,
												   FPMath.Min(circle->TargetRadius, Constants.SHRINKINGCIRCLE_NAVMESH_CORRECTION_RADIUS),
												   out targetPos);
			}
			
			circle->TargetCircleCenter = targetPos.XZ;

			// When we change a step of a circle, we need to remove current spell from all players
			// So in update the up-to-date spell will be added
			foreach (var pair in f.Unsafe.GetComponentBlockIterator<AlivePlayerCharacter>())
			{
				RemoveShrinkingDamage(f, pair.Entity);
			}

			f.Events.OnNewShrinkingCircle(*circle);

			// Air drop
			if (config.AirdropChance > 0 && f.RNG->Next() <= config.AirdropChance + circle->AirDropChance)
			{
				AirDrop.Create(f, config);
			}
			else
			{
				circle->AirDropChance += config.AirdropChance;
			}
		}

		private void AddShrinkingDamage(Frame f, EntityRef playerEntity, FPVector3 position)
		{
			if (TryGetSpellEntity(f, playerEntity, false, out _))
			{
				return;
			}

			var newSpell = f.Create();
			var circle = f.Unsafe.GetPointerSingleton<ShrinkingCircle>();
			var damage = f.Unsafe.GetPointer<Stats>(playerEntity)->GetStatData(StatType.Health).StatValue * circle->Damage;

			f.ResolveList(f.Unsafe.GetPointer<Stats>(playerEntity)->SpellEffects).Add(newSpell);
			var spell = new Spell
			{
				Id = Spell.ShrinkingCircleId,
				Attacker = newSpell,
				SpellSource = newSpell,
				Cooldown = f.GameConfig.ShrinkingDamageCooldown,
				EndTime = FP.MaxValue,
				NextHitTime = FP._0,
				OriginalHitPosition = position,
				PowerAmount = (uint) damage,
				TeamSource = Constants.TEAM_ID_NEUTRAL,
				Victim = playerEntity,
				IgnoreShield = true,
			};
			f.Add(newSpell, spell);
			f.Unsafe.GetPointer<AlivePlayerCharacter>(playerEntity)->TakingCircleDamage = true;
		}

		private void RemoveShrinkingDamage(Frame f, EntityRef playerEntity)
		{
			if (TryGetSpellEntity(f, playerEntity, true, out var spellEntity))
			{
				f.Destroy(spellEntity);
			}

			f.Unsafe.GetPointer<AlivePlayerCharacter>(playerEntity)->TakingCircleDamage = false;
		}

		private bool TryGetSpellEntity(Frame f, EntityRef playerEntity, bool removeIfFound, out EntityRef spellEntity)
		{
			var spellList = f.ResolveList(f.Unsafe.GetPointer<Stats>(playerEntity)->SpellEffects);

			spellEntity = EntityRef.None;

			for (var i = spellList.Count - 1; i > -1; --i)
			{
				if (!f.TryGet<Spell>(spellList[i], out var spell))
				{
					spellList.RemoveAt(i);

					continue;
				}

				if (spell.Id != Spell.ShrinkingCircleId)
				{
					continue;
				}

				spellEntity = spellList[i];

				if (removeIfFound)
				{
					spellList.RemoveAt(i);
				}

				return true;
			}

			return false;
		}

		private void SetShrinkingCircleCenteredOnLocalPlayer(ShrinkingCircle* circle, Frame f)
		{
			var characterEntity = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[0].Entity;
			if (QuantumHelpers.IsDestroyed(f, characterEntity))
			{
				return;
			}

			if (f.TryGet<Transform3D>(characterEntity, out var trans))
			{
				circle->TargetCircleCenter = new FPVector2(trans.Position.X, trans.Position.Z);
			}
		}
	}
}