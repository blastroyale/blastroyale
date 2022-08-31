using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <inheritdoc/>
	/// <remarks>
	/// Responsible of hiding the top of a building when a player enters it
	/// </remarks>
	public class BuildingTopRemovalMonoComponent : MonoBehaviour
	{
		private static readonly int _topAnimatorPlayerInsideParamNameHash = Animator.StringToHash("PlayerInside");

		[SerializeField] private Animator _topRemovalAnimator;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private List<EntityRef> _currentlyCollidingEntities;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_currentlyCollidingEntities = new List<EntityRef>();

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
		}

		private void OnDestroy()
		{
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			CheckUpdateBuildingTop();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player) &&
			    player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				_currentlyCollidingEntities.Add(player.EntityRef);
				
				UpdateBuildingTop(true);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player) &&
			    player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				_currentlyCollidingEntities.Remove(player.EntityRef);
				
				UpdateBuildingTop(false);
			}
		}

		private void CheckUpdateBuildingTop()
		{
			foreach (var entity in _currentlyCollidingEntities)
			{
				if (entity == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
				{
					UpdateBuildingTop(true);
					return;
				}
			}

			UpdateBuildingTop(false);
		}

		private void UpdateBuildingTop(bool playerInside)
		{
			_topRemovalAnimator.SetBool(_topAnimatorPlayerInsideParamNameHash, playerInside);
		}
	}
}