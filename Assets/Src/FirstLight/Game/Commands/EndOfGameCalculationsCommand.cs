using FirstLight.Game.Logic;
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Quantum;

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
		public bool PlayedMatchmakingGame;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var trophiesBeforeChange = gameLogic.PlayerLogic.Trophies.Value;
			var trophyChange = 0;
			var rewards = new List<RewardData>();
			var gameMode = gameLogic.ConfigsProvider.GetConfig<QuantumMapConfig>(PlayersMatchData[0].MapId).GameMode;

			if (PlayedMatchmakingGame && gameMode == GameMode.BattleRoyale
			                          // TODO: Remove this check when server plugin is running
			                          && gameLogic.EquipmentLogic.EnoughLoadoutEquippedToPlay())
			{
				trophyChange = gameLogic.PlayerLogic.UpdateTrophies(PlayersMatchData, LocalPlayerRef);
				rewards = gameLogic.RewardLogic.GiveMatchRewards(PlayersMatchData[LocalPlayerRef], DidPlayerQuit);
			}

			gameLogic.MessageBrokerService.Publish(new GameCompletedRewardsMessage
			{
				Rewards = rewards,
				TrophiesChange = trophyChange,
				TrophiesBeforeChange = trophiesBeforeChange
			});
		}
	}
}