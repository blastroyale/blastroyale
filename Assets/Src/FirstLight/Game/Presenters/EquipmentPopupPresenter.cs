using System;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.UIElements;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the equipment selection screen.
	/// </summary>
	[LoadSynchronously]
	public class EquipmentPopupPresenter : UiToolkitPresenterData<EquipmentPopupPresenter.StateData>
	{
		public struct StateData
		{
			public Mode PopupMode;
			public UniqueId[] EquipmentIds;
			public Action OnCloseClicked;
			public Action<Mode, UniqueId> OnActionConfirmed;
		}

		private const string UssFullPage = "equipment-popup--full-page";

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private Label _title;
		private EquipmentCardElement _card;
		private VisualElement _popup;
		private VisualElement _scrappingContent;
		private VisualElement _upgradingContent;
		private VisualElement _repairingContent;
		private VisualElement _fusingContent;
		private VisualElement _rustedContent;

		private VisualElement _blastHubContainer;

		private EquipmentPopupScrapView _scrapView;
		private EquipmentPopupUpgradeView _upgradeView;
		private EquipmentPopupRepairView _repairView;
		private EquipmentPopupRustedView _rustedView;
		private EquipmentPopupFuseView _fuseView;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_popup = root.Q<VisualElement>("EquipmentPopup").Required();
			_title = root.Q<Label>("Title").Required();
			_card = root.Q<EquipmentCardElement>("Card").Required();
			_blastHubContainer = root.Q<VisualElement>("BHContainer").Required();

			_scrappingContent = root.Q<VisualElement>("Scrapping").Required().AttachView(this, out _scrapView);
			_upgradingContent = root.Q<VisualElement>("Upgrading").Required().AttachView(this, out _upgradeView);
			_repairingContent = root.Q<VisualElement>("Repairing").Required().AttachView(this, out _repairView);
			_rustedContent = root.Q<VisualElement>("Rusted").Required().AttachView(this, out _rustedView);
			_fusingContent = root.Q<VisualElement>("Fusion").Required().AttachView(this, out _fuseView);

			var csCounter = root.Q<CurrencyDisplayElement>("CSCurrency").Required();
			csCounter.AttachView(this, out CurrencyDisplayView _);
			csCounter.SetDisplay(false);
			
			root.Q<CurrencyDisplayElement>("CoinCurrency").Required().AttachView(this, out CurrencyDisplayView _);
			
			var fragmentsCounter = root.Q<CurrencyDisplayElement>("FragmentsCurrency").Required();
			fragmentsCounter.AttachView(this, out CurrencyDisplayView _);
			fragmentsCounter.SetDisplay(false);

			root.Q<ImageButton>("CloseButton").clicked += Data.OnCloseClicked;
			root.Q<VisualElement>("Background")
				.RegisterCallback<ClickEvent, StateData>((_, data) => data.OnCloseClicked(), Data);

			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetupPopup();
		}

		private void SetupPopup()
		{
			_popup.RemoveModifiers();
			_scrappingContent.SetDisplay(Data.PopupMode == Mode.Scrap);
			_upgradingContent.SetDisplay(Data.PopupMode == Mode.Upgrade);
			_repairingContent.SetDisplay(Data.PopupMode == Mode.Repair);
			_fusingContent.SetDisplay(Data.PopupMode == Mode.Fuse);
			_rustedContent.SetDisplay(Data.PopupMode == Mode.Rusted);

			if (Data.PopupMode == Mode.Rusted)
			{
				_popup.AddToClassList(UssFullPage);
				_blastHubContainer.SetDisplay(false);

				_title.text = ScriptLocalization.UITEquipment.popup_items_rusted;
				_rustedView.SetData(Data.EquipmentIds.Select(id => _gameDataProvider.EquipmentDataProvider.GetInfo(id)),
					() => Data.OnActionConfirmed(Mode.Rusted, UniqueId.Invalid),
					Data.OnCloseClicked);
				return;
			}

			var info = _gameDataProvider.EquipmentDataProvider.GetInfo(Data.EquipmentIds[0]);

			_card.SetEquipment(info.Equipment, info.Id, false, info.IsNft);
			_blastHubContainer.SetDisplay(info.IsNft);

			switch (Data.PopupMode)
			{
				case Mode.Scrap:
					_title.text = ScriptLocalization.UITEquipment.popup_scrapping_item;
					_scrapView.SetData(info, () => Data.OnActionConfirmed(Mode.Scrap, info.Id));
					break;
				case Mode.Upgrade:
					_title.text = ScriptLocalization.UITEquipment.popup_upgrading_item;
					_upgradeView.SetData(info, () => Data.OnActionConfirmed(Mode.Upgrade, info.Id),
						!HasEnoughCurrency(info.UpgradeCost));
					break;
				case Mode.Repair:
					_title.text = ScriptLocalization.UITEquipment.popup_repairing_item;
					_repairView.SetData(info, () => Data.OnActionConfirmed(Mode.Repair, info.Id),
						!HasEnoughCurrency(info.RepairCost));
					break;
				case Mode.Fuse:
					_title.text = ScriptLocalization.UITEquipment.popup_fusing_item;
					_fuseView.SetData(info, () => Data.OnActionConfirmed(Mode.Fuse, info.Id),
						new bool[] { !HasEnoughCurrency(info.FuseCost[0]), !HasEnoughCurrency(info.FuseCost[0]) });
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private bool HasEnoughCurrency(Pair<GameId, uint> cost)
		{
			return cost.Value <= _gameDataProvider.CurrencyDataProvider.GetCurrencyAmount(cost.Key);
		}

		public enum Mode
		{
			Scrap,
			Upgrade,
			Repair,
			Rusted,
			Fuse
		}
	}
}