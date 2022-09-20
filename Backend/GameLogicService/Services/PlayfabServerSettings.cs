using FirstLight.Server.SDK.Services;
using PlayFab;

namespace Backend.Game.Services
{
	/// <summary>
	///     Interface of creating playfab server configuration.
	/// </summary>
	public interface IPlayfabServer
	{
		/// <summary>
		///     Creates and returns a playfab server configuration.
		/// </summary>
		public PlayFabServerInstanceAPI CreateServer(string playfabId);
	}

	/// <summary>
	///     Server settings to connect to PlayFab
	/// </summary>
	public class PlayfabServerSettings : IPlayfabServer
	{
		public PlayfabServerSettings(IServerConfiguration cfg)
		{
			SecretKey = cfg.PlayfabSecretKey;
			TitleId = cfg.PlayfabTitle;
			PlayFabSettings.staticSettings.TitleId = TitleId;
			PlayFabSettings.staticSettings.DeveloperSecretKey = SecretKey;
		}

		public string SecretKey { get; }

		public string TitleId { get; }

		/// <inheritdoc />
		public PlayFabServerInstanceAPI CreateServer(string playfabId)
		{
			var settings = new PlayFabApiSettings
			{
				TitleId = TitleId,
				DeveloperSecretKey = SecretKey
			};
			var authContext = new PlayFabAuthenticationContext
			{
				PlayFabId = playfabId
			};
			return new PlayFabServerInstanceAPI(settings, authContext);
		}
	}
}

