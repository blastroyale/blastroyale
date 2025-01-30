using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Display a list of rewards, currently used inside the Battlepass Screen, but can be used anywhere. 
	/// </summary>
	public class RewardsScreenPresenter : UIPresenterData<RewardsScreenPresenter.StateData>
	{
		private const int MIN_ITEMS_SHOW_SKIP_ALL = 50;

		public class StateData
		{
			public bool FameRewards;
			public IEnumerable<ItemData> Items;
			public ItemData ParentItem;
			public Action OnFinish;
			public bool SkipSummary;
		}

		#region Dependencies

		[SerializeField, Required] private AnimatedBackground _animatedBackground;
		[SerializeField, Required] private AnimatedBackground.AnimatedBackgroundColor _genericRewardBgColor;
		[SerializeField, Required] private AnimatedBackground.AnimatedBackgroundColor _summaryBgColor;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _genericRewardDirector;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _summaryDirector;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _fameSummaryDirector;

		#endregion

		#region Elements

		private GenericRewardView _genericRewardView;
		private RewardsSummaryView _summaryView;

		private Button _skipAllButton;
		private Label _remainingNumber;
		private VisualElement _blocker;
		private VisualElement _remainingRoot;

		#endregion

		#region UIState

		private bool _summaryShowedOrIgnored;
		private bool _canSkipSummary;
		private bool _finished;
		private Queue<ItemData> _remaining;
		private RewardsAnimationController _animations;

		#endregion

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		protected override void QueryElements()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.ResolveServices();

			_animations = new RewardsAnimationController();
			_remainingRoot = Root.Q<VisualElement>("RewardsRemaining").Required();
			_blocker = Root.Q<VisualElement>("Blocker").Required();
			_skipAllButton = Root.Q<Button>("SkipAllButton").Required();
			_skipAllButton.clicked += OnSkipAllClicked;
			_remainingNumber = _remainingRoot.Q<Label>("NumberLabel").Required();

			_blocker.RegisterCallback<ClickEvent>(OnClick);

			Root.Q<VisualElement>("OneReward").Required().AttachView(this, out _genericRewardView);
			_genericRewardView.Init(_animations, _animatedBackground, _genericRewardDirector, _genericRewardBgColor);

			Root.Q<VisualElement>("RewardsSummary").Required().AttachView(this, out _summaryView);
			_summaryView.Init(_animations, _animatedBackground, Data.FameRewards ? _fameSummaryDirector : _summaryDirector, Data.FameRewards, _summaryBgColor);

			_summaryView.CreateSummaryElements(Data.Items, Data.FameRewards);
		}

		private void OnSkipAllClicked()
		{
			_remaining.Clear();
			Move();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			if (Data.FameRewards)
			{
				_remaining = new Queue<ItemData>();
				ShowSummary();
				_summaryShowedOrIgnored = true;
			}
			else
			{
				_remaining = new Queue<ItemData>(Data.Items);
				Move();
			}

			return base.OnScreenOpen(reload);
		}

		public void OnClick(ClickEvent evt)
		{
			Move();
			evt.StopImmediatePropagation();
		}

		private void Move()
		{
			if (_finished) return;
			if (_animations.Skip()) return;
			if (ShowNext())
			{
				_remainingNumber.text = _remaining.Count.ToString();
				return;
			}

			if (!_summaryShowedOrIgnored)
			{
				_summaryShowedOrIgnored = true;
				if (ShouldShowSummary())
				{
					ShowSummary();
					return;
				}
			}

			_finished = true;
			if (_services.RewardService.TryGetUnseenCore(out var unseen))
			{
				_services.UIService.OpenScreen<RewardsScreenPresenter>(new StateData()
				{
					Items = unseen.Results,
					FameRewards = false,
					ParentItem = unseen.Core,
					OnFinish = Data.OnFinish
				}).Forget();
			}

			FLog.Info("Data on finish invoked!");
			Data.OnFinish?.Invoke();
		}

		private bool ShowNext()
		{
			if (!_remaining.TryDequeue(out var current))
			{
				return false;
			}

			SetCurrentReward(current);
			return true;
		}

		private void SetCurrentReward(ItemData item)
		{
			var itemViewModelData = item.GetViewModel();

			_genericRewardView.ShowReward(itemViewModelData);
			SetDisplays(_genericRewardView);
		}

		private bool ShouldShowSummary()
		{
			if (Data.SkipSummary) return false;
			return Data.Items.Count() > 1;
		}

		private void ShowSummary()
		{
			_canSkipSummary = Data.FameRewards;
			_summaryView.Show();
			SetDisplays(_summaryView);
		}

		private void SetDisplays(UIView view)
		{
			_genericRewardView.Element.SetDisplay(view == _genericRewardView);
			_summaryView.Element.SetDisplay(view == _summaryView);
			_remainingRoot.SetDisplay(_remaining.Count > 0);
			_skipAllButton.SetDisplay(view != _summaryView && !Data.FameRewards && Data.Items.Count() >= MIN_ITEMS_SHOW_SKIP_ALL);
		}

		public void OnLevelUpSignal()
		{
			_summaryView.SetPlayerLevel(_gameDataProvider.PlayerDataProvider.Level.Value);
		}

		public void OnChangeRewardsSignal()
		{
			var nextLevel = _gameDataProvider.PlayerDataProvider.Level.Value;
			var nextLevelRewards = _gameDataProvider.PlayerDataProvider.GetRewardsForFameLevel(nextLevel);
			_summaryView.CreateSummaryElements(nextLevelRewards, true);
		}
	}
}