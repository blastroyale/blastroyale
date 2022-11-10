using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public class AnalyticsEconomy : AnalyticsCalls
	{
		public AnalyticsEconomy(IAnalyticsService analyticsService) : base(analyticsService)
		{
		}
		
		/// <summary>
		/// Logs when the user purchases a product
		/// </summary>
		public void Purchase(Product product, RewardData reward, float price, float netIncomeModifier)
		{
			var data = new Dictionary<string, object>
			{
				{"currency", "USD"},
				{"transaction_id", product.transactionID},
				{"price", price},
				{"dollar_gross", price},
				{"dollar_net", price * netIncomeModifier},
				{"item_id", product.definition.id},
				{"item_name", reward.RewardId.ToString()}
			};

			SingularSDK.InAppPurchase(product, data);
			
			_analyticsService.LogEvent(AnalyticsEvents.Purchase, data);
		}
	}
}
