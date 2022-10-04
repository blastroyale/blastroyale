using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This object represents a Vfx that can be spawned at any time in the game.
	/// The Vfx is defined by the given <typeparamref name="T"/> id type
	/// </summary>
	public abstract class Vfx<T> : MonoBehaviour, IPoolEntitySpawn, IPoolEntityDespawn where T : struct, Enum
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
			if (_service != null && _service.HasPool(_id))
			{
				return _service.Despawn(this);
			}
			
			Destroy(gameObject);
			return false;
		}
		
		protected virtual void OnInitialized() {}
		
		protected virtual void OnSpawned() {}
		
		protected virtual void OnDespawned() {}

		protected virtual async void Despawner(float time)
		{
			if (time is < -float.Epsilon and > -1)
			{
				time = 0;
			}
			
			await Task.Delay((int) (time * 1000));

			if (this != null && gameObject != null)
			{
				Despawn();
			}
		}

		internal void Init(IVfxService<T> service)
		{
			_service = service;
			
			OnInitialized();
		}
	}
	
	/// <summary>
	/// This service allows to manage multiple <see cref="Vfx{T}"/> of the defined <typeparamref name="T"/> vfx enum type.
	/// </summary>
	public interface IVfxService<T> : IDisposable where T : struct, Enum
	{
		/// <inheritdoc cref="IPoolService.Spawn{T}"/>
		Vfx<T> Spawn(T vfxId);
		/// <inheritdoc cref="IPoolService.Despawn{T}"/>
		bool Despawn(Vfx<T> vfx);
		/// <inheritdoc cref="IPoolService.DespawnAll{T}"/>
		void DespawnAll(T vfxId);
		/// <inheritdoc cref="IPoolService.DespawnAll{T}"/>
		void DespawnAll();
		/// <inheritdoc cref="IPoolService.Clear"/>
		List<Vfx<T>> Clear();
		/// <inheritdoc cref="IPoolService.HasPool"/>
		bool HasPool(T vfxId);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Used only on internal creation data and should not be exposed to the views
	/// </remarks>
	public interface IVfxInternalService<T> : IVfxService<T> where T : struct, Enum
	{
		/// <inheritdoc cref="IPoolService.AddPool{T}"/>
		void AddPool(Vfx<T> reference, uint initialSize = 5);
		/// <inheritdoc cref="IPoolService.AddPool{T}"/>
		void AddPool(T vfxId, GameObjectPool<Vfx<T>> pool);
		/// <inheritdoc cref="IPoolService.RemovePool{T}"/>
		void RemovePool(T vfxId);
	}
	
	/// <inheritdoc />
	public class VfxService<T> : IVfxInternalService<T> where T : struct, Enum
	{
		/// <summary>
		/// Use this container when instantiating new VFX elements
		/// </summary>
		public readonly Transform Container;
		
		private readonly IDictionary<T, GameObjectPool<Vfx<T>>> _pools = new Dictionary<T, GameObjectPool<Vfx<T>>>();

		public VfxService()
		{
			Container = new GameObject("Vfx Container").transform;
		}

		/// <inheritdoc />
		public Vfx<T> Spawn(T vfxId)
		{
			return GetPool(vfxId).Spawn();
		}

		/// <inheritdoc />
		public bool Despawn(Vfx<T> vfx)
		{
			if(_pools.TryGetValue(vfx.Id, out var pool) && pool.Despawn(vfx))
			{
				vfx.transform.SetParent(Container);
				
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public void DespawnAll(T vfxId)
		{
			GetPool(vfxId).DespawnAll();
		}

		/// <inheritdoc />
		public void DespawnAll()
		{
			foreach (var pool in _pools)
			{
				pool.Value.DespawnAll();
			}
		}

		/// <inheritdoc />
		public List<Vfx<T>> Clear()
		{
			var list = new List<Vfx<T>>();
			
			foreach (var pool in _pools)
			{
				list.AddRange(pool.Value.Clear());
			}
			
			_pools.Clear();

			return list;
		}

		/// <inheritdoc />
		public void AddPool(Vfx<T> reference, uint initialSize = 5)
		{
			AddPool(reference.Id, new GameObjectPool<Vfx<T>>(initialSize, reference, Instantiator));
		}

		/// <inheritdoc />
		public void AddPool(T vfxId, GameObjectPool<Vfx<T>> pool)
		{
			_pools.Add(vfxId, pool);
		}

		/// <inheritdoc />
		public void RemovePool(T vfxType)
		{
			_pools.Remove(vfxType);
		}

		/// <inheritdoc />
		public bool HasPool(T vfxType)
		{
			return _pools.ContainsKey(vfxType);
		}
		
		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var pool in _pools)
			{
				var spawned = pool.Value.SpawnedReadOnly;
				
				pool.Value.Clear();

				for (var i = 0; i < spawned.Count; i++)
				{
					if (spawned[i]?.gameObject != null)
					{
						UnityEngine.Object.Destroy(spawned[i].gameObject);
					}
				}
			}
			
			_pools.Clear();

			if (Container != null && Container.gameObject != null)
			{
				UnityEngine.Object.Destroy(Container.gameObject);
			}
		}
		
		private Vfx<T> Instantiator(Vfx<T> entityRef)
		{
			var instance = UnityEngine.Object.Instantiate(entityRef, Container, true);

			instance.gameObject.SetActive(false);
			instance.Init(this);

			return instance;
		}

		private ObjectRefPool<Vfx<T>> GetPool(T vfxType)
		{
			if (!_pools.TryGetValue(vfxType, out var pool))
			{
				throw new ArgumentException("The pool was not initialized for the vfx type " + vfxType);
			}

			return pool;
		}
	}
}