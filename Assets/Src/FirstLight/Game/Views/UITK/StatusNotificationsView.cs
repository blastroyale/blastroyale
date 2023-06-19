using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles all status notifications on the match HUD screen.
	/// </summary>
	public class StatusNotificationsView : UIView
	{
		private IMatchServices _matchServices;

		private VisualElement _blastedNotification;
		private Label _blastedPlayerName;

		private PlayableDirector _blastedDirector;

		private readonly Queue<string> _killedPlayersQueue = new();

		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_matchServices = MainInstaller.ResolveMatchServices();

			_blastedNotification = element.Q("BlastedNotification").Required();
			_blastedPlayerName = _blastedNotification.Q<Label>("PlayerNameLabel").Required();

			_blastedNotification.SetDisplay(false);
		}

		/// <summary>
		/// Sets the directors / animations needed for status notifications.
		/// </summary>
		public void SetDirectors(PlayableDirector blastedDirector)
		{
			_blastedDirector = blastedDirector;

			_blastedDirector.stopped += TryShowBlastedNotification;
		}

		public override void SubscribeToEvents()
		{
			base.SubscribeToEvents();

			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
		}

		public override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();

			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.EntityKiller != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			_killedPlayersQueue.Enqueue(_blastedPlayerName.text = callback.PlayersMatchData.Count <= 1
				? "Dummy"
				: callback.PlayersMatchData[callback.PlayerDead].GetPlayerName());

			TryShowBlastedNotification(null);
		}

		private void TryShowBlastedNotification(PlayableDirector _)
		{
			if (_blastedDirector.state == PlayState.Playing || _killedPlayersQueue.Count == 0) return;

			_blastedPlayerName.text = _killedPlayersQueue.Dequeue();
			_blastedDirector.Play();
		}
	}
}