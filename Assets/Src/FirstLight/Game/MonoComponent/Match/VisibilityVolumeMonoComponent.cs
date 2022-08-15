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
		private List<PlayerCharacterViewMonoComponent> _currentlyCollidingPlayers;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_currentlyCollidingPlayers = new List<PlayerCharacterViewMonoComponent>();

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			CheckUpdateVisiblePlayers();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player) &&
			    player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				_currentlyCollidingPlayers.Add(player);

				foreach (var currentlyCollidingPlayer in _currentlyCollidingPlayers)
				{
					currentlyCollidingPlayer.SetRenderContainerActive(true);
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player) &&
			    player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				_currentlyCollidingPlayers.Remove(player);

				foreach (var currentlyCollidingPlayer in _currentlyCollidingPlayers)
				{
					currentlyCollidingPlayer.SetRenderContainerActive(false);
				}
			}
		}

		private void CheckUpdateVisiblePlayers()
		{
			var currentPlayerWithinVolume =
				_currentlyCollidingPlayers.FirstOrDefault(x => x.EntityRef ==
				                                               _matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			
			foreach (var player in _currentlyCollidingPlayers)
			{
				player.SetRenderContainerActive(currentPlayerWithinVolume != null);
			}
		}
	}
}

}