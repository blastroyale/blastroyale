using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using NSubstitute;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;


namespace FirstLight.Tests.EditorMode.Logic
{
	public class RewardLogicTest : MockedTestFixture<PlayerData>
	{
		private RewardLogic _rewardLogic;
		private QuantumPlayerMatchData _matchData;

		[SetUp]
		public void Init()
		{
			_rewardLogic = new RewardLogic(GameLogic, DataService);
			
			SetupData();
			_rewardLogic.Init();
		}

		[Test]
		public void CalculateMatchRewardsCheck_Winner()
		{
			// TODO:
		}

		[Test]
		public void CalculateMatchRewardsCheck_Loser()
		{
			// TODO:
		}

		[Test]
		public void GiveMatchRewardsCheck()
		{
			// TODO:
		}

		[Test]
		public void GiveMatchRewards_EmptyPool_RewardsNothing()
		{
			// TODO:
		}

		[Test]
		public void GiveMatchRewards_PlayerQuit_RewardsNothing()
		{
			var rewards = _rewardLogic.GiveMatchRewards(_matchData, true);
			
			Assert.AreEqual(0, rewards.Count);
		}

		[Test]
		public void GiveMatchRewards_NotBattleRoyale_RewardsNothing()
		{
			// TODO:
		}
		
		[Test]
		public void GiveMatchRewards_EmptyMatchData_RewardsNothing()
		{
			// TODO:
		}

		[Test]
		public void ClaimUncollectRewardsCheck()
		{ 
			var testReward = new RewardData { RewardId = GameId.CS, Value = 10 };
			
			TestData.UncollectedRewards.Add(testReward);

			var rewards = _rewardLogic.ClaimUncollectedRewards();

			Assert.Contains(testReward, rewards);
			Assert.AreEqual(1, rewards.Count);
			Assert.AreEqual(testReward, rewards[0]);
			Assert.AreEqual(0,TestData.UncollectedRewards.Count);
		}

		[Test]
		public void CollectRewards_Empty_DoesNothing()
		{
			var rewards = _rewardLogic.ClaimUncollectedRewards();
			
			Assert.AreEqual(0,rewards.Count);
		}

		private void SetupData()
		{
			var resourceInfo = new ResourcePoolInfo { WinnerRewardAmount = 10, CurrentAmount = 10 };

			ResourceLogic.GetResourcePoolInfo(Arg.Any<GameId>()).Returns(resourceInfo);
			InitConfigData(new QuantumMapConfig { Map = (GameId) _matchData.MapId });
			InitConfigData(new MatchRewardConfig { Placement = 1, Rewards = new GameIdUintDictionary { { GameId.CS, 100 }} });
		}
	}
}