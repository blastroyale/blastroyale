using System;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	public class GenericScrollingTextDialogPresenter : UiToolkitPresenterData<GenericScrollingTextDialogPresenter.StateData>
	{
		public struct StateData
		{
			public string Title;
			public string Text;
			public Action OnConfirm;
		}

		private Button _privacy;
		private Button _terms;
		private Button _confirm;
		private Label _text;
		private Label _title;
		private IGameServices _services;

		protected override void QueryElements(VisualElement root)
		{
			_services = MainInstaller.ResolveServices();
			_text = root.Q<Label>("PopupText").Required();
			_title = root.Q<Label>("Title").Required();
			_text.text = Data.Text;
			_title.text = Data.Title;
			_confirm = root.Q<Button>("ConfirmButton").Required();
			_confirm.clicked += Data.OnConfirm;
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			if (_text == null) return; // no idea first time opening happens
			_text.text = Data.Text;
			_title.text = Data.Title;
		}
	}
}