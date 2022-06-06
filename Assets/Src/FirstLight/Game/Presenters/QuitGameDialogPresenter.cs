using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the dialog that appears when a player wants to quit the game.
	/// </summary>
	public class QuitGameDialogPresenter : AnimatedUiPresenterData<QuitGameDialogPresenter.StateData>
	{
		public struct StateData
		{
			public Action ConfirmClicked;
		}
		
		[SerializeField, Required] private Button _confirmButton;
		[SerializeField, Required] private Button _cancelButton;
		[SerializeField, Required] private Button _blockerButton;

		private void Awake()
		{
			_confirmButton.onClick.AddListener(OnConfirmClicked);
			_cancelButton.onClick.AddListener(Close);
			_blockerButton.onClick.AddListener(Close);
		}

		private void OnConfirmClicked()
		{
			Close();

			Data.ConfirmClicked();
		}
	}
}
