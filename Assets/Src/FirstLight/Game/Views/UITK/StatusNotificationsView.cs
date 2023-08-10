using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles all status notifications on the match HUD screen.
	/// </summary>
	public class StatusNotificationsView : UIView
	{
		private const float LOW_HP_BLINK_SPEED = 1f;

		private IMatchServices _matchServices;

		private Label _blasted1PlayerName;
		private Label _blasted2PlayerName;
		private Label _blasted3PlayerName;
		private Label _blastedBeastPlayerName;
		private VisualElement _lowHP;

		private PlayableDirector _blasted1Director;
		private PlayableDirector _blasted2Director;
		private PlayableDirector _blasted3Director;
		private PlayableDirector _blastedBeastDirector;

		private readonly Queue<(string, uint)> _killedPlayersQueue = new();

		private int _lowHPThreshold;
		private IVisualElementScheduledItem _lowHPAnimation;
		private float _lowHPAnimationStartTime;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_matchServices = MainInstaller.ResolveMatchServices();

			var blasted1Notification = element.Q("BlastedNotification").Required();
			_blasted1PlayerName = blasted1Notification.Q<Label>("PlayerNameLabel").Required();

			var blasted2Notification = element.Q("DoubleBlastNotification").Required();
			_blasted2PlayerName = blasted2Notification.Q<Label>("PlayerNameLabel").Required();

			var blasted3Notification = element.Q("TripleBlastNotification").Required();
			_blasted3PlayerName = blasted3Notification.Q<Label>("PlayerNameLabel").Required();

			var blastedBeastNotification = element.Q("BeastBlastNotification").Required();
			_blastedBeastPlayerName = blastedBeastNotification.Q<Label>("PlayerNameLabel").Required();

			_lowHP = element.Q("LowHP").Required();

			blasted1Notification.SetDisplay(false);
			blasted2Notification.SetDisplay(false);
			blasted3Notification.SetDisplay(false);
			blastedBeastNotification.SetDisplay(false);
		}

		/// <summary>
		/// Sets the directors / animations needed for status notifications.
		/// </summary>
		public void Init(PlayableDirector blasted1Director, PlayableDirector blasted2Director,
						 PlayableDirector blasted3Director, PlayableDirector blastedBeastDirector,
						 int lowHPThreshold)
		{
			_blasted1Director = blasted1Director;
			_blasted2Director = blasted2Director;
			_blasted3Director = blasted3Director;
			_blastedBeastDirector = blastedBeastDirector;
			_lowHPThreshold = lowHPThreshold;

			_blasted1Director.stopped += TryShowBlastedNotification;
			_blasted2Director.stopped += TryShowBlastedNotification;
			_blasted3Director.stopped += TryShowBlastedNotification;
			_blastedBeastDirector.stopped += TryShowBlastedNotification;

			// Setup LowHP animation
			_lowHPAnimation = _lowHP.schedule.Execute(() =>
			{
				_lowHP.style.opacity = Mathf.Sin((Time.time - _lowHPAnimationStartTime) * LOW_HP_BLINK_SPEED * Mathf.PI) / 2f + 0.5f;
			}).Every(50);
			_lowHPAnimation.Pause();
		}

		public override void SubscribeToEvents()
		{
			base.SubscribeToEvents();

			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnHealthChanged>(this, OnHealthChanged);
		}

		private void OnHealthChanged(EventOnHealthChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			var shouldShowLowHP = callback.CurrentHealth <= _lowHPThreshold;
			if (shouldShowLowHP == _lowHPAnimation.isActive) return;

			if (shouldShowLowHP)
			{
				_lowHPAnimationStartTime = Time.time;
				_lowHPAnimation.Resume();
			}
			else
			{
				_lowHPAnimation.Pause();
				_lowHP.style.opacity = 0;
			}
		}

		public override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();

			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.EntityKiller != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			_killedPlayersQueue.Enqueue((_blasted1PlayerName.text = callback.PlayersMatchData.Count <= 1
					? "DUMMY"
					: callback.PlayersMatchData[callback.PlayerDead].GetPlayerName(),
				callback.CurrentMultiKill));

			TryShowBlastedNotification(null);
		}

		private void TryShowBlastedNotification(PlayableDirector _)
		{
			if (_blasted1Director.state == PlayState.Playing || _killedPlayersQueue.Count == 0) return;

			var notification = _killedPlayersQueue.Dequeue();
			var killstreak = notification.Item2;

			if (killstreak == 2)
			{
				_blasted2PlayerName.text = notification.Item1;
				_blasted2Director.Play();
			}
			else if (killstreak == 3)
			{
				_blasted3PlayerName.text = notification.Item1;
				_blasted3Director.Play();
			}
			else if (killstreak > 3)
			{
				_blastedBeastPlayerName.text = notification.Item1;
				_blastedBeastDirector.Play();
			}
			else
			{
				_blasted1PlayerName.text = notification.Item1;
				_blasted1Director.Play();
			}
		}
	}
}