using System;
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
			public EquipmentInfo Info;
			public bool IsSelected;
			public bool PlayViewNotificationAnimation;
			public bool IsSelectable;
			public Action<UniqueId> OnEquipmentClicked;
		}
		
		[SerializeField, Required] private EquipmentIconItemView _equipmentIconView;
		[SerializeField, Required] private TextMeshProUGUI _sliderLevelText;
		[SerializeField, Required] private Button _button;
		[SerializeField, Required] private Image _equippedImage;
		[SerializeField, Required] private GameObject _selectedFrameImage;
		[SerializeField, Required] private GameObject _hideSelectionImage;
		[SerializeField, Required] private Image _autoFireIcon;
		[SerializeField, Required] private Image _manualFireIcon;
		[SerializeField, Required] private NotificationUniqueIdView _notificationUniqueIdView;
		[SerializeField, Required] private NotificationUniqueIdUpgradeView _notificationUniqueIdUpgradeView;
		[SerializeField, Required] private Animation _cardItemAnimation;
		[SerializeField, Required] private AnimationClip _upgradeCardAnimationClip;
		[SerializeField, Required] private AnimationClip _equipCardAnimationClip;
		
		private long _uniqueId;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_services.MessageBrokerService.Subscribe<ItemEquippedMessage>(OnEquipCompletedMessage);
			_services.MessageBrokerService.Subscribe<ItemUpgradedMessage>(OnUpgradeCompletedMessage);
			_button.onClick.AddListener(OnButtonClick);
			OnAwake();
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		protected virtual void OnAwake() {}

		protected override void OnUpdateItem(EquipmentGridItemData data)
		{
			var uniqueId = data.Info.DataInfo.Data.Id;
			
			_selectedFrameImage.SetActive(data.IsSelected);
			_hideSelectionImage.SetActive(!data.IsSelectable);
			_equippedImage.enabled = data.Info.IsEquipped;
			_autoFireIcon.enabled = false;
			_manualFireIcon.enabled = false;

			if (data.IsSelected)
			{
				_gameDataProvider.UniqueIdDataProvider.NewIds.Remove(uniqueId);
			}
			
			_notificationUniqueIdView.SetUniqueId(uniqueId, data.PlayViewNotificationAnimation);
			_notificationUniqueIdUpgradeView.SetUniqueId(uniqueId);
			_equipmentIconView.SetInfo(data.Info.DataInfo);
			_uniqueId = uniqueId;
		}
		
		private void OnEquipCompletedMessage(ItemEquippedMessage message)
		{
			if (message.ItemId == _uniqueId)
			{
				_cardItemAnimation.clip = _equipCardAnimationClip;
				
				_cardItemAnimation.Rewind();
				_cardItemAnimation.Play();
			}
		}

		private void OnUpgradeCompletedMessage(ItemUpgradedMessage message)
		{
			if (message.ItemId == _uniqueId)
			{
				_cardItemAnimation.clip = _upgradeCardAnimationClip;
				
				_cardItemAnimation.Rewind();
				_cardItemAnimation.Play();
			}
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

