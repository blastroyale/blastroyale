using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Updates the player's weapon and gear loadout.
	/// </summary>
	public struct UpdateLoadoutCommand : IGameCommand
	{
		public IDictionary<GameIdGroup, UniqueId> SlotsToUpdate;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.EquipmentLogic.SetLoadout(SlotsToUpdate);
			
			gameLogic.MessageBrokerService.Publish(new UpdatedLoadoutMessage { SlotsUpdated = SlotsToUpdate });
		}
	}
}