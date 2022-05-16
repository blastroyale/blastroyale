using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using PlayFab.CloudScriptModels;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Speeds up all the extra timed boxes in the player's inventory that don't fit in the slots anymore
	/// </summary>
	public struct SpeedUpAllExtraTimedBoxesCommand : IGameCommand
	{
		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var cost = gameLogic.LootBoxLogic.GetLootBoxInventoryInfo().GetUnlockExtraBoxesCost(gameLogic.TimeService.DateTimeUtcNow);
			gameLogic.LootBoxLogic.SpeedUpAllExtraTimedBoxes();
		}
	}
}