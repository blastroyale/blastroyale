using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using FirstLight.UIService;
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
	/// This are the <see cref="GenericButtonDialogPresenter"/>, <see cref="GenericDialogVideoPresenter"/>, etc
	/// </summary>
	// TODO: All of this needs to be refactored
	public interface IGenericDialogService
	{
		/// <summary>
		/// Shows the Generic Dialog box PopUp with the given information without an option for the user except
		/// to click on the close button.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		UniTask OpenButtonDialog(string title, string desc, bool showCloseButton, GenericDialogButton button,
								 Action closeCallback = null);

		/// <summary>
		/// Shows an input field dialog box for the player to write specific string data.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		UniTask OpenInputDialog(string title, string desc, string initialInputText, GenericDialogButton<string> button,
								bool showCloseButton, Action<string> closeCallback = null,
								TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default);

		/// <summary>
		/// Displays a simple message dialog
		/// </summary>
		UniTask OpenSimpleMessage(string title, string desc, Action onClick = null);

		/// <summary>
		/// Displays a simple message and wait for it to be closed
		/// </summary>
		public UniTask OpenSimpleMessageAndWait(string title, string desc);

		/// <summary>
		/// Open the purchase confirmation dialog, and if the player doesn't have the amount of blast bucks open not enough popup
		/// </summary>
		UniTask OpenPurchaseOrNotEnough(GenericPurchaseDialogPresenter.IPurchaseData data);

		/// <summary>
		/// Closes the <see cref="GenericButtonDialogPresenter"/> if opened
		/// </summary>
		void CloseDialog();

		/// <summary>
		/// Check if there's any popup already open before calling a new one
		/// </summary>
		bool HasPopupOpen();
	}

	/// <inheritdoc />
	public class GenericDialogService : IGenericDialogService
	{
		private readonly UIService.UIService _uiService;
		private readonly ICurrencyDataProvider _currencyDataProvider;

		public GenericDialogService(UIService.UIService uiService, ICurrencyDataProvider currencyDataProvider)
		{
			_uiService = uiService;
			_currencyDataProvider = currencyDataProvider;
		}

		/// <inheritdoc />
		public async UniTask OpenButtonDialog(string title, string desc, bool showCloseButton = true,
											  GenericDialogButton button = new GenericDialogButton(),
											  Action closeCallback = null)
		{
			var ui = await _uiService.OpenScreen<GenericButtonDialogPresenter>();
			await UniTask.NextFrame(); // TODO: Hacky, this data should be passed with StateData, now we need to wait for a frame :(
			ui.SetInfo(title, desc, showCloseButton, button, closeCallback);
		}

		// TODO - Support different "content types" - requires refactor on UIT TextField
		/// <inheritdoc />
		public async UniTask OpenInputDialog(string title, string desc, string initialInputText,
											 GenericDialogButton<string> button,
											 bool showCloseButton, Action<string> closeCallback = null,
											 TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default)
		{
			var ui = await _uiService.OpenScreen<GenericInputDialogPresenter>();
			await UniTask.NextFrame(); // TODO: Hacky, this data should be passed with StateData, now we need to wait for a frame :(
			ui.SetInfo(title, desc, initialInputText, button, showCloseButton, closeCallback, keyboardType);
		}

		public UniTask OpenSimpleMessage(string title, string desc, Action onClick = null)
		{
			return OpenButtonDialog(title, desc, false, new GenericDialogButton()
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					CloseDialog();
					onClick?.Invoke();
				}
			});
		}

		public async UniTask OpenSimpleMessageAndWait(string title, string desc)
		{
			var completionSource = new UniTaskCompletionSource();
			await OpenButtonDialog(title, desc, false, new GenericDialogButton()
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					CloseDialog();
				}
			}, () =>
			{
				completionSource.TrySetResult();
			});
			await completionSource.Task;
		}

		public async UniTask OpenPurchaseOrNotEnough(GenericPurchaseDialogPresenter.IPurchaseData data)
		{
			var priceCoin = data.Price.Id;
			var ownedCurrency = _currencyDataProvider.GetCurrencyAmount(data.Price.Id);
			if (priceCoin.IsInGroup(GameIdGroup.CryptoCurrency) && MainInstaller.ResolveWeb3().IsEnabled())
			{
				// we validate on client based on the prediction
				ownedCurrency = MainInstaller.ResolveWeb3().GetWeb3Currencies()[priceCoin].TotalPredicted.Value;
				
				// TODO: Validate pending transactions to ensure consistency
			}
		
			await _uiService.OpenScreen<GenericPurchaseDialogPresenter>(new GenericPurchaseDialogPresenter.StateData
			{
				PurchaseData = data,
				OwnedCurrency = ownedCurrency
			});
		}

		/// <inheritdoc />
		public void CloseDialog()
		{
			_uiService.CloseLayer(UILayer.Popup).Forget();
		}

		public bool HasPopupOpen()
		{
			return _uiService.HasUIPresenterOpenOnLayer(UILayer.Popup);
		}
	}
}