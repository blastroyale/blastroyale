using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEditor.Graphs;
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
		private const string BADGE_SPRITE_PREFIX = "BadgePlace";

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
		private Label _fameTitle;
		private Label _playerName;
		private VisualElement _rewardsPanel;
		private VisualElement _trophies;
		private VisualElement _bpp;
		private VisualElement _fame;

		private RewardPanelView _trophiesView;
		private RewardLevelPanelView _bppView;
		private RewardLevelPanelView _levelView;

		private ScreenHeaderElement _header;

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
			base.QueryElements(root);

			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += OnNextButtonClicked;

			_nextButton = root.Q<Button>("NextButton").Required();
			_nextButton.clicked += OnNextButtonClicked;

			_leaderboardPanel = root.Q<VisualElement>("LeaderboardPanel").Required();
			_leaderboardScrollView = root.Q<ScrollView>("LeaderboardScrollView").Required();


			_rewardsPanel = root.Q<VisualElement>("RewardsPanel").Required();
			_trophies = _rewardsPanel.Q<VisualElement>("Trophies").Required();
			_trophies.AttachView(this, out _trophiesView);
			_bpp = _rewardsPanel.Q<VisualElement>("BPP").Required();
			_bpp.AttachView(this, out _bppView);

			_fame = _rewardsPanel.Q<VisualElement>("Fame").Required();
			_fame.AttachView(this, out _levelView);
			_levelView.HideFinalLevel();
			_fameTitle = root.Q<Label>("FameTitle").Required();

			root.Q<PlayerAvatarElement>("Avatar").Required().SetLocalPlayerData(_gameDataProvider, _gameServices);
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

			AnimatePanels().Forget();
		}

		private async UniTaskVoid AnimatePanels()
		{
			await UniTask.Delay(300);
			await _levelView.Animate();
			await _trophiesView.Animate();
			await _bppView.Animate();
		}

		private void UpdateRewards()
		{
			var rewards = ProcessRewards();

			// Trophies
			var trophiesReward = 0;
			if (rewards.TryGetValue(GameId.Trophies, out var r))
			{
				trophiesReward = r;
			}

			_trophiesView.SetData(trophiesReward, (int) _matchServices.MatchEndDataService.TrophiesBeforeChange);

			// BPP
			SetBPPReward(rewards);

			// Level (Fame)
			SetLevelReward(rewards);
		}

		private void SetLevelReward(Dictionary<GameId, int> rewards)
		{
			var xpRewards = 0;
			if (rewards.TryGetValue(GameId.XP, out var reward))
			{
				xpRewards = reward;
			}

			var maxLevel = GameConstants.Data.PLAYER_FAME_MAX_LEVEL;
			var gainedLeft = xpRewards;
			var levelsInfo = new List<RewardLevelPanelView.LevelLevelRewardInfo>();
			var nextLevel = (uint) Math.Clamp(_gameDataProvider.PlayerDataProvider.Level.Value, 1, maxLevel);
			var currentLevel = nextLevel;
			//var configs = _gameServices.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();

			do
			{
				var levelRewardInfo = new RewardLevelPanelView.LevelLevelRewardInfo();

				levelRewardInfo.MaxLevel = (int) maxLevel;

				// If it's the next level to the current one, we might have already some points in there
				if (nextLevel == currentLevel)
				{
					levelRewardInfo.Start = (int) _matchServices.MatchEndDataService.XPBeforeChange;
				}

				levelRewardInfo.MaxForLevel = (int) _gameDataProvider.PlayerDataProvider.GetXpNeededForLevel(currentLevel);
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

			_playerName = new Label();
			_playerName.AddToClassList(UIConstants.USS_PLAYER_LABEL);
			Root.Add(_playerName);
			if (playerRef == PlayerRef.None)
			{
				return;
			}


			var playerData = _matchServices.MatchEndDataService.PlayerMatchData;
			var localPlayerData = playerData[playerRef];


			string playerPrefix;
			// If the player is in the top 3 we show a badge
			if (localPlayerData.QuantumPlayerMatchData.PlayerRank <= 3)
			{
				playerPrefix = $"<sprite name=\"{BADGE_SPRITE_PREFIX + localPlayerData.QuantumPlayerMatchData.PlayerRank}\"> ";
			}
			else
			{
				playerPrefix = localPlayerData.QuantumPlayerMatchData.PlayerRank + ". ";
			}

			var rankColor = _gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked, (int) localPlayerData.QuantumPlayerMatchData.LeaderboardRank);
			var playerName = localPlayerData.QuantumPlayerMatchData.GetPlayerName();
			_playerName.text = playerPrefix + playerName;
			_fameTitle.text = playerName;
			_fameTitle.style.color = rankColor;
		}

		private void UpdateLeaderboard()
		{
			var entries = _matchServices.MatchEndDataService.QuantumPlayerMatchData;

			entries.SortByPlayerRank(false);

			foreach (var entry in entries)
			{
				var newEntry = _leaderboardEntryAsset.Instantiate();
				var borderColor = _gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked, (int) entry.LeaderboardRank);
				newEntry.AttachView(this, out LeaderboardEntryView view);
				view.SetData((int) entry.PlayerRank, entry.GetPlayerName(), (int) entry.Data.PlayersKilledCount,
					(int) entry.Data.PlayerTrophies,
					_matchServices.MatchEndDataService.LocalPlayer == entry.Data.Player, entry.AvatarUrl, null, borderColor);
				_leaderboardScrollView.Add(newEntry);
			}
		}

		private Dictionary<GameId, int> ProcessRewards()
		{
			var dictionary = new Dictionary<GameId, int>();
			var rewards = _matchServices.MatchEndDataService.Rewards;
			for (var i = 0; i < rewards.Count; i++)
			{
				var id = rewards[i].Id;
				if (!rewards[i].TryGetMetadata<CurrencyMetadata>(out var meta)) continue;
				if (!dictionary.ContainsKey(id))
				{
					dictionary.Add(id, 0);
				}

				dictionary[id] += meta.Amount;
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

			var skinId = _gameServices.CollectionService.GetCosmeticForGroup(playerData.Cosmetics, GameIdGroup.PlayerSkin);
			await _character.UpdateSkin(skinId, playerData.Gear.ToList());

			var targetPosition = _character.transform.position;
			var initialPosition = targetPosition;
			initialPosition.x += 20f;
			_character.transform.position = initialPosition;

			_character.transform.DOMove(targetPosition, 0.4f).SetEase(Ease.Linear).onUpdate += OnUpdateCharacterPosition;
		}


		private void OnUpdateCharacterPosition()
		{
			_playerName.SetPositionBasedOnWorldPosition(_character.transform.position);
		}
	}
}