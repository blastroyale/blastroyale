using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles Transform3D objects that fell out of world
	/// </summary>
	public unsafe class TransformOutOfWorldSystem : SystemMainThreadFilter<TransformOutOfWorldSystem.TransformFilter>
	{
		public struct TransformFilter
		{
			public EntityRef Entity;
			public Transform3D* Transform;
			public AlivePlayerCharacter* AlivePlayer;
			public Stats* Stats;
		}

		public override void Update(Frame f, ref TransformFilter filter)
		{
			if (f.Number % 30 != 0)
			{
				return;
			}

			if (filter.Transform->Position.Y >= Constants.OUT_OF_WORLD_Y_THRESHOLD) return;
			
			var newSpell = f.Create();
			var damage = filter.Stats->GetStatData(StatType.Health).StatValue * FP._0_10 * FP._3;

			f.ResolveList(filter.Stats->SpellEffects).Add(newSpell);
			var spell = new Spell
			{
				Id = Spell.DefaultId,
				Attacker = newSpell,
				SpellSource = newSpell,
				OriginalHitPosition = filter.Transform->Position,
				PowerAmount = (uint)damage,
				TeamSource = Constants.TEAM_ID_NEUTRAL,
				Victim = filter.Entity,
				IgnoreShield = true,
			};
			f.Add(newSpell, spell);
		}
	}
}