using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// TODO: DELETE THE HAZARD SYSTEM
	/// </summary>
	public unsafe class HazardSystem : SystemMainThreadFilter<HazardSystem.HazardFilter>
	{
		public struct HazardFilter
		{
			public EntityRef Entity;
			public Hazard* Hazard;
			public Transform3D* Transform;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref HazardFilter filter)
		{
			var hazard = filter.Hazard;
			
			if (f.Time > hazard->EndTime)
			{
				f.Add<EntityDestroyer>(filter.Entity);
			}
			
			if (f.Time < hazard->NextTickTime)
			{
				return;
			}

			hazard->NextTickTime += hazard->NextTickTime == FP._0 ? f.Time + hazard->Interval : hazard->Interval;

			ProcessAreaHazard(f, hazard, filter);
		}

		private void ProcessAreaHazard(Frame f, Hazard* hazard, HazardFilter filter)
		{

			if (f.GetSingleton<GameContainer>().IsGameOver)
			{
				return;
			}

			f.Events.OnHazardLand(filter.Hazard->GameId, filter.Transform->Position);

			//check the area when the hazard explodes
			var shape = Shape3D.CreateSphere(hazard->Radius);
			var hits = f.Physics3D.OverlapShape(filter.Transform->Position, FPQuaternion.Identity, shape,
												f.Context.TargetAllLayerMask, QueryOptions.HitDynamics | QueryOptions.HitKinematics);
			hits.SortCastDistance();

			//loop through each hit, and create a spell to deal damage to each target hit
			for (var j = 0; j < hits.Count; j++)
			{
				if (f.TryGet<Stats>(hits[j].Entity, out var targetStats))
				{
					var targetHP = targetStats.GetStatData(StatType.Health).BaseValue;
					var damage = targetHP * hazard->PowerAmount;

					var spell = new Spell
					{
						Id = Spell.DefaultId,
						Attacker = hazard->Attacker,
						Cooldown = FP._0,
						EndTime = FP._0,
						NextHitTime = FP._0,
						OriginalHitPosition = filter.Transform->Position,
						PowerAmount = (uint)damage,
						SpellSource = filter.Entity,
						TeamSource = hazard->TeamSource,
						Victim = hits[j].Entity,
						KnockbackAmount = hazard->Knockback
					};

					if(spell.Victim == spell.Attacker)
					{
						spell.TeamSource = 0;
						spell.PowerAmount = (uint)(spell.PowerAmount * Constants.SELF_DAMAGE_MODIFIER);
					}

					if (!QuantumHelpers.ProcessHit(f, spell))
					{
						continue;
					}

					OnHit(f, spell);
				}
			}
		}

		private void OnHit(Frame f, Spell spell)
		{
			var source = f.Get<Hazard>(spell.SpellSource);
			
			if (source.StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, spell.Victim, StatusModifierType.Stun, source.StunDuration);
			}
			
			f.Events.OnHazardHit(spell.Attacker, spell.Victim, source, spell.OriginalHitPosition);
		}
	}
}