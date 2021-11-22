using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
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
	/// Collects a reward on the to the player's trophy road.
	/// </summary>
	public struct CollectTrophyRoadRewardCommand : IGameCommand
	{
		public uint Level;
		
		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.MessageBrokerService.Publish(new TrophyRoadRewardCollectingStartedMessage  { Level = Level });
			
			var converter = new StringEnumConverter();
			var info = gameLogic.TrophyRoadLogic.CollectReward(Level);
			var reward = gameLogic.RewardLogic.GiveReward(info.Reward);
			
			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = nameof(CollectTrophyRoadRewardCommand),
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
			gameLogic.MessageBrokerService.Publish(new TrophyRoadRewardCollectedMessage
			{
				Level = Level,
				Reward = reward
			});
		}
	}
}