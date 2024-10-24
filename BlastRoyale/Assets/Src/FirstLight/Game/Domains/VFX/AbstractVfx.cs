using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Services;
using UnityEngine;

namespace FirstLight.Game.Domains.VFX
{
	/// <summary>
	/// This object represents a Vfx that can be spawned at any time in the game.
	/// The Vfx is defined by the given <typeparamref name="T"/> id type
	/// </summary>
	public abstract class AbstractVfx<T> : MonoBehaviour, IPoolEntitySpawn, IPoolEntityDespawn where T : struct, Enum
	{
		[SerializeField] private EnumSelector<T> _id;

		private IVfxService<T> _service;

		/// <summary>
		/// Requests the Id that represents this Vfx from the defined <typeparamref name="T"/>
		/// </summary>
		public T Id => _id.GetSelection();

		/// <inheritdoc />
		public void OnSpawn()
		{
			OnSpawned();
			var particle = GetComponent<ParticleSystem>();
			if (particle != null)
			{
				var main = particle.main;
				main.stopAction = ParticleSystemStopAction.Disable;
			}
		}

		/// <inheritdoc />
		public void OnDespawn()
		{
			OnDespawned();
		}

		/// <summary>
		/// Despawns this Vfx and sends it back to the <see cref="VfxService{T}"/> pool
		/// </summary>
		public bool Despawn()
		{
			if (_service != null && _service.HasPool(_id.GetSelection()))
			{
				return _service.Despawn(this);
			}

			Destroy(gameObject);
			return false;
		}

		protected virtual void OnInitialized()
		{
		}

		protected virtual void OnSpawned()
		{
		}

		protected virtual void OnDespawned()
		{
		}

		protected virtual async UniTaskVoid Despawner(float time)
		{
			if (time is < 0 and > -1)
			{
				await UniTask.NextFrame(); // cannot despawn on same frame it spawned to avoid messing the pool
			}
			else
			{
				await UniTask.Delay((int) (time * 1000));
			}

			if (this != null && gameObject != null)
			{
				Despawn();
			}
		}

		/// <summary>
		/// Starts the despawn timer to despawn this <see cref="Vfx{T}"/> in the given <paramref name="time"/>
		/// </summary>
		public void StartDespawnTimer(float time)
		{
			Despawner(time).Forget();
		}

		internal void Init(IVfxService<T> service)
		{
			_service = service;

			OnInitialized();
		}
	}
}