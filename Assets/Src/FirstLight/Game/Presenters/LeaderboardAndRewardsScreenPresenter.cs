using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the Leaderboards and Rewards Screen
	/// </summary>
	public class LeaderboardAndRewardsScreenPresenter : UiToolkitPresenterData<LeaderboardAndRewardsScreenPresenter.StateData>
	{
		private const string UssPlayerName = "player-name";
		private const string UssFirst = UssPlayerName + "--first";
		private const string UssSecond = UssPlayerName + "--second";
		private const string UssThird = UssPlayerName + "--third";
		private const string UssSpectator = "spectator";
		
		[SerializeField] private BaseCharacterMonoComponent _character;
		[SerializeField] private Camera _camera;
		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;
		
		public struct StateData
		{
			public Action ContinueClicked;
		}

		private IMatchServices _matchServices;
		private IGameDataProvider _gameDataProvider;

		private Button _nextButton;
		private Label _nextButtonLabel;
		private VisualElement _leaderboardPanel;
		private ScrollView _leaderboardScrollView;
		private VisualElement _playerName;
		private Label _playerNameText;
		private VisualElement _rewardsPanel;
		private VisualElement _craftSpice;
		private VisualElement _trophies;
		private VisualElement _bpp;

		private RewardPanelView _craftSpiceView;
		private RewardPanelView _trophiesView;
		private RewardBPPanelView _bppView;

		private bool _showingLeaderboards;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}
		
		protected override void OnOpened()
		{
			base.OnOpened();

			SetupCamera();
			UpdateCharacter();
			UpdatePlayerName();
			UpdateLeaderboard();
			UpdateRewards();
			ShowLeaderboards();
		}

		protected override void OnTransitionsReady()
		{
			SetupCamera();
		}

		protected override void QueryElements(VisualElement root)
		{
			_nextButton = root.Q<Button>("NextButton").Required();
			_nextButton.clicked += OnNextButtonClicked;
			_nextButtonLabel = _nextButton.Q<Label>("Label");
			
			_leaderboardPanel = root.Q<VisualElement>("LeaderboardPanel").Required();
			_leaderboardScrollView = root.Q<ScrollView>("LeaderboardScrollView").Required();

			_playerName = root.Q<VisualElement>("PlayerName").Required();
			_playerNameText = _playerName.Q<Label>("Text").Required();

			_rewardsPanel = root.Q<VisualElement>("RewardsPanel").Required();
			_craftSpice = _rewardsPanel.Q<VisualElement>("CraftSpice").Required();
			_craftSpice.AttachView(this, out _craftSpiceView);
			_trophies = _rewardsPanel.Q<VisualElement>("Trophies").Required();
			_trophies.AttachView(this, out _trophiesView);
			_bpp = _rewardsPanel.Q<VisualElement>("BPP").Required();
			_bpp.AttachView(this, out _bppView);
		}

		private void OnNextButtonClicked()
		{
			if (_showingLeaderboards && _gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0)
			{
				ShowRewards();
			}
			else
			{
				Data.ContinueClicked();
			}
		}

		private void ShowLeaderboards()
		{
			_showingLeaderboards = true;
			_nextButtonLabel.text = (_gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0) ? "NEXT" : "EXIT";
			_leaderboardPanel.style.display = DisplayStyle.Flex;
			_rewardsPanel.style.display = DisplayStyle.None;
		}
		
		private void ShowRewards()
		{
			_showingLeaderboards = false;
			_nextButtonLabel.text = "EXIT";
			_leaderboardPanel.style.display = DisplayStyle.None;
			_rewardsPanel.style.display = DisplayStyle.Flex;
		}

		private void UpdateRewards()
		{
			var rewards = ProcessRewards();
			
			// craft spice
			var csReward = 0;
			if (rewards.ContainsKey(GameId.CS))
			{
				csReward = rewards[GameId.CS];
			}
			_craftSpiceView.SetData(csReward, (int)_matchServices.MatchEndDataService.CSBeforeChange);

			// Trophies
			var trophiesReward = 0;
			if (rewards.ContainsKey(GameId.Trophies))
			{
				trophiesReward = rewards[GameId.Trophies];
			}
			_trophiesView.SetData(trophiesReward, (int)_matchServices.MatchEndDataService.TrophiesBeforeChange);
			
			// BPP
			var bppReward = 0;
			if (rewards.ContainsKey(GameId.BPP))
			{
				bppReward = rewards[GameId.BPP];
			}
			var maxLevel = _gameDataProvider.BattlePassDataProvider.MaxLevel;
			var nextLevel = Math.Clamp(_matchServices.MatchEndDataService.BPLevelBeforeChange + 1, 0, maxLevel) + 1;
			var bppPoolInfo = _gameDataProvider.ResourceDataProvider.GetResourcePoolInfo(GameId.BPP);
			_bppView.SetData(bppReward, (int)_matchServices.MatchEndDataService.BPPBeforeChange,
				(int)_gameDataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int)nextLevel), (int)nextLevel,
				(int)bppPoolInfo.CurrentAmount, (int)bppPoolInfo.PoolCapacity);
		}

		private void UpdatePlayerName()
		{
			if (_matchServices.MatchEndDataService.LocalPlayer == PlayerRef.None)
			{
				_playerNameText.text = "";
				return;
			}
			
			// Cleanup in case the screen is re-used
			_playerName.RemoveModifiers();
			
			var playerData = _matchServices.MatchEndDataService.PlayerMatchData;
			var localPlayerData = playerData[_matchServices.MatchEndDataService.LocalPlayer];

			_playerNameText.text = "";
			
			// If the player is in the top 3 we show a badge
			if (localPlayerData.QuantumPlayerMatchData.PlayerRank <= 3)
			{
				var rankClass = localPlayerData.QuantumPlayerMatchData.PlayerRank switch
				{
					1 => UssFirst,
					2 => UssSecond,
					3 => UssThird,
					_ => ""
				};
				_playerName.AddToClassList(rankClass);
			}
			else
			{
				_playerNameText.text = localPlayerData.QuantumPlayerMatchData.PlayerRank + ". ";
			}

			_playerNameText.text += localPlayerData.QuantumPlayerMatchData.PlayerName;
		}

		private void UpdateLeaderboard()
		{
			if (_matchServices.MatchEndDataService.LocalPlayer == PlayerRef.None)
			{
				Root.AddToClassList(UssSpectator);
			}

			var entries = _matchServices.MatchEndDataService.QuantumPlayerMatchData;

			foreach (var entry in entries)
			{
				var newEntry = _leaderboardEntryAsset.Instantiate();
				newEntry.AttachView(this, out LeaderboardEntryView view);
				view.SetData(entry, _matchServices.MatchEndDataService.LocalPlayer == entry.Data.Player);
				_leaderboardScrollView.Add(newEntry);
			}
		}
		
		private Dictionary<GameId, int> ProcessRewards()
		{
			var dictionary = new Dictionary<GameId, int>();
			var rewards = _matchServices.MatchEndDataService.Rewards;

			for (var i = 0; i < rewards.Count; i++)
			{
				var id = rewards[i].RewardId;

				if (!dictionary.ContainsKey(id))
				{
					dictionary.Add(id, 0);
				}

				dictionary[id] += rewards[i].Value;
			}

			return dictionary;
		}

		private void SetupCamera()
		{
			_camera.gameObject.SetActive(true);
			_camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(19f, 2.17f);
		}
		
		private async void UpdateCharacter()
		{
			if (_matchServices.MatchEndDataService.LocalPlayer == PlayerRef.None)
			{
				_character.gameObject.SetActive(false);
				return;
			}
			
			var playerData =
				_matchServices.MatchEndDataService.PlayerMatchData[_matchServices.MatchEndDataService.LocalPlayer];
			
			await _character.UpdateSkin(playerData.QuantumPlayerMatchData.Data.PlayerSkin, playerData.Gear.ToList());
		}
	}
}