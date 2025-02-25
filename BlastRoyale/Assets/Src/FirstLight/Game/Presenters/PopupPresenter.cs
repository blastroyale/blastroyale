using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Presenters.Social.Team;
using FirstLight.Game.Services;
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
	/// <summary>
	/// A general base presenter for all popups.
	/// </summary>
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
		[SerializeField, Required] private VisualTreeAsset _selectWeaponsDocument;
		[SerializeField, Required] private VisualTreeAsset _partyDocument;
		[SerializeField, Required] private VisualTreeAsset _inviteFriendsDocument;
		[SerializeField, Required] private VisualTreeAsset _enterCreatorCodeDocument;
		[SerializeField, Required] private VisualTreeAsset _genericInfoDocument;
		[SerializeField, Required] private VisualTreeAsset _genericConfirmDocument;

		private GenericPopupElement _popup;

		protected override void QueryElements()
		{
			_popup = Root.Q<GenericPopupElement>("Popup").Required();
			_popup.LocalizeTitle(Data.TitleKey);
			_popup.CloseClicked += () => ClosePopupScreen().Forget();

			Root.Q<ImageButton>("Blocker").Required().clicked += () => ClosePopupScreen().Forget();

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
				case TeamPopupView view:
					SetupPopup(_partyDocument, view);
					break;
				case InviteFriendsPopupView view:
					SetupPopup(_inviteFriendsDocument, view);
					break;
				case EnterCreatorCodePopupView view:
					SetupPopup(_enterCreatorCodeDocument, view);
					break;
				case GenericInfoPopupView view:
					SetupPopup(_genericInfoDocument, view);
					break;
				case GenericConfirmPopupView view:
					SetupPopup(_genericConfirmDocument, view);
					break;
				case SelectWeaponsPopupView view:
					SetupPopup(_selectWeaponsDocument, view);
					break;
				default:
					throw new NotImplementedException($"You need to implement the view type: {Data.View.GetType()}");
			}
		}

		public static UniTaskVoid OpenJoinWithCode(Action<string> onJoin)
		{
			return OpenPopup(new JoinWithCodePopupView(onJoin), ScriptTerms.UITCustomGames.join_with_code);
		}

		public static UniTaskVoid OpenSelectNumber(Action<int> onConfirm, string titleKey, string subtitleKey, int min, int max, int currentValue)
		{
			return OpenPopup(new SelectNumberPopupView(onConfirm, subtitleKey, min, max, currentValue), titleKey);
		}

		public static UniTaskVoid OpenSelectMap(Action<string> onMapSelected, IEnumerable<GameId> mapIDs, string currentMapID, bool showAny)
		{
			return OpenPopup(new SelectMapPopupView(onMapSelected, mapIDs, currentMapID, showAny), ScriptTerms.UITCustomGames.select_map);
		}

		public static UniTaskVoid OpenSelectSquadSize(Action<int> onSquadSizeSelected, uint currentSize)
		{
			return OpenPopup(new SelectSquadSizePopupView(onSquadSizeSelected, currentSize), ScriptTerms.UITCustomGames.select_squad_size);
		}

		public static UniTaskVoid OpenSelectMutators(Action<Mutator> onMutatorsSelected, Mutator mutators)
		{
			return OpenPopup(new SelectMutatorsPopupView(onMutatorsSelected, mutators), ScriptTerms.UITCustomGames.select_mutators);
		}

		public static UniTaskVoid OpenSelectWeapons(Action<List<string>> onWeaponsSelected, List<string> weapons)
		{
			return OpenPopup(new SelectWeaponsPopupView(onWeaponsSelected, weapons), ScriptTerms.UITCustomGames.select_weapons);
		}

		public static UniTaskVoid OpenParty()
		{
			return OpenPopup(new TeamPopupView(), ScriptTerms.UITHomeScreen.party);
		}
		

		public static UniTaskVoid OpenInviteFriends()
		{
			return OpenPopup(new InviteFriendsPopupView(), ScriptTerms.UITCustomGames.invite_blasters);
		}

		public static UniTaskVoid OpenEnterCreatorCode(Action<string> creatorCode)
		{
			return OpenPopup(new EnterCreatorCodePopupView(creatorCode), ScriptTerms.UITStore.content_creator);
		}

		public static UniTaskVoid OpenGenericInfo(string titleKey, string infoText)
		{
			return OpenPopup(new GenericInfoPopupView(infoText, () => ClosePopupScreen().Forget()), titleKey, true);
		}

		public static UniTaskVoid OpenGenericConfirm(string titleKey, string confirmText, Action confirmAction)
		{
			return OpenPopup(new GenericConfirmPopupView(confirmText, confirmAction, () => ClosePopupScreen().Forget()), titleKey, true);
		}

		public static UniTask ClosePopupScreen()
		{
			var services = MainInstaller.ResolveServices();
			return services.UIService.CloseScreen<PopupPresenter>(false);
		}

		private static async UniTaskVoid OpenPopup(UIView view, string titleKey, bool closeOpenedPopups = false)
		{
			if (closeOpenedPopups)
				ClosePopupScreen().Forget();

			var services = MainInstaller.ResolveServices();
			await services.UIService.OpenScreen<PopupPresenter>(new StateData
			{
				View = view,
				TitleKey = titleKey
			});
		}

		public static bool IsOpen<T>() where T : UIView
		{
			var services = MainInstaller.ResolveServices();
			if (!services.UIService.IsScreenOpen<PopupPresenter>())
			{
				return false;
			}

			var s = services.UIService.GetScreen<PopupPresenter>();
			return s.Data.View.GetType() == typeof(T);
		}

		private void SetupPopup(VisualTreeAsset contentAsset, UIView view)
		{
			var content = contentAsset.CloneTree();
			content.AttachExistingView(this, view);
			_popup.Add(content);
		}
	}
}