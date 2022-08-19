using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using Quantum;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles game mode selector
	/// </summary>
	public class GameModeSelectionPresenter : AnimatedUiPresenterData<GameModeSelectionPresenter.StateData>
	{
		public struct StateData
		{
			public Action GameModeChosen;
		}
		
		[SerializeField, Required] private Button _casualBrButton;
		[SerializeField, Required] private Button _rankedBrButton;
		[SerializeField, Required] private Button _dmButton;
		[SerializeField, Required] private Button _backButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			_casualBrButton.onClick.AddListener(CasualBattleRoyaleClicked);
			_rankedBrButton.onClick.AddListener(RankedBattleRoyaleClicked);
			_dmButton.onClick.AddListener(DeathmatchClicked);
			_backButton.onClick.AddListener(OnBlockerButtonPressed);
		}

		private void DeathmatchClicked()
		{
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.Deathmatch;
			_gameDataProvider.AppDataProvider.SelectedMatchType.Value = MatchType.Casual;
			Data.GameModeChosen();
		}

		private void CasualBattleRoyaleClicked()
		{
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.BattleRoyale;
			_gameDataProvider.AppDataProvider.SelectedMatchType.Value = MatchType.Casual;
			Data.GameModeChosen();
		}
		
		private void RankedBattleRoyaleClicked()
		{
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.BattleRoyale;
			_gameDataProvider.AppDataProvider.SelectedMatchType.Value = MatchType.Ranked;
			Data.GameModeChosen();
		}

		private void OnBlockerButtonPressed()
		{
			Data.GameModeChosen();
		}
	}
}