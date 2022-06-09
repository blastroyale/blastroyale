using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Commands
{
	public struct UpdateLoadoutCommand : IGameCommand
	{
		public Dictionary<GameIdGroup, UniqueId> SlotsToUpdate;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.EquipmentLogic.SetLoadout(SlotsToUpdate);
			gameLogic.MessageBrokerService.Publish(new UpdatedLoadoutMessage() { SlotsUpdated = SlotsToUpdate });
		}
	}
}