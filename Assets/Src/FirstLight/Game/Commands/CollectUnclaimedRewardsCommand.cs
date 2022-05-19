using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using PlayFab;
using PlayFab.CloudScriptModels;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Collects all the reward on the to the player's current inventory.
	/// </summary>
	public struct CollectUnclaimedRewardsCommand : IGameCommand
	{
		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.MessageBrokerService.Publish(new UnclaimedRewardsCollectingStartedMessage() {Rewards = gameLogic.RewardLogic.UnclaimedRewards.ToList()});
			var rewards = gameLogic.RewardLogic.ClaimUncollectedRewards();
			gameLogic.MessageBrokerService.Publish(new UnclaimedRewardsCollectedMessage { Rewards = rewards });
		}
	}
}