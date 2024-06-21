using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK.Popups;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
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

		// Unity can't serialize 'Type' so we need to be explicit here
		[SerializeField, Required] private VisualTreeAsset _joinWithCodeDocument;
		[SerializeField, Required] private VisualTreeAsset _selectNumberDocument;
		[SerializeField, Required] private VisualTreeAsset _selectMapDocument;
		[SerializeField, Required] private VisualTreeAsset _selectSquadSizeDocument;
		[SerializeField, Required] private VisualTreeAsset _selectMutatorsDocument;
		[SerializeField, Required] private VisualTreeAsset _partyDocument;

		private GenericPopupElement _popup;

		protected override void QueryElements()
		{
			_popup = Root.Q<GenericPopupElement>("Popup").Required();
			_popup.LocalizeTitle(Data.TitleKey);
			_popup.CloseClicked += () => Close().Forget();

			Root.Q<ImageButton>("Blocker").Required().clicked += () => Close().Forget();

			switch (Data.View)
			{
				case JoinWithCodePopupView view:
					SetupPopup(_joinWithCodeDocument, view);
					break;
				case SelectNumberPopupView view:
					SetupPopup(_selectNumberDocument, view);
					break;
				case SelectMapPopupView view:
					SetupPopup(_selectMapDocument, view);
					break;
				case SelectSquadSizePopupView view:
					SetupPopup(_selectSquadSizeDocument, view);
					break;
				case SelectMutatorsPopupView view:
					SetupPopup(_selectMutatorsDocument, view);
					break;
				case PartyPopupView view:
					SetupPopup(_partyDocument, view);
					break;
				default:
					throw new NotImplementedException($"You need to implement the view type: {Data.View.GetType()}");
			}
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_popup.AnimatePing(1.1f);
			return base.OnScreenOpen(reload);
		}

		public static UniTaskVoid OpenJoinWithCode(Action<string> onJoin)
		{
			return OpenPopup(new JoinWithCodePopupView(onJoin), ScriptTerms.UITCustomGames.join_with_code);
		}

		public static UniTaskVoid OpenSelectNumber(Action<int> onConfirm, string titleKey, string subtitleKey, int min, int max, int currentValue)
		{
			return OpenPopup(new SelectNumberPopupView(onConfirm, subtitleKey, min, max, currentValue), titleKey);
		}

		public static UniTaskVoid OpenSelectMap(Action<string> onMapSelected, string gameModeID, string currentMapID)
		{
			return OpenPopup(new SelectMapPopupView(onMapSelected, gameModeID, currentMapID), ScriptTerms.UITCustomGames.select_map);
		}

		public static UniTaskVoid OpenSelectSquadSize(Action<int> onSquadSizeSelected, int currentSize)
		{
			return OpenPopup(new SelectSquadSizePopupView(onSquadSizeSelected, currentSize), ScriptTerms.UITCustomGames.select_squad_size);
		}

		public static UniTaskVoid OpenSelectMutators(Action<Mutator> onMutatorsSelected, Mutator mutators)
		{
			return OpenPopup(new SelectMutatorsPopupView(onMutatorsSelected, mutators), ScriptTerms.UITCustomGames.select_mutators);
		}
		
		public static UniTaskVoid OpenParty()
		{
			return OpenPopup(new PartyPopupView(), ScriptTerms.UITHomeScreen.party);
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