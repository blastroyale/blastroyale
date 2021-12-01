using System;
using FirstLight.Game.Infos;
using FirstLight.Game.Presenters;
using FirstLight.Game.Views.TooltipView;
using UnityEngine;
using UnityEngine.Events;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.Services
{
	public struct GenericDialogButton
	{
		public string ButtonText;
		public UnityAction ButtonOnClick;

		/// <summary>
		/// Requests the state of the button, if it has listeners or not for it
		/// </summary>
		public bool IsEmpty => ButtonOnClick.GetInvocationList().Length == 0;
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
		void OpenDialog(string title, bool showCloseButton, GenericDialogButton button, Action closeCallback = null);
		
		/// <summary>
		/// Shows the Generic Dialog with an icon box PopUp with the given information without an option for
		/// the user except to click on the close button.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		void OpenIconDialog<TId>(string title, string descriptionText, TId id, bool showCloseButton, 
		                    GenericDialogButton button, Action closeCallback = null)
			where TId : struct, Enum;
		
		/// <summary>
		/// Shows the Generic Dialog box PopUp with an video with the given information without an option for the user except
		/// to click on the close button.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		void OpenVideoDialog<TId>(string title, string descriptionText, TId id, bool showCloseButton, 
			GenericDialogButton button, Action closeCallback = null)
			where TId : struct, Enum;
		
		/// <summary>
		/// Shows the Generic Dialog box PopUp for the user to spend Hard currency with the given information without
		/// an option for the user except to click on the close button.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		void OpenHcDialog(string title, string cost, bool showCloseButton,
			GenericDialogButton button,  bool showSC = false, Action closeCallback = null);

		/// <summary>
		/// Shows an input field dialog box for the player to write specific string data.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		void OpenInputFieldDialog(string title, string initialInputText, GenericDialogButton<string> button, 
		                          bool showCloseButton, Action<string> closeCallback = null);
		
		/// <summary>
		/// Closes the <see cref="GenericDialogPresenter"/> if opened
		/// </summary>
		void CloseDialog();

		/// <summary>
		/// Opens up a dialog to show the information of the possible contents of a Loot Box.
		/// </summary>
		void OpenLootInfoDialog(GenericDialogButton button, LootBoxInfo boxInfo, Action closeCallback = null);

		/// <summary>
		/// Opens up a tooltip dialog to show informative text.
		/// </summary>
		void OpenTooltipDialog(string locTag, Vector3 worldPos, TooltipHelper.TooltipArrowPosition tooltipArrowPosition);

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
		public void OpenDialog(string title, bool showCloseButton = true, 
		                       GenericDialogButton button = new GenericDialogButton(), Action closeCallback = null)
		{
			var ui = _uiService.OpenUi<GenericDialogPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(title, showCloseButton, button, closeCallback);
		}

		/// <inheritdoc />
		public void OpenIconDialog<TId>(string title, string descriptionText, TId id, bool showCloseButton = true, 
		                           GenericDialogButton button = new GenericDialogButton(), Action closeCallback = null)
			where TId : struct, Enum
		{
			var ui = _uiService.OpenUi<GenericDialogIconPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(title, descriptionText, id, showCloseButton, button, closeCallback);
		}
		
		/// <inheritdoc />
		public void OpenVideoDialog<TId>(string title, string descriptionText, TId id, bool showCloseButton, 
			GenericDialogButton button, Action closeCallback = null)
			where TId : struct, Enum
		{
			var ui = _uiService.OpenUi<GenericDialogVideoPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(title, descriptionText, id, showCloseButton, button, closeCallback);
		}
		
		/// <inheritdoc />
		public void OpenHcDialog(string title, string cost, bool showCloseButton, GenericDialogButton button, 
			bool showSC = false, Action closeCallback = null)
		{
			var ui = _uiService.OpenUi<GenericDialogHcPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(title, cost, showCloseButton, button, showSC, closeCallback);
		}

		/// <inheritdoc />
		public void OpenInputFieldDialog(string title, string initialInputText, GenericDialogButton<string> button, 
		                                 bool showCloseButton, Action<string> closeCallback = null)
		{
			var ui = _uiService.OpenUi<GenericDialogInputFieldPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(title, initialInputText, button, showCloseButton, closeCallback);
		}

		/// <summary>
		/// Opens up a dialog to show the information of the possible contents of a Loot Box.
		/// </summary>
		public void OpenLootInfoDialog(GenericDialogButton button, LootBoxInfo boxInfo, Action closeCallback = null)
		{
			var ui = _uiService.OpenUi<GenericDialogLootInfoPresenter>();

			_openDialogType = ui.GetType();
			
			ui.SetInfo(button, boxInfo, closeCallback);
		}

		/// <inheritdoc />
		public void OpenTooltipDialog(string locTag, Vector3 worldPos, TooltipHelper.TooltipArrowPosition tooltipArrowPosition)
		{
			var ui = _uiService.OpenUi<UiTooltipPresenter>();
			ui.ShowTooltipHelper(locTag, worldPos,tooltipArrowPosition);
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
			Assert.IsNotNull(_openDialogType);
			
			_uiService.CloseUi(_openDialogType);

			_openDialogType = null;
		}
	}
}