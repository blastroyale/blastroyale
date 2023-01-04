using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Marks a UniqueID as seen.
	/// </summary>
	public class MarkEquipmentSeenCommand : IGameCommand
	{
		public List<UniqueId> Ids;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
			foreach (var id in Ids)
			{
				ctx.Logic.UniqueIdLogic().MarkIdSeen(id);
			}
		}
	}
}