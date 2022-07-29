using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This Mono component controls loading and creation of player character equipment items and skin.
	/// </summary>
	public class MainMenuCharacterMonoComponent : MonoBehaviour
	{
		private readonly int _equipRightHandHash = Animator.StringToHash("equip_hand_r");
		private readonly int _equipBodyHash = Animator.StringToHash("equip_body");
		private readonly int _victoryHash = Animator.StringToHash("victory");

		[SerializeField, Required] private UnityEvent _characterLoadedEvent;
		[SerializeField, Required] private Transform _frontEndLootCamera;
		[SerializeField, Required] private Transform _characterAnchor;

		private Quaternion _defaultCharacterRotation;
		private MainMenuCharacterViewComponent _characterViewComponent;
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private Animator _animator;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_defaultCharacterRotation = transform.localRotation;

			_gameDataProvider.EquipmentDataProvider.Loadout.Observe(OnLoadoutUpdated);
			_services.MessageBrokerService.Subscribe<PlayerSkinUpdatedMessage>(OnChangeCharacterSkinMessage);
			_services.MessageBrokerService.Subscribe<UpdatedLoadoutMessage>(OnUpdatedLoadoutMessage);
		}

		private void Start()
		{
			var skin = _gameDataProvider.PlayerDataProvider.CurrentSkin.Value;

			_services.AssetResolverService.RequestAsset<GameId, GameObject>(skin, true, true, SkinLoaded);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_gameDataProvider?.EquipmentDataProvider?.Loadout?.StopObservingAll(this);
		}

		private async void EquipDefault()
		{
			await _characterViewComponent.EquipItem(GameId.Hammer);
		}

		private async void OnLoadoutUpdated(GameIdGroup key, UniqueId previousId, UniqueId newId, ObservableUpdateType updateType)
		{
			if (updateType == ObservableUpdateType.Removed)
			{
				_characterViewComponent.UnequipItem(key);
			
				if (key == GameIdGroup.Weapon)
				{
					EquipDefault();
				}
			}
			else if(updateType == ObservableUpdateType.Added)
			{
				await _characterViewComponent.EquipItem(_gameDataProvider.UniqueIdDataProvider.Ids[newId]);
			}
		}

		private void OnUpdatedLoadoutMessage(UpdatedLoadoutMessage msg)
		{
			if (msg.SlotsUpdated.Count == 1)
			{
				_animator.SetTrigger(msg.SlotsUpdated.Keys.ToArray()[0] == GameIdGroup.Weapon ? _equipRightHandHash : _equipBodyHash);
			}
			else if (msg.SlotsUpdated.Count > 1)
			{
				_animator.SetTrigger(_victoryHash);
			}
		}

		private void OnChangeCharacterSkinMessage(PlayerSkinUpdatedMessage callback)
		{
			Destroy(_characterViewComponent.gameObject);

			_services.AssetResolverService.RequestAsset<GameId, GameObject>(callback.SkinId, true, true, SkinLoaded);
		}

		private async void SkinLoaded(GameId id, GameObject instance, bool instantiated)
		{
			// Check that the player hasn't changed the skin again while we were loading
			if (this.IsDestroyed() || id != _gameDataProvider.PlayerDataProvider.CurrentSkin.Value)
			{
				Destroy(instance);
				return;
			}

			instance.SetActive(false);

			var cacheTransform = instance.transform;

			cacheTransform.SetParent(_characterAnchor);

			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localRotation = Quaternion.identity;
			_characterViewComponent = instance.GetComponent<MainMenuCharacterViewComponent>();

			await _characterViewComponent.Init(_gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo());

			if (!_gameDataProvider.EquipmentDataProvider.Loadout.ContainsKey(GameIdGroup.Weapon))
			{
				EquipDefault();
			}
			
			instance.SetActive(true);

			_animator = instance.GetComponent<Animator>();
			
			_characterLoadedEvent?.Invoke();
		}
	}
}