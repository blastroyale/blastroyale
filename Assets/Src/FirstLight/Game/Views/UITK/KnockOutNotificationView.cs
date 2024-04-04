using System;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.UIService;
using Quantum;
using Quantum.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles the knockout notifications, the and the label
	/// </summary>
	[Serializable]
	public class KnockOutNotificationView : UIView
	{
		[Required, SerializeField] private PlayableDirector _friendKnockedOutPlayable;
		[Required, SerializeField] private PlayableDirector _knockedOutPlayable;

		private IGameNetworkService _gameNetworkService;
		private IConfigsProvider _configsProvider;
		private IMatchServices _matchServices;
		private IGameServices _services;

		private VisualElement _knockedOut;
		private VisualElement _friendKnockedOut;
		private VisualElement _knockoutLabel;
		private PlayerHealthShieldElement _localPlayerHealthShield;


		public override void Attached(VisualElement element)
		{
			_matchServices = MainInstaller.ResolveMatchServices();
			_services = MainInstaller.ResolveServices();
			_knockedOut = element.Q<VisualElement>("KnockedOutNotification").Required();
			_friendKnockedOut = element.Q<VisualElement>("NeedshelpNotification").Required();
			_knockoutLabel = element.Q<VisualElement>("KnockedOutLabel").Required();
			_localPlayerHealthShield = element.Q<PlayerHealthShieldElement>("LocalPlayerHealthShield").Required();

			base.Attached(element);
		}

		public override void OnScreenOpen(bool reload)
		{
			QuantumEvent.SubscribeManual<EventOnPlayerKnockedOut>(this, OnPlayerKnockedOut);
			QuantumEvent.SubscribeManual<EventOnPlayerRevived>(this, OnPlayerRevived);
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
		}

		public override void OnScreenClose()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
			base.OnScreenClose();
		}

		private void OnMatchStarted(MatchStartedMessage obj)
		{
			var f = obj.Game.Frames.Verified;
			var currentPlayer = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;

			SetLocalPlayerKnockOutStatus(ReviveSystem.IsKnockedOut(f, currentPlayer));
		}

		private void SetLocalPlayerKnockOutStatus(bool knockedOut)
		{
			if (knockedOut)
			{
				_localPlayerHealthShield.SetKnockedOut(true);
				_knockoutLabel.SetDisplay(true);
				_knockedOut.SetDisplay(true);
				_knockedOutPlayable.Play();
				return;
			}

			_knockedOut.SetDisplay(false);
			_knockoutLabel.SetDisplay(false);
			_localPlayerHealthShield.SetKnockedOut(false);
		}

		private void OnPlayerRevived(EventOnPlayerRevived callback)
		{
			var currentPlayer = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;

			if (callback.Entity != currentPlayer) return;
			SetLocalPlayerKnockOutStatus(false);
		}

		private void OnPlayerKnockedOut(EventOnPlayerKnockedOut callback)
		{
			var currentPlayer = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;
			if (callback.Entity == currentPlayer)
			{
				SetLocalPlayerKnockOutStatus(true);
				return;
			}

			if (TeamSystem.HasSameTeam(callback.Game.Frames.Verified, currentPlayer, callback.Entity))
			{
				_friendKnockedOut.SetDisplay(true);
				_friendKnockedOutPlayable.Play();
			}
		}
	}
}