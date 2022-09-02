using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Infos;
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

		[SerializeField, Required] private EquipmentIconItemView _equipmentIconView;
		[SerializeField, Required] private Button _button;
		[SerializeField, Required] private Image _equippedImage;
		[SerializeField, Required] private Image _cooldownImage;
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
			OnAwake();
		}

		private void OnDestroy()
		{
			_gameDataProvider.EquipmentDataProvider.Loadout.StopObserving(OnLoadoutUpdated);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected virtual void OnAwake()
		{
		}

		protected override void OnUpdateItem(EquipmentGridItemData data)
		{
			var equipmentDataProvider = _gameDataProvider.EquipmentDataProvider;

			if (_gameDataProvider.EquipmentDataProvider.NftInventory.ContainsKey(data.Id))
			{
				var info = equipmentDataProvider.GetNftInfo(data.Id);
				_equippedImage.enabled = info.EquipmentInfo.IsEquipped;
				_cooldownImage.enabled = info.IsOnCooldown;
			}

			_selectedFrameImage.SetActive(data.IsSelected);
			
			if (data.IsSelected)
			{
				_gameDataProvider.UniqueIdDataProvider.NewIds.Remove(data.Id);
			}

			_notificationUniqueIdView.SetUniqueId(data.Id, data.PlayViewNotificationAnimation);
			_equipmentIconView.SetInfo(data.Id);
			_uniqueId = data.Id;
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