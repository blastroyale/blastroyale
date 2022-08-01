using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using PlayFab;

namespace Scripts.Base;

/// <summary>
/// Will setup playfab before script run
/// </summary>
public abstract class PlayfabScript : IScript
{
	public abstract string GetPlayfabTitle();
	public abstract string GetPlayfabSecret();
	public abstract void Execute(ScriptParameters args);

	public void HandleError(PlayFabError? error)
	{
		if (error == null)
			return;
		throw new Exception($"Playfab Error {error.ErrorMessage}:{JsonConvert.SerializeObject(error.ErrorDetails)}");
	}
	
	public PlayfabScript()
	{
		PlayFabSettings.staticSettings.TitleId = GetPlayfabTitle();
		PlayFabSettings.staticSettings.DeveloperSecretKey = GetPlayfabSecret();
		Console.WriteLine($"Using Playfab Title {PlayFabSettings.staticSettings.TitleId}");
	}
}