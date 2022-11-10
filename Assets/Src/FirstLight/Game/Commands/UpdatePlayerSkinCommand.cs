using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
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

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.PlayerLogic().ChangePlayerSkin(SkinId);
			ctx.Services.MessageBrokerService().Publish(new PlayerSkinUpdatedMessage { SkinId = SkinId });
		}
	}
}