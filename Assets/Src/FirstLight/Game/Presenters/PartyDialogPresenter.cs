using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Displays a dialog where you can create or join a party.
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class PartyDialogPresenter : UIPresenterData<PartyDialogPresenter.StateData>
	{
		public class StateData
		{
			public Action CreateParty;
			public Action<string> JoinParty;
		}

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			Root.Q<LocalizedLabel>("PartyDescription").Localize(ScriptTerms.UITHomeScreen.party_popup_desc);
			Root.Q<LocalizedButton>("CreatePartyButton").SetDisplay(true);
			
			Root.Q<LocalizedButton>("CreatePartyButton").clicked += OnCreateParty;
			Root.Q<LocalizedButton>("JoinPartyButton").clicked += OnJoinParty;
			Root.Q<ImageButton>("BlockerButton").clicked += () => _services.UIService.CloseScreen<PartyDialogPresenter>().Forget();
		}

		private void OnJoinParty()
		{
			var btn = new GenericDialogButton<string>()
			{
				ButtonOnClick = id => Data.JoinParty(id),
				ButtonText = ScriptLocalization.UITHomeScreen.join
			};

			_services.GenericDialogService.OpenInputDialog(ScriptLocalization.UITHomeScreen.party_id,
				ScriptLocalization.UITHomeScreen.party_id_desc, "", btn, true);

			if (_services.UIService.IsScreenOpen<PartyDialogPresenter>())
			{
				_services.UIService.CloseScreen<PartyDialogPresenter>().Forget();
			}
		}

		private void OnCreateParty()
		{
			Data.CreateParty();

			_services.UIService.CloseScreen<PartyDialogPresenter>().Forget();
		}
	}
}