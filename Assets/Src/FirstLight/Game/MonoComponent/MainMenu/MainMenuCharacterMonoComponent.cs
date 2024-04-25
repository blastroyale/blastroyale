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
			var skin = _gameDataProvider.CollectionDataProvider.GetEquipped(new (GameIdGroup.PlayerSkin));

			await UpdateSkin(skin);
		}

		private void OnDataReinitializedMessage(DataReinitializedMessage obj)
		{
			InitAllComponents();
		}


		private async void OnCharacterSkinUpdatedMessage(CollectionItemEquippedMessage msg)
		{
			if (msg.Category != new CollectionCategory(GameIdGroup.PlayerSkin)) return;

			if (_characterViewComponent != null)
			{
				Destroy(_characterViewComponent.gameObject);
			}

			if (msg.EquippedItem == null) return;

			await UpdateSkin(msg.EquippedItem);
		}
	}
}