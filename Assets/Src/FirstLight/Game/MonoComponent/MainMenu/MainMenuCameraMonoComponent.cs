using Cinemachine;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This Mono Component controls the active main menu's camera behaviour
	/// </summary>
	public class MainMenuCameraMonoComponent : MonoBehaviour
	{
		[SerializeField, Required] private CinemachineVirtualCamera _shopCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _lootCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _mainCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _socialCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _cratesCamera;
		
		[HideInInspector]
		[SerializeField, Required] private CinemachineBrain _cinemachineBrain;
		
		private IGameServices _services;
		
		private void OnValidate()
		{
			_cinemachineBrain = _cinemachineBrain ? _cinemachineBrain : GetComponent<CinemachineBrain>();
		}
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpened);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnPlayScreenOpened(PlayScreenOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			_mainCamera.gameObject.SetActive(true);
		}
	}
}