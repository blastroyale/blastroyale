using System;
using System.Collections.Generic;
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
	/// This Presenter handles the Rewards Screen, where players are awarded loot.
	/// Players can skip through animations if they are impatient.
	/// </summary>
	public class TrophiesScreenPresenter : AnimatedUiPresenterData<TrophiesScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action ContinueClicked;
		}
		
		[SerializeField, Required] private Button _continueButton;
		
		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private int _trophyChange;

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_continueButton.onClick.AddListener(OnContinueClicked);
		}

		protected override void OnOpenedCompleted()
		{
			base.OnOpenedCompleted();
		}

		private void PlayTrophiesSequence()
		{
			
		}

		private void OnContinueClicked()
		{
			Data.ContinueClicked();
		}

	}
}