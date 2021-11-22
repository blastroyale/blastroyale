using System;

namespace Backend.Data
{
	[Serializable]
	public class CatalogItemCustomData
	{
		public string ItemGameId;
		public string RewardGameId;
		public string PriceGameId;
		public uint RewardValue;
		public float PriceValue;
	}
}