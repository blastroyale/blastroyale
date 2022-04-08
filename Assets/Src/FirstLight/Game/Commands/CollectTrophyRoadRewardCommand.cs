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
			var info = gameLogic.TrophyRoadLogic.CollectReward(Level);
			var reward = gameLogic.RewardLogic.GiveReward(info.Reward);
			gameLogic.MessageBrokerService.Publish(new TrophyRoadRewardCollectedMessage
			{
				Level = Level,
				Reward = reward
			});
		}
	}
}