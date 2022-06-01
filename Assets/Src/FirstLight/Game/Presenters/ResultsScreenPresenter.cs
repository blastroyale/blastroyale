using UnityEngine;
using FirstLight.Game.Utils;
using System.Collections.Generic;
using Quantum;
using Button = UnityEngine.UI.Button;
using Slider = UnityEngine.UI.Slider; 
using System;
using System.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.Views.MainMenuViews;
using Sirenix.OdinInspector;
using TMPro;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Results Screen, where players can see who claimed the most loot and who performed the best.
	/// </summary>
	public class ResultsScreenPresenter : AnimatedUiPresenterData<ResultsScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action ContinueButtonClicked;
			public Action HomeButtonClicked;
		}

		[SerializeField, Required] private Button _gotoMainMenuButton;
		[SerializeField, Required] private Button _gotoNextButton;
		[SerializeField, Required] private Slider _goToNextSlider;
		[SerializeField, Required] private StandingsHolderView _standings;
		[SerializeField, Required] private TextMeshProUGUI _debugTotalMatchTimeText;
		
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_gotoNextButton.onClick.AddListener(ContinueButtonClicked);
			_gotoMainMenuButton.onClick.AddListener(HomeButtonClicked);
		}

		protected override void OnOpened()
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out _);
			var isBattleRoyale = frame.Context.MapConfig.GameMode == GameMode.BattleRoyale;

			_standings.Initialise(playerData.Count, isBattleRoyale, true);
			_standings.UpdateStandings(playerData);
			
			// Only play the animation after Results Award sprites have been loaded.
			_animation.clip = _introAnimationClip;
			_animation.Play();
			
			// StartCoroutine(TimeUpdateCoroutine()); Enable to force a replay after Time Coroutine finishes.

			_debugTotalMatchTimeText.enabled = Debug.isDebugBuild;
			
			if (Debug.isDebugBuild)
			{
				ShowDebugMatchTime();
			}
		}

		private void ShowDebugMatchTime()
		{
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;

			var ts = TimeSpan.FromSeconds(f.Time.AsFloat);
			_debugTotalMatchTimeText.text =  $"MATCH TIME: { ((uint) ts.TotalSeconds).ToHoursMinutesSeconds()}";
		}
		
		
		private IEnumerator TimeUpdateCoroutine()
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var totalForceTimeFp = config.GoToNextMatchForceTime;
			var totalForceTime = totalForceTimeFp.AsFloat;
			
			var endTime = Time.time + totalForceTime;
			
			_goToNextSlider.value = 0;

			while (Time.time < endTime)
			{
				_goToNextSlider.value = 1 - (endTime - Time.time) / totalForceTime; 

				yield return null;
			}
			
			_goToNextSlider.value = 1f;

			ContinueButtonClicked();
		}
		
		private void ContinueButtonClicked()
		{
			Data.ContinueButtonClicked.Invoke();
		}
		
		private void HomeButtonClicked()
		{
			Data.HomeButtonClicked.Invoke();
		}
	}
}