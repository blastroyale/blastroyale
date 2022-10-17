using FirstLight.Game.Logic;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Updates player trophies, restocks resource pools, and gives end-of-match rewards
	/// </summary>
	public class EndOfGameCalculationsCommand : IQuantumCommand, IGameCommand
	{
		public List<QuantumPlayerMatchData> PlayersMatchData;
		public QuantumValues QuantumValues;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Service;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Quantum;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var matchData = PlayersMatchData;
			var trophiesBeforeChange = gameLogic.PlayerLogic.Trophies.Value;
			var matchType = QuantumValues.MatchType;
			var trophyChange =
				gameLogic.PlayerLogic.UpdateTrophies(matchType, matchData, QuantumValues.ExecutingPlayer);
			var rewards =
				gameLogic.RewardLogic.GiveMatchRewards(matchType, matchData[QuantumValues.ExecutingPlayer], false);

			gameLogic.MessageBrokerService.Publish(new GameCompletedRewardsMessage
			{
				Rewards = rewards,
				TrophiesChange = trophyChange,
				TrophiesBeforeChange = trophiesBeforeChange
			});
		}

		public void FromFrame(Frame frame, QuantumValues quantumValues)
		{
			var gameContainer = frame.GetSingleton<GameContainer>();
			PlayersMatchData = gameContainer.GetPlayersMatchData(frame, out _);
			QuantumValues = quantumValues;
		}
	}
}