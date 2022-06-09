using System;
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

		[FormerlySerializedAs("_button")] [SerializeField, Required]
		private Button leaveButton;

		[SerializeField, Required] private Button spectateButton;
		[SerializeField, Required] private TextMeshProUGUI killerText;

		private void Awake()
		{
			leaveButton.onClick.AddListener(OnLeavePressed);
			spectateButton.onClick.AddListener(OnSpectatePressed);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			var killerName = "Shrinking Circle";

			if (Data.Killer != PlayerRef.None)
			{
				var f = QuantumRunner.Default.Game.Frames.Verified;
				var playersData = f.GetSingleton<GameContainer>().PlayersData;
				var data = new QuantumPlayerMatchData(f, playersData[Data.Killer]);

				killerName = data.GetPlayerName();
			}

			killerText.text = string.Format(ScriptLocalization.AdventureMenu.FraggedBy, killerName);
			spectateButton.gameObject.SetActive(Data.Killer != PlayerRef.None);
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