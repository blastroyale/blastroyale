using System;
using FirstLight.Services;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	public class PoolServiceTest
	{
		private PoolService _poolService;
		private IObjectPool<PoolableEntity> _pool;

		public class PoolableEntity : IPoolEntitySpawn, IPoolEntityDespawn
		{
			public void OnSpawn() {}
			public void OnDespawn() {}
		}

		[SetUp]
		public void Init()
		{
			_poolService = new PoolService();
			_pool = Substitute.For<IObjectPool<PoolableEntity>>();
			
			_poolService.AddPool(_pool);
		}

		[Test]
		public void AddPool_Successfully()
		{
			Assert.True(_poolService.HasPool<PoolableEntity>());
		}

		[Test]
		public void AddPool_SameType_ThrowsException()
		{
			Assert.Throws<ArgumentException>(() => _poolService.AddPool(_pool));
		}

		[Test]
		public void Spawn_Successfully()
		{
			var entity = Substitute.For<PoolableEntity>();

			_pool.Spawn().Returns(entity);
			
			Assert.AreEqual(entity,_poolService.Spawn<PoolableEntity>());

			_pool.Received().Spawn();
		}

		[Test]
		public void Spawn_NotAddedPool_ThrowsException()
		{
			_poolService = new PoolService();
			
			Assert.Throws<ArgumentException>(() => _poolService.Spawn<PoolableEntity>());
		}

		[Test]
		public void Despawn_Successfully()
		{
			var entity = Substitute.For<PoolableEntity>();
			
			_poolService.Despawn(entity);
			
			_pool.Received().Despawn(entity);
		}

		[Test]
		public void Despawn_NotAddedPool_ThrowsException()
		{
			var entity = Substitute.For<PoolableEntity>();
			
			_poolService = new PoolService();
			
			Assert.Throws<ArgumentException>(() => _poolService.Despawn(entity));
		}

		[Test]
		public void DespawnAll_Successfully()
		{
			_poolService.DespawnAll<PoolableEntity>();

			_pool.Received().DespawnAll();
		}

		[Test]
		public void RemovePool_Successfully()
		{
			_poolService.RemovePool<PoolableEntity>();
				
			Assert.False(_poolService.HasPool<PoolableEntity>());
		}

		[Test]
		public void RemovePool_NotAdded_DoesNothing()
		{
			_poolService = new PoolService();
			
			Assert.DoesNotThrow(() => _poolService.RemovePool<PoolableEntity>());
		}
	}
}