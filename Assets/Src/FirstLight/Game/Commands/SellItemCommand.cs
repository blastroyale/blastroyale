using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using PlayFab;
using PlayFab.CloudScriptModels;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Sell an Item from the player's current inventory or loadout and award soft currency based on it's sale price.
	/// </summary>
	public struct SellItemCommand : IGameCommand
	{
		public UniqueId ItemId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var info = gameLogic.EquipmentDataProvider.GetEquipmentInfo(ItemId);
			var saleCost = info.SellCost;
			gameLogic.EquipmentLogic.Sell(ItemId);
			gameLogic.CurrencyLogic.AddCurrency(GameId.SC, saleCost);
			gameLogic.MessageBrokerService.Publish(new ItemSoldMessage { ItemId = ItemId, SellAmount = saleCost});
		}
	}
}