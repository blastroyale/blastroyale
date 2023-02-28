using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles the party window on the home screen.
	/// </summary>
	public class HomePartyView : IUIView
	{
		private VisualElement _root;
		private VisualElement _container;
		private Label _code;
		private ListView _partyMemberList;

		private IPartyService _partyService;
		private IGenericDialogService _genericDialogService;

		private List<PartyMember> _partyMembers;

		public void Attached(VisualElement element)
		{
			_container = element;
			_partyService = MainInstaller.Resolve<IGameServices>().PartyService;
			_genericDialogService = MainInstaller.Resolve<IGameServices>().GenericDialogService;

			_code = element.Q<Label>("RoomCode").Required();
			_partyMemberList = element.Q<ListView>("PartyList").Required();
			_partyMemberList.DisableScrollbars();

			_partyMemberList.makeItem = CreatePartyListEntry;
			_partyMemberList.bindItem = BindPartyListEntry;

			RefreshPartyList();
		}

		public void SetRoot(VisualElement root)
		{
			_root = root;
		}

		private void BindPartyListEntry(VisualElement element, int index)
		{
			var partyMember = _partyMembers[index];
			var button = (Button) element;

			button.text =
				$"    {(partyMember.Leader ? "<sprite name=\"Crown\">" : string.Empty)}  {partyMember.DisplayName}  {(partyMember.Ready ? "<sprite name=\"Checked\"> " : string.Empty)}";

			if (CanKick() && !partyMember.Local)
			{
				element.UnregisterCallback<ClickEvent, Pair<int, VisualElement>>(OnPartyMemberClicked);
				element.RegisterCallback<ClickEvent, Pair<int, VisualElement>>(OnPartyMemberClicked,
					new Pair<int, VisualElement>(index, element));
			}
		}

		private VisualElement CreatePartyListEntry()
		{
			var label = new Button();
			label.enableRichText = true;
			label.AddToClassList("squad-member");
			return label;
		}

		private void OnPartyMemberClicked(ClickEvent e, Pair<int, VisualElement> args)
		{
			if (_partyService.OperationInProgress.Value) return;

			var index = args.Key;
			var element = args.Value;

			var kickButton = new LocalizedButton(ScriptTerms.UITSquads.kick);
			kickButton.AddToClassList("squad-button-kick");
			element.OpenTooltip(_root, kickButton, position: TooltipPosition.CenterLeft, offsetX: 50);
			kickButton.clicked += () => { KickPartyMember(index); };
		}

		private async void KickPartyMember(int index)
		{
			try
			{
				await _partyService.Kick(_partyMembers[index].PlayfabID);
			}
			catch (PartyException pe)
			{
				_genericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, pe.Error.GetTranslation(),
					true,
					new GenericDialogButton());
				FLog.Warn("Error on kicking squad member", pe);
			}
		}

		public void SubscribeToEvents()
		{
			_partyService.HasParty.InvokeObserve(OnHasPartyChanged);
			_partyService.PartyCode.InvokeObserve(OnPartyCodeChanged);
			_partyService.Members.Observe(OnPartyMembersChanged);
		}

		private void OnPartyMembersChanged(int index, PartyMember prev, PartyMember current,
										   ObservableUpdateType updateType)
		{
			_partyMemberList.bindItem = BindPartyListEntry;
			RefreshPartyList();
		}

		private void OnPartyCodeChanged(string _, string partyCode)
		{
			_code.text = string.Format(ScriptLocalization.UITHomeScreen.party_code, partyCode);
		}

		private void OnHasPartyChanged(bool _, bool hasParty)
		{
			_container.SetDisplay(hasParty);
		}

		public void UnsubscribeFromEvents()
		{
			_partyService.HasParty.StopObservingAll(this);
		}

		private void RefreshPartyList()
		{
			_partyMembers = _partyService.Members.ToList();
			_partyMemberList.itemsSource = _partyMembers;
			_partyMemberList.RefreshItems();
		}

		private bool CanKick()
		{
			return _partyService.Members.Any(m => m.Leader && m.Local);
		}
	}
}