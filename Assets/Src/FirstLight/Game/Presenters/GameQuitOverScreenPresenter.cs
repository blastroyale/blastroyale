using System;
using FirstLight.Game.Ids;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Game Quit Over Screen UI by:
	/// - Go to the main menu
	/// </summary>
	public class GameQuitOverScreenPresenter : AnimatedUiPresenterData<GameQuitOverScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action ContinueClicked;
		}
	
		[SerializeField, Required] private Button _continueButton;

		private void Awake()
		{
			_continueButton.onClick.AddListener(OnContinueButtonClicked);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			Services.AudioFxService.PlayClip2D(AudioId.GameOver1);
		}

		private void OnContinueButtonClicked()
		{
			Data.ContinueClicked.Invoke();
		}
	}
}