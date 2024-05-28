using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	public class MessageTriggerVolumeMonoComponent : MonoBehaviour
	{
		public string VolumeId;
		private IGameServices _services;

		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!other.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player)) return;

			_services.MessageBrokerService.Publish(new PlayerEnteredMessageVolume {VolumeId = VolumeId});
		}
	}
}