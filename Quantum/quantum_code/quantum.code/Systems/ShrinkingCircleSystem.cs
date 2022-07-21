using System.Threading;
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
			circle->CurrentRadius = f.Map.WorldSize / FP._2;

			SetShrinkingCircleData(f, circle, f.ShrinkingCircleConfigs.QuantumConfigs[0]);
		}

		/// <inheritdoc />
		public override void Update(Frame f)
		{
			var circle = ProcessShrinkingCircle(f);
			circle->GetMovingCircle(f, out var center, out var radius);

			foreach (var pair in f.GetComponentIterator<AlivePlayerCharacter>())
			{
				var transform = f.Get<Transform3D>(pair.Entity);
				var position = transform.Position;
				var distance = (position.XZ - center).SqrMagnitude;

				if (distance < radius * radius)
				{
					RemoveShrinkingDamage(f, pair.Entity);
				}
				else
				{
					AddShrinkingDamage(f, pair.Entity, position);
				}
			}
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
				circle->CurrentRadius = circle->TargetRadius;
				circle->CurrentCircleCenter = circle->TargetCircleCenter;

				return circle;
			}

			circle->ShrinkingStartTime += circle->ShrinkingDurationTime;
			circle->CurrentRadius = circle->TargetRadius;

			SetShrinkingCircleData(f, circle, configs[circle->Step]);

			return circle;
		}

		private void SetShrinkingCircleData(Frame f, ShrinkingCircle* circle, QuantumShrinkingCircleConfig config)
		{
			circle->Step = config.Step;
			circle->ShrinkingStartTime += config.DelayTime + config.WarningTime;
			circle->ShrinkingDurationTime = config.ShrinkingTime;
			circle->CurrentCircleCenter = circle->TargetCircleCenter;
			circle->TargetRadius = circle->CurrentRadius * config.ShrinkingSizeK;
			circle->Damage = config.MaxHealthDamage;

			QuantumHelpers.TryFindPosOnNavMesh(f, circle->CurrentCircleCenter.XOY,
			                                   circle->CurrentRadius - circle->TargetRadius,
			                                   out var targetPos);
			circle->TargetCircleCenter = targetPos.XZ;

			// When we change a step of a circle, we need to remove current spell from all players
			// So in update the up-to-date spell will be added
			foreach (var pair in f.GetComponentIterator<AlivePlayerCharacter>())
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
				PowerAmount = (uint)damage,
				TeamSource = (int) TeamType.Enemy,
				Victim = playerEntity
			};
			f.Add(newSpell, spell);
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