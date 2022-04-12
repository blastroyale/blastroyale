using System;
using PlayFab;

namespace Backend.Game;

/// <summary>
/// Interface of creating playfab server configuration.
/// </summary>
public interface IPlayfabServer
{
	/// <summary>
	/// Creates and returns a playfab server configuration.
	/// </summary>
	/// <param name="playfabId"></param>
	/// <returns>A PlayFabServerInstanceAPI instance</returns>
	public PlayFabServerInstanceAPI CreateServer(string playfabId);
}

/// <summary>
/// Server settings to connect to PlayFab
/// </summary>
public class PlayfabServerSettings : IPlayfabServer
{
	private string _secretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY") ?? "***REMOVED***";
	private string _titleId = Environment.GetEnvironmentVariable("PLAYFAB_TITLE") ?? "DDD52";
	
	public PlayfabServerSettings()
	{
		PlayFabSettings.staticSettings.TitleId = TitleId;
		PlayFabSettings.staticSettings.DeveloperSecretKey = SecretKey;
	}

	public string SecretKey
	{
		get => _secretKey;
		set => _secretKey = value;
	}

	public string TitleId
	{
		get => _titleId;
		set => _titleId = value;
	}

	/// <inheritdoc />
	public PlayFabServerInstanceAPI CreateServer(string playfabId)
	{
		var settings = new PlayFabApiSettings()
		{
			TitleId = _titleId,
			DeveloperSecretKey = _secretKey
		};
		var authContext = new PlayFabAuthenticationContext()
		{
			PlayFabId = playfabId
		};
		return new PlayFabServerInstanceAPI(settings, authContext);
	}
}