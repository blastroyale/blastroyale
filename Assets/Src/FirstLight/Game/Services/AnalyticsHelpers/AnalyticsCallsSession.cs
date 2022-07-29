using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public class AnalyticsCallsSession : AnalyticsCalls
	{
		public AnalyticsCallsSession(IAnalyticsService analyticsService) : base(analyticsService)
		{
		}

		/// <summary>
		/// Sends the mark of ending a game session
		/// </summary>
		public void SessionEnd(string reason)
		{
			var dic = new Dictionary<string, object> {{"reason", reason}};
			_analyticsService.LogEvent(AnalyticsEvents.SessionEnd, dic);
		}

		public void Heartbeat()
		{
			_analyticsService.LogEvent(AnalyticsEvents.SessionHeartbeat);
		}

		public void GameLoadStart()
		{
			var dic = new Dictionary<string, object> {{"client_version", Application.version}};
			_analyticsService.LogEvent(AnalyticsEvents.GameLoadStart, dic);
		}
	}
}