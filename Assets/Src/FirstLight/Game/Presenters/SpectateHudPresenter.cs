using System;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This is responsible for displaying the screen during spectate mode,
	/// that follows your killer around.
	/// TODO: Once some time is put aside, all the rest of MatchHud elements should be made compaitble with spectator mode,
	/// TODO: and this presenter should only have the spectate buttons.
	/// </summary>
	public class SpectateHudPresenter : UiPresenterData<SpectateHudPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnLeaveClicked;
		}

		[SerializeField, Required] private Button _leaveButton;
		[SerializeField, Required] private Button _nextPlayerButton;
		[SerializeField, Required] private Button _previousPlayerButton;
		[SerializeField, Required] private Button _camera1Button;
		[SerializeField, Required] private Button _camera2Button;
		[SerializeField, Required] private Button _camera3Button;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_nextPlayerButton.onClick.AddListener(OnNextPlayerClicked);
			_previousPlayerButton.onClick.AddListener(OnPreviousPlayerClicked);
			_camera1Button.onClick.AddListener(OnCamera1Clicked);
			_camera2Button.onClick.AddListener(OnCamera2Clicked);
			_camera3Button.onClick.AddListener(OnCamera3Clicked);
		}

		private void OnNextPlayerClicked()
		{
			_services.MessageBrokerService.Publish(new SpectateNextPlayerMessage());
		}

		private void OnPreviousPlayerClicked()
		{
			_services.MessageBrokerService.Publish(new SpectatePreviousPlayerMessage());
		}

		private void OnCamera1Clicked()
		{
			_services.MessageBrokerService.Publish(new SpectateSetCameraMessage {CameraId = 0});
		}

		private void OnCamera2Clicked()
		{
			_services.MessageBrokerService.Publish(new SpectateSetCameraMessage {CameraId = 1});
		}

		private void OnCamera3Clicked()
		{
			_services.MessageBrokerService.Publish(new SpectateSetCameraMessage {CameraId = 2});
		}
	}
}