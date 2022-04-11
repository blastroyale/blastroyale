using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="ShrinkingCircle"/>
	/// </summary>
	public unsafe class ShrinkingCircleSystem : SystemMainThread, ISignalOnComponentAdded<ShrinkingCircle>
	{
		/// <inheritdoc />
		public override bool StartEnabled => false;

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, ShrinkingCircle* circle)
		{
			circle->CurrentRadius = f.Map.WorldSize / 2;
			
			SetShrinkingCircleData(f, circle, f.ShrinkingCircleConfigs.QuantumConfigs[0]);
		}

		int prevStep = 0;
		/// <inheritdoc />
		public override void Update(Frame f)
		{
			var circle = ProcessShrinkingCircle(f);
			var lerp = FPMath.Max(0, (f.Time - circle->ShrinkingStartTime) / circle->ShrinkingDurationTime);
			var radius = FPMath.Lerp(circle->CurrentRadius, circle->TargetRadius, lerp);
			var center = FPVector2.Lerp(circle->CurrentCircleCenter, circle->TargetCircleCenter, lerp);
			var currentStep = circle->Step;
			radius *= radius;
			
			foreach (var pair in f.GetComponentIterator<AlivePlayerCharacter>())
			{
				var transform = f.Get<Transform3D>(pair.Entity);
				var position = transform.Position;
				var distance = (position.XZ - center).SqrMagnitude;

				if (distance < radius)
				{
					RemoveShrinkingDamage(f, pair.Entity);
				}
				else
				{
					AddShrinkingDamage(f, pair.Entity, position);
				}

				//whenever the step changes, update the shrinking damage spell
				if (prevStep != currentStep)
				{
					RemoveShrinkingDamage(f, pair.Entity);
					AddShrinkingDamage(f, pair.Entity, position);
				}
			}

			prevStep = currentStep;
		}

		private ShrinkingCircle* ProcessShrinkingCircle(Frame f)
		{
			var circle = f.Unsafe.GetPointerSingleton<ShrinkingCircle>();

			if (f.Time < circle->ShrinkingStartTime + circle->ShrinkingDurationTime)
			{
				return circle;
			}
			
			var configs = f.ShrinkingCircleConfigs.QuantumConfigs;

			if (circle->Step >= configs.Count)
			{
				circle->ShrinkingStartTime = FP.MaxValue;
				circle->ShrinkingDurationTime = FP.MaxValue;
					
				return circle;
			}
			
			circle->ShrinkingStartTime += circle->ShrinkingDurationTime;
			circle->CurrentRadius = circle->TargetRadius;

			SetShrinkingCircleData(f, circle, configs[circle->Step]);
			
			return circle;
		}

		private void SetShrinkingCircleData(Frame f, ShrinkingCircle* circle, QuantumShrinkingCircleConfig config)
		{
			var gameConfig = f.GameConfig;
			//var borderK = gameConfig.ShrinkingSizeK * gameConfig.ShrinkingBorderK;

			circle->Step = config.Step;
			circle->ShrinkingStartTime += config.DelayTime + config.WarningTime;
			circle->ShrinkingDurationTime = config.ShrinkingTime;
			circle->CurrentCircleCenter = circle->TargetCircleCenter;

			if (circle->Step != f.ShrinkingCircleConfigs.QuantumConfigs.Count - 1)
			{
				circle->TargetRadius = circle->CurrentRadius * gameConfig.ShrinkingSizeK;
			}
			else
			{
				circle->TargetRadius = FP._0_01;
			}

			FP maxDist = circle->CurrentRadius - circle->TargetRadius;
			circle->TargetCircleCenter = randomCoordWithinCircle(maxDist, circle->CurrentCircleCenter, f);

			f.Events.OnNewShrinkingCircle(*circle);
		}

		private FPVector2 randomCoordWithinCircle(FP maxRadius, FPVector2 currentCircle, Frame f)
		{
			var randA = f.RNG->NextInclusive(FP._0, FP._1) * 2 * FP.Pi;
			var randR = f.RNG->NextInclusive(FP._0, maxRadius);
			var xPos = currentCircle.X + randR * FPMath.Cos(randA);
			var yPos = currentCircle.Y + randR * FPMath.Sin(randA);

			return new FPVector2(xPos, yPos);
		}

		private void AddShrinkingDamage(Frame f, EntityRef playerEntity, FPVector3 position)
		{
			if (TryGetSpellEntity(f, playerEntity, false, out _))
			{
				return;
			}

			var newSpell = f.Create();
			var circle = f.Unsafe.GetPointerSingleton<ShrinkingCircle>();

			f.ResolveList(f.Unsafe.GetPointer<Stats>(playerEntity)->SpellEffects).Add(newSpell);
			f.Add(newSpell, new Spell
			{
				Id = Spell.ShrinkingCircleId,
				Attacker = newSpell,
				SpellSource = newSpell,
				Cooldown = f.GameConfig.ShrinkingDamageCooldown,
				EndTime = FP.MaxValue,
				NextHitTime = FP._0,
				OriginalHitPosition = position,
				PowerAmount = f.GameConfig.ShrinkingDamage * (uint)circle->Step,
				TeamSource = (int) TeamType.Enemy,
				Victim = playerEntity
			});
		}

		private void RemoveShrinkingDamage(Frame f, EntityRef playerEntity)
		{
			if (TryGetSpellEntity(f, playerEntity, true, out var spellEntity))
			{
				f.Destroy(spellEntity);
			}
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
	}
}