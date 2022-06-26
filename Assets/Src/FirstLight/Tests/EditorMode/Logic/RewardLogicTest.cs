using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;


namespace FirstLight.Tests.EditorMode.Logic
{
	public class RewardLogicTest : BaseTestFixture<PlayerData>
	{
		private RewardLogic _rewardLogic;

		[SetUp]
		public void Init()
		{
			_rewardLogic = new RewardLogic(GameLogic, DataService);
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
			// TODO:
		}
		
		[Test]
		public void GiveMatchRewards_EmptyMatchData_RewardsNothing()
		{
			_rewardLogic.GiveMatchRewards(new QuantumPlayerMatchData(), false);
			
			Assert.AreEqual(0,TestData.UncollectedRewards.Count);
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
	}
}