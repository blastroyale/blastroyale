using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;
using UnityEngine.Serialization;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Collects all the purchased items to the player's current inventory.
	/// </summary>
	public struct ClaimPurchasedItem : IGameCommand
	{
		public ItemData PurchasedItemToClaim;
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			var msg = new PurchaseClaimedMessage
			{
				ItemPurchased = ctx.Logic.RewardLogic().ClaimUnclaimedReward(PurchasedItemToClaim),
				SupportingContentCreator = ctx.Logic.ContentCreatorLogic().SupportingCreatorCode.Value 
			};

			ctx.Services.MessageBrokerService().Publish(msg);
			return UniTask.CompletedTask;
		}
	}
}