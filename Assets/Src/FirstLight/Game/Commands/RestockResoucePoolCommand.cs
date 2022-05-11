using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
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
	/// Upgrades an Item from the player's current loadout.
	/// </summary>
	public struct RestockResourcePoolCommand : IGameCommand
	{
		public GameId PoolId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			ResourcePoolData? poolRestocked = gameLogic.CurrencyLogic.TryRestockResourcePool(PoolId);
			gameLogic.MessageBrokerService.Publish(new ResourcePoolRestockedMessage { PoolRestocked = poolRestocked });
		}
	}
}