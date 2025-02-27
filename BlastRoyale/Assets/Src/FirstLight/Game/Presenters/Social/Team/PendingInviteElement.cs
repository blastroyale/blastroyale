using System;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.Social.Team
{
	/// <summary>
	/// Element used in the team ui to display a pending invite
	/// </summary>
	public class PendingInviteElement : VisualElement
	{
		[Q] private Label _playerName;
		[Q] private Label _status;
		[Q] private ImageButton _cancelButton;

		public PartyInvite Invite { get; private set; }
		public bool IsDeleting { get; private set; }

		public PendingInviteElement()
		{
			this.LoadTemplateAndBind("TemplatePendingInvite");
		}

		public PendingInviteElement SetPlayerInvite(PartyInvite invite)
		{
			Invite = invite;
			_playerName.text = invite.PlayerName;
			return this;
		}

		public PendingInviteElement OnCancel(Action action)
		{
			_cancelButton.clicked += action;
			return this;
		}

		public void Decline(Action onDeleted)
		{
			AddToClassList("pending--declined");
			_status.text = "Declined";
			IsDeleting = true;
			schedule.Execute(() =>
			{
				this.SetDisplay(false);
				IsDeleting = false;
				onDeleted?.Invoke();
			}).ExecuteLater(3000);
		}

		public new class UxmlFactory : UxmlFactory<PendingInviteElement, UxmlTraits>
		{
		}
	}
}