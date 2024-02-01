using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;
using Quantum.Prototypes;

namespace FirstLight.Game.Commands.Cheats
{
	/// <summary>
	/// Give Equipment to the player, and equip it!
	/// Requires admin permission on server.
	/// </summary>
	public class GiveAllEquipmentCommand : IGameCommand, IEnvironmentLock
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			var rarity = EquipmentRarity.RarePlus;

			var factions = new EquipmentFaction_Prototype[] { EquipmentFaction.Celestial, EquipmentFaction.Chaos, EquipmentFaction.Dark, EquipmentFaction.Organic, EquipmentFaction.Dimensional, EquipmentFaction.Order, EquipmentFaction.Shadow, };

			var equips = GameIdGroup.Equipment.GetIds().Where(id => id != GameId.Hammer)
				.Select(id => new Equipment(id,
					material: EquipmentMaterial.Golden,
					faction: ctx.Logic.RngLogic().RandomElement(factions),
					adjective: EquipmentAdjective.Divine,
					rarity: rarity,
					level: 1,
					grade: EquipmentGrade.GradeI,
					lastRepairTimestamp: DateTime.UtcNow.Ticks)
				)
				.Select(e =>
				{
					// Let this players test upgrades for us as well
					var max = ctx.Logic.EquipmentLogic().GetMaxLevel(e);
					e.Level = (uint) ctx.Logic.RngLogic().Range(1, max);
					return e;
				});

			foreach (var equipment in equips)
			{
				ctx.Logic.EquipmentLogic().AddToInventory(equipment);
			}


			// Add some coins as well to repair and upgrade
			ctx.Logic.CurrencyLogic().AddCurrency(GameId.COIN, 1_000_000);
			return UniTask.CompletedTask;
		}


		public Enum[] AllowedEnvironments()
		{
			return new Enum[] { Services.Environment.TESTNET, Services.Environment.DEV, };
		}
	}
}