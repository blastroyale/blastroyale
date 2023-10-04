using System.Threading.Tasks;

namespace FirstLightServerSDK.Services
{
	/// <summary>
	/// .
	/// </summary>
	public interface IServerPlayerProfileService
	{
		Task UpdatePlayerAvatarURL(string playerId, string url);
	}
}

