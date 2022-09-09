using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Upgrades an Item from the player's current loadout.
	/// </summary>
	public struct UpdatePlayerSkinCommand : IGameCommand
	{
		public GameId SkinId;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.PlayerLogic.ChangePlayerSkin(SkinId);
			gameLogic.MessageBrokerService.Publish(new PlayerSkinUpdatedMessage { SkinId = SkinId });
		}
	}
}