using System;
using System.Collections;
using Cinemachine;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using Button = UnityEngine.UIElements.Button;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Game Complete Screen UI by:
	/// - Showing the Game Complete info
	/// - Go to the winners screen
	/// </summary>
	public class GameCompleteScreenPresenter : UiToolkitPresenterData<GameCompleteScreenPresenter.StateData>
	{
		private const string HIDDEN = "hidden";
		private const string HIDDEN_START = "hidden-start";
		private const string HIDDEN_END = "hidden-end";
		
		public struct StateData
		{
			public Action ContinueClicked;
		}

		[SerializeField, Required] private CinemachineVirtualCamera _playerProxyCamera;
		[SerializeField, Required] protected PlayableDirector _director;

		private EntityRef _playerWinnerEntity;
		private IMatchServices _matchService;
		private IGameServices _services;

		private VisualElement _darkOverlay;
		private VisualElement _matchEndTitle;
		private VisualElement _blastedTitle;
		private VisualElement _youWinTitle;
		private VisualElement _winnerContainer;
		private Label _nameLabel;
		private Button _nextButton;

		private void Awake()
		{
			_matchService = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.Resolve<IGameServices>();

			QuantumEvent.Subscribe<EventOnPlayerLeft>(this, OnEventOnPlayerLeft);
		}

		protected override void QueryElements(VisualElement root)
		{
			_darkOverlay = root.Q<VisualElement>("DarkOverlay").Required();
			_matchEndTitle = root.Q<VisualElement>("MatchEndedTitle").Required();
			_blastedTitle = root.Q<VisualElement>("BlastedTitle").Required();
			_youWinTitle = root.Q<VisualElement>("YouWinTitle").Required();
			_winnerContainer = root.Q<VisualElement>("Winner").Required();
			_nameLabel = root.Q<Label>("NameLabel").Required();
			_nextButton = root.Q<Button>("NextButton").Required();

			_nextButton.clicked += OnContinueButtonClicked;
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetupCamera();

			StartCoroutine(MatchEndStepsCoroutine());
		}

		private IEnumerator MatchEndStepsCoroutine()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out var leader);
			var playerWinner = playerData[leader];
			
			// Show match ended
			ShowDarkOverlay();
			ShowMatchEnded();
			yield return new WaitForSeconds(3);
			HideMatchEnded();
			HideDarkOverlay();
			
			yield return new WaitForSeconds(1);
			
			// Show Victory / Blasted
			if (game.PlayerIsLocal(leader))
			{
				// Victory
				ShowDarkOverlay();
				ShowYouWin();
				yield return new WaitForSeconds(3);
				HideYouWin();
				HideDarkOverlay();
			}
			else if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				// Blasted
				ShowDarkOverlay();
				ShowBlasted();
				yield return new WaitForSeconds(3);
				HideBlasted();
				HideDarkOverlay();
			}

			// Show winner
			ShowWinner(playerWinner.GetPlayerName());
			PlayerWinnerCameraAnimation(playerWinner);

			_playerWinnerEntity = playerWinner.Data.Entity;
		}

		private void ShowDarkOverlay()
		{
			_darkOverlay.EnableInClassList(HIDDEN, false);
		}

		private void HideDarkOverlay()
		{
			_darkOverlay.EnableInClassList(HIDDEN, true);
		}
		
		private void ShowMatchEnded()
		{
			_matchEndTitle.RemoveFromClassList(HIDDEN_START);
		}
		
		private void HideMatchEnded()
		{
			_matchEndTitle.AddToClassList(HIDDEN_END);
		}
		
		private void ShowYouWin()
		{
			_youWinTitle.RemoveFromClassList(HIDDEN_START);
		}
		
		private void HideYouWin()
		{
			_youWinTitle.AddToClassList(HIDDEN_END);
		}
		
		private void ShowBlasted()
		{
			_blastedTitle.RemoveFromClassList(HIDDEN_START);
		}
		
		private void HideBlasted()
		{
			_blastedTitle.AddToClassList(HIDDEN_END);
		}
		
		private void ShowWinner(string winnerName)
		{
			_nameLabel.text = winnerName;
			_winnerContainer.RemoveFromClassList(HIDDEN_START);
		}
		
		private void HideWinner()
		{
			_winnerContainer.AddToClassList(HIDDEN_END);
		}

		private void PlayerWinnerCameraAnimation(QuantumPlayerMatchData playerWinner)
		{
			if (_matchService.EntityViewUpdaterService.TryGetView(playerWinner.Data.Entity, out var entityView))
			{
				var entityViewTransform = entityView.transform;

				_playerProxyCamera.Follow = entityViewTransform;
				_playerProxyCamera.LookAt = entityViewTransform;
				_director.time = 0;

				_director.Play();
			}
		}

		private void SetupCamera()
		{
			var cinemachineBrain = Camera.main.gameObject.GetComponent<CinemachineBrain>();

			foreach (var output in _director.playableAsset.outputs)
			{
				if (output.outputTargetType == typeof(CinemachineBrain))
				{
					_director.SetGenericBinding(output.sourceObject, cinemachineBrain);
				}
			}
		}

		private void OnContinueButtonClicked()
		{
			Data.ContinueClicked.Invoke();
			_director.Stop();
		}

		private void OnEventOnPlayerLeft(EventOnPlayerLeft callback)
		{
			if (_playerWinnerEntity == EntityRef.None || callback.Entity != _playerWinnerEntity)
			{
				return;
			}

			_director.Stop();
			_playerProxyCamera.gameObject.SetActive(false);
		}
	}
}