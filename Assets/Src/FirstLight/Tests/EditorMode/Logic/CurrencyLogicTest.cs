using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using NSubstitute;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class CurrencyLogicTest : MockedTestFixture<PlayerData>
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
			
			SetCurrencyData(new Pair<GameId, uint>(GameId.CS, 0));
			
			_currencyLogic.AddCurrency(GameId.CS, amount);
			
			Assert.AreEqual(amount, _currencyLogic.GetCurrencyAmount(GameId.CS));
		}

		[Test]
		public void DeductCurrencyCheck()
		{
			const int amount = 100;
			
			SetCurrencyData(new Pair<GameId, uint>(GameId.CS, amount));
			
			_currencyLogic.DeductCurrency(GameId.CS, amount);
			
			Assert.AreEqual(0, _currencyLogic.GetCurrencyAmount(GameId.CS));
		}

		[Test]
		public void DeductCurrency_InvalidAmount_ThrowsException()
		{
			const int amount = 100;
			
			SetCurrencyData(new Pair<GameId, uint>(GameId.CS, amount));
			
			Assert.Throws<LogicException>(() => _currencyLogic.DeductCurrency(GameId.CS, amount * 3));
		}

		[Test]
		public void InvalidCurrencyType_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _currencyLogic.AddCurrency(GameId.Random, 0));
			Assert.Throws<LogicException>(() => _currencyLogic.DeductCurrency(GameId.Random, 0));
			Assert.Throws<LogicException>(() => _currencyLogic.GetCurrencyAmount(GameId.Random));
		}

		private void SetCurrencyData(params Pair<GameId, uint>[] currencies)
		{
			foreach (var pair in currencies)
			{
				TestData.Currencies.Add(pair.Key, pair.Value);
			}
		}
	}
}