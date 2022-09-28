using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
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
			_levelText.text = (data.Level + 1).ToString();

			foreach (var go in _rewardClaimedObjects)
			{
				go.SetActive(data.IsRewardClaimed);
			}
			
			foreach (var go in _rewardNotClaimedObjects)
			{
				go.SetActive(!data.IsRewardClaimed);
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
		public uint MaxProgress;
		public BattlePassRewardConfig RewardConfig;
		public bool IsRewardClaimed;
	}
}
