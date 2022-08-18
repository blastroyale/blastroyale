using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Quantum;
using Quantum.Commands;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// The screen presenter is responsible for:
	/// - Show the local player's state when he dies in the deathmatch
	/// - Show all players performance that are playing the match
	/// - Respawn the player back again into action
	/// </summary>
	public class DeathmatchDeadScreenPresenter : AnimatedUiPresenterData<DeathmatchDeadScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnRespawnClicked;
			public Dictionary<PlayerRef, Pair<int, int>> KillerData;
		}
		
		[SerializeField, Required] private Button _button;
		[SerializeField, Required] private Button _respawnButton;
		[SerializeField, Required] private TextMeshProUGUI _fraggedByText;
		[SerializeField, Required] private TextMeshProUGUI _reviveTimeLeftText;
		[SerializeField, Required] private Slider _respawnSlider;
		[SerializeField, Required] private StandingsHolderView _standings;
		[SerializeField, Required] private GameObject _killTrackerHolder;
		[SerializeField, Required] private TextMeshProUGUI _playerNameText;
		[SerializeField, Required] private TextMeshProUGUI _enemyNameText;
		[SerializeField, Required] private TextMeshProUGUI _playerScoreText;
		[SerializeField, Required] private TextMeshProUGUI _enemyScoreText;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_button.onClick.AddListener(OnExitGamePressed);
			_respawnButton.onClick.AddListener(OnRespawnPressed);
			_respawnButton.gameObject.SetActive(false);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var localPlayer = game.GetLocalPlayers()[0];
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out _);
			var deadPlayer = frame.Get<DeadPlayerCharacter>(playerData[localPlayer].Data.Entity);
			var killerMatchData = playerData[deadPlayer.Killer];
			var localName = _gameDataProvider.AppDataProvider.Nickname;
			var isSuicide = localPlayer == deadPlayer.Killer;
			
			_killTrackerHolder.SetActive(!isSuicide);
			_standings.Initialise(playerData.Count, false, false);
			_standings.UpdateStandings(playerData, localPlayer);

			if (isSuicide)
			{
				_fraggedByText.text = ScriptLocalization.AdventureMenu.ChooseDeath;
			}
			else
			{
				var killerPlayerName = killerMatchData.GetPlayerName();
				_fraggedByText.text = string.Format(ScriptLocalization.AdventureMenu.FraggedBy, killerPlayerName);
				_playerNameText.text = localName;
				_enemyNameText.text = killerPlayerName;
				_playerScoreText.text = Data.KillerData[killerMatchData.Data.Player].Key.ToString();
				_enemyScoreText.text = Data.KillerData[killerMatchData.Data.Player].Value.ToString();
			}

			StartCoroutine(TimeUpdateCoroutine());
		}

		private void OnExitGamePressed()
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = OnExitConfirmed
			};

			_services.GenericDialogService.OpenDialog(ScriptLocalization.General.ConfirmQuit, true, confirmButton);
		}

		private void OnExitConfirmed()
		{
			_services.MessageBrokerService.Publish(new QuitGameClickedMessage());
		}

		private IEnumerator TimeUpdateCoroutine()
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var totalForceTimeFp = config.PlayerForceRespawnTime - config.PlayerRespawnTime;
			var totalForceTime = totalForceTimeFp.AsFloat;

			_respawnButton.gameObject.SetActive(false);

			yield return new WaitForSeconds(config.PlayerRespawnTime.AsFloat);

			var endTime = Time.time + totalForceTime;

			_respawnSlider.value = 0;
			_respawnButton.gameObject.SetActive(true);

			while (Time.time < endTime)
			{
				_respawnSlider.value = 1 - (endTime - Time.time) / totalForceTime;

				yield return null;
			}

			_respawnSlider.value = 1f;

			OnRespawnPressed();
		}

		private void OnRespawnPressed()
		{
			_respawnButton.gameObject.SetActive(false);
			Data.OnRespawnClicked();
			QuantumRunner.Default.Game.SendCommand(new PlayerRespawnCommand());
		}
	}
}