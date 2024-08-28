using System;
using FirstLight.Game.Utils;
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
		private const string USS_BUTTON_LEFT = USS_BUTTON + "--left";
		private const string USS_BUTTON_RIGHT = USS_BUTTON + "--right";

		private readonly Label _tooltipLabel;
		private readonly Button _leftButton;
		private readonly Button _rightButton;

		private string _tooltipFormat = "{0}";
		private bool _usePlusMinusSymbol;

		public bool usePlusMinusSymbol
		{
			get => _usePlusMinusSymbol;
			set
			{
				_leftButton.text = "-";
				_rightButton.text = "+";
				_usePlusMinusSymbol = value;
			}
		}

		public new int highValue
		{
			get => base.highValue;
			set
			{
				base.highValue = value;
				if (usePlusMinusSymbol) return;
				_rightButton.text = value.ToString();
			}
		}

		public new int lowValue
		{
			get => base.lowValue;
			set
			{
				base.lowValue = value;
				if (usePlusMinusSymbol) return;
				_leftButton.text = value.ToString();
			}
		}

		public new int value
		{
			get => base.value;
			set
			{
				base.value = value;
				UpdateTooltip(value);
			}
		}

		public SliderIntWithButtons()
		{
			AddToClassList(USS_BLOCK);
			{
				_leftButton = new Button(() => value = Math.Max(value - 1, lowValue)) {name = "LeftButton"};
				_leftButton.AddToClassList(USS_BUTTON);
				_leftButton.AddToClassList(USS_BUTTON_LEFT);
				_leftButton.text = "0";
				Insert(0, _leftButton);
			}
			{
				_rightButton = new Button(() => value = Math.Min(value + 1, highValue)) {name = "RightButton"};
				_rightButton.AddToClassList(USS_BUTTON);
				_rightButton.AddToClassList(USS_BUTTON_RIGHT);
				_rightButton.text = "99";
				Add(_rightButton);
			}
			var tracker = this.Q<VisualElement>("unity-tracker");
			var gradient = new GradientElement {name = "TrackerGradient"};
			gradient.AddToClassList(USS_TRACKER);
			tracker.Add(gradient);

			var tooltip = new VisualElement {name = "Tooltip"};
			tooltip.AddToClassList(USS_DRAGGER_TOOLTIP);
			tooltip.Add(_tooltipLabel = new Label("0") {name = "TooltipLabel", text = ""});
			_tooltipLabel.AddToClassList(USS_DRAGGER_TOOLTIP_LABEL);
			this.Q<VisualElement>("unity-dragger").Add(tooltip);

			RegisterCallback<ChangeEvent<int>, SliderIntWithButtons>((e, p) => p.UpdateTooltip(e.newValue), this);
		}

		public void SetTooltipFormat(string format)
		{
			_tooltipFormat = format;
			_tooltipLabel.SetDisplay(!string.IsNullOrEmpty(format));
		}

		private void UpdateTooltip(int val)
		{
			_tooltipLabel.text = string.Format(_tooltipFormat, val);
		}

		protected new class UxmlFactory : UxmlFactory<SliderIntWithButtons, UxmlTraits>
		{
		}

		protected new class UxmlTraits : SliderInt.UxmlTraits
		{
			private readonly UxmlBoolAttributeDescription _usePlusMinusSymbol = new ()
			{
				name = "use-plus-minus-symbols",
				defaultValue = false,
				use = UxmlAttributeDescription.Use.Optional
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				var el = (SliderIntWithButtons) ve;
				el.usePlusMinusSymbol = _usePlusMinusSymbol.GetValueFromBag(bag, cc);
				base.Init(ve, bag, cc);
			}
		}
	}
}