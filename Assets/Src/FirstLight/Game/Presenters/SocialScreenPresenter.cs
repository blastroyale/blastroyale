using FirstLight.UiService;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Social Screen UI.
	/// </summary>
	public class SocialScreenPresenter : AnimatedUiPresenterData<SocialScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnSocialBackButtonClicked;
		}

		[SerializeField] private Button _backButton;

		private void Awake()
		{
			_backButton.onClick.AddListener(OnBackButtonPressed);
		}

		private void OnBackButtonPressed()
		{
			Data.OnSocialBackButtonClicked();
		}
	}
}