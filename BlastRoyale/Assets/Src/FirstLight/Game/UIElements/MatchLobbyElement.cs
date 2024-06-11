using Unity.Services.Lobbies.Models;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class MatchLobbyElement : VisualElement
	{

		private readonly Label _label;
		
		
		private Lobby _lobby;

		public MatchLobbyElement()
		{
			Add(_label = new Label("something"));
		}

		public void SetLobby(Lobby lobby)
		{
			_lobby = lobby;
			_label.text = $"{lobby.Name} - {lobby.LobbyCode} - {lobby.Id} - {lobby.Players.Count}/{lobby.MaxPlayers} - {lobby.HostId}";
		}
	}
}