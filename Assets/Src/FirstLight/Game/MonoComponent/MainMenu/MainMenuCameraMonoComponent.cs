using Cinemachine;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
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
		[SerializeField, Required] private CinemachineVirtualCamera _skinsCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _helmetCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _armorCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _shieldCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _amuletCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _weaponCamera;
		
		private CinemachineBrain _cinemachineBrain;
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_cinemachineBrain = FLGCamera.Instance.CinemachineBrain;
			
			_services.MessageBrokerService.Subscribe<PlayScreenOpenedMessage>(OnPlayScreenOpened);
			_services.MessageBrokerService.Subscribe<SkinsScreenOpenedMessage>(OnSkinsScreenOpened);
			_services.MessageBrokerService.Subscribe<EquipmentScreenOpenedMessage>(OnEquipmentScreenOpened);
			_services.MessageBrokerService.Subscribe<SkinsScreenOpenedMessage>(OnSkinsScreenOpened);
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
		
		private void OnSkinsScreenOpened(SkinsScreenOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			_skinsCamera.gameObject.SetActive(true);
		}
		
		private void OnEquipmentScreenOpened(EquipmentScreenOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			_lootCamera.gameObject.SetActive(true);
		}
		
		private void OnEquipmentSlotOpened(EquipmentSlotOpenedMessage data)
		{
			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);
			
			switch (data.Slot)
			{
				case GameIdGroup.Weapon :
					_weaponCamera.gameObject.SetActive(true);
					break;
				case GameIdGroup.Armor :
					_armorCamera.gameObject.SetActive(true);
					break;
				case GameIdGroup.Shield :
					_shieldCamera.gameObject.SetActive(true);
					break;
				case GameIdGroup.Amulet :
					_amuletCamera.gameObject.SetActive(true);
					break;
				default: // helmet camera is ok to be default one as well as it points to face, just in case
					_helmetCamera.gameObject.SetActive(true);
					break;
			}
		}
	}
}