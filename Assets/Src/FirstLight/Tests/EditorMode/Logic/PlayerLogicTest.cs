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
		}*/
	}
}