using System.Collections.Generic;
using FirstLight.Game.Utils;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A button that has it's text set from a I2 Localization key.
	/// </summary>
	public class SliderIntWithButtons : SliderInt
	{
		private const string USS_BLOCK = "slider-with-buttons";
		private const string USS_BUTTON = USS_BLOCK + "__button";
		private const string USS_TRACKER = USS_BLOCK + "__gradient";
		private const string USS_DRAGGER_TOOLTIP = USS_BLOCK + "__dragger__tooltip";
		private const string USS_DRAGGER_TOOLTIP_LABEL = USS_DRAGGER_TOOLTIP + "__label";
		private const string USS_BUTTON_LABEL = USS_BUTTON + "__label";
		private const string USS_BUTTON_LEFT = USS_BUTTON + "--left";
		private const string USS_BUTTON_RIGHT = USS_BUTTON + "--right";

		private Label _tooltipLabel;

		public SliderIntWithButtons()
		{
			AddToClassList(USS_BLOCK);
			{
				var leftButton = new ImageButton(() => value--) {name = "LeftButton"};
				leftButton.AddToClassList(USS_BUTTON);
				leftButton.AddToClassList(USS_BUTTON_LEFT);
				var leftLabel = new Label("-");
				leftLabel.AddToClassList(USS_BUTTON_LABEL);
				leftButton.Add(leftLabel);
				Insert(0, leftButton);
			}
			{
				var rightButton = new ImageButton(() => value++) {name = "RightButton"};
				rightButton.AddToClassList(USS_BUTTON);
				rightButton.AddToClassList(USS_BUTTON_RIGHT);
				var rightLabel = new Label("+");
				rightLabel.AddToClassList(USS_BUTTON_LABEL);
				rightButton.Add(rightLabel);
				Add(rightButton);
			}
			var tracker = this.Q<VisualElement>("unity-tracker");
			var gradient = new GradientElement {name = "TrackerGradient"};
			gradient.AddToClassList(USS_TRACKER);
			tracker.Add(gradient);

			var tooltip = new VisualElement {name = "Tooltip"};
			tooltip.AddToClassList(USS_DRAGGER_TOOLTIP);
			tooltip.Add(_tooltipLabel = new Label() {name = "TooltipLabel", text = ""});
			_tooltipLabel.AddToClassList(USS_DRAGGER_TOOLTIP_LABEL);
			this.Q<VisualElement>("unity-dragger").Add(tooltip);
		}

		public void SetTooltipText(string text)
		{
			_tooltipLabel.SetDisplay(!string.IsNullOrEmpty(text));
			_tooltipLabel.text = text;
		}

		protected new class UxmlFactory : UxmlFactory<SliderIntWithButtons, UxmlTraits>
		{
		}
	}
}