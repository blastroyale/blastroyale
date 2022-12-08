using System;
using FirstLight.Game.Infos;
using FirstLight.Game.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Assert = UnityEngine.Assertions.Assert;

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
		void OpenButtonDialog(string title, string desc, bool showCloseButton, GenericDialogButton button, Action closeCallback = null);

		/// <summary>
		/// Shows an input field dialog box for the player to write specific string data.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		void OpenInputDialog(string title, string desc, string initialInputText, GenericDialogButton<string> button, 
		                          bool showCloseButton, /*TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard,*/ Action<string> closeCallback = null);
		
		/// <summary>
		/// Closes the <see cref="GenericDialogPresenter"/> if opened
		/// </summary>
		void CloseDialog();

		/// <summary>
		/// Opens up a tooltip dialog to show informative text.
		/// </summary>
		void OpenTooltipDialog(string locTag, Vector3 worldPos, TooltipArrowPosition tooltipArrowPosition);

		/// <summary>
		/// Opens up a Talking Head Dialog.
		/// </summary>
		void OpenTalkingHeadDialog(string title, Action closeCallback = null);
	}
	
	/// <inheritdoc />
	public class GenericDialogService : IGenericDialogService
	{
		private readonly IGameUiService _uiService;

		private Type _openDialogType;
		
		public GenericDialogService(IGameUiService uiService)
		{
			_uiService = uiService;
		}

		/// <inheritdoc />
		public void OpenButtonDialog(string title, string desc, bool showCloseButton = true, 
		                       GenericDialogButton button = new GenericDialogButton(), Action closeCallback = null)
		{
			var ui = _uiService.OpenUi<GenericDialogPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(title, desc, showCloseButton, button, closeCallback);
		}

		// TODO - Support different "content types" - requires refactor on UIT TextField
		/// <inheritdoc />
		public void OpenInputDialog(string title, string desc, string initialInputText, GenericDialogButton<string> button, 
		                                 bool showCloseButton, Action<string> closeCallback = null)
		{
			var ui = _uiService.OpenUi<GenericInputDialogPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(title, desc, initialInputText, button, showCloseButton, /*contentType,*/ closeCallback);
		}

		/// <inheritdoc />
		public void OpenTooltipDialog(string locTag, Vector3 worldPos, TooltipArrowPosition tooltipArrowPosition)
		{
			var ui = _uiService.OpenUi<UiTooltipPresenter>();
			ui.ShowTooltip(locTag, worldPos, tooltipArrowPosition);
		}

		/// <summary>
		/// Opens up a dialog to show the information of the possible contents of a Loot Box.
		/// </summary>
		public void OpenTalkingHeadDialog(string text, Action closeCallback = null)
		{
			var ui = _uiService.OpenUi<TalkingHeadDialogPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(text, closeCallback);
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