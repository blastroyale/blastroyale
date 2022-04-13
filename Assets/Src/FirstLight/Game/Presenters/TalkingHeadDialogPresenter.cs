using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	public class TalkingHeadDialogPresenter : AnimatedUiPresenter
	{
		[SerializeField] protected TextMeshProUGUI _titleText;
		[SerializeField] protected Button _closeButton;

		private Action _closeCallback;

		private void Start()
		{
			_closeButton.onClick.AddListener(Close);
		}

		/// <summary>
		/// Shows a Talking Head PopUp with the given <paramref name="title"/> and the <paramref name="button"/> information.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		public void SetInfo(string title, Action closeCallback)
		{
			_titleText.text = title;
			_closeCallback = closeCallback;
		}

		protected override void OnClosedCompleted()
		{
			_closeCallback?.Invoke();
		}
	}
}

