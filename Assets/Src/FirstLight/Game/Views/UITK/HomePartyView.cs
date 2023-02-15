using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
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
		private VisualElement _container;
		private Label _title;
		private ListView _partyMemberList;

		private IPartyService _partyService;

		private List<PartyMember> _partyMembers;

		public void Attached(VisualElement element)
		{
			_container = element;
			_partyService = MainInstaller.Resolve<IGameServices>().PartyService;

			_title = element.Q<Label>("PartyLabel").Required();
			_partyMemberList = element.Q<ListView>("PartyList").Required();
			_partyMemberList.DisableScrollbars();

			_partyMemberList.makeItem = CreatePartyListEntry;
			_partyMemberList.bindItem = BindPartyListEntry;

			RefreshPartyList();
		}

		private void BindPartyListEntry(VisualElement element, int index)
		{
			var partyMember = _partyMembers[index];

			((Label) element).text = (partyMember.Leader ? "<sprite name=\"Crown\">" : string.Empty) + (partyMember.Ready ? "<sprite name=\"Checked\"> " : string.Empty) + partyMember.DisplayName;
			((Label) element).enableRichText = true;

			if (CanKick() && !partyMember.Local)
			{
				element.UnregisterCallback<ClickEvent, int>(OnPartyMemberClicked);
				element.RegisterCallback<ClickEvent, int>(OnPartyMemberClicked, index);
			}
		}

		private async void OnPartyMemberClicked(ClickEvent e, int index)
		{
			await _partyService.Kick(_partyMembers[index].PlayfabID);
		}

		private VisualElement CreatePartyListEntry()
		{
			var label = new Label();
			label.enableRichText = true;
			label.AddToClassList("squad-member");
			return label;
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
			_title.text = $"{ScriptLocalization.UITHomeScreen.party} [{partyCode}]";
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