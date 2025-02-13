using System;
using System.Collections.Generic;
using System.Linq;

namespace FirstLight.Game.Data
{
	[Serializable]
	public class LocalStoreStateData
	{
		public List<LocalStoreCategory> Categories = new ();

		public bool HasUpdateAvailable()
		{
			return Categories.Any(c => c.HasUpdate());
		}
	}
	
	[Serializable]
	public class LocalStoreCategory
	{
		public string CategoryName { get; set; }
		public List<LocalStoreProduct> Products { get; set; } = new ();

		public bool HasUpdate()
		{
			return Products.Any(p => p.IsNewStoreItem);
		}

		public void MarkProductsAsSeen()
		{
			Products.ForEach(p => p.IsNewStoreItem = false);
		}
	}

	[Serializable]
	public class LocalStoreProduct
	{
		public string ProductName { get; set; }
		public bool IsNewStoreItem { get; set; }

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			var other = (LocalStoreProduct)obj;

			return ProductName == other.ProductName;
		}

		public override int GetHashCode()
		{
			return ProductName?.GetHashCode() ?? 0;
		}
	}
}