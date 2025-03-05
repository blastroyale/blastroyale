using System;
using System.Collections.Generic;


namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public class PlayerSpecialStoreData
	{
		/// <summary> Name of the special store. </summary>
		public string SpecialStoreName;

		/// <summary> Indicates whether the special store is active. </summary>
		public bool IsActive;

		/// <summary> Probability of the special store appearing. </summary>
		public float SpecialStoreChanceToShow;

		/// <summary> List of item IDs available in the special store. </summary>
		public List<string> SpecialStoreItemIDs = new();

		/// <summary> Timestamp of the store's last appearance. </summary>
		public DateTime LastAppearance;
	}
}