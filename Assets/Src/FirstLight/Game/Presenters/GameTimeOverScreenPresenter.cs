using System;
using FirstLight.Game.Ids;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Game Timer Over Screen UI by:
	/// - Go to the main menu
	/// </summary>
	public class GameTimeOverScreenPresenter : AnimatedUiPresenterData<GameTimeOverScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action ContinueClicked;
		}
	
		[SerializeField] private Button _continueButton;

		private void Start()
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