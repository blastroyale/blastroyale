using System.Linq;
using FirstLight.Game.Data;
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
			_services.MessageBrokerService.Subscribe<CollectionItemEquippedMessage>(OnCharacterSkinUpdatedMessage);
			_services.MessageBrokerService.Subscribe<DataReinitializedMessage>(OnDataReinitializedMessage);
		}

		private void Start()
		{
			InitAllComponents();
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_gameDataProvider?.EquipmentDataProvider?.Loadout?.StopObservingAll(this);
		}
		
		private async void InitAllComponents()
		{
			var skin = _gameDataProvider.CollectionDataProvider.GetEquipped(new (GameIdGroup.PlayerSkin)).Id;
			var loadout = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.All);

			await UpdateSkin(skin, loadout);
		}

		private void OnDataReinitializedMessage(DataReinitializedMessage obj)
		{
			InitAllComponents();
		}

		private async void OnLoadoutUpdated(GameIdGroup key, UniqueId previousId, UniqueId newId, ObservableUpdateType updateType)
		{
			// This happens when the system auto unequips/equips items during the loading of screen
			if (_characterViewComponent == null)
			{
				var index = _equipment.FindIndex(item => item.GameId.GetSlot() == key);
				
				if (index > -1)
				{
					_equipment.RemoveAt(index);
				}

				if (updateType is ObservableUpdateType.Added or ObservableUpdateType.Updated)
				{
					_equipment.Add(_gameDataProvider.EquipmentDataProvider.Inventory[newId]);
				}
			}
		}
		
		private async void OnCharacterSkinUpdatedMessage(CollectionItemEquippedMessage msg)
		{
			if (msg.Category != new CollectionCategory(GameIdGroup.PlayerSkin)) return;

			if (_characterViewComponent != null)
			{
				Destroy(_characterViewComponent.gameObject);
			}

			if (msg.EquippedItem == null) return;
			
			await UpdateSkin(msg.EquippedItem.Id, _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.All));
		}
	}
}