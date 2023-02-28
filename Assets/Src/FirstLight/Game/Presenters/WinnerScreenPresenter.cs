using System;
using Cinemachine;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Timeline;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter shows the match winner.
	/// </summary>
	public class WinnerScreenPresenter : UiToolkitPresenterData<WinnerScreenPresenter.StateData>, INotificationReceiver
	{
		public struct StateData
		{
			public Action ContinueClicked;
		}

		[SerializeField, Required] private CinemachineVirtualCamera _playerProxyCamera;
		[SerializeField, Required] protected PlayableDirector _director;

		private EntityRef _playerWinnerEntity;
		private IMatchServices _matchService;
		private IGameServices _services;

		private VisualElement _winnerBanner;
		private Label _nameLabel;
		private Button _nextButton;

		private QuantumGame _game;
		private QuantumGame _frame;
		private GameContainer _container;

		private bool _isSpectator;

		private void Awake()
		{
			_matchService = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_nameLabel = root.Q<Label>("NameLabel").Required();
			_winnerBanner = root.Q("WinnerBanner");

			root.Q<LocalizedButton>("NextButton").clicked += OnNextClicked;
		}

		protected override void SubscribeToEvents()
		{
			QuantumEvent.Subscribe<EventOnPlayerLeft>(this, OnEventOnPlayerLeft);
		}

		protected override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener<EventOnPlayerLeft>(this);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetupCamera();

			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GeneratePlayersMatchData(frame, out var leader);
			var playerWinner = playerData[leader];

			_playerWinnerEntity = playerWinner.Data.Entity;
			_nameLabel.text = playerWinner.GetPlayerName();
			_winnerBanner.SetDisplay(_services.TutorialService.CurrentRunningTutorial.Value != TutorialSection.FIRST_GUIDE_MATCH);

			PlayTimeline();
		}

		public void OnNotify(Playable origin, INotification notification, object context)
		{
			var playVfxMarker = notification as PlayVfxMarker;

			if (playVfxMarker != null && !_playerProxyCamera.LookAt.IsDestroyed())
			{
				_services.VfxService.Spawn(playVfxMarker.Vfx).transform.position = _playerProxyCamera.LookAt.position;
			}
		}

		private void PlayTimeline()
		{
			if (_matchService.EntityViewUpdaterService.TryGetView(_playerWinnerEntity, out var entityView))
			{
				var entityViewTransform = entityView.transform;

				_playerProxyCamera.Follow = entityViewTransform;
				_playerProxyCamera.LookAt = entityViewTransform;
				_director.time = 0;

				_director.Play();
			}
		}

		private void SetupCamera()
		{
			var cinemachineBrain = Camera.main.gameObject.GetComponent<CinemachineBrain>();

			foreach (var output in _director.playableAsset.outputs)
			{
				if (output.outputTargetType == typeof(CinemachineBrain))
				{
					_director.SetGenericBinding(output.sourceObject, cinemachineBrain);
				}
			}
		}

		private void OnNextClicked()
		{
			Data.ContinueClicked.Invoke();
			_director.Stop();
		}

		private void OnEventOnPlayerLeft(EventOnPlayerLeft callback)
		{
			if (_playerWinnerEntity == EntityRef.None || callback.Entity != _playerWinnerEntity)
			{
				return;
			}

			_director.Stop();
			_playerProxyCamera.gameObject.SetActive(false);
		}
	}
}