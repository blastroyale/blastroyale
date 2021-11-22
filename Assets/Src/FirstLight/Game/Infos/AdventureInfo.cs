using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;

namespace FirstLight.Game.Infos
{
	public struct AllAdventuresInfo
	{
		public List<AdventureInfo> Adventures;
		public AdventureInfo NextAdventure;
		public AdventureInfo SelectedAdventure;
		public int[] DifficultiesStartId;

		/// <summary>
		/// Requests <see cref="AdventureInfo"/> representing the first adventure on the given <paramref name="difficulty"/>
		/// </summary>
		public AdventureInfo GetStartIdInfo(AdventureDifficultyLevel difficulty)
		{
			return Adventures[DifficultiesStartId[(int) difficulty]];
		}
	}
	
	public struct AdventureInfo
	{
		public AdventureData AdventureData;
		public AdventureConfig Config;
		public bool IsUnlocked;

		/// <summary>
		/// Requests the completion state of the adventure
		/// </summary>
		public bool IsCompleted => AdventureData.KillCount > 0;
	}

	public enum AdventureDifficultyLevel
	{
		Normal,
		Hard,
		Master,
		TOTAL	// Used for debug purposes to know the total amount of difficulty levels  
	}
}