using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands.Cheats
{
	/// <summary>
	/// Give Equipment to the player, and equip it!
	/// Requires admin permission on server.
	/// </summary>
	public class GiveEquipmentCommand : IGameCommand
	{
		public List<Equipment> EquipmentToGive;
		public bool Equip;
		public bool CheckDuplicates;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Admin;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			var equips = ctx.Data.GetData<EquipmentData>().Inventory;
			List<Equipment> toAdd = new(EquipmentToGive);

			if (CheckDuplicates)
			{
				// Check for duplicates
				foreach (var equipment in toAdd.ToList())
				{
					var equal = equips.Where(keyValuePair => IsEqual(equipment, keyValuePair.Value)).ToList();
					if (equal.Count >= 1)
					{
						toAdd.Remove(equipment);
						if (Equip)
						{
							ctx.Logic.EquipmentLogic().Equip(equal.First().Key);
						}
					}
				}
			}

			foreach (var equipment in toAdd)
			{
				var id = ctx.Logic.EquipmentLogic().AddToInventory(equipment);
				if (Equip)
				{
					ctx.Logic.EquipmentLogic().Equip(id);
				}
			}
			return UniTask.CompletedTask;
		}

		private bool IsEqual(Equipment eq, Equipment eq2)
		{
			return eq.Adjective == eq2.Adjective &&
				   eq.GameId == eq2.GameId &&
				   eq.Rarity == eq2.Rarity &&
				   eq.Faction == eq2.Faction &&
				   eq.Grade == eq2.Grade;
		}
	}
}