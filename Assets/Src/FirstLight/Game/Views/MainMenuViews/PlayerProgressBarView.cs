using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Coffee.UIEffects;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class handles the Player's Meta XP Level Up Bar in the top left hand corner of the Main Menu.
	/// It will rack up and change level as the player gains Meta XP.
	///
	/// Also used to show player progress along the Trophy Road.
	/// </summary>
	public class PlayerProgressBarView : MonoBehaviour
	{
		[SerializeField, Required] private Image _iconImage;
		[SerializeField, Required] private TextMeshProUGUI _levelText;
		[SerializeField, Required] private VisualStateButtonView _stateButtonView;
		[SerializeField, Required] private Transform _xpAnimationTarget;
		[SerializeField, Required] private Slider _xpSlider;
		[SerializeField, Required] private Image _xpSliderFillImage; 
		[SerializeField, Required] private UIShiny _shiny;
		[SerializeField] private float _sliderAnimationTime = 1.5f;
		[SerializeField, Required] private Image _currentRewardImage;
		[SerializeField, Required] private Animation _claimRewardAnimation;
		[SerializeField, Required] private AnimationClip _claimAppearClip;
		[SerializeField, Required] private AnimationClip _claimRewardLoopClip;
		[SerializeField, Required] private GameObject _claimRewardObject;
		
		/// <summary>
		/// Triggered when the Xp slider animation is completed when the player levels up
		/// </summary>
		public UnityEvent<uint, uint> OnLevelUpXpSliderCompleted = new UnityEvent<uint, uint>();
		
		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private IMainMenuServices _mainMenuServices;
		private KeyValuePair<uint, uint>? _levelChange;
		private KeyValuePair<uint, uint>? _xpChange;
		private Coroutine _progressBarCoroutine;
		
		/// <summary>
		/// Requests the current level being shown on the player's progress bar
		/// </summary>
		public uint Level { get; private set; }

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_xpSliderFillImage.color = Color.yellow;

			_dataProvider.PlayerDataProvider.Level.Observe(OnLevelUpUpdated);
			_dataProvider.PlayerDataProvider.Xp.Observe(OnXpUpdated);
			_services.MessageBrokerService.Subscribe<PlayUiVfxCommandMessage>(OnPlayUiVfxCommandMessage);
		}

		private void OnDestroy()
		{
			_dataProvider?.PlayerDataProvider?.Level?.StopObserving(OnLevelUpUpdated);
			_dataProvider?.PlayerDataProvider?.Xp?.StopObserving(OnXpUpdated);
			_services?.MessageBrokerService?.UnsubscribeAll(this);

			if (_progressBarCoroutine != null)
			{
				_services?.CoroutineService?.StopCoroutine(_progressBarCoroutine);
			}
		}

		private void OnDisable()
		{
			_levelChange = null;
			_xpChange = null;
			Level = _dataProvider?.PlayerDataProvider?.Level?.Value ?? 1;
		}

		/// <summary>
		/// Updates the bar and level view of the player's trophy road progression
		/// </summary>
		public async void UpdateProgressView()
		{
			_claimRewardObject.SetActive(false);
			
			var unclaimedRewards = GetUnclaimedReward(Level - 1);
			var rewardId = unclaimedRewards?.RewardId ?? _dataProvider.TrophyRoadDataProvider.CurrentLevelInfo.Reward.RewardId;
			var level = _levelChange?.Key ?? _dataProvider.PlayerDataProvider.Level.Value;
			var info = _dataProvider.PlayerDataProvider.GetInfo(level);
			var xp = _xpChange?.Key ?? info.Xp;
			
			_xpSlider.value = unclaimedRewards.HasValue ? 1f : (float) xp / info.Config.LevelUpXP;
			_currentRewardImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(GetRewardSpriteId(rewardId));
			
			UpdateVisualState(unclaimedRewards.HasValue);
			SetLevel(Level);
		}

		private async void OnPlayUiVfxCommandMessage(PlayUiVfxCommandMessage message)
		{
			if (message.Id != GameId.XP)
			{
				return;
			}

			// Small hack to give time for the canvas to rebuild and make the XP move to the right position
			await Task.Delay(100);
			
			_mainMenuServices.UiVfxService.PlayVfx(message.Id, message.OriginWorldPosition,
			                                       _xpAnimationTarget.position, StartTweenXpSlider);
		}

		private void OnLevelUpUpdated(uint previousLevel, uint newLevel)
		{
			_levelChange = new KeyValuePair<uint, uint>(previousLevel, newLevel);
		}

		private void OnXpUpdated(uint previousXp, uint newXp)
		{
			_xpChange = new KeyValuePair<uint, uint>(previousXp, newXp);
		}

		private async void SetLevel(uint level)
		{
			Level = level;
			_levelText.text = level.ToString();
			_iconImage.sprite = await _services.AssetResolverService.RequestAsset<int, Sprite>((int) level);
		}

		private void StartTweenXpSlider()
		{
			if (_progressBarCoroutine != null)
			{
				return;
			}
			
			var unclaimedRewards = GetUnclaimedReward(Level - 1);
			
			if (unclaimedRewards.HasValue)
			{
				if (_levelChange.HasValue)
				{
					OnLevelUpXpSliderCompleted.Invoke(_levelChange.Value.Key, _levelChange.Value.Value);
				}
				return;
			}
			
			var info = _dataProvider.PlayerDataProvider.CurrentLevelInfo;
			var targetValue = _levelChange.HasValue ? 1f : (float) info.Xp / info.Config.LevelUpXP;
			
			_progressBarCoroutine = _services.CoroutineService.StartCoroutine(XpSliderCoroutine(targetValue));
		}

		private void PlayClaimRewardLoop()
		{
			_claimRewardAnimation.clip = _claimRewardLoopClip;
			_claimRewardAnimation.Play();
		}
		

		private IEnumerator XpSliderCoroutine(float targetValue)
		{
			var startValue = _xpSlider.value;
			var startTime = Time.time;
			var endTime = startTime + _sliderAnimationTime;
			
			while (Time.time < endTime)
			{
				yield return null;
				
				_xpSlider.value = Mathf.Lerp(startValue, targetValue, (Time.time - startTime) / _sliderAnimationTime);
			}

			_progressBarCoroutine = null;
			
			if (!_levelChange.HasValue)
			{
				yield break;
			}

			var previousLevel = _levelChange.Value.Key;
			var newLevel = _levelChange.Value.Value;
			
			SetLevel(newLevel);
			UpdateVisualState(GetUnclaimedReward(newLevel).HasValue);
			OnLevelUpXpSliderCompleted.Invoke(previousLevel, newLevel);
		}
		
		private RewardData? GetUnclaimedReward(uint level)
		{
			var infos = _dataProvider.TrophyRoadDataProvider.GetAllInfos(level);

			foreach (var trophyRoadRewardInfo in infos)
			{
				if (trophyRoadRewardInfo.IsReadyToCollect)
				{
					return trophyRoadRewardInfo.Reward;
				}
			}

			return null;
		}
		
		private GameId GetRewardSpriteId(GameId rewardId)
		{
			return GameId.ScBundle1;
		}

		private void UpdateVisualState(bool hasUnclaimedRewards)
		{
			_stateButtonView.UpdateState(true, false, hasUnclaimedRewards);
			_stateButtonView.UpdateShinyState();
			
			if (hasUnclaimedRewards)
			{
				_stateButtonView.PlayUnlockedStateAnimation();

				_shiny.enabled = true;
				_shiny.Play();
				_xpSliderFillImage.color = Color.green;

				_claimRewardObject.SetActive(true);
				_claimRewardAnimation.clip = _claimAppearClip;
				_claimRewardAnimation.Stop();
				_claimRewardAnimation.Play();
					
				this.LateCall(_claimRewardAnimation.clip.length, PlayClaimRewardLoop);
			}
			else
			{
				_shiny.enabled = false;
				_shiny.Stop();
				_xpSliderFillImage.color = Color.yellow;

				_claimRewardObject.SetActive(false);
			}
		}
	}
}
