using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class CurrencyLogicTest : BaseTestFixture<PlayerData>
	{
		private CurrencyLogic _currencyLogic;

		[SetUp]
		public void Init()
		{
			_currencyLogic = new CurrencyLogic(GameLogic, DataService);
			
			_currencyLogic.Init();
		}

		[Test]
		public void AddCurrencyCheck()
		{
			const int amount = 100;
			
			SetData(new Pair<GameId, uint>(GameId.SC, 0));
			
			_currencyLogic.AddCurrency(GameId.SC, amount);
			
			Assert.AreEqual(amount, _currencyLogic.GetCurrencyAmount(GameId.SC));
		}

		[Test]
		public void DeductCurrencyCheck()
		{
			const int amount = 100;
			
			SetData(new Pair<GameId, uint>(GameId.SC, amount));
			
			_currencyLogic.DeductCurrency(GameId.SC, amount);
			
			Assert.AreEqual(0, _currencyLogic.GetCurrencyAmount(GameId.SC));
		}

		[Test]
		public void DeductCurrency_InvalidAmount_ThrowsException()
		{
			const int amount = 100;
			
			SetData(new Pair<GameId, uint>(GameId.SC, amount));
			
			Assert.Throws<LogicException>(() => _currencyLogic.DeductCurrency(GameId.SC, amount * 3));
		}

		[Test]
		public void InvalidCurrencyType_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _currencyLogic.AddCurrency(GameId.Random, 0));
			Assert.Throws<LogicException>(() => _currencyLogic.DeductCurrency(GameId.Random, 0));
			Assert.Throws<LogicException>(() => _currencyLogic.GetCurrencyAmount(GameId.Random));
		}

		private void SetData(params Pair<GameId, uint>[] currencies)
		{
			foreach (var pair in currencies)
			{
				TestData.Currencies.Add(pair.Key, pair.Value);
			}
		}
	}
}