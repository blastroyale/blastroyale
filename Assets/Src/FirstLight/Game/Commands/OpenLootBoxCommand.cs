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
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Opens the given loot box id in the player's loot box slots
	/// </summary>
	public struct OpenLootBoxCommand : IGameCommand
	{
		public UniqueId LootBoxId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var info = gameLogic.LootBoxLogic.GetLootBoxInfo(LootBoxId);
			var loot = gameLogic.LootBoxLogic.Open(LootBoxId);
			for (var i = 0; i < loot.Count; i++)
			{
				var item = loot[i];
				loot[i] = gameLogic.EquipmentLogic.AddToInventory(item.GameId, item.Data.Rarity, item.Data.Level);
			}
			gameLogic.MessageBrokerService.Publish(new LootBoxOpenedMessage
			{
				LootBoxContent = loot,
				LootBoxInfo = info,
				LootBoxId = LootBoxId
			});
		}
	}
}