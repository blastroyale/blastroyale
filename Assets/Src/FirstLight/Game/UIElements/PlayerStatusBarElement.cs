using System;
using FirstLight.Game.Utils;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Random = UnityEngine.Random;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the status bar above a player's head.
	/// </summary>
	public class PlayerStatusBarElement : VisualElement
	{
		private const int MAX_AMMO_BARS = 20;

		private const int DAMAGE_NUMBER_MAX_POOL_SIZE = 5;
		private const int DAMAGE_NUMBER_ANIM_DURATION = 1000;
		private const int DAMAGE_ANIMATION_DURATION = 1500; // How long the bar takes to fade out after taking damage.

		private const string USS_BLOCK = "player-status-bar";
		private const string USS_FRIENDLY = USS_BLOCK + "--friendly";
		private const string USS_BACKGROUND = USS_BLOCK + "__background";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_LEVEL = USS_BLOCK + "__level";
		private const string USS_SHIELD_HOLDER = USS_BLOCK + "__shield-holder";
		private const string USS_SHIELD_BAR = USS_BLOCK + "__shield-bar";
		private const string USS_HEALTH_HOLDER = USS_BLOCK + "__health-holder";
		private const string USS_HEALTH_BAR = USS_BLOCK + "__health-bar";
		private const string USS_AMMO_HOLDER = USS_BLOCK + "__ammo-holder";
		private const string USS_AMMO_RELOAD_BAR = USS_BLOCK + "__ammo-reload-bar";
		private const string USS_AMMO_SEGMENT = USS_BLOCK + "__ammo-segment";
		private const string USS_NOTIFICATION = USS_BLOCK + "__notification";
		private const string USS_NOTIFICATION_ICON = USS_BLOCK + "__notification-icon";
		private const string USS_NOTIFICATION_SHIELDS = USS_NOTIFICATION + "--shields";
		private const string USS_NOTIFICATION_HEALTH = USS_NOTIFICATION + "--health";
		private const string USS_NOTIFICATION_AMMO = USS_NOTIFICATION + "--ammo";
		private const string USS_NOTIFICATION_LVLUP = USS_NOTIFICATION + "--lvlup";
		private const string USS_DAMAGE_HOLDER = USS_BLOCK + "__damage-holder";
		private const string USS_DAMAGE_NUMBER = USS_BLOCK + "__damage-number";

		private readonly Label _name;
		private readonly Label _level;
		private readonly VisualElement _shieldBar;
		private readonly VisualElement _healthBar;
		private readonly VisualElement _ammoHolder;
		private readonly VisualElement _ammoReloadBar;
		private readonly Label _notificationLabel;

		private readonly Label[] _damageNumberPool = new Label[DAMAGE_NUMBER_MAX_POOL_SIZE];
		private readonly ValueAnimation<float>[] _damageNumberAnimsPool = new ValueAnimation<float>[DAMAGE_NUMBER_MAX_POOL_SIZE];
		private int _damageNumberIndex;

		private bool _isFriendly;

		private readonly ValueAnimation<float> _opacityAnimation;
		private readonly IVisualElementScheduledItem _opacityAnimationHandle;
		private readonly IVisualElementScheduledItem _notificationHandle;
		private ValueAnimation<Vector3> _reloadAnimation;

		public PlayerStatusBarElement()
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

			Add(_ammoReloadBar = new VisualElement {name = "reload-bar"});
			_ammoReloadBar.AddToClassList(USS_AMMO_RELOAD_BAR);

			Add(_level = new Label("10") {name = "level"});
			_level.AddToClassList(USS_LEVEL);

			Add(_notificationLabel = new Label("MAX") {name = "notification-label"});
			_notificationLabel.AddToClassList(USS_NOTIFICATION);
			_notificationLabel.AddToClassList(USS_NOTIFICATION_HEALTH);
			{
				var notificationIcon = new VisualElement {name = "notification-icon"};
				_notificationLabel.Add(notificationIcon);
				notificationIcon.AddToClassList(USS_NOTIFICATION_ICON);
			}

			_opacityAnimation = experimental.animation.Start(1f, 0f, DAMAGE_ANIMATION_DURATION,
				(e, o) => e.style.opacity = o).KeepAlive();
			_opacityAnimation.Stop();

			_opacityAnimationHandle = schedule.Execute(_opacityAnimation.Start);
			_opacityAnimationHandle.Pause();

			_notificationHandle = schedule.Execute(() => { _notificationLabel.SetDisplay(false); });
			_notificationHandle.Pause();

			var damageHolder = new VisualElement {name = "damage-holder"};
			Add(damageHolder);
			damageHolder.AddToClassList(USS_DAMAGE_HOLDER);
			damageHolder.usageHints = UsageHints.GroupTransform;
			{
				for (int i = 0; i < DAMAGE_NUMBER_MAX_POOL_SIZE; i++)
				{
					// Create damage label
					var damageNumber = new Label("0") {name = "damage-number"};
					damageHolder.Add(damageNumber);
					damageNumber.AddToClassList(USS_DAMAGE_NUMBER);
					damageNumber.userData = Random.Range(-5f, 5f); // Random value stored in userData for convenience
					_damageNumberPool[i] = damageNumber;

					// Create animation
					var anim = damageNumber.experimental.animation.Start(0f, 1f, DAMAGE_NUMBER_ANIM_DURATION, AnimateDamageNumber);
					anim.KeepAlive().Stop();
					_damageNumberAnimsPool[i] = anim;
				}
			}

			SetIsFriendly(true);
			SetMagazine(4, 6);
		}

		/// <summary>
		/// Marks this bar as friendly (always visible with ammo) or not (only visible when damaged and no ammo info).
		/// </summary>
		public void SetIsFriendly(bool isFriendly)
		{
			_isFriendly = isFriendly;

			EnableInClassList(USS_FRIENDLY, isFriendly);

			style.opacity = isFriendly ? 1f : 0f;
		}

		/// <summary>
		/// If the bar is not friendly, display it for some amount of time.
		/// </summary>
		public void PingDamage(uint damage)
		{
			_damageNumberIndex = (_damageNumberIndex + 1) % DAMAGE_NUMBER_MAX_POOL_SIZE;
			var damageNumberLabel = _damageNumberPool[_damageNumberIndex];
			var damageNumberAnim = _damageNumberAnimsPool[_damageNumberIndex];
			damageNumberLabel.text = damage.ToString();
			damageNumberLabel.BringToFront();
			damageNumberAnim.Stop();
			damageNumberAnim.Start();

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
		/// Sets the magazine size and how full it is. Only affects friendly players.
		/// </summary>
		public void SetMagazine(int currentMagazine, int maxMagazine)
		{
			if (!_isFriendly) return;

			var infiniteMagazine = maxMagazine <= 0;
			var totalBars = infiniteMagazine ? 1 : Mathf.Min(maxMagazine, MAX_AMMO_BARS);
			var visibleBars = infiniteMagazine
				? 1
				: Mathf.FloorToInt(currentMagazine * Mathf.Min((float) MAX_AMMO_BARS / maxMagazine, 1f));

			// Max ammo
			if (_ammoHolder.childCount > totalBars)
			{
				for (var i = _ammoHolder.childCount - 1; i >= totalBars; i--)
				{
					_ammoHolder.RemoveAt(i);
				}
			}
			else if (_ammoHolder.childCount < totalBars)
			{
				for (var i = _ammoHolder.childCount; i < totalBars; i++)
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
				segment.SetVisibility(index++ < visibleBars);
			}

			// Cancel reload
			_reloadAnimation?.Stop();
			_ammoReloadBar.SetDisplay(false);
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
			_level.AnimatePing();
		}

		/// <summary>
		/// Sets the max and current health (i.e. the size of the health bar).
		/// </summary>
		public void SetHealth(int previous, int current, int max)
		{
			// TODO: Handle red bar when damaged (i.e. previous < current)
			_healthBar.style.flexGrow = (float) current / max;
		}

		/// <summary>
		/// Shows a notification above the player's head.
		/// </summary>
		public void ShowNotification(NotificationType type)
		{
			_notificationLabel.RemoveModifiers();

			switch (type)
			{
				case NotificationType.MaxShields:
					_notificationLabel.text = ScriptLocalization.UITMatch.max;
					_notificationLabel.AddToClassList(USS_NOTIFICATION_SHIELDS);
					break;
				case NotificationType.MaxAmmo:
					_notificationLabel.text = ScriptLocalization.UITMatch.max;
					_notificationLabel.AddToClassList(USS_NOTIFICATION_AMMO);
					break;
				case NotificationType.LevelUp:
					_notificationLabel.text = ScriptLocalization.UITMatch.lvl_up;
					_notificationLabel.AddToClassList(USS_NOTIFICATION_LVLUP);
					break;
				case NotificationType.MaxHealth:
					_notificationLabel.text = ScriptLocalization.UITMatch.max;
					_notificationLabel.AddToClassList(USS_NOTIFICATION_HEALTH);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}

			_notificationLabel.SetDisplay(true);
			_notificationHandle.ExecuteLater(1000);
			_notificationLabel.AnimatePing();
		}

		/// <summary>
		/// Displays the reload animation.
		/// </summary>
		public void ShowReload(int reloadTime)
		{
			if (!_isFriendly) return;

			_ammoReloadBar.SetDisplay(true);

			_ammoReloadBar.transform.position = Vector3.zero;

			_reloadAnimation?.Stop();
			_reloadAnimation = _ammoReloadBar.experimental.animation.Position(new Vector3(130, 0, 0), reloadTime)
				.OnCompleted(() =>
				{
					_ammoReloadBar.SetDisplay(false);
					_reloadAnimation = null;
				}).Ease(Easing.Linear);
			_reloadAnimation.Start();
		}

		private void AnimateDamageNumber(VisualElement damageNumber, float t)
		{
			// Bezier curve
			var random = (float) damageNumber.userData;
			var p0 = new Vector2(0, 0) + Vector2.one * random / 2f; // Less random offset on first point
			var p1 = new Vector2(50, -10) + Vector2.one * random;
			var p2 = new Vector2(75, 50) + Vector2.one * (random * 2f); // More random offset on last point

			var pd1 = Vector2.Lerp(p0, p1, t);
			var pd2 = Vector2.Lerp(p1, p2, t);
			var pf = Vector2.Lerp(pd1, pd2, t);

			// Opacity between 0-0.2 of d
			var opacity = Mathf.Clamp01(1f / 0.50f * (1f - t));

			// Scale overshoot (bump)
			var scale = t < 0.1 ? Mathf.Lerp(0, 2.5f, t * 10) :
				t < 0.3 ? Mathf.Lerp(2.5f, 1f, (t - 0.1f) * 10) :
				1f;

			damageNumber.transform.position = pf;
			damageNumber.transform.scale = new Vector3(scale, scale, 1);
			damageNumber.style.opacity = opacity;
		}

		public enum NotificationType
		{
			MaxShields,
			MaxHealth,
			MaxAmmo,
			LevelUp
		}

		public new class UxmlFactory : UxmlFactory<PlayerStatusBarElement, UxmlTraits>
		{
		}
	}
}