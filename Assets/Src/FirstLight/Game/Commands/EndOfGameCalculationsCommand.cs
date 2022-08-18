using FirstLight.Game.Logic;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Updates player trophies, restocks resource pools, and gives end-of-match rewards
	/// </summary>
	public struct EndOfGameCalculationsCommand : IQuantumConsensusCommand, IGameCommand
	{
		public string PlayfabToken;
		public List<QuantumPlayerMatchData> PlayersMatchData;
		public QuantumValues QuantumValues;

		public CommandAccessLevel AccessLevel => CommandAccessLevel.Service;
		public CommandExecutionMode CommandExecutionMode => CommandExecutionMode.QuantumConsensus;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var matchData = PlayersMatchData;
			var trophiesBeforeChange = gameLogic.PlayerLogic.Trophies.Value;
			var trophyChange = 0;
			var rewards = new List<RewardData>();
			if (QuantumValues.Ranked && gameLogic.EquipmentLogic.EnoughLoadoutEquippedToPlay())
			{
				trophyChange = gameLogic.PlayerLogic.UpdateTrophies(matchData, QuantumValues.ExecutingPlayer);
				rewards = gameLogic.RewardLogic.GiveMatchRewards(matchData[QuantumValues.ExecutingPlayer], false);
			}

			gameLogic.MessageBrokerService.Publish(new GameCompletedRewardsMessage
			{
				Rewards = rewards,
				TrophiesChange = trophyChange,
				TrophiesBeforeChange = trophiesBeforeChange
			});
		}

		/// <summary>
		/// Note: Due to an processor issue in Photon the below function and this object
		/// data structure has been copied to quantum server.
		/// In case of changes please refer to command consensus in quantum server.
		/// </summary>
		public bool HasConsensus(IQuantumConsensusCommand command)
		{
			if (!(command is EndOfGameCalculationsCommand))
			{
				return false;
			}
			var endCommand = (EndOfGameCalculationsCommand) command;
			var myHashes = PlayersMatchData.Select(d => d.GetHashCode());
			var hisHashes = endCommand.PlayersMatchData.Select(d => d.GetHashCode());
			return myHashes.SequenceEqual(hisHashes);
		}

		public void SetQuantumValues(QuantumValues values)
		{
			QuantumValues = values;
		}

		public string GetSessionToken()
		{
			return PlayfabToken;
		}
	}
}