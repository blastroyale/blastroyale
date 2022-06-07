using System;
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
		
		[SerializeField, Required] private Button _battleRoyaleButton;
		[SerializeField, Required] private Button _deathmatchButton;
		[SerializeField, Required] private Button _backButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			_battleRoyaleButton.onClick.AddListener(BattleRoyaleClicked);
			_deathmatchButton.onClick.AddListener(DeathmatchClicked);
			_backButton.onClick.AddListener(BackButton);
		}

		private void DeathmatchClicked()
		{
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.Deathmatch;
			Data.GameModeChosen();
		}

		private void BattleRoyaleClicked()
		{
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.BattleRoyale;
			Data.GameModeChosen();
		}

		private void BackButton()
		{
			Data.GameModeChosen();
		}
		
		
	}
}