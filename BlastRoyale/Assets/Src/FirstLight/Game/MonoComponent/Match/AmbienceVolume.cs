using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	public class AmbienceVolume : MonoBehaviour
	{
		[SerializeField] private AmbienceType _ambienceType;
		[SerializeField] private BoxCollider _boxCollider;
		private IGameServices _services;
		private IMatchServices _matchServices;
		private IEntityViewUpdaterService _entityViewUpdater;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;
			
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				_services.MessageBrokerService.Publish(new PlayerEnteredAmbienceMessage(){Ambience = _ambienceType});
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (!other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;
			
			if (player.EntityRef == _matchServices.SpectateService.SpectatedPlayer.Value.Entity)
			{
				_services.MessageBrokerService.Publish(new PlayerLeftAmbienceMessage(){Ambience = _ambienceType});
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(0.5f, 1f, 1f, 0.5f);
			Gizmos.DrawCube(transform.position, Vector3.Scale(_boxCollider.size, transform.localScale));
		}
	}
}