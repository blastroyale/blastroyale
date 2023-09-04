using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Display a list of rewards, currently used inside the Battlepass Screen, but can be used anywhere. 
	/// </summary>
	public class RewardsScreenPresenter : UiToolkitPresenterData<RewardsScreenPresenter.StateData>
	{
		public struct StateData
		{
			public bool FameRewards;
			public List<IReward> Rewards;
			public Action OnFinish;
		}

		#region Dependencies

		[SerializeField, Required] private AnimatedBackground _animatedBackground;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _genericRewardDirector;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _equipmentRewardDirector;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _summaryDirector;

		[SerializeField, Required, TabGroup("Animation")]
		private PlayableDirector _fameSummaryDirector;

		#endregion

		#region Elements

		private EquipmentRewardView _equipmentRewardView;
		private GenericRewardView _genericRewardView;
		private RewardsSummaryView _summaryView;

		private Label _remainingNumber;
		private VisualElement _remainingRoot;

		#endregion

		#region UIState

		private bool _summaryShowedOrSkipped;
		private bool _finished;
		private Queue<IReward> _remaining;
		private RewardsAnimationController _animations;

		#endregion

		private IGameDataProvider _gameDataProvider;

		protected override void QueryElements(VisualElement root)
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_animations = new RewardsAnimationController();
			_remainingRoot = root.Q<VisualElement>("RewardsRemaining").Required();
			_remainingNumber = _remainingRoot.Q<Label>("NumberLabel").Required();

			root.RegisterCallback<ClickEvent>(OnClick);

			root.Q<VisualElement>("EquipmentReward").Required().AttachView(this, out _equipmentRewardView);
			_equipmentRewardView.Init(_animations, _animatedBackground, _equipmentRewardDirector);

			root.Q<VisualElement>("OneReward").Required().AttachView(this, out _genericRewardView);
			_genericRewardView.Init(_animations, _animatedBackground, _genericRewardDirector);

			root.Q<VisualElement>("RewardsSummary").Required().AttachView(this, out _summaryView);
			_summaryView.Init(_animations, _animatedBackground, Data.FameRewards ? _fameSummaryDirector : _summaryDirector);

			_summaryView.CreateSummaryElements(Data.Rewards, Data.FameRewards);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			if (Data.FameRewards)
			{
				_remaining = new Queue<IReward>();
				ShowSummary();
				_summaryShowedOrSkipped = true;
			}
			else
			{
				_remaining = new Queue<IReward>(Data.Rewards);
				Move();
			}
		}

		public void OnClick(ClickEvent evt)
		{
			evt.StopImmediatePropagation();
			Move();
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

			if (!_summaryShowedOrSkipped)
			{
				_summaryShowedOrSkipped = true;
				if (ShouldShowSummary())
				{
					ShowSummary();
					return;
				}
			}

			_finished = true;
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

		private void SetCurrentReward(IReward rewardData)
		{
			if (rewardData is EquipmentReward eq)
			{
				_equipmentRewardView.ShowEquipment(eq);
				SetDisplays(_equipmentRewardView);
			}
			else
			{
				_genericRewardView.ShowReward(rewardData);
				SetDisplays(_genericRewardView);
			}
		}

		private bool ShouldShowSummary()
		{
			return Data.Rewards.Count > 1;
		}

		private void ShowSummary()
		{
			_summaryView.Show();
			SetDisplays(_summaryView);
		}

		private void SetDisplays(UIView view)
		{
			_genericRewardView.Element.SetDisplay(view == _genericRewardView);
			_summaryView.Element.SetDisplay(view == _summaryView);
			_equipmentRewardView.Element.SetDisplay(view == _equipmentRewardView);
			_remainingRoot.SetDisplay(_remaining.Count > 0);
		}

		public void OnLevelUpSignal()
		{
			_summaryView.SetPlayerLevel(_gameDataProvider.PlayerDataProvider.Level.Value);
		}

		public void OnChangeRewardsSignal()
		{
			var nextLevel = _gameDataProvider.PlayerDataProvider.Level.Value + 1;
			var nextLevelRewards = _gameDataProvider.PlayerDataProvider.GetRewardsForLevel(nextLevel);
			_summaryView.CreateSummaryElements(nextLevelRewards, true);
		}
	}
}