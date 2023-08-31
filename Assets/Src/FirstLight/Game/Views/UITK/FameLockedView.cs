using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class FameLockedView : UIView
	{
		private const string USS_FAME_LOCK = "fame-lock";
		private const string USS_FAME_LOCK_HOLDER = USS_FAME_LOCK + "__holder";
		private const string USS_FAME_LOCK_LABEL = USS_FAME_LOCK + "__label";
		private const string USS_FAME_LOCK_ICON = USS_FAME_LOCK + "__icon";

		private const string LABEL_FORMAT = "LVL {0}";

		private IGameDataProvider _dataProvider;

		private VisualElement _lockHolder;
		private Label _lockLabel;

		private uint _requiredLevel;
		private uint _currentLevel;

		public override void Attached(VisualElement element)
		{
			base.Attached(element);

			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		public override void SubscribeToEvents()
		{
			_dataProvider.PlayerDataProvider.Level.InvokeObserve(OnFameUpdated);
		}

		public override void UnsubscribeFromEvents()
		{
			_dataProvider.PlayerDataProvider.Level.StopObserving(OnFameUpdated);
		}

		public void Init(UnlockSystem unlockSystem)
		{
			_requiredLevel = _dataProvider.PlayerDataProvider.GetUnlockSystemLevel(unlockSystem);
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
			if (enable == (_lockHolder != null)) return;

			Element.SetEnabled(!enable);

			if (enable)
			{
				Element.AddToClassList(USS_FAME_LOCK);
				Element.Add(_lockHolder = new VisualElement {name = "lock-holder"});
				_lockHolder.AddToClassList(USS_FAME_LOCK_HOLDER);
				{
					var icon = new VisualElement();
					_lockHolder.Add(icon);
					icon.AddToClassList(USS_FAME_LOCK_ICON);

					_lockHolder.Add(_lockLabel = new Label(string.Format(LABEL_FORMAT, _requiredLevel)));
					_lockLabel.AddToClassList(USS_FAME_LOCK_LABEL);
				}
			}
			else if (_lockHolder != null)
			{
				Element.RemoveFromClassList(USS_FAME_LOCK);
				//_lockHolder.RemoveFromHierarchy();
				_lockHolder = null;
			}
		}
	}
}