using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using PlayFab;
using PlayFab.CloudScriptModels;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Sell an Item from the player's current inventory or loadout and award soft currency based on it's sale price.
	/// </summary>
	public struct GameCompleteRewardsCommand : IGameCommand
	{
		public QuantumPlayerMatchData PlayerMatchData;
		public bool DidPlayerQuit;
		
		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var converter = new StringEnumConverter();
			var success = true;
			var rewards = gameLogic.RewardLogic.GiveMatchRewards(PlayerMatchData, DidPlayerQuit);

			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ExecuteCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = nameof(GameCompleteRewardsCommand),
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
			gameLogic.MessageBrokerService.Publish(new GameCompletedRewardsMessage { Rewards = rewards });
		}
	}
}