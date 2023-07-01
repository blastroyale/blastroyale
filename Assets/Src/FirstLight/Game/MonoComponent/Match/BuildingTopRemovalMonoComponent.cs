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
			_services.MessageBrokerService.Subscribe<MatchEndedMessage>(OnMatchEndedMessage);
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
			if (next.Player == PlayerRef.None) return;
			
			CheckUpdateBuildingTop(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
		}
		
		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync) return;

			CheckUpdateBuildingTop(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
		}

		private void OnMatchEndedMessage(MatchEndedMessage msg)
		{
			var game = QuantumRunner.Default.Game;
			var playerData = game.GeneratePlayersMatchDataLocal(out var leader, out var localWinner);
			var playerWinner = localWinner ? playerData[game.GetLocalPlayerRef()] : playerData[leader];

			if (playerWinner.Data.IsValid)
			{
				CheckUpdateBuildingTop(playerWinner.Data.Entity);
			}
			
			_currentlyCollidingEntities.Clear();
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			if (_currentlyCollidingEntities.Contains(callback.Entity))
			{
				_currentlyCollidingEntities.RemoveAll(entityRef => entityRef == callback.Entity);
				CheckUpdateBuildingTop(_matchServices.SpectateService.SpectatedPlayer.Value.Entity);
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;
			
			_currentlyCollidingEntities.Add(player.EntityRef);
				
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				UpdateBuildingTop(true);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (!other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;
			
			_currentlyCollidingEntities.Remove(player.EntityRef);
			
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity &&
				!_currentlyCollidingEntities.Contains(player.EntityRef))
			{
				UpdateBuildingTop(false);
			}
		}

		private void CheckUpdateBuildingTop(EntityRef entityRef)
		{
			foreach (var entity in _currentlyCollidingEntities)
			{
				if (entity == entityRef)
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