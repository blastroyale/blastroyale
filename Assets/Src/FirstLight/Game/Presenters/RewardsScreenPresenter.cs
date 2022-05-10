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
		
		private RewardView [] _rewards;
		private IGameDataProvider _gameDataProvider;

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
			var rewards = ProcessRewards();
			var i = 0;
			
			_rewards = new RewardView[rewards.Count];

			foreach (var reward in rewards)
			{
				_rewards[i] = Instantiate(_rewardRef, _rewardRef.transform.parent);
				_rewards[i].Initialise(reward.Key, (uint) reward.Value);
				_rewards[i].gameObject.SetActive(false);

				if (i > 0)
				{
					_rewards[i - 1].OnRewardAnimationComplete = _rewards[i].StartRewardSequence;
					_rewards[i - 1].OnSummaryAnimationComplete = _rewards[i].StartSummarySequence;
				}

				i++;
			}

			if (_rewards.Length > 0)
			{
				_rewards[_rewards.Length - 1].OnRewardAnimationComplete = PlaySummariseSequence;
				_rewards[_rewards.Length - 1].OnSummaryAnimationComplete = OnSummariseSequenceCompleted;
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
			
			if (_rewards.Length > 0)
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
			_rewards[0].StartRewardSequence();
		}

		private void PlaySummariseSequence()
		{
			foreach (var reward in _rewards)
			{
				reward.transform.SetParent(_gridLayout);
				reward.gameObject.SetActive(false);
				reward.transform.localPosition = Vector3.zero;
			}

			_rewards[0].StartSummarySequence();
		}

		private void OnSummariseSequenceCompleted()
		{
			_gotoMainMenuButton.gameObject.SetActive(true);
			_rewindButton.gameObject.SetActive(false);
		}

		private void RewindButtonClicked()
		{
			var rectTransform = _rewardRef.GetComponent<RectTransform>();
			
			foreach (var reward in _rewards)
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
			foreach (var reward in _rewards)
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

				dictionary[id] += id.IsInGroup(GameIdGroup.LootBox) ? 1 : rewards[i].Value;
			}

			return dictionary;
		}
		
		private void OnGotoMainMenuClicked()
		{
			Data.MainMenuClicked.Invoke();
		}
	}
}