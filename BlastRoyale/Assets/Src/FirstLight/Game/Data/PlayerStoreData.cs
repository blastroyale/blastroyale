using System;
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;

namespace FirstLight.Game.Data
{
	[Serializable]
	public class PlayerStoreData 
	{
		
		public readonly List<StorePurchaseData> TrackedStorePurchases = new ();
		public readonly List<ProductBundlePurchaseData> ProductsBundlePurchases = new ();

		public override int GetHashCode()
		{
			int hash = 17;
			
			foreach (var e in TrackedStorePurchases)
				hash = hash * 23 + e.GetHashCode();
			foreach (var e in ProductsBundlePurchases)
				hash = hash * 23 + e.GetHashCode();
			
			return hash;
		}
	}
}