using System.Collections.Generic;
using System.Globalization;
using FirstLight.Game.Data.DataTypes;
using Quantum;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public class AnalyticsCallsEconomy : AnalyticsCalls
	{
		public AnalyticsCallsEconomy(IAnalyticsService analyticsService) : base(analyticsService)
		{
		}
		
		/// <summary>
		/// Logs when the user purchases a product
		/// </summary>
		public void PurchaseIngameItem(Product product, ItemData reward, string coinId, decimal price)
		{
			var data = new Dictionary<string, object>
			{
				{"purchase_cost_item", coinId},
				{"purchase_cost_amount", price.ToString(CultureInfo.InvariantCulture)},
				{"item_id", product.definition.id},
				{"item_name", reward.Id.ToString()}
			};
			_analyticsService.LogEvent(AnalyticsEvents.Purchase, data);
		}
		
		/// <summary>
		/// Logs when the user purchases a product
		/// </summary>
		public void Purchase(Product product, ItemData reward, decimal price, decimal netIncomeModifier)
		{
			var data = new Dictionary<string, object>
			{
				{"currency", "USD"},
				{"transaction_id", product.transactionID},
				{"price", price},
				{"dollar_gross", price},
				{"dollar_net", price * netIncomeModifier},
				{"item_id", product.definition.id},
				{"item_name", reward.Id.ToString()}
			};

			SingularSDK.InAppPurchase(product, data);
			
			_analyticsService.LogEvent(AnalyticsEvents.Purchase, data);
		}
	}
}
