using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Modules.Services.Runtime
{
	public class AsyncGameObjectPool<T> where T : Behaviour
	{
		public readonly T SampleEntity;

		public event Action<T> OnInstantiated;
		private readonly IList<T> _createdEntities = new List<T>();

		private readonly int _initSize;
		private readonly int _maxIdle = 999999;
		private readonly Stack<T> _stack;
		private readonly GameObject _prefab;
		private readonly Transform _container;

		public IReadOnlyList<T> SpawnedReadOnly => _createdEntities as IReadOnlyList<T>;

		public AsyncGameObjectPool(int initSize, GameObject prefab, Transform container)
		{
			_container = container;
			_initSize = initSize;
			_prefab = prefab;
			_stack = new Stack<T>(initSize);
		}

		public async UniTask InitPoolAsync()
		{
			if (_initSize == 0) return;
			var op = Object.InstantiateAsync(_prefab, _initSize);
			await op.ToUniTask();
			foreach (var gameObject in op.Result)
			{
				var comp = gameObject.GetComponent<T>();
				InitializePooledObject(gameObject, comp);
				_stack.Push(comp);
			}
		}

		public void InitPool()
		{
			for (var i = 0; i < _initSize; i++)
			{
				var ob = Object.Instantiate(_prefab);
				var comp = ob.GetComponent<T>();
				InitializePooledObject(ob, comp);
				_stack.Push(comp);
			}
		}

		public virtual T Spawn()
		{
			var entity = CreateOrPoolEntity();
			entity.gameObject.SetActive(true);
			CallOnSpawned(entity);
			return entity;
		}

		public virtual async UniTask<T> SpawnAsync()
		{
			var entity = await CreateOrPoolEntityAsync();
			entity.gameObject.SetActive(true);
			CallOnSpawned(entity);
			return entity;
		}

		public virtual bool Despawn(T entity)
		{
			if (!_createdEntities.Contains(entity) || entity == null || entity.Equals(null))
			{
				return false;
			}

			var poolEntity = entity as IPoolEntityDespawn;
			poolEntity?.OnDespawn();
			if (_stack.Count >= _maxIdle)
			{
				_createdEntities.Remove(entity);
				Object.Destroy(entity.gameObject);
				return false;
			}

			_stack.Push(entity);
			entity.gameObject.SetActive(false);
			entity.transform.SetParent(_container);
			return true;
		}

		private List<T> Clear()
		{
			var ret = new List<T>(_createdEntities);
			ret.AddRange(_stack);
			_createdEntities.Clear();
			_stack.Clear();

			return ret;
		}

		public void DestroyAll()
		{
			var content = Clear();

			foreach (var obj in content)
			{
				if (obj)
					Object.Destroy(obj.gameObject);
			}
		}

		private async UniTask<T> CreateOrPoolEntityAsync()
		{
			if (_stack.TryPop(out var pooled))
			{
				return pooled;
			}

			var ent = await CreateObjectAsync();
			InitializePooledObject(ent.gameObject, ent);
			return ent;
		}

		private T CreateOrPoolEntity()
		{
			if (_stack.TryPop(out var pooled))
			{
				return pooled;
			}

			var ent = CreateObject();
			InitializePooledObject(ent.gameObject, ent);
			return ent;
		}

		public T CreateObject()
		{
			var op = Object.Instantiate(_prefab);
			var ent = op.GetComponent<T>();
			return ent;
		}

		public async UniTask<T> CreateObjectAsync()
		{
			var op = Object.InstantiateAsync(_prefab);
			await op.ToUniTask();
			return op.Result[0].GetComponent<T>();
		}

		private void InitializePooledObject(GameObject ob, T cmp)
		{
			if (_container)
				ob.transform.SetParent(_container, true);
			ob.SetActive(false);
			ob.name = _prefab.name + " - " + _createdEntities.Count;
			OnInstantiated?.Invoke(cmp);
			_createdEntities.Add(cmp);
		}

		protected void CallOnSpawned(T entity)
		{
			var poolEntity = entity as IPoolEntitySpawn;

			poolEntity?.OnSpawn();
		}
	}
}