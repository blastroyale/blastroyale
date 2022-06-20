using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Views.AdventureHudViews;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Trophy Screen, where players are awarded/deducted trophies.
	/// Players can skip through animations if they are impatient.
	/// </summary>
	public class TrophiesScreenPresenter : AnimatedUiPresenterData<TrophiesScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action ExitTrophyScreen;
			public Func<int> LastTrophyChange;
			public Func<uint> TrophiesBeforeLastChange;
		}

		private const int ANIM_STAGE_INITIAL = 0;
		private const int ANIM_STAGE_FINAL = 2;
		private const float TRANSFER_TROPHIES_DURATION = 1f;

		[SerializeField, Required] private Button _continueButton;
		[SerializeField, Required] private AnimationClip _newTrophiesSequenceUnfoldClip;
		[SerializeField, Required] private TextMeshProUGUI _trophiesStatusText;
		[SerializeField, Required] private TextMeshProUGUI _trophyChangeText;
		[SerializeField, Required] private TextMeshProUGUI _trophyTotalText;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private int _trophyChange;
		private int _currentTrophies;
		private int _currentAnimStage = 0;
		private bool _isTransferringTrophies = false;

		public bool IsAnimating => _animation.isPlaying || _isTransferringTrophies;
		public string TrophyChangePrefix => _trophyChange > 0 ? "+" : "";
		
		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_continueButton.onClick.AddListener(OnContinueClicked);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			_trophyChange = Data.LastTrophyChange();
			
			if (_trophyChange > 0)
			{
				_trophiesStatusText.text = ScriptLocalization.MainMenu.TrophiesGained.ToUpper();
			}
			else
			{
				_trophiesStatusText.text = ScriptLocalization.MainMenu.TrophiesLost.ToUpper();
			}
			
			_currentTrophies = (int) _dataProvider.PlayerDataProvider.Trophies.Value;
			_trophyChangeText.text = TrophyChangePrefix + _trophyChange;
			_trophyTotalText.text = Data.TrophiesBeforeLastChange().ToString();
		}

		/// <summary>
		/// This function is called as at a specific point in _newTrophiesSequenceUnfoldClip sequence, when text
		/// briefly shrinks and disappears.
		/// </summary>
		public void ChangeStatusToTotalTrophies()
		{
			_trophiesStatusText.text = ScriptLocalization.MainMenu.TrophiesNewTotal.ToUpper();
		}

		private void OnContinueClicked()
		{
			if (IsAnimating)
			{
				return;
			}

			if (_currentAnimStage == ANIM_STAGE_INITIAL)
			{
				_currentAnimStage = ANIM_STAGE_FINAL;
				PlayTrophiesUnfoldSequence();
			}
			else if (_currentAnimStage == ANIM_STAGE_FINAL)
			{
				Data.ExitTrophyScreen();
			}
		}

		private void PlayTrophiesUnfoldSequence()
		{
			_animation.Play(_newTrophiesSequenceUnfoldClip.name);
			this.LateCoroutineCall(_newTrophiesSequenceUnfoldClip.length, TransferTrophies);
		}

		private void TransferTrophies()
		{
			var trophiesBeforeChange =  Data.TrophiesBeforeLastChange();
			_isTransferringTrophies = true;
			
			DOVirtual.Float(trophiesBeforeChange, _currentTrophies, TRANSFER_TROPHIES_DURATION,
			                (currentTrophyValue) =>
			                {
				                _trophyTotalText.text = currentTrophyValue.ToString("F0");
			                }).OnComplete(OnTransferTrophiesComplete);
		}

		private void OnTransferTrophiesComplete()
		{
			_isTransferringTrophies = false;
		}
	}
}