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
				CreatePlayer(1, 1000),
				CreatePlayer(2, 1000),
				CreatePlayer(3, 1000),
				CreatePlayer(4, 1000),
				CreatePlayer(5, 1000),
				CreatePlayer(localPlayerRank = 6, TestData.Trophies = 0, true),
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
				CreatePlayer(1, 100),
				CreatePlayer(2, 100),
				CreatePlayer(3, 100),
				CreatePlayer(4, 100),
				CreatePlayer(6, 100),
				CreatePlayer(localPlayerRank = 5, TestData.Trophies = 100, true),
			};

			_matchLogic.UpdateTrophies(players.ToArray(), localPlayerRank);

			Assert.AreEqual(92, _matchLogic.Trophies.Value);
		}

		[Test]
		public void TestTrophyCalculationMidway()
		{
			uint localPlayerRank;
			var players = new List<QuantumPlayerMatchData>
			{
				CreatePlayer(1, 120),
				CreatePlayer(2, 120),
				CreatePlayer(localPlayerRank = 3, TestData.Trophies = 120, true),
				CreatePlayer(4, 120),
				CreatePlayer(5, 120),
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
				CreatePlayer(localPlayerRank = 1, TestData.Trophies = 100, true),
				CreatePlayer(2, 100),
				CreatePlayer(3, 100),
				CreatePlayer(4, 100),
				CreatePlayer(5, 100),
				CreatePlayer(6, 100),
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
				CreatePlayer(localPlayerRank = 1, TestData.Trophies = 100, true),
				CreatePlayer(2, 300),
				CreatePlayer(3, 300),
				CreatePlayer(4, 300),
				CreatePlayer(5, 300),
				CreatePlayer(6, 300),
			};

			_matchLogic.UpdateTrophies(players.ToArray(), localPlayerRank);

			Assert.AreEqual(125, _matchLogic.Trophies.Value);
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