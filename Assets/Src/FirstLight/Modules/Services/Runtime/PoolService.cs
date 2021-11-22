using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This interface allows pooled objects to be notified when it is spawned
	/// </summary>
	public interface IPoolEntitySpawn
	{
		/// <summary>
		/// Invoked when the Entity is spawned
		/// </summary>
		void OnSpawn();
	}
	
	/// <summary>
	/// This interface allows pooled objects to be notified when it is despawned
	/// </summary>
	public interface IPoolEntityDespawn
	{
		/// <summary>
		/// Invoked when the entity is despawned
		/// </summary>
		void OnDespawn();
	}

	/// <summary>
	/// This interface allows to self despawn by maintaining the reference of the pool that created it
	/// </summary>
	public interface IPoolEntityObject<T>
	{
		/// <summary>
		/// Called by the <see cref="IObjectPool{T}"/> to initialize by the given <paramref name="pool"/>
		/// </summary>
		void Init(IObjectPool<T> pool);

		/// <summary>
		/// Despawns this pooled object
		/// </summary>
		bool Despawn();
	}

	/// <summary>
	/// Simple object pool implementation that can handle any type of entity objects
	/// </summary>
	public interface IObjectPool : IDisposable
	{
		/// <summary>
		/// Despawns all active spawned entities and returns them back to the pool to be used again later
		/// This function does not reset the entity. For that, have the entity implement <see cref="IPoolEntityDespawn"/> or do it externally
		/// </summary>
		void DespawnAll();
	}
	
	/// <inheritdoc />
	public interface IObjectPool<T> : IObjectPool
	{
		/// <summary>
		/// Requests the collection of already spawned elements as a read only list
		/// </summary>
		IReadOnlyList<T> SpawnedReadOnly { get; }

		/// <summary>
		/// Checks if there is an entity in the bool that matches the given <paramref name="conditionCheck"/>
		/// </summary>
		bool IsSpawned(Func<T, bool> conditionCheck);
		
		/// <summary>
		/// Spawns and returns an entity of the given type <typeparamref name="T"/>
		/// This function does not initialize the entity. For that, have the entity implement <see cref="IPoolEntitySpawn"/>
		/// or do it externally
		/// This function throws a <exception cref="StackOverflowException" /> if the pool is empty
		/// </summary>
		T Spawn();
		
		/// <summary>
		/// Despawns the entity that is valid with the given <paramref name="entityGetter"/> condition and returns it back to
		/// the pool to be used again later.
		/// If the given <paramref name="onlyFirst"/> is true then will only despawn one entity and not find more entities
		/// that match the given <paramref name="entityGetter"/> condition.
		/// This function does not reset the entity. For that, have the entity implement <see cref="IPoolEntityDespawn"/>
		/// or do it externally.
		/// Returns true if was able to despawn the entity back to the pool successfully, false otherwise
		/// </summary>
		bool Despawn(bool onlyFirst, Func<T, bool> entityGetter);
		
		/// <summary>
		/// Despawns the given <paramref name="entity"/> and returns it back to the pool to be used again later.
		/// This function does not reset the entity. For that, have the entity implement <see cref="IPoolEntityDespawn"/>
		/// or do it externally.
		/// Returns true if was able to despawn the entity back to the pool successfully, false otherwise.
		/// </summary>
		bool Despawn(T entity);

		/// <summary>
		/// Clears the contents out of this pool.
		/// Returns back its pool contents so they can be independently disposed
		/// </summary>
		List<T> Clear();
	}
	
	/// <summary>
	/// This service allows to manage multiple pools of different types.
	/// The service can only a single pool of the same type. 
	/// </summary>
	public interface IPoolService : IDisposable
	{
		/// <summary>
		/// Adds the given <paramref name="pool"/> of <typeparamref name="T"/> to the service
		/// </summary>
		/// <exception cref="ArgumentException">
		/// Thrown if the service already has a pool of the given <typeparamref name="T"/> type
		/// </exception>
		void AddPool<T>(IObjectPool<T> pool);

		/// <summary>
		/// Removes the pool of the given <typeparamref name="T"/>
		/// </summary>
		void RemovePool<T>();

		/// <summary>
		/// Checks if exists a pool of the given type already exists or needs to be added before calling <seealso cref="Spawn{T}"/>
		/// </summary>
		bool HasPool<T>();

		/// <inheritdoc cref="HasPool{T}"/>
		bool HasPool(Type type);

		/// <inheritdoc cref="IObjectPool{T}.IsSpawned"/>
		bool IsSpawned<T>(Func<T, bool> conditionCheck);
		
		/// <inheritdoc cref="IObjectPool{T}.Spawn"/>
		/// <exception cref="ArgumentException">
		/// Thrown if the service does not contains a pool of the given <typeparamref name="T"/> type
		/// </exception>
		T Spawn<T>();
		
		/// <inheritdoc cref="IObjectPool{T}.Despawn"/>
		/// <exception cref="ArgumentException">
		/// Thrown if the service does not contains a pool of the given <typeparamref name="T"/> type
		/// </exception>
		bool Despawn<T>(T entity);

		/// <inheritdoc cref="IObjectPool{T}.DespawnAll"/>
		/// <exception cref="ArgumentException">
		/// Thrown if the service does not contains a pool of the given <typeparamref name="T"/> type
		/// </exception>
		void DespawnAll<T>();
		
		/// <summary>
		/// Clears the contents out of this service.
		/// Returns back all pools so they can be independently disposed
		/// </summary>
		IDictionary<Type, IObjectPool> Clear();
	}

	/// <inheritdoc />
	public abstract class ObjectPoolBase<T> : IObjectPool<T>
	{
		public readonly T SampleEntity;
		
		protected readonly IList<T> SpawnedEntities = new List<T>();
		
		private readonly Stack<T> _stack;
		private readonly Func<T, T> _instantiator;

		/// <inheritdoc />
		public IReadOnlyList<T> SpawnedReadOnly => SpawnedEntities as IReadOnlyList<T>;
		
		protected ObjectPoolBase(uint initSize, T sampleEntity, Func<T, T> instantiator)
		{
			SampleEntity = sampleEntity;
			_instantiator = instantiator;
			_stack = new Stack<T>((int) initSize);

			for (var i = 0; i < initSize; i++)
			{
				var entity = instantiator.Invoke(SampleEntity);
				var poolEntity = entity as IPoolEntityObject<T>;
				
				poolEntity?.Init(this);
				_stack.Push(entity);
			}
		}

		/// <inheritdoc />
		public bool IsSpawned(Func<T,bool> conditionCheck)
		{
			for (var i = 0; i < SpawnedEntities.Count; i++)
			{
				if (conditionCheck(SpawnedEntities[i]))
				{
					return true;
				}
			}

			return false;
		}

		/// <inheritdoc />
		public virtual T Spawn()
		{
			var entity = SpawnEntity();

			CallOnSpawned(entity);

			return entity;
		}

		/// <inheritdoc />
		public bool Despawn(bool onlyFirst, Func<T, bool> entityGetter)
		{
			var despawned = false;
			
			for (var i = 0; i < SpawnedEntities.Count; i++)
			{
				if(!entityGetter(SpawnedEntities[i]))
				{
					continue;
				}
				
				despawned = Despawn(SpawnedEntities[i]);

				if (onlyFirst)
				{
					break;
				}
			}

			return despawned;
		}

		/// <inheritdoc />
		public virtual bool Despawn(T entity)
		{
			if (!SpawnedEntities.Remove(entity) || entity == null || entity.Equals(null))
			{
				return false;
			}
			
			var poolEntity = entity as IPoolEntityDespawn;
			_stack.Push(entity);
			poolEntity?.OnDespawn();

			return true;
		}

		/// <inheritdoc />
		public virtual void DespawnAll()
		{
			for (var i = SpawnedEntities.Count - 1; i > -1; i--)
			{
				Despawn(SpawnedEntities[i]);
			}
		}

		/// <inheritdoc />
		public List<T> Clear()
		{
			var ret = new List<T>(SpawnedEntities);
			
			ret.AddRange(_stack);
			SpawnedEntities.Clear();
			_stack.Clear();

			return ret;
		}

		/// <inheritdoc />
		public virtual void Dispose()
		{
			Clear();
		}

		protected T SpawnEntity()
		{
			var entity = _stack.Count == 0 ? _instantiator.Invoke(SampleEntity) : _stack.Pop();
			
			SpawnedEntities.Add(entity);

			return entity;
		}

		protected void CallOnSpawned(T entity)
		{
			var poolEntity = entity as IPoolEntitySpawn;
			
			poolEntity?.OnSpawn();
		}
	}

	/// <inheritdoc />
	public class ObjectPool<T> : ObjectPoolBase<T>
	{
		public ObjectPool(uint initSize, Func<T> instantiator) : base(initSize, instantiator(), entityRef => instantiator.Invoke())
		{
		}
	}
	
	/// <inheritdoc />
	/// <remarks>
	/// Useful to for pools that use object references to create new instances (ex: GameObjects)
	/// </remarks>
	public class ObjectRefPool<T> : ObjectPoolBase<T>
	{
		public ObjectRefPool(uint initSize, T sampleEntity, Func<T, T> instantiator) : base(initSize, sampleEntity, instantiator)
		{
		}
	}
	/// <inheritdoc />
	/// <remarks>
	/// Useful to for pools that use object references to create new <see cref="GameObject"/>
	/// </remarks>
	public class GameObjectPool : ObjectRefPool<GameObject>
	{
		/// <summary>
		/// If true then when the object is despawned back to the pool will be parented to the same as the sample entity
		/// parent transform
		/// </summary>
		public bool DespawnToSampleParent { get; set; }
		
		public GameObjectPool(uint initSize, GameObject sampleEntity) : base(initSize, sampleEntity, Instantiator)
		{
		}

		/// <inheritdoc />
		public override GameObject Spawn()
		{
			var entity = SpawnEntity();
			
			entity.SetActive(true);
			CallOnSpawned(entity);

			return entity;
		}

		/// <inheritdoc />
		public override bool Despawn(GameObject entity)
		{
			if (!base.Despawn(entity))
			{
				return false;
			}
			
			entity.SetActive(false);

			if (DespawnToSampleParent && SampleEntity != null)
			{
				entity.transform.SetParent(SampleEntity.transform.parent);
			}
				
			return true;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			var content = Clear();

			foreach (var obj in content)
			{
				Object.Destroy(obj);
			}
		}

		/// <summary>
		/// Generic instantiator for <see cref="GameObject"/> pools
		/// </summary>
		public static GameObject Instantiator(GameObject entityRef)
		{
			var instance = Object.Instantiate(entityRef, entityRef.transform.parent, true);

			instance.SetActive(false);

			return instance;
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// Useful to for pools that use object references to create new <see cref="GameObject"/> by their component reference
	/// </remarks>
	public class GameObjectPool<T> : ObjectRefPool<T> where T : Behaviour
	{
		/// <summary>
		/// If true then when the object is despawned back to the pool will be parented to the same as the sample entity
		/// parent transform
		/// </summary>
		public bool DespawnToSampleParent { get; set; }
		
		public GameObjectPool(uint initSize, T sampleEntity) : base(initSize, sampleEntity, Instantiator)
		{
		}
		
		public GameObjectPool(uint initSize, T sampleEntity, Func<T, T> instantiator) : base(initSize, sampleEntity, instantiator)
		{
		}

		/// <inheritdoc />
		public override T Spawn()
		{
			var entity = SpawnEntity();

			entity.gameObject.SetActive(true);
			CallOnSpawned(entity);

			return entity;
		}

		/// <inheritdoc />
		public override bool Despawn(T entity)
		{
			if (base.Despawn(entity))
			{
				entity.gameObject.SetActive(false);
				
				if (DespawnToSampleParent && SampleEntity != null && !SampleEntity.Equals(null))
				{
					entity.transform.SetParent(SampleEntity.transform.parent);
				}

				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			var content = Clear();

			foreach (var obj in content)
			{
				Object.Destroy(obj.gameObject);
			}
		}

		/// <summary>
		/// Generic instantiator for <see cref="GameObject"/> pools
		/// </summary>
		public static T Instantiator(T entityRef)
		{
			var parent = entityRef == null ? null : entityRef.transform.parent;
			var instance = Object.Instantiate(entityRef, parent, true);

			instance.gameObject.SetActive(false);

			return instance;
		}
	}
	
	/// <inheritdoc />
	public class PoolService : IPoolService
	{
		private readonly IDictionary<Type, IObjectPool> _pools = new Dictionary<Type, IObjectPool>();

		/// <inheritdoc />
		public void AddPool<T>(IObjectPool<T> pool)
		{
			_pools.Add(typeof(T), pool);
		}

		/// <inheritdoc />
		public void RemovePool<T>()
		{
			_pools.Remove(typeof(T));
		}

		/// <inheritdoc />
		public bool HasPool<T>()
		{
			return HasPool(typeof(T));
		}

		/// <inheritdoc />
		public bool HasPool(Type type)
		{
			return _pools.ContainsKey(type);
		}

		/// <inheritdoc />
		public bool IsSpawned<T>(Func<T, bool> conditionCheck)
		{
			return GetPool<T>().IsSpawned(conditionCheck);
		}

		/// <inheritdoc />
		public T Spawn<T>()
		{
			return GetPool<T>().Spawn();
		}

		/// <inheritdoc />
		public bool Despawn<T>(T entity)
		{
			return GetPool<T>().Despawn(entity);
		}

		/// <inheritdoc />
		public void DespawnAll<T>()
		{
			GetPool<T>().DespawnAll();
		}

		/// <inheritdoc />
		public IDictionary<Type, IObjectPool> Clear()
		{
			var ret = new Dictionary<Type, IObjectPool>(_pools);

			_pools.Clear();

			return ret;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var pool in _pools)
			{
				pool.Value.Dispose();
			}
			
			_pools.Clear();
		}

		private IObjectPool<T> GetPool<T>()
		{
			if (!_pools.TryGetValue(typeof(T), out var pool))
			{
				throw new ArgumentException("The pool was not initialized for the type " + typeof(T));
			}

			return pool as IObjectPool<T>;
		}
	}
}