using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class PlayerLogicTest : BaseTestFixture<PlayerData>
	{
		/*private PlayerLogic _playerLogic;

		[SetUp]
		public void Init()
		{
			TestData.Level = 1;
			TestData.Xp = 0;
			_playerLogic = new PlayerLogic(GameLogic, DataService);
			
			_playerLogic.Init();
			InitConfigData(x => (int) x.Level,
			               new QuantumPlayerLevelConfig { Level = 1, LevelUpXP = 100},
			                new QuantumPlayerLevelConfig { Level = 2, LevelUpXP = 100});
			                

			InitConfigData(new QuantumGameConfig
			{
				TrophyEloK = 5,
				TrophyEloRange = 50
			});
		}

		[Test]
		public void AddXpCheck()
		{
			const int amount = 10;

			_playerLogic.AddXp(amount);
			
			Assert.AreEqual(amount, TestData.Xp);
			Assert.AreEqual(1, TestData.Level);
		}

		[Test]
		public void AddXp_Overflow_LevelUp()
		{
			const int amount = 100;

			_playerLogic.AddXp(amount);
			
			Assert.AreEqual(0, TestData.Xp);
			Assert.AreEqual(2, TestData.Level);
		}

		[Test]
		public void AddXp_MaxLevelCheck()
		{
			const int amount = 150;
			
			_playerLogic.AddXp(amount);
			
			Assert.AreEqual(0, TestData.Xp);
			Assert.AreEqual(2, TestData.Level);
		}

		[Test]
		public void AddXp_MaxLevelOverflow_ThrowException()
		{
			const int amount = 100;

			_playerLogic.AddXp(amount);

			Assert.Throws<LogicException>(() => _playerLogic.AddXp(amount));
			Assert.AreEqual(0, TestData.Xp);
			Assert.AreEqual(2, TestData.Level);
		}
		

		[Test]
		public void TestTrophyCalculationZero()
		{
			var localPlayerRef = 5;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, 1000),
				CreatePlayer(1, 1000),
				CreatePlayer(2, 1000),
				CreatePlayer(3, 1000),
				CreatePlayer(4, 1000),
				CreatePlayer(5, TestData.Trophies = 0, true),
			};

			_matchLogic.UpdateTrophies(players, localPlayerRef);

			Assert.AreEqual(0, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationLoss()
		{
			var localPlayerRef = 5;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, 100),
				CreatePlayer(1, 100),
				CreatePlayer(2, 100),
				CreatePlayer(3, 100),
				CreatePlayer(4, 100),
				CreatePlayer(5, TestData.Trophies = 100, true),
			};

			_matchLogic.UpdateTrophies(players, localPlayerRef);

			Assert.AreEqual(88, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationMidway()
		{
			var localPlayerRef = 2;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, 120),
				CreatePlayer(1, 120),
				CreatePlayer(2, TestData.Trophies = 120, true),
				CreatePlayer(3, 120),
				CreatePlayer(4, 120),
			};

			_matchLogic.UpdateTrophies(players, localPlayerRef);

			Assert.AreEqual(120, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationWin()
		{
			var localPlayerRef = 0;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, TestData.Trophies = 100, true),
				CreatePlayer(1, 100),
				CreatePlayer(2, 100),
				CreatePlayer(3, 100),
				CreatePlayer(4, 100),
				CreatePlayer(5, 100),
			};

			_matchLogic.UpdateTrophies(players, localPlayerRef);

			Assert.AreEqual(112, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationMegaWin()
		{
			var localPlayerRef = 0;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, TestData.Trophies = 100, true),
				CreatePlayer(1, 300),
				CreatePlayer(2, 300),
				CreatePlayer(3, 300),
				CreatePlayer(4, 300),
				CreatePlayer(5, 300),
			};

			_matchLogic.UpdateTrophies(players, localPlayerRef);

			Assert.AreEqual(125, _matchLogic.Trophies.Value);
		}

		[Test, Description("Checks if the sum of trophies of two players is the same after a match as it was before.")]
		public void TestTrophyConsistency()
		{
			var localPlayerRef1 = 0;
			var players1 = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, TestData.Trophies = 100, true),
				CreatePlayer(1, 100),
			};
			_matchLogic.UpdateTrophies(players1, localPlayerRef1);
			var trophies1 = TestData.Trophies;

			var localPlayerRef2 = 1;
			var players2 = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, 100),
				CreatePlayer(1, TestData.Trophies = 100, true),
			};
			_matchLogic.UpdateTrophies(players2, localPlayerRef2);
			var trophies2 = TestData.Trophies;

			Assert.AreEqual(200, trophies1 + trophies2, $"P1: {trophies1}, P2: {trophies2}");
		}

		private static QuantumPlayerMatchData CreatePlayer(uint rank, uint trophies, bool localPlayer = false)
		{
			return new QuantumPlayerMatchData
			{
				PlayerRank = rank,
				IsLocalPlayer = localPlayer,
				Data = new PlayerMatchData
				{
					PlayerTrophies = trophies
				}
			};
		}
		*/
	}
}