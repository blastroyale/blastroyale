using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using NSubstitute;
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
			var claimableLevels = _battlePassLogic.GetClaimableLevels(out var points, PassType.Free);
			
			Assert.AreEqual(0, claimableLevels.Count);
			Assert.AreEqual(9, points);
		}

		[Test]
		public void TestRedeemBPPOneLevelUpExact()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(10);
			var claimableLevels = _battlePassLogic.GetClaimableLevels(out var points, PassType.Free);
			var rewards = _battlePassLogic.GetRewardConfigs(claimableLevels.ToArray(), PassType.Free);
			_battlePassLogic.SetLevelAndPoints(claimableLevels.Max(), points);
			
			Assert.AreEqual(1, claimableLevels.Max());
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(1, _battlePassLogic.CurrentLevel.Value);
			Assert.AreEqual(1, rewards.Count);
		}

		[Test]
		public void TestRedeemBPPOneLevelUp()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(15);
			var claimableLevels = _battlePassLogic.GetClaimableLevels(out var points, PassType.Free);
			var rewards = _battlePassLogic.GetRewardConfigs(claimableLevels.ToArray(), PassType.Free);
			var newLevel = claimableLevels.Max();
			_battlePassLogic.SetLevelAndPoints(claimableLevels.Max(), points);

			Assert.AreEqual(1, newLevel);
			Assert.AreEqual(5, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(1, _battlePassLogic.CurrentLevel.Value);
			Assert.AreEqual(1, rewards.Count);
		}

		[Test]
		public void TestRedeemBPPThreeLevelUp()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(30);
			var claimableLevels = _battlePassLogic.GetClaimableLevels(out var points, PassType.Free);
			var rewards = _battlePassLogic.GetRewardConfigs(claimableLevels.ToArray(), PassType.Free);
			var newLevel = claimableLevels.Max();
			_battlePassLogic.SetLevelAndPoints(claimableLevels.Max(), points);

			Assert.AreEqual(3, newLevel);
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(3, _battlePassLogic.CurrentLevel.Value);
			Assert.AreEqual(3, rewards.Count);
		}

		[Test]
		public void TestRedeem100BPP()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(100);
			var claimableLevels = _battlePassLogic.GetClaimableLevels(out var points, PassType.Free);
			var rewards = _battlePassLogic.GetRewardConfigs(claimableLevels.ToArray(), PassType.Free);
			var newLevel = claimableLevels.Max();
			_battlePassLogic.SetLevelAndPoints(claimableLevels.Max(), points);

			Assert.AreEqual(_battlePassLogic.MaxLevel, newLevel);
			Assert.AreEqual(4, _battlePassLogic.CurrentLevel.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(4, rewards.Count);
			Assert.AreEqual(_battlePassLogic.GetRemainingPointsOfBp(), 0);
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

			Assert.AreEqual(6, _battlePassLogic.GetRemainingPointsOfBp());

			var claimableLevels = _battlePassLogic.GetClaimableLevels(out var points, PassType.Free);
			_battlePassLogic.SetLevelAndPoints(claimableLevels.Max(), points);

			Assert.AreEqual(6, _battlePassLogic.GetRemainingPointsOfBp());
		}

		[Test]
		public void TestGetRewardForLevel()
		{
			var reward = _battlePassLogic.GetRewardForLevel(2, PassType.Free);

			Assert.AreEqual(1, reward.Id);
			Assert.AreEqual(GameId.ApoMinigun, reward.GameId);
		}

		[Test]
		public void TestIsRedeemable()
		{
			Assert.AreEqual(0, _battlePassLogic.CurrentPoints.Value);
			Assert.AreEqual(0, _battlePassLogic.CurrentLevel.Value);
			var pointsPerLevel = _battlePassLogic.GetRequiredPointsForLevel((int) _battlePassLogic.CurrentLevel.Value);

			_battlePassLogic.AddBPP(9);

			Assert.IsFalse(_battlePassLogic.HasUnclaimedRewards());
			Assert.AreEqual(10, pointsPerLevel);

			_battlePassLogic.AddBPP(5);

			Assert.IsTrue(_battlePassLogic.HasUnclaimedRewards());
			Assert.AreEqual(10, pointsPerLevel);
		}
		
		private void SetupConfigs()
		{
			var bpConfig = new BattlePassConfig
			{
				DefaultPointsPerLevel = 10,
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

			var bpRewardConfigs = new EquipmentRewardConfig[]
			{
				new()
				{
					Id = 0,
					GameId = GameId.ApoCrossbow
				},
				new()
				{
					Id = 1,
					GameId = GameId.ApoMinigun
				},
				new()
				{
					Id = 2,
					GameId = GameId.ApoRifle
				},
				new()
				{
					Id = 3,
					GameId = GameId.ApoRPG
				}
			};


			InitConfigData(bpConfig);
			InitConfigData(data => data.Id, bpRewardConfigs);
		}
	}
}