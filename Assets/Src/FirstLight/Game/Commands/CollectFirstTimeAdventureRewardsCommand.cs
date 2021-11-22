using System.Collections.Generic;
using FirstLight.Game.Data;
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
	/// Collects the rewards when the player completes the given adventure for the first time
	/// </summary>
	public struct CollectFirstTimeAdventureRewardsCommand : IGameCommand
	{
		public int AdventureId;
		
		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.MessageBrokerService.Publish(new AdventureFirstTimeRewardsCollectingStartedMessage { AdventureId = AdventureId });
			gameLogic.AdventureLogic.MarkFirstTimeRewardsCollected(AdventureId);
			
			var converter = new StringEnumConverter();
			var rewards = gameLogic.RewardLogic.CollectFirstTimeAdventureRewards(AdventureId);
			
			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = nameof(CollectFirstTimeAdventureRewardsCommand),
					Platform = Application.platform.ToString(),
					Data = new Dictionary<string, string>
					{
						{nameof(IdData), JsonConvert.SerializeObject(dataProvider.GetData<IdData>(), converter)},
						{nameof(PlayerData), JsonConvert.SerializeObject(dataProvider.GetData<PlayerData>(), converter)}
					}
				},
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
			gameLogic.MessageBrokerService.Publish(new AdventureFirstTimeRewardsCollectedMessage
			{
				AdventureId = AdventureId,
				Rewards = rewards
			});
		}
	}
}