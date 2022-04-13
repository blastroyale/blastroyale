using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
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
	/// Enhances the given list of items to a brand new item with higher rarity, at a cost
	/// </summary>
	public struct EnhanceItemsCommand : IGameCommand
	{
		public List<UniqueId> EnhanceItems;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			
			var info = gameLogic.EquipmentLogic.GetEnhancementInfo(EnhanceItems);
			gameLogic.CurrencyLogic.DeductCurrency(GameId.SC, info.EnhancementCost);
			var item = gameLogic.EquipmentLogic.Enhance(EnhanceItems);
			gameLogic.MessageBrokerService.Publish(new ItemsEnhancedMessage { ResultItem = item });
		}
	}
}