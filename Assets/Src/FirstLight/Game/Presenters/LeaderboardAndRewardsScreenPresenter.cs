using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the Leaderboards and Rewards Screen
	/// </summary>
	public class LeaderboardAndRewardsScreenPresenter :
		UiToolkitPresenterData<LeaderboardAndRewardsScreenPresenter.StateData>
	{
		private const string UssPlayerName = "player-name";
		private const string UssFirst = UssPlayerName + "--first";
		private const string UssSecond = UssPlayerName + "--second";
		private const string UssThird = UssPlayerName + "--third";
		private const string UssSpectator = "spectator";

		[SerializeField] private BaseCharacterMonoComponent _character;
		[SerializeField] private CinemachineVirtualCamera _camera;
		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;

		public struct StateData
		{
			public Action ContinueClicked;
		}

		private IMatchServices _matchServices;
		private IGameServices _gameServices;
		private IGameDataProvider _gameDataProvider;

		private Button _nextButton;
		private VisualElement _leaderboardPanel;
		private ScrollView _leaderboardScrollView;
		private VisualElement _playerName;
		private Label _playerNameText;
		private Label _fameTitle;
		private VisualElement _rewardsPanel;
		private VisualElement _craftSpice;
		private VisualElement _trophies;
		private VisualElement _bpp;
		private VisualElement _fame;

		private RewardPanelView _craftSpiceView;
		private RewardPanelView _trophiesView;
		private RewardLevelPanelView _bppView;
		private RewardLevelPanelView _levelView;

		private bool _showingLeaderboards;

		protected override void OnInitialized()
		{
			base.OnInitialized();

			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_gameServices = MainInstaller.Resolve<IGameServices>();
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

			_fame = _rewardsPanel.Q<VisualElement>("Fame").Required();
			_fame.AttachView(this, out _levelView);
			_fameTitle = root.Q<Label>("FameTitle").Required();
			
			root.Q<PlayerAvatarElement>("Avatar").Required().SetLocalPlayerData(_gameDataProvider);
		}

		private void OnNextButtonClicked()
		{
			if (_showingLeaderboards)
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
			_nextButton.text = ScriptLocalization.UITShared.next;
			_leaderboardPanel.style.display = DisplayStyle.Flex;
			_rewardsPanel.style.display = DisplayStyle.None;
		}

		private void ShowRewards()
		{
			_rewardsPanel.style.display = DisplayStyle.Flex;
			_leaderboardPanel.AddToClassList("hidden-right");
			_rewardsPanel.RemoveFromClassList("rewards-panel--hidden-start");
			_showingLeaderboards = false;
			_nextButton.text = ScriptLocalization.UITShared.leave;

			AnimatePanels();
		}

		private async void AnimatePanels()
		{
			await Task.Delay(400);
			await _craftSpiceView.Animate();
			await _trophiesView.Animate();
			await _bppView.Animate();
			await _levelView.Animate();
		}

		private void UpdateRewards()
		{
			var rewards = ProcessRewards();

			// craft spice
			var csReward = 0;
			if (rewards.TryGetValue(GameId.CS, out var reward))
			{
				csReward = reward;
			}

			_craftSpiceView.SetData(csReward, (int) _matchServices.MatchEndDataService.CSBeforeChange);

			// Trophies
			var trophiesReward = 0;
			if (rewards.TryGetValue(GameId.Trophies, out var r))
			{
				trophiesReward = r;
			}

			_trophiesView.SetData(trophiesReward, (int) _matchServices.MatchEndDataService.TrophiesBeforeChange);

			FLog.Info("PACO", "SetLevelData1");
			// BPP
			SetBPPReward(rewards);

			FLog.Info("PACO", "SetLevelData2");
			// Level (Fame)
			SetLevelReward(rewards);
			FLog.Info("PACO", "SetLevelData3");
		}

		private void SetLevelReward(Dictionary<GameId, int> rewards)
		{
			var bppReward = 0;
			FLog.Info("PACO", "SetLevelReward1");

			if (rewards.TryGetValue(GameId.XP, out var reward))
			{
				bppReward = reward;
			}
			
			FLog.Info("PACO", "SetLevelReward2");

			var maxLevel = 99;
			var gainedLeft = bppReward;
			var levelsInfo = new List<RewardLevelPanelView.LevelLevelRewardInfo>();
			var nextLevel = (int) Math.Clamp(_matchServices.MatchEndDataService.LevelBeforeChange, 0, maxLevel);
			var currentLevel = nextLevel;

			do
			{
				FLog.Info("PACO", "SetLevelReward: nextLevel: " + nextLevel + " currentLevel: " + currentLevel + " gainedLeft: " + gainedLeft);
				var levelRewardInfo = new RewardLevelPanelView.LevelLevelRewardInfo();

				levelRewardInfo.MaxLevel = 99;

				// If it's the next level to the current one, we might have already some points in there
				if (nextLevel == currentLevel)
				{
					levelRewardInfo.Start = (int) _matchServices.MatchEndDataService.LevelBeforeChange;
				}

				levelRewardInfo.MaxForLevel =
					(int) _gameServices.ConfigsProvider.GetConfig<PlayerLevelConfig>(currentLevel).LevelUpXP;
				levelRewardInfo.NextLevel = currentLevel;

				var amountToMax = levelRewardInfo.MaxForLevel - levelRewardInfo.Start;
				if (amountToMax < gainedLeft)
				{
					levelRewardInfo.Total = amountToMax;
					gainedLeft -= amountToMax;
				}
				else
				{
					levelRewardInfo.Total = gainedLeft;
					gainedLeft = 0;
				}

				levelsInfo.Add(levelRewardInfo);

				currentLevel++;
			} while (gainedLeft > 0 && currentLevel < maxLevel);

			_levelView.SetData(levelsInfo);
		}

		private void SetBPPReward(Dictionary<GameId, int> rewards)
		{
			var bppReward = 0;
			if (rewards.ContainsKey(GameId.BPP))
			{
				bppReward = rewards[GameId.BPP];
			}

			var maxLevel = _gameDataProvider.BattlePassDataProvider.MaxLevel;
			var bppPoolInfo = _gameDataProvider.ResourceDataProvider.GetResourcePoolInfo(GameId.BPP);
			var gainedLeft = bppReward;
			var levelsInfo = new List<RewardLevelPanelView.LevelLevelRewardInfo>();
			var nextLevel = (int) Math.Clamp(_matchServices.MatchEndDataService.BPLevelBeforeChange + 1, 0, maxLevel);
			var currentLevel = nextLevel;

			do
			{
				var levelRewardInfo = new RewardLevelPanelView.LevelLevelRewardInfo();

				levelRewardInfo.MaxLevel = (int) maxLevel;

				// If it's the next level to the current one, we might have already some points in there
				if (nextLevel == currentLevel)
				{
					levelRewardInfo.Start = (int) _matchServices.MatchEndDataService.BPPBeforeChange;
				}

				levelRewardInfo.MaxForLevel =
					(int) _gameDataProvider.BattlePassDataProvider.GetRequiredPointsForLevel(currentLevel - 1);
				levelRewardInfo.NextLevel = (int) currentLevel;

				var amountToMax = levelRewardInfo.MaxForLevel - levelRewardInfo.Start;
				if (amountToMax < gainedLeft)
				{
					levelRewardInfo.Total = amountToMax;
					gainedLeft -= amountToMax;
				}
				else
				{
					levelRewardInfo.Total = gainedLeft;
					gainedLeft = 0;
				}

				levelsInfo.Add(levelRewardInfo);

				currentLevel++;
			} while (gainedLeft > 0 && currentLevel < maxLevel);

			_bppView.SetData(levelsInfo, (int) bppPoolInfo.CurrentAmount, (int) bppPoolInfo.PoolCapacity, bppPoolInfo);
		}

		private void UpdatePlayerName()
		{
			var playerRef = _matchServices.MatchEndDataService.LocalPlayer == PlayerRef.None
				? _matchServices.MatchEndDataService.Leader
				: _matchServices.MatchEndDataService.LocalPlayer;

			if (playerRef == PlayerRef.None)
			{
				_playerNameText.text = "";
				return;
			}

			// Cleanup in case the screen is re-used
			_playerName.RemoveModifiers();

			var playerData = _matchServices.MatchEndDataService.PlayerMatchData;
			var localPlayerData = playerData[playerRef];

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

			var playerName = localPlayerData.QuantumPlayerMatchData.GetPlayerName();
			_playerNameText.text += playerName;
			_fameTitle.text = playerName;
		}

		private void UpdateLeaderboard()
		{
			var entries = _matchServices.MatchEndDataService.QuantumPlayerMatchData;

			entries.SortByPlayerRank(false);

			foreach (var entry in entries)
			{
				// TODO: PFP
				var newEntry = _leaderboardEntryAsset.Instantiate();
				newEntry.AttachView(this, out LeaderboardEntryView view);
				view.SetData((int) entry.PlayerRank, entry.GetPlayerName(), (int) entry.Data.PlayersKilledCount,
					(int) entry.Data.PlayerTrophies,
					_matchServices.MatchEndDataService.LocalPlayer == entry.Data.Player, entry.AvatarUrl);
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
			// A very magic number that makes the character look good enough in any aspect ratio
			_camera.m_Lens.FieldOfView = Camera.HorizontalToVerticalFieldOfView(20f, _camera.m_Lens.Aspect);
		}

		private async void UpdateCharacter()
		{
			var playerRef = _matchServices.MatchEndDataService.LocalPlayer == PlayerRef.None
				? _matchServices.MatchEndDataService.Leader
				: _matchServices.MatchEndDataService.LocalPlayer;

			if (playerRef == PlayerRef.None)
			{
				_character.gameObject.SetActive(false);
				return;
			}

			if (!_matchServices.MatchEndDataService.PlayerMatchData.ContainsKey(playerRef))
			{
				return;
			}

			var playerData = _matchServices.MatchEndDataService.PlayerMatchData[playerRef];

			await _character.UpdateSkin(playerData.QuantumPlayerMatchData.Data.PlayerSkin, playerData.Gear.ToList());

			var targetPosition = _character.transform.position;
			var initialPosition = targetPosition;
			initialPosition.x += 20f;
			_character.transform.position = initialPosition;

			_character.transform.DOMove(targetPosition, 0.4f).SetEase(Ease.Linear);
		}
	}
}