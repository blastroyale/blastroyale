using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class MatchLogicTest : BaseTestFixture<PlayerData>
	{
		private MatchLogic _matchLogic;

		[SetUp]
		public void Init()
		{
			_matchLogic = new MatchLogic(GameLogic, DataService);
			_matchLogic.Init();

			InitConfigData(new QuantumGameConfig
			{
				TrophyEloK = 5,
				TrophyEloRange = 50
			});
		}

		[Test]
		public void TestTrophyCalculationZero()
		{
			uint localPlayerRank;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, 1000),
				CreatePlayer(1, 1000),
				CreatePlayer(2, 1000),
				CreatePlayer(3, 1000),
				CreatePlayer(4, 1000),
				CreatePlayer(localPlayerRank = 5, TestData.Trophies = 0, true),
			};

			_matchLogic.UpdateTrophies(players.ToArray(), localPlayerRank);

			Assert.AreEqual(0, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationLoss()
		{
			uint localPlayerRank;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, 100),
				CreatePlayer(1, 100),
				CreatePlayer(2, 100),
				CreatePlayer(3, 100),
				CreatePlayer(4, 100),
				CreatePlayer(localPlayerRank = 5, TestData.Trophies = 100, true),
			};

			_matchLogic.UpdateTrophies(players.ToArray(), localPlayerRank);

			Assert.AreEqual(88, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationMidway()
		{
			uint localPlayerRank;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, 120),
				CreatePlayer(1, 120),
				CreatePlayer(localPlayerRank = 2, TestData.Trophies = 120, true),
				CreatePlayer(3, 120),
				CreatePlayer(4, 120),
			};

			_matchLogic.UpdateTrophies(players.ToArray(), localPlayerRank);

			Assert.AreEqual(120, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationWin()
		{
			uint localPlayerRank;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(localPlayerRank = 0, TestData.Trophies = 100, true),
				CreatePlayer(1, 100),
				CreatePlayer(2, 100),
				CreatePlayer(3, 100),
				CreatePlayer(4, 100),
				CreatePlayer(5, 100),
			};

			_matchLogic.UpdateTrophies(players.ToArray(), localPlayerRank);

			Assert.AreEqual(112, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationMegaWin()
		{
			uint localPlayerRank;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(localPlayerRank = 0, TestData.Trophies = 100, true),
				CreatePlayer(1, 300),
				CreatePlayer(2, 300),
				CreatePlayer(3, 300),
				CreatePlayer(4, 300),
				CreatePlayer(5, 300),
			};

			_matchLogic.UpdateTrophies(players.ToArray(), localPlayerRank);

			Assert.AreEqual(125, _matchLogic.Trophies.Value);
		}

		/// <summary>
		/// Checks if the sum of trophies of two players
		/// is the same after a match as it was before.
		/// </summary>
		[Test]
		public void TestTrophyConsistency()
		{
			uint localPlayerRank1;
			var players1 = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(localPlayerRank1 = 0, TestData.Trophies = 100, true),
				CreatePlayer(1, 100),
			};
			_matchLogic.UpdateTrophies(players1.ToArray(), localPlayerRank1);
			var trophies1 = TestData.Trophies;

			uint localPlayerRank2;
			var players2 = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(0, 100),
				CreatePlayer(localPlayerRank2 = 1, TestData.Trophies = 100, true),
			};
			_matchLogic.UpdateTrophies(players2.ToArray(), localPlayerRank2);
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
	}
}