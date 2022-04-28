using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using PlayFab.CloudScriptModels;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Speeds up the given loot box id in the player's loot box slots
	/// </summary>
	public struct SpeedUpLootBoxCommand : IGameCommand
	{
		public UniqueId LootBoxId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var info = gameLogic.LootBoxLogic.GetTimedBoxInfo(LootBoxId);
			var cost = info.UnlockCost(gameLogic.TimeService.DateTimeUtcNow);
			// Spend Hard currency
			gameLogic.CurrencyLogic.DeductCurrency(GameId.HC, cost);
			gameLogic.LootBoxLogic.SpeedUp(LootBoxId);
			gameLogic.MessageBrokerService.Publish(new LootBoxHurryCompletedMessage { LootBoxId = LootBoxId });
		}
	}
}