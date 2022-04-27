using System;
using FirstLight.Game.Services;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using I2.Loc;
using Quantum;
using Button = UnityEngine.UI.Button;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class LoginScreenPresenter : AnimatedUiPresenterData<LoginScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action LoginClicked;
			public Action GoToRegisterClicked;
		}
		
		[SerializeField] private Button _closeButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			_closeButton.onClick.AddListener(Close);
		}
	}
}