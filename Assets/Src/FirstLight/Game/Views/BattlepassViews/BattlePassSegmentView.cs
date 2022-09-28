using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
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
		
		public void Init(BattlePassSegmentData data)
		{
			var levelForUi = data.LevelForRewards + 1;
			var isRewardClaimed = data.CurrentLevel >= data.LevelForRewards;
			
			foreach (var go in _rewardClaimedObjects)
			{
				go.SetActive(isRewardClaimed);
			}
			
			foreach (var go in _rewardNotClaimedObjects)
			{
				go.SetActive(!isRewardClaimed);
			}
			
			_levelText.text = levelForUi.ToString();
			_rewardTitleText.text = data.RewardConfig.Reward.GameId.ToString();

			_rewardReadyToClaimObject.SetActive(false);
			_rewardStatusText.gameObject.SetActive(false);
			
			if (!isRewardClaimed && data.RedeemableLevel >= data.LevelForRewards)
			{
				_rewardReadyToClaimObject.SetActive(true);
			}
			else if(!isRewardClaimed && (data.RedeemableLevel+1) == data.LevelForRewards)
			{
				_rewardStatusText.gameObject.SetActive(true);
				_rewardStatusText.text = ScriptLocalization.MainMenu.BattlepassRewardClaimNext.ToUpper();
			}
			else if(!isRewardClaimed && (data.RedeemableLevel+1) < data.LevelForRewards)
			{
				_rewardStatusText.gameObject.SetActive(true);
				_rewardStatusText.text = string.Format(ScriptLocalization.MainMenu.BattlepassRewardClaimFarOut, levelForUi).ToUpper();
			}
			else
			{
				_rewardStatusText.text = "";
			}

			if (data.RedeemableLevel > data.Level)
			{
				_progressBar.fillAmount = 1f;
				_progressText.text = "";
			}
			else if (data.RedeemableLevel == data.Level)
			{
				_progressBar.fillAmount = (float) data.RedeemableProgress / data.MaxProgress;
				_progressText.text = $"{data.RedeemableProgress}/{data.MaxProgress}";
			}
			else
			{
				_progressBar.fillAmount = 0;
				_progressText.text = "";
			}
		}
	}
	
	/// <summary>
	/// This class holds the data used to update BattlePassSegmentViews
	/// </summary>
	public class BattlePassSegmentData
	{
		public uint Level;
		public uint CurrentLevel;
		public uint CurrentProgress;
		public uint RedeemableLevel;
		public uint RedeemableProgress;
		public uint MaxProgress;
		public BattlePassRewardConfig RewardConfig;

		public uint LevelForRewards => Level + 1;
	}
}
