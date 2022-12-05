using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Marks a UniqueID as seen.
	/// </summary>
	public class MarkEquipmentSeenCommand : IGameCommand
	{
		public UniqueId Id;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;


		public void Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.UniqueIdLogic().MarkIdSeen(Id);
		}
	}
}