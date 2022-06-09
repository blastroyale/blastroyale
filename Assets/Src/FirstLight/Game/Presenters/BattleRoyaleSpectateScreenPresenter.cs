using System;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This is responsible for displaying the screen during spectate mode,
	/// that follows your killer around.
	/// </summary>
	public class BattleRoyaleSpectateScreenPresenter : UiPresenterData<BattleRoyaleSpectateScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnLeaveClicked;
		}

		[SerializeField, Required] private Button _leaveButton;

		private void Start()
		{
			_leaveButton.onClick.AddListener(OnLeaveClicked);
		}

		private void OnLeaveClicked()
		{
			Data.OnLeaveClicked();
		}
	}
}