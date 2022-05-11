using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This mono component controls the behaviour of the entire FTUE level
	/// </summary>
	public class FtueMonoComponent : MonoBehaviour
	{
		private GameObject _gameObject;
		private IGameServices _service;

		private void Awake()
		{
			_service = MainInstaller.Resolve<IGameServices>();
			
			_service.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
		}

		private void OnDestroy()
		{
			if (_gameObject != null)
			{
				Addressables.ReleaseInstance(_gameObject);
			}
			
			_service?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private async void OnMatchStarted(MatchStartedMessage message)
		{
			_gameObject = await Addressables.InstantiateAsync(AddressableId.Timeline_FtueTimeline.GetConfig().Address, transform).Task;
			
			//_timeline.Play();
			//_timeline.playableGraph.PlayTimeline();
		}
	}
}