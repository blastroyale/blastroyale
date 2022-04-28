using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using PlayFab.CloudScriptModels;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Cleans all the extra timed boxes from the player's inventory that don't fit on the slots anymore
	/// </summary>
	public struct CleanExtraTimedBoxesCommand : IGameCommand
	{
		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.LootBoxLogic.CleanExtraTimedBoxes();
		}
	}
}