using System;
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
		[Q] private ImageButton _cancelButton;

		public PendingInviteElement()
		{
			this.LoadTemplateAndBind("TemplatePendingInvite");
		}

		public PendingInviteElement SetPlayerName(string playerName)
		{
			_playerName.text = playerName;
			return this;
		}

		public PendingInviteElement OnCancel(Action action)
		{
			_cancelButton.clicked += action;
			return this;
		}

		public new class UxmlFactory : UxmlFactory<PendingInviteElement, UxmlTraits>
		{
		}
	}
}