using System;
using System.Linq;
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
	public class LeaderboardAndRewardsScreenPresenter : UiToolkitPresenterData<LeaderboardAndRewardsScreenPresenter.StateData>
	{
		private const string UssFirst = "first";
		private const string UssSecond = "second";
		private const string UssThird = "third";
		private const string UssSpectator = "spectator";
		
		[SerializeField] private BaseCharacterMonoComponent _character;
		[SerializeField] private Camera _camera;
		[SerializeField] private VisualTreeAsset _leaderboardEntryAsset;
		
		public struct StateData
		{
			public Action ContinueClicked;
		}

		private IMatchServices _matchServices;

		private Button _nextButton;
		private ScrollView _leaderboardScrollView;
		private VisualElement _playerName;
		private Label _playerNameText;
		
		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}
		
		protected override void OnOpened()
		{
			base.OnOpened();

			SetupCamera();
			UpdateCharacter();
			UpdatePlayerName();
			UpdateLeaderboard();
		}

		protected override void OnTransitionsReady()
		{
			SetupCamera();
		}

		protected override void QueryElements(VisualElement root)
		{
			_leaderboardScrollView = root.Q<ScrollView>("LeaderboardScrollView").Required();

			_nextButton = root.Q<Button>("NextButton").Required();
			_nextButton.clicked += Data.ContinueClicked;
			
			_playerName = root.Q<VisualElement>("PlayerName").Required();
			_playerNameText = _playerName.Q<Label>("Text").Required();
		}

		private void UpdatePlayerName()
		{
			if (_matchServices.MatchEndDataService.LocalPlayer == PlayerRef.None)
			{
				_playerNameText.text = "";
				return;
			}
			
			// Cleanup in case the screen is re-used
			_playerName.RemoveFromClassList(UssFirst);
			_playerName.RemoveFromClassList(UssSecond);
			_playerName.RemoveFromClassList(UssThird);
			
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