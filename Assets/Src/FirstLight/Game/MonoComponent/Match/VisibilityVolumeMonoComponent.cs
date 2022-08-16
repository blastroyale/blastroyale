using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.MapViews
{
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

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			CheckUpdateAllVisiblePlayers();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player))
			{
				_currentlyCollidingPlayers.Add(player.EntityRef, player);
				
				if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
				{
					CheckUpdateAllVisiblePlayers();
				}
				else
				{
					CheckUpdateOneVisiblePlayer(player);
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player))
			{
				_currentlyCollidingPlayers.Remove(player.EntityRef);
				
				if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
				{
					CheckUpdateAllVisiblePlayers();
				}
				else
				{
					CheckUpdateOneVisiblePlayer(player);
				}
			}
		}

		private void CheckUpdateAllVisiblePlayers()
		{
			var spectatedPlayerWithinVolume = _currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			
			foreach (var player in _currentlyCollidingPlayers)
			{
				player.Value.SetRenderContainerActive(!spectatedPlayerWithinVolume);
			}
		}

		private void CheckUpdateOneVisiblePlayer(PlayerCharacterViewMonoComponent otherPlayer)
		{
			var spectatedPlayerWithinVolume = _currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			var otherPlayerWithinVolume = _currentlyCollidingPlayers.ContainsKey(otherPlayer.EntityRef);
			
			otherPlayer.SetRenderContainerActive(!spectatedPlayerWithinVolume && otherPlayerWithinVolume);
		}
	}
}