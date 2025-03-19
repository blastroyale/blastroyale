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
		public enum AllowedOptions
		{
			Tutorial,
			SkipTutorial,
			Login
		}

		public AngledContainerElement _tutorialButton;
		public AngledContainerElement _skipButton;
		public AngledContainerElement _loginButton;

		protected override void QueryElements()
		{
			_tutorialButton = Root.Q("Beginner").Q<AngledContainerElement>();
			_skipButton = Root.Q("Expert").Q<AngledContainerElement>();
			_loginButton = Root.Q("Citizen").Q<AngledContainerElement>();
			_tutorialButton.clicked += () => SetResult(AllowedOptions.Tutorial);
			_skipButton.clicked += () => SetResult(AllowedOptions.SkipTutorial);
			_loginButton.clicked += () => SetResult(AllowedOptions.Login);
		}

		public void DisableLoginOption()
		{
			_loginButton.SetDisplay(false);
		}
	}
}