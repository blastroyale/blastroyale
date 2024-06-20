using Unity.Services.Friends.Models;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helpers for the UCS Friends service.
	/// </summary>
	public static class FriendsServiceExtensions
	{
		/// <summary>
		/// Checks if the availability of a user / presence is online.
		/// </summary>
		public static bool IsOnline(this Presence presence)
		{
			return presence.Availability == Availability.Online;
		}
	}
}