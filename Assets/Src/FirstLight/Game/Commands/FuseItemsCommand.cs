using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
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
	/// Fuses the given list of items to generate a brand new item with higher rarity, at a cost
	/// </summary>
	public struct FuseCommand : IGameCommand
	{
		public List<UniqueId> FusingItems;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var converter = new StringEnumConverter();
			var info = gameLogic.EquipmentLogic.GetFusionInfo(FusingItems);
			
			gameLogic.CurrencyLogic.DeductCurrency(GameId.SC, info.FusingCost);
			
			var item = gameLogic.EquipmentLogic.Fuse(FusingItems);

			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = nameof(FuseCommand),
					Platform = Application.platform.ToString(),
					Data = new Dictionary<string, string>
					{
						{nameof(IdData), JsonConvert.SerializeObject(dataProvider.GetData<IdData>(), converter)},
						{nameof(RngData), JsonConvert.SerializeObject(dataProvider.GetData<RngData>(), converter)},
						{nameof(PlayerData), JsonConvert.SerializeObject(dataProvider.GetData<PlayerData>(), converter)}
					}
				},
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
			gameLogic.MessageBrokerService.Publish(new ItemsFusedMessage { ResultItem = item });
		}
	}
}