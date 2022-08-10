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
		public bool PlayedRankedMatch;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var trophiesBeforeChange = gameLogic.PlayerLogic.Trophies.Value;
			var trophyChange = 0;
			var rewards = new List<RewardData>();

			// TODO: Remove EnoughLoadoutEquippedToPlay check when server plugin is running
			if (PlayedRankedMatch && gameLogic.EquipmentLogic.EnoughLoadoutEquippedToPlay())
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