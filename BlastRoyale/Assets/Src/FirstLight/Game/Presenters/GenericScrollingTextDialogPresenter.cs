using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	[UILayer(UILayer.Popup)]
	public class GenericScrollingTextDialogPresenter : UIPresenterData<GenericScrollingTextDialogPresenter.StateData>
	{
		public class StateData
		{
			public string Title;
			public string Text;
			public Action OnConfirm;
		}

		private Button _privacy;
		private Button _terms;
		private LocalizedButton _confirm;
		private Label _text;
		private Label _title;
		private IGameServices _services;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();
			_text = Root.Q<Label>("PopupText").Required();
			_title = Root.Q<Label>("Title").Required();
			_text.text = Data.Text;
			_title.text = Data.Title;
			_confirm = Root.Q<LocalizedButton>("ConfirmButton").Required();
			_confirm.clicked += Data.OnConfirm;
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			if (_text == null) return UniTask.CompletedTask; // no idea first time opening happens
			_text.text = Data.Text;
			_title.text = Data.Title;
			return base.OnScreenOpen(reload);
		}
	}
}