using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Collection;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using QuickEye.UIToolkit;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the Leaderboards and Rewards Screen
	/// </summary>
	public class LeaderboardAndRewardsScreenPresenter :
		UIPresenterData<LeaderboardAndRewardsScreenPresenter.StateData>
	{
		private const string BADGE_SPRITE_PREFIX = "BadgePlace";

		[SerializeField] private BaseCharacterMonoComponent _character;
		[SerializeField] private CinemachineVirtualCamera _camera;
		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;
		[SerializeField, Required] private VisualTreeAsset _playerNameTemplate;

		public class StateData
		{
			public Action ContinueClicked;
		}

		private IMatchServices _matchServices;
		private IGameServices _gameServices;
		private ICollectionService _collectionService;
		private IGameDataProvider _gameDataProvider;

		[Q("RewardsPanel")] private VisualElement _rewardPanel;
		private LocalizedButton _nextButton;
		private VisualElement _leaderboardPanel;
		private ScrollView _leaderboardScrollView;
		private Label _fameTitle;
		private VisualElement _worldPositioning;
		private VisualElement _rewardsPanel;
		private VisualElement _trophies;
		private VisualElement _bpp;
		private VisualElement _fame;

		private FoundOnMapView _foundMapView;
		private RewardPanelView _trophiesView;
		private RewardLevelPanelView _bppView;
		private RewardLevelPanelView _levelView;

		private ScreenHeaderElement _header;

		private bool _showingLeaderboards;

		private void Awake()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_gameServices = MainInstaller.Resolve<IGameServices>();
			_collectionService = _gameServices.CollectionService;
		}

		protected override async UniTask OnScreenOpen(bool reload)
		{
			UpdateLeaderboard();
			UpdateRewards();
			ShowLeaderboards();
			await UpdateCharacter();
			await base.OnScreenOpen(reload);
		}

		protected override void QueryElements()
		{
			_worldPositioning = Root.Q<VisualElement>("WorldPositioning");
			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			_header.SetButtonsVisibility(false);

			_nextButton = Root.Q<LocalizedButton>("NextButton").Required();
			_nextButton.clicked += OnNextButtonClicked;

			_leaderboardPanel = Root.Q<VisualElement>("LeaderboardPanel").Required();
			_leaderboardScrollView = Root.Q<ScrollView>("LeaderboardScrollView").Required();

			_rewardsPanel = Root.Q<VisualElement>("RewardsPanel").Required();
			_trophies = _rewardsPanel.Q<VisualElement>("Trophies").Required();
			_trophies.AttachView(this, out _trophiesView);

			_rewardsPanel.Q("FoundMap").AttachView(this, out _foundMapView);

			_bpp = _rewardsPanel.Q<VisualElement>("BPP").Required();
			_bpp.AttachView(this, out _bppView);

			_fame = _rewardsPanel.Q<VisualElement>("Fame").Required();
			_fame.AttachView(this, out _levelView);
			_levelView.HideFinalLevel();
			_fameTitle = Root.Q<Label>("FameTitle").Required();

			Root.Q<PlayerAvatarElement>("Avatar").Required().SetLocalPlayerData(_gameDataProvider, _gameServices);
		}

		private void OnNextButtonClicked()
		{
			if (_showingLeaderboards && !_matchServices.MatchEndDataService.JoinedAsSpectator)
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
			await _foundMapView.Animate();
			// animate other two views
		}

		private void UpdateRewards()
		{
			// TODO: Fix for reconnection
			if (_matchServices.MatchEndDataService.CachedRewards == null)
			{
				FLog.Warn("For some reason CachedRewards was null");
				return;
			}

			if (_matchServices.MatchEndDataService.JoinedAsSpectator)
			{
				_trophiesView.Element.SetVisibility(false);
				_bppView.Element.SetVisibility(false);
				_levelView.Element.SetVisibility(false);
				_foundMapView.Element.SetVisibility(false);
				return;
			}

			var allRewards = _matchServices.MatchEndDataService.CachedRewards?.ReceivedInCommand;
			var rewards = ProcessRewards();

			// Trophies
			var trophiesReward = 0;
			if (rewards.TryGetValue(GameId.Trophies, out var r))
			{
				trophiesReward = r;
			}

			_trophiesView.SetData(trophiesReward, (int) _matchServices.MatchEndDataService.CachedRewards.Before.Trophies);

			// BPP
			SetBPPReward(rewards);

			// Level (Fame)
			SetLevelReward(rewards);

			if (allRewards != null && allRewards.CollectedRewards.Count > 0)
			{
				var rewardArray = allRewards.CollectedRewards.Select(kp => (kp.Key, (ushort) kp.Value)).ToArray();
				_foundMapView.SetRewards(allRewards);
			}
			else
			{
				_foundMapView.SetRewards(null);
			}
			
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

			do
			{
				var levelRewardInfo = new RewardLevelPanelView.LevelLevelRewardInfo();

				levelRewardInfo.MaxLevel = (int) maxLevel;

				// If it's the next level to the current one, we might have already some points in there
				if (nextLevel == currentLevel)
				{
					levelRewardInfo.Start = (int) _matchServices.MatchEndDataService.CachedRewards.Before.Xp;
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
			var (bpLevelBefore, bpPointsBefore) = _matchServices.MatchEndDataService.CachedRewards.BattlePassBefore;
			var nextLevel = (int) Math.Clamp(bpLevelBefore + 1, 0, maxLevel);
			var currentLevel = nextLevel;

			do
			{
				var levelRewardInfo = new RewardLevelPanelView.LevelLevelRewardInfo();

				levelRewardInfo.MaxLevel = (int) maxLevel;

				// If it's the next level to the current one, we might have already some points in there
				if (nextLevel == currentLevel)
				{
					levelRewardInfo.Start = (int) bpPointsBefore;
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

			var rankColor = _gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked,
				(int) localPlayerData.QuantumPlayerMatchData.LeaderboardRank);
			var playerName = localPlayerData.QuantumPlayerMatchData.GetPlayerName();
			var playerNameEl = _playerNameTemplate.CloneTree();
			playerNameEl.AttachView<VisualElement, PlayerNameView>(this, out var view);
			_worldPositioning.Add(playerNameEl);
			view.SetLocal(playerPrefix + playerName, rankColor);
			_fameTitle.text = playerName;
			_fameTitle.style.color = rankColor;
			playerNameEl.ListenOnce<GeometryChangedEvent>(() =>
			{
				playerNameEl.SetPositionBasedOnWorldPosition(_character.Anchor);
			});
		}

		private void UpdateLeaderboard()
		{
			var entries = _matchServices.MatchEndDataService.QuantumPlayerMatchData;

			entries.SortByPlayerRank(false);

			foreach (var playerEntry in entries)
			{
				if (!playerEntry.Data.Player.IsValid)
				{
					continue;
				}
				var borderColor =
					_gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked, (int) playerEntry.LeaderboardRank);

				var leaderboardEntry = _leaderboardEntryAsset.Instantiate();
				leaderboardEntry.AttachView(this, out LeaderboardEntryView leaderboardEntryView);
				var isLocal = _matchServices.MatchEndDataService.LocalPlayer == playerEntry.Data.Player;
				leaderboardEntryView.SetData((int) playerEntry.PlayerRank, playerEntry.GetPlayerName(), (int) playerEntry.Data.DamageDone,
											 (int) playerEntry.Data.PlayersKilledCount, (int) playerEntry.Data.PlayerTrophies, isLocal, null, borderColor);
				if (!isLocal)
				{
					leaderboardEntryView.SetOptions((el) =>
					{
						_gameServices.GameSocialService.OpenPlayerOptions(el, Root, playerEntry.UnityId, playerEntry.GetPlayerName());
					});
				}

				var playersCosmetics = _matchServices.MatchEndDataService.PlayerMatchData[playerEntry.Data.Player].Cosmetics;
				ResolveLeaderboardEntryPlayerAvatar(playersCosmetics, leaderboardEntryView);

				_leaderboardScrollView.Add(leaderboardEntry);
			}
		}

		private void ResolveLeaderboardEntryPlayerAvatar(GameId[] playersCosmetics, LeaderboardEntryView leaderboardEntryView)
		{
			//Set to null while async loads the character skin avatar.
			leaderboardEntryView.SetLeaderboardEntryPFPSprite(null);

			_collectionService.LoadCollectionItemSprite(_collectionService.GetCosmeticForGroup(playersCosmetics, GameIdGroup.PlayerSkin))
				.ContinueWith(leaderboardEntryView.SetLeaderboardEntryPFPSprite).Forget();
		}

		private Dictionary<GameId, int> ProcessRewards()
		{
			var dictionary = new Dictionary<GameId, int>();
			var rewards = _matchServices.MatchEndDataService.CachedRewards?.ReceivedInCommand ?? new (); // TODO: Nullref on reconnection
			for (var i = 0; i < rewards.FinalRewards.Count; i++)
			{
				var id = rewards.FinalRewards[i].Id;
				if (!rewards.FinalRewards[i].TryGetMetadata<CurrencyMetadata>(out var meta)) continue;
				if (!dictionary.ContainsKey(id))
				{
					dictionary.Add(id, 0);
				}

				dictionary[id] += meta.Amount;
			}

			return dictionary;
		}

		private async UniTask UpdateCharacter()
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
			await _character.UpdateSkin(skinId, false);
			UpdatePlayerName();
		}
	}
}