using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Services;
using NSubstitute;
using NUnit.Framework;
using Quantum;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class LootBoxLogicTest : BaseTestFixture<PlayerData>
	{
		/*private const int _lootBucketId = 1;
		private const GameId _loot = GameId.AssaultRifle;
		
		private LootBoxLogic _lootBoxLogic;

		[SetUp]
		public void Init()
		{
			_lootBoxLogic = new LootBoxLogic(GameLogic, DataService);
		}
		
		[TestCase(true)]
		[TestCase(false)]
		public void OpenLootBoxCheck(bool canDropSame)
		{
			const uint lootCount = 1;
			
			SetupLootConfig(lootCount, canDropSame, _loot);

			var loot = _lootBoxLogic.Open(_lootBucketId);

			Assert.AreEqual(lootCount, loot.Count);
			Assert.AreEqual(_loot, loot[0]);
		}
		
		[Test]
		public void OpenLootBox_CanDropSame_Check([NUnit.Framework.Range(1, 5)] int lootCount)
		{
			SetupLootConfig((uint) lootCount, true, _loot);

			var loot = _lootBoxLogic.Open(_lootBucketId);

			Assert.AreEqual(lootCount, loot.Count);
			
			for (var i = 0; i < lootCount; i++)
			{
				Assert.AreEqual(_loot, loot[i]);
			}
		}
		
		[Test]
		public void OpenLootBox_CannotDropSame_Check()
		{
			var items = new[]
			{
				GameId.AssaultRifle,
				GameId.M60,
				GameId.Hammer
			};
			
			SetupLootConfig((uint) items.Length, false, items);

			var loot = _lootBoxLogic.Open(_lootBucketId);

			for (var i = 0; i < items.Length; i++)
			{
				Assert.Contains(items[i], loot);
			}
			
			Assert.AreEqual(items.Length, loot.Count);
		}

		[Test]
		public void OpenLootBox_MoreLootThanItems_ThrowsException()
		{
			SetupLootConfig(2, false, _loot);
			
			Assert.Throws<LogicException>(() => _lootBoxLogic.Open(_lootBucketId));
		}

		private void SetupLootConfig(uint lootCount, bool canDropSame, params GameId[] items)
		{
			var itemList = new List<QuantumPair<uint, GameId>>();

			foreach (var item in items)
			{
				itemList.Add(new QuantumPair<uint, GameId>((uint) Random.Range(1, 10), item));
			}
			
			var config = new QuantumLootBoxConfig
			{
				Id = _lootBucketId, 
				Items = itemList,
				CanDropSameItem = canDropSame,
				ItemsAmount = lootCount
			};
			
			InitConfigData(config);
		}*/
	}
}