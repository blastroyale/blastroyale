using System;
using System.Collections;
using Cinemachine;
using FirstLight.Game.Services;
using FirstLight.Game.Timeline;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using Button = UnityEngine.UI.Button;
using UnityEngine.UI;
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
		public struct StateData
		{
			public Action ContinueClicked;
		}

		[SerializeField, Required] private CinemachineVirtualCamera _playerProxyCamera;
		[SerializeField, Required] protected PlayableDirector _director;

		private EntityRef _playerWinnerEntity;
		private IMatchServices _matchService;
		private IGameServices _services;

		private void Awake()
		{
			_matchService = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.Resolve<IGameServices>();

			QuantumEvent.Subscribe<EventOnPlayerLeft>(this, OnEventOnPlayerLeft);
		}

		protected override void QueryElements(VisualElement root)
		{
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetupCamera();
		}

		private IEnumerator MatchEndStepsCoroutine()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out var leader);
			var playerWinner = playerData[leader];
			
			// Show match ended
			
			
			// Show Victory / Blasted
			if (game.PlayerIsLocal(leader))
			{
				// Victory
			}
			else if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				// Game over after spectating
			}
			else
			{
				// Blasted
			}

			// Show winner
			var name = string.Format(ScriptLocalization.AdventureMenu.PlayerWon, playerWinner.GetPlayerName());
			PlayerWinnerCameraAnimation(playerWinner);

			_playerWinnerEntity = playerWinner.Data.Entity;

			yield return null;
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