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
	/// Starts unlocking the given loot box id in the player's loot box slots
	/// </summary>
	public struct StartUnlockingLootBoxCommand : IGameCommand
	{
		public UniqueId LootBoxId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.LootBoxLogic.StartUnlocking(LootBoxId);
			gameLogic.MessageBrokerService.Publish(new LootBoxUnlockingMessage { LootBoxId = LootBoxId });
		}
	}
}