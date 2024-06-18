using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK.Popups;
using FirstLight.UIService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	[UILayer(UILayer.Popup)]
	public class PopupPresenter : UIPresenterData<PopupPresenter.StateData>
	{
		public class StateData
		{
			public string TitleKey;
			public UIView View;
		}

		[SerializeField, Required] private VisualTreeAsset _joinWithCodeDocument;

		private GenericPopupElement _popup;

		protected override void QueryElements()
		{
			_popup = Root.Q<GenericPopupElement>("Popup").Required();
			_popup.LocalizeTitle(Data.TitleKey);

			switch (Data.View)
			{
				case JoinWithCodePopupView view:
					SetupPopup(_joinWithCodeDocument, view);
					break;
				default:
					throw new NotImplementedException($"You need to implement the view type: {Data.View.GetType()}");
			}
		}

		public static UniTaskVoid OpenJoinWithCode(Action<string> onJoin)
		{
			return OpenPopup(new JoinWithCodePopupView(onJoin), "JOIN WITH CODE");
		}

		public static UniTask Close()
		{
			var services = MainInstaller.ResolveServices();
			return services.UIService.CloseScreen<PopupPresenter>();
		}

		private static async UniTaskVoid OpenPopup(UIView view, string titleKey)
		{
			var services = MainInstaller.ResolveServices();
			await services.UIService.OpenScreen<PopupPresenter>(new StateData
			{
				View = view,
				TitleKey = titleKey
			});
		}

		private void SetupPopup(VisualTreeAsset contentAsset, UIView view)
		{
			var content = contentAsset.CloneTree();
			content.AttachExistingView(this, view);
			_popup.Add(content);
		}
	}
}