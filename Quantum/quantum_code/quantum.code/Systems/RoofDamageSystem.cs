using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Applies damage (via spells) to players which are standing on roofs / not on the ground (above a certain threshold).
	/// </summary>
	[OptionalSystem]
	public unsafe class RoofDamageSystem : SystemMainThreadFilter<RoofDamageSystem.RoofDamageFilter>
	{
		public override bool StartEnabled => false;
		
		public struct RoofDamageFilter
		{
			public EntityRef Entity;
			public AlivePlayerCharacter* AlivePlayerCharacter;
		}

		public override void Update(Frame f, ref RoofDamageFilter filter)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(filter.Entity);
			if (playerCharacter->IsSkydiving(f, filter.Entity)) return;

			var maxHeight = f.GameConfig.RoofDamageHeight;
			var position = f.Unsafe.GetPointer<Transform3D>(filter.Entity)->Position;
			var currentHeight = position.Y;
			var apc = filter.AlivePlayerCharacter;

			if (currentHeight > maxHeight && !apc->AboveGroundIllegally)
			{
				AddHeightDamage(f, filter.Entity, apc, position);
				f.Events.OnLocalPlayerRoofDetected(f.Get<PlayerCharacter>(filter.Entity).Player, true);
			}
			else if (currentHeight <= maxHeight && apc->AboveGroundIllegally)
			{
				RemoveHeightDamage(f, filter.Entity, apc);
				f.Events.OnLocalPlayerRoofDetected(f.Get<PlayerCharacter>(filter.Entity).Player, false);
			}
		}

		private void AddHeightDamage(Frame f, EntityRef playerEntity, AlivePlayerCharacter* apc, FPVector3 position)
		{
			apc->AboveGroundIllegally = true;

			var spell = f.Create();
			var damage = f.Unsafe.GetPointer<Stats>(playerEntity)->GetStatData(StatType.Health).StatValue *
				f.GameConfig.RoofDamageAmount;

			f.ResolveList(f.Unsafe.GetPointer<Stats>(playerEntity)->SpellEffects).Add(spell);

			f.Add(spell, new Spell
			{
				Id = Spell.HeightDamageId,
				Attacker = spell,
				SpellSource = spell,
				Cooldown = f.GameConfig.RoofDamageCooldown,
				EndTime = FP.MaxValue,
				NextHitTime = f.Time + f.GameConfig.RoofDamageDelay,
				OriginalHitPosition = position,
				PowerAmount = (uint) damage,
				TeamSource = Constants.TEAM_ID_NEUTRAL,
				Victim = playerEntity
			});
		}

		private void RemoveHeightDamage(Frame f, EntityRef playerEntity, AlivePlayerCharacter* apc)
		{
			apc->AboveGroundIllegally = false;

			var spellList = f.ResolveList(f.Unsafe.GetPointer<Stats>(playerEntity)->SpellEffects);

			foreach (var spellEntity in spellList)
			{
				if (f.Unsafe.TryGetPointer<Spell>(spellEntity, out var spell) && spell->Id == Spell.HeightDamageId)
				{
					spellList.Remove(spellEntity);
					f.Destroy(spellEntity);
					return;
				}
			}
		}
	}
}