using System;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
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
		
		[SerializeField] private Camera _camera;
		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;
		[SerializeField] private LeaderboardUIEntryView _playerRankEntryRef;


		private VisualElement _leaderboardPanel;

		private VisualElement _rewardsPanel;
		private VisualElement _craftSpice;
		private VisualElement _trophies;
		private VisualElement _bpp;
		private ScreenHeaderElement _header;
		
		private RewardPanelView _craftSpiceView;
		private RewardPanelView _trophiesView;
		private RewardBPPanelView _bppView;
		public struct StateData
		{
			public Action OnBackClicked;
		}

		private IMatchServices _matchServices;
		private IGameServices _services;
		private IObjectPool<LeaderboardUIEntryView> _playerRankPool;

		private int lowestTopRankedPosition = 0;

		
		private ScrollView _leaderboardScrollView;
		private VisualElement _playerName;
		private Label _playerNameText;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}
		protected override void OnOpened()
		{
			base.OnOpened();
			
			/* TODO - NOT IN USE AT CURRENT DESIGN (kept here in case it will be added)
			var nextResetDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
			nextResetDate.AddMonths(1);
			
			var timeDiff = nextResetDate - DateTime.UtcNow;
			*/
			
			// TODO - ALL TEXT ON THIS PRESENTER NEEDS TO BE ADDED, HOOKED UP AND LOCALIZED. THIS IS PLACEHOLDER
			//_seasonEndText.text = "Season ends in " + timeDiff.ToString(@"d\d\ h\h\ mm\m");

			// Leaderboard is requested and displayed in 2 parts
			// First - top players, then, if needed - current player and neighbors, in a separate anchored section
			
			_services.PlayfabService.GetTopRankLeaderboard(GameConstants.Network.LEADERBOARD_TOP_RANK_AMOUNT,
				OnLeaderboardTopRanksReceived, OnLeaderboardRequestError);
		}
		
		protected override void QueryElements(VisualElement root)
		{
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += Data.OnBackClicked;
			_header.homeClicked += Data.OnBackClicked;
			
			_leaderboardPanel = root.Q<VisualElement>("LeaderboardPanel").Required();
			_leaderboardScrollView = root.Q<ScrollView>("LeaderboardScrollView").Required();
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
			
			Data.OnBackClicked();
		}

		private void OnLeaderboardTopRanksReceived(GetLeaderboardResult result)
		{
			bool localPlayerInTopRanks = false;

			lowestTopRankedPosition = result.Leaderboard[result.Leaderboard.Count - 1].Position;

			for (int i = 0; i < 11; i++)
			{
				var isLocalPlayer = result.Leaderboard[i].PlayFabId == PlayFabSettings.staticPlayer.PlayFabId;


				var newEntry = _leaderboardEntryAsset.Instantiate();
				newEntry.AttachView(this, out LeaderboardUIEntryView view);
				view.SetData(result.Leaderboard[i], isLocalPlayer);
				_leaderboardScrollView.Add(newEntry);

				
				if (isLocalPlayer)
				{
					localPlayerInTopRanks = true;
				}
			}
			
			_leaderboardPanel.style.display = DisplayStyle.Flex;
			
			
			
			if (localPlayerInTopRanks) return;

			_services.PlayfabService.GetNeighborRankLeaderboard(GameConstants.Network.LEADERBOARD_NEIGHBOR_RANK_AMOUNT,
			                                                    OnLeaderboardNeighborRanksReceived, OnLeaderboardRequestError);
		}

		private void OnLeaderboardNeighborRanksReceived(GetLeaderboardAroundPlayerResult result)
		{
			
			var localPlayer = result.Leaderboard.First(x => x.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId);

			var newEntry = _leaderboardEntryAsset.Instantiate();
			newEntry.AttachView(this, out LeaderboardUIEntryView view);
			view.SetData(localPlayer, true);
			_leaderboardScrollView.Add(newEntry);

			return;
			/*
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
		
		private void SetupCamera()
		{
			_camera.gameObject.SetActive(true);
			_camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(19f, 2.17f);
		}
		
	}
}