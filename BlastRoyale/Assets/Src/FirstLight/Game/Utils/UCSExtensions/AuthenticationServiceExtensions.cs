using Unity.Services.Authentication;

namespace FirstLight.Game.Utils.UCSExtensions
{
	/// <summary>
	/// Helpers for the UCS Authentication service.
	/// </summary>
	public static class AuthenticationServiceExtensions
	{
		/// <summary>
		/// Returns the local players name without the hashtag and numbers.
		/// </summary>
		public static string PlayerNameTrimmed(this IAuthenticationService authService)
		{
			var name = authService.PlayerName;
			return name.Remove(name.LastIndexOf('#'));
		}
	}
}