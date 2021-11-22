using Cinemachine;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This Mono Component controls the active main menu's camera behaviour
	/// </summary>
	public class MainMenuCameraMonoComponent : MonoBehaviour
	{
		[SerializeField] private CinemachineVirtualCamera _shopCamera;
		[SerializeField] private CinemachineVirtualCamera _lootCamera;
		[SerializeField] private CinemachineVirtualCamera _mainCamera;
		[SerializeField] private CinemachineVirtualCamera _socialCamera;
		[SerializeField] private CinemachineVirtualCamera _cratesCamera;
		[HideInInspector]
		[SerializeField] private CinemachineBrain _cinemachineBrain;
		
		private IGameServices _services;
		
		private void OnValidate()
		{
			_cinemachineBrain = _cinemachineBrain ? _cinemachineBrain : GetComponent<CinemachineBrain>();
		}
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_services.MessageBrokerService.Subscribe<ShopScreenOpenedMessage>(OnShopScreenOpened);
			_services.MessageBrokerService.Subscribe<LootScreenOpenedMessage>(OnLootScreenOpened);
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpened);
			_services.MessageBrokerService.Subscribe<SocialScreenOpenedMessage>(OnSocialScreenOpened);
			_services.MessageBrokerService.Subscribe<CratesScreenOpenedMessage>(OnCratesScreenOpened);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnShopScreenOpened(ShopScreenOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			_shopCamera.gameObject.SetActive(true);
		}

		private void OnLootScreenOpened(LootScreenOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			_lootCamera.gameObject.SetActive(true);
		}

		private void OnPlayScreenOpened(PlayScreenOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			_mainCamera.gameObject.SetActive(true);
		}
		
		private void OnSocialScreenOpened(SocialScreenOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			_socialCamera.gameObject.SetActive(true);
		}

		private void OnCratesScreenOpened(CratesScreenOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			_cratesCamera.gameObject.SetActive(true);
		}
	}
}