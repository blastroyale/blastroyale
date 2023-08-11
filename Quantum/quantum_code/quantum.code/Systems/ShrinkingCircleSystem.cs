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
			circle->CurrentRadius = f.Map.WorldSize / FP._2;
			circle->Step = -1;
		}

		/// <inheritdoc />
		public override void Update(Frame f)
		{
			if (!f.Context.GameModeConfig.ShrinkingCircleCenteredOnPlayer ||
				f.GetSingleton<GameContainer>().PlayersData[0].Entity != EntityRef.None)
			{
				var circle = f.Unsafe.GetPointerSingleton<ShrinkingCircle>();

				// Set initial shrinking circle data
				if (circle->Step < 0)
				{
					var config = f.Context.MapShrinkingCircleConfigs[0];
					SetShrinkingCircleData(f, circle, ref config);
				}

				ProcessShrinkingCircle(f, circle);
				circle->GetMovingCircle(f, out var center, out var radius);

				foreach (var pair in f.Unsafe.GetComponentBlockIterator<AlivePlayerCharacter>())
				{
					var transform = f.Get<Transform3D>(pair.Entity);
					var position = transform.Position;
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
			SetShrinkingCircleData(f, circle, ref config);
		}

		private void SetShrinkingCircleData(Frame f, ShrinkingCircle* circle, ref QuantumShrinkingCircleConfig config)
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
			var radiusDiff = circle->CurrentRadius - fitRadius;
			var radiusToPickNewCenter = FP._0;

			if (config.NewSafeSpaceAreaSizeK > FP._1)
			{
				radiusToPickNewCenter = FPMath.Min(circle->CurrentRadius,
					fitRadius + radiusDiff * (config.NewSafeSpaceAreaSizeK - FP._1));
			}
			else
			{
				radiusToPickNewCenter = FPMath.Max(0, fitRadius * config.NewSafeSpaceAreaSizeK);
			}

			QuantumHelpers.TryFindPosOnNavMesh(f, circle->CurrentCircleCenter.XOY,
				radiusToPickNewCenter,
				out var targetPos);
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
				AirDrop.Create(f, ref config);
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
			var circle = f.GetSingleton<ShrinkingCircle>();
			var damage = f.Get<Stats>(playerEntity).GetStatData(StatType.Health).StatValue * circle.Damage;

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
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[0].Entity;
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