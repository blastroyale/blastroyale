using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Handles Multishot power-up
	/// </summary>
	public unsafe class MultishotSystem : SystemMainThreadFilter<MultishotSystem.MultishotFilter>, 
	                                      ISignalOnComponentAdded<Multishot>, ISignalOnComponentRemoved<Multishot>,
	                                      ISignalProjectileShootTriggered
	{
		public struct MultishotFilter
		{
			public EntityRef Entity;
			public Multishot* Multishot;
		}

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, Multishot* component)
		{
			component->NextShootTimes = f.AllocateList<FP>();
			component->ProjectileData = f.AllocateList<ProjectileData>();
		}
		
		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, Multishot* component)
		{
			f.FreeList(component->NextShootTimes);
			f.FreeList(component->ProjectileData);
			
			component->NextShootTimes = default;
			component->ProjectileData = default;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref MultishotFilter filter)
		{
			var shootTimes = f.ResolveList(filter.Multishot->NextShootTimes);
			
			if (shootTimes.Count > 0 && f.Time >= shootTimes[0])
			{
				var projectileData = f.ResolveList(filter.Multishot->ProjectileData);
				
				Projectile.Create(f, projectileData[0]);
				
				shootTimes.RemoveAt(0);
				projectileData.RemoveAt(0);
			}
		}

		/// <inheritdoc />
		public void ProjectileShootTriggered(Frame f, EntityRef projectile)
		{
			var data = f.Get<Projectile>(projectile).Data;
			
			if (!f.Unsafe.TryGetPointer<Multishot>(data.Attacker, out var multishot))
			{
				return;
			}
			
			var shotTimes = f.ResolveList(multishot->NextShootTimes);
			var projectileData = f.ResolveList(multishot->ProjectileData);
			var level = multishot->Level - 1;
			var projectilesAmount = multishot->BaseAmount + multishot->AmountLevelUpStep * level;
			var shootTimeGap = multishot->BaseShootTimeGap + multishot->ShootTimeGapLevelUpStep * level;
			var spreadAngle = 0;
			
			if (f.TryGet<Weapon>(data.Attacker, out var weapon))
			{
				spreadAngle = (int) weapon.BulletSpreadAngle;
			}
			
			for (var i = 0; i < projectilesAmount - 1; i++)
			{
				shotTimes.Add(f.Time + shootTimeGap * (i + FP._1));
				
				if (spreadAngle > 0)
				{
					data.NormalizedDirection = Projectile.DivertOnRandomAngle(f, data.OriginalDirection, spreadAngle);
				}
				
				projectileData.Add(data);
			}
		}
	}
}