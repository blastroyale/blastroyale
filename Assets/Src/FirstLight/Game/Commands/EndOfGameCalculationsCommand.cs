using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using PlayFab;
using PlayFab.CloudScriptModels;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Updates player trophies, restocks resource pools, and gives end-of-match rewards
	/// </summary>
	public struct EndOfGameCalculationsCommand : IGameCommand
	{
		public List<QuantumPlayerMatchData> PlayersMatchData;
		public PlayerRef LocalPlayerRef;
		public bool DidPlayerQuit;
		
		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var currentTrophies = gameLogic.PlayerLogic.Trophies.Value;
			gameLogic.PlayerLogic.UpdateTrophies(PlayersMatchData, LocalPlayerRef);
			var trophiesChange = (int) currentTrophies - (int) gameLogic.PlayerLogic.Trophies.Value;

			gameLogic.CurrencyLogic.RestockResourcePool(GameId.CS);
			gameLogic.CurrencyLogic.RestockResourcePool(GameId.EquipmentXP);

			var player = PlayersMatchData[LocalPlayerRef];
			var rewards = gameLogic.RewardLogic.GiveMatchRewards(player, DidPlayerQuit);
			
			gameLogic.MessageBrokerService.Publish(new GameCompletedRewardsMessage
			{
				Rewards = rewards,
				TrophiesChange = trophiesChange
			});
		}
	}
}