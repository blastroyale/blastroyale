using System;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	public class BattlePassScreenPresenter : UiPresenterData<BattlePassScreenPresenter.StateData>
	{
		[SerializeField, Required] private Button _backButton;
		[SerializeField, Required] private TextMeshProUGUI _currentLevel;
		[SerializeField, Required] private TextMeshProUGUI _nextLevel;
		[SerializeField, Required] private TextMeshProUGUI _progressText;
		[SerializeField, Required] private Image _progressBar;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		public struct StateData
		{
			public Action BackClicked;
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_backButton.onClick.AddListener(OnBackClicked);
		}

		protected override void OnOpened()
		{
			_services.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);

			if (_gameDataProvider.BattlePassDataProvider.IsRedeemable(out _))
			{
				_services.CommandService.ExecuteCommand(new RedeemBPPCommand());
			}
		}

		protected override void OnClosed()
		{
			_services.MessageBrokerService.Unsubscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);
		}

		private void OnBackClicked()
		{
			Data.BackClicked();
		}

		private void OnBattlePassLevelUp(BattlePassLevelUpMessage message)
		{
			FLog.Info("PACO", $"BattlePass leveled up with rewards({string.Join(",", message.Rewards.Select(r => r.Reward.GameId.ToString()).ToList())}) to level({message.newLevel})");
		}
	}
}