using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
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
		
		// private static bool _animationPlayed = false; // Hacky but this will be refactored

		protected override void Awake()
		{
			base.Awake();

			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_services.MessageBrokerService.Subscribe<CollectionItemEquippedMessage>(OnCharacterSkinUpdatedMessage);
			_services.MessageBrokerService.Subscribe<DataReinitializedMessage>(OnDataReinitializedMessage);
		}

		private void Start()
		{
			UpdateLocalPlayerSkin().Forget();
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_gameDataProvider?.EquipmentDataProvider?.Loadout?.StopObservingAll(this);
		}


		private void OnDataReinitializedMessage(DataReinitializedMessage obj)
		{
			UpdateLocalPlayerSkin().Forget();
		}

		private async UniTaskVoid UpdateLocalPlayerSkin()
		{
			var skin = _gameDataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PLAYER_SKINS);
			var melee = _gameDataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.MELEE_SKINS);

			// await UpdateSkin(skin, !_animationPlayed);
			await UpdateSkin(skin, false);
			await UpdateMeleeSkin(melee);

			// _animationPlayed = true;
		}

		private void OnCharacterSkinUpdatedMessage(CollectionItemEquippedMessage msg)
		{
			if (msg.Category != new CollectionCategory(GameIdGroup.PlayerSkin)) return;

			if (CharacterViewComponent != null)
			{
				Destroy(CharacterViewComponent.gameObject);
			}

			if (msg.EquippedItem == null) return;
			UpdateLocalPlayerSkin().Forget();
		}
	}
}