using System;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the status bar above a player's head.
	/// </summary>
	public class PlayerStatusBarElement : VisualElement
	{
		private const int DAMAGE_NUMBER_MAX_POOL_SIZE = 15;
		private const int DAMAGE_NUMBER_ANIM_DURATION = 1000;

		private const float SMALL_DAMAGE = 32;
		private const float DAMAGE_SCALE = 64;

		private const string USS_BLOCK = "player-status-bar";
		private const string USS_NOTIFICATION = USS_BLOCK + "__notification";
		private const string USS_NOTIFICATION_HIDDEN = USS_NOTIFICATION + "--hidden";
		private const string USS_NOTIFICATION_SHIELDS = USS_NOTIFICATION + "--shields";
		private const string USS_NOTIFICATION_HEALTH = USS_NOTIFICATION + "--health";
		private const string USS_NOTIFICATION_AMMO = USS_NOTIFICATION + "--ammo";
		private const string USS_NOTIFICATION_LVLUP = USS_NOTIFICATION + "--lvlup";
		private const string USS_NOTIFICATION_MISC = USS_NOTIFICATION + "--misc";
		private const string USS_NOTIFICATION_NOINPUTWARNING = USS_NOTIFICATION + "--noinputwarning";
		private const string USS_NOTIFICATION_WOUNDED = USS_NOTIFICATION + "--wounded";
		private const string USS_DAMAGE_HOLDER = USS_BLOCK + "__damage-holder";
		private const string USS_DAMAGE_NUMBER = USS_BLOCK + "__damage-number";

		private readonly PlayerHealthShieldElement _healthShield;
		private readonly Label _notificationLabel;
		private readonly Label[] _damageNumbersPool = new Label[DAMAGE_NUMBER_MAX_POOL_SIZE];
		private readonly ValueAnimation<float>[] _damageNumberAnims = new ValueAnimation<float>[DAMAGE_NUMBER_MAX_POOL_SIZE];
		private readonly float[] _damageNumberAnimOffsets = new float[DAMAGE_NUMBER_MAX_POOL_SIZE];
		private readonly float[] _damageNumberAnimValues = new float[DAMAGE_NUMBER_MAX_POOL_SIZE];
		private readonly EventKey[] _damageEventKeys = new EventKey[DAMAGE_NUMBER_MAX_POOL_SIZE];
		private int _damageNumberIndex;
		private float _maxHealth;
		private bool _isFriendly;
		private readonly IVisualElementScheduledItem _notificationHandle;
		private readonly StyleColor _defaultPingDmgColor = new (new Color(1f, 1f, 1f));

		private bool _barsEnabled = true;

		public PlayerStatusBarElement()
		{
			usageHints = UsageHints.DynamicTransform;

			AddToClassList(USS_BLOCK);

			Add(_notificationLabel = new LabelOutlined("MAX") {name = "notification-label"});
			_notificationLabel.AddToClassList(USS_NOTIFICATION);
			_notificationLabel.AddToClassList(USS_NOTIFICATION_HEALTH);

			Add(_healthShield = new PlayerHealthShieldElement {name = "health-shield"});

			_notificationHandle = schedule.Execute(() => { _notificationLabel.AddToClassList(USS_NOTIFICATION_HIDDEN); });
			_notificationHandle.Pause();

			var damageHolder = new VisualElement {name = "damage-holder"};
			Add(damageHolder);
			damageHolder.AddToClassList(USS_DAMAGE_HOLDER);
			damageHolder.usageHints = UsageHints.GroupTransform;
			{
				float maxOffset = 15;
				var divisions = 5;
				for (int i = 0; i < DAMAGE_NUMBER_MAX_POOL_SIZE; i++)
				{
					// Create damage label
					var damageNumber = new LabelOutlined("0") {name = "damage-number"};
					damageHolder.Add(damageNumber);
					damageNumber.AddToClassList(USS_DAMAGE_NUMBER);
					damageNumber.userData = i; // Save index to userData
					var multiplier = (float) i % divisions / (divisions - 1);
					_damageNumberAnimOffsets[i] = -maxOffset + multiplier * maxOffset * 2;
					_damageNumbersPool[i] = damageNumber;
					// Create animation
					var anim = damageNumber.experimental.animation.Start(0f, 1f, DAMAGE_NUMBER_ANIM_DURATION, AnimateDamageNumber);
					anim.KeepAlive().Stop();
					_damageNumberAnims[i] = anim;
				}
			}
		}

		/// <summary>
		/// If the bar is not friendly, display it for some amount of time.
		/// </summary>
		public void PingDamage(uint damage, EventKey eventKey, StyleColor? color = null)
		{
			_damageNumberIndex = (_damageNumberIndex + 1) % DAMAGE_NUMBER_MAX_POOL_SIZE;
			var damageNumberLabel = _damageNumbersPool[_damageNumberIndex];
			var damageNumberAnim = _damageNumberAnims[_damageNumberIndex];
			_damageNumberAnimValues[_damageNumberIndex] = damage;
			_damageEventKeys[_damageNumberIndex] = eventKey;
			damageNumberLabel.style.color = color ?? _defaultPingDmgColor;
			damageNumberLabel.text = damage.ToString();
			damageNumberLabel.BringToFront();
			damageNumberAnim.Stop();
			damageNumberAnim.Start();
		}

		public void OnEventCancelled(EventKey cancelledEvent)
		{
			var index = Array.IndexOf(_damageEventKeys, cancelledEvent);

			if (index < 0)
			{
				return;
			}

			var animation = _damageNumberAnims[index];
			var label = _damageNumbersPool[index];
			label.style.opacity = 0;
			animation.Stop();
		}

		/// <summary>
		/// Enables or disables the health / shield status bars.
		/// </summary>
		public void EnableStatusBars(bool enable)
		{
			_barsEnabled = enable;
			_healthShield.SetDisplay(enable);
		}

		/// <summary>
		/// Sets the max and current shield (i.e. the size of the shield bar).
		/// </summary>
		public void UpdateShield(int previous, int current, int max)
		{
			if (!_barsEnabled) return;
			_healthShield.UpdateShield(previous, current, max);
		}

		/// <summary>
		/// Sets the max and current health (i.e. the size of the health bar).
		/// </summary>
		public void UpdateHealth(int previous, int current, int max)
		{
			_maxHealth = max;
			if (!_barsEnabled) return;
			_healthShield.UpdateHealth(previous, current, max);
		}

		/// <summary>
		/// Shows a notification above the player's head.
		/// </summary>
		public void ShowNotification(NotificationType type, string data = null)
		{
			_notificationLabel.RemoveModifiers();

			switch (type)
			{
				case NotificationType.MaxShields:
					_notificationLabel.text = ScriptLocalization.UITMatch.max + " <sprite name=\"Shieldicon\">";
					_notificationLabel.AddToClassList(USS_NOTIFICATION_SHIELDS);
					break;
				case NotificationType.MaxAmmo:
					_notificationLabel.text = ScriptLocalization.UITMatch.max + " <sprite name=\"Ammoicon\">";
					_notificationLabel.AddToClassList(USS_NOTIFICATION_AMMO);
					break;
				case NotificationType.LevelUp:
					_notificationLabel.text = ScriptLocalization.UITMatch.lvl_up;
					_notificationLabel.AddToClassList(USS_NOTIFICATION_LVLUP);
					break;
				case NotificationType.MaxHealth:
					_notificationLabel.text = ScriptLocalization.UITMatch.max + " <sprite name=\"HPicon\">";
					_notificationLabel.AddToClassList(USS_NOTIFICATION_HEALTH);
					break;
				case NotificationType.MaxSpecials:
					_notificationLabel.text = ScriptLocalization.UITMatch.full;
					_notificationLabel.AddToClassList(USS_NOTIFICATION_MISC);
					break;
				case NotificationType.Wounded:
					_notificationLabel.text = " <sprite name=\"RedCrossIcon\"> ";
					_notificationLabel.AddToClassList(USS_NOTIFICATION_WOUNDED);
					break;
				case NotificationType.MiscPickup:
					_notificationLabel.text = data;
					_notificationLabel.AddToClassList(USS_NOTIFICATION_MISC);
					break;
				case NotificationType.NoInputWarning:
					_notificationLabel.text = data;
					_notificationLabel.AddToClassList(USS_NOTIFICATION_NOINPUTWARNING);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}

			_notificationLabel.SetDisplay(true);

			if (type == NotificationType.NoInputWarning)
			{
				_notificationHandle.ExecuteLater(4000);
				_notificationLabel.AnimatePing(1.4f, 4000);
			}
			else
			{
				_notificationHandle.ExecuteLater(1000);
				_notificationLabel.AnimatePing();
			}
		}

		private void AnimateDamageNumber(VisualElement damageNumber, float t)
		{
			var index = (int) damageNumber.userData;
			var offset = _damageNumberAnimOffsets[index];
			var damage = _damageNumberAnimValues[index];

			// Bezier curve
			var p0 = new Vector2(0, 0) + Vector2.one * offset / 2f; // Less random offset on first point
			var p1 = new Vector2(50, -10) + Vector2.one * offset;
			var p2 = new Vector2(75, 50) + Vector2.one * (offset * 2f); // More random offset on last point

			var pd1 = Vector2.Lerp(p0, p1, t);
			var pd2 = Vector2.Lerp(p1, p2, t);
			var pf = Vector2.Lerp(pd1, pd2, t);

			// Opacity between 0-0.2 of d
			var opacity = Mathf.Clamp01(1f / 0.50f * (1f - t));

			// Scale overshoot (bump)
			var scaleMagnitude = Mathf.Clamp01(Mathf.InverseLerp(20, 100, damage)) * 1.1f;
			var scale = t < 0.1 ? Mathf.Lerp(0, 1.5f + scaleMagnitude, t * 10) :
				t < 0.3 ? Mathf.Lerp(1.5f + scaleMagnitude, 1f, (t - 0.1f) * 10) :
				1f;

			// Color
			// var color = t < 0.3 ? Color.Lerp(Color.red, Color.white, t * 3.33f) : Color.white;

			damageNumber.transform.position = pf;
			damageNumber.transform.scale = new Vector3(scale, scale, 1);
			damageNumber.style.opacity = opacity;
			// damageNumber.style.color = color;
		}

		public enum NotificationType
		{
			MaxShields,
			MaxHealth,
			MaxAmmo,
			LevelUp,
			MaxSpecials,
			MiscPickup,
			Wounded,
			NoInputWarning
		}

		public new class UxmlFactory : UxmlFactory<PlayerStatusBarElement, UxmlTraits>
		{
		}
	}
}