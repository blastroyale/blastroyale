using FirstLight.Server.SDK.Models;
using Newtonsoft.Json;
using Quantum;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Extension methods to extract specific analytics data from objects.
	/// </summary>
	public static class AnalyticsExtensions
	{
		/// <summary>
		/// Extracts analytics data from the given Equipment model
		/// </summary>
		public static AnalyticsData ToAnalyticsData(this Equipment equipment)
		{
			return new AnalyticsData()
			{
				{"GameId", equipment.GameId},
				{"edition", equipment.Edition },
				{"faction", equipment.Faction },
				{"rarity", equipment.Rarity },
				{"adjective", equipment.Adjective },
				{"material", equipment.Material },
				{"level", equipment.Level },
				{"groups", JsonConvert.SerializeObject(equipment.GameId.GetGroups())}
			};
		}
	}
}