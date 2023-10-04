using System.Threading.Tasks;
using PlayFab;
using PlayFab.ServerModels;
using FirstLightServerSDK.Services;
using ServerCommon;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Backend.Game.Services
{
	/// <summary>
	/// Implements ...
	/// </summary>
	public class PlayfabProfileService : IServerPlayerProfileService
	{
		private readonly ILogger _log;
		private IErrorService<PlayFabError> _errorService;
		private IPlayfabServer _server;

		public PlayfabProfileService(ILogger log, IErrorService<PlayFabError> errorService, IPlayfabServer server)
		{
			_log = log;
			_errorService = errorService;
			_server = server;
		}

		/// <inheritdoc />
		public async Task UpdatePlayerAvatarURL(string playerId, string url)
		{
			var request = new UpdateAvatarUrlRequest()
			{
				PlayFabId = playerId,
				ImageUrl = url
			};
			
			var response = await PlayFabServerAPI.UpdateAvatarUrlAsync(request);
			_errorService.CheckErrors(response);
		}
	}
}