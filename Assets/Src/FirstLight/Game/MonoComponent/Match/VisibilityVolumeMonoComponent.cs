using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			CheckUpdateAllVisiblePlayers();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player))
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
			if (other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player))
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
				if (player.Key == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
				{
					continue;
				}
				
				player.Value.SetRenderContainerActive(spectatedPlayerWithinVolume);
			}
		}

		private void CheckUpdateOneVisiblePlayer(PlayerCharacterViewMonoComponent player)
		{
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				return;
			}
			
			var spectatedPlayerWithinVolume = _currentlyCollidingPlayers.ContainsKey(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			var otherPlayerWithinVolume = _currentlyCollidingPlayers.ContainsKey(player.EntityRef);

			player.SetRenderContainerActive((spectatedPlayerWithinVolume == otherPlayerWithinVolume) ||
			                                (spectatedPlayerWithinVolume));
		}
	}
}