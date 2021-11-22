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
			var converter = new StringEnumConverter();
			
			// Spend Hard currency
			gameLogic.CurrencyLogic.DeductCurrency(GameId.HC, cost);
			gameLogic.LootBoxLogic.SpeedUp(LootBoxId);

			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = nameof(SpeedUpLootBoxCommand),
					Platform = Application.platform.ToString(),
					Data = new Dictionary<string, string>
					{
						{nameof(PlayerData), JsonConvert.SerializeObject(dataProvider.GetData<PlayerData>(), converter)}
					}
				},
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
			gameLogic.MessageBrokerService.Publish(new LootBoxHurryCompletedMessage { LootBoxId = LootBoxId });
		}
	}
}