using FirstLight.FLogger;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the status bar above a player's head.
	/// </summary>
	public class PlayerBarElement : VisualElement
	{
		private const int SHIELD_DIVIDER_AMOUNT = 100;
		private const int HEALTH_DIVIDER_AMOUNT = 100;

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
		private readonly VisualElement _shieldHolder;
		private readonly VisualElement _shieldBar;
		private readonly VisualElement _healthHolder;
		private readonly VisualElement _healthBar;
		private readonly VisualElement _ammoHolder;

		private bool _isFriendly;

		public PlayerBarElement()
		{
			usageHints = UsageHints.DynamicTransform;

			AddToClassList(USS_BLOCK);

			Add(_name = new Label("PLAYER NAME") {name = "name"});
			_name.AddToClassList(USS_NAME);

			var background = new VisualElement {name = "background"};
			Add(background);
			background.AddToClassList(USS_BACKGROUND);

			Add(_shieldHolder = new VisualElement {name = "shield-holder"});
			_shieldHolder.AddToClassList(USS_SHIELD_HOLDER);
			{
				_shieldHolder.Add(_shieldBar = new VisualElement {name = "shield-bar"});
				_shieldBar.AddToClassList(USS_SHIELD_BAR);
			}

			Add(_healthHolder = new VisualElement {name = "health-holder"});
			_healthHolder.AddToClassList(USS_HEALTH_HOLDER);
			{
				_healthHolder.Add(_healthBar = new VisualElement {name = "health-bar"});
				_healthBar.AddToClassList(USS_HEALTH_BAR);
			}

			Add(_ammoHolder = new VisualElement {name = "ammo-holder"});
			_ammoHolder.AddToClassList(USS_AMMO_HOLDER);

			Add(_level = new Label("10") {name = "level"});
			_level.AddToClassList(USS_LEVEL);

			SetIsFriendly(true);
			SetMagazine(4, 6);
		}

		public void SetIsFriendly(bool isFriendly)
		{
			_isFriendly = isFriendly;
			_ammoHolder.SetDisplay(_isFriendly);
		}

		public void SetName(string playerName)
		{
			_name.text = playerName;
		}

		public void SetMagazine(int currentMagazine, int maxMagazine)
		{
			if (!_isFriendly) return;

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

			// curr = 3
			int index = 0;
			foreach (var segment in _ammoHolder.Children())
			{
				segment.SetVisibility(index++ < currentMagazine);
			}
		}

		public void SetShield(int current, int max)
		{
			_shieldBar.style.flexGrow = (float) current / max;
		}

		public void SetLevel(int level)
		{
			_level.text = level.ToString();
		}

		public void SetHealth(int previous, int current, int max)
		{
			_healthBar.style.flexGrow = (float) current / max;
		}

		public new class UxmlFactory : UxmlFactory<PlayerBarElement, UxmlTraits>
		{
		}
	}
}