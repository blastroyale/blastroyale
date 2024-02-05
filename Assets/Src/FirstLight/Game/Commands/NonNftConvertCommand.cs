using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	public class NonNftConvertCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;
		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		private int ComputePoints(Equipment e)
		{
			return (int) e.Rarity + 1;
		}
		
		public UniTask Execute(CommandExecutionContext ctx)
		{
			var logic = ctx.Logic.EquipmentLogic();
			foreach (var kp in logic.Loadout.ReadOnlyDictionary.ToList())
			{
				if (!kp.Value.IsValid) continue;
				logic.Unequip(kp.Value);
			}

			var removed = new List<Equipment>();
			foreach (var i in logic.Inventory.ReadOnlyDictionary!.ToList())
			{
				if (logic.TryGetNftInfo(i.Key, out var _)) continue;
				logic.RemoveFromInventory(i.Key);
				removed.Add(i.Value);
			}

			var bbWon = removed.Select(ComputePoints).Sum();
			ctx.Logic.RewardLogic().Reward(new [] { ItemFactory.Currency(GameId.BlastBuck, bbWon) });
			ctx.Services.MessageBrokerService().Publish(new ItemConvertedToBlastBuckMessage()
			{
				Items = removed,
				BlastBucks = bbWon
			});
			return UniTask.CompletedTask;
		}
	}
}