using System;
using UnityEngine;
using TMPro;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the register screen
	/// </summary>
	public class ConnectIdScreenPresenter : AnimatedUiPresenterData<ConnectIdScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string,string,string> ConnectClicked;
			public Action BackClicked;
		}

		[SerializeField] private TMP_InputField _emailInputField;
		[SerializeField] private TMP_InputField _nameInputField;
		[SerializeField] private TMP_InputField _passwordInputField;

		[SerializeField] private Button _connectButton;
		[SerializeField] private Button _backButton;
		[SerializeField] private Button _backgroundBlockerButton;
		[SerializeField] private GameObject _frontDimBlocker;
		
		private void Awake()
		{
			_connectButton.onClick.AddListener(ConnectClicked);
			_backButton.onClick.AddListener(BackClicked);
			_backgroundBlockerButton.onClick.AddListener(BackClicked);
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

		private void ConnectClicked()
		{
			Data.ConnectClicked(_emailInputField.text, _nameInputField.text, _passwordInputField.text);
		}

		private void BackClicked()
		{
			Data.BackClicked();
		}
	}
}