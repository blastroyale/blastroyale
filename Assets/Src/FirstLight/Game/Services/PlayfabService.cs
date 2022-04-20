using FirstLight.Game.Logic;
using PlayFab;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services
{

	/// <summary>
	/// This service handles general interaction with playfab that are not needed by the server
	/// </summary>
	public interface IPlayfabService
	{
		/// <summary>
		/// Updates the user nickname in playfab.
		/// </summary>
		void UpdateNickname(string newNickname);
	}
	
	/// <inheritdoc cref="IPlayfabService" />
	public class PlayfabService : IPlayfabService
	{
		private IAppLogic _app;
		
		public PlayfabService(IAppLogic app)
		{
			_app = app;
		}
		
		/// <inheritdoc />
		public void UpdateNickname(string newNickname)
		{
			var request = new UpdateUserTitleDisplayNameRequest { DisplayName = newNickname };
			
			PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnResultCallback, GameCommandService.OnPlayFabError);
			
			void OnResultCallback(UpdateUserTitleDisplayNameResult result)
			{
				_app.NicknameId.Value = result.DisplayName;
			}
		}
	}
}