using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;
using PlayFab;
using PlayFab.ClientModels;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Updates the player's name with the given nickname. The new nickname Id will be processed by the Playfab's backend first
	/// </summary>
	public struct UpdatePlayerNicknameCommand : IGameCommand
	{
		public string Nickname;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var request = new UpdateUserTitleDisplayNameRequest { DisplayName = Nickname };
			
			PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnResultCallback, GameCommandService.OnPlayFabError);
			
			void OnResultCallback(UpdateUserTitleDisplayNameResult result)
			{
				gameLogic.PlayerLogic.NicknameId.Value = result.DisplayName;
			}
		}
	}
}