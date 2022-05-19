using System;
using System.Collections.Generic;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Views.AdventureHudViews;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Rewards Screen, where players are awarded loot.
	/// Players can skip through animations if they are impatient.
	/// </summary>
	public class RewardsScreenPresenter : AnimatedUiPresenterData<RewardsScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action MainMenuClicked;
		}
		
		[SerializeField, Required] private Button _gotoMainMenuButton;
		[SerializeField, Required] private Button _rewindButton;
		[SerializeField, Required] private Button _screenButton;
		[SerializeField, Required] private GameObject _yourLootObject;
		[SerializeField, Required] private Transform _gridLayout;
		[SerializeField, Required] private RewardView _rewardRef;
		[SerializeField, Required] private TextMeshProUGUI _yourLootText;
		
		private RewardView [] _rewardViews;
		private IGameDataProvider _gameDataProvider;
		private Dictionary<GameId, int> _rewards;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_gotoMainMenuButton.onClick.AddListener(OnGotoMainMenuClicked);
			_rewindButton.onClick.AddListener(RewindButtonClicked);
			_screenButton.onClick.AddListener(SkipCurrentAnimation);
			_gotoMainMenuButton.gameObject.SetActive(false);
			_rewardRef.gameObject.SetActive(false);
			_yourLootObject.gameObject.SetActive(false);
			_rewindButton.gameObject.SetActive(false);
		}

		protected override void OnOpened()
		{
			_rewards = ProcessRewards();
			var i = 0;
			
			_rewardViews = new RewardView[_rewards.Count];

			foreach (var reward in _rewards)
			{
				// Only play unpack animation if its not the last reward in list
				bool playUnpackAnim = i != _rewards.Count - 1;
				_rewardViews[i] = Instantiate(_rewardRef, _rewardRef.transform.parent);
				_rewardViews[i].Initialise(reward.Key, (uint) reward.Value, playUnpackAnim);
				_rewardViews[i].gameObject.SetActive(false);

				if (i > 0)
				{
					_rewardViews[i - 1].OnRewardAnimationComplete = _rewardViews[i].StartRewardSequence;
					_rewardViews[i - 1].OnSummaryAnimationComplete = _rewardViews[i].StartSummarySequence;
				}

				i++;
			}

			if (_rewardViews.Length > 0)
			{
				_rewardViews[_rewardViews.Length - 1].OnRewardAnimationComplete = PlaySummariseSequence;
				_rewardViews[_rewardViews.Length - 1].OnSummaryAnimationComplete = OnSummariseSequenceCompleted;
				_yourLootText.text = ScriptLocalization.AdventureMenu.YourLoot;
			}
			else
			{
				_yourLootText.text = ScriptLocalization.AdventureMenu.NoRewardsCollected;
			}

			base.OnOpened();
		}

		protected override void OnOpenedCompleted()
		{
			_yourLootObject.gameObject.SetActive(true);
			
			if (_rewardViews.Length > 0)
			{
				PlayRewardSequence();
			}
			else
			{
				OnSummariseSequenceCompleted();
			}
		}

		private void PlayRewardSequence()
		{
			_rewardViews[0].StartRewardSequence();
		}

		private void PlaySummariseSequence()
		{
			foreach (var reward in _rewardViews)
			{
				reward.transform.SetParent(_gridLayout);
				reward.gameObject.SetActive(false);
				reward.transform.localPosition = Vector3.zero;
			}

			_rewardViews[0].StartSummarySequence();
		}

		private void OnSummariseSequenceCompleted()
		{
			_gotoMainMenuButton.gameObject.SetActive(true);
			_rewindButton.gameObject.SetActive(false);
		}

		private void RewindButtonClicked()
		{
			var rectTransform = _rewardRef.GetComponent<RectTransform>();
			
			foreach (var reward in _rewardViews)
			{
				reward.Rewind(rectTransform);
			}
			
			_yourLootObject.gameObject.SetActive(false);
			_gotoMainMenuButton.gameObject.SetActive(false);
			_rewindButton.gameObject.SetActive(false);
			PlayRewardSequence();
		}

		private void SkipCurrentAnimation()
		{
			foreach (var reward in _rewardViews)
			{
				if (reward.IsPlaying)
				{
					reward.EndAnimationEarly();
					return;
				}
			}
		}

		private Dictionary<GameId, int> ProcessRewards()
		{
			var dictionary = new Dictionary<GameId, int>();
			var rewards = _gameDataProvider.RewardDataProvider.UnclaimedRewards;

			for (var i = 0; i < rewards.Count; i++)
			{
				var id = rewards[i].RewardId;
				
				if (!dictionary.ContainsKey(id))
				{
					dictionary.Add(id, 0);
				}

				dictionary[id] += rewards[i].Value;
			}

			return dictionary;
		}
		
		private void OnGotoMainMenuClicked()
		{
			Data.MainMenuClicked.Invoke();
		}
	}
}