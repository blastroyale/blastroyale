using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.MapViews
{
	/// <summary>
	/// This class handles showing/hiding player renderers inside and outside of visibility volumes based on various factors
	/// </summary>
	public class VisibilityVolumeMonoComponent : MonoBehaviour
	{
		private IGameServices _services;
		private IMatchServices _matchServices;
		private IEntityViewUpdaterService _entityViewUpdater;
		private Dictionary<EntityRef, PlayerCharacterViewMonoComponent> _currentlyCollidingPlayers;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_currentlyCollidingPlayers = new Dictionary<EntityRef, PlayerCharacterViewMonoComponent>();

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		private void OnDestroy()
		{
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		/// <summary>
		/// Requests to check if this volume contains 
		/// </summary>
		public bool VolumeHasSpectatedPlayer()
		{
			return _currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player))
			{
				if (!_currentlyCollidingPlayers.ContainsKey(player.EntityRef))
				{
					_currentlyCollidingPlayers.Add(player.EntityRef, player);
				}

				player.CollidingVisibilityVolumes.Add(this);

				if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
				{
					CheckUpdateAllVisiblePlayers();
				}
				else if (player.CollidingVisibilityVolumes.Count == 1)
				{
					CheckUpdateOneVisiblePlayer(player);
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player))
			{
				_currentlyCollidingPlayers.Remove(player.EntityRef);
				player.CollidingVisibilityVolumes.Remove(this);

				if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
				{
					CheckUpdateAllVisiblePlayers();
				}
				else if (player.CollidingVisibilityVolumes.Count == 0)
				{
					CheckUpdateOneVisiblePlayer(player);
				}
			}
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			CheckUpdateAllVisiblePlayers();
		}
		
		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync) return;

			CheckUpdateAllVisiblePlayers();
		}

		private void CheckUpdateAllVisiblePlayers()
		{
			var spectatedPlayerWithinVolume =
				_currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);

			foreach (var player in _currentlyCollidingPlayers)
			{
				player.Value.SetRenderContainerVisible(spectatedPlayerWithinVolume);
			}
		}

		private void CheckUpdateOneVisiblePlayer(PlayerCharacterViewMonoComponent player)
		{
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}

			var spectatedPlayerWithinVolume =
				_currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			var otherPlayerWithinVolume = _currentlyCollidingPlayers.ContainsKey(player.EntityRef);

			player.SetRenderContainerVisible((spectatedPlayerWithinVolume == otherPlayerWithinVolume) ||
			                                 (spectatedPlayerWithinVolume));
		}
	}
}