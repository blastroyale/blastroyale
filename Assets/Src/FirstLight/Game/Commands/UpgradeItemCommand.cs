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
	/// Upgrades an Item in the player's current loadout.
	/// </summary>
	public struct UpgradeItemCommand : IGameCommand
	{
		public UniqueId ItemId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var info = gameLogic.EquipmentLogic.GetEquipmentInfo(ItemId);
			gameLogic.CurrencyLogic.DeductCurrency(GameId.SC, info.UpgradeCost);
			gameLogic.EquipmentLogic.Upgrade(ItemId);
			gameLogic.MessageBrokerService.Publish(new ItemUpgradedMessage
			{
				ItemId = ItemId,
				PreviousLevel = info.DataInfo.Data.Level,
				NewLevel = info.DataInfo.Data.Level + 1,
			});
		}
	}
}