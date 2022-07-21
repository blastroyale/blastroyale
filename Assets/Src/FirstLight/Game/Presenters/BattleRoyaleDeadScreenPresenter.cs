using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// The screen presenter is responsible for:
	/// - Show the local player's state when he dies in the battle royale game
	/// - Show the player's state that is currently spectating
	/// - Leave the match
	/// </summary>
	public class BattleRoyaleDeadScreenPresenter : AnimatedUiPresenterData<BattleRoyaleDeadScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnLeaveClicked;
			public Action OnSpectateClicked;
			public PlayerRef Killer;
		}

		[SerializeField, Required] private Button _leaveButton;
		[SerializeField, Required] private Button _spectateButton;
		[SerializeField, Required] private TextMeshProUGUI _killerText;

		private IGameServices _services;

		private void Awake()
		{
			_leaveButton.onClick.AddListener(OnLeavePressed);
			_spectateButton.onClick.AddListener(OnSpectatePressed);
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			if (Data.Killer != PlayerRef.None)
			{
				var f = QuantumRunner.Default.Game.Frames.Verified;
				var playersData = f.GetSingleton<GameContainer>().PlayersData;
				var data = new QuantumPlayerMatchData(f, playersData[Data.Killer]);
				_killerText.text = string.Format(ScriptLocalization.AdventureMenu.FraggedBy, data.GetPlayerName());
			}
			else
			{
				_killerText.text = ScriptLocalization.AdventureMenu.YouDied;
			}
		}

		private void OnLeavePressed()
		{
			Data.OnLeaveClicked();
		}

		private void OnSpectatePressed()
		{
			Data.OnSpectateClicked();
		}
	}
}