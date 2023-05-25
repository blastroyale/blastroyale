using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A death notification - displays a killer and a victim.
	/// </summary>
	public class DeathNotificationElement : VisualElement
	{
		private const string UssBlock = "death-notification";
		private const string UssContainer = UssBlock + "__container";
		private const string UssHidden = UssBlock + "--hidden";
		private const string UssHiddenEnd = UssBlock + "--hidden-end";
		private const string UssGradient = UssBlock + "__gradient";
		private const string UssGradientKiller = UssGradient + "--killer";
		private const string UssGradientVictim = UssGradient + "--victim";
		private const string UssBar = UssBlock + "__bar";
		private const string UssBarKiller = UssBar + "--killer";
		private const string UssBarVictim = UssBar + "--victim";
		private const string UssPfp = UssBlock + "__pfp";
		private const string UssPfpKiller = UssPfp + "--killer";
		private const string UssPfpVictim = UssPfp + "--victim";
		private const string UssName = UssBlock + "__name";
		private const string UssNameKiller = UssName + "--killer";
		private const string UssNameVictim = UssName + "--victim";

		private readonly VisualElement _container;

		private readonly VisualElement _killerPfp;
		private readonly Label _killerName;

		private readonly VisualElement _victimPfp;
		private readonly Label _victimName;

		public DeathNotificationElement() : this("PLAYERNAME", "PLAYERNAME")
		{
		}

		public DeathNotificationElement(string killerName, string victimName)
		{
			AddToClassList(UssBlock);

			Add(_container = new VisualElement {name = "container"});
			_container.AddToClassList(UssContainer);

			var killerGradient = new GradientElement {name = "killer-gradient"};
			_container.Add(killerGradient);
			killerGradient.AddToClassList(UssGradient);
			killerGradient.AddToClassList(UssGradientKiller);

			var killerBar = new VisualElement {name = "killer-bar"};
			_container.Add(killerBar);
			killerBar.AddToClassList(UssBar);
			killerBar.AddToClassList(UssBarKiller);

			_container.Add(_killerPfp = new VisualElement {name = "killer-pfp"});
			_killerPfp.AddToClassList(UssPfp);
			_killerPfp.AddToClassList(UssPfpKiller);

			_container.Add(_killerName = new Label(killerName) {name = "killer-name"});
			_killerName.AddToClassList(UssName);
			_killerName.AddToClassList(UssNameKiller);

			// TODO: TEMP
			var divider = new VisualElement {name = "divider"};
			_container.Add(divider);
			divider.style.flexGrow = 1;

			var victimGradient = new GradientElement {name = "victim-gradient"};
			_container.Add(victimGradient);
			victimGradient.AddToClassList(UssGradient);
			victimGradient.AddToClassList(UssGradientVictim);

			_container.Add(_victimName = new Label(victimName) {name = "victim-name"});
			_victimName.AddToClassList(UssName);
			_victimName.AddToClassList(UssNameVictim);

			_container.Add(_victimPfp = new VisualElement {name = "victim-pfp"});
			_victimPfp.AddToClassList(UssPfp);
			_victimPfp.AddToClassList(UssPfpVictim);

			var victimBar = new VisualElement {name = "victim-bar"};
			_container.Add(victimBar);
			victimBar.AddToClassList(UssBar);
			victimBar.AddToClassList(UssBarVictim);
		}

		public void SetData(string killerName, string victimName)
		{
			_killerName.text = killerName;
			_victimName.text = victimName;
		}

		public void Show()
		{
			RemoveFromClassList(UssHidden);
		}

		public void Hide(bool end)
		{
			RemoveFromClassList(UssHidden);
			RemoveFromClassList(UssHiddenEnd);
			AddToClassList(end ? UssHiddenEnd : UssHidden);
		}

		public new class UxmlFactory : UxmlFactory<DeathNotificationElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
		}
	}
}