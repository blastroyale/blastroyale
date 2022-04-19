using System;
using FirstLight.Game.Commands;
using FirstLight.Game.Infos;
using UnityEngine;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Views;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Services;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the overflow crates to award to the player
	/// </summary>
	public class OverflowLootDialogPresenter : AnimatedUiPresenterData<OverflowLootDialogPresenter.StateData>
	{
		public struct StateData
		{
			public Action CloseClicked;
			public Action SpeedUpAllBoxes;
		}

		[SerializeField] private Button _confirmButton;
		[SerializeField] private Button _declineButton;
		[SerializeField] private Transform _gridLayout;
		[SerializeField] private ScrollRect _scrollRect;
		[SerializeField] private RewardView _rewardRef;
		[SerializeField] private TextMeshProUGUI _hardCurrencyCostText;

		private IObjectPool<RewardView> _rewardPool;
		private IGameDataProvider _gameDataProvider;
		private LootBoxInventoryInfo _info;
		
		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_rewardRef.gameObject.SetActive(false);
			_confirmButton.onClick.AddListener(OnConfirmPressed);
			_declineButton.onClick.AddListener(OnDeclinePressed);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			_rewardPool = new GameObjectPool<RewardView>(4, _rewardRef);
			_info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			_hardCurrencyCostText.text = _info.GetUnlockExtraBoxesCost(Services.TimeService.DateTimeUtcNow).ToString();
		}

		protected override void OnOpenedCompleted()
		{
			_rewardPool.DespawnAll();
			
			for (var i = 0; i < _info.TimedBoxExtra.Count; i++)
			{
				_rewardPool.Spawn().Initialise(_info.TimedBoxExtra[i].Config.LootBoxId, 1);
			}
		}
		
		private void OnConfirmPressed()
		{
			if (_gameDataProvider.CurrencyDataProvider.Currencies[GameId.HC] < _info.GetUnlockExtraBoxesCost(Services.TimeService.DateTimeUtcNow))
			{
				ShowNotEnoughGemsDialog();
			}
			else
			{
				Data.SpeedUpAllBoxes.Invoke();
			}
		}
		
		private void ShowNotEnoughGemsDialog()
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = Services.GenericDialogService.CloseDialog
			};

			Services.GenericDialogService.OpenDialog(ScriptLocalization.General.NotEnoughGems, false, confirmButton);
		}

		private void OnDeclinePressed()
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = OnDeclineConfirmed
			};

			Services.GenericDialogService.OpenDialog(ScriptLocalization.MainMenu.NotEnoughSpaceWarning, true, confirmButton);
		}

		private void OnDeclineConfirmed()
		{
			Services.GenericDialogService.CloseDialog();
			Data.CloseClicked.Invoke();
		}
	}
}