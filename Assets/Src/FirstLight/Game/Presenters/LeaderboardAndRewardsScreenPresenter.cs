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
		private Label _playerName;
		
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
		}

		protected override void OnTransitionsReady()
		{
			SetupCamera();
		}

		protected override void QueryElements(VisualElement root)
		{
			_leaderboardScrollView = root.Q<ScrollView>("LeaderboardScrollView").Required();
#if UNITY_EDITOR
			// Scrollview doesn't let us drag scroll with the mouse, so this will help us test in the editor
			_leaderboardScrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
#endif
			_nextButton = root.Q<Button>("NextButton").Required();
			_nextButton.clicked += Data.ContinueClicked;
			
			_playerName = root.Q<Label>("PlayerName").Required();

			UpdatePlayerName();
			UpdateLeaderboard();
		}

		private void UpdatePlayerName()
		{
			if (_matchServices.MatchEndDataService.LocalPlayer == PlayerRef.None)
			{
				_playerName.text = "";
				return;
			}
			
			var playerData = _matchServices.MatchEndDataService.PlayerMatchData;
			var localPlayerData = playerData[_matchServices.MatchEndDataService.LocalPlayer];
			_playerName.text = localPlayerData.QuantumPlayerMatchData.PlayerRank+". "+ localPlayerData.QuantumPlayerMatchData.PlayerName;
		}

		private void UpdateLeaderboard()
		{
			if (_matchServices.MatchEndDataService.LocalPlayer == PlayerRef.None)
			{
				Root.AddToClassList("spectator");
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