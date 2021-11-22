using System;
using System.Threading.Tasks;
using Coffee.UIEffects;
using FirstLight.Game.Commands;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Services;
using I2.Loc;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Quantum;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script controls rewards on the Trophy Road screen.
	/// </summary>
	public class ProgressMenuGridItemView : GridItemBase<ProgressMenuGridItemView.ProgressMenuGridItemData>
	{
		public struct ProgressMenuGridItemData
		{
			public AdventureInfo Info;
			public bool PlayIntroAnimation;
			public bool PlayAdventureCompletedAnimation;
		}
		
		[SerializeField] private Button _button;
		[SerializeField] private TextMeshProUGUI _buttonText;
		[SerializeField] private TextMeshProUGUI _reccomendedPowerText;
		[SerializeField] private TextMeshProUGUI _mapNameText;
		[SerializeField] private TextMeshProUGUI _playersActiveText;
		[SerializeField] private TextMeshProUGUI _bossNameText;
		[SerializeField] private GameObject _newItemsHolder;
		[SerializeField] private Image _mapImage;
		[SerializeField] private Image _bossImage;
		[SerializeField] private Animation _animations;
		[SerializeField] private AnimationClip _appearAnimationClip;
		[SerializeField] private AnimationClip _missionCompleteAnimationClip;
		[SerializeField] private MainMenuRewardView _smallCardRef;

		[SerializeField] private TextMeshProUGUI _difficultyText;
		[SerializeField] private TextMeshProUGUI _bossLevelNumberText;
		[SerializeField] private TextMeshProUGUI _levelNumberText;
		[SerializeField] private TextMeshProUGUI _chapterNumberText;
		[SerializeField] private TextMeshProUGUI _defeatedText;
		[SerializeField] private TextMeshProUGUI _rewardsTitleText;
		[SerializeField] private UIGradient _hardnessShaderGradient;
		[SerializeField] private Color[] _hardnessLeftGradient;
		[SerializeField] private Color[] _hardnessRightGradient;

		[SerializeField] private Image _greyscaleImage;
		[SerializeField] private Image _missionCompletedPlayImage;
		[SerializeField] private Image _missionAvailablePlayImage;
		[SerializeField] private Image _missionUnavailablePlayImage;
		[SerializeField] private Image _missionCompleteImage;

		[SerializeField] private GameObject _firstVictoryRewardStrikethrough;
		[SerializeField] private GameObject _lockIcon;
		[SerializeField] private GameObject _undefeatedHolder;
		[SerializeField] private GameObject _defeatedHolder;
		[SerializeField] private GameObject _chapterHolder;
		[SerializeField] private UIShiny _buttonShiny;
		
		private IObjectPool<MainMenuRewardView> _smallCardPool;
		private ProgressMenuGridItemData _data;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private IMainMenuServices _mainMenuServices;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_smallCardPool = new GameObjectPool<MainMenuRewardView>(3, _smallCardRef);

			_smallCardRef.gameObject.SetActive(false);
			_button.onClick.AddListener(OnButtonClick);
		}

		protected override void OnUpdateItem(ProgressMenuGridItemData data)
		{
			var replaceText = ScriptTerms.Chapters.Chapter1.Replace("1", data.Info.Config.Chapter.ToString());
			
			_data = data;
			_mapNameText.text = data.Info.Config.Map.GetTranslation();
			_levelNumberText.text = _data.Info.Config.Stage.ToString();
			_bossLevelNumberText.text = string.Format(ScriptLocalization.MainMenu.BossLevel, data.Info.Config.BossDifficulty.ToString());
			_chapterNumberText.text = string.Format(ScriptLocalization.MainMenu.ProgressChapter,  data.Info.Config.Chapter.ToString(), 
			                                        LocalizationManager.GetTranslation(replaceText));

			_bossImage.color = !_data.Info.IsUnlocked ? Color.black : Color.white;

			if (_data.Info.IsCompleted)
			{
				var colorString = "<color=\"red\">";

				_defeatedText.text = string.Format(ScriptLocalization.MainMenu.DefeatedBossXTimes, colorString,
					"<color=\"white\">", _data.Info.AdventureData.KillCount.ToString());
			}
			
			// TODO: Use API to fetch players, update once every 60 seconds via coroutine.
			_playersActiveText.text = string.Format(ScriptLocalization.MainMenu.PlayersActiveNumber, 50);
			_playersActiveText.enabled = false;

			_greyscaleImage.gameObject.SetActive(!data.Info.IsUnlocked);
			
			SetDifficultyDataView();

			SetRewardsDataView();

			_chapterHolder.SetActive(_data.Info.Config.Stage == 1);
			SetStateDataView();
			LoadSprites(data.Info);
			PlayAnimations();
		}

		private async void LoadSprites(AdventureInfo info)
		{
			_mapImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(info.Config.Map, false);
		}
		
		private void PlayAnimations()
		{
			_animations.clip = _appearAnimationClip;
			_animations.Play();

			if (_data.PlayAdventureCompletedAnimation)
			{
				var completedTagged = _gameDataProvider.AdventureDataProvider.AdventuresCompletedTagged;
				
				_data.PlayAdventureCompletedAnimation = false;

				if (!completedTagged.Contains(_data.Info.AdventureData.Id))
				{
					completedTagged.Add(_data.Info.AdventureData.Id);
				}

				_animations.clip = _missionCompleteAnimationClip;
				_animations.PlayQueued(_missionCompleteAnimationClip.name);
			}
		}

		private void SetStateDataView()
		{
			_missionCompleteImage.gameObject.SetActive(_data.Info.IsCompleted && !_data.PlayAdventureCompletedAnimation);
			_missionCompletedPlayImage.enabled = _data.Info.IsCompleted && _data.Info.AdventureData.RewardCollected;
			_missionAvailablePlayImage.enabled = _data.Info.IsUnlocked && !_data.Info.AdventureData.RewardCollected;
			_missionUnavailablePlayImage.enabled = !_data.Info.IsUnlocked;
			_buttonShiny.enabled = (_data.Info.IsCompleted && !_data.Info.AdventureData.RewardCollected) || _missionAvailablePlayImage.enabled;
			
			if (!_data.Info.IsUnlocked)
			{
				_buttonText.text = ScriptLocalization.MainMenu.Locked;
			}
			else if (_data.Info.IsCompleted)
			{
				_buttonText.text = _data.Info.AdventureData.RewardCollected ? 
					                   ScriptLocalization.MainMenu.PlayGame : 
					                   ScriptLocalization.MainMenu.Collect;
			}
			else
			{
				_buttonText.text = ScriptLocalization.MainMenu.PlayGame;
			}
			
			_lockIcon.SetActive(!_data.Info.IsUnlocked);
			_defeatedHolder.SetActive(_data.Info.IsCompleted);
			_undefeatedHolder.SetActive(!_data.Info.IsCompleted);
		}

		private void SetRewardsDataView()
		{
			_smallCardPool?.DespawnAll();
			
			_firstVictoryRewardStrikethrough.SetActive(_data.Info.AdventureData.RewardCollected);

			if (!_data.Info.AdventureData.RewardCollected)
			{
				_rewardsTitleText.text = ScriptLocalization.MainMenu.FirstVictoryRewards;
				
				foreach (var reward in _data.Info.Config.FirstClearReward)
				{
					_smallCardPool.Spawn().Initialise(reward.Key, (int)reward.Value);
				}
			}
			else
			{
				_firstVictoryRewardStrikethrough.SetActive(false);
				_rewardsTitleText.text = ScriptLocalization.MainMenu.PossibleRewards;
				
				_smallCardPool.Spawn().Initialise(GameId.SC, -1);
				_smallCardPool.Spawn().Initialise(GameId.XP, -1);
			}
		}

		private void SetDifficultyDataView()
		{
			_reccomendedPowerText.text = _data.Info.Config.RecommendedPower.ToString();
			_difficultyText.text = _data.Info.Config.Difficulty.ToString();
			_hardnessShaderGradient.color1 = _hardnessLeftGradient[(int)_data.Info.Config.Difficulty];
			_hardnessShaderGradient.color2 = _hardnessRightGradient[(int)_data.Info.Config.Difficulty];

			switch (_data.Info.Config.Difficulty)
			{
				case AdventureDifficultyLevel.Normal: _difficultyText.color = Color.green; break;
				case AdventureDifficultyLevel.Hard: _difficultyText.color = Color.yellow; break;
				case AdventureDifficultyLevel.Master: _difficultyText.color = Color.red; break;
			}
		}

		private void OnButtonClick()
		{
			if (!_data.Info.IsUnlocked)
			{
				// Do we need to complete an easier difficulty first?
				var unlockRequirementAdventure =
					_gameDataProvider.AdventureDataProvider.GetInfo(_data.Info.Config.UnlockedAdventureRequirement);

				var unlockString = string.Format(ScriptLocalization.MainMenu.BeatLevelFirst,
					unlockRequirementAdventure.Config.Chapter, (unlockRequirementAdventure.Config.Stage));
				
				_mainMenuServices.UiVfxService.PlayFloatingTextAtPosition(unlockString, _lockIcon.transform.position);

				return;
			}
			
			if (_data.Info.IsCompleted && !_data.Info.AdventureData.RewardCollected)
			{
				_services.CommandService.ExecuteCommand(new CollectFirstTimeAdventureRewardsCommand{AdventureId = _data.Info.Config.Id});

				return;
			}
			
			_gameDataProvider.AdventureDataProvider.AdventureSelectedId.Value = _data.Info.AdventureData.Id;
		}

	}
}

