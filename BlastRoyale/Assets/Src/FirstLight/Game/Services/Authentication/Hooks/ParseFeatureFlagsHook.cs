using Cysharp.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	public class ParseFeatureFlagsHook : IAuthenticationHook
	{
		private IMessageBrokerService _msgBroker;

		public ParseFeatureFlagsHook(IMessageBrokerService msgBroker)
		{
			_msgBroker = msgBroker;
		}

		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false)
		{
			return UniTask.CompletedTask;
		}

		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn = false)
		{
			LoadPlayfabFeatureFlags(result);
			return UniTask.CompletedTask;
		}
		
		public UniTask BeforeLogout()
		{
			return UniTask.CompletedTask;
		}

		public UniTask AfterFetchedState(LoginResult result)
		{
			return UniTask.CompletedTask;
		}

		private void LoadPlayfabFeatureFlags(LoginResult result)
		{
			var titleData = result.InfoResultPayload.TitleData;
			FeatureFlags.ParseFlags(titleData);
			FeatureFlags.ParseLocalFeatureFlags();
			_msgBroker.Publish(new FeatureFlagsReceived()
			{
				TitleData = titleData
			});
		}
	}
}