using System;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public class StorePurchaseData
	{

		public string CatalogItemId { get; }
		public bool ShouldDailyReset { get; }
		
		public int AmountPurchased { get; set; }
		public DateTime LastPurchaseTime { get; set; }

		public StorePurchaseData(string catalogItemId, bool shouldDailyReset)
		{
			CatalogItemId = catalogItemId;
			ShouldDailyReset = shouldDailyReset;
			AmountPurchased = 1;
			LastPurchaseTime = DateTime.UtcNow;
			
		}
		
		public bool Equals(StorePurchaseData other)
		{
			return CatalogItemId == other.CatalogItemId;
		}
		
		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + CatalogItemId.GetHashCode();
			return hash;
		}
		
	}
}