using System;
using Cinemachine;
using FirstLight.Game.Services;
using FirstLight.Game.Timeline;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using Button = UnityEngine.UI.Button;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Game Complete Screen UI by:
	/// - Showing the Game Complete info
	/// - Go to the main menu
	/// </summary>
	public class GameCompleteScreenPresenter : AnimatedUiPresenterData<GameCompleteScreenPresenter.StateData>,
	                                           INotificationReceiver
	{
		public struct StateData
		{
			public Action ContinueClicked;
		}

		[SerializeField, Required] private CinemachineVirtualCamera _playerProxyCamera;
		[SerializeField, Required] protected PlayableDirector _director;
		[SerializeField, Required] private GameObject _winningPlayerRoot;
		[SerializeField, Required] private TextMeshProUGUI _winningPlayerText;
		[SerializeField, Required] private TextMeshProUGUI _titleText;
		[SerializeField, Required] private Image _emojiImage;
		[SerializeField, Required] private Sprite _happyEmojiSprite;
		[SerializeField, Required] private Sprite _sickEmojiSprite;
		[SerializeField, Required] private Button _gotoResultsMenuButton;
		[SerializeField, Required] private GameObject[] _shineAnimObjects;

		private EntityRef _playerWinnerEntity;
		private IMatchServices _matchService;
		private IGameServices _services;

		private void Awake()
		{
			_matchService = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.Resolve<IGameServices>();

			_gotoResultsMenuButton.onClick.AddListener(OnContinueButtonClicked);

			QuantumEvent.Subscribe<EventOnPlayerLeft>(this, OnEventOnPlayerLeft);
		}

		public void OnNotify(Playable origin, INotification notification, object context)
		{
			var playVfxMarker = notification as PlayVfxMarker;

			if (playVfxMarker != null && !_playerProxyCamera.LookAt.IsDestroyed())
			{
				Services.VfxService.Spawn(playVfxMarker.Vfx).transform.position = _playerProxyCamera.LookAt.position;
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			var cinemachineBrain = Camera.main.gameObject.GetComponent<CinemachineBrain>();

			foreach (var output in _director.playableAsset.outputs)
			{
				if (output.outputTargetType == typeof(CinemachineBrain))
				{
					_director.SetGenericBinding(output.sourceObject, cinemachineBrain);
				}
			}

			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out var leader);
			var playerWinner = playerData[leader];

			if (game.PlayerIsLocal(leader))
			{
				_emojiImage.sprite = _happyEmojiSprite;
				_titleText.text = ScriptLocalization.General.Victory_;
			}
			else
			{
				if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
				{
					_titleText.text = ScriptLocalization.AdventureMenu.GameOver;
				}
				else
				{
					var localPlayerData = playerData[game.GetLocalPlayers()[0]];
					var placement = ((int) localPlayerData.PlayerRank).GetOrdinalTranslation();

					_emojiImage.sprite = _sickEmojiSprite;
					_titleText.text = string.Format(ScriptLocalization.General.PlacementMessage,
					                                localPlayerData.PlayerRank + placement);
				}
			}

			if (container.IsGameOver)
			{
				_winningPlayerRoot.gameObject.SetActive(true);
				foreach (var shine in _shineAnimObjects)
				{
					shine.SetActive(true);
				}

				_winningPlayerText.text =
					string.Format(ScriptLocalization.AdventureMenu.PlayerWon, playerWinner.GetPlayerName());
			}
			else
			{
				foreach (var shine in _shineAnimObjects)
				{
					shine.SetActive(false);
				}

				_winningPlayerRoot.gameObject.SetActive(false);
			}
			

			if (_matchService.EntityViewUpdaterService.TryGetView(playerWinner.Data.Entity, out var entityView))
			{
				var entityViewTransform = entityView.transform;

				_playerProxyCamera.Follow = entityViewTransform;
				_playerProxyCamera.LookAt = entityViewTransform;
				_director.time = 0;

				_director.Play();
			}

			_playerWinnerEntity = playerWinner.Data.Entity;
		}

		private void OnContinueButtonClicked()
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