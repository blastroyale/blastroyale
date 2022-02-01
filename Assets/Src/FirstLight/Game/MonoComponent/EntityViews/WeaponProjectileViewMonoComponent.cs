using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// This Mono component handles spawning vfx when a weapon receives a quantum projectile fire and player attack event.
	/// </summary>
	public class WeaponProjectileViewMonoComponent : EntityViewBase
	{
		[SerializeField] private GameObject _rocket;

		private IObjectPool<GameObject> _pool;

		protected override void OnInit()
		{
			_pool = new GameObjectPool(3, _rocket);
			
			QuantumEvent.Subscribe<EventOnProjectileFired>(this, OnEventOnProjectileFired);
		}
		
		private void OnEventOnProjectileFired(EventOnProjectileFired callback)
		{
			if (callback.ProjectileData.Attacker == EntityRef && 
			    Services.EntityViewUpdaterService.TryGetView(callback.Projectile, out var projectile))
			{
				var go = _pool.Spawn();
				var goTransform = go.transform;
				
				goTransform.SetParent(projectile.transform);
				
				goTransform.localPosition = Vector3.zero;
				goTransform.localRotation = Quaternion.identity;
				
				projectile.OnEntityDestroyed.AddListener(_ => _pool?.Despawn(go));
			}
		}
	}
}