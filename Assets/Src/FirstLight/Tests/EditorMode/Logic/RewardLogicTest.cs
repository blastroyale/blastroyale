using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using NSubstitute;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;


namespace FirstLight.Tests.EditorMode.Logic
{
	public class RewardLogicTest : MockedTestFixture<PlayerData>
	{
		private const int PLACEMENT1_CS_PERCENTAGE = 100; 
		private const int PLACEMENT2_CS_PERCENTAGE = 50; 
		private const int PLACEMENT3_CS_PERCENTAGE = 30;

		private const int PLACEMENT1_BPP = 11;
		private const int PLACEMENT2_BPP = 5;
		private const int PLACEMENT3_BPP = 3;

		private const int RESOURCEINFO_CSS_WINAMOUNT = 100;
		private const int RESOURCEINFO_CSS_STARTAMOUNT = 100;
		private const int RESOURCEINFO_BPP_STARTAMOUNT = 100;

		
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
		public void CalculateMatchRewards_WinningPlacement_GetsCorrectRewards()
		{
			_matchData.PlayerRank = 1;
			FeatureFlags.BATTLE_PASS_ENABLED = true;
			var rewards = _rewardLogic.CalculateMatchRewards(MatchType.Ranked, _matchData, false);
			
			Assert.AreEqual(2, rewards.Count);
			Assert.AreEqual(RESOURCEINFO_CSS_WINAMOUNT * PLACEMENT1_CS_PERCENTAGE / 100, rewards.Find(data => data.RewardId == GameId.CS).Value);
			Assert.AreEqual(PLACEMENT1_BPP, rewards.Find(data => data.RewardId == GameId.BPP).Value);
		}

		[Test]
		public void CalculateMatchRewards_NoWinningPlacement_GetsLastRewards()
		{
			_matchData.PlayerRank = 10;
			FeatureFlags.BATTLE_PASS_ENABLED = true;
			var rewards = _rewardLogic.CalculateMatchRewards(MatchType.Ranked, _matchData, false);
			
			Assert.AreEqual(2, rewards.Count);
			Assert.AreEqual(RESOURCEINFO_CSS_WINAMOUNT * PLACEMENT3_CS_PERCENTAGE / 100, rewards.Find(data => data.RewardId == GameId.CS).Value);
			Assert.AreEqual(PLACEMENT3_BPP, rewards.Find(data => data.RewardId == GameId.BPP).Value);
		}

		[Test]
		public void GiveMatchRewards_WinningPlacement_GetsCorrectRewards()
		{
			_matchData.PlayerRank = 1;
			FeatureFlags.BATTLE_PASS_ENABLED = true;
			var rewards = _rewardLogic.GiveMatchRewards(MatchType.Ranked, _matchData, false);
			
			Assert.AreEqual(2, rewards.Count);
			Assert.AreEqual(RESOURCEINFO_CSS_WINAMOUNT * PLACEMENT1_CS_PERCENTAGE / 100, rewards.Find(data => data.RewardId == GameId.CS).Value);
			Assert.AreEqual(PLACEMENT1_BPP, rewards.Find(data => data.RewardId == GameId.BPP).Value);
		}

		[Test]
		public void GiveMatchRewards_NoWinningPlacement_GetsLastRewards()
		{
			_matchData.PlayerRank = 10;
			FeatureFlags.BATTLE_PASS_ENABLED = true;
			var rewards = _rewardLogic.GiveMatchRewards(MatchType.Ranked, _matchData, false);
			
			Assert.AreEqual(2, rewards.Count);
			Assert.AreEqual(RESOURCEINFO_CSS_WINAMOUNT * PLACEMENT3_CS_PERCENTAGE / 100, rewards.Find(data => data.RewardId == GameId.CS).Value);
			Assert.AreEqual(PLACEMENT3_BPP, rewards.Find(data => data.RewardId == GameId.BPP).Value);
		}

		[Test]
		public void GiveMatchRewards_EmptyPool_RewardsNothing()
		{
			_matchData.PlayerRank = 1;
			FeatureFlags.BATTLE_PASS_ENABLED = true;
			SetupZeroResources();
			var rewards = _rewardLogic.GiveMatchRewards(MatchType.Ranked, _matchData, false);

			
			Assert.AreEqual(0, rewards.Count);
		}

		[Test]
		public void GiveMatchRewards_PlayerQuit_RewardsNothing()
		{
			var rewards = _rewardLogic.GiveMatchRewards(MatchType.Ranked, _matchData, true);
			
			Assert.AreEqual(0, rewards.Count);
		}

		[Test]
		public void GiveMatchRewards_Custom_RewardsNothing()
		{
			_matchData.PlayerRank = 1;
			FeatureFlags.BATTLE_PASS_ENABLED = true;
			var rewards = _rewardLogic.GiveMatchRewards(MatchType.Custom, _matchData, false);
			
			Assert.AreEqual(0, rewards.Count);
		}
		
		[Test]
		public void GiveMatchRewards_Casual_OnlyRewardsBPP()
		{
			_matchData.PlayerRank = 1;
			FeatureFlags.BATTLE_PASS_ENABLED = true;
			var rewards = _rewardLogic.GiveMatchRewards(MatchType.Casual, _matchData, false);
			
			Assert.AreEqual(1, rewards.Count);
			Assert.AreEqual(PLACEMENT1_BPP, rewards.Find(data => data.RewardId == GameId.BPP).Value);
		}
		
		[Test]
		public void GiveMatchRewards_EmptyMatchData_RewardsNothing()
		{
			FeatureFlags.BATTLE_PASS_ENABLED = true;
			var rewards = _rewardLogic.GiveMatchRewards(MatchType.Ranked, new QuantumPlayerMatchData(), false);

			Assert.AreEqual(0, rewards.Count);
		}

		[Test]
		public void ClaimUncollectedRewards_WhenCalled_ReturnsCorrectRewards()
		{ 
			var testReward = new RewardData { RewardId = GameId.CS, Value = RESOURCEINFO_CSS_STARTAMOUNT };
			
			TestData.UncollectedRewards.Add(testReward);

			var rewards = _rewardLogic.ClaimUncollectedRewards();
			
			Assert.AreEqual(1, rewards.Count);
			Assert.AreEqual(testReward, rewards[0]);
		}
		
		[Test]
		public void ClaimUncollectedRewards_WhenCalled_CleansRewards()
		{ 
			var testReward = new RewardData { RewardId = GameId.CS, Value = RESOURCEINFO_CSS_STARTAMOUNT };
			
			TestData.UncollectedRewards.Add(testReward);

			_rewardLogic.ClaimUncollectedRewards();
			
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
			var resourceInfoCS = new ResourcePoolInfo { WinnerRewardAmount = RESOURCEINFO_CSS_WINAMOUNT, CurrentAmount = RESOURCEINFO_CSS_STARTAMOUNT };
			ResourceLogic.GetResourcePoolInfo(GameId.CS).Returns(resourceInfoCS);
			var resourceInfoBPP = new ResourcePoolInfo { CurrentAmount = RESOURCEINFO_BPP_STARTAMOUNT };
			ResourceLogic.GetResourcePoolInfo(GameId.BPP).Returns(resourceInfoBPP);
			
			GameLogic.BattlePassLogic.GetRemainingPoints().Returns<uint>(100);
			
			InitConfigData(new QuantumMapConfig { Map = (GameId) _matchData.MapId });
			
			InitConfigData(config => (int) config.Placement, 
			               new MatchRewardConfig { Placement = 1, Rewards = new GameIdUintDictionary {{GameId.CS, PLACEMENT1_CS_PERCENTAGE}, {GameId.BPP, PLACEMENT1_BPP}}}, 
			               new MatchRewardConfig { Placement = 2, Rewards = new GameIdUintDictionary { { GameId.CS, PLACEMENT2_CS_PERCENTAGE }, {GameId.BPP, PLACEMENT2_BPP}} }, 
			               new MatchRewardConfig { Placement = 3, Rewards = new GameIdUintDictionary { { GameId.CS, PLACEMENT3_CS_PERCENTAGE }, {GameId.BPP, PLACEMENT3_BPP}} });
		}

		private void SetupZeroResources()
		{
			var resourceInfoCS = new ResourcePoolInfo { WinnerRewardAmount = RESOURCEINFO_CSS_WINAMOUNT, CurrentAmount = 0 };
			ResourceLogic.GetResourcePoolInfo(GameId.CS).Returns(resourceInfoCS);
			var resourceInfoBPP = new ResourcePoolInfo { CurrentAmount = 0 };
			ResourceLogic.GetResourcePoolInfo(GameId.BPP).Returns(resourceInfoBPP);
		}
	}
}