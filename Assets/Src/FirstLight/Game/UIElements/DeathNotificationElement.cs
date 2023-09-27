using FirstLight.Game.Utils;
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
		private const string USS_GRADIENT = USS_BLOCK + "__gradient";
		private const string USS_GRADIENT_KILLER = USS_GRADIENT + "--killer";
		private const string USS_GRADIENT_VICTIM = USS_GRADIENT + "--victim";
		private const string USS_BAR = USS_BLOCK + "__bar";
		private const string USS_BAR_FRIENDLY = USS_BAR + "--friendly";
		private const string USS_BAR_ENEMY = USS_BAR + "--enemy";
		private const string USS_BAR_KILLER = USS_BAR + "--killer";
		private const string USS_BAR_VICTIM = USS_BAR + "--victim";
		private const string USS_PFP = USS_BLOCK + "__pfp";
		private const string USS_PFP_KILLER = USS_PFP + "--killer";
		private const string USS_PFP_VICTIM = USS_PFP + "--victim";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_NAME_KILLER = USS_NAME + "--killer";
		private const string USS_NAME_VICTIM = USS_NAME + "--victim";
		private const string USS_NAME_FRIENDLY = USS_NAME + "--friendly";
		private const string USS_NAME_ENEMY = USS_NAME + "--enemy";
		private const string USS_KILL_ICON = USS_BLOCK + "__kill-icon";

		private readonly VisualElement _container;

		private readonly VisualElement _killerPfp;
		private readonly Label _killerName;
		private readonly VisualElement _killerBar;

		private readonly VisualElement _victimPfp;
		private readonly Label _victimName;
		private readonly VisualElement _victimBar;

		public DeathNotificationElement() : this("FRIENDLYLONGNAME", true, "ENEMYLONGNAMEHI", false, false, GameConstants.PlayerName.DEFAULT_COLOR, GameConstants.PlayerName.DEFAULT_COLOR)
		{
		}

		public DeathNotificationElement(string killerName, bool killerFriendly, string victimName, bool victimFriendly, bool suicide, StyleColor killerColor, StyleColor victimColor)
		{
			AddToClassList(USS_BLOCK);

			Add(_container = new VisualElement {name = "container"});
			_container.AddToClassList(USS_CONTAINER);

			var killerGradient = new GradientElement {name = "killer-gradient"};
			_container.Add(killerGradient);
			killerGradient.AddToClassList(USS_GRADIENT);
			killerGradient.AddToClassList(USS_GRADIENT_KILLER);

			_container.Add(_killerBar = new VisualElement {name = "killer-bar"});
			_killerBar.AddToClassList(USS_BAR);
			_killerBar.AddToClassList(USS_BAR_KILLER);
			_killerBar.AddToClassList(USS_BAR_FRIENDLY);

			_container.Add(_killerPfp = new VisualElement {name = "killer-pfp"});
			_killerPfp.AddToClassList(USS_PFP);
			_killerPfp.AddToClassList(USS_PFP_KILLER);

			_container.Add(_killerName = new Label(killerName) {name = "killer-name"});
			_killerName.AddToClassList(USS_NAME);
			_killerName.AddToClassList(USS_NAME_KILLER);

			var victimGradient = new GradientElement {name = "victim-gradient"};
			_container.Add(victimGradient);
			victimGradient.AddToClassList(USS_GRADIENT);
			victimGradient.AddToClassList(USS_GRADIENT_VICTIM);

			var killIcon = new VisualElement {name = "kill-icon"};
			_container.Add(killIcon);
			killIcon.AddToClassList(USS_KILL_ICON);

			_container.Add(_victimName = new Label(victimName) {name = "victim-name"});
			_victimName.AddToClassList(USS_NAME);
			_victimName.AddToClassList(USS_NAME_VICTIM);

			_container.Add(_victimPfp = new VisualElement {name = "victim-pfp"});
			_victimPfp.AddToClassList(USS_PFP);
			_victimPfp.AddToClassList(USS_PFP_VICTIM);

			_container.Add(_victimBar = new VisualElement {name = "victim-bar"});
			_victimBar.AddToClassList(USS_BAR);
			_victimBar.AddToClassList(USS_BAR_VICTIM);
			_victimBar.AddToClassList(USS_BAR_ENEMY);

			SetData(killerName, killerFriendly, victimName, victimFriendly, suicide, killerColor, victimColor);
		}

		public void SetData(string killerName, bool killerFriendly, string victimName, bool victimFriendly, bool suicide, StyleColor killerColor, StyleColor victimColor)
		{
			_killerName.text = killerName.ToUpper();
			_victimName.text = victimName.ToUpper();

			_killerName.style.color = killerColor;
			_victimName.style.color = victimColor;

			_killerBar.RemoveFromClassList(USS_BAR_FRIENDLY);
			_killerBar.RemoveFromClassList(USS_BAR_ENEMY);
			_killerBar.AddToClassList(killerFriendly ? USS_BAR_FRIENDLY : USS_BAR_ENEMY);
			_killerName.RemoveFromClassList(USS_NAME_FRIENDLY);
			_killerName.RemoveFromClassList(USS_NAME_ENEMY);
			_killerName.AddToClassList(killerFriendly ? USS_NAME_FRIENDLY : USS_NAME_ENEMY);

			_victimBar.RemoveFromClassList(USS_BAR_FRIENDLY);
			_victimBar.RemoveFromClassList(USS_BAR_ENEMY);
			_victimBar.AddToClassList(victimFriendly ? USS_BAR_FRIENDLY : USS_BAR_ENEMY);
			_victimName.RemoveFromClassList(USS_NAME_FRIENDLY);
			_victimName.RemoveFromClassList(USS_NAME_ENEMY);
			_victimName.AddToClassList(victimFriendly ? USS_NAME_FRIENDLY : USS_NAME_ENEMY);
		}

		public void Show()
		{
			RemoveFromClassList(USS_HIDDEN);
		}

		public void Hide(bool end)
		{
			RemoveFromClassList(USS_HIDDEN);
			RemoveFromClassList(USS_HIDDEN_END);
			AddToClassList(end ? USS_HIDDEN_END : USS_HIDDEN);
		}

		public new class UxmlFactory : UxmlFactory<DeathNotificationElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}
	}
}