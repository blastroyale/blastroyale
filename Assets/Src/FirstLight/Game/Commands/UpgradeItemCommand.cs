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
			var converter = new StringEnumConverter();
			var info = gameLogic.EquipmentLogic.GetEquipmentInfo(ItemId);
			
			gameLogic.CurrencyLogic.DeductCurrency(GameId.SC, info.UpgradeCost);
			gameLogic.EquipmentLogic.Upgrade(ItemId);

			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = nameof(UpgradeItemCommand),
					Platform = Application.platform.ToString(),
					Data = new Dictionary<string, string>
					{
						{nameof(PlayerData), JsonConvert.SerializeObject(dataProvider.GetData<PlayerData>(), converter)}
					}
				},
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
			gameLogic.MessageBrokerService.Publish(new ItemUpgradedMessage
			{
				ItemId = ItemId,
				PreviousLevel = info.DataInfo.Data.Level,
				NewLevel = info.DataInfo.Data.Level + 1,
			});
		}
	}
}