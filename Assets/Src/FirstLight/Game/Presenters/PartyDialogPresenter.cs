using System;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Displays a dialog where you can create or join a party.
	/// </summary>
	public class PartyDialogPresenter : UiToolkitPresenterData<PartyDialogPresenter.StateData>
	{
		public struct StateData
		{
			public Action CreateParty;
			public Action<string> JoinParty;
		}

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			root.Q<LocalizedButton>("CreatePartyButton").clicked += OnCreateParty;
			root.Q<LocalizedButton>("JoinPartyButton").clicked += OnJoinParty;
			root.Q<ImageButton>("BlockerButton").clicked += () => Close(true);
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

			Close(true);
		}

		private void OnCreateParty()
		{
			Data.CreateParty();

			Close(true);
		}
	}
}