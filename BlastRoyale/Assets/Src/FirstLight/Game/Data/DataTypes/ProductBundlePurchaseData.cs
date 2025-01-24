using System;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public class ProductBundlePurchaseData
	{

		public string ProductsBundleId { get; }
		
		public bool HasSeenProductsBundleBanner { get; set;  }
		public bool HasPurchasedProductsBundle { get; set; }
		public DateTime HasPurchasedProductsBundleAt { get; set; }
		public DateTime ProductsBundleFirstAppearance { get; set;  }

		public ProductBundlePurchaseData(string productsBundleId)
		{
			ProductsBundleId = productsBundleId;
		}
		
		public bool Equals(StorePurchaseData other)
		{
			return ProductsBundleId == other.CatalogItemId;
		}
		
		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + ProductsBundleId.GetHashCode();
			return hash;
		}
		
	}
}