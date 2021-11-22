using System;
using Cinemachine;
using FirstLight.Game.Ids;
using FirstLight.Game.TimelinePlayables;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
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
	public class GameCompleteScreenPresenter : AnimatedUiPresenterData<GameCompleteScreenPresenter.StateData>, INotificationReceiver
	{
		public struct StateData
		{
			public Action ContinueClicked;
		}
		
		[SerializeField] private CinemachineVirtualCamera _playerProxyCamera;
		[SerializeField] protected PlayableDirector _director;
		[SerializeField] private TextMeshProUGUI _winningPlayerText;
		[SerializeField] private TextMeshProUGUI _titleText;
		[SerializeField] private Image _emojiImage;
		[SerializeField] private Sprite _happyEmojiSprite;
		[SerializeField] private Sprite _sickEmojiSprite;
		[SerializeField] private Button _gotoResultsMenuButton;

		private EntityRef _playerWinnerEntity;
		
		private void Awake()
		{
			_gotoResultsMenuButton.onClick.AddListener(OnContinueButtonClicked);
			
			QuantumEvent.Subscribe<EventOnPlayerLeft>(this, OnEventOnPlayerLeft);
		}
		
		public void OnNotify(Playable origin, INotification notification, object context)
		{
			var playVfxMarker = notification as PlayVfxMarker;
			if (playVfxMarker != null)
			{
				var fx = Services.VfxService.Spawn(playVfxMarker._vfxId);
				fx.transform.position = _playerProxyCamera.LookAt.position;
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
			var data = container.PlayersData;
			var playerWinner = data[0];

			
			for(var i = 1; i < data.Length; i++)
			{
				if (data[i].PlayersKilledCount > playerWinner.PlayersKilledCount ||
				    data[i].PlayersKilledCount == playerWinner.PlayersKilledCount && data[i].DeathCount < playerWinner.DeathCount)
				{
					playerWinner = data[i];
				}
			}

			if (game.PlayerIsLocal(playerWinner.Player))
			{
				_emojiImage.sprite = _happyEmojiSprite;
				_titleText.text = ScriptLocalization.General.Victory_;
			}
			else
			{
				_emojiImage.sprite = _sickEmojiSprite;
				_titleText.text = ScriptLocalization.General.DEFEATED;
			}

			var matchData = new QuantumPlayerMatchData(frame, playerWinner);
			_winningPlayerText.text = string.Format(ScriptLocalization.AdventureMenu.PlayerWon, matchData.GetPlayerName());
			
			Services.AudioFxService.PlayClip2D(AudioId.Victory1);
			
			if (Services.EntityViewUpdaterService.TryGetView(playerWinner.Entity, out var entityView))
			{
				var entityViewTransform = entityView.transform;
				_playerProxyCamera.Follow = entityViewTransform;
				_playerProxyCamera.LookAt = entityViewTransform;

				_director.time = 0;
				_director.Play();
			}
			
			_playerWinnerEntity = playerWinner.Entity;
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

