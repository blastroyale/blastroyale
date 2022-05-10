using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles any loss of Internet Connection during the game.
	/// </summary>
	public class NoConnectionScreenPresenter : UiPresenter
	{
		[SerializeField, Required] private Button _reconnectButton;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_reconnectButton.onClick.AddListener(OnReconnectClicked);
		}

		private void OnReconnectClicked()
		{
			//TODO: Make the game restart reconnect.� instead
		}
	}
}