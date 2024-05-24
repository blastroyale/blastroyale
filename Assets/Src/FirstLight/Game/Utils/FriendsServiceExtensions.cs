using Unity.Services.Friends.Models;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Extensions to help working with the friends service.
	/// </summary>
	public static class FriendsServiceExtensions
	{
		/// <summary>
		/// Checks if the players availability is online.
		/// </summary>
		public static bool IsOnline(this Presence presence)
		{
			return presence.Availability == Availability.Online;
		}
	}
}