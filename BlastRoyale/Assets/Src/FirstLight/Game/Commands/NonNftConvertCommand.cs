using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	public class NonNftConvertCommand : IGameCommand
	{
		//non-NFT conversion to Blast Bucks formula link: https://docs.google.com/spreadsheets/d/1LrHGwlNi2tbb7I8xmQVNCKKbc9YgEJjYyA8EFsIFarw/edit#gid=945899350
		private static readonly int[] _rarityBlastBuckConversionReward = { 5, 10, 15, 25, 40, 70, 120, 200, 350, 600 };
		private static readonly int[] _levelBlastBuckConversionReward =
		{
			0, 5, 7, 9, 11, 16, 21, 26, 31,
			36, 43, 50, 57, 64, 71, 81, 91,
			101, 111, 121, 133, 145, 157, 169, 181,
			196, 211, 226, 241, 256, 273, 290, 307,
			324, 341
		};
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;
		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		private int ComputePoints(Equipment e)
		{
			return _rarityBlastBuckConversionReward[System.Math.Clamp((int)e.Rarity, 0, _rarityBlastBuckConversionReward.Length-1)] + 
				_levelBlastBuckConversionReward[System.Math.Clamp((int)e.Level-1, 0, _levelBlastBuckConversionReward.Length-1)];
		}
		
		public UniTask Execute(CommandExecutionContext ctx)
		{
			var logic = ctx.Logic.EquipmentLogic();
			logic.RemoveAllFromLoadout();

			var removed = new List<Equipment>();
			foreach (var i in logic.Inventory.ReadOnlyDictionary!.ToList())
			{
				if (logic.IsNftInfoValid(i.Key)) continue;
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