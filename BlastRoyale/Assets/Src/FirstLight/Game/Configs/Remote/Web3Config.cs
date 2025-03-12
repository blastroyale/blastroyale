using System;
using System.Linq;
using JetBrains.Annotations;
using Quantum;

namespace FirstLight.Game.Configs.Remote
{
	[Serializable]
	public class Web3Config
	{
		public string RPC;
		public int ChainId;
		public CurrencyConfig[] Currencies;
		
		[CanBeNull] public CurrencyConfig FindCurrency(GameId id) => Currencies.FirstOrDefault(c => c.GameId == id);
	}

	[Serializable]
	public class CurrencyConfig
	{
		public GameId GameId;
		public string Contract;
		public string ClaimContract;
		public string ShopContract;
	}
}