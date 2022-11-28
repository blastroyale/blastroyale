using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.UIElements;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using I2.Loc;
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
			public UniqueId EquipmentId;
			public Action OnCloseClicked;
			public Action<Mode> OnActionConfirmed;
		}

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private Label _title;
		private EquipmentCardElement _card;
		private VisualElement _scrappingContent;
		private VisualElement _upgradingContent;
		private VisualElement _repairingContent;

		private VisualElement _blastHubContainer;

		private EquipmentPopupScrapView _scrapView;
		private EquipmentPopupUpgradeView _upgradeView;
		private EquipmentPopupRepairView _repairView;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_title = root.Q<Label>("Title").Required();
			_card = root.Q<EquipmentCardElement>("Card").Required();
			_blastHubContainer = root.Q<VisualElement>("BHContainer").Required();

			_scrappingContent = root.Q<VisualElement>("Scrapping").Required().AttachView(this, out _scrapView);
			_upgradingContent = root.Q<VisualElement>("Upgrading").Required().AttachView(this, out _upgradeView);
			_repairingContent = root.Q<VisualElement>("Repairing").Required().AttachView(this, out _repairView);

			root.Q<ImageButton>("CloseButton").clicked += Data.OnCloseClicked;


			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetupPopup();
		}

		private void SetupPopup()
		{
			var info = _gameDataProvider.EquipmentDataProvider.GetInfo(Data.EquipmentId);

			_card.SetEquipment(info.Equipment, info.Id, false, info.IsNft);

			_scrappingContent.SetDisplay(Data.PopupMode == Mode.Scrap);
			_upgradingContent.SetDisplay(Data.PopupMode == Mode.Upgrade);
			_repairingContent.SetDisplay(Data.PopupMode == Mode.Repair);
			_blastHubContainer.SetDisplay(info.IsNft);

			switch (Data.PopupMode)
			{
				case Mode.Scrap:
					_title.text = ScriptLocalization.UITEquipment.popup_scrapping_item;
					_scrapView.SetData(info, () => Data.OnActionConfirmed(Mode.Scrap), Data.OnCloseClicked);
					break;
				case Mode.Upgrade:
					_title.text = ScriptLocalization.UITEquipment.popup_upgrading_item;
					_upgradeView.SetData(info, () => Data.OnActionConfirmed(Mode.Upgrade));
					break;
				case Mode.Repair:
					_title.text = ScriptLocalization.UITEquipment.popup_repairing_item;
					_repairView.SetData(info, () => Data.OnActionConfirmed(Mode.Repair));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public enum Mode
		{
			Scrap,
			Upgrade,
			Repair
		}
	}
}