using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Quantum;
using Quantum.Commands;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Adventure Spectator Hud
	/// </summary>
	///<remarks>
	/// Currently without any features but added so that the logic is in place to do so.
	/// </remarks>
	public class AdventureSpectatorHudPresenter : AnimatedUiPresenterData<AdventureSpectatorHudPresenter.StateData>
	{
		public struct StateData
		{
			public Dictionary<PlayerRef, Pair<int, int>> KillerData;
		}
		
		[SerializeField] private Button _button;
		[SerializeField] private Button _respawnButton;
		[SerializeField] private TextMeshProUGUI _fraggedByText;
		[SerializeField] private TextMeshProUGUI _reviveTimeLeftText;
		[SerializeField] private Slider _respawnSlider;
		[SerializeField] private StandingsHolderView _standings;

		[SerializeField] private GameObject _killTrackerHolder;
		[SerializeField] private TextMeshProUGUI _playerNameText;
		[SerializeField] private TextMeshProUGUI _enemyNameText;
		[SerializeField] private TextMeshProUGUI _playerScoreText;
		[SerializeField] private TextMeshProUGUI _enemyScoreText;
		
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
			var playerData = frame.GetSingleton<GameContainer>().PlayersData;
			var deadPlayer = frame.Get<DeadPlayerCharacter>(playerData[localPlayer].Entity);
			var killerMatchData = new QuantumPlayerMatchData(frame, playerData[deadPlayer.Killer]);
			var localName = _gameDataProvider.PlayerDataProvider.Nickname;
			
			_killTrackerHolder.SetActive(!killerMatchData.IsLocalPlayer);
			
			if (killerMatchData.IsLocalPlayer)
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

			ProcessResultScreenData(frame);
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

		private void ProcessResultScreenData(Frame f)
		{
			var container = f.GetSingleton<GameContainer>();
			var playerData = new List<QuantumPlayerMatchData>();

			for(var i = 0; i < container.PlayersData.Length; i++)
			{
				if (!container.PlayersData[i].IsValid)
				{
					continue;
				}
				
				var playerMatchData = new QuantumPlayerMatchData(f,container.PlayersData[i]);
				
				playerData.Add(playerMatchData);
			}
			
			_standings.Initialise(playerData, false, false);
		}

		private void OnRespawnPressed()
		{
			QuantumRunner.Default.Game.SendCommand(new PlayerRespawnCommand());
		}
	}
}