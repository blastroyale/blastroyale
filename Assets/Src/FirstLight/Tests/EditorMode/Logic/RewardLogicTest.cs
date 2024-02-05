using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using NSubstitute;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
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

		private const int PLACEMENT1_Trophies = 20;
		private const int PLACEMENT2_Trophies = 15;
		private const int PLACEMENT3_Trophies = 13;

		private const int BRACKET_Trophies = 500;

		private RewardLogic _rewardLogic;
		private List<QuantumPlayerMatchData> _matchData;
		private int _executingPlayer;

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
			SetPlayerRank(1, 10);

			var rewards = _rewardLogic.CalculateMatchRewards(new RewardSource
			{
				MatchType = MatchType.Matchmaking,
				AllowedRewards = GameConstants.Data.AllowedGameRewards,
				MatchData = _matchData,
				ExecutingPlayer = _executingPlayer,
				DidPlayerQuit = false,
				GamePlayerCount = _matchData.Count
			}, out _);

			Assert.AreEqual(3, rewards.Count);
			Assert.AreEqual(RESOURCEINFO_CSS_WINAMOUNT * PLACEMENT1_CS_PERCENTAGE / 100,
				rewards.Find(data => data.Id == GameId.CS).GetMetadata<CurrencyMetadata>().Amount);
			Assert.AreEqual(PLACEMENT1_BPP, rewards.Find(data => data.Id == GameId.BPP).GetMetadata<CurrencyMetadata>().Amount);
			Assert.AreEqual(PLACEMENT1_Trophies, rewards.Find(data => data.Id == GameId.Trophies).GetMetadata<CurrencyMetadata>().Amount);
		}

		[Test]
		public void CalculateMatchRewards_NoWinningPlacement_GetsLastRewards()
		{
			SetPlayerRank(10, 10);
			var rewards = _rewardLogic.CalculateMatchRewards(new RewardSource
			{
				MatchType = MatchType.Matchmaking,
				MatchData = _matchData,
				ExecutingPlayer = _executingPlayer,
				DidPlayerQuit = false,
				GamePlayerCount = _matchData.Count,
				AllowedRewards = GameConstants.Data.AllowedGameRewards,
			}, out _);

			Assert.AreEqual(3, rewards.Count);
			Assert.AreEqual(RESOURCEINFO_CSS_WINAMOUNT * PLACEMENT3_CS_PERCENTAGE / 100,
				rewards.Find(data => data.Id == GameId.CS).GetMetadata<CurrencyMetadata>().Amount);
			Assert.AreEqual(PLACEMENT3_BPP, rewards.Find(data => data.Id == GameId.BPP).GetMetadata<CurrencyMetadata>().Amount);
			Assert.AreEqual(PLACEMENT3_Trophies, rewards.Find(data => data.Id == GameId.Trophies).GetMetadata<CurrencyMetadata>().Amount);
		}

		[Test]
		public void GiveMatchRewards_WinningPlacement_GetsCorrectRewards()
		{
			SetPlayerRank(1, 10);
			var rewards = _rewardLogic.CalculateMatchRewards(new RewardSource
			{
				MatchType = MatchType.Matchmaking,
				MatchData = _matchData,
				ExecutingPlayer = _executingPlayer,
				DidPlayerQuit = false,
				GamePlayerCount = _matchData.Count,
				AllowedRewards = GameConstants.Data.AllowedGameRewards,
			}, out _);

			Assert.AreEqual(3, rewards.Count);
			Assert.AreEqual(RESOURCEINFO_CSS_WINAMOUNT * PLACEMENT1_CS_PERCENTAGE / 100,
				rewards.Find(data => data.Id == GameId.CS).GetMetadata<CurrencyMetadata>().Amount);
			Assert.AreEqual(PLACEMENT1_BPP, rewards.Find(data => data.Id == GameId.BPP).GetMetadata<CurrencyMetadata>().Amount);
			Assert.AreEqual(PLACEMENT1_Trophies, rewards.Find(data => data.Id == GameId.Trophies).GetMetadata<CurrencyMetadata>().Amount);
		}

		[Test]
		public void GiveMatchRewards_NoWinningPlacement_GetsLastRewards()
		{
			SetPlayerRank(10, 10);
			var rewards = _rewardLogic.CalculateMatchRewards(new RewardSource
			{
				MatchType = MatchType.Matchmaking,
				MatchData = _matchData,
				ExecutingPlayer = _executingPlayer,
				DidPlayerQuit = false,
				GamePlayerCount = _matchData.Count,
				AllowedRewards = GameConstants.Data.AllowedGameRewards,
			}, out _);

			Assert.AreEqual(3, rewards.Count);
			Assert.AreEqual(RESOURCEINFO_CSS_WINAMOUNT * PLACEMENT3_CS_PERCENTAGE / 100,
				rewards.Find(data => data.Id == GameId.CS).GetMetadata<CurrencyMetadata>().Amount);
			Assert.AreEqual(PLACEMENT3_BPP, rewards.Find(data => data.Id == GameId.BPP).GetMetadata<CurrencyMetadata>().Amount);
			Assert.AreEqual(PLACEMENT3_Trophies, rewards.Find(data => data.Id == GameId.Trophies).GetMetadata<CurrencyMetadata>().Amount);
		}

		[Test]
		public void GiveMatchRewards_EmptyPool_RewardsTrophies()
		{
			SetPlayerRank(1, 10);
			SetupZeroResources();
			var rewards = _rewardLogic.CalculateMatchRewards(new RewardSource
			{
				MatchType = MatchType.Matchmaking,
				MatchData = _matchData,
				ExecutingPlayer = _executingPlayer,
				DidPlayerQuit = false,
				GamePlayerCount = _matchData.Count,
				AllowedRewards = GameConstants.Data.AllowedGameRewards,
			}, out _);

			Assert.AreEqual(1, rewards.Count);
		}

		[Test]
		public void GiveMatchRewards_PlayerQuit_RewardsTrophies()
		{
			var rewards = _rewardLogic.CalculateMatchRewards(new RewardSource
			{
				MatchType = MatchType.Matchmaking,
				MatchData = _matchData,
				ExecutingPlayer = _executingPlayer,
				DidPlayerQuit = true,
				GamePlayerCount = _matchData.Count,
				AllowedRewards = GameConstants.Data.AllowedGameRewards,
			}, out _);

			Assert.AreEqual(1, rewards.Count);
		}

		[Test]
		public void GiveMatchRewards_Custom_RewardsNothing()
		{
			SetPlayerRank(1, 10);
			var rewards = _rewardLogic.CalculateMatchRewards(new RewardSource
			{
				MatchType = MatchType.Custom,
				MatchData = _matchData,
				ExecutingPlayer = _executingPlayer,
				DidPlayerQuit = false,
				GamePlayerCount = _matchData.Count,
				AllowedRewards = new List<GameId>(),
			}, out _);

			Assert.AreEqual(0, rewards.Count);
		}

		[Test]
		public void GiveMatchRewards_Casual_OnlyRewardsBPP()
		{
		
			SetPlayerRank(1, 10);
			var rewards = _rewardLogic.CalculateMatchRewards(new RewardSource
			{
				MatchType = MatchType.Matchmaking,
				MatchData = _matchData,
				ExecutingPlayer = _executingPlayer,
				DidPlayerQuit = false,
				GamePlayerCount = _matchData.Count,
				AllowedRewards = new List<GameId>()
				{
					GameId.BPP
				},
			}, out _);

			Assert.AreEqual(1, rewards.Count);
			Assert.AreEqual(PLACEMENT1_BPP, rewards.Find(data => data.Id == GameId.BPP).GetMetadata<CurrencyMetadata>().Amount);
		}

		[Test]
		public void GiveMatchRewards_EmptyMatchData_RewardsNothing()
		{
			Assert.Throws<MatchDataEmptyLogicException>(() =>
			{
				_rewardLogic.CalculateMatchRewards(new RewardSource
				{
					MatchType = MatchType.Matchmaking,
					MatchData = new List<QuantumPlayerMatchData> {new()},
					ExecutingPlayer = 0,
					DidPlayerQuit = false,
					GamePlayerCount = 0
				}, out _);
			});
		}

		[Test]
		public void ClaimUncollectedRewards_WhenCalled_ReturnsCorrectRewards()
		{
			var testReward = ItemFactory.Currency(GameId.CS, RESOURCEINFO_CSS_STARTAMOUNT);

			TestData.UncollectedRewards.Add(testReward);

			var rewards = _rewardLogic.ClaimUnclaimedRewards();

			Assert.AreEqual(1, rewards.Count);
			Assert.AreEqual(testReward, rewards[0]);
		}

		[Test]
		public void ClaimUncollectedRewards_WhenCalled_CleansRewards()
		{
			var testReward = ItemFactory.Currency(GameId.CS, RESOURCEINFO_CSS_STARTAMOUNT);
			TestData.UncollectedRewards.Add(testReward);
			_rewardLogic.ClaimUnclaimedRewards();
			Assert.AreEqual(0, TestData.UncollectedRewards.Count);
		}
		
		[Test]
		public void TestItemDataCompare()
		{
			var i1 = ItemFactory.Collection(GameId.Female01Avatar);
			var i2 = ItemFactory.Collection(GameId.Female01Avatar);

			Assert.AreEqual(i1, i2);
		}
		
		[Test]
		public void TestCurrencySerialization()
		{
			var i1 = ItemFactory.Currency(GameId.COIN, 100);

			var i2 = ModelSerializer.Deserialize<ItemData>(ModelSerializer.Serialize(i1).Value);

			Assert.AreEqual(i1, i2);
		}

		[Test]
		public void CollectRewards_Empty_DoesNothing()
		{
			var rewards = _rewardLogic.ClaimUnclaimedRewards();

			Assert.AreEqual(0, rewards.Count);
		}

		private void SetupData()
		{
			var resourceInfoCS = new ResourcePoolInfo
				{WinnerRewardAmount = RESOURCEINFO_CSS_WINAMOUNT, CurrentAmount = RESOURCEINFO_CSS_STARTAMOUNT};
			ResourceLogic.GetResourcePoolInfo(GameId.CS).Returns(resourceInfoCS);
			var resourceInfoBPP = new ResourcePoolInfo {CurrentAmount = RESOURCEINFO_BPP_STARTAMOUNT};
			ResourceLogic.GetResourcePoolInfo(GameId.BPP).Returns(resourceInfoBPP);

			GameLogic.BattlePassLogic.GetRemainingPointsOfBp().Returns<uint>(100);

			_matchData = new List<QuantumPlayerMatchData> {new()};
			SetPlayerRank(1, 10);

			InitConfigData(new QuantumMapConfig {Map = (GameId) _matchData[_executingPlayer].MapId});

			InitConfigData(new QuantumGameConfig
				{TrophiesPerKill = FP._1_50, TrophyEloK = 4, TrophyEloRange = 400, TrophyMinChange = FP._0_05});

			InitConfigData(new QuantumGameModeConfig() {MaxPlayers = 30, MaxPlayersInTeam = 1});

			InitConfigData(config => config.Placement,
				new MatchRewardConfig
				{
					Placement = 1,
					TeamSize = 1,
					Rewards = new SerializedDictionary<GameId, int>
					{
						{GameId.CS, PLACEMENT1_CS_PERCENTAGE}, {GameId.BPP, PLACEMENT1_BPP}
					}
				},
				new MatchRewardConfig
				{
					Placement = 2,
					TeamSize = 1,
					Rewards = new SerializedDictionary<GameId, int>
					{
						{GameId.CS, PLACEMENT2_CS_PERCENTAGE}, {GameId.BPP, PLACEMENT2_BPP}
					}
				},
				new MatchRewardConfig
				{
					Placement = 3,
					TeamSize = 1,
					Rewards = new SerializedDictionary<GameId, int>
					{
						{GameId.CS, PLACEMENT3_CS_PERCENTAGE}, {GameId.BPP, PLACEMENT3_BPP}
					}
				});
			InitConfigData(config => config.Placement,
			               new TrophyRewardConfig
			               {
				               Placement = 1,
				               TeamSize = 1,
				               BracketReward = new SerializedDictionary<int, int>
				               {
					               {BRACKET_Trophies, PLACEMENT1_Trophies}
				               }
			               },
			               new TrophyRewardConfig
			               {
				               Placement = 2,
				               TeamSize = 1,
				               BracketReward = new SerializedDictionary<int, int>
				               {
					               {BRACKET_Trophies, PLACEMENT2_Trophies}
				               }
			               },
			               new TrophyRewardConfig
			               {
				               Placement = 3,
				               TeamSize = 1,
				               BracketReward = new SerializedDictionary<int, int>
				               {
					               {BRACKET_Trophies, PLACEMENT3_Trophies}
				               }
			               });
		}

		private void SetupZeroResources()
		{
			var resourceInfoCS = new ResourcePoolInfo
				{WinnerRewardAmount = RESOURCEINFO_CSS_WINAMOUNT, CurrentAmount = 0};
			ResourceLogic.GetResourcePoolInfo(GameId.CS).Returns(resourceInfoCS);
			var resourceInfoBPP = new ResourcePoolInfo {CurrentAmount = 0};
			ResourceLogic.GetResourcePoolInfo(GameId.BPP).Returns(resourceInfoBPP);
		}

		private void SetPlayerRank(int rank, int totalPlayers, byte collectedNfts = 1)
		{
			Debug.Assert(totalPlayers >= rank);
			Debug.Assert(rank >= 1);
			_matchData.Clear();
			for (int i = 1; i <= totalPlayers; i++)
			{
				_matchData.Add(new QuantumPlayerMatchData
				{
					PlayerRank = (uint) i,
					Data = new PlayerMatchData()
				});
				_executingPlayer = rank - 1;
			}
		}
	}
}