using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// The screen presenter is responsible for:
	/// - Show the local player's state when he dies in the battle royale game
	/// - Show the player's state that is currently spectating
	/// - Leave the match
	/// </summary>
	public class BattleRoyaleSpectatorHudPresenter : AnimatedUiPresenterData<BattleRoyaleSpectatorHudPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnLeaveClicked;
			public QuantumPlayerMatchData Killer;
		}
		
		[SerializeField] private Button _button;
		[SerializeField] private TextMeshProUGUI _killerText;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider; 

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_button.onClick.AddListener(OnLeavePressed);
		}
		protected override void OnOpened()
		{
			base.OnOpened();
			
			_killerText.text = string.Format(ScriptLocalization.AdventureMenu.FraggedBy, Data.Killer.GetPlayerName());
		}

		private void OnLeavePressed()
		{
			Data.OnLeaveClicked();
		}
	}
}