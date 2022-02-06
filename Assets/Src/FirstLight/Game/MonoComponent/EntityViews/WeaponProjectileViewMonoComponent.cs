using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// This Mono component handles spawning vfx when a weapon receives a quantum projectile fire and player attack event.
	/// </summary>
	public class WeaponProjectileViewMonoComponent : EntityViewBase
	{
		[FormerlySerializedAs("_rocket")] [SerializeField] private GameObject _projectile;

		private IObjectPool<GameObject> _pool;

		protected override void OnInit()
		{
			_pool = new GameObjectPool(4, _projectile);
			
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
				
				projectile.OnEntityDestroyed.AddListener(_ => DespawnProjectileAsset(go));
			}
		}

		private void DespawnProjectileAsset(GameObject go)
		{
			if (this.IsDestroyed() || go == null)
			{
				return;
			}
			
			go.transform.SetParent(_projectile.transform.parent);

			_pool.Despawn(go);
		}
	}
}