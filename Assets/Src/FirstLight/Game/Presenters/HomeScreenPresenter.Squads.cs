using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using FirstLight.UIService;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public partial class HomeScreenPresenter
	{
		// TODO: THIS PARTIAL CLASS IS NOT A GOOD DESIGN, BUT FOR NOW IT SHOULD BE ENOUGH
		// IN THE FUTURE WE SHOULD REORGANIZE THE HOME SCREEN PRESENTER WITH MORE COMPOSITIONS

		private LocalizedButton _partyButton;
		private VisualElement _partyContainer;
		private HomePartyView _partyView;

		private void QueryElementsSquads(VisualElement root)
		{
			_partyContainer = root.Q("PartyContainer").Required().AttachView2(this, out _partyView);
			_partyView.SetRoot(root);
			_partyButton = root.Q<LocalizedButton>("PartyButton").Required();
			_partyButton.LevelLock2(this, root, Configs.UnlockSystem.Squads, OnPartyClicked);
		}

		private void UpdateSquadsButtonVisibility()
		{
			_partyButton.SetVisibility(FeatureFlags.DISPLAY_SQUADS_BUTTON);
		}

		private void SubscribeToSquadEvents()
		{
			_partyService.HasParty.InvokeObserve(OnHasPartyChanged);
			_partyService.PartyReady.InvokeObserve(OnPartyReadyChanged);
			_partyService.Members.Observe(OnMembersChanged);
			_partyService.OperationInProgress.InvokeObserve(OnPartyLoadingProgress);
			_partyService.OnLocalPlayerKicked += OnLocalPlayerKicked;
		}

		private void UnsubscribeFromSquadEvents()
		{
			_partyService.HasParty.StopObserving(OnHasPartyChanged);
			_partyService.PartyReady.StopObserving(OnPartyReadyChanged);
			_partyService.Members.StopObserving(OnMembersChanged);
			_partyService.OperationInProgress.StopObserving(OnPartyLoadingProgress);
			_partyService.OnLocalPlayerKicked -= OnLocalPlayerKicked;
		}

		private async void OnCreateSquadButtonClicked()
		{
			FLog.Info("Creating party.");
			try
			{
				await _services.PartyService.CreateParty();
			}
			catch (PartyException pe)
			{
				HandlePartyException(pe);
			}
		}

		private async void OnJoinPartyButtonClicked(string partyId)
		{
			FLog.Info($"Joining party: {partyId}");

			try
			{
				await _services.PartyService.JoinParty(partyId);
			}
			catch (PartyException pe)
			{
				HandlePartyException(pe);
			}
		}

		private void OnPartyLoadingProgress(bool _, bool loading)
		{
			_partyButton.SetEnabled(!loading);
			OnAnyPartyUpdate();
		}

		private void OnHasPartyChanged(bool _, bool hasParty)
		{
			_partyButton.Localize(hasParty ? ScriptTerms.UITHomeScreen.leave_party : ScriptTerms.UITHomeScreen.party);
			OnAnyPartyUpdate();
		}

		private void OnPartyReadyChanged(bool _, bool isReady)
		{
			OnAnyPartyUpdate();
		}

		private void OnMembersChanged(int i, PartyMember _, PartyMember member, ObservableUpdateType type)
		{
			OnAnyPartyUpdate();
		}

		private void OnLocalPlayerKicked()
		{
			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITHomeScreen.party, ScriptLocalization.UITSquads.kicked, true,
				new GenericDialogButton());
		}

		private void HandlePartyException(PartyException pe)
		{
			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, pe.Error.GetTranslation(), true,
				new GenericDialogButton());
			FLog.Warn("Error squads", pe);
		}

		private async void OnPartyClicked()
		{
			if (_services.PartyService.HasParty.Value)
			{
				await _services.PartyService.LeaveParty();
			}
			else
			{
				var data = new PartyDialogPresenter.StateData
				{
					JoinParty = OnJoinPartyButtonClicked,
					CreateParty = OnCreateSquadButtonClicked
				};
				await _services.UIService.OpenScreen<PartyDialogPresenter>(data);
			}
		}

		private void OnAnyPartyUpdate()
		{
			UpdatePlayButton();
			UpdateGameModeButton();
		}
	}
}