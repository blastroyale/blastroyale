using System;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.Events;

namespace FirstLight.Game.Services
{
	public struct GenericDialogButton
	{
		public string ButtonText;
		public Action ButtonOnClick;

		/// <summary>
		/// Requests the state of the button, if it has listeners or not for it
		/// </summary>
		public bool IsEmpty => ButtonOnClick == null || ButtonOnClick.GetInvocationList().Length == 0;
	}

	public struct GenericDialogButton<T>
	{
		public string ButtonText;
		public UnityAction<T> ButtonOnClick;

		/// <summary>
		/// Requests the state of the button, if it has listeners or not for it
		/// </summary>
		public bool IsEmpty => ButtonOnClick.GetInvocationList().Length == 0;
	}
	

	/// <summary>
	/// This service provides a direct reference to UI Generic dialogs to any system in the game.
	/// This are the <see cref="GenericDialogPresenter"/>, <see cref="GenericDialogVideoPresenter"/>, etc
	/// </summary>
	public interface IGenericDialogService
	{
		/// <summary>
		/// Shows the Generic Dialog box PopUp with the given information without an option for the user except
		/// to click on the close button.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		void OpenButtonDialog(string title, string desc, bool showCloseButton, GenericDialogButton button,
							  Action closeCallback = null);

		/// <summary>
		/// Shows an input field dialog box for the player to write specific string data.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		void OpenInputDialog(string title, string desc, string initialInputText, GenericDialogButton<string> button,
							 bool showCloseButton, Action<string> closeCallback = null,
							 TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default);

		/// <summary>
		/// Displays a simple message dialog
		/// </summary>
		void OpenSimpleMessage(string title, string desc, Action onClick = null);
		

		/// <summary>
		/// Open the purchase confirmation dialog, and if the player doesn't have the amount of blast bucks open not enough popup
		/// </summary>
		void OpenPurchaseOrNotEnough(GenericPurchaseDialogPresenter.GenericPurchaseOptions options);


		/// <summary>
		/// Closes the <see cref="GenericDialogPresenter"/> if opened
		/// </summary>
		void CloseDialog();
	}

	/// <inheritdoc />
	public class GenericDialogService : IGenericDialogService
	{
		private readonly IGameUiService _uiService;
		private readonly ICurrencyDataProvider _currencyDataProvider;

		private Type _openDialogType;

		public GenericDialogService(IGameUiService uiService, ICurrencyDataProvider currencyDataProvider)
		{
			_uiService = uiService;
			_currencyDataProvider = currencyDataProvider;
		}

		/// <inheritdoc />
		public void OpenButtonDialog(string title, string desc, bool showCloseButton = true,
									 GenericDialogButton button = new GenericDialogButton(),
									 Action closeCallback = null)
		{
			var ui = _uiService.OpenUi<GenericDialogPresenter>();

			_openDialogType = ui.GetType();

			ui.SetInfo(title, desc, showCloseButton, button, closeCallback);
		}

		// TODO - Support different "content types" - requires refactor on UIT TextField
		/// <inheritdoc />
		public void OpenInputDialog(string title, string desc, string initialInputText,
									GenericDialogButton<string> button,
									bool showCloseButton, Action<string> closeCallback = null,
									TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default)
		{
			var ui = _uiService.OpenUi<GenericInputDialogPresenter>();

			_openDialogType = ui.GetType();

			ui.SetInfo(title, desc, initialInputText, button, showCloseButton, closeCallback, keyboardType);
		}

		public void OpenSimpleMessage(string title, string desc, Action onClick = null)
		{
			OpenButtonDialog(title, desc, false, new GenericDialogButton()
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					CloseDialog();
					onClick?.Invoke();
				}
			});
		}

		public void OpenPurchaseOrNotEnough(GenericPurchaseDialogPresenter.GenericPurchaseOptions options)
		{
			var bucks = _currencyDataProvider.GetCurrencyAmount(options.Currency);
			var ui = _uiService.OpenUi<GenericPurchaseDialogPresenter>();
			_openDialogType = ui.GetType();

			if (bucks >= options.Value)
			{
				ui.SetHasEnoughOptions(options);
			}
			else
			{
				ui.SetNotEnoughOptions(options);		
			}
		}
		
		/// <inheritdoc />
		public void CloseDialog()
		{
			if (_openDialogType == null) return;
			_uiService.CloseUi(_openDialogType);
			_openDialogType = null;
		}
	}
}