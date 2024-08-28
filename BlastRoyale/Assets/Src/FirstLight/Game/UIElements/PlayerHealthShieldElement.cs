using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	public class PlayerHealthShieldElement : VisualElement
	{
		private const int HEALTH_SEPARATORS = 3;

		private const int DAMAGE_ANIM_HIDE_DURATION = 1500;

		private const string USS_BLOCK = "player-health-shield";
		private const string USS_KNOCKED_OUT_MODIFIER = USS_BLOCK + "--knockedout";
		private const string USS_HEALTH_CONTAINER = USS_BLOCK + "__health-container";
		private const string USS_SHIELD_CONTAINER = USS_BLOCK + "__shield-container";
		private const string USS_HEALTH_BAR = USS_BLOCK + "__health-bar";
		private const string USS_SHIELD_BAR = USS_BLOCK + "__shield-bar";
		private const string USS_DAMAGE_BAR = USS_BLOCK + "__damage-bar";
		private const string USS_HEALTH_LABEL = USS_BLOCK + "__health-label";
		private const string USS_SHIELD_LABEL = USS_BLOCK + "__shield-label";
		private const string USS_SEPARATOR_HOLDER = USS_BLOCK + "__separator-holder";
		private const string USS_SEPARATOR = USS_BLOCK + "__separator";

		private readonly VisualElement _healthBar;
		private readonly VisualElement _healthDamageBar;
		private readonly VisualElement _shieldBar;
		private readonly VisualElement _shieldDamageBar;
		private readonly Label _healthLabel;
		private readonly Label _shieldLabel;

		private readonly StyleValues _flexSizeZero = new () {flexGrow = 0};
		private ValueAnimation<StyleValues> _healthDamageBarAnimation;
		private ValueAnimation<StyleValues> _shieldDamageBarAnimation;

		public PlayerHealthShieldElement()
		{
			AddToClassList(USS_BLOCK);

			var shieldBarContainer = new VisualElement {name = "shield-container"};
			shieldBarContainer.AddToClassList(USS_SHIELD_CONTAINER);
			Add(shieldBarContainer);
			{
				shieldBarContainer.Add(_shieldBar = new VisualElement {name = "shield-bar"});
				_shieldBar.AddToClassList(USS_SHIELD_BAR);

				shieldBarContainer.Add(_shieldDamageBar = new VisualElement {name = "shield-damage-bar"});
				_shieldDamageBar.AddToClassList(USS_DAMAGE_BAR);

				shieldBarContainer.Add(_shieldLabel = new LabelOutlined("100") {name = "shield-label"});
				_shieldLabel.AddToClassList(USS_SHIELD_LABEL);
			}

			var healthBarContainer = new VisualElement {name = "health-container"};
			healthBarContainer.AddToClassList(USS_HEALTH_CONTAINER);
			Add(healthBarContainer);
			{
				healthBarContainer.Add(_healthBar = new VisualElement {name = "health-bar"});
				_healthBar.AddToClassList(USS_HEALTH_BAR);

				healthBarContainer.Add(_healthDamageBar = new VisualElement {name = "health-damage-bar"});
				_healthDamageBar.AddToClassList(USS_DAMAGE_BAR);

				var healthSeparatorHolder = new VisualElement {name = "separator-holder"};
				healthBarContainer.Add(healthSeparatorHolder);
				healthSeparatorHolder.AddToClassList(USS_SEPARATOR_HOLDER);
				for (int i = 0; i < HEALTH_SEPARATORS + 2; i++) // +2 for first and last one that aren't visible
				{
					var separator = new VisualElement {name = $"separator-{i}"};
					separator.AddToClassList(USS_SEPARATOR);
					healthSeparatorHolder.Add(separator);

					if (i is 0 or HEALTH_SEPARATORS + 1)
					{
						separator.SetVisibility(false);
					}
				}

				healthBarContainer.Add(_healthLabel = new LabelOutlined("90") {name = "health-label"});
				_healthLabel.AddToClassList(USS_HEALTH_LABEL);
			}
		}

		public void SetKnockedOut(bool knockedout)
		{
			EnableInClassList(USS_KNOCKED_OUT_MODIFIER, knockedout);
		}

		public void UpdateHealth(int previous, int current, int max)
		{
			_healthLabel.text = current.ToString();
			_healthBar.style.flexGrow = (float) current / max;
			_healthDamageBarAnimation?.Stop();
			if (previous > current)
			{
				var flexSize = ((float) (previous - current) / max) + _healthDamageBar.style.flexGrow.value;
				_healthDamageBar.style.flexGrow = flexSize;
				_healthDamageBarAnimation = _healthDamageBar.experimental.animation
					.Start(new StyleValues {flexGrow = flexSize}, _flexSizeZero, DAMAGE_ANIM_HIDE_DURATION).KeepAlive();
			}
			else
			{
				_healthDamageBar.style.flexGrow = 0;
			}

			if (previous != current)
			{
				_healthLabel.AnimatePing();
			}
		}

		public void UpdateShield(int previous, int current, int max)
		{
			_shieldLabel.text = current.ToString();
			_shieldBar.style.flexGrow = (float) current / max;
			_shieldDamageBarAnimation?.Stop();
			if (previous > current)
			{
				var flexSize = ((float) (previous - current) / max) + _shieldDamageBar.style.flexGrow.value;
				_shieldDamageBar.style.flexGrow = flexSize;
				_shieldDamageBarAnimation = _shieldDamageBar.experimental.animation
					.Start(new StyleValues {flexGrow = flexSize}, _flexSizeZero, DAMAGE_ANIM_HIDE_DURATION).KeepAlive();
			}
			else
			{
				_shieldDamageBar.style.flexGrow = 0;
			}

			if (previous != current)
			{
				_shieldLabel.AnimatePing();
			}
		}

		public new class UxmlFactory : UxmlFactory<PlayerHealthShieldElement, UxmlTraits>
		{
		}
	}
}