using System;
using PlayFab;

namespace Backend.Game.Services
{
	/// <summary>
    /// Interface of creating playfab server configuration.
    /// </summary>
    public interface IPlayfabServer
    {
    	/// <summary>
    	/// Creates and returns a playfab server configuration.
    	/// </summary>
    	public PlayFabServerInstanceAPI CreateServer(string playfabId);
    }
    
    /// <summary>
    /// Server settings to connect to PlayFab
    /// </summary>
    public class PlayfabServerSettings : IPlayfabServer
    {
    	private string _secretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process) 
    	                            ?? "***REMOVED***";
    	
    	private string _titleId = Environment.GetEnvironmentVariable("PLAYFAB_TITLE", EnvironmentVariableTarget.Process) 
    	                          ?? "DDD52";
    	
    	public PlayfabServerSettings()
    	{
    		PlayFabSettings.staticSettings.TitleId = TitleId;
    		PlayFabSettings.staticSettings.DeveloperSecretKey = SecretKey;
    	}
    
    	public string SecretKey
    	{
    		get => _secretKey;
    	}
    
    	public string TitleId
    	{
    		get => _titleId;
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
}

