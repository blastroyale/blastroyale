using System;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.MapViews
{
	/// <inheritdoc/>
	/// <remarks>
	/// Responsible of hiding the top of a building when a player enters it
	/// </remarks>
	public class BuildingTopRemovalViewMonoComponent : MonoBehaviour
	{
		private static readonly int _topAnimatorPlayerInsideParamNameHash = Animator.StringToHash("PlayerInside");
			
		[SerializeField] private Animator _topRemovalAnimator;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private EntityRef _currentlyObservedPlayer;
		private List<EntityRef> _currentlyCollidingEntities;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_currentlyCollidingEntities = new List<EntityRef>();
			
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
			
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
		}
		
		private void OnDestroy()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			_currentlyObservedPlayer = callback.Entity;
		}
		
		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() || _currentlyObservedPlayer.IsValid)
			{
				return;
			}

			_currentlyObservedPlayer = callback.Entity;
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			_currentlyObservedPlayer = next.Entity;
			CheckUpdateBuildingTop();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player))
			{
				_currentlyCollidingEntities.Add(player.EntityRef);
				
				if(player.EntityRef == _currentlyObservedPlayer)
				{
					UpdateBuildingTop(true);
				}
			} 
		}
		private void OnTriggerExit(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player))
			{
				_currentlyCollidingEntities.Remove(player.EntityRef);
				
				if(player.EntityRef == _currentlyObservedPlayer)
				{
					UpdateBuildingTop(false);
				}
			}
		}

		private void CheckUpdateBuildingTop()
		{
			foreach (var entity in _currentlyCollidingEntities)
			{
				if (entity == _currentlyObservedPlayer)
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