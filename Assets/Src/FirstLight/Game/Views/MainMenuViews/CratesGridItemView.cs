using System;
using System.Collections;
using Coffee.UIEffects;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script controls a specific Equipment Item held within the List of Items in teh Equipment screen.
	/// Tapping on an item brings up it's information.
	/// </summary>
	public class CratesGridItemView : GridItemBase<CratesGridItemView.CratesGridItemViewData>
	{
		public struct CratesGridItemViewData
		{
			public TimedBoxInfo Info;
			public bool IsEmpty;
			public bool IsSelected;
			public bool PlayViewNotificationAnimation;
			public Action<int> OnCrateClicked;
		}

		[SerializeField] private Sprite _backgroundSprite;
		[SerializeField] private TextMeshProUGUI _unlockTimeText;
		[SerializeField] private TextMeshProUGUI Text;
		[SerializeField] private Image BackgroundImage;
		[SerializeField] private Image IconImage;
		[SerializeField] private Button Button;
		[SerializeField] private Image SelectedImage;
		[SerializeField] private Image _frameImage;
		[SerializeField] private Color _regularColor;
		[SerializeField] private Color _selectedColor;
		[SerializeField] private GameObject _selectedFrameImage;
		[SerializeField] private GameObject _holderObject;
		[SerializeField] private Animation _readyToOpenAnimation;
		[SerializeField] private NotificationUniqueIdView _notificationUniqueIdView;
		[SerializeField] private UIShiny _shiny;
		[SerializeField] private Image _skullDetailImage;
		[SerializeField] private Image _clockIcon;
		[SerializeField] private Image _clockHandIcon;
		[SerializeField] private GameObject _messageHolder;
		[SerializeField] private GameObject _hurryHolder;
		[SerializeField] private TextMeshProUGUI _hardCurrencySkipCostText;
		[SerializeField] private TextMeshProUGUI _freeSlotText;
		
		private CratesGridItemViewData _data;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private Coroutine _coroutineTime;
		private float _skipBaseCost;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_skipBaseCost = _services.ConfigsProvider.GetConfig<QuantumGameConfig>().MinuteCostInHardCurrency.AsFloat;
			
			Button.onClick.AddListener(OnButtonClick);
		}

		private void OnDestroy()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
		}
		
		protected override async void OnUpdateItem(CratesGridItemViewData data)
		{
			var boxState = data.Info.GetState(_services.TimeService.DateTimeUtcNow);
			var assetResolver = _services.AssetResolverService;
			
			_data = data;
			_frameImage.color = data.IsSelected ? _selectedColor : _regularColor;
			SelectedImage.enabled = data.IsSelected;
			_holderObject.transform.localScale = Vector3.one;
			_holderObject.transform.rotation = new Quaternion(0, 0, 0, 0);
			_shiny.enabled = false;
			_skullDetailImage.enabled = false;
			
			if (data.IsSelected)
			{
				_gameDataProvider.UniqueIdDataProvider.NewIds.Remove(data.Info.Data.Id);
			}
			
			_notificationUniqueIdView.SetUniqueId(data.Info.Data.Id, data.PlayViewNotificationAnimation);
			_selectedFrameImage.SetActive(data.IsSelected);
			_readyToOpenAnimation.Stop();
			_hurryHolder.SetActive(false);
			_clockIcon.enabled = false;
			_clockHandIcon.enabled = false;
			_freeSlotText.enabled = false;

			if (_coroutineTime != null)
			{
				StopCoroutine(_coroutineTime);
				_coroutineTime = null;
			}

			if (_data.IsEmpty)
			{
				_unlockTimeText.text = "";
				Text.text = ScriptLocalization.MainMenu.FreeSlot;
				IconImage.enabled = false;
				_skullDetailImage.enabled = true;
				_freeSlotText.enabled = true;
				_messageHolder.SetActive(false);
				BackgroundImage.sprite = _backgroundSprite;
				return;
			}
			
			_messageHolder.SetActive(true);
			IconImage.enabled = true;
			IconImage.sprite = await assetResolver.RequestAsset<GameId, Sprite>(data.Info.Config.LootBoxId, false);
			BackgroundImage.sprite = await assetResolver.RequestAsset<ItemRarity, Sprite>(BackgroundSprite(data.Info.Config.LootBoxId), false);

			if (boxState == LootBoxState.Locked)
			{
				Text.text = ScriptLocalization.MainMenu.TapToUnlock;
				_unlockTimeText.text = _data.Info.Config.SecondsToOpen.ToHMS();
			}
			else if (boxState == LootBoxState.Unlocked)
			{
				ReadyToOpen();
			}
			else
			{
				_clockIcon.enabled = true;
				_clockHandIcon.enabled = true;
				_hurryHolder.SetActive(true);
				_unlockTimeText.text = "";
				_coroutineTime = StartCoroutine(UpdateState());
			}
		}

		private IEnumerator UpdateState()
		{
			var time = _data.Info.Data.EndTime - _services.TimeService.DateTimeUtcNow;
			var waiter = new WaitForSeconds(1);

			while (time.TotalSeconds > 0)
			{
				Text.text = ((uint) time.TotalSeconds).ToHoursMinutesSeconds();
				_hardCurrencySkipCostText.text = (time.TotalMinutes * _skipBaseCost).ToString("N0"); 
					
				yield return waiter;
				
				time = _data.Info.Data.EndTime - _services.TimeService.DateTimeUtcNow;
			}

			Text.text = ScriptLocalization.MainMenu.OpenCrate;
			_coroutineTime = null;
			
			ReadyToOpen();
		}
		
		private void OnButtonClick()
		{
			if (_data.IsEmpty)
			{
				return;
			}
			
			Data.OnCrateClicked(_data.Info.Data.Slot);
		}
		
		private void ReadyToOpen()
		{
			_unlockTimeText.text = "";
			Text.text = ScriptLocalization.MainMenu.OpenCrate;

			if (!_readyToOpenAnimation.isPlaying)
			{
				_readyToOpenAnimation.Play();
			}
		}

		private ItemRarity BackgroundSprite(GameId id)
		{
			switch (id)
			{
				case GameId.CommonBox: return ItemRarity.Common;
				case GameId.UncommonBox: return ItemRarity.Uncommon;
				case GameId.RareBox: return ItemRarity.Rare;
				case GameId.EpicBox: return ItemRarity.Epic;
				case GameId.LegendaryBox: return ItemRarity.Legendary;
				default: throw new ArgumentException($"Wrong Box Game ID: {id}");
			}
		}
	}
}