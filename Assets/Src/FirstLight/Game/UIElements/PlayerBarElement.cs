using FirstLight.Game.Utils;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the status bar above a player's head.
	/// </summary>
	public class PlayerBarElement : VisualElement
	{
		// TODO: Add dividers to the health and shield bars.
		private const int SHIELD_DIVIDER_AMOUNT = 100;
		private const int HEALTH_DIVIDER_AMOUNT = 100;

		private const int DAMAGE_ANIMATION_DURATION = 1500; // How long the bar takes to fade out after taking damage.

		private const string USS_BLOCK = "playerbar";
		private const string USS_BACKGROUND = USS_BLOCK + "__background";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_LEVEL = USS_BLOCK + "__level";
		private const string USS_SHIELD_HOLDER = USS_BLOCK + "__shield-holder";
		private const string USS_SHIELD_BAR = USS_BLOCK + "__shield-bar";
		private const string USS_HEALTH_HOLDER = USS_BLOCK + "__health-holder";
		private const string USS_HEALTH_BAR = USS_BLOCK + "__health-bar";
		private const string USS_AMMO_HOLDER = USS_BLOCK + "__ammo-holder";
		private const string USS_AMMO_SEGMENT = USS_BLOCK + "__ammo-segment";

		private readonly Label _name;
		private readonly Label _level;
		private readonly VisualElement _shieldBar;
		private readonly VisualElement _healthBar;
		private readonly VisualElement _ammoHolder;

		private bool _isFriendly;

		private readonly ValueAnimation<float> _opacityAnimation;
		private readonly IVisualElementScheduledItem _opacityAnimationHandle;

		public PlayerBarElement()
		{
			usageHints = UsageHints.DynamicTransform;

			AddToClassList(USS_BLOCK);

			Add(_name = new Label("PLAYER NAME") {name = "name"});
			_name.AddToClassList(USS_NAME);

			var background = new VisualElement {name = "background"};
			Add(background);
			background.AddToClassList(USS_BACKGROUND);

			var shieldHolder = new VisualElement {name = "shield-holder"};
			Add(shieldHolder);
			shieldHolder.AddToClassList(USS_SHIELD_HOLDER);
			{
				shieldHolder.Add(_shieldBar = new VisualElement {name = "shield-bar"});
				_shieldBar.AddToClassList(USS_SHIELD_BAR);
			}

			var healthHolder = new VisualElement {name = "health-holder"};
			Add(healthHolder);
			healthHolder.AddToClassList(USS_HEALTH_HOLDER);
			{
				healthHolder.Add(_healthBar = new VisualElement {name = "health-bar"});
				_healthBar.AddToClassList(USS_HEALTH_BAR);
			}

			Add(_ammoHolder = new VisualElement {name = "ammo-holder"});
			_ammoHolder.AddToClassList(USS_AMMO_HOLDER);

			Add(_level = new Label("10") {name = "level"});
			_level.AddToClassList(USS_LEVEL);

			_opacityAnimation = experimental.animation.Start(1f, 0f, DAMAGE_ANIMATION_DURATION,
				(e, o) => e.style.opacity = o).KeepAlive();
			_opacityAnimation.Stop();

			_opacityAnimationHandle = schedule.Execute(_opacityAnimation.Start);
			_opacityAnimationHandle.Pause();

			SetIsFriendly(true);
			SetMagazine(4, 6);
		}

		/// <summary>
		/// Marks this bar as friendly (always visible with ammo) or not (only visible when damaged and no ammo info).
		/// </summary>
		public void SetIsFriendly(bool isFriendly)
		{
			_isFriendly = isFriendly;
			_ammoHolder.SetDisplay(_isFriendly);

			style.opacity = isFriendly ? 1f : 0f;
		}

		/// <summary>
		/// If the bar is not friendly, display it for some amount of time.
		/// </summary>
		public void PingDamage()
		{
			if (_isFriendly) return;

			_opacityAnimation.Stop();
			style.opacity = 1f;
			_opacityAnimationHandle.ExecuteLater(GameConstants.Visuals.GAMEPLAY_POST_ATTACK_HIDE_DURATION);
		}

		/// <summary>
		/// Sets the name of the player.
		/// </summary>
		public void SetName(string playerName)
		{
			_name.text = playerName;
		}

		/// <summary>
		/// Sets the magazine size and how full it is. Only affects firendly players.
		/// </summary>
		public void SetMagazine(int currentMagazine, int maxMagazine)
		{
			if (!_isFriendly) return;

			if (maxMagazine <= 0)
			{
				_ammoHolder.Clear();
				return;
			}

			// Max ammo
			if (_ammoHolder.childCount > maxMagazine)
			{
				for (var i = _ammoHolder.childCount - 1; i >= maxMagazine; i--)
				{
					_ammoHolder.RemoveAt(i);
				}
			}
			else if (_ammoHolder.childCount < maxMagazine)
			{
				for (var i = _ammoHolder.childCount; i < maxMagazine; i++)
				{
					var segment = new VisualElement {name = "ammo-segment"};
					_ammoHolder.Add(segment);
					segment.AddToClassList(USS_AMMO_SEGMENT);
				}
			}

			// Current ammo
			int index = 0;
			foreach (var segment in _ammoHolder.Children())
			{
				segment.SetVisibility(index++ < currentMagazine);
			}
		}

		/// <summary>
		/// Sets the max and current shield (i.e. the size of the shield bar).
		/// </summary>
		public void SetShield(int current, int max)
		{
			_shieldBar.style.flexGrow = (float) current / max;
		}

		/// <summary>
		/// Sets the level of the player.
		/// </summary>
		public void SetLevel(int level)
		{
			_level.text = level.ToString();
		}

		/// <summary>
		/// Sets the max and current health (i.e. the size of the health bar).
		/// </summary>
		public void SetHealth(int previous, int current, int max)
		{
			// TODO: Handle red bar when damaged (i.e. previous < current)
			_healthBar.style.flexGrow = (float) current / max;
		}

		public new class UxmlFactory : UxmlFactory<PlayerBarElement, UxmlTraits>
		{
		}
	}
}