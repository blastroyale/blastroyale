using System;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Holder for currency rewards, ex Coins
	/// </summary>
	public class CurrencyReward : IReward
	{
		public GameId GameId => _gameId;
		public uint Amount => _amount;
		public string DisplayName => GameId.GetCurrencyLocalization(_amount);

		private GameId _gameId;
		private uint _amount;

		public CurrencyReward(GameId gameId, uint amount)
		{
			if (!gameId.IsInGroup(GameIdGroup.Currency) && !gameId.IsInGroup(GameIdGroup.Resource))
			{
				throw new Exception($"GameId {gameId} is not a currency!");
			}

			_gameId = gameId;
			_amount = amount;
		}
	}
}