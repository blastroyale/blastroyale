using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.BattlePassViews
{
	/// <summary>
	/// This class handles displaying relevant information about a level of a battle pass (progress, rewards)
	/// </summary>
	public class BattlePassSegmentView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _levelText;
		[SerializeField, Required] private TextMeshProUGUI _progressText;
		[SerializeField, Required] private TextMeshProUGUI _rewardTitleText;
		[SerializeField, Required] private TextMeshProUGUI _rewardStatusText;
		[SerializeField, Required] private GameObject _rewardReadyToClaimObject;
		[SerializeField, Required] private GameObject _levelSegmentBackgroundReached;
		[SerializeField, Required] private GameObject _levelSegmentBackgroundNotReached;
		[SerializeField, Required] private Image _progressBar;
		[SerializeField, Required] private Image _rewardImage;
		[SerializeField, Required] private List<GameObject> _rewardClaimedObjects;
		[SerializeField, Required] private List<GameObject> _rewardNotClaimedObjects;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		
		public void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}
		
		public async void Init(BattlePassSegmentData data)
		{
			var levelForUi = data.SegmentLevelForRewards + 1;
			var isRewardClaimed = data.CurrentLevel >= data.SegmentLevelForRewards;
			
			foreach (var go in _rewardClaimedObjects)
			{
				go.SetActive(isRewardClaimed);
			}
			
			foreach (var go in _rewardNotClaimedObjects)
			{
				go.SetActive(!isRewardClaimed);
			}
			
			// Update reward card 
			_rewardReadyToClaimObject.SetActive(false);
			_rewardStatusText.gameObject.SetActive(false);
			_rewardTitleText.text = data.RewardConfig.GameId.GetTranslation().ToUpper();
			
			_rewardImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(data.RewardConfig.GameId);

			if (!isRewardClaimed && data.PredictedCurrentLevel >= data.SegmentLevelForRewards)
			{
				_rewardReadyToClaimObject.SetActive(true);
			}
			else if(!isRewardClaimed && (data.PredictedCurrentLevel+1) == data.SegmentLevelForRewards)
			{
				_rewardStatusText.gameObject.SetActive(true);
				_rewardStatusText.text = ScriptLocalization.MainMenu.BattlepassRewardClaimNext.ToUpper();
			}
			else if(!isRewardClaimed && (data.PredictedCurrentLevel+1) < data.SegmentLevelForRewards)
			{
				_rewardStatusText.gameObject.SetActive(true);
				_rewardStatusText.text = string.Format(ScriptLocalization.MainMenu.BattlepassRewardClaimFarOut, levelForUi).ToUpper();
			}
			else
			{
				_rewardStatusText.text = "";
			}

			// Update progress bar and level
			_levelText.text = levelForUi.ToString();
			_levelSegmentBackgroundReached.SetActive(data.PredictedCurrentLevel >= data.SegmentLevelForRewards);
			_levelSegmentBackgroundNotReached.SetActive(data.PredictedCurrentLevel < data.SegmentLevelForRewards);
			
			if (data.PredictedCurrentLevel > data.SegmentLevel)
			{
				_progressBar.fillAmount = 1f;
				_progressText.gameObject.SetActive(false);
			}
			else if (data.PredictedCurrentLevel == data.SegmentLevel)
			{
				_progressBar.fillAmount = (float) data.PredictedCurrentProgress / data.MaxProgress;
				_progressText.text = $"{data.PredictedCurrentProgress}/{data.MaxProgress}";
				_progressText.gameObject.SetActive(true);
			}
			else
			{
				_progressBar.fillAmount = 0;
				_progressText.gameObject.SetActive(false);
			}
		}
	}
	
	/// <summary>
	/// This class holds the data used to update BattlePassSegmentViews
	/// </summary>
	public class BattlePassSegmentData
	{
		public uint SegmentLevel;
		public uint CurrentLevel;
		public uint CurrentProgress;
		public uint PredictedCurrentLevel;
		public uint PredictedCurrentProgress;
		public uint MaxProgress;
		public BattlePassRewardConfig RewardConfig;

		public uint SegmentLevelForRewards => SegmentLevel + 1;
	}
}
