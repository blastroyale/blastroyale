using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This FTUE mono component marks the end of the FTUE level when the player collides with the defined trigger
	/// </summary>
	public class FtueCheckEndTriggerMonoComponent : MonoBehaviour
	{
		private IGameServices _gameServices;

		private void Awake()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out _))
			{
				_gameServices.MessageBrokerService.Publish(new FtueEndedMessage() );
			}
		}
	}
}
