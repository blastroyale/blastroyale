using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Collects all the reward on the to the player's current inventory.
	/// </summary>
	public struct LiveopsActionCommand : IGameCommand
	{
		public int ActionIdentifier;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			if (ctx.Logic.LiveopsLogic().HasTriggeredSegmentationAction(ActionIdentifier))
			{
				throw new LogicException($"Action {ActionIdentifier} was already triggered");
			}
			ctx.Logic.LiveopsLogic().MarkTriggeredSegmentationAction(ActionIdentifier);
			return UniTask.CompletedTask;
		}
	}
}