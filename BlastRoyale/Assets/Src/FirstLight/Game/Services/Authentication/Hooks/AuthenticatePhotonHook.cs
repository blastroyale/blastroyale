using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	public class AuthenticatePhotonHook : IAuthenticationHook
	{
		private IInternalGameNetworkService _networkService;

		public AuthenticatePhotonHook(IInternalGameNetworkService networkService)
		{
			_networkService = networkService;
		}

		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false)
		{
			return UniTask.CompletedTask;
		}

		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn = false)
		{
			// We need to disconnect photon to re-authenticate and re-generate an auth token
			// when logging in with a different user
			if (previouslyLoggedIn) _networkService.DisconnectPhoton();

			_networkService.UserId.Value = result.PlayFabId;
			return AuthenticatePhoton();
		}

		public UniTask AfterFetchedState(LoginResult result)
		{
			return UniTask.CompletedTask;
		}

		public UniTask BeforeLogout()
		{
			return UniTask.CompletedTask;
		}

		public async UniTask AuthenticatePhoton()
		{
			var appId = FLEnvironment.Current.PhotonAppIDRealtime;
			var getPhotonToken =
				await AsyncPlayfabAPI.ClientAPI.GetPhotonAuthenticationToken(new GetPhotonAuthenticationTokenRequest {PhotonApplicationId = appId});
			_networkService.QuantumClient.AuthValues.AddAuthParameter("token", getPhotonToken.PhotonCustomAuthenticationToken);
			_networkService.ConnectPhotonServer();
		}
	}
}