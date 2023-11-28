using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// This monocomponent extends the BaseCharacterMonoComponent to listen to changed in the player's equipment and update the 3d model accordingly
	/// </summary>
	public class MainMenuMeleeWeaponMonoComponent : MonoBehaviour
	{
		[SerializeField] private Transform _anchor;
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private Transform _currentMeleeWeapon;

		protected void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.ResolveServices();
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

		private void OnDataReinitializedMessage(DataReinitializedMessage obj)
		{
			InitAllComponents();
		}

		private async void InitAllComponents()
		{
			var skin = _gameDataProvider.CollectionDataProvider.GetEquipped(new (GameIdGroup.MeleeSkin));

			await UpdateMeleeObject(skin);
		}

		private async Task UpdateMeleeObject(ItemData skin)
		{
			if (_currentMeleeWeapon != null)
			{
				Destroy(_currentMeleeWeapon.gameObject);
				_currentMeleeWeapon = null;
			}

			var model = await _services.CollectionService.LoadCollectionItem3DModel(skin, true);
			model.transform.SetParent(_anchor);
			model.transform.localPosition = Vector3.zero;
			model.transform.localRotation = Quaternion.identity;;
			model.transform.localScale = Vector3.one;;
			_currentMeleeWeapon = model.transform;
		}


		private async void OnCharacterSkinUpdatedMessage(CollectionItemEquippedMessage msg)
		{
			if (msg.Category != new CollectionCategory(GameIdGroup.MeleeSkin)) return;
			if (msg.EquippedItem == null) return;
			await UpdateMeleeObject(msg.EquippedItem);
		}
	}
}