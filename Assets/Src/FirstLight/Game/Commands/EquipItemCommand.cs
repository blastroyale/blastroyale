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
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Equips an Item to the player's current loadout.
	/// </summary>
	public struct EquipItemCommand : IGameCommand
	{
		public UniqueId ItemId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{

			gameLogic.EquipmentLogic.Equip(ItemId);
			gameLogic.MessageBrokerService.Publish(new ItemEquippedMessage { ItemId = ItemId });
		}
	}
}