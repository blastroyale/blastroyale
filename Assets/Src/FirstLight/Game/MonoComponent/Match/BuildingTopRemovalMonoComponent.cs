using System.Collections.Generic;
using FirstLight.Game.Messages;
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

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnPlayerDead);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			CheckUpdateBuildingTop();
		}
		
		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync) return;

			CheckUpdateBuildingTop();
		}
		
		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			if (_currentlyCollidingEntities.Contains(callback.Entity))
			{
				_currentlyCollidingEntities.Remove(callback.Entity);
				CheckUpdateBuildingTop();
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;

			if (!_currentlyCollidingEntities.Contains(player.EntityRef))
			{
				_currentlyCollidingEntities.Add(player.EntityRef);
			}
			
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				UpdateBuildingTop(true);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (!other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;
			
			_currentlyCollidingEntities.Remove(player.EntityRef);
			
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
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