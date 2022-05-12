using System.Collections.Generic;
using FirstLight.Game.Configs;
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
		public ResourcePoolConfig PoolConfig;
		public bool ForceRestock;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			ResourcePoolData poolRestocked = gameLogic.CurrencyLogic.RestockResourcePool(PoolId, PoolConfig, ForceRestock);
			gameLogic.MessageBrokerService.Publish(new ResourcePoolRestockedMessage { PoolRestocked = poolRestocked });
		}
	}
}