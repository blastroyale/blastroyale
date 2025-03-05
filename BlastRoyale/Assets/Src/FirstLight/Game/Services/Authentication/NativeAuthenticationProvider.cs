using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using PlayFab.ClientModels;
using VoxelBusters.CoreLibrary;
using VoxelBusters.EssentialKit;

namespace FirstLight.Game.Services.Authentication
{
	public class NativeAuthenticationProvider : INativeGamesLoginProvider
	{
		public async UniTask TryToLinkAccount(LoginResult result)
		{
#if UNITY_ANDROID
			if (result.InfoResultPayload.AccountInfo.GooglePlayGamesInfo != null &&
				result.InfoResultPayload.AccountInfo.GooglePlayGamesInfo.GooglePlayGamesPlayerId != null)
			{
				return;
			}

			var token = await RequestServerSideAccess();
			await AsyncPlayfabAPI.ClientAPI.LinkGooglePlayGamesServicesAccount(new LinkGooglePlayGamesServicesAccountRequest()
			{
				ServerAuthCode = token.ServerCredentials.AndroidProperties.ServerAuthCode
			});

#elif UNITY_IOS
	if (result.InfoResultPayload.AccountInfo.GameCenterInfo != null &&
				result.InfoResultPayload.AccountInfo.GameCenterInfo.GameCenterId != null)
			{
				return;
			}
			var token = await RequestServerSideAccess();
			await AsyncPlayfabAPI.ClientAPI.LinkGameCenterAccount(new LinkGameCenterAccountRequest()
			{
				Salt = Convert.ToBase64String(token.ServerCredentials.IosProperties.Salt),
				Signature = Convert.ToBase64String(token.ServerCredentials.IosProperties.Signature),
				PublicKeyUrl = token.ServerCredentials.IosProperties.PublicKeyUrl,
				Timestamp = token.ServerCredentials.IoresProperties.Timestamp.ToString(),
				GameCenterId = VoxelBusters.EssentialKit.GameServices.LocalPlayer.DeveloperScopeId
			});
#else
			await UniTask.Yield();
#endif
		}

		public UniTask<GameServicesAuthStatusChangeResult> NativeAuthenticate()
		{
			var completionSource = new UniTaskCompletionSource<GameServicesAuthStatusChangeResult>();
			VoxelBusters.EssentialKit.GameServices.Authenticate();
			VoxelBusters.EssentialKit.GameServices.OnAuthStatusChange += ((status, error) =>
			{
				if (error == null)
				{
					completionSource.TrySetResult(status);
				}
				else
				{
					if (status.AuthStatus == LocalPlayerAuthStatus.Authenticating) return;
					completionSource.TrySetException(new Exception(error.ToString()));
				}
			});
			return completionSource.Task;
		}

		public UniTask<GameServicesLoadServerCredentialsResult> RequestServerSideAccess()
		{
			var completionSource = new UniTaskCompletionSource<GameServicesLoadServerCredentialsResult>();
			VoxelBusters.EssentialKit.GameServices.LoadServerCredentials((result, error) =>
			{
				if (error == null)
				{
					completionSource.TrySetResult(result);
				}
				else
				{
					completionSource.TrySetException(new Exception(error.ToString()));
				}
			});

			return completionSource.Task;
		}

		public async UniTask<bool> CanAuthenticate()
		{
			try
			{
				var auth = await NativeAuthenticate();
				if (auth.AuthStatus == LocalPlayerAuthStatus.Authenticated) return true;
			}
			catch (Exception ex)
			{
				FLog.Warn("Failed to natively authenticate ", ex);
			}
			return false;
		}

		public async UniTask<LoginResult> Authenticate()
		{
			var token = await RequestServerSideAccess();
			FLog.Verbose("Games Authenticate token " + ModelSerializer.PrettySerialize(token));
			FLog.Verbose("Games Authenticate User " + VoxelBusters.EssentialKit.GameServices.LocalPlayer.Id);
			FLog.Verbose("Games Authenticate User Json " +
			ModelSerializer.PrettySerialize(VoxelBusters.EssentialKit.GameServices.LocalPlayer));

#if UNITY_ANDROID
			return await AsyncPlayfabAPI.ClientAPI.LoginWithGooglePlayGamesServices(new LoginWithGooglePlayGamesServicesRequest()
			{
				InfoRequestParameters = AuthService.StandardLoginInfoRequestParams,
				ServerAuthCode = token.ServerCredentials.AndroidProperties.ServerAuthCode,
				CreateAccount = true,
			});

#elif UNITY_IOS
	return await AsyncPlayfabAPI.ClientAPI.LoginWithGameCenter(new LoginWithGameCenterRequest()
			{
				InfoRequestParameters = AuthService.StandardLoginInfoRequestParams,
				Salt = Convert.ToBase64String(token.ServerCredentials.IosProperties.Salt),
				Signature = Convert.ToBase64String(token.ServerCredentials.IosProperties.Signature),
				PublicKeyUrl = token.ServerCredentials.IosProperties.PublicKeyUrl,
				Timestamp = token.ServerCredentials.IosProperties.Timestamp.ToString(),
				CreateAccount = true,
				PlayerId = VoxelBusters.EssentialKit.GameServices.LocalPlayer.DeveloperScopeId
			});
#else
			await UniTask.Yield();
			return null;
#endif
		}
	}
}