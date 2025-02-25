using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters.Store;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.UIElements.Kit;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles purchase confirmations
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class SkipTutorialPopupPresenter : UIPresenterResult<SkipTutorialPopupPresenter.AllowedOptions>
	{
		public const string USS_SELECTED = "option--selected";

		public enum AllowedOptions
		{
			Tutorial,
			SkipTutorial,
			Login
		}

		[Q("Popup")] public GenericPopupElement _popup;
		[Q("TutorialButton")] public ImageButton _tutorialButton;
		[Q("SkipButton")] public ImageButton _skipButton;
		[Q("LoginButton")] public ImageButton _loginButton;
		[Q("ConfirmButton")] public KitButton _confirmButton;

		private Dictionary<AllowedOptions, ImageButton> _buttons;
		private AllowedOptions Selected = AllowedOptions.Tutorial;

		protected override async UniTask OnScreenOpen(bool reload)
		{
			await _popup.AnimateOpen();
		}

		protected override async UniTask OnScreenClose()
		{
			await _popup.AnimateClose();
		}

		protected override void QueryElements()
		{
			_buttons = new ();
			Register(_tutorialButton, AllowedOptions.Tutorial);
			Register(_skipButton, AllowedOptions.SkipTutorial);
			Register(_loginButton, AllowedOptions.Login);
			SetActive(AllowedOptions.Tutorial);
			_confirmButton.clicked += () =>
			{
				SetResult(Selected);
			};
		}

		public void Register(ImageButton button, AllowedOptions option)
		{
			_buttons.Add(option, button);
			button.clicked += () =>
			{
				SetActive(option);
			};
		}

		public void DisableLoginOption()
		{
			_buttons.Remove(AllowedOptions.Login);
			_loginButton.SetDisplay(false);
		}

		public void SetActive(AllowedOptions option)
		{
			Selected = option;
			foreach (var (otherOption, otherButton) in _buttons)
			{
				otherButton.EnableInClassList(USS_SELECTED, otherOption == option);
			}
		}
	}
}