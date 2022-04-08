using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using PlayFab;
using PlayFab.CloudScriptModels;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Unequips an Item from the player's current loadout.
	/// </summary>
	public struct UnequipItemCommand : IGameCommand
	{
		public UniqueId ItemId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.EquipmentLogic.Unequip(ItemId);
			gameLogic.MessageBrokerService.Publish(new ItemUnequippedMessage{ ItemId = ItemId });
		}
	}
}