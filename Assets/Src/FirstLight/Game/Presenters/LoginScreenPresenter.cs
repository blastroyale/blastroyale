using System;
using FirstLight.Game.Services;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using I2.Loc;
using Quantum;
using TMPro;
using Button = UnityEngine.UI.Button;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the login screen
	/// </summary>
	public class LoginScreenPresenter : AnimatedUiPresenterData<LoginScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string,string> LoginClicked;
			public Action GoToRegisterClicked;
		}
		
		// TODO EVE
		// This is where all of the magic happens.
		// This script is linked to LoginScreen.prefab in the project, where all of the feature lives.
		//
		// First - you need to create a new button in LoginScreen.prefab
		// * Duplicate RegisterButton, call it something like 'DevRegisterButton'
		// * Under DevRegisterButton -> RegisterText - remove I2Localize component, and set the text to 'Dev Register'
		// * Store reference to this button in this script like one of the other buttons
		//
		// The reason we remove the I2Localize component, is that this dev register button doesnt need translations - 
		// its for developers only, so we can just write some text directly.

		[SerializeField] private TMP_InputField _emailInputField;
		[SerializeField] private TMP_InputField _passwordInputField;

		// TODO EVE - remove this _registerRootObject and all lines that use it, not needed anymore
		[SerializeField] private GameObject _registerRootObject;
		[SerializeField] private Button _goToRegisterButton;
		[SerializeField] private Button _loginButton;
		// TODO EVE - create and store the new dev register button
		
		[SerializeField] private GameObject _frontDimBlocker;
		
		private void Awake()
		{
			_registerRootObject.SetActive(Debug.isDebugBuild);
			_goToRegisterButton.onClick.AddListener(GoToRegisterClicked);
			_loginButton.onClick.AddListener(LoginClicked);
			
			// TODO EVE 
			// Link the new _devRegisterButton to new GoToDevRegisterClicked method
			// Activate this button _devRegisterButton on condition: Debug.isDebugBuild
		}
		
		private void OnEnable()
		{
			SetFrontDimBlockerActive(false);
		}
		
		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_frontDimBlocker.SetActive(active);
		}
		
		private void LoginClicked()
		{
			Data.LoginClicked(_emailInputField.text, _passwordInputField.text);
		}

		private void GoToRegisterClicked()
		{
			Data.GoToRegisterClicked();
			
			// TODO EVE - move the above line to GoToDevRegisterclicked instead
			//
			// Then you need this function to open the marketplace
			// There is currently only dev marketplace online: http://flgmarketplacestorage.z33.web.core.windows.net/ 
			// However, for release, we will need this function to open either the dev marketplace, or real marketplace,
			// depending on whether we are playing on a debug build, or a release build.
			//
			// So:
			// * Make an if statemen  based on Debug.isDebugBuild
			// * Use method Application.OpenURL(""); to open marketplaces
			// * For now, both cases in the if statements should open the dev marketplace
			// * However, write a TODO in the else block
			//	* The devs will need to know to replace the link with a real marketplace link once real market is open
			//
			// ***TEST*** on all platforms - editor, android, iOS, as OpenURL does different things on different platforms
		}
		
		// TODO EVE - create a new 'GoToDevRegisterClicked' method
		// This is a simple method that should only call Data.GoToRegisterClicked(); like in the current register function
	}
}