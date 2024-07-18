using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Creates a Generic Dialog for showing information to the player usually for yes / no choices.
	/// Usually used for pointing out something informative to players.
	/// </summary>
	[UILayer(UILayer.Popup)]
	public abstract class GenericDialogPresenterBase : UIPresenter
	{
		protected IGameServices _services;

		private Label _titleLabel;
		private Label _descLabel;
		private Button _confirmButton;
		private Button _cancelButton;
		private Button _blockerButton;

		private Action _closeCallback;
		private Action _confirmCallback;

		public string Title => _titleLabel?.text;

		private void Awake()
		{
			_services = MainInstaller.ResolveServices();
		}

		protected override void QueryElements()
		{
			_titleLabel = Root.Q<Label>("Title").Required();
			_descLabel = Root.Q<Label>("Desc").Required();

			_confirmButton = Root.Q<Button>("ConfirmButton").Required();
			_cancelButton = Root.Q<Button>("CancelButton").Required();
			_blockerButton = Root.Q<Button>("BlockerButton").Required();

			_confirmCallback = null;
			_closeCallback = null;
		}

		protected override UniTask OnScreenClose()
		{
			_closeCallback?.Invoke();
			return base.OnScreenClose();
		}

		protected void SetBaseInfo(string title, string desc, bool showCloseButton, GenericDialogButton button, Action closeCallback)
		{
			_titleLabel.text = title;
			_descLabel.text = desc;
			_closeCallback = closeCallback;
			_cancelButton.SetDisplay(showCloseButton);

			if (button.IsEmpty)
			{
				_confirmButton.SetDisplay(false);
			}
			else
			{
				_confirmCallback = button.ButtonOnClick;
				_confirmButton.text = button.ButtonText;

				_confirmButton.SetDisplay(true);
				_confirmButton.clicked += _confirmCallback;
				_confirmButton.clicked += CloseRequested;
			}

			if (showCloseButton)
			{
				_cancelButton.clicked += CloseRequested;
				_blockerButton.clicked += CloseRequested;
			}
		}

		private void CloseRequested()
		{
			_services.UIService.CloseLayer(UILayer.Popup).Forget();
		}
	}
}