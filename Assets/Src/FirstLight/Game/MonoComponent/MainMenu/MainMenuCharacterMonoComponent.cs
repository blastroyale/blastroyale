using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This monocomponent extends the BaseCharacterMonoComponent to listen to changed in the player's equipment and update the 3d model accordingly
	/// </summary>
	public class MainMenuCharacterMonoComponent : BaseCharacterMonoComponent
	{
		private IGameDataProvider _gameDataProvider;

		protected override void Awake()
		{
			base.Awake();
			
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_gameDataProvider.EquipmentDataProvider.Loadout.Observe(OnLoadoutUpdated);
			_services.MessageBrokerService.Subscribe<PlayerSkinUpdatedMessage>(OnChangeCharacterSkinMessage);
			_services.MessageBrokerService.Subscribe<UpdatedLoadoutMessage>(OnUpdatedLoadoutMessage);
		}

		private async void Start()
		{
			var skin = _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin;
			var loadout = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.Both);

			await UpdateSkin(skin, loadout);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_gameDataProvider?.EquipmentDataProvider?.Loadout?.StopObservingAll(this);
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
			else if(updateType is ObservableUpdateType.Added or ObservableUpdateType.Updated)
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

		private async void OnChangeCharacterSkinMessage(PlayerSkinUpdatedMessage callback)
		{
			Destroy(_characterViewComponent.gameObject);
			
			await UpdateSkin(callback.SkinId, _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.Both));
		}

		// private async void SkinLoaded(GameId id, GameObject instance, bool instantiated)
		// {
		// 	// Check that the player hasn't changed the skin again while we were loading
		// 	if (this.IsDestroyed() || id != _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin)
		// 	{
		// 		Destroy(instance);
		// 		return;
		// 	}
		//
		// 	instance.SetActive(false);
		//
		// 	var cacheTransform = instance.transform;
		// 	var loadout = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.Both);
		//
		// 	cacheTransform.SetParent(_characterAnchor);
		//
		// 	cacheTransform.localPosition = Vector3.zero;
		// 	cacheTransform.localRotation = Quaternion.identity;
		// 	_characterViewComponent = instance.GetComponent<MainMenuCharacterViewComponent>();
		//
		// 	await _characterViewComponent.Init(loadout);
		//
		// 	if (!_gameDataProvider.EquipmentDataProvider.Loadout.ContainsKey(GameIdGroup.Weapon))
		// 	{
		// 		EquipDefault();
		// 	}
		// 	
		// 	instance.SetActive(true);
		//
		// 	_animator = instance.GetComponent<Animator>();
		// 	
		// 	_characterLoadedEvent?.Invoke();
		// }
	}
}