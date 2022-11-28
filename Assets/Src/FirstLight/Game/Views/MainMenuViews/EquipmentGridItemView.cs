using System;
using UnityEngine;
using UnityEngine.UI;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using Quantum;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script controls a specific Equipment Item held within the List of Items in teh Equipment screen.
	/// Tapping on an item brings up it's information.
	/// </summary>
	public class EquipmentGridItemView : GridItemBase<EquipmentGridItemView.EquipmentGridItemData>
	{
		public struct EquipmentGridItemData
		{
			public UniqueId Id;
			public Equipment Equipment;
			public bool IsSelected;
			public bool PlayViewNotificationAnimation;
			public bool IsSelectable;
			public Action<UniqueId> OnEquipmentClicked;
		}

		[SerializeField, Required] private EquipmentCardView _equipmentCardView;
		[SerializeField, Required] private Button _button;
		[SerializeField, Required] private Image _equippedImage;
		[SerializeField, Required] private Image _cooldownImage;
		[SerializeField, Required] private Image _nftImage;
		[SerializeField, Required] private GameObject _selectedFrameImage;
		[SerializeField, Required] private NotificationUniqueIdView _notificationUniqueIdView;
		[SerializeField, Required] private Animation _cardItemAnimation;
		[SerializeField, Required] private AnimationClip _equipCardAnimationClip;

		private long _uniqueId;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_gameDataProvider.EquipmentDataProvider.Loadout.Observe(OnLoadoutUpdated);
			_button.onClick.AddListener(OnButtonClick);
		}

		private void OnDestroy()
		{
			_gameDataProvider.EquipmentDataProvider.Loadout.StopObserving(OnLoadoutUpdated);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void OnUpdateItem(EquipmentGridItemData data)
		{
			var equipmentDataProvider = _gameDataProvider.EquipmentDataProvider;

			if (_gameDataProvider.EquipmentDataProvider.TryGetNftInfo(data.Id, out var nftInfo))
			{
				_equippedImage.enabled = nftInfo.EquipmentInfo.IsEquipped;
				_cooldownImage.enabled = nftInfo.IsOnCooldown;
				_nftImage.gameObject.SetActive(!nftInfo.IsOnCooldown);
			}
			else
			{
				var info = equipmentDataProvider.GetInfo(data.Id);
				_equippedImage.enabled = info.IsEquipped;
				_cooldownImage.enabled = false;
				_nftImage.gameObject.SetActive(false);
			}

			_selectedFrameImage.SetActive(data.IsSelected);

			if (data.IsSelected)
			{
				_gameDataProvider.UniqueIdDataProvider.NewIds.Remove(data.Id);
			}

			_notificationUniqueIdView.SetUniqueId(data.Id, data.PlayViewNotificationAnimation);
			_uniqueId = data.Id;

#pragma warning disable CS4014
			_equipmentCardView.Initialise(data.Equipment);
#pragma warning restore CS4014
		}

		private void OnLoadoutUpdated(GameIdGroup key, UniqueId previousId, UniqueId newId,
			ObservableUpdateType updateType)
		{
			if (newId != _uniqueId || updateType != ObservableUpdateType.Added)
			{
				return;
			}

			_cardItemAnimation.clip = _equipCardAnimationClip;

			_cardItemAnimation.Rewind();
			_cardItemAnimation.Play();
		}

		private void OnButtonClick()
		{
			if (Data.IsSelectable)
			{
				Data.OnEquipmentClicked(_uniqueId);
			}
		}
	}
}