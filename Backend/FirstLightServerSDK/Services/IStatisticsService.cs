using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstLightServerSDK.Services
{
	/// <summary>
	/// Tracks statistics of players. Statistics can be used for multiple reasons such as leaderboards, user
	/// segmentations and so on
	/// </summary>
	public interface IStatisticsService
	{
		/// <summary>
		/// Attempts to set up a new statistic. If OnlyDeltas is true, then the statistic should
		/// only receive delta updates, when false, it should always be set to the final number.
		/// </summary>
		void SetupStatistic(string name, bool onlyDeltas);
		
		/// <summary>
		/// Atempts to send user metrics to a given statistic. This is a non-blocking
		/// fire and forget operation.
		/// </summary>
		void UpdateStatistics(string user, params ValueTuple<string, int> [] statistics);

		/// <summary>
		/// Gets the current season of a given statistic
		/// </summary>
		void GetSeason(string name, Action<int> onGetSeason, Action<string> onError);
		
		/// <summary>
		/// Gets season of a metric, async
		/// </summary>
		Task<int> GetSeasonAsync(string name);
	}
}