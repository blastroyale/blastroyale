using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class SpecialDisplayElement : ImageButton
	{
		private const string UssBlock = "special-display";

		private const string UssIcon = UssBlock + "__icon";
		private const string UssIconModifier = UssIcon + "--";

		// private GameId specialId { get; set; }

		private VisualElement _icon;

		public SpecialDisplayElement() : this(GameId.SpecialAimingAirstrike)
		{
		}

		public SpecialDisplayElement(GameId special)
		{
			AddToClassList(UssBlock);

			Add(_icon = new VisualElement());
			_icon.AddToClassList(UssIcon);

			_icon.AddToClassList(UssIconModifier + special.ToString().ToLowerInvariant().Replace("special", ""));
		}
		
		public new class UxmlFactory : UxmlFactory<SpecialDisplayElement, UxmlTraits>
		{
		}
	}
}