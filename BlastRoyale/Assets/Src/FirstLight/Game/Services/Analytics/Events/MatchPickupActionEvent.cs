using FirstLight.Game.Utils;
using Unity.Services.Authentication;

namespace FirstLight.Game.Services.Analytics.Events
{
	/// <summary>
	/// Triggered when the player picks up a collectable during the match.
	/// </summary>
	public class MatchPickupActionEvent : FLEvent
	{
		public MatchPickupActionEvent(string matchId, string matchType, string gameModeId, string mutators, string mapId, string itemType) :
			base("match_pickup_action")
		{
			SetParameter(AnalyticsParameters.MATCH_ID, matchId);
			SetParameter(AnalyticsParameters.MATCH_TYPE, matchType);
			SetParameter(AnalyticsParameters.GAME_MODE, gameModeId);
			SetParameter(AnalyticsParameters.MUTATORS, mutators);
			SetParameter(AnalyticsParameters.MAP_ID, mapId);
			SetParameter(AnalyticsParameters.ITEM_TYPE, itemType);
			SetParameter(AnalyticsParameters.USER_IP, NetworkExtensions.GetLocalIPAddress());
		}
	}
}