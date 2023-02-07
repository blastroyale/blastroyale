using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using FirstLight;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Utils;
using I2.Loc;
using UnityEngine;

public partial class SROptions
{
	private bool _initiatedParty = false;
	private IGenericDialogService _dialogService;
	private IPartyService _partyService;

	private readonly bool _observeChanges = false;

	void InitPartyDebug()
	{
		if (_initiatedParty)
		{
			return;
		}

		_partyService = MainInstaller.Resolve<IGameServices>().PartyService;
		_dialogService = MainInstaller.Resolve<IGameServices>().GenericDialogService;

		_initiatedParty = true;

		if (_observeChanges)
		{
			_partyService.Members.Observe((i, memberBefore, memberAfter, updateType) =>
			{
				// Party not fully loaded yet
				if (!_partyService.HasParty.Value)
				{
					return;
				}

				bool isLocalMember = memberBefore is {Local: true} || memberAfter is {Local: true};

				if (!isLocalMember)
				{
					if (updateType == ObservableUpdateType.Added)
					{
						ShowMessage("" + memberAfter.DisplayName + " joined the party!");
					}

					if (updateType == ObservableUpdateType.Removed)
					{
						ShowMessage("" + memberBefore.DisplayName + " left the party!");
					}
				}
			});
			_partyService.HasParty.Observe((before, after) =>
			{
				PartyId = _partyService.PartyCode.Value;
				// Joined party
				if (after)
				{
					// Only have one member means and the player just joined means it was created
					if (_partyService.Members.Count == 1)
					{
						ShowMessage("Party created " + _partyService.PartyCode.Value + " !");
						return;
					}

					var leader = _partyService.Members.FirstOrDefault(m => m.Leader)?.DisplayName ?? "No Name";
					ShowMessage("You joined the party " + _partyService.PartyCode.Value + " of " + leader);
				}
				else
				{
					// Left party
					// If the player HasParty is set to `false` and player is not a member, it got kicked, otherwise the player left
					bool isMember = _partyService.Members.Any(m => m.Local);
					if (isMember)
					{
						ShowMessage("You left the party " + _partyService.PartyCode.Value);
					}
					else
					{
						ShowMessage("You got kicked from the party! \nStop being so annoying!!!");
					}
				}
			});
		}
	}

	public void ShowMessage(string str)
	{
		var confirmButton = new GenericDialogButton
		{
			ButtonText = ScriptLocalization.UITShared.ok,
			ButtonOnClick = () => { _dialogService.CloseDialog(); }
		};
		_dialogService.OpenButtonDialog(
			"Party",
			str,
			false,
			confirmButton);
	}

	[Category("Party")] [Sort(0)] public String PartyId { get; set; }


	[Category("Party")]
	[Sort(1)]
	public void Join()
	{
		InitPartyDebug();
		var services = MainInstaller.Resolve<IGameServices>();
		services.PartyService.JoinParty(PartyId);
	}

	[Category("Party")]
	[Sort(2)]
	public void Create()
	{
		InitPartyDebug();
		var services = MainInstaller.Resolve<IGameServices>();
		services.PartyService.CreateParty();
	}


	[Category("Party")]
	[Sort(3)]
	public void Leave()
	{
		InitPartyDebug();
		var services = MainInstaller.Resolve<IGameServices>();
		services.PartyService.LeaveParty();
	}

	[Category("Party")]
	[Sort(4)]
	public void KickOther()
	{
		InitPartyDebug();
		var party = MainInstaller.Resolve<IGameServices>().PartyService;
		foreach (var partyMember in party.Members)
		{
			if (!partyMember.Local)
			{
				party.Kick(partyMember.PlayfabID);
			}
		}
	}

	[Category("Party")]
	[Sort(5)]
	public void ShowMembers()
	{
		InitPartyDebug();
		if (_partyService.Members.Count == 0)
		{
			ShowMessage("Empty party!");
			return;
		}

		StringBuilder builder = new($"  Code {_partyService.PartyCode.Value} \n\n");
		var leader = _partyService.Members.FirstOrDefault(m => m.Leader);

		builder.AppendLine("        BIG BOSS        ");
		builder.AppendLine(Display(leader));

		if (_partyService.Members.Count > 1)
		{
			builder.AppendLine();
			builder.AppendLine("    Bajoran Workers   ");
			foreach (var partyServiceMember in _partyService.Members)
			{
				if (partyServiceMember != leader)
				{
					builder.AppendLine(Display(partyServiceMember));
				}
			}
		}

		ShowMessage(builder.ToString());
	}

	private String Display(PartyMember pm)
	{
		return $"{pm.PlayfabID}: {pm.DisplayName}(LVL {pm.BPPLevel} - TRO {pm.Trophies})";
	}
}