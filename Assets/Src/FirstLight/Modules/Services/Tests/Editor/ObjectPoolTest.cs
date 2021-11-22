using System.Collections.Generic;
using FirstLight.Services;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	public class ObjectPoolTest
	{
		private ObjectPool<PoolableEntity> _pool;
		private PoolableEntity _poolableEntity;
		private uint initialSize = 5;

		public class PoolableEntity : IPoolEntitySpawn, IPoolEntityDespawn
		{
			public void OnSpawn() {}
			public void OnDespawn() {}
		}

		[SetUp]
		public void Init()
		{
			_pool = new ObjectPool<PoolableEntity>(initialSize, () => Substitute.For<PoolableEntity>());
			_poolableEntity = Substitute.For<PoolableEntity>();
		}

		[Test]
		public void Spawn_Successfully()
		{
			var newEntity = _pool.Spawn();
			
			newEntity.Received().OnSpawn();
			
			Assert.AreNotSame(_poolableEntity, newEntity);
		}

		[Test]
		public void Spawn_EmptyPool_Successfully()
		{
			var pool = new ObjectPool<PoolableEntity>(initialSize, () => Substitute.For<PoolableEntity>());
			
			var newEntity = pool.Spawn();
			
			newEntity.Received().OnSpawn();
			
			Assert.AreNotSame(_poolableEntity, newEntity);
		}

		[Test]
		public void Despawn_Successfully()
		{
			_pool.Despawn(_poolableEntity);
			
			_poolableEntity.Received().OnDespawn();
		}

		[Test]
		public void DespawnAll_Successfully()
		{
			var entities = new List<PoolableEntity>();

			for (int i = 0; i < initialSize; i++)
			{
				entities.Add(_pool.Spawn());
			}
			
			_pool.DespawnAll();

			foreach (var entity in entities)
			{
				entity.Received().OnDespawn();
			}
		}
	}
}