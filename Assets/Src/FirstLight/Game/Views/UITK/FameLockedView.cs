using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles locked elements.
	/// TODO: This should really be a custom visual element that wraps the original button.
	/// </summary>
	public class FameLockedView : UIView
	{
		private const string USS_FAME_LOCK = "fame-lock";
		private const string USS_FAME_LOCK_HOLDER = USS_FAME_LOCK + "__holder";
		private const string USS_FAME_LOCK_ICON = USS_FAME_LOCK + "__icon";

		private IGameDataProvider _dataProvider;

		private VisualElement _root;
		private VisualElement _lockHolder;
		private Label _lockLabel;

		private Action _unlockedAction;
		private uint _requiredLevel;
		private uint _currentLevel;
		private bool _locked;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();

			if (Element is ImageButton ib)
			{
				ib.clicked += OnClick;
			}
			else if (Element is Button b)
			{
				b.clicked += OnClick;
			}
			else
			{
				throw new NotSupportedException($"Adding lock to unsupported element type: {Element.GetType()}.");
			}
		}

		public override void SubscribeToEvents()
		{
			_dataProvider.PlayerDataProvider.Level.InvokeObserve(OnFameUpdated);
		}

		private void OnClick()
		{
			if (_locked)
			{
				// Tooltip
				Element.OpenTooltip(_root, $"You need to reach level <color=#f8c72e>{_requiredLevel}</color> to unlock this.",
					position: TooltipPosition.BottomRight, direction: TooltipDirection.TopLeft);
			}
			else
			{
				_unlockedAction();
			}
		}

		public override void UnsubscribeFromEvents()
		{
			_dataProvider.PlayerDataProvider.Level.StopObserving(OnFameUpdated);
		}

		public void Init(UnlockSystem unlockSystem, VisualElement root, Action unlockedCallback)
		{
			_requiredLevel = _dataProvider.PlayerDataProvider.GetUnlockSystemLevel(unlockSystem);
			_unlockedAction = unlockedCallback;
			_root = root;
			UpdateLock(false);
		}

		private void OnFameUpdated(uint _, uint level)
		{
			_currentLevel = level;
			UpdateLock(true);
		}

		private void UpdateLock(bool animate)
		{
			var locked = _currentLevel < _requiredLevel;
			EnableLock(locked, animate);
		}

		private void EnableLock(bool enable, bool animate)
		{
			if (enable == _locked) return;
			_locked = enable;

			if (enable)
			{
				Element.AddToClassList(USS_FAME_LOCK);
				Element.Add(_lockHolder = new VisualElement {name = "lock-holder"});
				_lockHolder.AddToClassList(USS_FAME_LOCK_HOLDER);
				{
					var icon = new VisualElement();
					_lockHolder.Add(icon);
					icon.AddToClassList(USS_FAME_LOCK_ICON);
				}
			}
			else
			{
				Element.RemoveFromClassList(USS_FAME_LOCK);
				//_lockHolder.RemoveFromHierarchy();
				_lockHolder = null;
			}
		}
	}
}