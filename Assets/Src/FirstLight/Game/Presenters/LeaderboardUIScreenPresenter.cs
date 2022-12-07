using System;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.Game.Views.MatchHudViews;
using FirstLight.Services;
using FirstLight.UiService;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the Leaderboards and Rewards Screen
	/// </summary>
	public class LeaderboardUIScreenPresenter : UiToolkitPresenterData<LeaderboardUIScreenPresenter.StateData>
	{
		private const string UssPlayerName = "player-name";
		private const string UssFirst = UssPlayerName + "--first";
		private const string UssSecond = UssPlayerName + "--second";
		private const string UssThird = UssPlayerName + "--third";
		private const string UssSpectator = "spectator";
		
		[SerializeField] private BaseCharacterMonoComponent _character;
		[SerializeField] private Camera _camera;
		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;
		[SerializeField] private LeaderboardUIEntryView _playerRankEntryRef;


		private VisualElement _leaderboardPanel;

		private VisualElement _rewardsPanel;
		private VisualElement _craftSpice;
		private VisualElement _trophies;
		private VisualElement _bpp;
		
		private RewardPanelView _craftSpiceView;
		private RewardPanelView _trophiesView;
		private RewardBPPanelView _bppView;
		public struct StateData
		{
			public Action BackClicked;
		}

		private IMatchServices _matchServices;
		private IGameServices _services;
		private IObjectPool<LeaderboardUIEntryView> _playerRankPool;

		private int lowestTopRankedPosition = 0;

		

		[SerializeField] private Button _backButton;
		[SerializeField] private Button _homeButton;
		private ScrollView _leaderboardScrollView;
		private VisualElement _playerName;
		private Label _playerNameText;
		
		/*protected override void OnInitialized()
		{
			base.OnInitialized();
			
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}
		*/
	/*	protected override void OnOpened()
		{
			base.OnOpened();

			SetupCamera();
			UpdateCharacter();
			UpdatePlayerName();
			UpdateLeaderboard();
		}*/
	

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
				
		//	_playerRankPool =
		//		new GameObjectPool<LeaderboardUIEntryView>(GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT + 
		//		                                           GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT, _playerRankEntryRef);
		}
		protected override void OnOpened()
		{
			base.OnOpened();

			//_farRankLeaderboardRoot.SetActive(false);
			//int month = DateTime.UtcNow.Month + 1;
			//if (month > 12) month -= 12;
			var nextResetDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
			nextResetDate.AddMonths(1);
			var timeDiff = nextResetDate - DateTime.UtcNow;

			// TODO - ALL TEXT ON THIS PRESENTER NEEDS TO BE ADDED, HOOKED UP AND LOCALIZED. THIS IS PLACEHOLDER
			//_seasonEndText.text = "Season ends in " + timeDiff.ToString(@"d\d\ h\h\ mm\m");

			// Leaderboard is requested and displayed in 2 parts
			// First - top players, then, if needed - current player and neighbors, in a separate anchored section
			
			_services.PlayfabService.GetTopRankLeaderboard(GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT,
				OnLeaderboardTopRanksReceived, OnLeaderboardRequestError);
		}
		
		protected override void QueryElements(VisualElement root)
		{
			_homeButton = root.Q<Button>("HomeButton").Required();
			_homeButton.clicked += OnBackClicked;
			_backButton = root.Q<Button>("BackButton").Required();
			_backButton.clicked += OnBackClicked;

			_leaderboardPanel = root.Q<VisualElement>("LeaderboardPanel").Required();
			_leaderboardScrollView = root.Q<ScrollView>("LeaderboardScrollView").Required();

		//	_playerName = root.Q<VisualElement>("PlayerName").Required();
		//	_playerNameText = _playerName.Q<Label>("Text").Required();

	//		_rewardsPanel = root.Q<VisualElement>("RewardsPanel").Required();
	//		_craftSpice = _rewardsPanel.Q<VisualElement>("CraftSpice").Required();
			//_craftSpice.AttachView(this, out _craftSpiceView);
	//		_trophies = _rewardsPanel.Q<VisualElement>("Trophies").Required();
		//	_trophies.AttachView(this, out _trophiesView);
	//		_bpp = _rewardsPanel.Q<VisualElement>("BPP").Required();
	//		_bpp.AttachView(this, out _bppView);
			
			_leaderboardScrollView = root.Q<ScrollView>("LeaderboardScrollView").Required();

	//		_backButton = root.Q<Button>("NextButton").Required();
	//		_backButton.clicked += Data.BackClicked;
			
	//		_playerName = root.Q<VisualElement>("PlayerName").Required();
	//		_playerNameText = _playerName.Q<Label>("Text").Required();
		}
		
		private void OnLeaderboardRequestError(PlayFabError error)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error(JsonConvert.SerializeObject(error.ErrorDetails));
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, error.ErrorMessage, false, confirmButton);
			
			Data.BackClicked();
		}

		private void OnLeaderboardTopRanksReceived(GetLeaderboardResult result)
		{
			bool localPlayerInTopRanks = false;

			lowestTopRankedPosition = result.Leaderboard[result.Leaderboard.Count - 1].Position;

			for (int i = 0; i < result.Leaderboard.Count; i++)
			{
				var isLocalPlayer = result.Leaderboard[i].PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;
				//var newEntry = _playerRankPool.Spawn();
				//newEntry.transform.SetParent(_topRankSpawnTransform);
				//newEntry.gameObject.SetActive(true);
				//newEntry.SetInfo(result.Leaderboard[i].Position + 1, result.Leaderboard[i].DisplayName,
				//                 result.Leaderboard[i].StatValue, isLocalPlayer, null);

				var newEntry = _leaderboardEntryAsset.Instantiate();
				newEntry.AttachView(this, out LeaderboardUIEntryView view);
				view.SetData(result.Leaderboard[i], isLocalPlayer);
				_leaderboardScrollView.Add(newEntry);

			
				//var newEntry = _leaderboardEntryAsset.Instantiate();
				//newEntry.AttachView(this, out LeaderboardUIEntryView view);
				//view.SetData(result.Leaderboard[i] , _matchServices.MatchEndDataService.LocalPlayer == result.Leaderboard[i]);
				//_leaderboardScrollView.Add(newEntry);

			
				
				
				if (isLocalPlayer)
				{
					localPlayerInTopRanks = true;
				}
			}
			
			_leaderboardPanel.style.display = DisplayStyle.Flex;

			return;
			
			if (localPlayerInTopRanks) return;

			_services.PlayfabService.GetNeighborRankLeaderboard(GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT,
			                                                    OnLeaderboardNeighborRanksReceived, OnLeaderboardRequestError);
		}

		private void OnLeaderboardNeighborRanksReceived(GetLeaderboardAroundPlayerResult result)
		{
			return;
		/*	var localPlayer = result.Leaderboard.First(x => x.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId);

			// If the rank above player joins with the top ranks leaderboard, we just append player to the normal leaderboard
			if (Math.Abs(lowestTopRankedPosition - localPlayer.Position) == 1)
			{
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.SetInfo(localPlayer.Position + 1, localPlayer.DisplayName, localPlayer.StatValue, true, null);
				return;
			}

			_farRankLeaderboardRoot.SetActive(true);

			for (int i = 0; i < result.Leaderboard.Count; i++)
			{
				var isLocalPlayer = result.Leaderboard[i].PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.transform.SetParent(_farRankSpawnTransform);
				newEntry.SetInfo(result.Leaderboard[i].Position + 1, result.Leaderboard[i].DisplayName,
				                 result.Leaderboard[i].StatValue, isLocalPlayer, null);
			}*/
		}
		protected override void OnTransitionsReady()
		{
			SetupCamera();
		}


		/*private void UpdatePlayerName()
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
		}*/

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

		private void SetupCamera()
		{
			_camera.gameObject.SetActive(true);
			_camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(19f, 2.17f);
		}
		
		
		private void OnBackClicked()
		{
			Data.BackClicked();
		}
	}
	
	/*
	public struct StateData
		{
			public Action BackClicked;
		}

		[SerializeField] private PlayerRankEntryView _playerRankEntryRef;
		[SerializeField] private Transform _topRankSpawnTransform;
		[SerializeField] private Transform _farRankSpawnTransform;
		[SerializeField] private GameObject _farRankLeaderboardRoot;
		[SerializeField] private TextMeshProUGUI _seasonEndText;
		[SerializeField] private Button _backButton;

		private IGameServices _services;
		private IObjectPool<PlayerRankEntryView> _playerRankPool;

		private int lowestTopRankedPosition = 0;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_backButton.onClick.AddListener(OnBackClicked);
			
			_playerRankPool =
				new GameObjectPool<PlayerRankEntryView>(GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT + 
				                                        GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT, _playerRankEntryRef);
		}

		private void OnDestroy()
		{
			_backButton.onClick.RemoveAllListeners();
		}
		
		protected override async Task OnClosed()
		{
			await base.OnClosed();

			_playerRankPool.DespawnAll();
			_farRankLeaderboardRoot.SetActive(false);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			_farRankLeaderboardRoot.SetActive(false);

			var nextResetDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month + 1, 1);
			var timeDiff = nextResetDate - DateTime.UtcNow;

			// TODO - ALL TEXT ON THIS PRESENTER NEEDS TO BE ADDED, HOOKED UP AND LOCALIZED. THIS IS PLACEHOLDER
			_seasonEndText.text = "Season ends in " + timeDiff.ToString(@"d\d\ h\h\ mm\m");

			// Leaderboard is requested and displayed in 2 parts
			// First - top players, then, if needed - current player and neighbors, in a separate anchored section
			_services.PlayfabService.GetTopRankLeaderboard(GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT,
			                                               OnLeaderboardTopRanksReceived, OnLeaderboardRequestError);
		}

		private void OnLeaderboardRequestError(PlayFabError error)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error(JsonConvert.SerializeObject(error.ErrorDetails));
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, error.ErrorMessage, false, confirmButton);
			
			Data.BackClicked();
		}

		private void OnLeaderboardTopRanksReceived(GetLeaderboardResult result)
		{
			bool localPlayerInTopRanks = false;

			lowestTopRankedPosition = result.Leaderboard[result.Leaderboard.Count - 1].Position;

			for (int i = result.Leaderboard.Count - 1; i >= 0; i--)
			{
				var isLocalPlayer = result.Leaderboard[i].PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;
				var newEntry = _playerRankPool.Spawn();
				newEntry.transform.SetParent(_topRankSpawnTransform);
				newEntry.gameObject.SetActive(true);
				newEntry.SetInfo(result.Leaderboard[i].Position + 1, result.Leaderboard[i].DisplayName,
				                 result.Leaderboard[i].StatValue, isLocalPlayer, null);

				if (isLocalPlayer)
				{
					localPlayerInTopRanks = true;
				}
			}

			if (localPlayerInTopRanks) return;

			_services.PlayfabService.GetNeighborRankLeaderboard(GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT,
			                                                    OnLeaderboardNeighborRanksReceived, OnLeaderboardRequestError);
		}

		private void OnLeaderboardNeighborRanksReceived(GetLeaderboardAroundPlayerResult result)
		{
			var localPlayer = result.Leaderboard.First(x => x.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId);

			// If the rank above player joins with the top ranks leaderboard, we just append player to the normal leaderboard
			if (Math.Abs(lowestTopRankedPosition - localPlayer.Position) == 1)
			{
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.SetInfo(localPlayer.Position + 1, localPlayer.DisplayName, localPlayer.StatValue, true, null);
				return;
			}

			_farRankLeaderboardRoot.SetActive(true);

			for (int i = 0; i < result.Leaderboard.Count; i++)
			{
				var isLocalPlayer = result.Leaderboard[i].PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;
				var newEntry = _playerRankPool.Spawn();
				newEntry.gameObject.SetActive(true);
				newEntry.transform.SetParent(_farRankSpawnTransform);
				newEntry.SetInfo(result.Leaderboard[i].Position + 1, result.Leaderboard[i].DisplayName,
				                 result.Leaderboard[i].StatValue, isLocalPlayer, null);
			}
		}

		private void OnBackClicked()
		{
			Data.BackClicked();
		}
	 * 
	 */
}