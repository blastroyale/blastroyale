using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// This Mono component handles spawning vfx when a weapon receives a quantum projectile fire and player attack event.
	/// </summary>
	public class WeaponProjectileViewMonoComponent : EntityViewBase
	{
		[SerializeField, Required] private ProjectileViewMonoComponent _projectile;

		private IObjectPool<ProjectileViewMonoComponent> _pool;

		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnProjectileFired>(this, OnEventOnProjectileFired);
		}

		protected override void OnInit(QuantumGame game)
		{
			_pool = new GameObjectPool<ProjectileViewMonoComponent>(4, _projectile);
		}
		
		private void OnEventOnProjectileFired(EventOnProjectileFired callback)
		{
			if (callback.ProjectileData.Attacker == EntityRef && 
			    MatchServices.EntityViewUpdaterService.TryGetView(callback.Projectile, out var projectile))
			{
				var go = _pool.Spawn();
				var goTransform = go.transform;

				goTransform.SetParent(projectile.transform);
				
				goTransform.localPosition = Vector3.zero;
				goTransform.localRotation = Quaternion.identity;
				
				projectile.OnEntityDestroyed.AddListener(_ => DespawnProjectileAsset(go));
				
				go.SetEntityView(callback.Game, projectile);
			}
		}

		private void DespawnProjectileAsset(ProjectileViewMonoComponent go)
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