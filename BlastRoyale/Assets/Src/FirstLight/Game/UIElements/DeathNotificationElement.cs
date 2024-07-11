using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A death notification - displays a killer and a victim.
	/// </summary>
	public class DeathNotificationElement : VisualElement
	{
		private const string USS_BLOCK = "death-notification";
		private const string USS_CONTAINER = USS_BLOCK + "__container";
		private const string USS_HIDDEN = USS_BLOCK + "--hidden";
		private const string USS_HIDDEN_END = USS_BLOCK + "--hidden-end";
		private const string USS_PFP = USS_BLOCK + "__pfp";
		private const string USS_PFP_KILLER = USS_PFP + "--killer";
		private const string USS_PFP_VICTIM = USS_PFP + "--victim";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_NAME_KILLER = USS_NAME + "--killer";
		private const string USS_NAME_VICTIM = USS_NAME + "--victim";
		private const string USS_NAME_FRIENDLY = USS_NAME + "--friendly";
		private const string USS_NAME_ENEMY = USS_NAME + "--enemy";
		private const string USS_KILL_ICON = USS_BLOCK + "__kill-icon";

		public bool JobDone { get; set; }

		private readonly VisualElement _container;

		private readonly VisualElement _killerPfp;
		private readonly Label _killerName;

		private readonly VisualElement _victimPfp;
		private readonly Label _victimName;

		public DeathNotificationElement() : this("FRIENDLYLONGNAME", true, null, "ENEMYLONGNAMEHI", false, null, false,
			GameConstants.PlayerName.DEFAULT_COLOR, GameConstants.PlayerName.DEFAULT_COLOR)
		{
		}

		public DeathNotificationElement(string killerName, bool killerFriendly, Sprite killerCharacterPfpSprite, string victimName, bool victimFriendly,
										Sprite victimCharacterPfpSprite, bool suicide, StyleColor killerColor, StyleColor victimColor)
		{
			AddToClassList(USS_BLOCK);

			Add(_container = new VisualElement {name = "container"});
			_container.AddToClassList(USS_CONTAINER);

			_container.Add(_killerPfp = new VisualElement {name = "killer-pfp"});
			_killerPfp.AddToClassList(USS_PFP);
			_killerPfp.AddToClassList(USS_PFP_KILLER);

			_container.Add(_killerName = new Label(killerName) {name = "killer-name"});
			_killerName.AddToClassList(USS_NAME);
			_killerName.AddToClassList(USS_NAME_KILLER);

			var killIcon = new VisualElement {name = "kill-icon"};
			_container.Add(killIcon);
			killIcon.AddToClassList(USS_KILL_ICON);

			_container.Add(_victimName = new Label(victimName) {name = "victim-name"});
			_victimName.AddToClassList(USS_NAME);
			_victimName.AddToClassList(USS_NAME_VICTIM);

			_container.Add(_victimPfp = new VisualElement {name = "victim-pfp"});
			_victimPfp.AddToClassList(USS_PFP);
			_victimPfp.AddToClassList(USS_PFP_VICTIM);

			SetData(killerName, killerFriendly, killerCharacterPfpSprite, victimName, victimFriendly, victimCharacterPfpSprite, suicide, killerColor, victimColor);
		}

		public void SetData(string killerName, bool killerFriendly, Sprite killerCharacterPfpSprite, string victimName, bool victimFriendly,
							Sprite victimCharacterPfpSprite, bool suicide, StyleColor killerColor, StyleColor victimColor)
		{
			_killerName.text = killerName.ToUpper();
			_victimName.text = victimName.ToUpper();

			_killerName.style.color = killerColor;
			_victimName.style.color = victimColor;

			_killerName.RemoveFromClassList(USS_NAME_FRIENDLY);
			_killerName.RemoveFromClassList(USS_NAME_ENEMY);
			_killerName.AddToClassList(killerFriendly ? USS_NAME_FRIENDLY : USS_NAME_ENEMY);

			_victimName.RemoveFromClassList(USS_NAME_FRIENDLY);
			_victimName.RemoveFromClassList(USS_NAME_ENEMY);
			_victimName.AddToClassList(victimFriendly ? USS_NAME_FRIENDLY : USS_NAME_ENEMY);

			if (Application.isPlaying)
			{
				// killerAvatarUrl = "https://mainnetprodflghubstorage.blob.core.windows.net/collections/corpos/1.png".Replace("1.png",
				//  	$"{Random.Range(1, 888)}.png");
				// victimAvatarUrl = "https://mainnetprodflghubstorage.blob.core.windows.net/collections/corpos/1.png".Replace("1.png",
				// 	$"{Random.Range(1, 888)}.png");
				// LoadAvatars(killerAvatarUrl, victimAvatarUrl);
				_killerPfp.style.backgroundImage = new StyleBackground(killerCharacterPfpSprite);
				_victimPfp.style.backgroundImage = new StyleBackground(victimCharacterPfpSprite);
			}
		}

		public void Show()
		{
			RemoveFromClassList(USS_HIDDEN);
		}

		public void Hide(bool end)
		{
			JobDone = end;
			RemoveFromClassList(USS_HIDDEN);
			RemoveFromClassList(USS_HIDDEN_END);
			AddToClassList(end ? USS_HIDDEN_END : USS_HIDDEN);
		}

		private void LoadAvatars(string killerAvatarUrl, string victimAvatarUrl)
		{
			// TODO: This needs more handling if we start pooling

			var rts = MainInstaller.ResolveServices().RemoteTextureService;

			if (!string.IsNullOrEmpty(killerAvatarUrl))
			{
				rts.RequestTexture(
					killerAvatarUrl,
					tex =>
					{
						if (panel == null) return;
						_killerPfp.style.backgroundImage = new StyleBackground(tex);
					});
			}

			if (!string.IsNullOrEmpty(victimAvatarUrl))
			{
				rts.RequestTexture(
					victimAvatarUrl,
					tex =>
					{
						if (panel == null) return;
						_victimPfp.style.backgroundImage = new StyleBackground(tex);
					});
			}
		}

		public new class UxmlFactory : UxmlFactory<DeathNotificationElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}
	}
}