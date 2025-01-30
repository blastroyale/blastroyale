using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.Modules.Services.Runtime;
using FirstLight.Services;
using Quantum;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.Game.Domains.VFX
{
	/// <summary>
	/// This service allows to manage multiple <see cref="AbstractVfx{T}"/> of the defined <typeparamref name="T"/> vfx enum type.
	/// </summary>
	public interface IVfxService<T> : IDisposable where T :
		struct, Enum
	{
		UniTask AddPoolAsync(AbstractVfx<T> reference, int initialSize = 5);

		public bool Despawn(AbstractVfx<T> abstractVfx);

		/// <inheritdoc cref="IPoolService.Spawn{T}"/>
		/// If pool = false you are resposible for destroying the vfx
		AbstractVfx<T> Spawn(T vfxId, bool pool = true);

		/// If pool = false you are resposible for destroying the vfx
		public UniTask<AbstractVfx<T>> SpawnAsync(T vfxId, bool pool = true);

		/// <inheritdoc cref="IPoolService.HasPool"/>
		bool HasPool(T vfxId);

		/// <inheritdoc cref="IPoolService.RemovePool{T}"/>
		void RemovePool(T vfxId);
	}

	/// <inheritdoc />
	public class VfxService<T> : IVfxService<T> where T : struct, Enum
	{
		/// <summary>
		/// Use this container when instantiating new VFX elements
		/// </summary>
		public readonly Transform Container;

		private readonly IDictionary<T, AsyncGameObjectPool<AbstractVfx<T>>> _pools = new Dictionary<T, AsyncGameObjectPool<AbstractVfx<T>>>();
		private IVfxService<T> _vfxServiceImplementation;

		public VfxService() : this("Vfx Container")
		{
		}

		public VfxService(string containerName)
		{
			Container = new GameObject(containerName).transform;
		}

		/// <inheritdoc />
		public AbstractVfx<T> Spawn(T vfxId, bool pool = true)
		{
			return pool ? GetPool(vfxId).Spawn() : GetPool(vfxId).CreateObject();
		}

		public UniTask<AbstractVfx<T>> SpawnAsync(T vfxId, bool pool = true)
		{
			return pool ? GetPool(vfxId).SpawnAsync() : GetPool(vfxId).CreateObjectAsync();
		}

		/// <inheritdoc />
		public bool Despawn(AbstractVfx<T> abstractVfx)
		{
			if (_pools.TryGetValue(abstractVfx.Id, out var pool) && pool.Despawn(abstractVfx))
			{
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public async UniTask AddPoolAsync(AbstractVfx<T> reference, int initialSize = 5)
		{
			var pool = new AsyncGameObjectPool<AbstractVfx<T>>(initialSize, reference.gameObject, Container);
			pool.OnInstantiated += Instantiator;
			_pools.Add(reference.Id, pool);
			await pool.InitPoolAsync();
		}

		private void Instantiator(AbstractVfx<T> instance)
		{
			instance.gameObject.SetActive(false);
			instance.Init(this);
		}

		/// <inheritdoc />
		public void RemovePool(T vfxType)
		{
			FLog.Verbose("Removing VFX " + vfxType.GetType().Name + " from pool");
			_pools.Remove(vfxType);
		}

		/// <inheritdoc />
		public bool HasPool(T vfxType)
		{
			return _pools.ContainsKey(vfxType);
		}

		/// <inheritdoc />
		public void DestroyAllEntities(T vfxId)
		{
			GetPool(vfxId).DestroyAll();
		}

		/// <inheritdoc />
		public void DestroyAllEntities()
		{
			foreach (var pool in _pools.Values)
			{
				pool.DestroyAll();
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			FLog.Info("Disposing VFX Service");
			foreach (var pool in _pools)
			{
				pool.Value.DestroyAll();
			}

			_pools.Clear();

			if (Container != null && Container.gameObject != null)
			{
				UnityEngine.Object.Destroy(Container.gameObject);
			}
		}

		private AsyncGameObjectPool<AbstractVfx<T>> GetPool(T vfxType)
		{
			if (!_pools.TryGetValue(vfxType, out var pool))
			{
				throw new ArgumentException("The pool was not initialized for the vfx type " + vfxType);
			}

			return pool;
		}
	}
}