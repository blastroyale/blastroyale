using System;
using FirstLight.Game.Services;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Creates a Generic Dialog for showing information to the player usually for yes / no choices.
	/// Usually used for pointing out something informative to players.
	/// </summary>
	public abstract class GenericDialogPresenterBase : AnimatedUiPresenter
	{
		[SerializeField, Required] protected TextMeshProUGUI TitleText;
		[SerializeField, Required] protected TextMeshProUGUI ConfirmButtonText;
		[SerializeField, Required] protected Button CloseButton;
		[SerializeField, Required] protected Button ConfirmButton;
		[SerializeField, Required] protected Button BlockerButton;

		private Action _closeCallback;

		private void Awake()
		{
			CloseButton.onClick.AddListener(CloseRequested);
			BlockerButton.onClick.AddListener(CloseRequested);

			OnAwake();
		}

		private void CloseRequested()
		{
			Close(false);
		}

		protected virtual void OnAwake() { }

		protected override void Close(bool destroy)
		{
			if (IsOpenedComplete)
			{
				base.Close(destroy);
			}
		}

		protected void SetBaseInfo(string title, bool showCloseButton, GenericDialogButton button, Action closeCallback)
		{
			TitleText.text = title;
			_closeCallback = closeCallback;

			if (button.IsEmpty)
			{
				ConfirmButton.gameObject.SetActive(false);
			}
			else
			{
				ConfirmButtonText.text = button.ButtonText;
				
				ConfirmButton.gameObject.SetActive(true);
				ConfirmButton.onClick.RemoveAllListeners();
				ConfirmButton.onClick.AddListener(CloseRequested);
				ConfirmButton.onClick.AddListener(button.ButtonOnClick);
			}
			
			CloseButton.gameObject.SetActive(showCloseButton);
			BlockerButton.enabled = showCloseButton;
		}

		protected override void OnClosedCompleted()
		{
			_closeCallback?.Invoke();
		}
	}
}