using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class BattlePassLogicTest : MockedTestFixture<PlayerData>
	{
		private BattlePassLogic _battlePassLogic;

		[SetUp]
		public void Init()
		{
			_battlePassLogic = new BattlePassLogic(GameLogic, DataService);

			SetupConfigs();
			_battlePassLogic.Init();
		}

		[Test]
		public void TestAddBPP()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(5);

			Assert.AreEqual(5, _battlePassLogic.CurrentPoints.Value);

			_battlePassLogic.AddBPP(13);

			Assert.AreEqual(18, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(5000);

			Assert.AreEqual(40, _battlePassLogic.CurrentPoints.Value);
		}

		[Test]
		public void TestRedeemBPPNoLevelUp()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(9);
			var redeemed = _battlePassLogic.RedeemBPP(out var rewards, out var newLevel);

			Assert.AreEqual(0, newLevel);
			Assert.AreEqual(9, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);
			Assert.IsEmpty(rewards);
			Assert.IsFalse(redeemed);
		}

		[Test]
		public void TestRedeemBPPOneLevelUpExact()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(10);
			var redeemed = _battlePassLogic.RedeemBPP(out var rewards, out var newLevel);

			Assert.AreEqual(1, newLevel);
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(1, _battlePassLogic.CurrentLevel.Value);
			Assert.AreEqual(1, rewards.Count);
			Assert.IsTrue(redeemed);
		}

		[Test]
		public void TestRedeemBPPOneLevelUp()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(15);
			var redeemed = _battlePassLogic.RedeemBPP(out var rewards, out var newLevel);

			Assert.AreEqual(1, newLevel);
			Assert.AreEqual(5, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(1, _battlePassLogic.CurrentLevel.Value);
			Assert.AreEqual(1, rewards.Count);
			Assert.IsTrue(redeemed);
		}

		[Test]
		public void TestRedeemBPPThreeLevelUp()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(30);
			var redeemed = _battlePassLogic.RedeemBPP(out var rewards, out var newLevel);

			Assert.AreEqual(3, newLevel);
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(3, _battlePassLogic.CurrentLevel.Value);
			Assert.AreEqual(3, rewards.Count);
			Assert.IsTrue(redeemed);
		}

		[Test]
		public void TestMaxLevel()
		{
			Assert.AreEqual(4, _battlePassLogic.MaxLevel);
		}

		[Test]
		public void TestGetRemainingPoints()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(34);

			Assert.AreEqual(6, _battlePassLogic.GetRemainingPoints());

			_battlePassLogic.RedeemBPP(out _, out _);

			Assert.AreEqual(6, _battlePassLogic.GetRemainingPoints());
		}

		[Test]
		public void TestGetRewardForLevel()
		{
			var reward = _battlePassLogic.GetRewardForLevel(2);

			Assert.AreEqual(1, reward.Id);
			//Assert.AreEqual(GameId.ApoMinigun, reward.Reward.GameId); // TODO BP
		}

		[Test]
		public void TestIsRedeemable()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(9);

			Assert.IsFalse(_battlePassLogic.IsRedeemable(out var nextLevelPoints));
			Assert.AreEqual(10, nextLevelPoints);

			_battlePassLogic.AddBPP(5);

			Assert.IsTrue(_battlePassLogic.IsRedeemable(out var nextLevelPoints2));
			Assert.AreEqual(10, nextLevelPoints2);
		}

		[Test]
		public void TestAddLevels()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(5);

			_battlePassLogic.AddLevels(2, out var rewards, out var newLevel);

			Assert.AreEqual(5, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(2, _battlePassLogic.CurrentLevel.Value);
			Assert.AreEqual(2, newLevel);
			Assert.AreEqual(2, rewards.Count);
		}

		private void SetupConfigs()
		{
			var bpConfig = new BattlePassConfig
			{
				PointsPerLevel = 10,
				Levels = new List<BattlePassConfig.BattlePassLevel>
				{
					new()
					{
						RewardId = 0
					},
					new()
					{
						RewardId = 1
					},
					new()
					{
						RewardId = 2
					},
					new()
					{
						RewardId = 3
					}
				}
			};

			var bpRewardConfigs = new BattlePassRewardConfig[]
			{
				new()
				{
					Id = 0,
					//Reward = new Equipment(GameId.ApoCrossbow) // TODO BP
				},
				new()
				{
					Id = 1,
					//Reward = new Equipment(GameId.ApoMinigun) // TODO BP
				},
				new()
				{
					Id = 2,
					//Reward = new Equipment(GameId.ApoRifle) // TODO BP
				},
				new()
				{
					Id = 3,
					//Reward = new Equipment(GameId.ApoRPG) // TODO BP
				}
			};


			InitConfigData(bpConfig);
			InitConfigData(data => data.Id, bpRewardConfigs);
		}
	}
}