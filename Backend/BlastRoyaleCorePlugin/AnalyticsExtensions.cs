using FirstLight.Server.SDK.Models;
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
				{"generation", equipment.Generation },
				{"rarity", equipment.Rarity },
				{"adjective", equipment.Adjective },
				{"material", equipment.Material },
				{"level", equipment.Level },
			};
		}
	}
}